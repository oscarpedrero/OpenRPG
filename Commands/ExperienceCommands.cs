using System;
using System.Linq;
using BepInEx.Logging;
using OpenRPG.Models;
using ProjectM.Network;
using OpenRPG.Systems;
using OpenRPG.Utils;
using Unity.Entities;
using VampireCommandFramework;
using LogSystem = OpenRPG.Plugin.LogSystem;

namespace OpenRPG.Commands
{

    [CommandGroup("experience","xp")]
    public static class ExperienceCommands {
        private static EntityManager _entityManager = Plugin.Server.EntityManager;

        [Command("get", "g", "", "Display your current xp", adminOnly: false)]
        public static void GetXp(ChatCommandContext ctx) {
            if (!Plugin.ExperienceSystemActive)
            {
                ctx.Reply("Experience system is not enabled.");
                return;
            }
            var user = ctx.Event.User;
            var characterName = user.CharacterName.ToString();
            var steamID = user.PlatformId;
            int userXp = ExperienceSystem.GetXp(steamID);
            ExperienceSystem.GetLevelAndProgress(userXp, out int progress, out int earnedXp, out int neededXp);
            int userLevel = ExperienceSystem.ConvertXpToLevel(userXp);
            string response = "-- <color=#ffffff>" + characterName + "</color> --\n";
            response += $"Level:<color=#ffffff> {userLevel}</color> (<color=#ffffff>{progress}%</color>) ";
            response += $" [ XP:<color=#ffffff> {earnedXp}</color>/<color=#ffffff>{neededXp}</color> ]";
            if (ExperienceSystem.LevelRewardsOn) response += $" You have {(Database.PlayerAbilityIncrease.ContainsKey(steamID) ? Database.PlayerAbilityIncrease[steamID] : 0)} ability points to spend.";
            ctx.Reply(response);
        }

        [Command("set", "s", "<playerName> <XP>", "Sets the specified player's current xp to the given value", adminOnly: true)]
        public static void SetXp(ChatCommandContext ctx, string name, int xp){
            if (!Plugin.ExperienceSystemActive){
                ctx.Reply("Experience system is not enabled.");
                return;
            }
            ulong steamID;

            if (Helper.FindPlayer(name, true, out var targetEntity, out var targetUserEntity)){
                steamID = _entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
            }
            else
            {
                ctx.Reply($"Could not find specified player \"{name}\".");
                return;
            }
            Database.PlayerExperience[steamID] = xp;
            ExperienceSystem.SetLevel(targetEntity, targetUserEntity, steamID);
            ctx.Reply($"Player \"{name}\" Experience is now set to be<color=#ffffff> {ExperienceSystem.GetXp(steamID)}</color>");
        }

        [Command("log", "l", "", "Toggles logging of xp gain.", adminOnly: false)]
        public static void LogExperience(ChatCommandContext ctx){
            if (!Plugin.ExperienceSystemActive){
                ctx.Reply("Experience system is not enabled.");
                return;
            }
            var steamID = ctx.User.PlatformId;
            var loggingData = Database.PlayerLogConfig[steamID];
            loggingData.LoggingExp = !loggingData.LoggingExp;
            ctx.Reply(loggingData.LoggingExp
                ? "Experience gain is now being logged."
                : $"Experience gain is no longer being logged.");
            Database.PlayerLogConfig[steamID] = loggingData;
        }

        [Command("ability", "a", "<AbilityName> <amount>", "Spend given points on given ability", adminOnly: false)]
        public static void AddClassAbility(ChatCommandContext ctx, string name, int amount){
            if (!Plugin.ExperienceSystemActive){
                ctx.Reply("Experience system is not enabled.");
                return;
            }
            if (!ExperienceSystem.LevelRewardsOn){
                ctx.Reply("Experience Class system is not enabled.");
                return;
            }
            
            ulong steamID = ctx.Event.User.PlatformId;
            var userEntity = ctx.Event.SenderUserEntity;
            var playerCharacter = ctx.Event.SenderCharacterEntity;
            
            try{
                int spendPoints = amount;
                string abilityName = name;

                if (!Database.PlayerAbilityIncrease.ContainsKey(steamID)) Database.PlayerAbilityIncrease[steamID] = 0;

                if (Database.PlayerAbilityIncrease[steamID] < spendPoints){
                    ctx.Reply("Not enough points!");
                    return;
                }

                if (Database.ExperienceClassStats.ContainsKey(abilityName.ToLower())){
                    foreach (var buff in Database.ExperienceClassStats[abilityName.ToLower()]){
                        Database.PlayerLevelStats[steamID][buff.Key] += buff.Value * spendPoints;
                    }

                    Database.PlayerAbilityIncrease[steamID] -= spendPoints;
                    Helper.ApplyBuff(userEntity, playerCharacter, Helper.AppliedBuff);
                    ctx.Reply($"Spent {spendPoints}. You have {Database.PlayerAbilityIncrease[steamID]} points left to spend.");
                    foreach (var buff in Database.PlayerLevelStats[steamID]){
                        ctx.Reply($"{buff.Key} : {buff.Value}");
                    }
                }
                else {
                    ctx.Reply("Type \".xp ability show\" to see current buffs.");
                    ctx.Reply($"Type .xp ability <ability> to spend ability points. You have {Database.PlayerAbilityIncrease[steamID]} points left to spend.");
                    ctx.Reply("You can spend ability points on:");
                    ctx.Reply(string.Join(", ", Database.ExperienceClassStats.Keys.ToList()));
                }

            }
            catch (Exception ex){
                Plugin.Log(LogSystem.Xp, LogLevel.Error, $"Could not spend point! {ex}");
                ctx.Reply($"Could not spend point! {ex.Message}");
            }
        }
        
        [Command("ability show", "as", "", "Display the buffs provided by the XP class system", adminOnly: false)]
        public static void ShowClassAbility(ChatCommandContext ctx){
            if (!Plugin.ExperienceSystemActive){
                ctx.Reply("Experience system is not enabled.");
                return;
            }
            if (!ExperienceSystem.LevelRewardsOn){
                ctx.Reply("Experience Class system is not enabled.");
                return;
            }
            
            var steamID = ctx.User.PlatformId;
            
            foreach (var buff in Database.PlayerLevelStats[steamID]){
                ctx.Reply($"{buff.Key} : {buff.Value}");
            }
        }
        
        [Command("ability reset", "ar", "", "Reset your spent ability points", adminOnly: false)]
        public static void ResetClassAbility(ChatCommandContext ctx){
            if (!Plugin.ExperienceSystemActive){
                ctx.Reply("Experience system is not enabled.");
                return;
            }
            if (!ExperienceSystem.LevelRewardsOn){
                ctx.Reply("Experience Class system is not enabled.");
                return;
            }
            
            var steamID = ctx.User.PlatformId;
            var userEntity = ctx.Event.SenderUserEntity;
            var playerCharacter = ctx.Event.SenderCharacterEntity;
            
            Database.PlayerLevelStats[steamID] = new LazyDictionary<ProjectM.UnitStatType, float>();
            Database.PlayerAbilityIncrease[steamID] = 0;
            Cache.player_level[steamID] = 0;
            ExperienceSystem.SetLevel(playerCharacter, userEntity, steamID);
            ctx.Reply("Ability level up points reset.");
        }
    }
}
