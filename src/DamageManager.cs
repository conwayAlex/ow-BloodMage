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
        public static DamageManager Instance;
        internal void Awake()
        {
            DamageManager.Instance = this;
        }

        public int HypovolemiaID = -28001;

        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GetAmplifiedBleedingDamage))]
        public class GetAmplifiedBleedingDamagePatch
        {
            static void Postfix(CharacterStats __instance, ref float __result)
            {
                if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(DamageManager.Instance.HypovolemiaID))
                {
                    __result *= .4f;
                }
            }
        }
    }
}
