using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SideLoader;

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

        public static int Hypovolemia = -28001;
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
        public class HypovolemiaBleedDamagePatch
        {
            static void Postfix(CharacterStats __instance, ref float __result)
            {
                if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.Hypovolemia))
                {
                    __result *= .5f;
                }
            }
        }

        
        //Leyline Abandonment and Entanglement Patch
        //Redirect consumption of skills to appropriate channels
        [HarmonyPatch(typeof(Skill), nameof(Skill.ConsumeResources))]
        public class LeylinePassivesConsumptionPatch
        {
            static bool Prefix(Skill __instance)
            {
                if(__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    __instance.HealthCost = __instance.ManaCost;
                    __instance.ManaCost = 0;
                    return true;
                }
                else if(__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineEntanglement))
                {
                    float derivedMana = __instance.HealthCost / 2f;
                    __instance.ManaCost += derivedMana;
                    float derivedHealth = __instance.ManaCost / 2f;
                    __instance.HealthCost += derivedHealth;
                    return true;
                }
                return true;
            }
        }

        //Leyline Abandonment Patch
        //Force game to allow casting with 0 mana
        [HarmonyPatch(typeof(Skill), nameof(Skill.HasEnoughMana))]
        public class LeylineAbandonmentManaOverride
        {
            static bool Prefix(Skill __instance, bool _tryingToActivate, ref bool __result)
            {
                if(__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    __result = __instance.HasEnoughHealth(_tryingToActivate);
                    return false;
                }
                return true;
            }
        }

        //Leyline Abandonment Patch
        //Disallow restoration of burnt mana set by passive.
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.RestoreBurntMana))]
        public class LeylineAbandonmentBurntManaRestoreOverride
        {
            static bool Prefix(CharacterStats __instance)
            {
                if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    return false;
                }
                return true;
            }
        }
        
        //Leyline Abandonment Patch
        //Force burnt mana to be 100% of mana while LA learned.
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.IncreaseBurntMana))]
        public class LeylineAbandonmentBurntManaPatch
        {
            static bool Prefix(CharacterStats __instance)
            {
                if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    __instance.m_burntMana = __instance.MaxMana;
                    return false;
                }
                return true;
            }
        }

        //Leyline Abandonment Patch
        //Takes players current max mana and gets hp/stam derivation
        //Burns all of players mana to remove as resource.
        //Adds derived hp and stamina to appropriate pools.
        [HarmonyPatch(typeof(CharacterKnowledge), nameof(CharacterKnowledge.AddItem))]
        public class LeylinePassivesLearnedPatch
        {
            static bool Prefix(CharacterKnowledge __instance, Item _item)
            {
                if(_item != null)
                {
                    if(_item.ItemID == BloodMage.LeylineAbandonment)
                    {
                        BloodMage.Log.LogMessage("Learning Leyline Abandonment");
                        float derived = __instance.m_character.Stats.MaxMana * .25f;
                        BloodMage.Log.LogMessage($"Mana to hp/stam {derived}");
                        if (derived == 0f)
                        {
                            return true;
                        }

                        BloodMage.Log.LogMessage($"Mana current {__instance.m_character.Stats.MaxMana}");
                        float currentMana = __instance.m_character.Stats.MaxMana;
                        __instance.m_character.Stats.m_burntMana = __instance.m_character.Stats.MaxMana;
                        BloodMage.Log.LogMessage($"New burnt Mana current {__instance.m_character.Stats.m_burntMana}");

                        __instance.m_character.Stats.m_maxHealthStat.BaseValue += derived;
                        __instance.m_character.Stats.m_maxStamina.BaseValue += derived;
                    }
                }

                return true;
            }
        }

        //Leyline Abandonment Patch
        //Undoes AddItem patch
        //Remove passive patch here?


        //Leyline Abandonment Patch
        //Supress further investment in the leyline.
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetManaPoint))]
        public class LeylineAbandonmentManaPointPatch
        {
            static bool Prefix(CharacterStats __instance)
            {
                if(__instance.m_character)
                {
                    if(__instance.m_character.Inventory)
                    {
                        if(__instance.m_character.Inventory.SkillKnowledge)
                        {
                            if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                            {
                                BloodMage.Log.LogMessage("Mana points suppressed.");
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
        }

        //Leyline Abandonment Patch
        //When more max mana gained, hp and stamina calculated and burnt mana maxed again.
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.AddStatStack))]
        public class LeylineAbandonmentAddStackPatch
        {
            static void Postfix(CharacterStats __instance, Tag _stat, StatStack _stack, bool _multiplier)
            {
                if (_stat.TagName == "MaxMana")
                {
                    if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                    {
                        __instance.m_burntMana = __instance.MaxMana;

                        float derived = _stack.RawValue * .25f; //20 mana -> 5 hp and 5 stamina, 1/4 of 20
                        BloodMage.Log.LogMessage($"Health/stamina derived {derived}");
                        __instance.m_character.Stats.m_maxHealthStat.BaseValue = __instance.m_character.Stats.m_maxHealthStat.BaseValue + derived;
                        __instance.m_character.Stats.m_maxStamina.BaseValue = __instance.m_character.Stats.m_maxStamina.BaseValue + derived;
                    }
                }
            }
        }

        //Leyline Abandonment Patch
        //Undo stat fiddlings from the adding patch.
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.RemoveStatStack))]
        public class LeylineAbandonmentStackRemovePatch
        {
            static void Prefix(CharacterStats __instance, Tag _stat, string _sourceID, bool _multiplier)
            {
                if(_stat.TagName == "MaxMana")
                {
                    if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                    {
                        StatStack stack = __instance.m_maxManaStat.m_rawStack[_sourceID];

                        float derived = stack.RawValue * .25f;

                        __instance.m_character.Stats.m_maxHealthStat.BaseValue = __instance.m_character.Stats.m_maxHealthStat.BaseValue - derived;
                        __instance.m_character.Stats.m_maxStamina.BaseValue = __instance.m_character.Stats.m_maxStamina.BaseValue - derived;

                        __instance.m_burntMana = __instance.MaxMana;
                    }
                }
            }
        }
    }
}
