using System;
using System.Collections.Generic;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using static SteamVR_Controller;
using WindowsInput.Native;

namespace KoikatuVR
{
    public class SchoolTool : Tool
    {
        private KoikatuInterpreter _Interpreter;
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

        protected override void OnAwake()
        {
            base.OnAwake();

            _Interpreter = VR.Interpreter as KoikatuInterpreter;
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
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnLevel(int level)
        {
            base.OnLevel(level);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            var device = this.Controller;

            if (device.GetPressDown(ButtonMask.Trigger))
            {
                _Interpreter.StartWalking();
            }

            if (device.GetPressUp(ButtonMask.Trigger))
            {
                _Interpreter.StopWalking();
            }

            if (device.GetPress(ButtonMask.Grip))
            {
                _Interpreter.MovePlayerToCamera();
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
                        _Interpreter.Crouch();
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
                        _Interpreter.RotatePlayer(-45);
                        break;
                    case "RROTATION":
                        _Interpreter.RotatePlayer(45);
                        break;
                    case "NEXT":
                        ChangeKeySet();
                        break;
                    case "CROUCH":
                        _Interpreter.StandUp();
                        break;
                    default:
                        VR.Input.Keyboard.KeyUp((VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), keyName));
                        break;
                }
            }
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>();
        }
    }
}
