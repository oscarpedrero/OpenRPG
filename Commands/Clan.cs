using ProjectM.Network;
using OpenRPG.Systems;
using OpenRPG.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Entities;
using VampireCommandFramework;
using Bloodstone.API;
using static VCF.Core.Basics.RoleCommands;
using VRising.GameData.Models;
using VRising.GameData;
using OpenRPG.Exceptions;
using ProjectM;

namespace OpenRPG.Commands
{

    public static class Clan
    {
        [Command(name: "clantest", shortHand: "ct", adminOnly: false, usage: "", description: "Test Clan FUnction")]
        public static void ClanTestCommand(ChatCommandContext ctx)
        {
            UserModel userModel = GameData.Users.GetUserByCharacterName(ctx.User.CharacterName.ToString());
            var systemBase = VWorld.Server.GetExistingSystem<SystemBase>();
            var buff = BuffUtility.BuffSpawnerSystemData.Create(systemBase);
            DebugEventsSystem.UnlockVBloodFeatures(systemBase, buff, userModel.FromCharacter, DebugEventsSystem.VBloodFeatureType.Ability);

            try
            {
                string userEntityInfo = VWorld.Server.EntityManager.Debug.GetEntityInfo(userModel.Character.Entity);
                Plugin.Logger.LogInfo($"{userEntityInfo}");
                ClanSystem.ProcessClanXp(userModel, ctx.User.ClanEntity._Entity);
            } catch(NoClanMemberException e)
            {
                throw ctx.Error(e.Message);
            }

            ctx.Reply("Test Clan OK");
            
        }

        public static bool UpdateAutoRespawn(ulong SteamID, bool isAutoRespawn)
        {
            bool isExist = Database.autoRespawn.ContainsKey(SteamID);
            if (isExist || !isAutoRespawn) RemoveAutoRespawn(SteamID);
            else Database.autoRespawn.Add(SteamID, isAutoRespawn);
            return true;
        }

        public static void SaveAutoRespawn()
        {
            File.WriteAllText("BepInEx/config/OpenRPG/Saves/autorespawn.json", JsonSerializer.Serialize(Database.autoRespawn, Database.JSON_options));
        }

        public static bool RemoveAutoRespawn(ulong SteamID)
        {
            if (Database.autoRespawn.ContainsKey(SteamID))
            {
                Database.autoRespawn.Remove(SteamID);
                return true;
            }
            return false;
        }

        public static void LoadAutoRespawn()
        {
            if (!Directory.Exists(Plugin.ConfigPath)) Directory.CreateDirectory(Plugin.ConfigPath);
            if (!Directory.Exists(Plugin.SavesPath)) Directory.CreateDirectory(Plugin.SavesPath);

            if (!File.Exists(Plugin.AutorespawnJson))
            {
                var stream = File.Create(Plugin.AutorespawnJson);
                stream.Dispose();
            }
            string json = File.ReadAllText(Plugin.AutorespawnJson);
            try
            {
                Database.autoRespawn = JsonSerializer.Deserialize<Dictionary<ulong, bool>>(json);
                Plugin.Logger.LogWarning("AutoRespawn DB Populated.");
            }
            catch
            {
                Database.autoRespawn = new Dictionary<ulong, bool>();
                Plugin.Logger.LogWarning("AutoRespawn DB Created.");
            }
        }
    }
}
