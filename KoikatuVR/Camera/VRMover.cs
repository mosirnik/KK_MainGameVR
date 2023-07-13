using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using HarmonyLib;
using System.Collections;

namespace KoikatuVR.Camera
{
    /// <summary>
    /// A class responsible for moving the VR camera.
    /// </summary>
    public class VRMover
    {
        public static VRMover Instance {
            get {
                if (_instance == null)
                {
                    _instance = new VRMover();
                }
                return _instance;
            }
        }
        private static VRMover _instance;

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private KoikatuSettings _settings;

        public delegate void OnMoveAction();
        public event OnMoveAction OnMove;

        public VRMover()
        {
            _lastPosition = Vector3.zero;
            _lastRotation = Quaternion.identity;
            _settings = VR.Settings as KoikatuSettings;
        }

        /// <summary>
        /// Move the camera to the specified pose.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="keepHeight"></param>
        public void MoveTo(Vector3 position, Quaternion rotation, bool keepHeight, bool quiet = false)
        {
            if (!quiet)
            {
                VRLog.Debug($"Moving camera to {position} {rotation.eulerAngles}");
            }
            _lastPosition = position;
            _lastRotation = rotation;
            VR.Mode.MoveToPosition(position, rotation, ignoreHeight: keepHeight);
            OnMove?.Invoke();
        }

        /// <summary>
        /// Move the camera using some heuristics.
        ///
        /// The position and rotation arguments should represent the pose
        /// the camera would take in the 2D version of the game.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="keepHeight"></param>
        public void MaybeMoveTo(Vector3 position, Quaternion rotation, bool keepHeight)
        {
            MoveWithHeuristics(position, rotation, keepHeight, pretendFading: false);
        }

        /// <summary>
        /// Similar to MaybeMoveTo, but also considers the ADV fade state.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="keepHeight"></param>
        public void MaybeMoveADV(ADV.TextScenario textScenario, Vector3 position, Quaternion rotation, bool keepHeight)
        {
            var advFade = new Traverse(textScenario).Field<ADVFade>("advFade").Value;
            MoveWithHeuristics(position, rotation, keepHeight, pretendFading: !advFade.IsEnd);
        }

        /// <summary>
        /// This should be called every time a set of ADV commands has been executed.
        /// Moves the camera appropriately.
        /// </summary>
        /// <param name="textScenario"></param>
        public void HandleTextScenarioProgress(ADV.TextScenario textScenario)
        {
            bool isFadingOut = IsFadingOut(new Traverse(textScenario).Field<ADVFade>("advFade").Value);

            VRLog.Debug($"HandleTextScenarioProgress isFadingOut={isFadingOut}");

            if (_settings.FirstPersonADV &&
                FindMaleToImpersonate(out var male) &&
                male.objHead != null)
            {
                VRLog.Debug("Maybe impersonating male");
                male.StartCoroutine(ImpersonateCo(isFadingOut, male.objHead.transform));
            }
            else if (ShouldApproachCharacter(textScenario, out var character))
            {
                var distance = InCafe() ? 0.95f :  0.7f;
                float height;
                Quaternion rotation;
                if (Manager.Scene.Instance.NowSceneNames[0] == "H")
                {
                    VRLog.Debug("Approaching character (H)");
                    // TODO: find a way to get a proper height.
                    height = character.transform.position.y + 1.3f;
                    rotation = character.transform.rotation * Quaternion.AngleAxis(180f, Vector3.up);
                }
                else
                {
                    VRLog.Debug("Approaching character (non-H)");
                    var originalTarget = Camera.ActionCameraControl.GetIdealTransformFor(textScenario.AdvCamera);
                    height = originalTarget.position.y;
                    rotation = originalTarget.rotation;
                }
                var cameraXZ = character.transform.position - rotation * (distance * Vector3.forward);
                MoveWithHeuristics(
                    new Vector3(cameraXZ.x, height, cameraXZ.z),
                    rotation,
                    keepHeight: false,
                    pretendFading: isFadingOut);
            }
            else
            {
                var target = Camera.ActionCameraControl.GetIdealTransformFor(textScenario.AdvCamera);
                MoveWithHeuristics(target.position, target.rotation, keepHeight: false, pretendFading: isFadingOut);
            }
        }

        private bool IsFadingOut(ADVFade fade)
        {
            bool IsFadingOutSub(ADVFade.Fade f)
            {
                return f.initColor.a > 0.5f && !f.IsEnd;
            }

            var trav = new Traverse(fade);
            return IsFadingOutSub(trav.Field<ADVFade.Fade>("front").Value) ||
                IsFadingOutSub(trav.Field<ADVFade.Fade>("back").Value);
        }

        private IEnumerator ImpersonateCo(bool isFadingOut, Transform head)
        {
            // For reasons I don't understand, the male may not have a correct pose
            // until later in the update loop.
            yield return new WaitForEndOfFrame();
            MoveWithHeuristics(
                head.TransformPoint(0, 0.15f, 0.15f),
                head.rotation,
                keepHeight: false,
                pretendFading: isFadingOut);
        }

        private void MoveWithHeuristics(Vector3 position, Quaternion rotation, bool keepHeight, bool pretendFading)
        {
            var fade = Manager.Scene.Instance.sceneFade;
            bool fadeOk = (fade._Fade == SimpleFade.Fade.Out) ^ fade.IsEnd;
            if (pretendFading || fadeOk || IsDestinationFar(position, rotation))
            {
                MoveTo(position, rotation, keepHeight);
            }
            else
            {
                VRLog.Debug("Not moving because heuristic conditions are not met");
            }
        }

        private bool IsDestinationFar(Vector3 position, Quaternion rotation)
        {
            var distance = (position - _lastPosition).magnitude;
            var angleDistance = Mathf.DeltaAngle(rotation.eulerAngles.y, _lastRotation.eulerAngles.y);
            return 1f < distance / 2f + angleDistance / 90f;
        }

        private bool FindMaleToImpersonate(out ChaControl male)
        {
            male = null;

            if (!Manager.Character.IsInstance())
            {
                return false;
            }

            var males = Manager.Character.Instance.dictEntryChara.Values
                .Where(ch => ch.isActiveAndEnabled && ch.sex == 0 && ch.objTop?.activeSelf == true && ch.visibleAll)
                .ToArray();
            if (males.Length == 1)
            {
                male = males[0];
                return true;
            }
            return false;
        }

        private bool ShouldApproachCharacter(ADV.TextScenario textScenario, out ChaControl control)
        {
            if ((Manager.Scene.Instance.NowSceneNames[0] == "H" || textScenario.BGParam.visible) &&
                textScenario.currentChara != null)
            {
                control = textScenario.currentChara.chaCtrl;
                return true;
            }
            control = null;
            return false;
        }

        private static bool InCafe()
        {
            return Manager.Game.IsInstance() &&
                Manager.Game.Instance.actScene.transform.Find("cafeChair");
        }
    }
}
