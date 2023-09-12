using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using KoikatuVR.Interpreters;
using VRGIN.Core;
using VRGIN.Controls;
using UnityEngine;

namespace KoikatuVR.Controls
{
    class KoikatuWarpTool : VRGIN.Controls.Tools.WarpTool
    {
        private KoikatuInterpreter _interpreter;
        private KoikatuSettings _settings;
        private GameObject _protagonistToFollow;

        protected override void OnStart()
        {
            base.OnStart();
            _interpreter = VR.Interpreter as KoikatuInterpreter;
            _settings = VR.Settings as KoikatuSettings;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Camera.VRMover.Instance.OnMove += OnCameraMove;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Camera.VRMover.Instance.OnMove -= OnCameraMove;
        }

        protected override void OnUpdate()
        {
            var origin = VR.Camera.Origin;
            var oldOriginPosition = origin.position;
            var oldOriginRotation = origin.rotation;
            base.OnUpdate();

            // Detect teleporting in Roam mode.
            if (_settings.TeleportWithProtagonist &&
                (origin.position - oldOriginPosition).sqrMagnitude > 0.04f &&
                _interpreter.SceneInterpreter is ActionSceneInterpreter act &&
                GameObject.Find("ActionScene/Player") is GameObject player &&
                player.activeInHierarchy)
            {
                // It looks like we either just teleported or become upright.
                var diffRotation = origin.rotation * Quaternion.Inverse(oldOriginRotation);
                var nonHorizontal = Vector3.Angle(Vector3.up, diffRotation * Vector3.up);
                if (nonHorizontal < 0.1f)
                {
                    // Looks like we teleported.
                    act.MovePlayerToCamera();
                    // We undo the camera movement because we want the actual
                    // teleportation to happen after the game has a chance
                    // to correct the protagonist's position.
                    origin.SetPositionAndRotation(oldOriginPosition, oldOriginRotation);
                    _protagonistToFollow = player;
                }
            }
        }

        protected override void OnLateUpdate()
        {
            base.OnLateUpdate();
            if (_protagonistToFollow != null)
            {
                var player = _protagonistToFollow.GetComponent<ActionGame.Chara.Player>();
                StartCoroutine(FollowDelayedCo(player));
                _protagonistToFollow = null;
            }
        }


        private IEnumerator FollowDelayedCo(ActionGame.Chara.Player player)
        {
            // Temporarily hide the protagonist.
            bool oldActive = player.chaCtrl.objTop.activeSelf;
            player.chaCtrl.objTop.SetActive(false);
            // Wait for the game to correct the protagonist's position.
            yield return null;
            if (_interpreter.SceneInterpreter is ActionSceneInterpreter act)
            {
                VRLog.Debug("Following player");
                act.MoveCameraToPlayer();
            }
            player.chaCtrl.objTop.SetActive(oldActive);
        }

        private void OnCameraMove()
        {
            OnPlayAreaUpdated();
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[] {
                ToolUtil.HelpTrackpadCenter(Owner, "Press to teleport"),
                ToolUtil.HelpGrip(Owner, "Hold to move"),
            }.Where(x => x != null));
        }
    }
}
