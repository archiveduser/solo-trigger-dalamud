using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace SoloTrigger;

internal sealed class ProfileWindow : Window
{
    private readonly TriggerProfile profile;
    private readonly TriggerRuntime runtime;
    private readonly IObjectTable objectTable;

    public ProfileWindow(TriggerProfile profile, TriggerRuntime runtime, IObjectTable objectTable)
        : base($"Solo Trigger - {profile.Name}###{profile.Id}")
    {
        this.profile = profile;
        this.runtime = runtime;
        this.objectTable = objectTable;
        this.Size = new Vector2(360, 160);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        this.WindowName = $"Solo Trigger - {this.profile.Name}###{this.profile.Id}";
        var count = PlayerCounter.Select(PlayerCounter.Count(this.objectTable), this.profile.CountType);
        var countLabel = this.profile.CountType == TriggerCountType.NearbyPlayers
            ? "附近玩家数量"
            : "附近非离开玩家数量";

        ImGui.Text($"配置：{this.profile.Name}");
        ImGui.Text($"{countLabel}：{count}");
        ImGui.Text($"触发条件：人数 ≤ {this.profile.TriggerCount}");
        ImGui.Text($"状态：{this.runtime.StatusText}");
        ImGui.Separator();

        ImGui.BeginDisabled(this.runtime.IsRunning);
        if (ImGui.Button("开始", new Vector2(100, 0)))
        {
            this.runtime.Start(count);
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(!this.runtime.IsRunning);
        if (ImGui.Button("停止", new Vector2(100, 0)))
        {
            this.runtime.Stop();
        }

        ImGui.EndDisabled();
    }
}
