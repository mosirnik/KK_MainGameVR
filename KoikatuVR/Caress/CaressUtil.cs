using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace KoikatuVR.Caress
{
    public class CaressUtil
    {
        /// <summary>
        /// Modify the internal state of the hand controls so that subsequent mouse button
        /// presses are interpreted to point to the specified (female, point) pair.
        /// </summary>
        public static void SetSelectKindTouch(HSceneProc proc, int femaleIndex, HandCtrl.AibuColliderKind colliderKind)
        {
            var hands = GetHands(proc);
            for (int i = 0; i < hands.Count; i++)
            {
                var kind = i == femaleIndex ? colliderKind : HandCtrl.AibuColliderKind.none;
                new Traverse(hands[i]).Field("selectKindTouch").SetValue(kind);
            }
        }

        public static List<HandCtrl> GetHands(HSceneProc proc)
        {
            var ret = new List<HandCtrl>();
            for (int i = 0; i < proc.flags.lstHeroine.Count; i++)
            {
                ret.Add(i == 0 ? proc.hand : Compat.HSceenProc_hand1(proc));
            }
            return ret;
        }

        /// <summary>
        /// Send a synthetic click event to the hand controls.
        /// </summary>
        /// <returns></returns>
        public static IEnumerator ClickCo()
        {
            bool consumed = false;
            HandCtrlHooks.InjectMouseButtonDown(0, () => consumed = true);
            while (!consumed)
            {
                yield return null;
            }
            HandCtrlHooks.InjectMouseButtonUp(0);
        }

        /// <summary>
        /// Is the specified female speaking? Moans are ignored.
        /// </summary>
        public static bool IsSpeaking(HSceneProc proc, int femaleIndex)
        {
            return proc.voice.nowVoices[femaleIndex].state == HVoiceCtrl.VoiceKind.voice &&
                Manager.Voice.Instance.IsVoiceCheck(proc.flags.transVoiceMouth[femaleIndex], true);
        }
    }
}
