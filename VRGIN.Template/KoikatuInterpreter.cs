using System;
using UnityEngine;
using VRGIN.Core;
using WindowsInput.Native;

namespace KoikatuVR
{
    class KoikatuInterpreter : GameInterpreter
    {
        public const int OtherScene = -1;
        public const int ActionScene = 0;
        public const int TalkScene = 1;
        public const int HScene = 2;
        public const int NightMenuScene = 2;

        private KoikatuSettings _Settings;
        private int _Scene;
        private GameObject _Map;
        private GameObject _CameraSystem;
        private bool _NeedsResetCamera = false;
        private bool _NeedsMoveCamera = false;
        private bool _IsStanding = true;
        private bool _Walking = false;
        private bool _Dashing = false; // ダッシュ時は_Walkingと両方trueになる
        private int _MoveCameraWaitTime = 0;

        public void MoveCameraToPlayer(bool onlyPosition = false)
        {
            var player = GameObject.Find("ActionScene/Player").transform;
            var playerHead = player.transform.Find("chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head");
            //var playerHead = GameObject.Find("ActionScene/Player/chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head").transform;
            var cam = GameObject.Find("VRGIN_Camera (origin)").transform;
            var headCam = GameObject.Find("VRGIN_Camera (origin)/VRGIN_Camera (eye)/VRGIN_Camera (head)").transform;

            // 歩いているときに回転をコピーするとおかしくなるバグの暫定対策
            // 歩く方向がHMDの方向基準なので歩いている時はコピーしなくても回転は一致する
            if (!onlyPosition)
            {
                cam.rotation = player.rotation;
                var delta_y = cam.rotation.eulerAngles.y - headCam.rotation.eulerAngles.y;
                cam.Rotate(Vector3.up * delta_y);
            }

            Vector3 cf = Vector3.Scale(player.forward, new Vector3(1, 0, 1)).normalized;
            
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
            cam.position = pos - (headCam.position - cam.position) + cf * 0.13f; // 首が見えるとうざいのでほんの少し前目
        }

        public void MovePlayerToCamera(bool onlyRotation = false)
        {
            var player = GameObject.Find("ActionScene/Player").transform;
            var playerHead = player.transform.Find("chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head");
            //var playerHead = GameObject.Find("ActionScene/Player/chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head").transform;
            //var cam = GameObject.Find("VRGIN_Camera (origin)").transform;
            var headCam = GameObject.Find("VRGIN_Camera (origin)/VRGIN_Camera (eye)/VRGIN_Camera (head)").transform;

            var pos = headCam.position;
            pos.y += player.position.y - playerHead.position.y;

            var delta_y = headCam.rotation.eulerAngles.y - player.rotation.eulerAngles.y;
            player.Rotate(Vector3.up * delta_y);
            Vector3 cf = Vector3.Scale(player.forward, new Vector3(1, 0, 1)).normalized;

            if (!onlyRotation)
            {
                player.position = pos - cf * 0.1f;
            }
        }

        public void RotatePlayer(int angle)
        {
            var player = GameObject.Find("ActionScene/Player").transform;
            player.Rotate(Vector3.up * angle);
            _NeedsMoveCamera = true;
        }

        public void Crouch()
        {
            if (_IsStanding)
            {
                _IsStanding = false;
                VR.Input.Keyboard.KeyDown(VirtualKeyCode.VK_Z);

                // 数F待ってから視点移動する
                //_NeedsMoveCamera = true;
                _MoveCameraWaitTime = 30;
            }
        }

