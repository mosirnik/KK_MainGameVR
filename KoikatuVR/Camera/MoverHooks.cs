using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Core;
using UnityEngine;
using HarmonyLib;
using System.Collections;
using System.Reflection;

// This file is a collection of hooks to move the VR camera at appropriate
// points of the game.

namespace KoikatuVR.Camera
{
    [HarmonyPatch(typeof(ADV.TextScenario))]
    class TextScenarioPatches1
    {
        [HarmonyPatch("ADVCameraSetting")]
        [HarmonyPostfix]
        static void PostADVCameraSetting(ADV.TextScenario __instance)
        {
            if (Manager.Scene.IsInstance() && Manager.Scene.Instance.NowSceneNames[0] == "Talk")
            {
                // Talk scenes are handled separately.
                return;
            }

            VRLog.Debug("PostADVCameraSetting");
            var backTrans = __instance.BackCamera?.transform;
            if (backTrans == null)
            {
                // backTrans can be null in Roaming. We don't want to move the
                // camera anyway in that case.
                return;
            }
            VRMover.Instance.MaybeMoveADV(__instance, backTrans.position, backTrans.rotation, keepHeight: false);
        }
    }

    [HarmonyPatch]
    class RequestNextLinePatches
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            // In some versions of the base game, MainScenario._RequestNextLine
            // duplicates the logic found in TextScenario._RequestNextLine.
            // In other versions, the former simply calls the latter.
            // We want to patch both methods or the latter alone depending on
            // the version.
            yield return AccessTools.Method(typeof(ADV.TextScenario), "_RequestNextLine");
            if (AccessTools.Field(typeof(ADV.MainScenario), "textHash") == null)
            {
                yield return AccessTools.Method(typeof(ADV.MainScenario), "_RequestNextLine");
            }
        }

        static void Postfix(ADV.TextScenario __instance, ref IEnumerator __result)
        {
            if (Manager.Scene.IsInstance() && Manager.Scene.Instance.NowSceneNames[0] == "Talk")
            {
                // Talk scenes are handled separately.
                return;
            }

            if (__instance.advScene == null)
            {
                // Outside ADV scene (probably roaming), ignore.
                return;
            }

            __result = new[] { __result, Postfix() }.GetEnumerator();

            IEnumerator Postfix()
            {
                VRMover.Instance.HandleTextScenarioProgress(__instance);
                yield break;
            }
        }
    }

    [HarmonyPatch(typeof(ADV.Program))]
    class ProgramPatches1
    {
        [HarmonyPatch(nameof(ADV.Program.SetNull))]
        [HarmonyPostfix]
        static void PostSetNull(Transform transform)
        {
            VRLog.Debug("PostSetNull");
            VRMover.Instance.MaybeMoveTo(transform.position, transform.rotation, keepHeight: false);
        }
    }

    [HarmonyPatch(typeof(HSceneProc))]
    class HSceneProcPatches
    {
        [HarmonyPatch("ChangeAnimator")]
        [HarmonyPostfix]
        public static void PostChangeAnimator(HSceneProc __instance, bool _isForceCameraReset, List<ChaControl> ___lstFemale)
        {
            if (_isForceCameraReset)
            {
                UpdateVRCamera(__instance, ___lstFemale, null);
            }
        }

        [HarmonyPatch("ChangeCategory")]
        [HarmonyPrefix]
        public static void PreChangeCategory(List<ChaControl> ___lstFemale, out float __state)
        {
            __state = ___lstFemale[0].objTop.transform.position.y;
        }

        [HarmonyPatch("ChangeCategory")]
        [HarmonyPostfix]
        public static void PostChangeCategory(HSceneProc __instance, List<ChaControl> ___lstFemale, float __state)
        {
            UpdateVRCamera(__instance, ___lstFemale, __state);
        }


        /// <summary>
        /// Update the transform of the VR camera.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="lstFemale"></param>
        static void UpdateVRCamera(HSceneProc instance, List<ChaControl> lstFemale, float? previousFemaleY)
        {
            var baseTransform = lstFemale[0].objTop.transform;
            var camDat = new Traverse(instance.flags.ctrlCamera).Field<BaseCameraControl_Ver2.CameraData>("CamDat").Value;
            var cameraRotation = baseTransform.rotation * Quaternion.Euler(camDat.Rot);
            Vector3 dir;
            switch (instance.flags.mode)
            {
                case HFlag.EMode.masturbation:
                case HFlag.EMode.peeping:
                case HFlag.EMode.lesbian:
                    // Use the default distance for 3rd-person scenes.
                    dir = camDat.Dir;
                    break;
                default:
                    // Start closer otherwise.
                    dir = Vector3.back * 0.8f;
                    break;
            }
            var cameraPosition = cameraRotation * dir + baseTransform.TransformPoint(camDat.Pos);
            if (previousFemaleY is float prevY)
            {
                // Keep the relative Y coordinate from the female.
                var cameraHeight = VR.Camera.transform.position.y + baseTransform.position.y - prevY;
                var destination = new Vector3(cameraPosition.x, cameraHeight, cameraPosition.z);
                VRMover.Instance.MaybeMoveTo(destination, cameraRotation, keepHeight: false);
            }
            else
            {
                // We are starting from scratch.
                // TODO: the height calculation below assumes standing mode.
                var cameraHeight = lstFemale[0].transform.position.y + VR.Camera.transform.localPosition.y;
                var destination = new Vector3(cameraPosition.x, cameraHeight, cameraPosition.z);
                VRMover.Instance.MoveTo(destination, cameraRotation, keepHeight: false);
            }
        }
    }
}
