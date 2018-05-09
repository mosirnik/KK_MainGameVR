using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;
using VRGIN.U46.Visuals;
using VRGIN.Visuals;
using static SteamVR_Controller;
using VRGIN.Native;
using static VRGIN.Native.WindowsInterop;
using WindowsInput.Native;

namespace KoikatuVR
{
    public class SchoolTool : Tool
    {
        private bool _Linked = false;
        private bool _IsStanding = true;
        private bool _Walking = false;
        private bool _NeedsMoveCamera = false;

        private KoikatuSettings _Settings;
        private KeySet _KeySet;
        private int _KeySetIndex = 0;

        // 手抜きのためNumpad方式で方向を保存
        private int _prevTouchDirection = -1;

        private void ChangeKeySet()
        {
            List<KeySet> keySets = _Settings.KeySets;

            _KeySetIndex = (_KeySetIndex + 1) % keySets.Count;
            _KeySet = keySets[_KeySetIndex];
        }

        public override Texture2D Image
        {
            get
            {
                return UnityHelper.LoadImage("icon_school.png");
            }
        }

        private void MoveCameraToPlayer()
        {
            var player = GameObject.Find("ActionScene/Player").transform;
            var playerHead = GameObject.Find("ActionScene/Player/chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head").transform;
            var cam = GameObject.Find("VRGIN_Camera (origin)").transform;
            var headCam = GameObject.Find("VRGIN_Camera (origin)/VRGIN_Camera (eye)/VRGIN_Camera (head)").transform;

            cam.rotation = player.rotation;
            var delta_y =  cam.rotation.eulerAngles.y - headCam.rotation.eulerAngles.y;
            cam.Rotate(Vector3.up * delta_y);

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

        private void MovePlayerToCamera(Boolean onlyRotation = false)
        {
            var player = GameObject.Find("ActionScene/Player").transform;
            var playerHead = GameObject.Find("ActionScene/Player/chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head/cf_s_head").transform;
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

        private void Rotate(int angle)
        {
            var player = GameObject.Find("ActionScene/Player").transform;
            player.Rotate(Vector3.up * angle);
            _NeedsMoveCamera = true;
        }

        // プレイヤーと一体化する
        private void LinkPlayer()
        {
            //if (!_Linked)
            //{
                var pl = GameObject.Find("ActionScene/Player/chaM_001/BodyTop");///p_cf_body_bone_low/cf_j_root");

                if (pl != null && pl.activeSelf)
                {
                    pl.transform.Find("p_cf_body_bone_low/cf_j_root").gameObject.SetActive(false);
                    MoveCameraToPlayer();
                    _Linked = true;
                }
                
            //}
        }

        private void UnlinkPlayer()
        {
            //if (_Linked)
            //{
                var pl = GameObject.Find("ActionScene/Player/chaM_001/BodyTop/");///p_cf_body_bone_low/cf_j_root");

                if (pl != null)
                {
                    pl.transform.Find("p_cf_body_bone_low/cf_j_root").gameObject.SetActive(true);
                    _Linked = false;
                }
            //}
        }

        private void Crouch()
        {
            _IsStanding = false;
            VR.Input.Keyboard.KeyDown(VirtualKeyCode.VK_Z);
            _NeedsMoveCamera = true;
        }

        private void StandUp()
        {
            _IsStanding = true;
            VR.Input.Keyboard.KeyUp(VirtualKeyCode.VK_Z);
            _NeedsMoveCamera = true;
        }

        private void UpdateCrouch()
        {
            if (_Settings.CrouchByHMDPos && _Linked)
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

        protected override void OnAwake()
        {
            base.OnAwake();

            _Settings = (VR.Context.Settings as KoikatuSettings);
            _KeySet = _Settings.KeySets[0];
        }

        protected override void OnStart()
        {
            base.OnStart();

        }

        protected override void OnDestroy()
        {
            // nothing to do.
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            UnlinkPlayer();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            LinkPlayer();
        }
        
        protected override void OnLevel(int level)
        {
            base.OnLevel(level);

            LinkPlayer();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            var device = this.Controller;

            UpdateCrouch();

            if (device.GetPressDown(ButtonMask.Trigger))
            {
                MovePlayerToCamera(true);
                VR.Input.Keyboard.KeyDown(VirtualKeyCode.SHIFT);
                VR.Input.Mouse.LeftButtonDown();
                _Walking = true;
            }

            if (device.GetPressUp(ButtonMask.Trigger))
            {
                VR.Input.Mouse.LeftButtonUp();
                VR.Input.Keyboard.KeyUp(VirtualKeyCode.SHIFT);
                _Walking = false;
            }


            if (device.GetPress(ButtonMask.Grip))
            {
                MovePlayerToCamera();
            }


            if (device.GetPressDown(ButtonMask.Touchpad))
            {
                Vector2 touchPosition = device.GetAxis();
                {
                    if (touchPosition.y > 0.8f) // up
                    {
                        InputKey(_KeySet.Up, KeyMode.PressDown);
                        _prevTouchDirection = 8;
                    }
                    else if (touchPosition.y < -0.8f) // down
                    {
                        InputKey(_KeySet.Down, KeyMode.PressDown);
                        _prevTouchDirection = 2;
                    }
                    else if (touchPosition.x > 0.8f) // right
                    {
                        InputKey(_KeySet.Right, KeyMode.PressDown);
                        _prevTouchDirection = 6;
                    }
                    else if (touchPosition.x < -0.8f)// left
                    {
                        InputKey(_KeySet.Left, KeyMode.PressDown);
                        _prevTouchDirection = 4;
                    }
                    else
                    {
                        InputKey(_KeySet.Center, KeyMode.PressDown);
                        _prevTouchDirection = 5;
                    }
                }
             }

            // 上げたときの位置によらず、押したボタンを離す
            if (device.GetPressUp(ButtonMask.Touchpad))
            {
                Vector2 touchPosition = device.GetAxis();
                {
                    if (_prevTouchDirection == 8) // up
                    {
                        InputKey(_KeySet.Up, KeyMode.PressUp);
                    }
                    else if (_prevTouchDirection == 2) // down
                    {
                        InputKey(_KeySet.Down, KeyMode.PressUp);
                    }
                    else if (_prevTouchDirection == 6) // right
                    {
                        InputKey(_KeySet.Right, KeyMode.PressUp);
                    }
                    else if (_prevTouchDirection == 4)// left
                    {
                        InputKey(_KeySet.Left, KeyMode.PressUp);
                    }
                    else if (_prevTouchDirection == 5)
                    {
                        InputKey(_KeySet.Center, KeyMode.PressUp);
                    }
                }
            }

            if (_NeedsMoveCamera || _Walking)
            {
                MoveCameraToPlayer();
                _NeedsMoveCamera = false;
            }
        }

        private void InputKey(string keyName, KeyMode mode)
        {
            if (mode == KeyMode.PressDown)
            {
                switch (keyName)
                {
                    case "LBUTTON":
                        VR.Input.Mouse.LeftButtonDown();
                        break;
                    case "RBUTTON":
                        VR.Input.Mouse.RightButtonDown();
                        break;
                    case "LROTATION":
                    case "RROTATION":
                    case "NEXT":
                        // ここでは何もせず、上げたときだけ処理する
                        break;
                    case "CROUCH":
                        Crouch();
                        break;
                    default:
                        VR.Input.Keyboard.KeyDown((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), keyName));
                        break;
                }
            }
            else
            {
                switch (keyName)
                {
                    case "LBUTTON":
                        VR.Input.Mouse.LeftButtonUp();
                        break;
                    case "RBUTTON":
                        VR.Input.Mouse.RightButtonUp();
                        break;
                    case "LROTATION":
                        Rotate(-45);
                        break;
                    case "RROTATION":
                        Rotate(45);
                        break;
                    case "NEXT":
                        ChangeKeySet();
                        break;
                    case "CROUCH":
                        StandUp();
                        break;
                    default:
                        VR.Input.Keyboard.KeyUp((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), keyName));
                        break;
                }
            }
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[] {
                HelpText.Create($"Link {_Linked}", FindAttachPosition("trigger"), new Vector3(0.1f, 0.04f, -0.05f)),
            });
        }
    }
}
