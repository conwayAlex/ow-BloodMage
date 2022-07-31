using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using SideLoader;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using NodeCanvas.Tasks.Actions;

//using Object = UnityEngine.Object; //?


namespace BloodMage
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BloodMage : BaseUnityPlugin
    {
        //Metadata
        public const string GUID = "com.LlamaMage.bloodmage";
        public const string NAME = "Blood Mage";
        public const string VERSION = "0.9.1";

        public static BloodMage Instance;

        public static int HarnessBlood = -28000;
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

            SL.OnPacksLoaded += this.SL_OnPacksLoaded;

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
        
        private void SL_OnPacksLoaded()
        {
            SlimShady.Init();
        }


        public class LearnHarnessBlood : ActionNode
        {
            public override Status OnExecute(Component agent, IBlackboard bb)
            {
                Character instigator = bb.GetVariable<Character>("gInstigator").GetValue();

                if(!(instigator.Inventory.SkillKnowledge.IsItemLearned(BloodMage.HarnessBlood)))
                {
                    instigator.Inventory.ReceiveSkillReward(HarnessBlood);
                }


                return Status.Success;
                //return base.OnExecute(agent, bb);
            }
        }
        public static class SlimShady
        {
            public static void Init()
            {
                SLPack slpack = SL.GetSLPack("llama-mage-Blood_Mage");
                SlimShady.trainerTemplate = slpack.CharacterTemplates["com.llamamage.bloodmage.trainer"];
                SlimShady.trainerTemplate.OnSpawn += LocalTrainerSetup;
            }

            public static void LocalTrainerSetup(Character trainer, string _)
            {
                DestroyImmediate(trainer.GetComponent<CharacterStats>());
                DestroyImmediate(trainer.GetComponent<StartingEquipment>());

                //Create Actor
                DialogueActor componentInChildren = trainer.GetComponentInChildren<DialogueActor>();
                componentInChildren.SetName(SlimShady.trainerTemplate.Name);

                //Creater trainer
                Trainer componentInChildren2 = trainer.GetComponentInChildren<Trainer>();
                componentInChildren2.m_skillTreeUID = new UID("com.llamamage.bloodmage.trainer.tree");

                //Create dialogue tree
                DialogueTreeController componentInChildren3 = trainer.GetComponentInChildren<DialogueTreeController>();
                Graph graph = componentInChildren3.graph;

                //set up actor stuff
                List<DialogueTree.ActorParameter> actorParameters = (graph as DialogueTree)._actorParameters;
                actorParameters[0].actor = componentInChildren;
                actorParameters[0].name = componentInChildren.name;

                //Opening line
                StatementNodeExt statementNodeExt = graph.AddNode<StatementNodeExt>();
                statementNodeExt.statement = new Statement("Hey, you! Come here! How about I teach you how to draw on the power within?");
                statementNodeExt.SetActorName(componentInChildren.name);

                //Response graph
                MultipleChoiceNodeExt multipleChoiceNodeExt = graph.AddNode<MultipleChoiceNodeExt>();
                multipleChoiceNodeExt.availableChoices.Add(new MultipleChoiceNodeExt.Choice
                {
                    statement = new Statement
                    {
                        text = "I'm listening."
                    }
                });
                multipleChoiceNodeExt.availableChoices.Add(new MultipleChoiceNodeExt.Choice
                {
                    statement = new Statement
                    {
                        text = "How do I use this power?"
                    }
                });

                //Open skill tree
                ActionNode actionNode = graph.allNodes[1] as ActionNode;
                (actionNode.action as TrainDialogueAction).Trainer = new BBParameter<Trainer>(componentInChildren2);
                //Learn skill
                LearnHarnessBlood learnHarnessBlood = new LearnHarnessBlood();
                

                //Organize dialogue
                //Reset graph
                graph.allNodes.Clear();
                //Add Nodes
                graph.allNodes.Add(statementNodeExt);
                graph.allNodes.Add(multipleChoiceNodeExt);
                graph.allNodes.Add(actionNode);
                graph.allNodes.Add(learnHarnessBlood);

                //Begin graph
                graph.primeNode = statementNodeExt;
                //Opening line to -> two options
                graph.ConnectNodes(statementNodeExt, multipleChoiceNodeExt, -1, -1);

                graph.ConnectNodes(multipleChoiceNodeExt, actionNode, 0, -1);
                graph.ConnectNodes(multipleChoiceNodeExt, learnHarnessBlood, 1, -1);


                trainer.gameObject.SetActive(true);
            }

            internal static SL_Character trainerTemplate;
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
                    if(__instance.GetStaminaCost() > 0f)
                    {
                        return true;
                    }

                    if(__instance.ManaCost != 0f)
                    {
                        __instance.m_ownerCharacter.Stats.ReceiveDamage(__instance.ManaCost);
                    }

                    if(__instance.HealthCost != 0f)
                    {
                        __instance.m_ownerCharacter.Stats.ReceiveDamage(__instance.HealthCost);
                    }

                    return false;
                }
                else if(__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineEntanglement))
                {
                    if (__instance.ManaCost != 0f)
                    {
                        float derivedHealth = __instance.ManaCost / 2f;

                        __instance.m_ownerCharacter.Stats.ReceiveDamage(derivedHealth);
                        __instance.m_ownerCharacter.Stats.UseMana(null, derivedHealth);
                    }

                    if(__instance.HealthCost != 0f)
                    {
                        float derivedMana = __instance.HealthCost / 2f;

                        __instance.m_ownerCharacter.Stats.ReceiveDamage(derivedMana);
                        __instance.m_ownerCharacter.Stats.UseMana(null, derivedMana);
                    }

                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Skill), nameof(Skill.HasEnoughHealth))]
        public class LeylinePassivesHealthOverride
        {
            static bool Prefix(Skill __instance, bool _tryingToActivate, ref bool __result)
            {
                if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineEntanglement))
                {
                    float derived = __instance.ManaCost / 2f;
                    __result = (derived >= __instance.m_ownerCharacter.Health);

                    if (!__result && __instance.m_ownerCharacter.CharacterUI && _tryingToActivate)
                    {
                        __instance.m_ownerCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Skill_NotEnoughHealth");
                    }
                    return false;
                }
                return true;
            }
        }

        //Leyline Abandonment Patch
        //Force game to allow casting with 0 mana, logic for entanglement
        [HarmonyPatch(typeof(Skill), nameof(Skill.HasEnoughMana))]
        public class LeylinePassivesManaOverride
        {
            static bool Prefix(Skill __instance, bool _tryingToActivate, ref bool __result)
            {
                if(__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    __result = __instance.HasEnoughHealth(_tryingToActivate);
                    return false;
                }
                else if (__instance.m_ownerCharacter.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineEntanglement))
                {
                    float derived = (__instance.m_ownerCharacter.Stats) ? __instance.m_ownerCharacter.Stats.GetFinalManaConsumption(null, __instance.ManaCost) : __instance.ManaCost;
                    __result = (derived / 2f) >= __instance.m_ownerCharacter.Mana;

                    if(!__result && __instance.m_ownerCharacter.CharacterUI && _tryingToActivate)
                    {
                        __instance.m_ownerCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Skill_NotEnoughMana");
                    }
                    return false;
                }
                return true;
            }
        }

        //Leyline Abandonment Patch
        //Disallow restoration of burnt mana set by passive.
        /*[HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.RestoreBurntMana))]
        public class LeylineAbandonmentBurntManaRestoreOverride
        {
            static bool Prefix(CharacterStats __instance)
            {
                if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    BloodMage.Log.LogMessage($"burnt mana before restore {__instance.m_burntMana}");
                    __instance.m_burntMana = __instance.m_character.Stats.m_maxManaStat.m_currentValue;
                    BloodMage.Log.LogMessage($"burnt mana after restore {__instance.m_burntMana}");
                    return false;
                }
                return true;
            }
        }*/
        
        //Leyline Abandonment Patch
        //Force burnt mana to be 100% of mana while LA learned.
        /*[HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.IncreaseBurntMana))]
        public class LeylineAbandonmentBurntManaPatch
        {
            static bool Prefix(CharacterStats __instance)
            {
                if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    BloodMage.Log.LogMessage($"burnt mana before increase {__instance.m_burntMana}");
                    __instance.m_burntMana = __instance.m_character.Stats.m_maxManaStat.m_currentValue;
                    BloodMage.Log.LogMessage($"burnt mana after increase {__instance.m_burntMana}");
                    return false;
                }
                return true;
            }
        }*/

        //Leyline Abandonment Patch
        //Takes players current max mana and gets hp/stam derivation
        //Burns all of players mana to remove as resource.
        //Adds derived hp and stamina to appropriate pools.
        [HarmonyPatch(typeof(CharacterKnowledge), nameof(CharacterKnowledge.AddItem))]
        public class LeylinePassivesLearnedPatch
        {
            static bool Prefix(CharacterKnowledge __instance, Item _item)
            {
                if (_item != null)
                {
                    if (_item.ItemID == BloodMage.LeylineAbandonment)
                    {
                        BloodMage.Log.LogMessage("Learning Leyline Abandonment");
                        StatStack statstack;

                        //Learning with mana active
                        if (__instance.m_character.Stats.m_manaPoint > 0)
                        {
                            BloodMage.Log.LogMessage("Reset Mana Points");
                            //Reset mana points
                            __instance.m_character.Stats.m_manaPoint = 0;
                        }

                        BloodMage.Log.LogMessage("Evaluating stat stacks");
                        //Grab all stat stacks for mana and convert
                        foreach (var stack in __instance.m_character.Stats.m_maxManaStat.RawStack)
                        {
                            //Skip the dedicated mana points stat
                            if (stack.SourceID.Contains("ManaAugmentation"))
                            {
                                continue;
                            }
                            //Add hp & stam for a 1/4 of mana added from stacks
                            statstack = new StatStack(stack.SourceID, stack.RawValue * .25f, null);
                            __instance.m_character.Stats.m_maxHealthStat.AddRawStack(statstack);
                            __instance.m_character.Stats.m_maxStamina.AddRawStack(statstack);
                        }
                    }
                }
                return true;
            }
        }

        //Leyline Abandonment Patch
        //Undoes AddItem patch
        //Remove passive patch here?
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.GiveManaPoint))]
        public class LeylineAbandonmentGiveManaPointPatch
        {
            static void Postfix(CharacterStats __instance)
            {
                if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                {
                    __instance.m_manaPoint = 0;
                }
            }
        }

        //Leyline Abandonment Patch
        //When more max mana gained, hp and stamina calculated and burnt mana maxed again.
        [HarmonyPatch(typeof(CharacterStats), nameof(CharacterStats.AddStatStack))]
        public class LeylineAbandonmentAddStackPatch
        {
            static bool Prefix(CharacterStats __instance, Tag _stat, StatStack _stack, bool _multiplier)
            {
                if (_stat.TagName == "MaxMana")
                {
                    if(__instance.m_character.Inventory.SkillKnowledge.IsItemLearned(BloodMage.LeylineAbandonment))
                    {
                        float derived = _stack.RawValue * .25f; //20 mana -> 5 hp and 5 stamina, 1/4 of 20

                        StatStack statstack = new StatStack(_stack.SourceID, derived, null);
                        __instance.m_character.Stats.m_maxHealthStat.AddRawStack(statstack);
                        __instance.m_character.Stats.m_maxStamina.AddRawStack(statstack);

                    }
                }

                return true;
            }
        }
    }
}
