using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using static SteamVR_Controller;
using WindowsInput.Native;
using KoikatuVR.Interpreters;
using System.ComponentModel;

namespace KoikatuVR.Controls
{
    public class SchoolTool : Tool
    {
        private KoikatuInterpreter _Interpreter;
        private KoikatuSettings _Settings;
        private KeySet _KeySet;
        private int _KeySetIndex = 0;
        private bool _InHScene = false;
        private Controller.Lock _lock = VRGIN.Controls.Controller.Lock.Invalid;

        private Controller.TrackpadDirection? _touchDirection;
        private Controller.TrackpadDirection? _lastPressDirection;

        // When eneabled, exactly one of the below is non-null.
        private ButtonsSubtool _buttonsSubtool;
        private GrabAction _grab;

        private void ChangeKeySet()
        {
            List<KeySet> keySets = KeySets();

            _KeySetIndex = (_KeySetIndex + 1) % keySets.Count;
            _KeySet = keySets[_KeySetIndex];
            UpdateIcon();
        }

        private List<KeySet> KeySets()
        {
            return _InHScene ? _Settings.HKeySets : _Settings.KeySets;
        }

        private void ResetKeys()
        {
            SetScene(_InHScene);
        }

        private void SetScene(bool inHScene)
        {
            if (_buttonsSubtool != null)
            {
                _buttonsSubtool.Destroy();
                _buttonsSubtool = new ButtonsSubtool(_Interpreter, _Settings);
            }
            _InHScene = inHScene;
            var keySets = KeySets();
            _KeySetIndex = 0;
            _KeySet = keySets[0];
            UpdateIcon();
        }

        private void UpdateIcon()
        {
            Texture2D icon =
                _InHScene
                    ? _Settings.HKeySets.Count > 1
                        ? _KeySetIndex == 0
                            ? _hand1Texture
                            : _hand2Texture
                        : _handTexture
                    : _Settings.KeySets.Count > 1
                        ? _KeySetIndex == 0
                            ? _school1Texture
                            : _school2Texture
                        : _schoolTexture;
            Graphics.CopyTexture(icon, _image);
        }

        public override Texture2D Image => _image;
        private readonly Texture2D _image = new Texture2D(512, 512);
        private readonly Texture2D _schoolTexture = UnityHelper.LoadImage("icon_school.png");
        private readonly Texture2D _school1Texture = UnityHelper.LoadImage("icon_school_1.png");
        private readonly Texture2D _school2Texture = UnityHelper.LoadImage("icon_school_2.png");
        private readonly Texture2D _handTexture = UnityHelper.LoadImage("icon_hand.png");
        private readonly Texture2D _hand1Texture = UnityHelper.LoadImage("icon_hand_1.png");
        private readonly Texture2D _hand2Texture = UnityHelper.LoadImage("icon_hand_2.png");

        protected override void OnAwake()
        {
            base.OnAwake();
        }

        protected override void OnStart()
        {
            base.OnStart();

            _Settings = (VR.Context.Settings as KoikatuSettings);
            SetScene(inHScene: false);
            _Settings.AddListener("KeySets", (_, _1) => ResetKeys());
            _Settings.AddListener("HKeySets", (_, _1) => ResetKeys());
        }

        protected override void OnDestroy()
        {
            // nothing to do.
        }

        protected override void OnDisable()
        {
            _buttonsSubtool?.Destroy();
            _buttonsSubtool = null;
            _grab?.Destroy();
            _grab = null;
            if (_lock.IsValid)
            {
                _lock.Release();
            }
            _touchDirection = null;
            _lastPressDirection = null;
            base.OnDisable();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _Interpreter = VR.Interpreter as KoikatuInterpreter;
            _buttonsSubtool = new ButtonsSubtool(_Interpreter, _Settings);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            UpdateLock();

            var inHScene = _Interpreter.CurrentScene == KoikatuInterpreter.SceneType.HScene;
            if (inHScene != _InHScene)
            {
                SetScene(inHScene);
            }

            if (_grab != null)
            {
                if (_grab.HandleGrabbing() != GrabAction.Status.Continue)
                {
                    _grab.Destroy();
                    _grab = null;
                    _buttonsSubtool = new ButtonsSubtool(_Interpreter, _Settings);
                }
            }

            if (_buttonsSubtool != null)
            {
                HandleButtons();
            }
        }

        private void UpdateLock()
        {
            bool wantLock = _grab != null || _buttonsSubtool?.WantLock() == true;
            if (wantLock && !_lock.IsValid)
            {
                _lock = Owner.AcquireFocus(keepTool: true);
            }
            else if(!wantLock && _lock.IsValid)
            {
                _lock.Release();
            }
        }