        public void StandUp()
        {
            if (!_IsStanding)
            {
                _IsStanding = true;
                VR.Input.Keyboard.KeyUp(VirtualKeyCode.VK_Z);

                // 数F待ってから視点移動する
                //_NeedsMoveCamera = true;
                _MoveCameraWaitTime = 30;
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
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _Settings = (VR.Context.Settings as KoikatuSettings);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            DetectScene();
            
            if (_Scene == HScene)
            {
                if (_NeedsResetCamera)
                {
                    ResetCameraH();
                }
            }
            else if (_Scene == ActionScene)
            {
                GameObject map = GameObject.Find("Map");

                if (map != _Map)
                {

                    VRLog.Info("! map changed.");

                    ResetState();
                    _Map = map;
                    _NeedsResetCamera = true;
                }
                
                UpdateCrouch();

                if (_MoveCameraWaitTime > 0)
                {
                    _MoveCameraWaitTime--;

                    if (_MoveCameraWaitTime == 0)
                    {
                        _NeedsMoveCamera = true;
                    }
                }

                if (_NeedsMoveCamera || _Walking)
                {
                    MoveCameraToPlayer(_Walking);
                    _NeedsMoveCamera = false;
                    _MoveCameraWaitTime = 0;
                }

                if (_NeedsResetCamera)
                {
                    ResetCameraAction();
                }
            }
            else
            {
                _NeedsResetCamera = false;
            }

            //HoldCamera();
        }

        private void UpdateCrouch()
        {
            if (_Settings.CrouchByHMDPos)// && _CameraSystem != null)
            {
                var cam = GameObject.Find("VRGIN_Camera (origin)").transform;
                var headCam = GameObject.Find("VRGIN_Camera (origin)/VRGIN_Camera (eye)/VRGIN_Camera (head)").transform;
                var delta_y = cam.position.y - headCam.position.y;

                if (_IsStanding && delta_y > _Settings.CrouchThrethould)
                {
                    Crouch();
                }
                else if (!_IsStanding && delta_y < _Settings.StandUpThrethould)
                {
                    StandUp();
                }
            }
        }

        private void ResetState()
        {
            StandUp();
            StopWalking();
            _NeedsResetCamera = true;
            _NeedsMoveCamera = false;
            _MoveCameraWaitTime = 0;
        }

        private void ResetCameraAction()
        {
            var pl = GameObject.Find("ActionScene/Player/chaM_001/BodyTop");
            //_CameraSystem = GameObject.Find("ActionScene/CameraSystem");

            if (pl != null && pl.activeSelf)
            {
                _CameraSystem = GameObject.Find("ActionScene").transform.Find("CameraSystem").gameObject;

                var player = GameObject.Find("ActionScene/Player").GetComponent<ActionGame.Chara.Player>();

                // トイレなどでFPS視点になっている場合にTPS視点に戻す
                _CameraSystem.GetComponent<ActionGame.CameraStateDefinitionChange>().ModeChange(ActionGame.CameraMode.TPS, player);

                // プレイヤーキャラの頭を非表示にする
                pl.transform.Find("p_cf_body_bone_low/cf_j_root").gameObject.SetActive(false);

                // カメラをプレイヤーの位置に移動
                MoveCameraToPlayer();

                _NeedsResetCamera = false;
                VRLog.Info("ResetCamera succeeded");
            }
        }

        private void ResetCameraH()
        {
            var cam = GameObject.FindObjectOfType<CameraControl_Ver2>();

            if (cam != null)
            {
                cam.enabled = false;
                _NeedsResetCamera = false;

                VRLog.Info("! done.");
            }
            VRLog.Info("! camera not found.");
        }

        private void HoldCamera()
        {
            _CameraSystem = GameObject.Find("ActionScene").transform.Find("CameraSystem").gameObject;

            if (_CameraSystem != null)
            {
                _CameraSystem.SetActive(false);
            }
        }

        private void ReleaseCamera()
        {
            if (_CameraSystem != null)
            {
                _CameraSystem.SetActive(true);
            }
        }

        private void DetectScene()
        {
            bool changed = false;

            if (GameObject.Find("TalkScene") != null)
            {
                if (_Scene != TalkScene)
                {
                    _Scene = TalkScene;
                    changed = true;
                    VRLog.Info("Start TalkScene");
                }
            }

            else if (GameObject.Find("HScene") != null)
            {
                if (_Scene != HScene)
                {
                    _Scene = HScene;
                    changed = true;
                    VRLog.Info("Start HScene");
                }
            }

            else if (GameObject.Find("NightMenuScene") != null)
            {
                if (_Scene != NightMenuScene)
                {
                    _Scene = NightMenuScene;
                    changed = true;
                    VRLog.Info("Start NightMenuScene");

                    // 夜メニューの時点で、登校時のイベントでちょうどいい位置に移動しておく
                    // これによってキャラクターレイヤの光源がうまく適用されない場合があるのも回避できてる？
                    var player = GameObject.Find("ActionScene/Player").transform;
                    var playerHead = GameObject.Find("ActionScene/Player/chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head").transform;
                    var cam = GameObject.Find("VRGIN_Camera (origin)").transform;
                    var headCam = GameObject.Find("VRGIN_Camera (origin)/VRGIN_Camera (eye)/VRGIN_Camera (head)").transform;

                    cam.rotation = player.rotation;
                    var delta_y = 180 + cam.rotation.eulerAngles.y - headCam.rotation.eulerAngles.y;
                    cam.Rotate(Vector3.up * delta_y);

                    Vector3 cf = Vector3.Scale(player.forward, new Vector3(1, 0, 1)).normalized;

                    Vector3 pos;
                    pos = playerHead.position;
                    cam.position = pos - (headCam.position - cam.position) + cf;
                }
            }

            else if (GameObject.Find("ActionScene") != null)
            {
                if (_Scene != ActionScene)
                {
                    _Scene = ActionScene;
                    HoldCamera();

                    changed = true;
                    VRLog.Info("Start ActionScene");
                }
            }

            else
            {
                if (_Scene != OtherScene)
                {
                    _Scene = OtherScene;
                    changed = true;
                    VRLog.Info("Start OtherScene");
                }
            }

            if (changed)
            {
                ReleaseCamera();
                ResetState();
            }
        }
    }
}
