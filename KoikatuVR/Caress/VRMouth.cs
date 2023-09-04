using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using HarmonyLib;
using KoikatuVR.Interpreters;

namespace KoikatuVR.Caress
{
    /// <summary>
    /// A component to be attached to the VR camera during an H scene.
    /// It allows the user to kiss in H scenes by moving their head.
    /// </summary>
    public class VRMouth : ProtectedBehaviour
    {
        private KoikatuSettings _settings;
        private AibuColliderTracker _aibuTracker;
        private Transform _firstFemale;
        private Transform _firstFemaleMouth;
        private VRMouthColliderObject _small, _large;
        private bool _inCaressMode = true;
        private readonly LongDistanceKissMachine _machine = new LongDistanceKissMachine();

        /// <summary>
        /// Indicates whether the currently running KissCo should end.
        /// null if KissCo is not running.
        /// </summary>
        private bool? _kissCoShouldEnd;
        /// <summary>
        /// Indicates whether the currently running KissCo should end.
        /// null if LickCo is not running.
        /// </summary>
        private bool? _lickCoShouldEnd;

        protected override void OnAwake()
        {
            base.OnAwake();
            _settings = VR.Context.Settings as KoikatuSettings;

            // Create 2 colliders, a small one for entering and a large one for exiting.
            _small = VRMouthColliderObject
                .Create("VRMouthSmall", new Vector3(0, 0, 0), new Vector3(0.05f, 0.05f, 0.07f));
            _small.TriggerEnter += HandleTriggerEnter;
            _large = VRMouthColliderObject
                .Create("VRMouthLarge", new Vector3(0, 0, 0.05f), new Vector3(0.1f, 0.1f, 0.15f));
            _large.TriggerExit += HandleTriggerExit;

            var hProc = GameObject.FindObjectOfType<HSceneProc>();

            if (hProc == null)
            {
                VRLog.Error("hProc is null");
                return;
            }
            _aibuTracker = new AibuColliderTracker(hProc, referencePoint: transform);
            var lstFemale = new Traverse(hProc).Field("lstFemale").GetValue<List<ChaControl>>();
            _firstFemale = lstFemale[0].objTop.transform;
            _firstFemaleMouth = lstFemale[0].objHeadBone.transform.Find(
                "cf_J_N_FaceRoot/cf_J_FaceRoot/cf_J_FaceBase/cf_J_FaceLow_tz/a_n_mouth");
        }

        private void OnDestroy()
        {
            GameObject.Destroy(_small.gameObject);
            GameObject.Destroy(_large.gameObject);
        }

        protected override void OnUpdate()
        {
            HandleScoreBasedKissing();
        }

        private void HandleScoreBasedKissing()
        {
            var inCaressMode = _aibuTracker.Proc.flags.mode == HFlag.EMode.aibu;
            if (inCaressMode)
            {
                bool decision = _settings.AutomaticKissing &&
                    _machine.Step(
                        Time.time,
                        _small.transform.InverseTransformPoint(_firstFemaleMouth.position),
                        _firstFemaleMouth.InverseTransformPoint(_small.transform.position),
                        Mathf.DeltaAngle(_firstFemale.eulerAngles.y, _firstFemaleMouth.transform.eulerAngles.y));
                if (decision)
                {
                    StartKiss();
                }
                else
                {
                    FinishKiss();
                }
            }

            if (_inCaressMode & !inCaressMode)
            {
                FinishKiss();
                _machine.Reset();
            }
            _inCaressMode = inCaressMode;
        }

        private void HandleTriggerEnter(Collider other)
        {
            if (_aibuTracker.AddIfRelevant(other))
            {
                var colliderKind = _aibuTracker.GetCurrentColliderKind(out int femaleIndex);
                UpdateKissLick(colliderKind);

                if (_kissCoShouldEnd == null &&
                    HandCtrl.AibuColliderKind.reac_head <= colliderKind &&
                    _settings.AutomaticTouchingByHmd &&
                    !CaressUtil.IsSpeaking(_aibuTracker.Proc, femaleIndex))
                {
                    StartCoroutine(TriggerReactionCo(femaleIndex, colliderKind));
                }
            }
        }

