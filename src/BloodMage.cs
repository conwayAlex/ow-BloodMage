using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

        [HarmonyPatch(typeof(CharacterSkillKnowledge), nameof(CharacterSkillKnowledge.AddItem))]
        public class LeylineAbandonmentLearnedPatch
        {
            static bool Prefix(CharacterSkillKnowledge __instance, Item _item)
            {
                //If player is learning skill, remove all mana points
                //and refund health and stamina. 
                if (_item.ItemID == BloodMage.LeylineAbandonment)
                {
                    BloodMage.Log.LogMessage("Learning Leyline Abandonment, resetting investment.");


                    //player has this much mana when learning your skill
                    float CurrentMana = __instance.m_character.Stats.MaxMana;
                    __instance.m_character.Stats.SetManaPoint(0);


                    float PercentHp = CurrentMana * 0.25f;
                    float PercentStamina = CurrentMana * 0.25f;

                    __instance.m_character.Stats.m_maxHealthStat.BaseValue = __instance.m_character.Stats.m_maxHealthStat.BaseValue + PercentHp;
                    __instance.m_character.Stats.m_maxStamina.BaseValue = __instance.m_character.Stats.m_maxStamina.BaseValue + PercentStamina;
                }
                return true;
            }
        }
        
        //The purpose behind this patch is to prevent players from investing further in the Leyline
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.SetManaPoint))]
        public class LeylineAbandonmentManaPointPatch
        {
            static void Postfix(CharacterStats __instance)
            {
                if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    BloodMage.Log.LogMessage("Mana points suppressed.");
                    __instance.m_manaPoint = 0;
                }
            }
        }

        //Leyline Abandonment will also cause any mana gained to instead be
        //gained as its counterpart health and stamina
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.AddStatStack))]
        public class AddStatStackPatch
        {
            static bool Prefix(CharacterStats __instance, Tag _stat, StatStack _stack, bool _multiplier)
            {
                if (_stat.TagName == "MaxMana")
                {
                    if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                    {
                        _multiplier = false; //Make sure this is false
                        float derived = _stack.RawValue * .25f; //20 mana -> 5 hp and 5 stamina, which is 1/4 of 20

                        BloodMage.Log.LogMessage($"Health/stamina derived {derived}");

                        //Create new statstack to add health
                        Tag newTag = BloodMage.GetTagDefinition("MaxHealth");
                        Tag[] healthTag = { newTag };
                        Stat stat = __instance.GetStat(newTag);

                        //Mirror what the original method would do
                        if (stat != null)
                        {
                            stat.AddStack(new StatStack("-28007:2:0", derived, healthTag), _multiplier);
                        }

                        //Set the variables for stamina and let the normal method run with manipulated values
                        _stat = BloodMage.GetTagDefinition("MaxStamina");
                        Tag[] staminaTag = { _stat };
                        _stack = new StatStack("-28007:2:1", derived, staminaTag);
                    }
                }
                return true;
            }
        }

    }
}
