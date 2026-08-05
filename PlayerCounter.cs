using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace SoloTrigger;

internal readonly record struct PlayerSnapshot(
    int AllPlayers,
    int NonAwayPlayers,
    float? NearestMajorAetheryteHorizontalDistance);

internal static class PlayerCounter
{
    internal const float MajorAetheryteExclusionRange = 125f;

    private const uint AwayOnlineStatusId = 17;

    public static HashSet<uint> BuildMajorAetheryteIds(IDataManager dataManager) =>
        dataManager.GetExcelSheet<Aetheryte>()
            .Where(aetheryte => aetheryte.IsAetheryte)
            .Select(aetheryte => aetheryte.RowId)
            .ToHashSet();

    public static PlayerSnapshot Capture(IObjectTable objectTable, IReadOnlySet<uint> majorAetheryteIds)
    {
        var localPlayer = objectTable.LocalPlayer;
        var localPlayerId = localPlayer?.GameObjectId;
        var allPlayers = 0;
        var nonAwayPlayers = 0;

        // PlayerObjects only scans the real character-manager player slots.
        // Iterating the full object table also includes UI preview characters.
        foreach (var gameObject in objectTable.PlayerObjects)
        {
            if (gameObject is not IPlayerCharacter player || gameObject.GameObjectId == localPlayerId)
            {
                continue;
            }

            allPlayers++;
            if (player.OnlineStatus.RowId != AwayOnlineStatusId)
            {
                nonAwayPlayers++;
            }
        }

        float? nearestAetheryteDistance = null;
        if (localPlayer is not null)
        {
            var playerPosition = new Vector2(localPlayer.Position.X, localPlayer.Position.Z);
            foreach (var gameObject in objectTable)
            {
                if (gameObject.ObjectKind != ObjectKind.Aetheryte || !majorAetheryteIds.Contains(gameObject.BaseId))
                {
                    continue;
                }

                var aetherytePosition = new Vector2(gameObject.Position.X, gameObject.Position.Z);
                var distance = Vector2.Distance(playerPosition, aetherytePosition);
                if (nearestAetheryteDistance is null || distance < nearestAetheryteDistance.Value)
                {
                    nearestAetheryteDistance = distance;
                }
            }
        }

        return new PlayerSnapshot(allPlayers, nonAwayPlayers, nearestAetheryteDistance);
    }

    public static int Select(PlayerSnapshot snapshot, PlayerCountMode mode)
    {
        if (UsesAetheryteExclusion(mode) && IsInsideMajorAetheryteRange(snapshot))
        {
            return 0;
        }

        return mode is PlayerCountMode.NonAwayPlayers or PlayerCountMode.NonAwayPlayersAwayFromAetheryte
            ? snapshot.NonAwayPlayers
            : snapshot.AllPlayers;
    }

    public static bool IsInsideMajorAetheryteRange(PlayerSnapshot snapshot) =>
        snapshot.NearestMajorAetheryteHorizontalDistance is <= MajorAetheryteExclusionRange;

    public static bool UsesAetheryteExclusion(PlayerCountMode mode) =>
        mode is PlayerCountMode.AllPlayersAwayFromAetheryte or PlayerCountMode.NonAwayPlayersAwayFromAetheryte;

    public static string GetModeLabel(PlayerCountMode mode) => mode switch
    {
        PlayerCountMode.AllPlayers => "所有玩家",
        PlayerCountMode.NonAwayPlayers => "非离开玩家",
        PlayerCountMode.AllPlayersAwayFromAetheryte => "所有玩家（排除大水晶附近）",
        PlayerCountMode.NonAwayPlayersAwayFromAetheryte => "非离开玩家（排除大水晶附近）",
        _ => "所有玩家",
    };
}
