using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace SoloTrigger;

internal sealed class DebugWindow : Window
{
    private readonly IObjectTable objectTable;

    public DebugWindow(IObjectTable objectTable)
        : base("Solo Trigger Debug###SoloTriggerDebug")
    {
        this.objectTable = objectTable;
        this.Size = new Vector2(360, 150);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var counts = PlayerCounter.Count(this.objectTable);
        ImGui.Text($"附近玩家：{counts.Nearby}");
        ImGui.Text($"非离开玩家：{counts.NonAway}");

        var nearestAetheryteDistance = this.GetNearestAetheryteDistance();
        if (nearestAetheryteDistance is null)
        {
            ImGui.Text("最近传送水晶距离：不可用");
            ImGui.TextDisabled("当前区域未加载传送水晶，或玩家尚未登录。");
        }
        else
        {
            ImGui.Text($"最近传送水晶距离：{nearestAetheryteDistance.Value:F1} yalms");
        }
    }

    private float? GetNearestAetheryteDistance()
    {
        var localPlayer = this.objectTable.LocalPlayer;
        if (localPlayer is null)
        {
            return null;
        }

        float? nearestDistance = null;
        foreach (var gameObject in this.objectTable)
        {
            if (gameObject.ObjectKind != ObjectKind.Aetheryte)
            {
                continue;
            }

            var distance = Vector3.Distance(localPlayer.Position, gameObject.Position);
            if (nearestDistance is null || distance < nearestDistance.Value)
            {
                nearestDistance = distance;
            }
        }

        return nearestDistance;
    }
}
