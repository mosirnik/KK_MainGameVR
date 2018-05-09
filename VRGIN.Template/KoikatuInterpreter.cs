using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace KoikatuVR
{
    class KoikatuInterpreter : GameInterpreter
    {
        private GameObject _CameraSystem;
        protected override void OnUpdate()
        {
            base.OnUpdate();

            HoldCamera();
        }

        private void HoldCamera()
        {
            _CameraSystem = GameObject.Find("ActionScene/CameraSystem");

            if (_CameraSystem != null)
            {
                _CameraSystem.SetActive(false);
            }
        }

        private void ReleaseCamera()
        {
            if (_CameraSystem != null)
            {
                _CameraSystem.SetActive(true);
            }
        }
    }
}
