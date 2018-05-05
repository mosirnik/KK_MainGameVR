using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Helpers;
using static VRGIN.Visuals.GUIMonitor;

namespace KoikatuVR
{
    /// <summary>
    /// Class that holds settings for VR. Saved as an XML file.
    /// 
    /// In order to create your own settings file, extend this class and add your own properties. Make sure to call <see cref="TriggerPropertyChanged(string)"/> if you want to use
    /// the events.
    /// IMPORTANT: When extending, add an XmlRoot annotation to the class like so:
    /// <code>[XmlRoot("Settings")]</code>
    /// </summary>
    [XmlRoot("Settings")]
    public class KoikatuSettings : VRSettings
    {
        // XMLSerializerは配列にデフォルト値をつけると、指定値とデフォルト値の両方を含む配列にしてしまうので
        public static KoikatuSettings Load(string path)
        {
            KoikatuSettings settings = VRSettings.Load<KoikatuSettings>(path);
            if (settings.KeySets.Count == 0)
            {
                settings.KeySets = new List<KeySet> { new KeySet() };
            }

            return settings;
        }

        // 配列だが、現在は初めの値しか使っていない
        [XmlElement(Type = typeof(List<KeySet>))]
        public List<KeySet> KeySets { get { return _KeySets; } set { _KeySets = value; } }
        private List<KeySet> _KeySets = null;
    }

    [XmlRoot("KeySet")]
    public class KeySet
    {
        public KeySet()
        {
            Up = "F3";
            Down = "F4";
            Right = "F2";
            Left = "F8";
            Center = "RBUTTON";
        }

        public KeySet(string Up, string Down, string Right, string Left, string Center)
        {
            this.Up = Up;
            this.Down = Down;
            this.Right = Right;
            this.Left = Left;
            this.Center = Center;
        }

        [System.Xml.Serialization.XmlElement("Up")]
        public String Up { get; set; }

        [System.Xml.Serialization.XmlElement("Down")]
        public String Down { get; set; }

        [System.Xml.Serialization.XmlElement("Right")]
        public String Right { get; set; }

        [System.Xml.Serialization.XmlElement("Left")]
        public String Left { get; set; }

        [System.Xml.Serialization.XmlElement("Center")]
        public String Center { get; set; }
    }
}
