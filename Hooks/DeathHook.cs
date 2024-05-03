using BepInEx.Logging;
using HarmonyLib;
using OpenRPG.Configuration;
using ProjectM;
using ProjectM.Network;
using OpenRPG.Systems;
using OpenRPG.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using LogSystem = OpenRPG.Plugin.LogSystem;

namespace OpenRPG.Hooks {
    [HarmonyPatch]
    public class DeathEventListenerSystem_Patch {
        [HarmonyPatch(typeof(DeathEventListenerSystem), "OnUpdate")]
        [HarmonyPostfix]
        public static void Postfix(DeathEventListenerSystem __instance) {
            Plugin.Log(LogSystem.Death, LogLevel.Info, "beginning Death Tracking");
            NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);
            Plugin.Log(LogSystem.Death, LogLevel.Info, "Death events converted successfully, length is " + deathEvents.Length);
            foreach (DeathEvent ev in deathEvents) {
                Plugin.Log(LogSystem.Death, LogLevel.Info, "Death Event occured");
                //-- Just track whatever died...
                if (WorldDynamicsSystem.isFactionDynamic) WorldDynamicsSystem.MobKillMonitor(ev.Died);

                //-- Player Creature Kill Tracking
                var killer = ev.Killer;

                // If the entity killing is a minion, switch the killer to the owner of the minion.
                if (__instance.EntityManager.HasComponent<Minion>(killer)) {
                    Plugin.Log(LogSystem.Death, LogLevel.Info, $"Minion killed entity. Getting owner...");
                    if (__instance.EntityManager.TryGetComponentData<EntityOwner>(killer, out var entityOwner)) {
                        killer = entityOwner.Owner;
                        Plugin.Log(LogSystem.Death, LogLevel.Info, $"Owner found, switching killer to owner.");
                    }
                }

                if (__instance.EntityManager.HasComponent<PlayerCharacter>(killer) && __instance.EntityManager.HasComponent<Movement>(ev.Died)) {
                    Plugin.Log(LogSystem.Death, LogLevel.Info, "Killer is a player, running xp and heat and the like");
                    
                    if ((ExperienceSystem.isEXPActive || HunterHuntedSystem.isActive) && ExperienceSystem.EntityProvidesExperience(ev.Died)) {
                        var isVBlood = Plugin.Server.EntityManager.TryGetComponentData(ev.Died, out BloodConsumeSource bS) && bS.UnitBloodType.Equals(Helper.vBloodType);

                        var useGroup = ExperienceSystem.groupLevelScheme != ExperienceSystem.GroupLevelScheme.None && ExperienceSystem.GroupModifier > 0;

                        var triggerLocation = Plugin.Server.EntityManager.GetComponentData<LocalToWorld>(ev.Died);                        
                        var closeAllies = Alliance.GetClosePlayers(
                            triggerLocation.Position, killer, ExperienceSystem.GroupMaxDistance, true, useGroup, LogSystem.Death);

                        // If you get experience for the kill, you get heat for the kill
                        if (ExperienceSystem.isEXPActive) ExperienceSystem.EXPMonitor(closeAllies, ev.Died, isVBlood);
                        if (HunterHuntedSystem.isActive) HunterHuntedSystem.PlayerKillEntity(closeAllies, ev.Died, isVBlood);
                    }
                    
                    if (WeaponMasterSystem.isMasteryEnabled) WeaponMasterSystem.UpdateMastery(killer, ev.Died);
                    if (Bloodlines.areBloodlinesEnabled) Bloodlines.UpdateBloodline(killer, ev.Died);

                }

                //-- Auto Respawn & HunterHunted System Begin
                if (__instance.EntityManager.HasComponent<PlayerCharacter>(ev.Died)) {
                    Plugin.Log(LogSystem.Death, LogLevel.Info, "the dead person is a player, running xp loss and heat dumping");
                    if (HunterHuntedSystem.isActive) HunterHuntedSystem.PlayerDied(ev.Died);
                    if (ExperienceSystem.isEXPActive && ExperienceSystem.xpLostOnRelease) {
                        ExperienceSystem.deathXPLoss(ev.Died, ev.Killer);
                    }

                    PlayerCharacter player = __instance.EntityManager.GetComponentData<PlayerCharacter>(ev.Died);
                    Entity userEntity = player.UserEntity;
                    User user = __instance.EntityManager.GetComponentData<User>(userEntity);
                    ulong SteamID = user.PlatformId;

                    //-- Check for AutoRespawn
                    if (user.IsConnected) {
                        bool isServerWide = Database.autoRespawn.ContainsKey(1);
                        bool doRespawn = isServerWide || Database.autoRespawn.ContainsKey(SteamID);

                        if (doRespawn) {
                            Utils.RespawnCharacter.Respawn(ev.Died, player, userEntity);
                        }
                    }
                    //-- ----------------------------------------
                }
            }
            
            // TODO this should integrate iterating into the loop above
            //-- Random Encounters
            if (deathEvents.Length > 0 && RandomEncountersConfig.Enabled.Value && Plugin.isInitialized)
                RandomEncountersSystem.ServerEvents_OnDeath(__instance, deathEvents);
            //-- ----------------------------------------
        }
    }
}