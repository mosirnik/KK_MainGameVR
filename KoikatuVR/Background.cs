using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRGIN.Core;
using HarmonyLib;

namespace KoikatuVR
{
    /// <summary>
    /// A singleton object that is responsible for displaying the
    /// background image as a background.
    /// </summary>
    public class Background
    {
        public static Background Instance { get; private set; } = new Background();
        /// <summary>
        /// Where in the background image the eye level (or the vanishing point) is.
        /// 0 is the bottom of the image, 1 is the top.
        /// </summary>
        public float EyeLevel { get; set; } = 0.33f;

        private Vector3 _cameraBasePosition;
        private Quaternion _cameraBaseRotation;
        private Canvas _bgCanvas;
        private const float Height = 50f;

        Background()
        {
            Camera.VRMover.Instance.OnMove += OnCameraMove;
        }

        public void OnCameraMove()
        {
            _cameraBasePosition = VR.Camera.transform.position;
            _cameraBaseRotation = Quaternion.Euler(0, VR.Camera.transform.eulerAngles.y, 0);
            UpdateCanvasPlacement();
        }

        public void TakeCanvas(Canvas canvas)
        {
            VRLog.Info($"Taking canvas: {canvas.name}");
            if (_bgCanvas != null)
            {
                VRLog.Warn("taking a second canvas?");
            }
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            canvas.gameObject.layer = 0;
            canvas.GetComponent<RectTransform>().sizeDelta = canvas.pixelRect.size;
            canvas.transform.localScale = Vector3.one * (Height / canvas.pixelRect.height);
            var imageTrans = canvas.GetComponentInChildren<Image>().transform;
            imageTrans.localPosition = new Vector3(imageTrans.localPosition.x, imageTrans.localPosition.y, 0);
            _bgCanvas = canvas;
            UpdateCanvasPlacement();
        }

        private void UpdateCanvasPlacement()
        {
            if (_bgCanvas == null)
            {
                return;
            }

            float level = Mathf.Clamp(EyeLevel, 0.25f, 0.75f);
            float y = (0.5f - level) * Height;
            _bgCanvas.transform.SetPositionAndRotation(
                _cameraBasePosition + _cameraBaseRotation * new Vector3(0, y, 0.3f * Height),
                _cameraBaseRotation);
        }
    }

    [HarmonyPatch(typeof(Illusion.Component.UI.BackGroundParam))]
    class BackGroundParamPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Illusion.Component.UI.BackGroundParam.Load))]
        private static void PostLoad(string assetName)
        {
            Background.Instance.EyeLevel = EyeLevelFor(assetName);
        }

        private static float EyeLevelFor(string assetName)
        {
            switch (assetName)
            {
                case "bg_op":
                    return 0.33f;
                case "bg_04":
                case "bg_05_e":
                    return 0.70f;
                case "bg_06":
                    return 0.50f;
                case "bg_08":
                case "bg_09_e":
                    return 0.60f;
                case "bg_10":
                case "bg_11_e":
                    return 0.50f;
                case "bg_12":
                case "bg_13_e":
                    return 0.50f;
                case "bg_14":
                case "bg_15_e":
                    return 0.37f;
                case "bg_16":
                case "bg_16_e":
                case "bg_16_n":
                    return 0.42f;
                case "bg_ferris_wheel_ni":
                    return 0.64f;
                case "bg_night_view":
                case "bg_night_view_late":
                    return 0.56f;
                case "bg_park_no":
                case "bg_park_ev":
                case "bg_park_ni":
                    return 0.50f;
                case "bg_station_no":
                case "bg_station_ev":
                case "bg_station_ni":
                    return 0.28f;
                case "bg_theme_park_no":
                case "bg_theme_park_ev":
                case "bg_theme_park_ni":
                    return 0.40f;
                case "cg_ti_00":
                    return 0.50f;
                case "ev_chapel_door_no":
                case "ev_chapel_door_open_no":
                    return 0.60f;
                case "ev_chapel_no":
                case "ev_chapel_ev":
                    return 0.36f;
                case "ev_ferris_wheel_ni":
                    return 0.58f;
                case "ev_night_view":
                    return 0.48f;
                default:
                    VRLog.Warn($"Unknown BG asset: {assetName}");
                    return 0.40f;
            }
        }
    }
}
