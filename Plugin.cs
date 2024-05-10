using System;
using BepInEx;
using VampireCommandFramework;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using OpenRPG.Commands;
using OpenRPG.Utils;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using VRising.GameData;
using OpenRPG.Configuration;
using OpenRPG.Components.RandomEncounters;
using OpenRPG.Systems;
using OpenRPG.Utils.Prefabs;
using ProjectM;

namespace OpenRPG
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.Bloodstone")]
    [BepInDependency("gg.deca.VampireCommandFramework")]
    public class Plugin : BasePlugin
    {
        public static Harmony harmony;

        internal static Plugin Instance { get; private set; }

        public static bool IsInitialized = false;
        public static bool BloodlineSystemActive = false;
        public static bool ExperienceSystemActive = true;
        public static bool PlayerGroupsActive = true;
        public static bool PowerUpCommandsActive = false;
        public static bool RandomEncountersSystemActive = false;
        public static bool WeaponMasterySystemActive = false;
        public static bool WantedSystemActive = true;
        public static bool WaypointsActive = false;
        
        private static bool _disableCommandAdminRequirement = false;

        private static ManualLogSource _logger;
        private static World _serverWorld;
        public static World Server
        {
            get
            {
                if (_serverWorld != null) return _serverWorld;

                _serverWorld = GetWorld("Server")
                    ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
                return _serverWorld;
            }
        }

        public static bool IsServer => Application.productName == "VRisingServer";

        private static World GetWorld(string name)
        {
            foreach (var world in World.s_AllWorlds)
            {
                if (world.Name == name)
                {
                    return world;
                }
            }

            return null;
        }

        public void InitCoreConfig()
        {
            Helper.buffGUID = Config.Bind("Core", "Buff GUID", (int)SetBonus.SetBonus_Damage_Minor_Buff_01, "The GUID of the buff that gets used when mastery, bloodline, etc changes.\nDefault is now boneguard set bonus 2, but you can set anything else too.\nThe only reason to change this is if it clashes with another mod.").Value;
            Helper.AppliedBuff = new PrefabGUID(Helper.buffGUID);
            Helper.forbiddenBuffGUID = Config.Bind("Core", "Forbidden Buff GUID", Helper.forbiddenBuffGUID, "The GUID of the buff that prohibits you from getting mastery buffs\nDefault is boneguard set bonus 1. If this is the same value as Buff GUID, then none will get buffs.\nThe only reason to change this is if it clashes with another mod.").Value;
            Helper.humanReadablePercentageStats = Config.Bind("Core", "Human Readable Percentage Stats", true, "Determines if rates for percentage stats should be read as out of 100 instead of 1.").Value;
            Helper.inverseMultipersDisplayReduction = Config.Bind("Core", "Inverse Multipliers Display Reduction", true, "Determines if inverse multiplier stats display their reduction, or the final value.").Value;
            
            _disableCommandAdminRequirement = Config.Bind("Admin", "Disable command admin requirement", false, "Disables all admin checks when running commands.").Value;

            BloodlineSystemActive = Config.Bind("System", "Enable Bloodline Mastery system", false,  "Enable/disable the bloodline mastery system.").Value;
            ExperienceSystemActive = Config.Bind("System", "Enable Experience system", true,  "Enable/disable the experience system.").Value;
            PlayerGroupsActive = Config.Bind("System", "Enable Player Groups", true,  "Enable/disable the player group system.").Value;
            // Disabling this for now as it needs more attention.
            //RandomEncountersSystemActive = Config.Bind("System", "Enable Random Encounters system", false,  "Enable/disable the random encounters system.").Value;
            WeaponMasterySystemActive = Config.Bind("System", "Enable Weapon Mastery system", false,  "Enable/disable the weapon mastery system.").Value;
            WantedSystemActive = Config.Bind("System", "Enable Wanted system", false,  "Enable/disable the wanted system.").Value;
            
            // I only want to keep waypoints around as it makes it easier to test.
            //WaypointsActive = Config.Bind("Core", "Enable Wanted system", false,  "Enable/disable waypoints.").Value;

            if (WaypointsActive)
            {
                WaypointCommands.WaypointLimit = Config.Bind("Config", "Waypoint Limit", 2, "Set a waypoint limit for per non-admin user.").Value;
            }
            
            AutoSaveSystem.AutoSaveFrequency = Config.Bind("Auto-save", "Frequency", 0, "Enable and set the frequency for auto-saving the database. 0 is disabled, 1 is every time the server saves, 2 is every second time, etc.").Value;
            AutoSaveSystem.BackupFrequency = Config.Bind("Auto-save", "Backup", 0, "Enable and set the frequency for saving to the backup folder. The backup save will run every X saves. 0 to disable.").Value;
        }

        public override void Load()
        {
            // Ensure the logger is accessible in static contexts.
            _logger = base.Log;
            if(!IsServer)
            {
                Plugin.Log(LogSystem.Core, LogLevel.Warning, $"This is a server plugin. Not continuing to load on client.", true);
                return;
            }
            
            InitCoreConfig();
            GameData.OnInitialize += GameDataOnInitialize;
            GameData.OnDestroy += GameDataOnDestroy;
            
            Instance = this;
            GameFrame.Initialize();
            
            // Load command registry for systems that are active
            // Note: Displaying these in alphabetical order for ease of maintenance
            if (PlayerGroupsActive) CommandRegistry.RegisterCommandType(typeof(AllianceCommands));
            if (BloodlineSystemActive) CommandRegistry.RegisterCommandType(typeof(BloodlineCommands));
            CommandRegistry.RegisterCommandType(typeof(CacheCommands));
            if (ExperienceSystemActive) CommandRegistry.RegisterCommandType(typeof(ExperienceSystem));
            if (WeaponMasterySystemActive) CommandRegistry.RegisterCommandType(typeof(MasteryCommands));
            CommandRegistry.RegisterCommandType(typeof(PermissionCommands));
            CommandRegistry.RegisterCommandType(typeof(PlayerInfoCommands));
            if (PowerUpCommandsActive) CommandRegistry.RegisterCommandType(typeof(PowerUpCommands)); // Currently unused.
            if (RandomEncountersSystemActive) CommandRegistry.RegisterCommandType(typeof(RandomEncountersCommands));
            if (WantedSystemActive) CommandRegistry.RegisterCommandType(typeof(WantedCommands));
            if (WaypointsActive) CommandRegistry.RegisterCommandType(typeof(WaypointCommands));
            
            
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Plugin.Log(LogSystem.Core, LogLevel.Info, $"Plugin is loaded [version: {MyPluginInfo.PLUGIN_VERSION}]", true);
        }

        private static void GameDataOnInitialize(World world)
        {
            if (RandomEncountersSystemActive)
            {
                RandomEncounters.GameData_OnInitialize();
                RandomEncounters.EncounterTimer = new Timer();
                RandomEncounters.StartEncounterTimer();
            }
            
            Initialize();
        }

        private static void GameDataOnDestroy()
        {
        }

        public override bool Unload()
        {
            Config.Clear();
            harmony.UnpatchSelf();
            return true;
        }

        public static void Initialize()
        {
            Plugin.Log(LogSystem.Core, LogLevel.Warning, $"Trying to Initialize {MyPluginInfo.PLUGIN_NAME}: isInitialized == {IsInitialized}", IsInitialized);
            if (IsInitialized) return;
            Plugin.Log(LogSystem.Core, LogLevel.Info, $"Initializing {MyPluginInfo.PLUGIN_NAME}...", true);
            
            //-- Initialize System
            // Pre-initialise some constants
            Helper.GetServerGameSettings(out _);
            Helper.GetServerGameManager(out _);
            Helper.GetUserActivityGridSystem(out _);
            
            DebugLoggingConfig.Initialize();
            if (BloodlineSystemActive) BloodlineConfig.Initialize();
            if (ExperienceSystemActive) ExperienceConfig.Initialize();
            if (WeaponMasterySystemActive) MasteryConfig.Initialize();
            if (WantedSystemActive) WantedConfig.Initialize();

            //-- Apply configs
            
            Plugin.Log(LogSystem.Core, LogLevel.Info, "Initialising player cache and internal database...");
            Helper.CreatePlayerCache();
            AutoSaveSystem.LoadOrInitialiseDatabase();
            
            // Validate any potential change in permissions
            var commands = Command.GetAllCommands();
            Command.ValidatedCommandPermissions(commands);
            // Note for devs: To regenerate Command.md and PermissionSystem.DefaultCommandPermissions, uncomment the following:
            // TODO re-comment out this.
            Command.GenerateCommandMd(commands);
            Command.GenerateDefaultCommandPermissions(commands);
            
            Plugin.Log(LogSystem.Core, LogLevel.Info, $"Setting CommandRegistry middleware");
            if (_disableCommandAdminRequirement)
            {
                Plugin.Log(LogSystem.Core, LogLevel.Info, "Removing admin privilege requirements");
                CommandRegistry.Middlewares.Clear();                
            }
            CommandRegistry.Middlewares.Add(new Command.PermissionMiddleware());

            Plugin.Log(LogSystem.Core, LogLevel.Info, "Finished initialising", true);

            IsInitialized = true;
        }

        public enum LogSystem
        {
            Alliance,
            Bloodline,
            Buff,
            Core,
            Death,
            Faction,
            Mastery,
            PowerUp,
            RandomEncounter,
            SquadSpawn,
            Wanted,
            Xp
        }
        
        public new static void Log(LogSystem system, LogLevel logLevel, string message, bool forceLog = false)
        {
            var isLogging = forceLog || DebugLoggingConfig.IsLogging(system);
            if (isLogging) _logger.Log(logLevel, $"{DateTime.Now.ToString("u")}: [{Enum.GetName(system)}] {message}");
        }
        
        // Log overload to allow potentially more computationally expensive logs to be hidden when not being logged
        public new static void Log(LogSystem system, LogLevel logLevel, Func<string> messageGenerator)
        {
            Log(system, logLevel, messageGenerator());
        }
    }
}
