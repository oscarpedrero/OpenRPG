﻿using ProjectM;
using System;
using System.Collections.Generic;
using OpenRPG.Models;
using OpenRPG.Systems;
using Unity.Collections;
using Unity.Entities;

namespace OpenRPG.Utils
{
    using WeaponMasteryData = LazyDictionary<WeaponMasterySystem.MasteryType, MasteryData>;
    using BloodlineMasteryData = LazyDictionary<BloodlineSystem.BloodType, MasteryData>;
    public static class Cache
    {
        //-- Cache (Wiped on plugin reload, server restart, and shutdown.)

        //-- -- Player Cache
        public static LazyDictionary<FixedString64, PlayerData> NamePlayerCache = new();
        public static LazyDictionary<ulong, PlayerData> SteamPlayerCache = new();
        public static LazyDictionary<ulong, List<BuffData>> buffData = new();
        
        //-- -- Combat
        public static LazyDictionary<ulong, DateTime> playerCombatStart = new();
        public static LazyDictionary<ulong, DateTime> playerCombatEnd = new();

        //-- -- Wanted System
        public static LazyDictionary<ulong, PlayerHeatData> heatCache = new();

        //-- -- Mastery System
        public static LazyDictionary<ulong, DateTime> player_last_combat = new();
        public static LazyDictionary<ulong, int> player_combat_ticks = new();

        //-- -- Experience System
        public static LazyDictionary<ulong, float> player_level = new();
        public static LazyDictionary<ulong, Dictionary<UnitStatType, float>> player_geartypedonned = new();
        
        //-- -- Alliance System
        public static LazyDictionary<Entity, Guid> AlliancePlayerToGroupId = new();
        public static LazyDictionary<Guid, Alliance.PlayerGroup> AlliancePlayerGroups = new();
        public static LazyDictionary<Entity, Alliance.PlayerGroup> AllianceAutoPlayerAllies = new();
        public static LazyDictionary<Entity, HashSet<AlliancePendingInvite>> AlliancePendingInvites = new();

        //-- -- CustomNPC Spawner
        public static SizedDictionaryAsync<float, SpawnNpcListen> spawnNPC_Listen = new(500);

        public static DateTime GetCombatStart(ulong steamID) {
            if (!playerCombatStart.TryGetValue(steamID, out var start)) {
                start = DateTime.MinValue;
            }

            return start;
        }
        public static DateTime GetCombatEnd(ulong steamID) {
            if (!playerCombatEnd.TryGetValue(steamID, out var start)) {
                start = DateTime.MinValue;
            }

            return start;
        }

        public static bool PlayerInCombat(ulong steamID)
        {
            return GetCombatStart(steamID) > GetCombatEnd(steamID);
        }
    }

    public static class Database
    {
        //-- Dynamic Database (Saved on a JSON file on plugin reload, server restart, and shutdown.)
        //-- Initialization for the data loading is on each command or related CS file.

        //-- -- Commands
        public static LazyDictionary<string, WaypointData> Waypoints = new();
        public static LazyDictionary<ulong, int> UserPermission = new();
        public static LazyDictionary<string, int> CommandPermission = new();
        public static LazyDictionary<ulong, PowerUpData> PowerUpList = new();

        //-- -- EXP System
        public static LazyDictionary<ulong, int> PlayerExperience = new();

        /// <summary>
        /// Ability points awarded per level.
        /// </summary>
        public static LazyDictionary<ulong, int> PlayerAbilityIncrease = new();

        /// <summary>
        /// Buff stat bonuses from leveling
        /// </summary>
        public static LazyDictionary<ulong, LazyDictionary<UnitStatType, float>> PlayerLevelStats = new();

        /// <summary>
        /// A configuration database of class stats per ability point spent.
        /// </summary>
        public static LazyDictionary<string, LazyDictionary<UnitStatType, float>> ExperienceClassStats = new();

        public static LazyDictionary<ulong, PlayerLog> PlayerLogConfig = new();

        public static LazyDictionary<ulong, DateTime> PlayerLogout = new();

        //-- -- Mastery System
        public static LazyDictionary<ulong, WeaponMasteryData> PlayerWeaponmastery = new();
        public static LazyDictionary<WeaponMasterySystem.MasteryType, List<StatConfig>> MasteryStatConfig = new();

        //-- -- Bloodline System
        public static LazyDictionary<ulong, BloodlineMasteryData> PlayerBloodline = new();
        public static LazyDictionary<BloodlineSystem.BloodType, List<StatConfig>> BloodlineStatConfig = new();
        
        //-- -- Alliance System
        public static LazyDictionary<Entity, AlliancePlayerPreferences> AlliancePlayerPrefs = new();
    }
}
