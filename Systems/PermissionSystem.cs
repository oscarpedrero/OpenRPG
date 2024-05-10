using System;
using ProjectM;
using ProjectM.Network;
using OpenRPG.Utils;
using System.Collections.Generic;
using System.Linq;
using OpenRPG.Models;
using OpenRPG.Utils.Prefabs;
using Unity.Entities;
using VampireCommandFramework;

namespace OpenRPG.Systems
{
    public static class PermissionSystem
    {
        private static EntityManager em = Plugin.Server.EntityManager;

        public static int HighestPrivilege = 100;
        public static int LowestPrivilege = 0;

        public static int GetUserPermission(ulong steamID)
        {
            return Database.UserPermission.GetValueOrDefault(steamID, LowestPrivilege);
        }

        public static int GetCommandPermission(string command)
        {
            return Database.CommandPermission.GetValueOrDefault(command, HighestPrivilege);
        }

        private static object SendPermissionList(ChatCommandContext ctx, List<string> messages)
        {
            foreach(var m in messages)
            {
                ctx.Reply(m);
            }
            return new object();
        }

        public static void UserPermissionList(ChatCommandContext ctx)
        {
            var sortedPermission = Database.UserPermission.ToList();
            // Sort by privilege descending
            sortedPermission.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            ctx.Reply($"===================================");
            if (sortedPermission.Count == 0) ctx.Reply($"<color=#ffffff>No permissions</color>");
            else
            {
                foreach (var (item, index) in sortedPermission.Select((item, index) => (item, index)))
                {
                    ctx.Reply($"{index}. <color=#ffffff>{Helper.GetNameFromSteamID(item.Key)} : {item.Value}</color>");
                }
            }
            ctx.Reply($"===================================");
        }
        
        public static void CommandPermissionList(ChatCommandContext ctx)
        {
            var sortedPermission = Database.CommandPermission.ToList();
            // Sort by command name
            sortedPermission.Sort((pair1, pair2) => String.Compare(pair1.Key, pair2.Key, StringComparison.CurrentCultureIgnoreCase));
            ctx.Reply($"===================================");
            if (sortedPermission.Count == 0) ctx.Reply($"<color=#ffffff>No commands</color>");
            else
            {
                foreach (var (item, index) in sortedPermission.Select((item, index) => (item, index)))
                {
                    ctx.Reply($"{index}. <color=#ffffff>{item.Key} : {item.Value}</color>");
                }
            }
            ctx.Reply($"===================================");
        }

        public static LazyDictionary<string, int> DefaultCommandPermissions()
        {
            var permissions = new LazyDictionary<string, int>()
            {
                {"ban info", 0},
                {"ban player", 100},
                {"ban unban", 100},
                {"bloodline add", 100},
                {"bloodline get", 0},
                {"bloodline get-all", 0},
                {"bloodline log", 0},
                {"bloodline reset", 0},
                {"bloodline set", 100},
                {"experience ability", 0},
                {"experience ability reset", 50},
                {"experience ability show", 0},
                {"experience get", 0},
                {"experience log", 0},
                {"experience set", 100},
                {"godmode", 100},
                {"kick", 100},
                {"kit", 100},
                {"load", 100},
                {"mastery add", 100},
                {"mastery get", 0},
                {"mastery get-all", 0},
                {"mastery log", 0},
                {"mastery reset", 0},
                {"mastery set", 100},
                {"nocooldown", 100},
                {"permission", 100},
                {"permission set command", 100},
                {"permission set user", 100},
                {"playerinfo", 0},
                {"powerdown", 100},
                {"powerup", 100},
                {"re disable", 100},
                {"re enable", 100},
                {"re me", 100},
                {"re player", 100},
                {"re start", 100},
                {"save", 100},
                {"speed", 100},
                {"sunimmunity", 100},
                {"unlock achievements", 100},
                {"unlock research", 100},
                {"unlock vbloodability", 100},
                {"unlock vbloodpassive", 100},
                {"unlock vbloodshapeshift", 100},
                {"wanted fixminions", 100},
                {"wanted get", 0},
                {"wanted log", 0},
                {"wanted set", 100},
                {"wanted trigger", 100},
                {"waypoint go", 100},
                {"waypoint list", 0},
                {"waypoint remove", 100},
                {"waypoint remove global", 100},
                {"waypoint set", 100},
                {"waypoint set global", 100},
                {"worlddynamics ignore", 100},
                {"worlddynamics info", 0},
                {"worlddynamics load", 100},
                {"worlddynamics save", 100},
                {"worlddynamics unignore", 100}
            };
            return permissions;
        }
    }
}
