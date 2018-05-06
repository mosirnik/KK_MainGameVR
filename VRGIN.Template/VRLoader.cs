using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Core;
using VRGIN.Helpers;

namespace KoikatuVR
{
	class VRLoader : ProtectedBehaviour
	{
		private static string DeviceOpenVR = "OpenVR";
		private static string DeviceNone = "None";

		private static bool _isVREnable = false;
		private static VRLoader _Instance;
		public static VRLoader Instance
		{
			get
			{
				if (_Instance == null)
				{
					throw new InvalidOperationException("VR Loader has not been created yet!");
				}
				return _Instance;
			}
		}

		public static VRLoader Create(bool isEnable)
		{
			_isVREnable = isEnable;
			_Instance = new GameObject("VRLoader").AddComponent<VRLoader>();

			return _Instance;
		}

		protected override void OnAwake()
		{
			if (_isVREnable)
			{
				StartCoroutine(LoadDevice(DeviceOpenVR));
			}
			else
			{
				StartCoroutine(LoadDevice(DeviceNone));
			}
		}

		#region Helper code

		private IVRManagerContext CreateContext(string path)
		{
			var serializer = new XmlSerializer(typeof(ConfigurableContext));

			if (File.Exists(path))
			{
				// Attempt to load XML
				using (var file = File.OpenRead(path))
				{
					try
					{
						return serializer.Deserialize(file) as ConfigurableContext;
					}
					catch (Exception e)
					{
						VRLog.Error("Failed to deserialize {0} -- using default", path);
					}
				}
			}

			// Create and save file
			var context = new ConfigurableContext();
			try
			{
				using (var file = new StreamWriter(path))
				{
					file.BaseStream.SetLength(0);
					serializer.Serialize(file, context);
				}
			}
			catch (Exception e)
			{
				VRLog.Error("Failed to write {0}", path);
			}

			return context;
		}
		#endregion

		/// <summary>
		/// VRデバイスのロード。
		/// </summary>
		IEnumerator LoadDevice(string newDevice)
		{
			bool vrMode = newDevice != DeviceNone;

			// 指定されたデバイスの読み込み.
			UnityEngine.VR.VRSettings.LoadDeviceByName(newDevice);
			// 次のフレームまで待つ.
			yield return null;
			// VRモードを有効にする.
			UnityEngine.VR.VRSettings.enabled = vrMode;
			// 次のフレームまで待つ.
			yield return null;

			// デバイスの読み込みが完了するまで待つ.
			while (UnityEngine.VR.VRSettings.loadedDeviceName != newDevice || UnityEngine.VR.VRSettings.enabled != vrMode)
			{
				yield return null;
			}


			if (vrMode)
			{
				// Boot VRManager!
				// Note: Use your own implementation of GameInterpreter to gain access to a few useful operatoins
				// (e.g. characters, camera judging, colliders, etc.)
				VRManager.Create<KoikatuInterpreter>(CreateContext("VRContext.xml"));
				VR.Manager.SetMode<GenericStandingMode>();
			}
		}
	}
}
