using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using OpenRPG.Configuration;
using OpenRPG.Models.RandomEncounters;
using OpenRPG.Properties;
using VRising.GameData;
using VRising.GameData.Models;
using LogSystem = OpenRPG.Plugin.LogSystem;

namespace OpenRPG.Utils.RandomEncounters
{
    internal static class DataFactory
    {
        private static readonly Random Random = new();
        private static List<NpcDataModel> _npcs;
        private static List<ItemDataModel> _items;

        internal static void Initialize()
        {
            var tsv = Encoding.UTF8.GetString(Resources.npcs);
            _npcs = tsv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Select(l => new NpcDataModel(l)).Where(n => n.NpcModel != null && n.NpcModel.HasDropTable && !n.PrefabName.Contains("summon", StringComparison.OrdinalIgnoreCase)).ToList();
            tsv = Encoding.UTF8.GetString(Resources.items);
            _items = tsv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Select(l => new ItemDataModel(l)).ToList();
        }

        internal static NpcDataModel GetRandomNpc(float playerLevel)
        {
            var lowestLevel = playerLevel - RandomEncountersConfig.EncounterMaxLevelDifferenceLower.Value;
            var highestLevel = playerLevel + RandomEncountersConfig.EncounterMaxLevelDifferenceUpper.Value;
            Plugin.Log(LogSystem.RandomEncounter, LogLevel.Info, $"Searching an NPC between levels {lowestLevel} and {highestLevel}");
            return _npcs
                .Where(n => RandomEncountersConfig.Npcs.TryGetValue(n.Id, out var npcSetting) && npcSetting.Value && n.Level >= lowestLevel && n.Level <= highestLevel).ToList()
                .GetRandomItem();
        }

        internal static ItemDataModel GetRandomItem()
        {
            return _items
                .Where(n => RandomEncountersConfig.Items.TryGetValue(n.Id, out var itemSetting) && itemSetting.Value > 0).ToList()
                .GetRandomItem();
        }

        internal static int GetOnlineUsersCount()
        {
            return GameData.Users.Online.Count();
        }

        internal static List<ItemDataModel> GetAllItems()
        {
            return _items;
        }

        internal static List<NpcDataModel> GetAllNpcs()
        {
            return _npcs;
        }

        internal static List<UserModel> GetOnlineAdmins()
        {
            return GameData.Users.Online.Where(u => u.IsAdmin).ToList();
        }

        private static T GetRandomItem<T>(this List<T> items)
        {
            if (items == null || items.Count == 0)
            {
                return default;
            }

            return items[Random.Next(items.Count)];
        }
    }
}