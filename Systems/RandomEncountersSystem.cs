using OpenRPG.Models.RandomEncounters;
using OpenRPG.Utils.RandomEncounters;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using VRising.GameData.Models;
using VRising.GameData;
using OpenRPG.Configuration;
using System.Collections.Concurrent;
using BepInEx.Logging;
using VRising.GameData.Methods;
using LogSystem = OpenRPG.Plugin.LogSystem;

namespace OpenRPG.Systems
{
    internal static class RandomEncountersSystem
    {
        private static readonly ConcurrentDictionary<ulong, ConcurrentDictionary<int, ItemDataModel>> RewardsMap = new();

        private static readonly ConcurrentDictionary<int, UserModel> NpcPlayerMap = new();

        private static readonly Entity StationEntity = new();
        private static float Lifetime => RandomEncountersConfig.EncounterLength.Value;
        private static string MessageTemplate => RandomEncountersConfig.EncounterMessageTemplate.Value;

        internal static Dictionary<long, (float actualDuration, Action<Entity> Actions)> PostActions = new();

        private static System.Random Random = new System.Random();
        private const LogSystem LoggingSystem = LogSystem.RandomEncounter;

        internal static void StartEncounter(UserModel user = null)
        {
            var world = GameData.World;

            if (user == null)
            {
                var users = GameData.Users.Online;
                if (RandomEncountersConfig.SkipPlayersInCastle.Value)
                {
                    users = users.Where(u => !u.IsInCastle());
                }

                if (RandomEncountersConfig.SkipPlayersInCombat.Value)
                {
                    users = users.Where(u => !u.IsInCombat());
                }
                user = users.OrderBy(_ => Random.Next()).FirstOrDefault();
            }

            if (user == null)
            {
                Plugin.Log(LoggingSystem, LogLevel.Message, "Could not find any eligible players for a random encounter...");
                return;
            }

            var npc = DataFactory.GetRandomNpc(user.Character.Equipment.Level);
            if (npc == null)
            {
                Plugin.Log(LoggingSystem, LogLevel.Warning,$"Could not find any NPCs within the given level range. , User Level: {user.Character.Equipment.Level})");
                return;
            }
            Plugin.Log(LoggingSystem, LogLevel.Message, $"Attempting to start a new encounter for {user.CharacterName} with {npc.Name}");
            var minSpawnDistance = RandomEncountersConfig.MinSpawnDistance.Value;
            var maxSpawnDistance = RandomEncountersConfig.MaxSpawnDistance.Value;
            try
            {
                NpcPlayerMap[npc.Id] = user;
                var spawnPosition = user.Position;

                world.GetExistingSystem<UnitSpawnerUpdateSystem>()
                    .SpawnUnit(StationEntity, new PrefabGUID(npc.Id), spawnPosition, 1, minSpawnDistance, maxSpawnDistance, Lifetime);

            }
            catch (Exception ex)
            {
                Plugin.Log(LoggingSystem, LogLevel.Error, $"RE Failed spawning unit {ex}");
                // Suppress
            }
        }

        internal static void ServerEvents_OnUnitSpawned(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<PrefabGUID>(entity))
            {
                return;
            }

            var prefabGuid = entityManager.GetComponentData<PrefabGUID>(entity);
            if (!NpcPlayerMap.TryGetValue(prefabGuid.GuidHash, out var user))
            {
                return;
            }
            if (!entityManager.HasComponent<LifeTime>(entity))
            {
                return;
            }
            var lifeTime = entityManager.GetComponentData<LifeTime>(entity);
            if (Math.Abs(lifeTime.Duration - Lifetime) > 0.001)
            {
                return;
            }

            var npcData = DataFactory.GetAllNpcs().FirstOrDefault(n => n.Id == prefabGuid.GuidHash);
            if (npcData == null)
            {
                return;
            }

            NpcPlayerMap.TryRemove(prefabGuid.GuidHash, out _);

            if (!RewardsMap.ContainsKey(user.PlatformId))
            {
                RewardsMap[user.PlatformId] = new ConcurrentDictionary<int, ItemDataModel>();
            }
            var message =
                string.Format(
                    MessageTemplate,
                    npcData.Name, Lifetime);

            user.SendSystemMessage(message);
            Plugin.Log(LoggingSystem, LogLevel.Info, $"Encounters started: {user.CharacterName} vs. {npcData.Name}");

            if (RandomEncountersConfig.NotifyAdminsAboutEncountersAndRewards.Value)
            {
                var onlineAdmins = DataFactory.GetOnlineAdmins();
                foreach (var onlineAdmin in onlineAdmins)
                {
                    onlineAdmin.SendSystemMessage($"Encounter started: {user.CharacterName} vs. {npcData.Name}");
                }
            }
            RewardsMap[user.PlatformId][entity.Index] = DataFactory.GetRandomItem();
        }

        internal static void ServerEvents_OnDeath(DeathEventListenerSystem sender, NativeArray<DeathEvent> deathEvents)
        {
            foreach (var deathEvent in deathEvents)
            {
                if (!sender.EntityManager.HasComponent<PlayerCharacter>(deathEvent.Killer))
                {
                    continue;
                }

                var playerCharacter = sender.EntityManager.GetComponentData<PlayerCharacter>(deathEvent.Killer);
                var userModel = GameData.Users.FromEntity(playerCharacter.UserEntity);


                if (RewardsMap.TryGetValue(userModel.PlatformId, out var bounties) &&
                    bounties.TryGetValue(deathEvent.Died.Index, out var itemModel))
                {
                    var itemGuid = new PrefabGUID(itemModel.Id);
                    var quantity = RandomEncountersConfig.Items[itemModel.Id];
                    if (!userModel.TryGiveItem(new PrefabGUID(itemModel.Id), quantity.Value, out _))
                    {
                        userModel.DropItemNearby(itemGuid, quantity.Value);
                    }
                    var message = string.Format(RandomEncountersConfig.RewardMessageTemplate.Value, itemModel.Color, itemModel.Name);
                    userModel.SendSystemMessage(message);
                    bounties.TryRemove(deathEvent.Died.Index, out _);
                    Plugin.Log(LoggingSystem, LogLevel.Info, $"{userModel.CharacterName} earned reward: {itemModel.Name}");
                    var globalMessage = string.Format(RandomEncountersConfig.RewardAnnouncementMessageTemplate.Value,
                        userModel.CharacterName, itemModel.Color, itemModel.Name);
                    if (RandomEncountersConfig.NotifyAllPlayersAboutRewards.Value)
                    {
                        var onlineUsers = GameData.Users.Online;
                        foreach (var model in onlineUsers.Where(u => u.PlatformId != userModel.PlatformId))
                        {
                            model.SendSystemMessage(globalMessage);
                        }

                    }
                    else if (RandomEncountersConfig.NotifyAdminsAboutEncountersAndRewards.Value)
                    {
                        var onlineAdmins = DataFactory.GetOnlineAdmins();
                        foreach (var onlineAdmin in onlineAdmins)
                        {
                            onlineAdmin.SendSystemMessage($"{userModel.CharacterName} earned an encounter reward: <color={itemModel.Color}>{itemModel.Name}</color>");
                        }
                    }
                }
            }
        }
    }
}
