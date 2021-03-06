﻿using Jobs;
using NPC;
using Pandaros.API.Entities;
using Pandaros.API.Items.Armor;
using Pandaros.API.Items.Weapons;
using Pipliz;
using Pipliz.JSON;
using Recipes;
using System;
using System.Collections.Generic;
using static ItemTypes;
using Random = Pipliz.Random;
using Time = Pipliz.Time;

namespace Pandaros.API.ColonyManagement
{
    [ModLoader.ModManager]
    public static class ColonistManager
    {
        public const string KNOWN_ITTERATIONS = "SKILLED_ITTERATIONS";
        public const int _NUMBEROFCRAFTSPERPERCENT = 200;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameInitializer.NAMESPACE + ".SettlerManager.OnUpdate")]
        public static void OnUpdate()
        {
            if (ServerManager.ColonyTracker != null)
                foreach (var colony in ServerManager.ColonyTracker.ColoniesByID.Values)
                {
                    var cs = ColonyState.GetColonyState(colony);

                    UpdateMagicItemms(cs);
                }
        }

        private static void UpdateMagicItemms(ColonyState state)
        {
            try
            {
                if (state.MagicUpdateTime < Time.SecondsSinceStartDouble)
                {
                    var colony = state.ColonyRef;

                    foreach (var follower in colony.Followers)
                    {
                        var inv = ColonistInventory.Get(follower);

                        if (inv.MagicItemUpdateTime < Time.SecondsSinceStartDouble)
                        {
                            foreach (var item in inv.Armor)
                                if (item.Value.Id != 0 && ArmorFactory.ArmorLookup.TryGetValue(item.Value.Id, out var armor))
                                {
                                    armor.Update();

                                    if (armor.HPTickRegen != 0)
                                        follower.Heal(armor.HPTickRegen);
                                }

                            if (WeaponFactory.WeaponLookup.TryGetValue(inv.Weapon.Id, out var wep))
                            {
                                wep.Update();

                                if (wep.HPTickRegen != 0)
                                    follower.Heal(wep.HPTickRegen);
                            }

                            inv.MagicItemUpdateTime = Time.SecondsSinceStartDouble + Random.Next(3, 5);
                        }
                    }

                    state.MagicUpdateTime = Time.SecondsSinceStartDouble + 5;
                }
            }
            catch (Exception ex)
            {
                APILogger.LogError(ex);
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCLoaded, GameInitializer.NAMESPACE + ".SettlerManager.OnNPCLoaded")]
        public static void OnNPCLoaded(NPCBase npc, JSONNode node)
        {
            if (npc.CustomData == null)
                npc.CustomData = new JSONNode();

            if (node.TryGetAs<JSONNode>(GameInitializer.SETTLER_INV, out var invNode))
                npc.CustomData.SetAs(GameInitializer.SETTLER_INV, new ColonistInventory(invNode, npc));

            if (node.TryGetAs<float>(GameInitializer.ALL_SKILLS, out var skills))
                npc.CustomData.SetAs(GameInitializer.ALL_SKILLS, skills);

            if (node.TryGetAs<int>(KNOWN_ITTERATIONS, out var jobItterations))
                npc.CustomData.SetAs(KNOWN_ITTERATIONS, jobItterations);

            var npcInv = ColonistInventory.Get(npc);

            if (npc.NPCType.IsLaborer)
                npcInv.UnemployedLeaveTime = TimeCycle.TotalHours + 48;
            else
                npcInv.UnemployedLeaveTime = 0;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCSaved, GameInitializer.NAMESPACE + ".SettlerManager.OnNPCSaved")]
        public static void OnNPCSaved(NPCBase npc, JSONNode node)
        {
            node.SetAs(GameInitializer.SETTLER_INV, ColonistInventory.Get(npc).ToJsonNode());

            if (npc.CustomData.TryGetAs(GameInitializer.ALL_SKILLS, out float allSkill))
                node.SetAs(GameInitializer.ALL_SKILLS, allSkill);

        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCGathered, GameInitializer.NAMESPACE + ".SettlerManager.OnNPCGathered")]
        public static void OnNPCGathered(IJob job, Vector3Int location, List<ItemTypeDrops> results)
        {
            if (job != null && job.NPC != null && results != null && results.Count > 0)
            {
                IncrimentSkill(job.NPC);
                var inv = ColonistInventory.Get(job.NPC);
               
                foreach (var item in results)
                {
                    if (ItemTypes.TryGetType(item.Type, out var itemType))
                        inv.IncrimentStat(itemType.Name, item.Amount);
                }
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCCraftedRecipe, GameInitializer.NAMESPACE + ".SettlerManager.OnNPCCraftedRecipe")]
        public static void OnNPCCraftedRecipe(IJob job, Recipe recipe, List<RecipeResult> results)
        {
            IncrimentSkill(job.NPC);
            var inv = ColonistInventory.Get(job.NPC);
            inv.IncrimentStat("Number of Crafts");

        }

        public static void IncrimentSkill(NPCBase npc)
        {
            GetSkillInformation(npc, out var nextLevel, out var itt, out var allSkill);

            if (itt >= nextLevel)
            {
                var nextFloat = allSkill + 0.005f;

                if (nextFloat > 0.25f)
                    nextFloat = 0.25f;

                npc.CustomData.SetAs(KNOWN_ITTERATIONS, 1);
                npc.CustomData.SetAs(GameInitializer.ALL_SKILLS, nextFloat);
            }
        }

        public static void GetSkillInformation(NPCBase npc, out int nextLevel, out int itt, out float allSkill)
        {
            if (!npc.CustomData.TryGetAs(KNOWN_ITTERATIONS, out itt))
            {
                npc.CustomData.SetAs(KNOWN_ITTERATIONS, 1);
                itt = 1;
            }
            else
            {
                itt++;
                npc.CustomData.SetAs(KNOWN_ITTERATIONS, itt);
            }

            if (!npc.CustomData.TryGetAs(GameInitializer.ALL_SKILLS, out allSkill))
            {
                npc.CustomData.SetAs(GameInitializer.ALL_SKILLS, 0.005f);
                allSkill = 0.005f;
            }

            nextLevel = Pipliz.Math.RoundToInt(allSkill * 1000) * _NUMBEROFCRAFTSPERPERCENT;
        }

        public static float PenalizeFood(Colony c, float percent)
        {
            var cost = (float)System.Math.Ceiling(c.Stockpile.TotalFood * percent);
            var num = 0f;

            if (cost < 1)
                cost = 1;

            c.Stockpile.TryRemoveFood(ref num, cost);
            return cost;
        }


        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCJobChanged, GameInitializer.NAMESPACE + ".SettlerManager.OnNPCJobChanged")]
        public static void OnNPCJobChanged(ValueTuple<NPCBase, IJob, IJob> data)
        {
            try
            {
                if (data.Item1 != null)
                {
                    if (data.Item1.CustomData == null)
                        data.Item1.CustomData = new JSONNode();

                    var inv = ColonistInventory.Get(data.Item1);

                    if (data.Item1.NPCType.IsLaborer)
                        inv.UnemployedLeaveTime = TimeCycle.TotalHours + 48;
                    else
                        inv.UnemployedLeaveTime = 0;
                }

                if (data.Item3 is GuardJobInstance guardJob && data.Item3.TryGetNPCGuardDefaultSettings(out var settings))
                {
                    if (settings != null)
                    {
                        var setting = Activator.CreateInstance(settings.GetType()) as GuardJobSettings;
                        setting.BlockTypes = settings.BlockTypes;
                        setting.CooldownMissingItem = settings.CooldownMissingItem;
                        setting.CooldownSearchingTarget = settings.CooldownSearchingTarget;
                        setting.CooldownShot = settings.CooldownShot;
                        setting.Damage = settings.Damage;
                        setting.NPCType = settings.NPCType;
                        setting.NPCTypeKey = settings.NPCTypeKey;
                        setting.OnHitAudio = settings.OnHitAudio;
                        setting.OnShootAudio = settings.OnShootAudio;
                        setting.OnShootResultItem = settings.OnShootResultItem;
                        setting.Range = settings.Range;
                        setting.RecruitmentItem = settings.RecruitmentItem;
                        setting.ShootItem = settings.ShootItem;
                        setting.SleepType = settings.SleepType;
                        
                        guardJob.Settings = setting;
                    }
                }
                else if (data.Item3 is CraftingJobInstance craftingJob)
                {
                    if (craftingJob.Settings.GetType() == typeof(CraftingJobRotatedLitSettings) && data.Item3.TryGetNPCCraftDefaultSettings(out CraftingJobRotatedLitSettings craftSettingslit))
                    {
                        var litSettings = Activator.CreateInstance(craftSettingslit.GetType(), craftSettingslit.BlockTypes[0].Name, craftSettingslit.NPCTypeKey, craftSettingslit.CraftingCooldown, craftSettingslit.MaxCraftsPerHaul, craftSettingslit.OnCraftedAudio) as CraftingJobRotatedLitSettings;

                        litSettings.BlockTypes = craftSettingslit.BlockTypes;
                        litSettings.CraftingCooldown = craftSettingslit.CraftingCooldown;
                        litSettings.MaxCraftsPerHaul = craftSettingslit.MaxCraftsPerHaul;
                        litSettings.NPCType = craftSettingslit.NPCType;
                        litSettings.NPCTypeKey = craftSettingslit.NPCTypeKey;
                        litSettings.OnCraftedAudio = craftSettingslit.OnCraftedAudio;
                        litSettings.RecruitmentItem = craftSettingslit.RecruitmentItem;

                        craftingJob.Settings = litSettings;
                    }
                    else if (craftingJob.Settings.GetType() == typeof(CraftingJobRotatedSettings) && data.Item3.TryGetNPCCraftDefaultSettings(out CraftingJobRotatedSettings craftSettingsRot))
                    {
                        var litSettings = Activator.CreateInstance(craftSettingsRot.GetType(), craftSettingsRot.BlockTypes[0].Name, craftSettingsRot.NPCTypeKey, craftSettingsRot.CraftingCooldown, craftSettingsRot.MaxCraftsPerHaul, craftSettingsRot.OnCraftedAudio) as CraftingJobRotatedSettings;

                        litSettings.BlockTypes = craftSettingsRot.BlockTypes;
                        litSettings.CraftingCooldown = craftSettingsRot.CraftingCooldown;
                        litSettings.MaxCraftsPerHaul = craftSettingsRot.MaxCraftsPerHaul;
                        litSettings.NPCType = craftSettingsRot.NPCType;
                        litSettings.NPCTypeKey = craftSettingsRot.NPCTypeKey;
                        litSettings.OnCraftedAudio = craftSettingsRot.OnCraftedAudio;
                        litSettings.RecruitmentItem = craftSettingsRot.RecruitmentItem;

                        craftingJob.Settings = litSettings;
                    }
                    else if (craftingJob.Settings.GetType() == typeof(CraftingJobSettings) && data.Item3.TryGetNPCCraftDefaultSettings(out CraftingJobSettings craftSettings))
                    {
                        var cSettings = Activator.CreateInstance(craftSettings.GetType()) as CraftingJobSettings;


                        cSettings.BlockTypes = craftSettings.BlockTypes;
                        cSettings.CraftingCooldown = craftSettings.CraftingCooldown;
                        cSettings.MaxCraftsPerHaul = craftSettings.MaxCraftsPerHaul;
                        cSettings.NPCType = craftSettings.NPCType;
                        cSettings.NPCTypeKey = craftSettings.NPCTypeKey;
                        cSettings.OnCraftedAudio = craftSettings.OnCraftedAudio;
                        cSettings.RecruitmentItem = craftSettings.RecruitmentItem;


                        craftingJob.Settings = cSettings;
                    }
                }

                data.Item1?.ApplyJobResearch();
            }
            catch (Exception ex)
            {
                APILogger.LogError(ex);
            }
        }

        public static void ApplyJobCooldownsToNPCs(Colony c)
        {
            foreach (var npc in c.Followers)
                npc.ApplyJobResearch();
        }
    }
}