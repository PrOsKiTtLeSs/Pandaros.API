﻿using Jobs;
using NPC;
using Pandaros.API.Models;
using Pipliz;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pandaros.API.Jobs.Roaming
{
    [ModLoader.ModManager]
    public static class RoamingJobRegister
    {
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCDied, GameInitializer.NAMESPACE + ".Jobs.Roaming.RoamingJobRegister.OnDeath")]
        public static void OnDeath(NPCBase nPC)
        {
            if (nPC.Job != null && nPC.Job.GetType() == typeof(RoamingJob) && ((RoamingJob)nPC.Job).TargetObjective != null)
                ((RoamingJob)nPC.Job).TargetObjective.JobRef = null;
        }
    }

    public abstract class RoamingJob : BlockJobInstance
    {
        protected float cooldown = 2f;
        private const float COOLDOWN = 3f;
        private int _stuckCount;
        

        public RoamingJob(IBlockJobSettings settings, Vector3Int position, ItemTypes.ItemType type, ByteReader reader) :
            base(settings, position, type, reader)
        {
            OriginalPosition = position;
        }

        public RoamingJob(IBlockJobSettings settings, Vector3Int position, ItemTypes.ItemType type, Colony colony) :
            base(settings, position, type, colony)
        {
            OriginalPosition = position;
        }

        public virtual List<ItemId> OkStatus { get; } = new List<ItemId>();
        public RoamingJobState TargetObjective { get; set; }
        public RoamingJobState PreviousObjective { get; set; }
        public Vector3Int OriginalPosition { get; private set; }
        public virtual string JobItemKey => null;
        public virtual List<string> ObjectiveCategories => new List<string>();
        public virtual int ActionsPreformed { get; set; }

        public override void OnNPCCouldNotPathToGoal()
        {
            NPC.SetPosition(OriginalPosition);
            NPC.SendPositionUpdate();
        }

        public override Vector3Int GetJobLocation()
        {
            var pos = OriginalPosition;

            if (NPC != null)
                try
                {
                    if (TargetObjective == null)
                    {
                        RoamingJobState closest = null;
                        float distance = float.MaxValue;

                        foreach (var cat in ObjectiveCategories)
                            if (RoamingJobManager.Objectives.TryGetValue(Owner, out var roamingJobObjectiveDic) && 
                                roamingJobObjectiveDic.TryGetValue(cat, out var states))
                                foreach (var objective in states.Values)
                                    if (objective != null && objective != PreviousObjective && objective.PositionIsValid() && objective.JobRef == null)
                                    {
                                        var dis = UnityEngine.Vector3.Distance(objective.Position.Vector, pos.Vector);

                                        if (dis < distance && dis <= objective.RoamingJobSettings.WatchArea)
                                        {
                                            string actionName = string.Empty;

                                            foreach (var actionKvp in objective.ActionEnergy)
                                                if (objective.RoamingJobSettings.ActionCallbacks.TryGetValue(actionKvp.Key, out var objectiveAction) &&
                                                    actionKvp.Value < objectiveAction.ActionEnergyMinForFix)
                                                {
                                                    actionName = actionKvp.Key;
                                                    break;
                                                }

                                            if (!string.IsNullOrEmpty(actionName) && objective.RoamingJobSettings.ActionCallbacks.ContainsKey(actionName))
                                            {
                                                closest = objective;
                                                distance = dis;
                                            }
                                        }
                                    }

                        if (closest != null)
                        {
                            TargetObjective = closest;
                            TargetObjective.JobRef = this;

                            pos = TargetObjective.Position.GetClosestPositionWithinY(NPC.Position, 5);
                        }
                    }
                    else
                    {
                        pos = TargetObjective.Position.GetClosestPositionWithinY(NPC.Position, 5);
                    }
                }
                catch (Exception ex)
                {
                    APILogger.LogError(ex);
                }

            return pos;
        }

        public override void OnNPCAtJob(ref NPCBase.NPCState state)
        {
            var status        = ItemId.GetItemId(GameInitializer.NAMESPACE + ".Waiting");
            var cooldown      = COOLDOWN;
            var allActionsComplete = false;
            bool actionFound = false;

            try
            {
                if (TargetObjective != null && NPC != null)
                {
                    NPC.LookAt(TargetObjective.Position.Vector);

                    foreach (var action in new Dictionary<string, float>(TargetObjective.ActionEnergy))
                        if (action.Value < .5f)
                        {
                            if (TargetObjective.RoamingJobSettings.ActionCallbacks.TryGetValue(action.Key, out var roamingJobObjective))
                            {
                                status = roamingJobObjective.PreformAction(Owner, TargetObjective);
                                cooldown = roamingJobObjective.TimeToPreformAction;
                                AudioManager.SendAudio(TargetObjective.Position.Vector, roamingJobObjective.AudioKey);
                                actionFound = true;
                            }
                        }

                    if (!actionFound)
                    {
                        PreviousObjective = null;
                        TargetObjective.JobRef = null;
                        TargetObjective = null;
                        allActionsComplete = true;
                    }
                }

                // if the objective is gone, Abort.
                CheckIfValidObjective();

                if (OkStatus.Contains(status))
                {
                    if (actionFound)
                        ActionsPreformed++;

                    _stuckCount = 0;
                }
                else if (status != 0)
                    _stuckCount++;

                if (_stuckCount > 5 || TargetObjective == null)
                {
                    state.JobIsDone = true;
                    status = ItemId.GetItemId(GameInitializer.NAMESPACE + ".Waiting");

                    if (allActionsComplete)
                        cooldown = 0.5f;

                    if (_stuckCount > 5)
                    {
                        PreviousObjective = TargetObjective;
                        TargetObjective.JobRef = null;
                        TargetObjective = null;
                    }
                }

                if (OkStatus.Contains(status))
                    state.SetIndicator(new IndicatorState(cooldown, status.Id));
                else if (status != 0)
                    state.SetIndicator(new IndicatorState(cooldown, status.Id, true));
                else
                    state.SetIndicator(new IndicatorState(cooldown, ColonyBuiltIn.ItemTypes.MISSINGERROR.Name));
            }
            catch (Exception ex)
            {
                APILogger.LogError(ex);
            }

            state.SetCooldown(cooldown);
        }

        private void CheckIfValidObjective()
        {
            if (TargetObjective != null && !TargetObjective.PositionIsValid())
            {
                TargetObjective.JobRef = null;
                TargetObjective           = null;
            }
        }
    }
}