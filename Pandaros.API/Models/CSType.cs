﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pipliz.JSON;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using static Pandaros.API.Items.StaticItems;

namespace Pandaros.API.Models
{
    public class CSType : ICSType
    {
        public virtual string name { get; set; }
        public virtual bool? isDestructible { get; set; }
        public virtual bool? isRotatable { get; set; }
        public virtual bool? isSolid { get; set; }
        public virtual bool? isFertile { get; set; }
        public virtual bool? isPlaceable { get; set; }
        public virtual bool? needsBase { get; set; }
        public virtual bool? requiresFertileBelow { get; set; }
        public virtual int? maxStackSize { get; set; }
        public virtual float? foodValue { get; set; }
        public virtual float? happiness { get; set; }
        public virtual float? dailyFoodFractionOptimal { get; set; }
        public virtual string mesh { get; set; }
        public virtual SerializableVector3 meshRotationEuler { get; set; }
        public virtual SerializableVector3 meshOffset { get; set; }
        public virtual SerializableVector3 meshScale { get; set; }
        public virtual float? luxuryHours { get; set; }
        public virtual string icon { get; set; }
        public virtual string onRemoveAudio { get; set; }
        public virtual string onPlaceAudio { get; set; }
        public virtual int? destructionTime { get; set; }
        public virtual JObject customData { get; set; }
        public virtual string parentType { get; set; }
        [JsonProperty("rotatablex+")]
        public virtual string rotatablexp { get; set; }
        [JsonProperty("rotatablex-")]
        public virtual string rotatablexn { get; set; }
        [JsonProperty("rotatablez+")]
        public virtual string rotatablezp { get; set; }
        [JsonProperty("rotatablez-")]
        public virtual string rotatablezn { get; set; }
        public virtual string sideall { get; set; }
        [JsonProperty("sidex+")]
        public virtual string sidexp { get; set; }
        [JsonProperty("sidex-")]
        public virtual string sidexn { get; set; }
        [JsonProperty("sidey+")]
        public virtual string sideyp { get; set; }
        [JsonProperty("sidey-")]
        public virtual string sideyn { get; set; }
        [JsonProperty("sidez+")]
        public virtual string sidezp { get; set; }
        [JsonProperty("sidez-")]
        public virtual string sidezn { get; set; }
        public virtual string color { get; set; } 
        public virtual string onRemoveType { get; set; }
        public virtual string onRemoveAmount { get; set; }
        public virtual string onRemoveChance { get; set; }
        public virtual List<OnRemove> onRemove { get; set; }
        public virtual bool? blocksPathing { get; set; }
        public virtual Colliders colliders { get; set; }
        public virtual List<string> categories { get; set; }
        public virtual ItemRarity Rarity { get; set; } = ItemRarity.Common;
        public virtual StaticItem StaticItemSettings { get; set; }
        public virtual OpenMenuSettings OpensMenuSettings { get; set; }
        public virtual ConnectedBlock ConnectedBlock { get; set; }
        public virtual TrainConfiguration TrainConfiguration { get; set; }
        public virtual TrainStationSettings TrainStationSettings { get; set; }
        public int lightingTransparency { get; set; } = 255;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
