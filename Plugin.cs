using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace SoloTrigger;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/solotrigger";
    private const string DebugCommandName = "/solotriggerdebug";

    private readonly WindowSystem windowSystem = new("SoloTrigger");
    private readonly Dictionary<Guid, TriggerRuntime> runtimes = [];
    private readonly Dictionary<Guid, ProfileWindow> profileWindows = [];
    private readonly CommandDispatcher commandDispatcher;
    private readonly ConfigurationWindow configurationWindow;
    private readonly DebugWindow debugWindow;
    private DateTime nextEvaluationAt = DateTime.MinValue;

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    internal PluginConfiguration Configuration { get; }

    public Plugin()
    {
        this.Configuration = PluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        this.RepairConfiguration();
        this.commandDispatcher = new CommandDispatcher(CommandManager, Log);

        this.configurationWindow = new ConfigurationWindow(this);
        this.debugWindow = new DebugWindow(ObjectTable);
        this.windowSystem.AddWindow(this.configurationWindow);
        this.windowSystem.AddWindow(this.debugWindow);
        foreach (var profile in this.Configuration.Profiles)
        {
            this.CreateProfileRuntime(profile);
        }

        CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "打开配置页；使用 /solotrigger <配置名> 打开对应配置窗口。",
        });
        CommandManager.AddHandler(DebugCommandName, new CommandInfo(this.OnDebugCommand)
        {
            HelpMessage = "打开 Solo Trigger Debug 窗口。",
        });

        PluginInterface.UiBuilder.Draw += this.DrawUi;
        PluginInterface.UiBuilder.OpenMainUi += this.ToggleConfigurationWindow;
        Framework.Update += this.OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Framework.Update -= this.OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= this.DrawUi;
        PluginInterface.UiBuilder.OpenMainUi -= this.ToggleConfigurationWindow;
        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(DebugCommandName);
        this.windowSystem.RemoveAllWindows();
    }

    internal void AddProfile()
    {
        var index = 1;
        string name;
        do
        {
            name = $"配置 {index++}";
        }
        while (this.Configuration.Profiles.Any(profile => profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        var newProfile = new TriggerProfile { Name = name };
        this.Configuration.Profiles.Add(newProfile);
        this.CreateProfileRuntime(newProfile);
        this.SaveConfiguration();
    }

    internal void DeleteProfile(TriggerProfile profile)
    {
        if (this.runtimes.Remove(profile.Id, out var runtime))
        {
            runtime.Stop();
        }

        this.commandDispatcher.RemoveProfile(profile.Id);

        if (this.profileWindows.Remove(profile.Id, out var window))
        {
            this.windowSystem.RemoveWindow(window);
        }

        this.Configuration.Profiles.Remove(profile);
        this.SaveConfiguration();
    }

    internal bool CanUseProfileName(TriggerProfile current, string name)
    {
        var trimmed = name.Trim();
        return !string.IsNullOrEmpty(trimmed) &&
               !this.Configuration.Profiles.Any(profile =>
                   profile.Id != current.Id && profile.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    internal void OpenProfile(TriggerProfile profile)
    {
        if (this.profileWindows.TryGetValue(profile.Id, out var window))
        {
            window.IsOpen = true;
        }
    }

    internal void ToggleProfileWindow(TriggerProfile profile)
    {
        if (this.profileWindows.TryGetValue(profile.Id, out var window))
        {
            window.Toggle();
        }
    }

    internal bool IsProfileWindowOpen(TriggerProfile profile) =>
        this.profileWindows.TryGetValue(profile.Id, out var window) && window.IsOpen;

    internal TriggerRuntime? GetProfileRuntime(TriggerProfile profile) =>
        this.runtimes.GetValueOrDefault(profile.Id);

    internal void SaveConfiguration() => PluginInterface.SavePluginConfig(this.Configuration);

    private void RepairConfiguration()
    {
        var usedIds = new HashSet<Guid>();
        foreach (var profile in this.Configuration.Profiles)
        {
            if (profile.Id == Guid.Empty || !usedIds.Add(profile.Id))
            {
                profile.Id = Guid.NewGuid();
                usedIds.Add(profile.Id);
            }

            profile.Name = string.IsNullOrWhiteSpace(profile.Name) ? "未命名配置" : profile.Name.Trim();
            profile.TriggerCount = Math.Max(0, profile.TriggerCount);
            profile.StartCommand ??= string.Empty;
            profile.EndCommand ??= string.Empty;
        }
    }

    private void CreateProfileRuntime(TriggerProfile profile)
    {
        var runtime = new TriggerRuntime(profile, this.commandDispatcher);
        var window = new ProfileWindow(profile, runtime, ObjectTable);
        this.runtimes[profile.Id] = runtime;
        this.profileWindows[profile.Id] = window;
        this.windowSystem.AddWindow(window);
    }

    private void OnCommand(string command, string args)
    {
        var requestedName = args.Trim();
        if (string.IsNullOrEmpty(requestedName))
        {
            this.ToggleConfigurationWindow();
            return;
        }

        var profile = this.Configuration.Profiles.FirstOrDefault(candidate =>
            candidate.Name.Equals(requestedName, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
        {
            ChatGui.PrintError($"[Solo Trigger] 未找到配置：{requestedName}");
            return;
        }

        this.OpenProfile(profile);
    }

    private void OnDebugCommand(string command, string args) => this.debugWindow.Toggle();

    private void OnFrameworkUpdate(IFramework framework)
    {
        this.commandDispatcher.Update();

        var now = DateTime.UtcNow;
        if (now < this.nextEvaluationAt)
        {
            return;
        }

        this.nextEvaluationAt = now.AddMilliseconds(250);
        var counts = PlayerCounter.Count(ObjectTable);
        foreach (var (profileId, runtime) in this.runtimes)
        {
            var profile = this.Configuration.Profiles.FirstOrDefault(candidate => candidate.Id == profileId);
            if (profile is not null && runtime.IsRunning)
            {
                runtime.Evaluate(PlayerCounter.Select(counts, profile.CountType));
            }
        }
    }

    private void ToggleConfigurationWindow() => this.configurationWindow.Toggle();

    private void DrawUi() => this.windowSystem.Draw();
}
