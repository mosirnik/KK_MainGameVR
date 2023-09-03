using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsInput;
using WindowsInput.Native;
using VRGIN.Core;
using VRGIN.Native;
using HarmonyLib;
using UnityEngine;
using System.Threading;
using System.Runtime.InteropServices;

namespace KoikatuVR
{
    /// <summary>
    /// A version of InputSimulator that tries to avoid the issue of
    /// requiring focus & accidentally clicking outside the application window.
    /// </summary>
    public class RobustInputSimulator : IInputSimulator
    {
        public IKeyboardSimulator Keyboard { get; private set; }
        public IMouseSimulator Mouse { get; private set; }
        public IInputDeviceStateAdaptor InputDeviceState { get; private set; }

        public RobustInputSimulator()
        {
            Keyboard = new RobustKeyboardSimulator(this);
            Mouse = new RobustMouseSimulator(this);
            InputDeviceState = new WindowsInputDeviceStateAdaptor();
        }

        /// <summary>
        /// Make Unity think we have focus, if it doesn't already. This is necessary to prevent
        /// messages from being ignored.
        /// </summary>
        internal static void FakeFocus()
        {
            if (!Application.isFocused)
            {
                NativeMethods.PostMessage(
                    WindowManager.Handle,
                    0x6, // WM_ACTIVATE
                    new IntPtr(0x2), // Activated by a mouse click
                    IntPtr.Zero); // Handle to the previously active window
            }
        }

