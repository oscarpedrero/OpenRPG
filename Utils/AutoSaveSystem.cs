﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BepInEx;
using BepInEx.Logging;
using OpenRPG.Systems;
using ProjectM;
using LogSystem = OpenRPG.Plugin.LogSystem;

namespace OpenRPG.Utils
{
    //-- AutoSave is now directly hooked into the Server game save activity.
    public static class AutoSaveSystem
    {
        // Config paths
        public static readonly string BasePath = Paths.ConfigPath ?? Path.Combine("BepInEx", "config");
        public static readonly string ConfigPath = Path.Combine(BasePath, "OpenRPG");
        public static readonly string SavesPath = Path.Combine(ConfigPath, "Data");
        public static readonly string BackupsPath = Path.Combine(SavesPath, "Backup");
        
        // Config files
        private const string PowerUpJson = "powerUp.json";
        private const string WaypointsJson = "waypoints.json";
        private const string WeaponMasteryJson = "weaponMastery.json";
        private const string PlayerLogoutJson = "playerLogout.json";
        private const string WeaponMasteryConfigJson = "weaponMasteryConfig.json";
        private const string PlayerLogConfigJson = "playerLogConfig.json";
        private const string BloodlinesJson = "bloodlines.json";
        private const string BloodlineConfigJson = "bloodlineConfig.json";
        private const string PlayerExperienceJson = "playerExperience.json";
        private const string PlayerAbilityPointsJson = "playerAbilityPoints.json";
        private const string PlayerLevelStatsJson = "playerLevelStats.json";
        private const string ExperienceClassStatsJson = "experienceClassStats.json";
        private const string CommandPermissionJson = "commandPermission.json";
        private const string UserPermissionJson = "userPermission.json";
        private const string AlliancePreferencesJson = "alliancePreferences.json";

