using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace BloodMage
{
    class ExtrasManager : MonoBehaviour
    {
        public static ExtrasManager Instance;

        internal void Awake()
        {
            ExtrasManager.Instance = this;
        }

        public int VilePact = -28006;
        public int LeylineAbandonment = -28007;
        public int LeylineEntanglement = -28008;

        [HarmonyPatch(typeof(CharacterSkillKnowledge), nameof(CharacterSkillKnowledge.AddItem))]
        public class LeylineAbandonmentLearnedPatch
        {
            static bool Prefix(CharacterSkillKnowledge __instance, Item _item)
            {
                if(_item.ItemID == ExtrasManager.Instance.LeylineAbandonment)
                {
                    BloodMage.Log.LogMessage("Learning Leyline Abandonment, resetting investment.");
                    __instance.m_character.Stats.SetManaPoint(0);
                }
                return true;
            }
        }

        //[HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetManaPoint))]
        //public class LeylineAbandonmentManaPointPatch
        //{
        //    static void Postfix(CharacterStats __instance)
        //    {
        //        if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(ExtrasManager.Instance.LeylineAbandonment))
        //        {
        //            BloodMage.Log.LogMessage("Mana points suppressed.");
        //            __instance.m_manaPoint = 0;
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.AddStatStack))]
        //public class AddStatStackPatch
        //{
        //    static bool Prefix(CharacterStats __instance, Tag _stat, StatStack _stack, bool _multiplier)
        //    {
        //        if (_stat.TagName == "MaxMana")
        //        {
        //            if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(ExtrasManager.Instance.LeylineAbandonment))
        //            {
        //                _multiplier = false; //Make sure this is false
        //                float derived = _stack.RawValue * .25f; //20 mana -> 5 hp and 5 stamina, which is 1/4 of 20

        //                BloodMage.Log.LogMessage($"Health/stamina derived {derived}");

        //                //Create new statstack to add health
        //                Tag newTag = BloodMage.GetTagDefinition("MaxHealth");
        //                Tag[] healthTag = { newTag };
        //                Stat stat = __instance.GetStat(newTag);

        //                //Mirror what the original method would do
        //                if(stat != null)
        //                {
        //                    stat.AddStack(new StatStack("-28007:2:0", derived, healthTag), _multiplier);
        //                }

        //                //Set the variables for stamina and let the normal method run with manipulated values
        //                _stat = BloodMage.GetTagDefinition("MaxStamina");
        //                Tag[] staminaTag = { _stat };
        //                _stack = new StatStack("-28007:2:1", derived, staminaTag);
        //            }
        //        }
        //        return true;
        //    }
        //}
    }
}
