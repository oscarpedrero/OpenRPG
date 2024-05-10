using OpenRPG.Configuration;
using OpenRPG.Systems;
using VampireCommandFramework;
using VRising.GameData;

namespace OpenRPG.Commands
{
    [CommandGroup("re")]
    internal class RandomEncountersCommands
    {
        [Command("start", usage: "", description: "Starts an encounter for a random online user.", adminOnly: true)]
        public static void StartCommand(ChatCommandContext ctx)
        {
            RandomEncountersSystem.StartEncounter();
            ctx.Reply("The hunt has begun...");
            return;
        }

        [Command("me", usage: "", description: "Starts an encounter for the admin who sends the command.", adminOnly: true)]
        public static void MeCommand(ChatCommandContext ctx)
        {
            var senderModel = GameData.Users.FromEntity(ctx.Event.SenderUserEntity);
            RandomEncountersSystem.StartEncounter(senderModel);
            ctx.Reply("Prepare for the fight...");
            return;
        }

        [Command("player", usage: "<PlayerName>", description: "Starts an encounter for the given player.", adminOnly: true)]
        public static void PlayerCommand(ChatCommandContext ctx, string PlayerName)
        {
            var senderModel = GameData.Users.GetUserByCharacterName(PlayerName);
            if(senderModel == null)
            {
                throw ctx.Error($"Player not found");
            }
            if(!senderModel.IsConnected)
            {
                throw ctx.Error($"Could not find an online player with name {PlayerName}");
            }
            RandomEncountersSystem.StartEncounter(senderModel);
            ctx.Reply($"Sending an ambush to {PlayerName}.");
        }

        [Command("enable", usage: "", description: "Enables the random encounter timer.", adminOnly: true)]
        public static void EnableCommand(ChatCommandContext ctx)
        {
            if (Plugin.RandomEncountersSystemActive)
            {
                throw ctx.Error("Already enabled.");
            }
            Plugin.RandomEncountersSystemActive = true;
            RandomEncounters.StartEncounterTimer();
            ctx.Reply($"Enabled");
        }

        [Command("disable", usage: "", description: "Disables the random encounter timer.", adminOnly: true)]
        public static void DisableCommand(ChatCommandContext ctx)
        {
            if (!Plugin.RandomEncountersSystemActive)
            {
                throw ctx.Error("Already disabled.");
            }
            Plugin.RandomEncountersSystemActive = false;
            RandomEncounters.EncounterTimer.Stop();
            ctx.Reply("Disabled.");
        }
    }
}
