﻿using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM.Network;
using OpenRPG.Systems;
using OpenRPG.Utils;
using Unity.Entities;
using VampireCommandFramework;

namespace OpenRPG.Commands {
    [CommandGroup("mastery", "m")]
    public static class MasteryCommands {
        private static EntityManager _entityManager = Plugin.Server.EntityManager;

        [Command("get", "g", "[masteryType]", "Display your current mastery progression for your equipped or specified weapon type")]
        public static void GetMastery(ChatCommandContext ctx, string weaponType = "") {
            if (!Plugin.WeaponMasterySystemActive) {
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;

            if (!Database.PlayerWeaponmastery.ContainsKey(steamID)) {
                ctx.Reply("You haven't even tried to master anything...");
                return;
            }

            WeaponMasterySystem.MasteryType type;
            if (string.IsNullOrEmpty(weaponType))
            {
                type = WeaponMasterySystem.WeaponToMasteryType(WeaponMasterySystem.GetWeaponType(ctx.Event.SenderCharacterEntity));
            }
            else if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out type))
            {
                ctx.Reply($"Mastery type not found! did you typo?");
                return;
            }
            
            ctx.Reply("-- <color=#ffffffff>Weapon Mastery</color> --");

            ctx.Reply(GetMasteryDataStringForType(steamID, type));
        }

        [Command("get-all", "ga", "", "Display your current mastery progression in everything")]
        public static void GetAllMastery(ChatCommandContext ctx) {
            if (!Plugin.WeaponMasterySystemActive) {
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;
            
            if (!Database.PlayerWeaponmastery.ContainsKey(steamID)) {
                ctx.Reply("You haven't even tried to master anything...");
                return;
            }

            ctx.Reply("-- <color=#ffffffff>Weapon Mastery</color> --");

            foreach (var type in Enum.GetValues<WeaponMasterySystem.MasteryType>())
            {
                ctx.Reply(GetMasteryDataStringForType(steamID, type));
            }
        }

        private static string GetMasteryDataStringForType(ulong steamID, WeaponMasterySystem.MasteryType type){
            var wd = Database.PlayerWeaponmastery[steamID];
            var wdType = wd[type]; 

            var name = Enum.GetName(type);
            var mastery = wdType.Mastery;
            var effectiveness = WeaponMasterySystem.EffectivenessSubSystemEnabled ? wdType.Effectiveness : 1;
            var growth = wdType.Growth;
            
            var statData = Database.MasteryStatConfig.GetValueOrDefault(type).Select(config =>
            {
                var val = Helper.CalcBuffValue(mastery, effectiveness, config.rate, config.type);
                
                // If the value was inverted for the buff calculation, un-invert it here.
                if (Helper.inverseMultipersDisplayReduction && Helper.inverseMultiplierStats.Contains(config.type))
                {
                    val = 1 - val;
                }
                
                if (Helper.percentageStats.Contains(config.type) && Helper.humanReadablePercentageStats) {
                    return $"{Helper.CamelCaseToSpaces(config.type)} <color=#75FF33>{val/100:F3}%</color>";
                }

                return $"{Helper.CamelCaseToSpaces(config.type)} <color=#75FF33>{val:F3}</color>";
            });

            return $"{name}:<color=#ffffff> {mastery:F2}%</color> ({string.Join(",", statData)}) Effectiveness: {effectiveness * 100}%, Growth: {growth * 100}%";
        }

        [Command("add", "a", "<weaponType> <amount>", "Adds the amount to the mastery of the specified weaponType", adminOnly: true)]
        public static void AddMastery(ChatCommandContext ctx, string weaponType, double amount){
            if (!Plugin.WeaponMasterySystemActive)
            {
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;
            var charName = ctx.Event.User.CharacterName.ToString();
            var userEntity = ctx.Event.SenderUserEntity;
            var charEntity = ctx.Event.SenderCharacterEntity;

            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                ctx.Reply($"Mastery type not found! did you typo?");
                return;
            }

            WeaponMasterySystem.ModMastery(steamID, masteryType, amount / WeaponMasterySystem.MasteryGainMultiplier);
            ctx.Reply($"{Enum.GetName(masteryType)} Mastery for \"{charName}\" adjusted by <color=#ffffff>{amount:F2}%</color>");
            Helper.ApplyBuff(userEntity, charEntity, Helper.AppliedBuff);
        }
        
        [Command("set", "s", "<playerName> <weaponType> <masteryValue>", "Sets the specified player's mastery to a specific value", adminOnly: true)]
        public static void SetMastery(ChatCommandContext ctx, string name, string weaponType, double value) {
            if (!Plugin.WeaponMasterySystemActive) {
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            ulong steamID;
            if (Helper.FindPlayer(name, false, out _, out var targetUserEntity)) {
                steamID = _entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
            } else {
                ctx.Reply($"Could not find specified player \"{name}\".");
                return;
            }

            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                ctx.Reply($"Mastery type not found! did you typo?");
                return;
            }

            WeaponMasterySystem.ModMastery(steamID, masteryType, -100000);
            WeaponMasterySystem.ModMastery(steamID, masteryType, value / WeaponMasterySystem.MasteryGainMultiplier);
            ctx.Reply($"{Enum.GetName(masteryType)} Mastery for \"{name}\" set to <color=#ffffff>{value:F2}%</color>");
        }

        [Command("log", "l", "", "Toggles logging of mastery gain.", adminOnly: false)]
        public static void LogMastery(ChatCommandContext ctx)
        {
            var steamID = ctx.User.PlatformId;
            var loggingData = Database.PlayerLogConfig[steamID];
            loggingData.LoggingMastery = !loggingData.LoggingMastery;
            ctx.Reply(loggingData.LoggingMastery
                ? "Mastery gain is now being logged."
                : $"Mastery gain is no longer being logged.");
            Database.PlayerLogConfig[steamID] = loggingData;
        }


        [Command("reset", "r", "<weaponType>", "Resets a mastery to gain more power with it.", adminOnly: false)]
        public static void ResetMastery(ChatCommandContext ctx, string weaponType)
        {
            if (!Plugin.WeaponMasterySystemActive){
                ctx.Reply("Weapon Mastery system is not enabled.");
                return;
            }
            var steamID = ctx.Event.User.PlatformId;
            if (!WeaponMasterySystem.KeywordToMasteryMap.TryGetValue(weaponType.ToLower(), out var masteryType)) {
                ctx.Reply($"Mastery type not found! did you typo?");
                return;
            }
            ctx.Reply($"Resetting {Enum.GetName(masteryType)} Mastery");
            WeaponMasterySystem.ResetMastery(steamID, masteryType);
        }
    }
}
