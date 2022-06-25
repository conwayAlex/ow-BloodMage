﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BloodMage
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BloodMage : BaseUnityPlugin
    {
        //Metadata
        public const string GUID = "com.LlamaMage.bloodmage";
        public const string NAME = "Blood Mage";
        public const string VERSION = "0.1.1";

        public static BloodMage Instance;

        public static int HypovolemiaID = -28001;

        public static int VilePact = -28006;
        public static int LeylineAbandonment = -28007;
        public static int LeylineEntanglement = -28008;

        // For accessing your BepInEx Logger from outside of this class (eg Plugin.Log.LogMessage("");)
        internal static ManualLogSource Log;

        // If you need settings, define them like so:
        //public static ConfigEntry<bool> ExampleConfig;

        // Awake is called when your plugin is created. Use this to set up your mod.
        internal void Awake()
        {
            Log = this.Logger;
            Log.LogMessage($"{NAME} {VERSION} loading...");

            BloodMage.Instance = this;
            
            // Any config settings you define should be set up like this:
            //ExampleConfig = Config.Bind("ExampleCategory", "ExampleSetting", false, "This is an example setting.");

            var harmony = new Harmony(GUID);
            harmony.PatchAll();

            Log.LogMessage($"{NAME} {VERSION} load complete.");
        }

        // Update is called once per frame. Use this only if needed.
        // You also have all other MonoBehaviour methods available (OnGUI, etc)
        internal void Update()
        {

        }

        //The below code does not belong to me, taken from Emo on the OW modding discord
        public static Tag GetTagDefinition(string TagName)
        {
            foreach (var item in TagSourceManager.Instance.m_tags)
            {

                if (item.TagName == TagName)
                {
                    return item;
                }
            }

            return default(Tag);
        }

        //Hypovolemia Patch
        //Applies a % reduction after the original method determines the output damage
        //based on whichever bleed the player has currently.
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GetAmplifiedBleedingDamage))]
        public class GetAmplifiedBleedingDamagePatch
        {
            static void Postfix(CharacterStats __instance, ref float __result)
            {
                if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.HypovolemiaID))
                {
                    __result *= .5f;
                }
            }
        }

        //[HarmonyPatch(typeof(Item), nameof(Item.RegisterKnowledge))]
        //public class LeylineAbandonmentRegisteredPatch
        //{
        //    static bool Prefix(Item __instance)
        //    {
        //        if(__instance.ItemID == BloodMage.LeylineAbandonment)
        //        {
        //            if(!__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
        //            {
        //                BloodMage.Log.LogMessage("Learning Leyline Abandonment test");
        //                if (__instance.m_ownerCharacter.Stats.m_manaPoint == 0)
        //                {
        //                    BloodMage.Log.LogMessage("Mana not obtained.");
        //                    __instance.m_ownerCharacter.Stats.SetManaPoint(1);

        //                    float hpstam = 5f;
        //                    float derived = __instance.m_ownerCharacter.Stats.MaxMana * .25f;

        //                    foreach(var stack in __instance.m_ownerCharacter.Stats.m_maxManaStat.RawStack)
        //                    {
        //                        __instance.m_ownerCharacter.Stats.m_maxManaStat.RemoveRawStack(stack.SourceID);
        //                    }
        //                    __instance.m_ownerCharacter.Stats.m_maxManaStat.BaseValue = 0;

        //                    __instance.m_ownerCharacter.Stats.m_maxHealthStat.BaseValue += derived + hpstam;
        //                    __instance.m_ownerCharacter.Stats.m_maxStamina.BaseValue += derived += hpstam;

        //                }
        //                else
        //                {
        //                    BloodMage.Log.LogMessage("Mana obtained already.");
        //                    BloodMage.Log.LogMessage($"Mana current {__instance.m_ownerCharacter.Stats.MaxMana}");

        //                    float derived = __instance.m_ownerCharacter.Stats.MaxMana * .25f;
        //                    __instance.m_ownerCharacter.Stats.m_maxManaStat.BaseValue = 0;

        //                    BloodMage.Log.LogMessage($"Mana to hp/stam {derived}");

        //                    foreach (var stack in __instance.m_ownerCharacter.Stats.m_maxManaStat.RawStack)
        //                    {
        //                        __instance.m_ownerCharacter.Stats.m_maxManaStat.RemoveRawStack(stack.SourceID);
        //                    }

        //                    __instance.m_ownerCharacter.Stats.m_maxHealthStat.BaseValue += derived;
        //                    __instance.m_ownerCharacter.Stats.m_maxStamina.BaseValue += derived;
        //                }
        //                return true;
        //            }
        //            return false;
        //        }
        //        return true;
        //    }
        //}


        [HarmonyPatch(typeof(CharacterSkillKnowledge), nameof(CharacterSkillKnowledge.AddItem))]
        public class LeylineAbandonmentLearnedPatch
        {
            static bool Prefix(CharacterSkillKnowledge __instance, Item _item)
            {
                //If player is learning skill, remove all mana points
                //and refund health and stamina. 

                if (_item != null && !__instance.m_learnedItemUIDs.Contains(_item.UID))
                {
                    if(_item.ItemID == BloodMage.LeylineAbandonment)
                    {
                        BloodMage.Log.LogMessage("Learning Leyline Abandonment");

                        if (__instance.m_character.Stats.m_manaPoint == 0)
                        {
                            BloodMage.Log.LogMessage("Mana not obtained.");
                            __instance.m_character.Stats.SetManaPoint(1);
                            float hpstam = 5f;
                            float derived = __instance.m_character.Stats.MaxMana * .25f;

                            __instance.m_character.Stats.m_maxManaStat.BaseValue = 0;

                            foreach (var stack in __instance.m_character.Stats.m_maxManaStat.RawStack)
                            {
                                __instance.m_character.Stats.m_maxManaStat.RemoveRawStack(stack.SourceID);
                            }

                            __instance.m_character.Stats.m_maxHealthStat.BaseValue = __instance.m_character.Stats.m_maxHealthStat.BaseValue + derived + hpstam;
                            __instance.m_character.Stats.m_maxStamina.BaseValue = __instance.m_character.Stats.m_maxStamina.BaseValue + derived + hpstam;
                        }
                        else
                        {
                            BloodMage.Log.LogMessage("Mana obtained already.");
                            BloodMage.Log.LogMessage($"Mana current {__instance.m_character.Stats.MaxMana}");
                            float derived = __instance.m_character.Stats.MaxMana * .25f;
                            __instance.m_character.Stats.m_maxManaStat.BaseValue = 0;

                            BloodMage.Log.LogMessage($"Mana to hp/stam {derived}");

                            foreach (var stack in __instance.m_character.Stats.m_maxManaStat.RawStack)
                            {
                                __instance.m_character.Stats.m_maxManaStat.RemoveRawStack(stack.SourceID);
                            }

                            __instance.m_character.Stats.m_maxHealthStat.BaseValue = __instance.m_character.Stats.m_maxHealthStat.BaseValue + derived;
                            __instance.m_character.Stats.m_maxStamina.BaseValue = __instance.m_character.Stats.m_maxStamina.BaseValue + derived;
                        }

                    }
                }
                return true;
            }
        }

        //The purpose behind this patch is to prevent players from investing further in the Leyline
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetManaPoint))]
        public class LeylineAbandonmentManaPointPatch
        {
            static bool Prefix(CharacterStats __instance)
            {
                if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    BloodMage.Log.LogMessage("Mana points suppressed.");
                    return false;
                }
                return true;
            }
        }

        //Leyline Abandonment will also cause any mana gained to instead be
        //gained as its counterpart health and stamina
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.AddStatStack))]
        public class AddStatStackPatch
        {
            static void Postfix(CharacterStats __instance, Tag _stat, StatStack _stack, bool _multiplier)
            {
                if (_stat.TagName == "MaxMana")
                {
                    if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                    {
                        __instance.m_character.Stats.RemoveStatStack(_stat, _stack.SourceID, _multiplier);

                        float derived = _stack.RawValue * .25f; //20 mana -> 5 hp and 5 stamina, which is 1/4 of 20
                        BloodMage.Log.LogMessage($"Health/stamina derived {derived}");
                        __instance.m_character.Stats.m_maxHealthStat.BaseValue = __instance.m_character.Stats.m_maxHealthStat.BaseValue + derived;
                        __instance.m_character.Stats.m_maxStamina.BaseValue = __instance.m_character.Stats.m_maxStamina.BaseValue + derived;


                        //_multiplier = false; //make sure this is false
                        
                        ////create new statstack to add health
                        //Tag newtag = BloodMage.GetTagDefinition("MaxHealth");
                        //Tag[] healthtag = { newtag };
                        //Stat stat = __instance.GetStat(newtag);

                        ////mirror what the original method would do
                        //if (stat != null)
                        //{
                        //    _stack = new StatStack("-28007:2:0", derived, healthtag);
                        //    _stack.EffectiveValue = derived;
                        //    stat.AddStack(_stack, _multiplier);
                        //}

                        ////set the variables for stamina and let the normal method run with manipulated values
                        //_stat = BloodMage.GetTagDefinition("MaxStamina");
                        //Tag[] staminatag = { _stat };
                        //_stack = new StatStack("-28007:2:1", derived, staminatag);
                        //_stack.EffectiveValue = derived;
                    }
                }
            }
        }

    }
}