        internal class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool GetKeyboardState([Out] byte[] lpKeyState);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool SetKeyboardState([In] byte[] lpKeyState);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        }
    }

    /// <summary>
    /// This class is similar to MouseSimulator, but avoids generating
    /// button down messages using SendInput. Instead it uses PostMessage
    /// to send a fake WndProc message to the game window. This means
    /// the plugin continues to work fine if the game window loses focus.
    /// It also avoids the risk of accidentally clicking a random window.
    /// The overall strategy is:
    ///
    /// * Down and Up events: Use a WndProc message.
    /// * Wheel scroll: Use WM_MOUSEWHEEL, but this only affects IMGui for
    ///   some reason. So we additionally patch UnityEngine.Input
    ///   to inject synthetic wheel scrolls for UI and game code.
    /// * Cursor movement: Use SendInput to make sure that the actual cursor
    ///   moves. This is necessary because game code often queries for the
    ///   cursor position. However this is insufficient for IMGUI to recognize
    ///   a drag. For this reason, an additional WM_MOUSEMOVE message is
    ///   generated when dragging.
    /// </summary>
    public class RobustMouseSimulator : IMouseSimulator
    {
        private readonly MouseSimulator _systemSimulator;
        private Buttons _pressedMask = 0;
        private WindowsInterop.POINT? _dragCurrent;

        // Bitmask that can be passed to WM_MOUSEMOVE
        enum Buttons
        {
            Left = 0x1,
            Right = 0x2,
            Middle = 0x10,
        }

        public IKeyboardSimulator Keyboard => _inputSimulator.Keyboard;

        public RobustMouseSimulator(RobustInputSimulator inputSimulator)
        {
            _inputSimulator = inputSimulator;
            _systemSimulator = new MouseSimulator(inputSimulator);
        }

        public IMouseSimulator MoveMouseBy(int pixelDeltaX, int pixelDeltaY)
        {
            RobustInputSimulator.FakeFocus();
            _systemSimulator.MoveMouseBy(pixelDeltaX, pixelDeltaY);
            if (_pressedMask != 0)
            {
                var startingPoint = _dragCurrent ?? MouseOperations.GetCursorPosition();
                DragToScreenCoordinate(startingPoint.X + pixelDeltaX, startingPoint.Y + pixelDeltaY);
            }
            return this;
        }

        public IMouseSimulator MoveMouseTo(double absoluteX, double absoluteY)
        {
            RobustInputSimulator.FakeFocus();
            _systemSimulator.MoveMouseTo(absoluteX, absoluteY);
            if (_pressedMask != 0)
            {
                int width = WindowsInterop.GetSystemMetrics(WindowsInterop.SystemMetric.SM_CXSCREEN);
                int height = WindowsInterop.GetSystemMetrics(WindowsInterop.SystemMetric.SM_CYSCREEN);
                DragToScreenCoordinate(
                    (int)Math.Round(absoluteX / 65535 * width),
                    (int)Math.Round(absoluteY / 65535 * height));
            }
            return this;
        }

        public IMouseSimulator MoveMouseToPositionOnVirtualDesktop(double absoluteX, double absoluteY)
        {
            RobustInputSimulator.FakeFocus();
            _systemSimulator.MoveMouseToPositionOnVirtualDesktop(absoluteX, absoluteY);
            if (_pressedMask != 0)
            {
                var vRect = WindowManager.GetVirtualScreenRect();
                DragToScreenCoordinate(
                    (int)Math.Round(absoluteX / 65535 * (vRect.Right - vRect.Left)) + vRect.Left,
                    (int)Math.Round(absoluteY / 65535 * (vRect.Bottom - vRect.Top)) + vRect.Top);

            }
            return this;
        }

        public IMouseSimulator LeftButtonDown()
        {
            RobustInputSimulator.FakeFocus();
            _pressedMask |= Buttons.Left;
            return PostButtonMessage(0x201); // WM_LBUTTONDOWN
        }

        public IMouseSimulator LeftButtonUp()
        {
            ClearPressed(Buttons.Left);
            return PostButtonMessage(0x202); // WM_LBUTTONUP
        }

        public IMouseSimulator LeftButtonClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator LeftButtonDoubleClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator MiddleButtonDown()
        {
            RobustInputSimulator.FakeFocus();
            _pressedMask |= Buttons.Middle;
            return PostButtonMessage(0x207); // WM_MBUTTONDOWN
        }

        public IMouseSimulator MiddleButtonUp()
        {
            ClearPressed(Buttons.Middle);
            return PostButtonMessage(0x208); // WM_MBUTTONUP
        }

        public IMouseSimulator MiddleButtonClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator MiddleButtonDoubleClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator RightButtonDown()
        {
            RobustInputSimulator.FakeFocus();
            _pressedMask |= Buttons.Right;
            return PostButtonMessage(0x204); // WM_RBUTTONDOWN
        }

        public IMouseSimulator RightButtonUp()
        {
            ClearPressed(Buttons.Right);
            return PostButtonMessage(0x205); // WM_RBUTTONUP
        }

        public IMouseSimulator RightButtonClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator RightButtonDoubleClick()
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator XButtonDown(int buttonId)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator XButtonUp(int buttonId)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator XButtonClick(int buttonId)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator XButtonDoubleClick(int buttonId)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator VerticalScroll(int scrollAmountInClicks)
        {
            RobustInputSimulator.FakeFocus();
            InputPatches.RequestScroll(scrollAmountInClicks);
            return PostScrollMessage(0x20a, scrollAmountInClicks * 120); // WM_MOUSEWHEEL
        }

        public IMouseSimulator VerticalScrollAbsolute(int scrollAmount)
        {
            RobustInputSimulator.FakeFocus();
            InputPatches.RequestScroll(scrollAmount);
            return PostScrollMessage(0x20a, scrollAmount * 120); // WM_MOUSEWHEEL
        }

        public IMouseSimulator HorizontalScroll(int scrollAmountInClicks)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator HorizontalScrollAbsolute(int scrollAmount)
        {
            throw new NotImplementedException();
        }

        public IMouseSimulator Sleep(int millsecondsTimeout)
        {
            _systemSimulator.Sleep(millsecondsTimeout);
            return this;
        }

        public IMouseSimulator Sleep(TimeSpan timeout)
        {
            _systemSimulator.Sleep(timeout);
            return this;
        }

        private IMouseSimulator PostButtonMessage(uint msg)
        {
            // wParam represents the modifier key state,
            // but Unity doesn't seem to care.
            return PostMouseMessage(msg, IntPtr.Zero);
        }

        private IMouseSimulator PostScrollMessage(uint msg, int amount)
        {
            return PostMouseMessage(msg, PackWords(amount, 0));
        }

        private IMouseSimulator PostMouseMessage(uint msg, IntPtr wParam)
        {
            WindowsInterop.GetCursorPos(out var mousePosition);
            var clientRect = WindowManager.GetClientRect();
            var x = mousePosition.X - clientRect.Left;
            var y = mousePosition.Y - clientRect.Top;
            RobustInputSimulator.NativeMethods.PostMessage(
                WindowManager.Handle,
                msg,
                wParam,
                PackWords(y, x));
            return this;
        }

        private void DragToScreenCoordinate(int x, int y)
        {
            var clientRect = WindowManager.GetClientRect();
            RobustInputSimulator.NativeMethods.PostMessage(
                WindowManager.Handle,
                0x200, // WM_MOUSEMOVE
                new IntPtr((int)_pressedMask),
                PackWords(y - clientRect.Top, x - clientRect.Left));
            _dragCurrent = new WindowsInterop.POINT(x, y);
        }

        private void ClearPressed(Buttons button)
        {
            _pressedMask &= ~button;
            if (_pressedMask == 0)
            {
                _dragCurrent = null;
            }
        }
        private static IntPtr PackWords(int hi, int lo)
        {
            return new IntPtr((hi << 16) | (lo & 0xffff));
        }

        private readonly RobustInputSimulator _inputSimulator;
    }

    /// <summary>
    /// A keyboard simulator that uses PostMessage and SetKeyboardState to
    /// simulate keypresses.
    /// </summary>
    public class RobustKeyboardSimulator : IKeyboardSimulator
    {
        public IMouseSimulator Mouse => _inputSimulator.Mouse;

        public RobustKeyboardSimulator(IInputSimulator inputSimulator)
        {
            _inputSimulator = inputSimulator;
        }

        public IKeyboardSimulator KeyDown(VirtualKeyCode code)
        {
            RobustInputSimulator.FakeFocus();
            RobustInputSimulator.NativeMethods.PostMessage(
                WindowManager.Handle,
                0x100, // WM_KEYDOWN
                (IntPtr)code,
                IntPtr.Zero);
            ModifyInputState(code, true);
            return this;
        }
        public IKeyboardSimulator KeyUp(VirtualKeyCode code)
        {
            RobustInputSimulator.NativeMethods.PostMessage(
                WindowManager.Handle,
                0x101, // WM_KEYUP
                (IntPtr)code,
                new IntPtr(0xc000_0001u)); // repeat=1, context=0, previous=1, transition=1
            ModifyInputState(code, false);
            return this;
        }
        public IKeyboardSimulator KeyPress(VirtualKeyCode code)
        {
            throw new NotImplementedException();
        }
        public IKeyboardSimulator KeyPress(params VirtualKeyCode[] code)
        {
            throw new NotImplementedException();
        }
        public IKeyboardSimulator ModifiedKeyStroke(IEnumerable<VirtualKeyCode> mods, IEnumerable<VirtualKeyCode> codes)
        {
            throw new NotImplementedException();
        }
        public IKeyboardSimulator ModifiedKeyStroke(IEnumerable<VirtualKeyCode> mods, VirtualKeyCode code)
        {
            throw new NotImplementedException();
        }
        public IKeyboardSimulator ModifiedKeyStroke(VirtualKeyCode mod, IEnumerable<VirtualKeyCode> codes)
        {
            throw new NotImplementedException();
        }
        public IKeyboardSimulator ModifiedKeyStroke(VirtualKeyCode mod, VirtualKeyCode code)
        {
            throw new NotImplementedException();
        }
        public IKeyboardSimulator TextEntry(string str)
        {
            throw new NotImplementedException();
        }
        public IKeyboardSimulator TextEntry(char c)
        {
            throw new NotImplementedException();
        }
        public IKeyboardSimulator Sleep(int millisecondsTimeout)
        {
            Thread.Sleep(millisecondsTimeout);
            return this;
        }
        public IKeyboardSimulator Sleep(TimeSpan timeout)
        {
            Thread.Sleep(timeout);
            return this;
        }

        private void ModifyInputState(VirtualKeyCode code, bool value)
        {
            var state = new byte[256];
            if (!RobustInputSimulator.NativeMethods.GetKeyboardState(state))
            {
                VRLog.Error($"GetKeyboardState failed");
                return;
            }
            state[(int)code] = (value ? (byte)0x80 : (byte)0);
            if (!RobustInputSimulator.NativeMethods.SetKeyboardState(state))
            {
                VRLog.Error($"SetKeyboardState failed");
                return;
            }
        }

        private readonly IInputSimulator _inputSimulator;
    }

    [HarmonyPatch(typeof(Input))]
    class InputPatches
    {
        [HarmonyPatch(nameof(Input.mouseScrollDelta), MethodType.Getter)]
        [HarmonyPostfix]
        private static void PostGetMouseScrollDelta(ref Vector2 __result)
        {
            UpdateForFrame();
            __result.y += _scrollCurrent;
        }

        [HarmonyPatch(nameof(Input.GetAxis))]
        [HarmonyPostfix]
        private static void PostGetAxis(string axisName, ref float __result)
        {
            if (axisName == "Mouse ScrollWheel")
            {
                UpdateForFrame();
                __result += _scrollCurrent;
            }
        }

        private static void UpdateForFrame()
        {
            if (Time.frameCount != _lastUpdate)
            {
                _lastUpdate = Time.frameCount;
                _scrollCurrent = _scrollRequest;
                _scrollRequest = 0;
            }
        }

        internal static void RequestScroll(int amount)
        {
            _scrollRequest += amount;
        }

        private static int _lastUpdate = -1;
        private static int _scrollRequest = 0;
        private static int _scrollCurrent = 0;
    }
}
