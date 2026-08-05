using Dalamud.Configuration;

namespace SoloTrigger;

public enum PlayerCountMode
{
    AllPlayers,
    NonAwayPlayers,
    AllPlayersAwayFromAetheryte,
    NonAwayPlayersAwayFromAetheryte,
}

public sealed class TriggerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "新配置";

    public PlayerCountMode CountType { get; set; }

    public int TriggerCount { get; set; }

    public string StartCommand { get; set; } = string.Empty;

    public string EndCommand { get; set; } = string.Empty;
}

public sealed class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public List<TriggerProfile> Profiles { get; set; } = [];
}
