namespace SoloTrigger;

internal sealed class TriggerRuntime
{
    private readonly TriggerProfile profile;
    private readonly CommandDispatcher commandDispatcher;
    private bool? lastCondition;

    public TriggerRuntime(TriggerProfile profile, CommandDispatcher commandDispatcher)
    {
        this.profile = profile;
        this.commandDispatcher = commandDispatcher;
    }

    public bool IsRunning { get; private set; }

    public string StatusText { get; private set; } = "已停止";

    public void Start(int currentCount)
    {
        if (this.IsRunning)
        {
            return;
        }

        this.IsRunning = true;
        this.lastCondition = null;
        this.Evaluate(currentCount);
    }

    public void Stop()
    {
        if (!this.IsRunning)
        {
            return;
        }

        this.IsRunning = false;
        this.lastCondition = null;
        this.Execute(this.profile.EndCommand, "结束");
        this.StatusText = "已停止";
    }

    public void Evaluate(int currentCount)
    {
        if (!this.IsRunning)
        {
            return;
        }

        var shouldStart = currentCount <= this.profile.TriggerCount;
        if (this.lastCondition == shouldStart)
        {
            return;
        }

        this.lastCondition = shouldStart;
        if (shouldStart)
        {
            this.Execute(this.profile.StartCommand, "开始");
            this.StatusText = $"已触发开始（{currentCount} ≤ {this.profile.TriggerCount}）";
        }
        else
        {
            this.Execute(this.profile.EndCommand, "结束");
            this.StatusText = $"已触发结束（{currentCount} > {this.profile.TriggerCount}）";
        }
    }

    private void Execute(string configuredCommand, string actionName)
    {
        this.commandDispatcher.Enqueue(this.profile, configuredCommand, actionName);
    }
}
