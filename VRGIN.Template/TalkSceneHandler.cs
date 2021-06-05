﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Core;
using VRGIN.Controls;
using UnityEngine;
using HarmonyLib;

namespace KoikatuVR
{
    /// <summary>
    /// A handler component to be attached to a controller, providing touch/look
    /// functionalities in talk scenes.
    /// 
    /// This component is meant to remain disabled outside talk scenes.
    /// </summary>
    class TalkSceneHandler : ProtectedBehaviour
    {
        private Controller _controller;
        private TalkScene _talkScene; // null while disabled
        private HashSet<Collider> _currentlyIntersecting
            = new HashSet<Collider>();
        private Controller.Lock _lock; // null or valid

        protected override void OnStart()
        {
            base.OnStart();

            _controller = GetComponent<Controller>();
        }

        protected void OnEnable()
        {
            _talkScene = GameObject.FindObjectOfType<TalkScene>();
            if (_talkScene == null)
            {
                VRLog.Warn("TalkSceneHandler: TalkScene not found");
            }
        }

        protected void OnDisable()
        {
            _currentlyIntersecting.Clear();
            UpdateLock();
            _talkScene = null;
        }

        protected override void OnUpdate()
        {
            if (_lock != null)
            {
                HandleTrigger();
            }
        }

        private void HandleTrigger()
        {
            var device = SteamVR_Controller.Input((int)_controller.Tracking.index);
            if (device.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                PerformAction();
            }
        }

        private void PerformAction()
        {
            // Find the nearest intersecting collider.
            var nearest = _currentlyIntersecting
                .OrderBy(_col => (_col.transform.position - transform.position).sqrMagnitude)
                .FirstOrDefault();
            if (nearest == null)
            {
                return;
            }
            var kind = StripPrefix("Com/Hit/", nearest.tag);
            if (kind != null)
            {
                new Traverse(_talkScene).Method("TouchFunc", new[] { typeof(string), typeof(Vector3) }).GetValue(kind, Vector3.zero);
            }
        }

        protected void OnTriggerEnter(Collider other)
        {
            if (_talkScene == null)
            {
                return;
            }
            if (other.tag.StartsWith("Com/Hit/"))
            {
                _currentlyIntersecting.Add(other);
                UpdateLock();
            }
        }

        protected void OnTriggerExit(Collider other)
        {
            if (_talkScene == null)
            {
                return;
            }
            if (_currentlyIntersecting.Remove(other))
            {
                UpdateLock();
            }
        }

        private void UpdateLock()
        {
            if (_currentlyIntersecting.Count > 0 && _lock == null)
            {
                _controller.TryAcquireFocus(out _lock);
            }
            else if (_currentlyIntersecting.Count == 0 && _lock != null)
            {
                _lock.Release();
                _lock = null;
            }
        }
        private static string StripPrefix(string prefix, string str)
        {
            if (str.StartsWith(prefix))
            {
                return str.Substring(prefix.Length);
            }
            return null;
        }

    }

}
