using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace KoikatuVR.Camera
{
    /// <summary>
    /// This component takes over control of an Action camera.
    ///
    /// An action camera is used in Roaming, Talk and ADV scenes
    /// (but not in H or Maker scenes), and has the following characteristics.
    ///
    /// * Its name is "ActionCamera" or "FrontCamera".
    /// * Its tag is "MainCamera".
    /// * It sometimes has a BaseCameraControl component attached to it.
    ///
    /// The base game has many parts of code that controls the pose
    /// (i.e. position and rotation) of the action camera.
    /// The situation gets rather complicated in VR because we want the camera
    /// to have the same pose as the VR camera, so that (a) characters looking
    /// at the camera do so correctly, (b) the directional light is correctly
    /// oriented, and (c) the protagonist walks in the right direction in
    /// Roaming.
    ///
    /// So our basic plan is:
    ///
    /// 1. Disable or patch out most code paths in the base game that changes
    ///   the action camera's pose.
    /// 2. In Update(), we copy the pose of the VR camera into the action
    ///   camera.
    ///
    /// Doing (1) is tricky because there are many code paths to worry about.
    /// It's not the end of the world if the pose of the main camera is
    /// wrong for one frame, but we want to make sure to at least deal with
    /// code that continuously moves the main camera.
    ///
    /// Some (but not all) attempts by the base game to move the action
    /// camera are redirected to move a dummy game object instead. This
    /// object is called VRIdealCamera, and is exposed to allow other parts
    /// of this plugin to use it as a target for moving.
    /// Actually moving the VR camera is outside the scope of this class.
    /// </summary>
    class ActionCameraControl : ProtectedBehaviour
    {
        public Transform VRIdealCamera { get; private set; }

        protected override void OnAwake()
        {
            VRIdealCamera = new GameObject("VRIdealCamera").transform;
            VRIdealCamera.SetPositionAndRotation(transform.position, transform.rotation);
            VRIdealCamera.gameObject.AddComponent<UnityEngine.Camera>().enabled = false;
            DontDestroyOnLoad(VRIdealCamera.gameObject);

            TransformDebug.targetTransform = transform;
        }

        protected override void OnUpdate()
        {
            var head = VR.Camera.Head;
            TransformDebug.targetTransform = null;
            transform.SetPositionAndRotation(head.position, head.rotation);
            TransformDebug.targetTransform = transform;
        }

        public void OnDestroy()
        {
            if (VR.Quitting)
            {
                return;
            }
            Destroy(VRIdealCamera.gameObject);
            TransformDebug.targetTransform = null;
        }

        public static Transform GetIdealTransformFor(Component c)
        {
            return GetIdealTransformForObject(c.gameObject);
        }

        public static Transform GetIdealTransformForObject(GameObject o)
        {
            // TODO: cache?
            var control = o.GetComponent<ActionCameraControl>();
            if (control != null)
            {
                return control.VRIdealCamera;
            }
            else
            {
                return o.transform;
            }
        }

        public static void SetIdealPositionAndRotation(Transform t, Vector3 position, Quaternion rotation)
        {
            GetIdealTransformFor(t).SetPositionAndRotation(position, rotation);
        }
    }

    [HarmonyPatch]
    class ActionCameraTransformPatches
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertySetter(typeof(BaseCameraControl), "CameraAngle");
            yield return AccessTools.Method(typeof(BaseCameraControl), "Reset");
            yield return AccessTools.Method(typeof(BaseCameraControl), "ForceCalculate");
            yield return AccessTools.Method(typeof(BaseCameraControl), "LateUpdate");
            yield return AccessTools.Method(typeof(BaseCameraControl), "TargetSet");
            yield return AccessTools.Method(typeof(BaseCameraControl), "FrontTarget");
            yield return AccessTools.Method(typeof(BaseCameraControl), "SetCamera", new[] { typeof(BaseCameraControl) });
            yield return AccessTools.Method(typeof(BaseCameraControl), "SetCamera", new[] { typeof(Vector3), typeof(Vector3), typeof(Quaternion), typeof(Vector3) });
            yield return AccessTools.Method(typeof(BaseCameraControl), "SetCamera", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(float) });

            yield return AccessTools.Method(typeof(ADV.Commands.Base.NullSet), "Do");
            yield return AccessTools.Method(typeof(ADV.Commands.Camera.LerpNullMove), "Do");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            // Replace read access to Component.transform with calls to
            // GetIdealTransformForComponent.
            var newMethod = AccessTools.Method(typeof(ActionCameraControl), nameof(ActionCameraControl.GetIdealTransformFor));
            foreach (var inst in code)
            {
                if ((inst.opcode == OpCodes.Call || inst.opcode == OpCodes.Callvirt) &&
                        inst.operand is MethodInfo method &&
                        method.ReflectedType == typeof(Component) &&
                        method.Name == "get_transform")
                {
                    yield return new CodeInstruction(OpCodes.Call, newMethod);
                }
                else
                {
                    yield return inst;
                }
            }
        }

        /*
        static void Prefix(MethodBase __originalMethod)
        {
            VRLog.Info("Intercepted operation to the action camera: {0}.{1}", __originalMethod.ReflectedType, __originalMethod.Name);
        }
        */
    }

    [HarmonyPatch(typeof(ADV.TextScenario))]
    class TextScenarioPatches
    {
        [HarmonyPatch("ADVCameraSetting")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransADVCameraSetting(IEnumerable<CodeInstruction> code)
        {
            // Replace a call to Transform.SetPositionAndRotation.
            foreach (var inst in code)
            {
                if ((inst.opcode == OpCodes.Call || inst.opcode == OpCodes.Callvirt) &&
                    inst.operand is MethodInfo method &&
                    method.ReflectedType == typeof(Transform) &&
                    method.Name == "SetPositionAndRotation")
                {
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(ActionCameraControl), nameof(ActionCameraControl.SetIdealPositionAndRotation)));
                }
                else
                {
                    yield return inst;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ADV.Program))]
    class ProgramPatches
    {
        [HarmonyPatch(nameof(ADV.Program.SetNull))]
        [HarmonyPrefix]
        static void PreSetNull(ref Transform transform)
        {
            transform = ActionCameraControl.GetIdealTransformFor(transform);
        }
    }

    [HarmonyPatch]
    class TalkScenePatches
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            // Our target is a particular lambda defined in TalkScene.Start.
            // The code below is ugly and fragile, but there doesn't seem to be
            // any good alternative.
            var nested1 = typeof(TalkScene).GetNestedType("<Start>c__Iterator0", BindingFlags.NonPublic);
            if (nested1 == null)
            {
                VRLog.Error("nested1 is null!");
                yield break;
            }

            var nested2 = nested1.GetNestedType("<Start>c__AnonStorey8", BindingFlags.NonPublic);
            if (nested2 == null)
            {
                VRLog.Error("nested2 is null");
                yield break;
            }

            yield return AccessTools.Method(nested2, "<>m__3");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            // Replace the second (!) call to Transform.SetPositionAndRotation.
            // This is laughably fragile, but it doesn't seem worthwhile to
            // make it more robust until an actual conflict is reported...
            int found = 0;
            foreach (var inst in code)
            {
                if ((inst.opcode == OpCodes.Call || inst.opcode == OpCodes.Callvirt) &&
                    inst.operand is MethodInfo method &&
                    method.ReflectedType == typeof(Transform) &&
                    method.Name == "SetPositionAndRotation" &&
                    found++ == 1)
                {
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(ActionCameraControl), nameof(ActionCameraControl.SetIdealPositionAndRotation)));
                }
                else
                {
                    yield return inst;
                }
            }
        }

    }

    [HarmonyPatch(typeof(ADV.EventCG.Data))]
    class EventCGDataPatches
    {
        [HarmonyPatch(nameof(ADV.EventCG.Data.camRoot), MethodType.Setter)]
        [HarmonyPrefix]
        static void PreSetCamRoot(ref Transform __0)
        {
            __0 = ActionCameraControl.GetIdealTransformFor(__0);
        }
    }

    class TransformDebug
    {
        public static Transform targetTransform;
    }
}
