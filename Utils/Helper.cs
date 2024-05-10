using ProjectM;
using ProjectM.Network;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using OpenRPG.Hooks;
using System.Text.RegularExpressions;
using ProjectM.Scripting;
using System.Collections.Generic;
using BepInEx.Logging;
using VampireCommandFramework;
using Bloodstone.API;
using OpenRPG.Models;
using OpenRPG.Utils.Prefabs;
using LogSystem = OpenRPG.Plugin.LogSystem;

namespace OpenRPG.Utils
{
    public static class Helper
    {
        private static Entity empty_entity = new Entity();
        private static System.Random rand = new System.Random();
        
        private static IsSystemInitialised<ServerGameSettings> _serverGameSettings = default;
        private static IsSystemInitialised<ServerGameManager> _serverGameManager = default;
        private static IsSystemInitialised<UserActivityGridSystem> _userActivityGridSystem = default;

        public static int buffGUID = (int)SetBonus.SetBonus_Damage_Minor_Buff_01;
        public static int forbiddenBuffGUID = (int)SetBonus.SetBonus_MaxHealth_Minor_Buff_01;
        public static PrefabGUID AppliedBuff = new PrefabGUID(buffGUID);
        public static PrefabGUID SeverePunishmentDebuff = new PrefabGUID((int)Buffs.Buff_General_Garlic_Fever);          //-- Using this for PvP Punishment debuff
        public static PrefabGUID MinorPunishmentDebuff = new PrefabGUID((int)Buffs.Buff_General_Garlic_Area_Inside);

        //-- LevelUp Buff
        public static PrefabGUID LevelUp_Buff = new PrefabGUID((int)Effects.AB_ChurchOfLight_Priest_HealBomb_Buff);
        public static PrefabGUID HostileMark_Buff = new PrefabGUID((int)Buffs.Buff_Cultist_BloodFrenzy_Buff);

        //-- Nice Effect...
        public static PrefabGUID AB_Undead_BishopOfShadows_ShadowSoldier_Minion_Buff = new PrefabGUID((int)Effects.AB_Undead_BishopOfShadows_ShadowSoldier_Minion_Buff);   //-- Impair cast & movement

        //-- Fun
        public static PrefabGUID HolyNuke = new PrefabGUID((int)Effects.AB_Paladin_HolyNuke_Buff);
        public static PrefabGUID Pig_Transform_Debuff = new PrefabGUID((int)Remainders.Witch_PigTransformation_Buff);


        //-- Possible Buff use
        public static PrefabGUID EquipBuff_Chest_Base = new PrefabGUID((int)EquipBuffs.EquipBuff_Chest_Base);         //-- Hmm... not sure what to do with this right now...
        public static PrefabGUID AB_BloodBuff_VBlood_0 = new PrefabGUID((int)Effects.AB_BloodBuff_VBlood_0);          //-- Does it do anything negative...? How can i check for this, seems like it's a total blank o.o

        public static Regex rxName = new Regex(@"(?<=\])[^\[].*");
        
        public static bool humanReadablePercentageStats = false;
        public static bool inverseMultipersDisplayReduction = true;

        public static bool GetUserActivityGridSystem(out UserActivityGridSystem userActivityGridSystem)
        {
            userActivityGridSystem = _userActivityGridSystem.system;
            if (!_userActivityGridSystem.isInitialised)
            {
                var aps = Plugin.Server.GetExistingSystem<AiPrioritizationSystem>();
                if (aps == null) return false;
                _userActivityGridSystem.system = aps._UserActivityGridSystem;
                userActivityGridSystem = _userActivityGridSystem.system;
            }
            return true;
        }

        public static bool GetServerGameManager(out ServerGameManager serverGameManager)
        {
            serverGameManager = _serverGameManager.system;
            if (!_serverGameManager.isInitialised)
            {
                var ssm = Plugin.Server.GetExistingSystem<ServerScriptMapper>();
                if (ssm == null) return false;
                _serverGameManager.system = ssm._ServerGameManager;
                serverGameManager = _serverGameManager.system;
            }
            return true;
        }

