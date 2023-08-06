using Bloodstone.API;
using OpenRPG.Exceptions;
using ProjectM;
using System;
using System.Text;
using Unity.Entities;
using VRising.GameData.Models;

namespace OpenRPG.Systems
{
    internal class ClanSystem
    {

        private static EntityManager _entityManager = VWorld.Server.EntityManager;
        private static Team _teamComponentData;
        private static ClanTeam _clanComponentData;
        private static TeamData _teamDataComponentData;

        internal static bool ProcessClanXp(UserModel userModel, Entity clanEntity)
        {

            if(!_entityManager.TryGetComponentData<Team>(userModel.Character.Entity, out _teamComponentData))
            {
                throw new NoClanMemberException("Player without Team");
            }

            Plugin.Logger.LogInfo($"{DateTime.Now}: Team Value: {_teamComponentData.Value.ToString()} - Faction Index: {_teamComponentData.FactionIndex.ToString()}");


            if (_entityManager.TryGetComponentData<ClanTeam>(userModel.Character.Entity, out _clanComponentData))
            {
                throw new NoClanMemberException("Player without ClanTeam");
            }

            Plugin.Logger.LogInfo($"{DateTime.Now}: ClanTeam Name Value: {_clanComponentData.Name} - TeamValue Index: {_clanComponentData.TeamValue.ToString()}  - ClanGuid Index: {_clanComponentData.ClanGuid.ToString()}");

            if (_entityManager.TryGetComponentData<TeamData>(userModel.Character.Entity, out _teamDataComponentData))
            {
                throw new NoClanMemberException("Player without TeamData");
            }

            Plugin.Logger.LogInfo($"{DateTime.Now}: TeamData Team Value: {_teamDataComponentData.TeamValue}");

            var systemBase = VWorld.Server.GetExistingSystem<SystemBase>();
            var buff = BuffUtility.BuffSpawnerSystemData.Create(systemBase);


            return true;
        }
    }
}
