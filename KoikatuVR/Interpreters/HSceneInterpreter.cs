using UnityEngine;
using VRGIN.Core;
using HarmonyLib;
using System.Collections.Generic;

namespace KoikatuVR.Interpreters
{
    class HSceneInterpreter : SceneInterpreter
    {
        bool _active;
        HSceneProc _proc;
        Caress.VRMouth _vrMouth;

        public override void OnStart()
        {
        }

        public override void OnDisable()
        {
            Deactivate();
        }

        public override void OnUpdate()
        {
            if (_active && (!_proc || !_proc.enabled))
            {
                // The HProc scene is over, but there may be one more coming.
                Deactivate();
            }

            if (!_active &&
                Manager.Scene.GetRootComponent<HSceneProc>("HProc") is HSceneProc proc &&
                proc.enabled)
            {
                _vrMouth = VR.Camera.gameObject.AddComponent<Caress.VRMouth>();
                AddControllerComponent<Caress.CaressController>();
                _proc = proc;
                _active = true;
            }
        }

        private void Deactivate()
        {
            if (_active)
            {
                GameObject.Destroy(_vrMouth);
                DestroyControllerComponent<Caress.CaressController>();
                _proc = null;
                _active = false;
            }
        }

    }
}