        private IEnumerator TriggerReactionCo(int femaleIndex, HandCtrl.AibuColliderKind colliderKind)
        {
            var kindFields = CaressUtil.GetHands(_aibuTracker.Proc)
                .Select(h => new Traverse(h).Field<HandCtrl.AibuColliderKind>("selectKindTouch"))
                .ToList();
            var oldKinds = kindFields.Select(f => f.Value).ToList();
            CaressUtil.SetSelectKindTouch(_aibuTracker.Proc, femaleIndex, colliderKind);
            yield return CaressUtil.ClickCo();
            for (int i = 0; i < kindFields.Count(); i++)
            {
                kindFields[i].Value = oldKinds[i];
            }
        }

        private void HandleTriggerExit(Collider other)
        {
            if (_aibuTracker.RemoveIfRelevant(other))
            {
                var colliderKind = _aibuTracker.GetCurrentColliderKind(out int _);
                UpdateKissLick(colliderKind);
            }
        }

        private void UpdateKissLick(HandCtrl.AibuColliderKind colliderKind)
        {
            if (!_inCaressMode && colliderKind == HandCtrl.AibuColliderKind.mouth && _settings.AutomaticKissing)
            {
                StartKiss();
            }
            else if(_settings.AutomaticLicking && IsLickingOk(colliderKind, out int layerNum))
            {
                StartLicking(colliderKind, layerNum);
            }
            else
            {
                if (!_inCaressMode)
                {
                    FinishKiss();
                }
                FinishLicking();
            }
        }