        public static bool GetServerGameSettings(out ServerGameSettings settings)
        {
            settings = _serverGameSettings.system;
            if (!_serverGameSettings.isInitialised)
            {
                var sgs = Plugin.Server.GetExistingSystem<ServerGameSettingsSystem>();
                if (sgs == null) return false;
                _serverGameSettings.system = sgs._Settings;
                settings = _serverGameSettings.system;
            }
            return true;
        }

        public static ModifyUnitStatBuff_DOTS MakeBuff(UnitStatType type, double strength) {
            ModifyUnitStatBuff_DOTS buff;

            var modType = ModificationType.Add;
            if (Helper.multiplierStats.Contains(type)) {
                modType = ModificationType.Multiply;
            }
            buff = (new ModifyUnitStatBuff_DOTS() {
                StatType = type,
                Value = (float)strength,
                ModificationType = modType,
                Id = ModificationId.NewId(0)
            });
            return buff;
        }
        
        public static double CalcBuffValue(double strength, double effectiveness, double rate, UnitStatType type)
        {
            effectiveness = Math.Max(effectiveness, 1);
            if (Helper.inverseMultiplierStats.Contains(type)) {
                var value = strength * effectiveness;
                return 1 - value / (value + rate);
            }
            return strength * rate * effectiveness;
        }

        public static FixedString64 GetTrueName(string name)
        {
            MatchCollection match = rxName.Matches(name);
            if (match.Count > 0)
            {
                name = match[match.Count - 1].ToString();
            }
            return name;
        }

