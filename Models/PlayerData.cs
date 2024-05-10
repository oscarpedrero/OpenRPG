using Unity.Collections;
using Unity.Entities;

namespace OpenRPG.Models;

public struct PlayerData(
    FixedString64 characterName,
    ulong steamID,
    bool isOnline,
    Entity userEntity,
    Entity charEntity)
{
    public FixedString64 CharacterName { get; set; } = characterName;
    public ulong SteamID { get; set; } = steamID;
    public bool IsOnline { get; set; } = isOnline;
    public Entity UserEntity { get; set; } = userEntity;
    public Entity CharEntity { get; set; } = charEntity;
}