        private bool IsLickingOk(HandCtrl.AibuColliderKind colliderKind, out int layerNum)
        {
            layerNum = 0;
            if (colliderKind <= HandCtrl.AibuColliderKind.mouth ||
                HandCtrl.AibuColliderKind.reac_head <= colliderKind)
            {
                return false;
            }

            int bodyPartId = colliderKind - HandCtrl.AibuColliderKind.muneL;
            var hand = _aibuTracker.Proc.hand;
            var handTrav = new Traverse(hand);
            var layerInfos = handTrav.Field<Dictionary<int, HandCtrl.LayerInfo>[]>("dicAreaLayerInfos").Value[bodyPartId];
            int clothState = handTrav.Method("GetClothState", new[] { typeof(HandCtrl.AibuColliderKind) }).GetValue<int>(colliderKind);
            var layerKv = layerInfos.Where(kv => kv.Value.useArray == 2).FirstOrDefault();
            var layerInfo = layerKv.Value;
            layerNum = layerKv.Key;
            if (layerInfo == null)
            {
                VRLog.Warn("Licking not ok: no layer found");
                return false;
            }
            if (layerInfo.plays[clothState] == -1)
            {
                return false;
            }
            var heroine = _aibuTracker.Proc.flags.lstHeroine[0];
            if (_aibuTracker.Proc.flags.mode != HFlag.EMode.aibu &&
                colliderKind == HandCtrl.AibuColliderKind.anal &&
                !heroine.denial.anal &&
                heroine.hAreaExps[3] == 0f)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempt to start a kiss.
        /// </summary>
        private void StartKiss()
        {
            if (_kissCoShouldEnd != null || new Traverse(_aibuTracker.Proc.hand).Field<bool>("isKiss").Value)
            {
                // Already kissing.
                return;
            }

            _kissCoShouldEnd = false;
            StartCoroutine(KissCo());
        }

        private IEnumerator KissCo()
        {
            StopAllLicking();

            var hand = _aibuTracker.Proc.hand;
            var handTrav = new Traverse(hand);
            var selectKindTouchTrav = handTrav.Field<HandCtrl.AibuColliderKind>("selectKindTouch");

            var prevKindTouch = selectKindTouchTrav.Value;
            selectKindTouchTrav.Value = HandCtrl.AibuColliderKind.mouth;
            bool messageDelivered = false;
            HandCtrlHooks.InjectMouseButtonDown(0, () => messageDelivered = true);
            while (!messageDelivered)
            {
                yield return null;
            }
            yield return new WaitForEndOfFrame();

            // Try to restore the old value of selectKindTouch.
            if (selectKindTouchTrav.Value == HandCtrl.AibuColliderKind.mouth)
            {
                selectKindTouchTrav.Value = prevKindTouch;
            }

            var isKissTrav = handTrav.Field<bool>("isKiss");
            while (_kissCoShouldEnd == false && isKissTrav.Value)
            {
                yield return null;
            }

            HandCtrlHooks.InjectMouseButtonUp(0);
            _kissCoShouldEnd = null;
        }

        private void FinishKiss()
        {
            if (_kissCoShouldEnd == false)
            {
                _kissCoShouldEnd = true;
            }
        }

        private void StartLicking(HandCtrl.AibuColliderKind colliderKind, int layerNum)
        {
            if (_lickCoShouldEnd != null)
            {
                // Already licking.
                return;
            }

            var hand = _aibuTracker.Proc.hand;
            var handTrav = new Traverse(hand);
            int bodyPartId = colliderKind - HandCtrl.AibuColliderKind.muneL;
            var usedItem = handTrav.Field<HandCtrl.AibuItem[]>("useAreaItems").Value[bodyPartId];

            // If another item is being used on the target body part, detach it.
            if (usedItem != null && usedItem.idUse != 2)
            {
                hand.DetachItemByUseItem(usedItem.idUse);
            }

            StartCoroutine(LickCo(colliderKind, layerNum));
        }

        private IEnumerator LickCo(HandCtrl.AibuColliderKind colliderKind, int layerNum)
        {
            _lickCoShouldEnd = false;

            var hand = _aibuTracker.Proc.hand;
            var handTrav = new Traverse(hand);
            var areaItem = handTrav.Field<int[]>("areaItem").Value;
            int bodyPartId = colliderKind - HandCtrl.AibuColliderKind.muneL;
            var selectKindTouchTrav = handTrav.Field<HandCtrl.AibuColliderKind>("selectKindTouch");


            var oldLayerNum = areaItem[bodyPartId];
            areaItem[bodyPartId] = layerNum;

            while (_lickCoShouldEnd == false && areaItem[bodyPartId] == layerNum)
            {
                var oldKindTouch = selectKindTouchTrav.Value;
                selectKindTouchTrav.Value = colliderKind;
                yield return CaressUtil.ClickCo();
                selectKindTouchTrav.Value = oldKindTouch;
                yield return new WaitForSeconds(0.2f);
            }

            hand.DetachItemByUseItem(2);
            if (areaItem[bodyPartId] == layerNum)
            {
                areaItem[bodyPartId] = oldLayerNum;
            }

            _lickCoShouldEnd = null;
        }

        private void FinishLicking()
        {
            if (_lickCoShouldEnd == false)
            {
                _lickCoShouldEnd = true;
            }
        }

        private void StopAllLicking()
        {
            FinishLicking();
            _aibuTracker.Proc.hand.DetachItemByUseItem(2);
        }

        class VRMouthColliderObject : ProtectedBehaviour
        {
            public delegate void TriggerHandler(Collider other);
            public event TriggerHandler TriggerEnter;
            public event TriggerHandler TriggerExit;

            public static VRMouthColliderObject Create(string name, Vector3 center, Vector3 size)
            {
                var gameObj = new GameObject(name);
                gameObj.transform.localPosition = new Vector3(0, -0.07f, 0.02f);
                gameObj.transform.SetParent(VR.Camera.transform, false);

                var collider = gameObj.AddComponent<BoxCollider>();
                collider.size = size;
                collider.center = center;
                collider.isTrigger = true;

                gameObj.AddComponent<Rigidbody>().isKinematic = true;
                return gameObj.AddComponent<VRMouthColliderObject>();
            }

            protected void OnTriggerEnter(Collider other)
            {
                try
                {
                    TriggerEnter?.Invoke(other);
                }
                catch (Exception e)
                {
                    VRLog.Error(e);
                }
            }

            protected void OnTriggerExit(Collider other)
            {
                try
                {
                    TriggerExit?.Invoke(other);
                }
                catch (Exception e)
                {
                    VRLog.Error(e);
                }
            }
        }
    }
}

// Notes on some fields in HandCtrl:
//
// areaItem[p] : The layer num of the item to be used on the body part p
// useAreaItems[p] : The item currently being used on the body part p
// useItems[s] : The item currently in the slot s
// dicAreaLayerInfos[p][l].useArray : The slot to be used when the layer l is used on the body part p.
//     3 means either slot 0 (left hand) or 1 (right hand).
// item.idUse: The slot to be used for the item.