        public static void CreatePlayerCache() {

            Cache.NamePlayerCache.Clear();
            Cache.SteamPlayerCache.Clear();
            EntityQuery query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc() {
                All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<User>()
                    },
                Options = EntityQueryOptions.IncludeDisabled
            });
            var userEntities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in userEntities) {
                var userData = Plugin.Server.EntityManager.GetComponentData<User>(entity);
                PlayerData playerData = new PlayerData(userData.CharacterName, userData.PlatformId, userData.IsConnected, entity, userData.LocalCharacter._Entity);

                Cache.NamePlayerCache.TryAdd(GetTrueName(userData.CharacterName.ToString().ToLower()), playerData);
                Cache.SteamPlayerCache.TryAdd(userData.PlatformId, playerData);

            }

            Plugin.Log(LogSystem.Core, LogLevel.Info, "Player Cache Created.");
        }
        
        public static void UpdatePlayerCache(Entity userEntity, User userData, bool forceOffline = false)
        {
            if (forceOffline) userData.IsConnected = false;
            PlayerData playerData = new PlayerData(userData.CharacterName, userData.PlatformId, userData.IsConnected, userEntity, userData.LocalCharacter._Entity);

            Cache.NamePlayerCache[GetTrueName(userData.CharacterName.ToString().ToLower())] = playerData;
            Cache.SteamPlayerCache[userData.PlatformId] = playerData;
        }

        public static void ApplyBuff(Entity User, Entity Char, PrefabGUID GUID)
        {
            var des = Plugin.Server.GetExistingSystem<DebugEventsSystem>();
            var fromCharacter = new FromCharacter()
            {
                User = User,
                Character = Char
            };
            var buffEvent = new ApplyBuffDebugEvent()
            {
                BuffPrefabGUID = GUID
            };
            des.ApplyBuff(fromCharacter, buffEvent);
        }

        public static void RemoveBuff(Entity Char, PrefabGUID GUID)
        {
            if (BuffUtility.HasBuff(Plugin.Server.EntityManager, Char, GUID))
            {
                BuffUtility.TryGetBuff(Plugin.Server.EntityManager, Char, GUID, out var BuffEntity_);
                Plugin.Server.EntityManager.AddComponent<DestroyTag>(BuffEntity_);
                return;
            }
        }

        public static string GetNameFromSteamID(ulong SteamID)
        {
            if (Cache.SteamPlayerCache.TryGetValue(SteamID, out var data))
            {
                return data.CharacterName.ToString();
            }
            else
            {
                return null;
            }
        }

        public static ulong GetSteamIDFromName(string name)
        {
            if (Cache.NamePlayerCache.TryGetValue(name.ToLower(), out var data))
            {
                return data.SteamID;
            }
            else
            {
                return 0;
            }
        }

        public static PrefabGUID GetGUIDFromName(string name)
        {
            var gameDataSystem = Plugin.Server.GetExistingSystem<GameDataSystem>();
            var managed = gameDataSystem.ManagedDataRegistry;

            foreach (var entry in gameDataSystem.ItemHashLookupMap)
            {
                try
                {
                    var item = managed.GetOrDefault<ManagedItemData>(entry.Key);
                    if (item.PrefabName.StartsWith("Item_VBloodSource") || item.PrefabName.StartsWith("GM_Unit_Creature_Base") || item.PrefabName == "Item_Cloak_ShadowPriest") continue;
                    var nameLower = name.ToLower();
                    if (item.Name.ToString().ToLower().Equals(nameLower) ||
                        item.PrefabName.ToLower().Equals(nameLower))
                    {
                        return entry.Key;
                    }
                }
                catch { }
            }

            return new PrefabGUID(0);
        }

        public static void KickPlayer(Entity userEntity)
        {
            EntityManager em = Plugin.Server.EntityManager;
            var userData = em.GetComponentData<User>(userEntity);
            int index = userData.Index;
            NetworkId id = em.GetComponentData<NetworkId>(userEntity);

            var entity = em.CreateEntity(
                ComponentType.ReadOnly<NetworkEventType>(),
                ComponentType.ReadOnly<SendEventToUser>(),
                ComponentType.ReadOnly<KickEvent>()
            );

            var KickEvent = new KickEvent()
            {
                PlatformId = userData.PlatformId
            };

            em.SetComponentData<SendEventToUser>(entity, new()
            {
                UserIndex = index
            });
            em.SetComponentData<NetworkEventType>(entity, new()
            {
                EventId = NetworkEvents.EventId_KickEvent,
                IsAdminEvent = false,
                IsDebugEvent = false
            });

            em.SetComponentData(entity, KickEvent);
        }

        public static void AddItemToInventory(ChatCommandContext ctx, PrefabGUID guid, int amount)
        {
            var gameData = Plugin.Server.GetExistingSystem<GameDataSystem>();
            var itemSettings = AddItemSettings.Create(Plugin.Server.EntityManager, gameData.ItemHashLookupMap);
            var inventoryResponse = InventoryUtilitiesServer.TryAddItem(itemSettings, ctx.Event.SenderCharacterEntity, guid, amount);
        }

        public static bool FindPlayer(string name, bool mustOnline, out Entity playerEntity, out Entity userEntity)
        {
            EntityManager entityManager = Plugin.Server.EntityManager;

            //-- Way of the Cache
            if (Cache.NamePlayerCache.TryGetValue(name.ToLower(), out var data))
            {
                playerEntity = data.CharEntity;
                userEntity = data.UserEntity;
                if (mustOnline)
                {
                    var userComponent = entityManager.GetComponentData<User>(userEntity);
                    if (!userComponent.IsConnected)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                playerEntity = empty_entity;
                userEntity = empty_entity;
                return false;
            }
        }
        
        public static bool FindPlayer(ulong steamid, bool mustOnline, out Entity playerEntity, out Entity userEntity)
        {
            EntityManager entityManager = Plugin.Server.EntityManager;

            //-- Way of the Cache
            if (Cache.SteamPlayerCache.TryGetValue(steamid, out var data))
            {
                playerEntity = data.CharEntity;
                userEntity = data.UserEntity;
                if (mustOnline)
                {
                    var userComponent = entityManager.GetComponentData<User>(userEntity);
                    if (!userComponent.IsConnected)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                playerEntity = empty_entity;
                userEntity = empty_entity;
                return false;
            }
        }

        public static bool HasBuff(Entity player, PrefabGUID BuffGUID)
        {
            return BuffUtility.HasBuff(Plugin.Server.EntityManager, player, BuffGUID);
        }

        public static void SetPvPShield(Entity character, bool value)
        {
            var em = Plugin.Server.EntityManager;
            var cUnitStats = em.GetComponentData<UnitStats>(character);
            var cBuffer = em.GetBuffer<BoolModificationBuffer>(character);
            cUnitStats.PvPProtected.SetBaseValue(value, cBuffer);
            em.SetComponentData(character, cUnitStats);
        }

        public static bool SpawnNPCIdentify(out float identifier, string name, float3 position, float minRange = 1, float maxRange = 2, float duration = -1)
        {
            identifier = 0f;
            float default_duration = 5.0f;
            float duration_final;
            var isFound = Enum.TryParse(name, true, out Prefabs.Units unit);
            if (!isFound) return false;

            float UniqueID = (float)rand.NextDouble();
            if (UniqueID == 0.0) UniqueID += 0.00001f;
            else if (UniqueID == 1.0f) UniqueID -= 0.00001f;
            duration_final = default_duration + UniqueID;

            while (Cache.spawnNPC_Listen.ContainsKey(duration))
            {
                UniqueID = (float)rand.NextDouble();
                if (UniqueID == 0.0) UniqueID += 0.00001f;
                else if (UniqueID == 1.0f) UniqueID -= 0.00001f;
                duration_final = default_duration + UniqueID;
            }

            UnitSpawnerReactSystem_Patch.listen = true;
            identifier = duration_final;
            var Data = new SpawnNpcListen(duration, default, default, default, false);
            Cache.spawnNPC_Listen.Add(duration_final, Data);

            Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, new PrefabGUID((int)unit), position, 1, minRange, maxRange, duration_final);
            return true;
        }

        public static bool SpawnAtPosition(Entity user, Prefabs.Units unit, int count, float3 position, float minRange = 1, float maxRange = 2, float duration = -1) {
            var guid = new PrefabGUID((int)unit);

            try
            {
                Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, guid, position, count, minRange, maxRange, duration);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static PrefabGUID GetPrefabGUID(Entity entity)
        {
            var entityManager = Plugin.Server.EntityManager;
            if (entity == Entity.Null || !entityManager.TryGetComponentData<PrefabGUID>(entity, out var prefabGuid))
            {
                prefabGuid = new PrefabGUID(0);
            }

            return prefabGuid;
        }

        public static string GetPrefabName(PrefabGUID hashCode)
        {
            var s = Plugin.Server.GetExistingSystem<PrefabCollectionSystem>();
            string name = "Nonexistent";
            if (hashCode.GuidHash == 0)
            {
                return name;
            }
            try
            {
                name = s.PrefabGuidToNameDictionary[hashCode];
            }
            catch
            {
                name = "NoPrefabName";
            }
            return name;
        }

        public static string GetPrefabName(Entity entity)
        {
            return GetPrefabName(GetPrefabGUID(entity));
        }
        
        public static Prefabs.Faction ConvertGuidToFaction(PrefabGUID guid) {
            if (Enum.IsDefined(typeof(Prefabs.Faction), guid.GetHashCode())) return (Prefabs.Faction)guid.GetHashCode();
            return Prefabs.Faction.Unknown;
        }
        
        public static Prefabs.Units ConvertGuidToUnit(PrefabGUID guid) {
            if (Enum.IsDefined(typeof(Prefabs.Units), guid.GetHashCode())) return (Prefabs.Units)guid.GetHashCode();
            return Prefabs.Units.Unknown;
        }

        public static void TeleportTo(ChatCommandContext ctx, float3 position)
        {
            var entity = VWorld.Server.EntityManager.CreateEntity(
                    ComponentType.ReadWrite<FromCharacter>(),
                    ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
                );

            VWorld.Server.EntityManager.SetComponentData<FromCharacter>(entity, new()
            {
                User = ctx.Event.SenderUserEntity,
                Character = ctx.Event.SenderCharacterEntity
            });

            VWorld.Server.EntityManager.SetComponentData<PlayerTeleportDebugEvent>(entity, new()
            {
                Position = new float3(position.x, position.y, position.z),
                Target = PlayerTeleportDebugEvent.TeleportTarget.Self
            });
        }
        
        public static PrefabGUID vBloodType = new(1557174542);


        // For stats that reduce as a multiplier of 1 - their value, so that a value of 0.5 halves the stat, and 0.75 quarters it.
        // I do this so that we can compute linear increases to a formula of X/(X+Y) where Y is the amount for +100% effectivness and X is the stat value
        public static HashSet<UnitStatType> inverseMultiplierStats = new()
            {
                UnitStatType.CooldownModifier,
                UnitStatType.PrimaryCooldownModifier
                /*,
                UnitStatType.PhysicalResistance,
                UnitStatType.SpellResistance,
                UnitStatType.ResistVsBeasts,
                UnitStatType.ResistVsCastleObjects,
                UnitStatType.ResistVsDemons,
                UnitStatType.ResistVsHumans,
                UnitStatType.ResistVsMechanical,
                UnitStatType.ResistVsPlayerVampires,
                UnitStatType.ResistVsUndeads,
                UnitStatType.BloodDrain,
                UnitStatType.ReducedResourceDurabilityLoss
                */
            };

        public static HashSet<UnitStatType> percentageStats = new()
            {
                UnitStatType.PhysicalCriticalStrikeChance,
                UnitStatType.SpellCriticalStrikeChance,
                UnitStatType.PhysicalCriticalStrikeDamage,
                UnitStatType.SpellCriticalStrikeDamage,
                UnitStatType.PhysicalLifeLeech,
                UnitStatType.PrimaryLifeLeech,
                UnitStatType.SpellLifeLeech,
                UnitStatType.AttackSpeed,
                UnitStatType.PrimaryAttackSpeed,
                UnitStatType.PassiveHealthRegen,
                UnitStatType.ResourceYield
            };

        //This should be a dictionary lookup for the stats to what mod type they should use
        public static HashSet<UnitStatType> multiplierStats = new()
            {
                UnitStatType.CooldownModifier,
                UnitStatType.PrimaryCooldownModifier, /*
                {UnitStatType.PhysicalResistance },
                {UnitStatType.SpellResistance },
                {UnitStatType.ResistVsBeasts },
                {UnitStatType.ResistVsCastleObjects },
                {UnitStatType.ResistVsDemons },
                {UnitStatType.ResistVsHumans },
                {UnitStatType.ResistVsMechanical },
                {UnitStatType.ResistVsPlayerVampires },
                {UnitStatType.ResistVsUndeads },
                {UnitStatType.ReducedResourceDurabilityLoss },
                {UnitStatType.BloodDrain },*/
                UnitStatType.ResourceYield
            };

        public static HashSet<UnitStatType> baseStatsSet = new()
            {
                UnitStatType.PhysicalPower,
                UnitStatType.ResourcePower,
                UnitStatType.SiegePower,
                UnitStatType.AttackSpeed,
                UnitStatType.FireResistance,
                UnitStatType.GarlicResistance,
                UnitStatType.SilverResistance,
                UnitStatType.HolyResistance,
                UnitStatType.SunResistance,
                UnitStatType.SpellResistance,
                UnitStatType.PhysicalResistance,
                UnitStatType.SpellCriticalStrikeDamage,
                UnitStatType.SpellCriticalStrikeChance,
                UnitStatType.PhysicalCriticalStrikeDamage,
                UnitStatType.PhysicalCriticalStrikeChance,
                UnitStatType.PassiveHealthRegen,
                UnitStatType.ResourceYield,
                UnitStatType.PvPResilience,
                UnitStatType.ReducedResourceDurabilityLoss
            };
        
        public static string CamelCaseToSpaces(UnitStatType type) {
            var name = Enum.GetName(type);
            // Split words by camel case
            // ie, PhysicalPower => "Physical Power"
            return Regex.Replace(name, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }

        private struct IsSystemInitialised<T>()
        {
            public bool isInitialised = false;
            public T system = default;
        }
    }
}
