using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
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
            GameObject gameObject = base.gameObject;
            gameObject.AddComponent<Manager>();

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

        public class Manager : MonoBehaviour
        {
            public static Manager Instance;
            internal void Awake()
            {
                Manager.Instance = this;
            }

            //Idea stolen from IggyTheMad, adapted for my needs
            public IEnumerator RevertLeyline(Character character, float timer)
            {
                float derived = 0;
                if (character.Stats.m_manaPoint == 0)
                {
                    BloodMage.Log.LogMessage("Mana not obtained.");
                    character.Stats.GiveManaPoint(1);
                    yield return new WaitForSeconds(0.5f);
                    float hpstam = 5f;
                    derived = (character.Stats.MaxMana * .25f) + hpstam;
                }
                else
                {
                    BloodMage.Log.LogMessage("Mana obtained already.");
                    BloodMage.Log.LogMessage($"Mana current {character.Stats.MaxMana}");
                    derived = character.Stats.MaxMana * .25f;
                    
                }

                BloodMage.Log.LogMessage($"Mana to hp/stam {derived}");

                foreach (var stack in character.Stats.m_maxManaStat.RawStack)
                {
                    character.Stats.m_maxManaStat.RemoveRawStack(stack.SourceID);
                }

                character.Stats.m_maxHealthStat.BaseValue += derived;
                character.Stats.m_maxStamina.BaseValue += derived;


                yield break;
            }
        }

        

        //Hypovolemia Patch
        //Applies a % reduction after the original method determines the output damage
        //based on whichever bleed the player has currently.
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GetAmplifiedBleedingDamage))]
        public class HypovolemiaBleedDamagePatch
        {
            static void Postfix(CharacterStats __instance, ref float __result)
            {
                if (__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.HypovolemiaID))
                {
                    __result *= .5f;
                }
            }
        }

        
        //Leyline Abandonment and Entanglement Patch
        //Redirect consumption of skills to appropriate channels
        [HarmonyPatch(typeof(Skill), nameof(Skill.ConsumeResources))]
        public class LeylinePassivesAffectManaPatch
        {
            static bool Prefix(Skill __instance)
            {
                if(__instance.ManaCost != 0f)
                {
                    if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                    {
                        __instance.m_ownerCharacter.Stats.ReceiveDamage(__instance.ManaCost);
                        __instance.ManaCost = 0f;
                    }
                    else if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineEntanglement))
                    {
                        float derived = __instance.ManaCost / 2f;
                        __instance.m_ownerCharacter.Stats.UseMana(null, __instance.m_ownerCharacter.Stats.GetFinalManaConsumption(null, derived));
                        __instance.m_ownerCharacter.Stats.ReceiveDamage(derived);
                        __instance.ManaCost = 0f;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Skill), nameof(Skill.HasEnoughMana))]
        public class LeylineAbandonmentManaOverride
        {
            static bool Prefix(Skill __instance, ref bool __result)
            {
                if(__instance.m_characterUsing.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

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

        //Leyline Abandonment Reversion
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
                        __instance.m_character.Stats.IncreaseBurntMana(100);
                        BloodMage.Log.LogMessage($"New burnt Mana current {__instance.m_character.Stats.m_burntMana}");

                        __instance.m_character.Stats.m_maxHealthStat.BaseValue += derived;
                        __instance.m_character.Stats.m_maxStamina.BaseValue += derived;
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

                        float derived = _stack.RawValue * .25f; //20 mana -> 5 hp and 5 stamina, 1/4 of 20
                        BloodMage.Log.LogMessage($"Health/stamina derived {derived}");
                        __instance.m_character.Stats.m_maxHealthStat.BaseValue = __instance.m_character.Stats.m_maxHealthStat.BaseValue + derived;
                        __instance.m_character.Stats.m_maxStamina.BaseValue = __instance.m_character.Stats.m_maxStamina.BaseValue + derived;
                    }
                }
            }
        }

    }
}
