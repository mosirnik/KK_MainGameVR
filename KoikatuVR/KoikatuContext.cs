using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using VRGIN.Controls.Speech;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Visuals;

namespace KoikatuVR
{
    public class KoikatuContext : IVRManagerContext
    {
        public KoikatuContext(KoikatuSettings settings)
        {
            // We'll keep those always the same
            Materials = new DefaultMaterialPalette();
            _settings = settings;
        }

        private KoikatuSettings _settings;

        public IMaterialPalette Materials { get; private set; }

        public VRSettings Settings => _settings;

        public Type VoiceCommandType { get { return typeof(VoiceCommand); } }

        public bool ConfineMouse { get { return _settings.UseLegacyInputSimulator;  } }

        public bool EnforceDefaultGUIMaterials { get { return false;  } }

        public bool GUIAlternativeSortingMode { get { return false; } }

        public float GuiFarClipPlane { get { return 1000f;  } }

        public string GuiLayer { get { return "Default";  } }

        public float GuiNearClipPlane { get { return -1000f; } }

        public int IgnoreMask { get { return 0; } }

        public string InvisibleLayer { get { return "Ignore Raycast"; } }

        public Color PrimaryColor { get { return Color.cyan; } }

        public bool SimulateCursor { get { return true; } }

        public string UILayer { get { return "UI"; } }

        public int UILayerMask { get { return LayerMask.GetMask(UILayer); } }

        public float UnitToMeter { get { return 1f; } }

        public float NearClipPlane => _settings.NearClipPlane;

        public GUIType PreferredGUI { get { return GUIType.uGUI; } }
    }
}
