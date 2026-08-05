using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace SoloTrigger;

internal sealed class DebugWindow : Window
{
    private readonly IObjectTable objectTable;
    private readonly IReadOnlySet<uint> majorAetheryteIds;

    public DebugWindow(IObjectTable objectTable, IReadOnlySet<uint> majorAetheryteIds)
        : base("Solo Trigger Debug###SoloTriggerDebug")
    {
        this.objectTable = objectTable;
        this.majorAetheryteIds = majorAetheryteIds;
        this.Size = new Vector2(360, 150);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var snapshot = PlayerCounter.Capture(this.objectTable, this.majorAetheryteIds);
        ImGui.Text($"所有玩家：{snapshot.AllPlayers}");
        ImGui.Text($"非离开玩家：{snapshot.NonAwayPlayers}");

        var nearestAetheryteDistance = snapshot.NearestMajorAetheryteHorizontalDistance;
        if (nearestAetheryteDistance is null)
        {
            ImGui.Text("最近大水晶水平距离：不可用");
            ImGui.TextDisabled("当前区域未加载大水晶，或玩家尚未登录。");
        }
        else
        {
            ImGui.Text($"最近大水晶水平距离：{nearestAetheryteDistance.Value:F1} yalms");
            ImGui.Text($"100 yalms 排除范围：{(nearestAetheryteDistance.Value <= PlayerCounter.MajorAetheryteExclusionRange ? "是" : "否")}");
        }
    }
}
