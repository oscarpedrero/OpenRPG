using ProjectM.Network;
using OpenRPG.Systems;
using OpenRPG.Utils;
using System;
using Unity.Transforms;
using VampireCommandFramework;
using Bloodstone.API;
using Unity.Entities;

namespace OpenRPG.Commands
{
    public static class PlayerInfoCommands
    {
        private static EntityManager entityManager = VWorld.Server.EntityManager;

        [Command(name: "playerinfo", shortHand: "pi", adminOnly: false, usage: "[PlayerName]", description: "Display the player information details.")]
        public static void PlayerInfoCommand(ChatCommandContext ctx, string playerName = null)
        {
            var user = ctx.Event.User;
            var playerEntity = ctx.Event.SenderCharacterEntity;
            var userEntity = ctx.Event.SenderUserEntity;

            if (playerName != null)
            {
                if (!Helper.FindPlayer(playerName, false, out playerEntity, out userEntity))
                {
                    throw ctx.Error("Player not found.");
                }

                user = entityManager.GetComponentData<User>(userEntity);
            }
            
            var steamID = user.PlatformId;
            var name = user.CharacterName.ToString();
            var playerEntityString = $"{playerEntity.Index.ToString()}:{playerEntity.Version.ToString()}";
            var userEntityString = $"{userEntity.Index.ToString()}:{userEntity.Version.ToString()}";
            var ping = entityManager.GetComponentData<Latency>(playerEntity).Value;
            var position = entityManager.GetComponentData<Translation>(playerEntity).Value;

            ctx.Reply($"Name: {Utils.Color.White(name)}");
            ctx.Reply($"SteamID: {Utils.Color.White(steamID.ToString())}");
            ctx.Reply($"Latency: {Utils.Color.White(ping.ToString())}s");
            ctx.Reply($"Admin: {Utils.Color.White(user.IsAdmin.ToString())}s");
            ctx.Reply($"-- Position --");
            ctx.Reply($"X: {Utils.Color.White(Math.Round(position.x, 2).ToString())} " +
                      $"Y: {Utils.Color.White(Math.Round(position.y, 2).ToString())} " +
                      $"Z: {Utils.Color.White(Math.Round(position.z, 2).ToString())}");
            ctx.Reply($"-- {Utils.Color.White("Entities")} --");
            ctx.Reply($"Char Entity: {Utils.Color.White(playerEntityString)}");
            ctx.Reply($"User Entity: {Utils.Color.White(userEntityString)}");
            if (Plugin.ExperienceSystemActive)
            {
                var currentXp = ExperienceSystem.GetXp(steamID);
                var currentLevel = ExperienceSystem.ConvertXpToLevel(currentXp);
                ExperienceSystem.GetLevelAndProgress(currentXp, out var levelProgress, out var xpEarned, out var xpNeeded);
                ctx.Reply($"-- {Utils.Color.White("Experience")} --");
                ctx.Reply($"Level: {Utils.Color.White(currentLevel.ToString())} [{Utils.Color.White(xpEarned.ToString())}/{Utils.Color.White(xpNeeded.ToString())}]");
            }
        }
    }
}
