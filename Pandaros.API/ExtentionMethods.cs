﻿using AI;
using Jobs;
using Newtonsoft.Json;
using NPC;
using Pandaros.API.Items;
using Pandaros.API.Models;
using Pipliz;
using Pipliz.JSON;
using Recipes;
using Science;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Random = System.Random;

namespace Pandaros.API
{
    public static class ExtentionMethods
    {
        private static BlockSide[] _blockSides = (BlockSide[])Enum.GetValues(typeof(BlockSide));

        public static bool IsConnected(this Players.Player p)
        {
            return p.ConnectionState == Players.EConnectionState.Connected || p.ConnectionState == Players.EConnectionState.Connecting;
        }

        public static double NextDouble(this Random rng, double min, double max)
        {
            return rng.NextDouble() * (max - min) + min;
        }

        public static bool TakeItemFromInventory(this Players.Player player, ushort itemType)
        {
            var hasItem = false;
            var invRef = player.Inventory;

            if (invRef != null)
                invRef.TryRemove(itemType);

            return hasItem;
        }

        public static Vector3Int GetClosestPositionWithinY(this Vector3Int goalPosition, Vector3Int currentPosition, int minMaxY)
        {
            var pos = currentPosition;

            if (!PathingManager.TryCanStandNear(goalPosition, out var canStand, out pos) || !canStand)
            {
                var y    = -1;
                var negY = minMaxY * -1;

                while (PathingManager.TryCanStandNear(goalPosition.Add(0, y, 0), out var canStandNow, out pos) && !canStandNow)
                {
                    if (y > 0)
                    {
                        y++;

                        if (y > minMaxY)
                            break;
                    }
                    else
                    {
                        y--;

                        if (y < negY)
                            y = 1;
                    }
                }
            }

            return pos;
        }

        public static void Heal(this NPCBase nPC, float heal)
        {
            if (nPC != null)
            {
                nPC.health += heal;

                if (nPC.health > nPC.Colony.NPCHealthMax)
                    nPC.health = nPC.Colony.NPCHealthMax;
            }
        }

        public static void Heal(this Players.Player pc, float heal)
        {
            pc.Health += heal;

            if (pc.Health > pc.HealthMax)
                pc.Health = pc.HealthMax;

            pc.SendHealthPacket();
        }

        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException(string.Format("Argumnent {0} is not an Enum", typeof(T).FullName));

            var Arr = (T[]) Enum.GetValues(src.GetType());
            var j   = Array.IndexOf(Arr, src) + 1;
            return Arr.Length == j ? Arr[0] : Arr[j];
        }

        public static T CallAndReturn<T>(this object o, string methodName, params object[] args)
        {
            var retVal = default(T);
            var mi     = o.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (mi != null)
                retVal = (T) mi.Invoke(o, args);

            return retVal;
        }

