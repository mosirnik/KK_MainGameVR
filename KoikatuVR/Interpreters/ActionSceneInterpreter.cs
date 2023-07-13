using UnityEngine;
using VRGIN.Core;
using WindowsInput.Native;
using StrayTech;

namespace KoikatuVR.Interpreters
{
    class ActionSceneInterpreter : SceneInterpreter
    {
        private KoikatuSettings _Settings;
        private ActionScene _ActionScene;

        private GameObject _Map;
        private GameObject _CameraSystem;
        private bool _NeedsResetCamera;
        private bool _IsStanding = true;
        private bool _Walking = false;
        private bool _Dashing = false; // ダッシュ時は_Walkingと両方trueになる

        public override void OnStart()
        {
            VRLog.Info("ActionScene OnStart");

            _Settings = (VR.Context.Settings as KoikatuSettings);
            _ActionScene = GameObject.FindObjectOfType<ActionScene>();

            ResetState();
            HoldCamera();
        }

        public override void OnDisable()
        {
            VRLog.Info("ActionScene OnDisable");

            ResetState();
            ReleaseCamera();
        }

        private void ResetState()
        {
            VRLog.Info("ActionScene ResetState");

            StandUp();
            StopWalking();
            _NeedsResetCamera = false;
        }

        private void ResetCamera()
        {
            var pl = _ActionScene.Player?.chaCtrl.objTop;

            if (pl != null && pl.activeSelf)
            {
                _CameraSystem = MonoBehaviourSingleton<CameraSystem>.Instance.gameObject;

                // トイレなどでFPS視点になっている場合にTPS視点に戻す
                Compat.CameraStateDefinitionChange_ModeChangeForce(
                    _CameraSystem.GetComponent<ActionGame.CameraStateDefinitionChange>(),
                    (ActionGame.CameraMode?) ActionGame.CameraMode.TPS);
                //scene.GetComponent<ActionScene>().isCursorLock = false;

                // カメラをプレイヤーの位置に移動
                MoveCameraToPlayer();

                _NeedsResetCamera = false;
                VRLog.Info("ResetCamera succeeded");
            }
        }

        private void HoldCamera()
        {
            VRLog.Info("ActionScene HoldCamera");

            _CameraSystem = MonoBehaviourSingleton<CameraSystem>.Instance.gameObject;

            if (_CameraSystem != null)
            {
                _CameraSystem.SetActive(false);

                VRLog.Info("succeeded");
            }
        }

        private void ReleaseCamera()
        {
            VRLog.Info("ActionScene ReleaseCamera");

            if (_CameraSystem != null)
            {
                _CameraSystem.SetActive(true);

                VRLog.Info("succeeded");
            }
        }

        public override void OnUpdate()
        {
            GameObject map = _ActionScene.Map.mapRoot?.gameObject;

            if (map != _Map)
            {

                VRLog.Info("! map changed.");

                ResetState();
                _Map = map;
                _NeedsResetCamera = true;
            }

            if (_Walking)
            {
                MoveCameraToPlayer(true, true);
            }

            if (_NeedsResetCamera)
            {
                ResetCamera();
            }

            UpdateCrouch();
        }

        private void UpdateCrouch()
        {
            var pl = _ActionScene.Player?.chaCtrl.objTop;

            if (_Settings.CrouchByHMDPos && pl?.activeInHierarchy == true)
            {
                var cam = VR.Camera.transform;
                var delta_y = cam.position.y - pl.transform.position.y;

                if (_IsStanding && delta_y < _Settings.CrouchThreshold)
                {
                    Crouch();
                }
                else if (!_IsStanding && delta_y > _Settings.StandUpThreshold)
                {
                    StandUp();
                }
            }
        }

        public void MoveCameraToPlayer(bool onlyPosition = false, bool quiet = false)
        {
            var player = _ActionScene.Player;

            var playerHead = player.chaCtrl.objHead.transform;
            var headCam = VR.Camera.transform;

            Vector3 cf = Vector3.Scale(player.transform.forward, new Vector3(1, 0, 1)).normalized;

            Vector3 pos;
            if (_Settings.UsingHeadPos)
            {
                pos = playerHead.position;
            }
            else
            {
                pos = player.position;
                pos.y += _IsStanding ? _Settings.StandingCameraPos : _Settings.CrouchingCameraPos;
            }

            Camera.VRMover.Instance.MoveTo(
                pos + cf * 0.23f, // 首が見えるとうざいのでほんの少し前目にする
                onlyPosition ? headCam.rotation : player.rotation,
                keepHeight: false,
                quiet: quiet);
        }

        public void MovePlayerToCamera(bool onlyRotation = false)
        {
            var player = _ActionScene.Player;
            var playerHead = player.chaCtrl.objHead.transform;
            var headCam = VR.Camera.transform;

            var pos = headCam.position;
            pos.y += player.position.y - playerHead.position.y;

            var delta_y = headCam.rotation.eulerAngles.y - player.rotation.eulerAngles.y;
            player.transform.Rotate(Vector3.up * delta_y);
            Vector3 cf = Vector3.Scale(player.transform.forward, new Vector3(1, 0, 1)).normalized;

            if (!onlyRotation)
            {
                player.position = pos - cf * 0.1f;
            }
        }

        public void Crouch()
        {
            if (_IsStanding)
            {
                _IsStanding = false;
                VR.Input.Keyboard.KeyDown(VirtualKeyCode.VK_Z);
            }
        }

        public void StandUp()
        {
            if (!_IsStanding)
            {
                _IsStanding = true;
                VR.Input.Keyboard.KeyUp(VirtualKeyCode.VK_Z);
            }
        }

        public void StartWalking(bool dash = false)
        {
            MovePlayerToCamera(true);

            if (!dash)
            {
                VR.Input.Keyboard.KeyDown(VirtualKeyCode.SHIFT);
                _Dashing = true;
            }

            VR.Input.Mouse.LeftButtonDown();
            _Walking = true;
            // Force hide the protagonist's head while walking, so that it
            // remains hidden when the game lags.
            VRMale.ForceHideHead = true;
        }

        public void StopWalking()
        {
            VR.Input.Mouse.LeftButtonUp();

            if (_Dashing)
            {
                VR.Input.Keyboard.KeyUp(VirtualKeyCode.SHIFT);
                _Dashing = false;
            }

            _Walking = false;
            VRMale.ForceHideHead = false;
        }
    }
}
