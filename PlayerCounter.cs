using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;

namespace SoloTrigger;

internal readonly record struct PlayerCounts(int Nearby, int NonAway);

internal static class PlayerCounter
{
    private const uint AwayOnlineStatusId = 17;

    public static PlayerCounts Count(IObjectTable objectTable)
    {
        var localPlayerId = objectTable.LocalPlayer?.GameObjectId;
        var nearby = 0;
        var nonAway = 0;

        // PlayerObjects only scans the real character-manager player slots.
        // Iterating the full object table also includes UI preview characters
        // created by the character, inspect, dye, and fitting-room windows.
        foreach (var gameObject in objectTable.PlayerObjects)
        {
            if (gameObject is not IPlayerCharacter player || gameObject.GameObjectId == localPlayerId)
            {
                continue;
            }

            nearby++;
            if (player.OnlineStatus.RowId != AwayOnlineStatusId)
            {
                nonAway++;
            }
        }

        return new PlayerCounts(nearby, nonAway);
    }

    public static int Select(PlayerCounts counts, TriggerCountType countType) =>
        countType == TriggerCountType.NonAwayPlayers ? counts.NonAway : counts.Nearby;
}
