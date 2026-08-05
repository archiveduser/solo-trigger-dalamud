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
    private readonly IReadOnlySet<uint> majorAetheryteIds;

    public ProfileWindow(
        TriggerProfile profile,
        TriggerRuntime runtime,
        IObjectTable objectTable,
        IReadOnlySet<uint> majorAetheryteIds)
        : base($"Solo Trigger - {profile.Name}###{profile.Id}")
    {
        this.profile = profile;
        this.runtime = runtime;
        this.objectTable = objectTable;
        this.majorAetheryteIds = majorAetheryteIds;
        this.Size = new Vector2(360, 160);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        this.WindowName = $"Solo Trigger - {this.profile.Name}###{this.profile.Id}";
        var snapshot = PlayerCounter.Capture(this.objectTable, this.majorAetheryteIds);
        var count = PlayerCounter.Select(snapshot, this.profile.CountType);
        var countLabel = PlayerCounter.GetModeLabel(this.profile.CountType);

        ImGui.Text($"配置：{this.profile.Name}");
        ImGui.Text($"检测模式：{countLabel}");
        ImGui.Text($"当前判断人数：{count}");
        if (PlayerCounter.UsesAetheryteExclusion(this.profile.CountType) &&
            PlayerCounter.IsInsideMajorAetheryteRange(snapshot))
        {
            ImGui.TextDisabled("位于大水晶 125 yalms 内，人数按 0 判断。");
        }
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