        private void HandleButtons()
        {
            var device = this.Controller;

            if (device.GetPressDown(ButtonMask.Trigger))
            {
                InputDown(_KeySet.Trigger, ButtonMask.Trigger);
            }

            if (device.GetPressUp(ButtonMask.Trigger))
            {
                InputUp(_KeySet.Trigger);
            }

            if (device.GetPressDown(ButtonMask.Grip))
            {
                InputDown(_KeySet.Grip, ButtonMask.Grip);
            }

            if (device.GetPressUp(ButtonMask.Grip))
            {
                InputUp(_KeySet.Grip);
            }

            if (device.GetPressDown(ButtonMask.Touchpad))
            {
                var dir = Owner.GetTrackpadDirection();
                var fun = GetTrackpadFunction(dir);
                bool requiresPress = RequiresPress(fun);
                if (requiresPress)
                {
                    _lastPressDirection = dir;
                    InputDown(fun, ButtonMask.Touchpad);
                }
            }

            if (_buttonsSubtool == null)
            {
                return;
            }

            // 上げたときの位置によらず、押したボタンを離す
            if (device.GetPressUp(ButtonMask.Touchpad) && _lastPressDirection is Controller.TrackpadDirection dirP)
            {
                InputUp(GetTrackpadFunction(dirP));
                _lastPressDirection = null;
            }

            var newTouchDirection =
                device.GetTouch(ButtonMask.Touchpad) ? (Controller.TrackpadDirection?)Owner.GetTrackpadDirection() : null;

            if (_touchDirection != newTouchDirection)
            {
                if (_touchDirection is Controller.TrackpadDirection oldDir &&
                    GetTrackpadFunction(oldDir) is var oldFun &&
                    !RequiresPress(oldFun))
                {
                    InputUp(oldFun);

                }

                if (newTouchDirection is Controller.TrackpadDirection newDir &&
                    GetTrackpadFunction(newDir) is var newFun &&
                    !RequiresPress(newFun))
                {
                    InputDown(newFun, ButtonMask.Touchpad);
                }
                _touchDirection = newTouchDirection;
            }

            _buttonsSubtool.Update();
        }

        private AssignableFunction GetTrackpadFunction(Controller.TrackpadDirection dir)
        {
            switch (dir)
            {
                case VRGIN.Controls.Controller.TrackpadDirection.Up:
                    return _KeySet.Up;
                case VRGIN.Controls.Controller.TrackpadDirection.Down:
                    return _KeySet.Down;
                case VRGIN.Controls.Controller.TrackpadDirection.Left:
                    return _KeySet.Left;
                case VRGIN.Controls.Controller.TrackpadDirection.Right:
                    return _KeySet.Right;
                default:
                    return _KeySet.Center;
            }
        }

        /// <summary>
        /// When this function is assigned to trackpad, does it require a press
        /// or does a touch suffice?
        /// </summary>
        /// <param name="fun"></param>
        /// <returns></returns>
        static bool RequiresPress(AssignableFunction fun)
        {
            switch (fun)
            {
                case AssignableFunction.SCROLLDOWN:
                case AssignableFunction.SCROLLUP:
                case AssignableFunction.LROTATION:
                case AssignableFunction.RROTATION:
                    return false;
                default:
                    return true;
            }
        }

        private void InputDown(AssignableFunction fun, ulong buttonMask)
        {
            switch (fun)
            {
                case AssignableFunction.NEXT:
                    break;
                case AssignableFunction.GRAB:
                    _buttonsSubtool.Destroy();
                    _buttonsSubtool = null;
                    _grab = new GrabAction(Owner, Controller, buttonMask);
                    break;
                default:
                    _buttonsSubtool.ButtonDown(fun);
                    break;
            }
        }

        private void InputUp(AssignableFunction fun)
        {
            switch (fun)
            {
                case AssignableFunction.NEXT:
                    ChangeKeySet();
                    break;
                case AssignableFunction.GRAB:
                    break;
                default:
                    _buttonsSubtool.ButtonUp(fun);
                    break;
            }
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new[] {
                ToolUtil.HelpTrigger(Owner, DescriptionFor(_KeySet.Trigger)),
                ToolUtil.HelpGrip(Owner, DescriptionFor(_KeySet.Grip)),
                ToolUtil.HelpTrackpadCenter(Owner, DescriptionFor(_KeySet.Center)),
                ToolUtil.HelpTrackpadLeft(Owner, DescriptionFor(_KeySet.Left)),
                ToolUtil.HelpTrackpadRight(Owner, DescriptionFor(_KeySet.Right)),
                ToolUtil.HelpTrackpadUp(Owner, DescriptionFor(_KeySet.Up)),
                ToolUtil.HelpTrackpadDown(Owner, DescriptionFor(_KeySet.Down)),
            }.Where(x => x != null));
        }

        private static string DescriptionFor(AssignableFunction fun)
        {
            var member = typeof(AssignableFunction).GetMember(fun.ToString()).FirstOrDefault();
            var descr = member?.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault()?.Description;
            return descr ?? fun.ToString();
        }
    }
}
