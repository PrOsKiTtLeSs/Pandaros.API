﻿using Pandaros.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pandaros.API.Items
{
    public class LootTable : ILootTable
    {
        public virtual string name { get; set; }
        public virtual List<LootPoolEntry> LootPoolList { get; set; } = new List<LootPoolEntry>();
        public virtual List<string> MonsterTypes { get; set; } = new List<string>();

        public Dictionary<ushort, int> GetDrops(float luckModifier = 0)
        {
            var dic = new Dictionary<ushort, int>();

            float weightSum = 0;
            float roll = Pipliz.Random.NextFloat() + luckModifier;

            foreach (LootPoolEntry drop in LootPoolList)
            {
                weightSum += drop.Weight;

                if (roll > weightSum && ItemTypes.IndexLookup.StringLookupTable.TryGetItem(drop.Item, out ItemTypes.ItemType itemAction))
                {
                    dic[itemAction.GetRootParentType().ItemIndex] = Pipliz.Random.Next(drop.MinCount, drop.MaxCount + 1);
                }
            }

            return dic;
        }
    }
}
