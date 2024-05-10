﻿using OpenRPG.Components.RandomEncounters;
using OpenRPG.Configuration;
using OpenRPG.Systems;
using OpenRPG.Utils.RandomEncounters;
using System;
using BepInEx.Logging;

namespace OpenRPG
{
    internal static class RandomEncounters
    {
        private const Plugin.LogSystem LogSystem = Plugin.LogSystem.RandomEncounter;

        public static Timer EncounterTimer;

        public static void Load()
        {;
        }

        internal static void GameData_OnInitialize()
        {
            Plugin.Log(LogSystem, LogLevel.Info, "Loading main data RandomEncounters");
            DataFactory.Initialize();
            Plugin.Log(LogSystem, LogLevel.Info, "Binding configuration RandomEncounters");
            RandomEncountersConfig.Initialize();
        }

        public static void StartEncounterTimer()
        {
            EncounterTimer.Start(
                _ =>
                {
                    Plugin.Log(LogSystem, LogLevel.Info, $"Starting an encounter.");
                    RandomEncountersSystem.StartEncounter();
                },
                input =>
                {
                    if (input is not int onlineUsersCount)
                    {
                        Plugin.Log(LogSystem, LogLevel.Error, "Encounter timer delay function parameter is not a valid integer");
                        return TimeSpan.MaxValue;
                    }
                    if (onlineUsersCount < 1)
                    {
                        onlineUsersCount = 1;
                    }
                    var seconds = new Random().Next(RandomEncountersConfig.EncounterTimerMin.Value, RandomEncountersConfig.EncounterTimerMax.Value);
                    Plugin.Log(LogSystem, LogLevel.Info, $"Next encounter will start in {seconds / onlineUsersCount} seconds.");
                    return TimeSpan.FromSeconds(seconds) / onlineUsersCount;
                });
        }

        public static void Unload()
        {
            EncounterTimer?.Stop();
            GameFrame.Uninitialize();
            Plugin.Log(LogSystem, LogLevel.Info, $"RandomEncounters unloaded!");
        }
    }
}
