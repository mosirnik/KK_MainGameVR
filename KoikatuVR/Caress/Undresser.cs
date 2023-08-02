using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

using static ChaFileDefine;

namespace KoikatuVR.Caress
{
    class Undresser
   {
        private readonly Dictionary<Collider, InteractionBodyPart> _currentlyIntersecting =
            new Dictionary<Collider, InteractionBodyPart>();

        public void Enter(Collider collider)
        {
            if (ColliderBodyPart(collider) is InteractionBodyPart part)
            {
                _currentlyIntersecting.Add(collider, part);
            }
        }

        public void Exit(Collider collider)
        {
            _currentlyIntersecting.Remove(collider);
        }

        public ClothesKind? ComputeUndressTarget(List<ChaControl> females, out int femaleIndex)
        {
            femaleIndex = 0;
            if (_currentlyIntersecting.Count == 0)
            {
                return null;
            }
            var part = _currentlyIntersecting.Values.Min();
            var targets = _itemsForPart[(int)part];
            var female = females[femaleIndex];
            VRLog.Info($"undress targets={string.Join(",", targets.Select((y) => $"[{y.kind}:{y.max_state}]").ToArray())}, female={femaleIndex}");
            if (part == InteractionBodyPart.Crotch && IsWearingSkirt(female))
            {
                // Special case: if the character is wearing a skirt, allow
                // directly removing the underwear.
                targets = _skirtCrotchTargets;
            }
            foreach (var target in targets)
            {
                if (!female.IsClothesStateKind((int)target.kind))
                {
                    continue;
                }
                var state = female.fileStatus.clothesState[(int)target.kind];
                if (target.min_state <= state && state <= target.max_state)
                {
                    VRLog.Info($"toUndress: {target.kind}");
                    return target.kind;
                }
            }
            return null;
        }

        private static bool IsWearingSkirt(ChaControl female)
        {
            var objBot = female.objClothes[1];
            return objBot != null && objBot.GetComponent<DynamicBone>() != null;
        }

        /// <summary>
        /// A body part the user can interact with. A more specific part gets
        /// a lower number.
        /// </summary>
        enum InteractionBodyPart
        {
            Crotch,
            Groin,
            Breast,
            LegL,
            LegR,
            Forearm,
            UpperArm,
            Thigh,
            Torso,
        }

        private static readonly UndressTarget[][] _itemsForPart = new[]
        {
            new[] { Target(ClothesKind.bot, 0), Target(ClothesKind.panst), Target(ClothesKind.shorts) },
            new[] { Target(ClothesKind.bot, 0), Target(ClothesKind.panst), Target(ClothesKind.shorts) },
            new[] { Target(ClothesKind.top, 0), Target(ClothesKind.bra) },
            new[] { Target(ClothesKind.socks), Target(ClothesKind.shorts, 2, 2) },
            new[] { Target(ClothesKind.socks) },
            new UndressTarget[] { },
            new[] { Target(ClothesKind.top) },
            new[] { Target(ClothesKind.panst), Target(ClothesKind.bot), Target(ClothesKind.socks) },
            new[] { Target(ClothesKind.top) },
        };

        private static readonly UndressTarget[] _skirtCrotchTargets =
            new[] { Target(ClothesKind.panst), Target(ClothesKind.shorts), Target(ClothesKind.bot) };

        private static UndressTarget Target(ClothesKind kind, int max_state = 2, int min_state = 0)
        {
            return new UndressTarget(kind, max_state, min_state);
        }

        private static InteractionBodyPart? ColliderBodyPart(Collider collider)
        {
            var name = collider.name;
            if (name == "aibu_hit_kokan" ||
                name == "aibu_hit_ana")
                return InteractionBodyPart.Crotch;
            if (name.StartsWith("aibu_hit_siri") ||
                name.StartsWith("aibu_reaction_waist"))
                return InteractionBodyPart.Groin;
            if (name.StartsWith("cf_hit_bust"))
                return InteractionBodyPart.Breast;
            if (name == "aibu_reaction_legL")
                return InteractionBodyPart.LegL;
            if (name == "aibu_reaction_legR")
                return InteractionBodyPart.LegR;
            if (name.StartsWith("cf_hit_wrist"))
                return InteractionBodyPart.Forearm;
            if (name.StartsWith("cf_hit_arm"))
                return InteractionBodyPart.UpperArm;
            if (name.StartsWith("aibu_reaction_thigh"))
                return InteractionBodyPart.Thigh;
            if (name == "cf_hit_spine01" ||
                name == "cf_hit_spine03" ||
                name == "cf_hit_berry")
                return InteractionBodyPart.Torso;
            VRLog.Warn($"Unknwon collider: {collider.name}");
            return null;
        }

        public struct UndressTarget
        {
            public UndressTarget(ClothesKind k, int m, int mm)
            {
                kind = k;
                max_state = m;
                min_state = mm;
            }
            public ClothesKind kind;
            public int max_state;
            public int min_state;
        }
    }
}
