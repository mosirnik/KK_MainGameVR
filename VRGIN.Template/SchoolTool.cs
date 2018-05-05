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
        private KeySet keySet = (VR.Context.Settings as KoikatuSettings).KeySets[0];
        // 手抜きのためNumpad方式で方向を保存
        private int _prevTouchDirection = -1;

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
            cam.position = playerHead.position - (headCam.position - cam.position) + cf * 0.15f; // 首が見えるとうざいのでほんの少し前目
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
            MoveCameraToPlayer();
        }

        // プレイヤーと一体化する
        private void LinkPlayer()
        {
            var pl = GameObject.Find("ActionScene/Player/chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root");

            if (pl != null)
            {
                GameObject.Find("ActionScene/CameraSystem").SetActive(false);
                pl.SetActive(false);
                MoveCameraToPlayer();
            }
        }

        private void UnlinkPlayer()
        {
            var pl = GameObject.Find("ActionScene/Player/chaM_001/BodyTop/p_cf_body_bone_low/cf_j_root");

            if (pl != null)
            {
                GameObject.Find("ActionScene/CameraSystem").SetActive(true);
                pl.SetActive(true);
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();
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

            if (device.GetPressDown(ButtonMask.Trigger))
            {
                MovePlayerToCamera(true);
                VR.Input.Keyboard.KeyDown(VirtualKeyCode.SHIFT);
                VR.Input.Mouse.LeftButtonDown();
            }

            if (device.GetPressUp(ButtonMask.Trigger))
            {
                VR.Input.Mouse.LeftButtonUp();
                VR.Input.Keyboard.KeyUp(VirtualKeyCode.SHIFT);
            }

            if (device.GetPress(ButtonMask.Trigger))
            {
                MoveCameraToPlayer();
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
                        InputKey(keySet.Up, KeyMode.PressDown);
                        _prevTouchDirection = 8;
                    }
                    else if (touchPosition.y < -0.8f) // down
                    {
                        InputKey(keySet.Down, KeyMode.PressDown);
                        _prevTouchDirection = 2;
                    }
                    else if (touchPosition.x > 0.8f) // right
                    {
                        InputKey(keySet.Right, KeyMode.PressDown);
                        _prevTouchDirection = 6;
                    }
                    else if (touchPosition.x < -0.8f)// left
                    {
                        InputKey(keySet.Left, KeyMode.PressDown);
                        _prevTouchDirection = 4;
                    }
                    else
                    {
                        InputKey(keySet.Center, KeyMode.PressDown);
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
                        InputKey(keySet.Up, KeyMode.PressUp);
                    }
                    else if (_prevTouchDirection == 2) // down
                    {
                        InputKey(keySet.Down, KeyMode.PressUp);
                    }
                    else if (_prevTouchDirection == 6) // right
                    {
                        InputKey(keySet.Right, KeyMode.PressUp);
                    }
                    else if (_prevTouchDirection == 4)// left
                    {
                        InputKey(keySet.Left, KeyMode.PressUp);
                    }
                    else if (_prevTouchDirection == 5)
                    {
                        InputKey(keySet.Center, KeyMode.PressUp);
                    }
                }
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
                        // ここでは何もせず、上げたときだけ処理する
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
                    default:
                        VR.Input.Keyboard.KeyUp((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), keyName));
                        break;
                }
            }
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[] {
                HelpText.Create("Tap to click", FindAttachPosition("trackpad"), new Vector3(0, 0.02f, 0.05f)),
                HelpText.Create("Slide to move cursor", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0), new Vector3(0.015f, 0, 0)),
                HelpText.Create("Attach/Remove menu", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0.0f, -0.05f))

            });
        }
    }
}
