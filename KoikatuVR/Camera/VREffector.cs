using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Core;
using UnityStandardAssets.ImageEffects;
using UnityEngine;

namespace KoikatuVR.Camera
{
    /// <summary>
    /// A component to be attached to the VR camera. Ensures that it has the
    /// same set of camera effects enabled as the game camera.
    /// </summary>
    class VREffector : ProtectedBehaviour
    {
        protected override void OnUpdate()
        {
            base.OnUpdate();

            var blueprint = VR.Camera.Blueprint;
            if (blueprint && _source != blueprint)
            {
                _source = blueprint;
                HandleNewGameCamera(blueprint);
            }
        }

        protected override void OnLateUpdate()
        {
            base.OnLateUpdate();

            _fog.UpdateEnabled();
            _amplifyColor.UpdateEnabled();
            if (_amplifyColor.mirror && _amplifyColor.source)
            {
                // NightDarkener tweaks exposure.
                _amplifyColor.mirror.Exposure = _amplifyColor.source.Exposure;
            }
            _amplifyOcclusion.UpdateEnabled();
            _bloom.UpdateEnabled();
            _sunShafts.UpdateEnabled();
            if (_sunShafts.mirror && _sunShafts.source && _sunShafts.mirror.enabled)
            {
                _sunShafts.mirror.sunColor = _sunShafts.source.sunColor;
                _sunShafts.mirror.sunTransform = _sunShafts.source.sunTransform;
            }
            _vignette.UpdateEnabled();
            _blur.UpdateEnabled();
            if (_blur.mirror && _blur.source && _blur.mirror.enabled)
            {
                _blur.mirror.iterations = _blur.source.iterations;
            }
            _sepia.UpdateEnabled();
            _flareLayer.UpdateEnabled();
        }

        Mirrored<GlobalFog> _fog;
        Mirrored<AmplifyColorEffect> _amplifyColor;
        Mirrored<AmplifyOcclusionEffect> _amplifyOcclusion;
        Mirrored<BloomAndFlares> _bloom;
        Mirrored<SunShafts> _sunShafts;
        Mirrored<VignetteAndChromaticAberration> _vignette;
        // Depth-of-field effect doesn't really make sense in VR, where the
        // player is free to look at whatever they want.
        //Mirrored<DepthOfField> _dof;
        Mirrored<Blur> _blur;
        // Crossfade with a still image won't work well in VR.
        //Mirrored<CrossFade> _crossFade;
        Mirrored<SepiaTone> _sepia;
        Mirrored<FlareLayer> _flareLayer;

        UnityEngine.Camera _source;

        private void HandleNewGameCamera(UnityEngine.Camera gameCamera)
        {
            void Copy<T>(ref Mirrored<T> m) where
                T: Behaviour
            {
                if (m.mirror != null)
                {
                    VRLog.Info("Removing effect: {0}", m.mirror.GetType());
                    Destroy(m.mirror);
                }
                m.source = gameCamera.GetComponent<T>();
                if (m.source == null)
                {
                    m.mirror = null;
                }
                else
                {
                    VRLog.Info("Adding effect: {0}", m.source.GetType());
                    m.mirror = VRGIN.Helpers.UnityHelper.CopyComponent(m.source, gameObject);
                }
            }

            Copy(ref _fog);
            Copy(ref _amplifyColor);
            Copy(ref _amplifyOcclusion);
            Copy(ref _bloom);
            Copy(ref _sunShafts);
            Copy(ref _vignette);
            Copy(ref _blur);
            Copy(ref _sepia);
            Copy(ref _flareLayer);
        }

        struct Mirrored<T>
            where T: Behaviour
        {
            public T mirror;
            public T source;

            public void UpdateEnabled()
            {
                if (mirror != null)
                {
                    mirror.enabled = (source && source.enabled);
                }
            }
        }
    }
}