        public static object Call(this object o, string methodName, params object[] args)
        {
            var mi = o.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null) return mi.Invoke(o, args);
            return null;
        }

        public static rT GetFieldValue<rT, oT>(this object o, string fieldName)
        {
            return (rT) typeof(oT).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
        }

        public static void SetFieldValue<oT>(this object o, string fieldName, object fieldValue)
        {
            typeof(oT).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, fieldValue);
        }

        public static byte[] ToUTF8SerializedJson(this object o)
        {
            string retstr = JsonConvert.SerializeObject(o);
            return Encoding.UTF8.GetBytes(retstr);
        }

        public static byte[] ToUTF8SerializedJson(this object o, params JsonConverter[] jsonConverters)
        {
            string retstr = JsonConvert.SerializeObject(o, jsonConverters);
            return Encoding.UTF8.GetBytes(retstr);
        }

        public static float TotalDamage(this Dictionary<DamageType, float> damage)
        {
            return damage.Sum(kvp => kvp.Value);
        }

        private static byte ToByte(float f)
        {
            f = UnityEngine.Mathf.Clamp01(f);
            return (byte)(f * 255);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }

        public static void ForEachOwner(this Colony source, Action<Players.Player> action)
        {
            foreach (var item in source.Owners)
                action(item);
        }

        public static bool OwnerIsOnline(this Colony source)
        {
            return source.Owners.Any(o => o.IsConnected());
        }

        public static T GetRandomItem<T>(this List<T> l)
        {
            return l[Pipliz.Random.Next(l.Count)];
        }

        public static bool TryGetItem(this Dictionary<string, ushort> itemInedex, string itemName, out ItemTypes.ItemType itemType)
        {
            itemType = null;

            if (itemInedex.TryGetValue(itemName, out ushort itemId) && ItemTypes.TryGetType(itemId, out itemType))
            {
                return true;
            }

            return false;
        }

        public static void Merge(this JSONNode oldNode, JSONNode newNode)
        {
            if (newNode.NodeType != NodeType.Array && oldNode.NodeType != NodeType.Array)
            {
                foreach (var node in newNode.LoopObject())
                {
                    if (oldNode.TryGetChild(node.Key, out JSONNode existingChild))
                        Merge(existingChild, node.Value);
                    else
                        oldNode.SetAs(node.Key, node.Value);
                }
            }
        }

        public static bool IsWithinBounds(this Vector3Int pos, Vector3Int boundsPos, BoundsInt bounds)
        {
            var boundsMax = boundsPos.Add(bounds.Size.x, bounds.Size.y, bounds.Size.z);

            return pos.x >= boundsPos.x && pos.y >= boundsPos.y && pos.z >= boundsPos.z &&
                    pos.x <= boundsMax.x && pos.y <= boundsMax.y && pos.z <= boundsMax.z;
        }

        public static JSONNode JsonSerialize<T>(this T obj)
        {
            var objStr = JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            APILogger.LogToFile(objStr);
            var json = JSON.DeserializeString(objStr);
            return json;
        }

        public static T JsonDeerialize<T>(this JSONNode node)
        {
            return JsonConvert.DeserializeObject<T>(node.ToString(), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(this IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });

            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);

            return type.GetField(name)
                .GetCustomAttributes(false)
                .OfType<TAttribute>()
                .SingleOrDefault();
        }

        public static UnityEngine.Vector3 GetVector(this BlockSide blockSide)
        {
            var vectorValues = blockSide.GetAttribute<BlockSideVectorValuesAttribute>();
            return new UnityEngine.Vector3(vectorValues.X, vectorValues.Y, vectorValues.Z);
        }

        public static Vector3Int GetBlockOffset(this Vector3Int vector, BlockSide blockSide)
        {
            var vectorValues = blockSide.GetAttribute<BlockSideVectorValuesAttribute>();

            if (vectorValues != null)
                return vector.Add(vectorValues.X, vectorValues.Y, vectorValues.Z);
            else
            {
                APILogger.Log(ChatColor.yellow, "Unable to find BlockSideVectorValuesAttribute for {0}", blockSide.ToString());
                return vector;
            }
        }

        public static bool ApproxEqual(this float x, float y, float epsilon = 0.5f)
        {
            return System.Math.Abs(x - y) < epsilon;
        }

        public static BlockSide GetBlocksideFromVector(this UnityEngine.Vector3 vector3)
        {
            foreach (var side in _blockSides)
            {
                var vectorValues = side.GetAttribute<BlockSideVectorValuesAttribute>();

                if (vector3.normalized.x.ApproxEqual(vectorValues.X) &&
                    vector3.normalized.y.ApproxEqual(vectorValues.Y) &&
                    vector3.normalized.z.ApproxEqual(vectorValues.Z))
                    return side;
            }

            return BlockSide.Invalid;
        }

        public static T RandomElement<T>(this IList<T> list)
        {
            return list[Pipliz.Random.Next(list.Count)];
        }

        public static T RandomElement<T>(this T[] array)
        {
            return array[Pipliz.Random.Next(array.Length)];
        }
    }
}
