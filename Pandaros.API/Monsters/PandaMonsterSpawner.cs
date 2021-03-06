﻿using Monsters;
using Pandaros.API.Entities;
using Pipliz;
using System.Collections.Generic;
using System.Linq;
using static AI.PathFinder;
using static AI.PathingManager;

namespace Pandaros.API.Monsters
{
    [ModLoader.ModManager]
    public class PandaMonsterSpawner : IPathingThreadAction
    {
        public static List<IPandaZombie> PandaZombies { get; set; } = new List<IPandaZombie>();
        private static Queue<IPandaZombie> _spawnQueue = new Queue<IPandaZombie>();
        private static PandaMonsterSpawner _pandaPathing = new PandaMonsterSpawner();
        private static double _updateTime = 0;
        public static Dictionary<Colony, long> MonsterCount { get; set; } = new Dictionary<Colony, long>();

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterSpawned, GameInitializer.NAMESPACE + ".Monsters.PandaMonsterSpawner.OnMonsterSpawned")]
        public static void OnMonsterSpawned(IMonster monster)
        {
            var cs = Entities.ColonyState.GetColonyState(monster.OriginalGoal);

            var fi = monster.CurrentHealth;

            float hpBonus = monster.OriginalGoal.FollowerCount * cs.Difficulty.MonsterHPPerColonist;

            if (MonsterManager.BossActive && MonsterManager.SpawnedBosses.TryGetValue(cs, out var boss) && boss != null && boss.ZombieHPBonus != 0)
                hpBonus += boss.ZombieHPBonus;

            monster.CurrentHealth = monster.CurrentHealth + hpBonus;
            monster.TotalHealth = monster.CurrentHealth + hpBonus;

            if (!MonsterCount.ContainsKey(monster.OriginalGoal))
                MonsterCount.Add(monster.OriginalGoal, 1);
            else
                MonsterCount[monster.OriginalGoal] = MonsterCount[monster.OriginalGoal] + 1;

        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterDied, GameInitializer.NAMESPACE + ".Monsters.PandaMonsterSpawner.MonsterDied")]
        public void MonsterDied(IMonster monster)
        {
            if (MonsterCount.ContainsKey(monster.OriginalGoal))
                MonsterCount[monster.OriginalGoal] = MonsterCount[monster.OriginalGoal] - 1;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameInitializer.NAMESPACE + ".Managers.MonsterManager.Update")]
        public static void OnUpdate()
        {
            if (!World.Initialized)
                return;

            while (_spawnQueue.Count > 0)
            {
                var pandaZombie = _spawnQueue.Dequeue();
                var cs = ColonyState.GetColonyState(pandaZombie.OriginalGoal);
                ModLoader.Callbacks.OnMonsterSpawned.Invoke(pandaZombie);
                MonsterTracker.Add(pandaZombie);
                cs.ColonyRef.OnZombieSpawn(true);
            }

            if (_updateTime < Time.SecondsSinceStartDouble)
            {
                ServerManager.PathingManager.QueueAction(_pandaPathing);
                _updateTime = Time.SecondsSinceStartDouble + Pipliz.Random.NextDouble(30, 45);
            }
        }

        public void PathingThreadAction(PathingContext context)
        {
            foreach (var colony in ServerManager.ColonyTracker.ColoniesByID.Values)
            {
                List<IPandaZombie> canSpawn = PandaZombies.Where(p => p.MinColonists < colony.FollowerCount).ToList();

                if (canSpawn.Count > 0 && colony.DifficultySetting.ShouldSpawnZombies(colony))
                {
                    var bannerGoal = colony?.Banners?.FirstOrDefault();

                    if (bannerGoal == null)
                        continue;

                    var cs = ColonyState.GetColonyState(colony);

                    if (cs.ColonyRef.OwnerIsOnline())
                    {
                        Vector3Int positionFinal;
                        var max = Math.RoundToInt(colony.FollowerCount / 100) + 1;

                        if (max == 0)
                            max = 1;

                        for (int i = 0; i < max; i++)
                        {
                            var zombie = canSpawn.GetRandomItem();

                            switch (((MonsterSpawner)MonsterTracker.MonsterSpawner).TryGetSpawnLocation(context, bannerGoal.Position, bannerGoal.SafeRadius, 200, 500f, out positionFinal))
                            {
                                case MonsterSpawner.ESpawnResult.Success:
                                    if (context.Pathing.TryFindPath(ref context, positionFinal, bannerGoal.Position, out var path, 2000000000) == EPathFindingResult.Success)
                                    {
                                        _spawnQueue.Enqueue(zombie.GetNewInstance(path, colony));
                                    }

                                    break;
                                case MonsterSpawner.ESpawnResult.NotLoaded:
                                case MonsterSpawner.ESpawnResult.Impossible:
                                    colony.OnZombieSpawn(true);
                                    break;
                                case MonsterSpawner.ESpawnResult.Fail:
                                    colony.OnZombieSpawn(false);
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
