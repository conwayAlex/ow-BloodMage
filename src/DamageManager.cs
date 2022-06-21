using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace BloodMage
{
    class DamageManager : MonoBehaviour
    {
        internal void Awake()
        {
            DamageManager.Instance = this;
        }

        public static DamageManager Instance;

        public int HypovolemiaID = -28001;

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GetAmplifiedBleedingDamage))]
        public class GetAmplifiedBleedingDamagePatch
        {
            static void Postfix(CharacterStats __instance, ref float _damage)
            {
                if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(DamageManager.Instance.HypovolemiaID))
                {
                    _damage = .1f * _damage;
                }
            }
        }

        //[HarmonyPatch(typeof(AffectHealth), nameof(AffectHealth.ActivateLocally))]
        //public class AffectHealthPatch
        //{
        //    static void Prefix(AffectHealth __instance, Character _affectedCharacter, ref object[] _infos )
        //    {
        //        if(_affectedCharacter != null && _affectedCharacter.Alive)
        //        {
        //            float num = __instance.GetAffectQuantity(_affectedCharacter);
        //            num *= (__instance.IsModifier ? (0.01f * _affectedCharacter.Stats.MaxHealth) : 1f);
        //            if(num < 0f)

        //            if (__instance.IsStatusEffect)
        //            {
        //                if (__instance.EffectType.Name == "Bleeding")
        //                {


        //                    if (_affectedCharacter.Inventory.SkillKnowledge.IsItemLearned(DamageManager.Instance.HypovolemiaID))
        //                    {
        //                        //damage * x
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    static void Postfix(AffectHealth __instance, Character _affectedCharacter, ref object[] _infos)
        //    {
        //        if (__instance.IsStatusEffect)
        //        {
        //            if (__instance.EffectType.Name == "Bleeding")
        //            {
        //                if (_affectedCharacter.Inventory.SkillKnowledge.IsItemLearned(DamageManager.Instance.HypovolemiaID))
        //                {
        //                    //damage / x
        //                }
        //            }
        //        }
        //    }
        //}

    }
}
