using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using Utf8String = FFXIVClientStructs.FFXIV.Client.System.String.Utf8String;

namespace SoloTrigger;

internal sealed class CommandDispatcher
{
    private static readonly TimeSpan CommandInterval = TimeSpan.FromMilliseconds(150);

    private readonly ICommandManager commandManager;
    private readonly IPluginLog log;
    private readonly List<QueuedCommand> queue = [];
    private DateTime nextCommandAt = DateTime.MinValue;

    public CommandDispatcher(ICommandManager commandManager, IPluginLog log)
    {
        this.commandManager = commandManager;
        this.log = log;
    }

    public void Enqueue(TriggerProfile profile, string configuredCommand, string actionName)
    {
        var command = configuredCommand.Trim();
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        if (!command.StartsWith('/'))
        {
            command = $"/{command}";
        }

        // If the condition flips again before this profile's previous command
        // has been sent, only the command for the newest state is relevant.
        this.queue.RemoveAll(item => item.ProfileId == profile.Id);
        this.queue.Add(new QueuedCommand(profile.Id, profile.Name, actionName, command));
        this.log.Information(
            "Profile {ProfileName}: queued {ActionName} command {Command}.",
            profile.Name,
            actionName,
            command);
    }

    public void Update()
    {
        var now = DateTime.UtcNow;
        if (this.queue.Count == 0 || now < this.nextCommandAt)
        {
            return;
        }

        var queuedCommand = this.queue[0];
        this.queue.RemoveAt(0);
        this.nextCommandAt = now + CommandInterval;
        this.Execute(queuedCommand);
    }

    public void RemoveProfile(Guid profileId) =>
        this.queue.RemoveAll(item => item.ProfileId == profileId);

    private unsafe void Execute(QueuedCommand queuedCommand)
    {
        try
        {
            if (this.commandManager.ProcessCommand(queuedCommand.Command))
            {
                this.log.Information(
                    "Profile {ProfileName}: dispatched {ActionName} plugin command {Command}.",
                    queuedCommand.ProfileName,
                    queuedCommand.ActionName,
                    queuedCommand.Command);
                return;
            }

            var uiModule = UIModule.Instance();
            var shellModule = RaptureShellModule.Instance();
            if (uiModule == null || shellModule == null)
            {
                this.log.Warning(
                    "Profile {ProfileName}: game UI is unavailable for {ActionName} command {Command}.",
                    queuedCommand.ProfileName,
                    queuedCommand.ActionName,
                    queuedCommand.Command);
                return;
            }

            var nativeCommand = Utf8String.FromString(queuedCommand.Command);
            if (nativeCommand == null)
            {
                this.log.Warning(
                    "Profile {ProfileName}: failed to allocate {ActionName} command {Command}.",
                    queuedCommand.ProfileName,
                    queuedCommand.ActionName,
                    queuedCommand.Command);
                return;
            }

            try
            {
                shellModule->ExecuteCommandInner(nativeCommand, uiModule);
                this.log.Information(
                    "Profile {ProfileName}: dispatched {ActionName} native command {Command}.",
                    queuedCommand.ProfileName,
                    queuedCommand.ActionName,
                    queuedCommand.Command);
            }
            finally
            {
                nativeCommand->Dtor(true);
            }
        }
        catch (Exception ex)
        {
            this.log.Error(
                ex,
                "Profile {ProfileName}: failed to execute {ActionName} command {Command}.",
                queuedCommand.ProfileName,
                queuedCommand.ActionName,
                queuedCommand.Command);
        }
    }

    private sealed record QueuedCommand(
        Guid ProfileId,
        string ProfileName,
        string ActionName,
        string Command);
}