        private static int _saveCount = 0;
        private static int _autoSaveCount = 0;
        public static int AutoSaveFrequency = 1;
        public static int BackupFrequency = 0;
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            IncludeFields = true,
            Converters =
            {
                new PrefabGuidConverter()
            }
        };
        private static readonly JsonSerializerOptions PrettyJsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true,
            Converters =
            {
                new PrefabGuidConverter()
            }
        };

        private enum LoadMethod
        {
            Both,
            Main,
            Backup,
            None,
        }

        // Returns true on all succeeding, false on any errors
        public static bool SaveDatabase(bool forceSave, bool forceBackup = false)
        {
            var anyErrors = false;
            if (forceSave)
            {
                anyErrors |= !InternalSaveDatabase(SavesPath);
                if (forceBackup) anyErrors |= !InternalSaveDatabase(BackupsPath);
            }
            else
            {
                var autoSave = _saveCount % AutoSaveFrequency == 0;
                if (autoSave)
                {
                    anyErrors |= !InternalSaveDatabase(SavesPath);
                    var saveBackup = _autoSaveCount % BackupFrequency == 0;
                    if (forceBackup || saveBackup) anyErrors |= !InternalSaveDatabase(BackupsPath);
                    
                    // Just ensure that it wraps around. No need to support ludicrously high save count numbers
                    _autoSaveCount = (_autoSaveCount + 1) % 100;
                }
                
                // Just ensure that it wraps around. No need to support ludicrously high save count numbers
                _saveCount = (_saveCount + 1) % 100;
            }

            return !anyErrors;
        }
        
        // Returns true on all succeeding, false on any errors
        private static bool InternalSaveDatabase(string saveFolder)
        {
            var anyErrors = false;
            // Core
            anyErrors |= !SaveDB(saveFolder, CommandPermissionJson, Database.CommandPermission, PrettyJsonOptions);
            anyErrors |= !SaveDB(saveFolder, UserPermissionJson, Database.UserPermission, PrettyJsonOptions);
            anyErrors |= !SaveDB(saveFolder, PlayerLogConfigJson, Database.PlayerLogConfig, JsonOptions);
            anyErrors |= !SaveDB(saveFolder, PlayerLogoutJson, Database.PlayerLogout, JsonOptions);
            
            if (Plugin.WaypointsActive) anyErrors |= !SaveDB(saveFolder, WaypointsJson, Database.Waypoints, JsonOptions);
            if (Plugin.PowerUpCommandsActive) anyErrors |= !SaveDB(saveFolder, PowerUpJson, Database.PowerUpList, JsonOptions);

            if (Plugin.ExperienceSystemActive)
            {
                anyErrors |= !SaveDB(saveFolder, PlayerExperienceJson, Database.PlayerExperience, JsonOptions);
                anyErrors |= !SaveDB(saveFolder, PlayerAbilityPointsJson, Database.PlayerAbilityIncrease, JsonOptions);
                anyErrors |= !SaveDB(saveFolder, PlayerLevelStatsJson, Database.PlayerLevelStats, JsonOptions);
                anyErrors |= !SaveDB(saveFolder, ExperienceClassStatsJson, Database.ExperienceClassStats,
                    PrettyJsonOptions);
            }

            if (Plugin.WeaponMasterySystemActive)
            {
                anyErrors |= !SaveDB(saveFolder, WeaponMasteryJson, Database.PlayerWeaponmastery, JsonOptions);
                anyErrors |= !SaveDB(saveFolder, WeaponMasteryConfigJson, Database.MasteryStatConfig, PrettyJsonOptions);
            }

            if (Plugin.BloodlineSystemActive)
            {
                anyErrors |= !SaveDB(saveFolder, BloodlinesJson, Database.PlayerBloodline, JsonOptions);
                anyErrors |= !SaveDB(saveFolder, BloodlineConfigJson, Database.BloodlineStatConfig, PrettyJsonOptions);
            }

            if (Plugin.PlayerGroupsActive) anyErrors |= !SaveDB(saveFolder, AlliancePreferencesJson, Database.AlliancePlayerPrefs, JsonOptions);

            Plugin.Log(LogSystem.Core, LogLevel.Info, $"All databases saved to: {saveFolder}");
            return !anyErrors;
        }

        public static void LoadOrInitialiseDatabase()
        {
            InternalLoadDatabase(true, LoadMethod.Both);
        }

        public static bool LoadDatabase(bool loadBackup)
        {
            return InternalLoadDatabase(false, loadBackup ? LoadMethod.Backup : LoadMethod.Main);
        }

        public static bool WipeDatabase()
        {
            return InternalLoadDatabase(true, LoadMethod.None);
        }
        
        private static bool InternalLoadDatabase(bool useInitialiser, LoadMethod loadMethod)
        {
            var anyErrors = false;
            // Core
            anyErrors |= !LoadDB(CommandPermissionJson, loadMethod, useInitialiser, ref Database.CommandPermission, PermissionSystem.DefaultCommandPermissions);
            anyErrors |= !LoadDB(UserPermissionJson, loadMethod, useInitialiser, ref Database.UserPermission);
            anyErrors |= !LoadDB(PlayerLogConfigJson, loadMethod, useInitialiser, ref Database.PlayerLogConfig);
            anyErrors |= !LoadDB(PlayerLogoutJson, loadMethod, useInitialiser, ref Database.PlayerLogout);
            
            if (Plugin.WaypointsActive) anyErrors |= !LoadDB(WaypointsJson, loadMethod, useInitialiser, ref Database.Waypoints);
            if (Plugin.PowerUpCommandsActive) anyErrors |= !LoadDB(PowerUpJson, loadMethod, useInitialiser, ref Database.PowerUpList);

            if (Plugin.ExperienceSystemActive)
            {
                anyErrors |= !LoadDB(PlayerExperienceJson, loadMethod, useInitialiser, ref Database.PlayerExperience);
                anyErrors |= !LoadDB(PlayerAbilityPointsJson, loadMethod, useInitialiser, ref Database.PlayerAbilityIncrease);
                anyErrors |= !LoadDB(PlayerLevelStatsJson, loadMethod, useInitialiser, ref Database.PlayerLevelStats);
                anyErrors |= !LoadDB(ExperienceClassStatsJson, loadMethod, useInitialiser, ref Database.ExperienceClassStats, ExperienceSystem.DefaultExperienceClassStats);
            }

            if (Plugin.WeaponMasterySystemActive)
            {
                anyErrors |= !LoadDB(WeaponMasteryJson, loadMethod, useInitialiser, ref Database.PlayerWeaponmastery);
                anyErrors |= !LoadDB(WeaponMasteryConfigJson, loadMethod, useInitialiser, ref Database.MasteryStatConfig, WeaponMasterySystem.DefaultMasteryConfig);
            }

            if (Plugin.BloodlineSystemActive)
            {
                anyErrors |= !LoadDB(BloodlinesJson, loadMethod, useInitialiser, ref Database.PlayerBloodline);
                anyErrors |= !LoadDB(BloodlineConfigJson, loadMethod, useInitialiser, ref Database.BloodlineStatConfig, BloodlineSystem.DefaultBloodlineConfig);
            }
            
            if (Plugin.PlayerGroupsActive) anyErrors |= !LoadDB(AlliancePreferencesJson, loadMethod, useInitialiser, ref Database.AlliancePlayerPrefs);

            Plugin.Log(LogSystem.Core, LogLevel.Info, "All database data is now loaded.", true);
            return !anyErrors;
        }
        
        
        
        private static bool SaveDB<TData>(string saveFolder, string specificFile, TData data, JsonSerializerOptions options)
        {
            try
            {
                var outputFile = Path.Combine(saveFolder, specificFile);
                File.WriteAllText(outputFile, JsonSerializer.Serialize(data, options));
                Plugin.Log(LogSystem.Core, LogLevel.Info, $"{specificFile} Saved.");
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log(LogSystem.Core, LogLevel.Error, $"Could not save DB {specificFile}: {e.Message}", true);
                return false;
            }
        }

        private static bool LoadDB<TData>(string specificFile, LoadMethod loadMethod, bool useInitialiser, ref TData currentRef, Func<TData> initialiser = null) where TData : class, new()
        {
            switch (loadMethod)
            {
                case LoadMethod.Main:
                    if (LoadDB(SavesPath, specificFile, ref currentRef)) return true;
                    break;
                case LoadMethod.Backup:
                    if (LoadDB(BackupsPath, specificFile, ref currentRef)) return true;
                    break;
                case LoadMethod.Both:
                default:
                    // attempt to load the main save first
                    if (LoadDB(SavesPath, specificFile, ref currentRef) ||
                        LoadDB(BackupsPath, specificFile, ref currentRef))
                    {
                        return true;
                    }
                    break;
                case LoadMethod.None:
                    Plugin.Log(LogSystem.Core, LogLevel.Info, $"Initialising DB for {specificFile}");
                    currentRef = initialiser == null ? new TData() : initialiser();
                    return true;
            }

            // If nothing loaded correctly, check if we should use the initialiser or just return the current value.
            if (!useInitialiser) return false;
            
            Plugin.Log(LogSystem.Core, LogLevel.Warning, $"Initialising DB for {specificFile}");
            currentRef = initialiser == null ? new TData() : initialiser();
            return false;
        }

        private static bool LoadDB<TData>(string folder, string specificFile, ref TData data) where TData : class, new()
        {
            // Default JSON content to valid json so that they have a chance to be serialised without errors when loading for the first time.
            var genericType = typeof(TData);
            var isJsonListData = genericType.IsGenericType &&
                                 (genericType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)) ||
                                  genericType.GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)));
            var defaultContents = isJsonListData ? "[]" : "{}";
            try {
                var saveFile = ConfirmFile(folder, specificFile, defaultContents);
                var json = File.ReadAllText(saveFile);
                data = JsonSerializer.Deserialize<TData>(json, JsonOptions);
                Plugin.Log(LogSystem.Core, LogLevel.Info, $"Main DB Loaded for {specificFile}");
                return true;
            } catch (Exception e) {
                Plugin.Log(LogSystem.Core, LogLevel.Error, $"Could not load main {specificFile}: {e.Message}", true);
                return false;
            }
        }
        
        public static string ConfirmFile(string address, string file, string defaultContents = "") {
            try {
                Directory.CreateDirectory(address);
            }
            catch (Exception e) {
                throw new Exception("Error creating directory at " + address + "\n Error is: " + e.Message);
            }
            var fileAddress = Path.Combine(address, file);
            try
            {
                // If the file does not exist, create a new empty file there
                if (!File.Exists(fileAddress))
                {
                    File.WriteAllText(fileAddress, defaultContents);
                }
            } catch (Exception e) {
                throw new Exception("Error creating file at " + fileAddress + "\n Error is: " + e.Message);
            }

            return fileAddress;
        }
    }
    
    public class PrefabGuidConverter : JsonConverter<PrefabGUID>
    {
        public override PrefabGUID Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            new PrefabGUID(reader.GetInt32()!);

        public override void Write(
            Utf8JsonWriter writer,
            PrefabGUID guid,
            JsonSerializerOptions options) =>
            writer.WriteNumberValue(guid.GuidHash);
    }
}
