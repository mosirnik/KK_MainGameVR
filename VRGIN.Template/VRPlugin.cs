using IllusionPlugin;
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KoikatuVR
{

    /// <summary>
    /// This is an example for a VR plugin. At the same time, it also functions as a generic one.
    /// </summary>
    public class VRPlugin : IPlugin
    {

        /// <summary>
        /// Put the name of your plugin here.
        /// </summary>
        public string Name
        {
            get
            {
                return "KoikatuVR";
            }
        }

        public string Version
        {
            get
            {
                return "0.6";
            }
        }

        /// <summary>
        /// Determines when to boot the VR code. In most cases, it makes sense to do the check as described here.
        /// </summary>
        public void OnApplicationStart()
        {
            bool vrDeactivated = Environment.CommandLine.Contains("--novr");
            bool vrActivated = Environment.CommandLine.Contains("--vr");

            if (vrActivated || (!vrDeactivated && SteamVRDetector.IsRunning))
            {
				VRLoader.Create(true);
            }
			else
			{
				VRLoader.Create(false);
            }
        }





        #region Unused
        public void OnApplicationQuit() { }
        public void OnFixedUpdate() { }
        public void OnLevelWasInitialized(int level) { }
        public void OnLevelWasLoaded(int level) { }
        public void OnUpdate() { }
        #endregion
    }
}
