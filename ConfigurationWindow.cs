using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SoloTrigger;

internal sealed class ConfigurationWindow : Window
{
    private readonly Plugin plugin;

    public ConfigurationWindow(Plugin plugin)
        : base("Solo Trigger 配置###SoloTriggerConfiguration")
    {
        this.plugin = plugin;
        this.Size = new Vector2(620, 480);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        ImGui.TextDisabled("/solotrigger <配置名> 可直接打开对应配置窗口");
        if (ImGui.Button("添加配置"))
        {
            this.plugin.AddProfile();
        }

        ImGui.Separator();

        TriggerProfile? profileToDelete = null;
        foreach (var profile in this.plugin.Configuration.Profiles)
        {
            ImGui.PushID(profile.Id.ToString());
            var runtime = this.plugin.GetProfileRuntime(profile);
            var runningText = runtime?.IsRunning == true ? "运行中" : "已停止";
            var detailText = runtime?.StatusText ?? "不可用";
            if (ImGui.BeginTable("profileHeader", 2, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("信息", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthFixed, 110);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted($"{profile.Name}｜监控状态：{runningText}｜触发状态：{detailText}");

                ImGui.TableSetColumnIndex(1);
                if (ImGui.SmallButton("查看"))
                {
                    this.plugin.ToggleProfileWindow(profile);
                }

                ImGui.SameLine();
                var shiftHeld = ImGui.GetIO().KeyShift;
                ImGui.BeginDisabled(!shiftHeld);
                if (ImGui.SmallButton("删除"))
                {
                    profileToDelete = profile;
                }

                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("按住 Shift 点击删除此配置");
                }

                ImGui.EndTable();
            }

            var headerOpen = ImGui.CollapsingHeader("配置详情##profile", ImGuiTreeNodeFlags.DefaultOpen);
            if (headerOpen)
            {
                this.DrawProfileEditor(profile);
            }

            ImGui.Separator();
            ImGui.PopID();
        }

        if (profileToDelete is not null)
        {
            this.plugin.DeleteProfile(profileToDelete);
        }

        if (this.plugin.Configuration.Profiles.Count == 0)
        {
            ImGui.TextDisabled("尚未创建配置。点击“添加配置”开始。");
        }
    }

    private void DrawProfileEditor(TriggerProfile profile)
    {
        ImGui.Indent();
        ImGui.SetNextItemWidth(280);
        var name = profile.Name;
        if (ImGui.InputText("名称", ref name, 100))
        {
            if (this.plugin.CanUseProfileName(profile, name))
            {
                profile.Name = name.Trim();
                this.plugin.SaveConfiguration();
            }
        }

        ImGui.TextDisabled("名称不能为空，且不能与其他配置重复。");

        ImGui.SetNextItemWidth(280);
        var typeLabel = PlayerCounter.GetModeLabel(profile.CountType);
        if (ImGui.BeginCombo("检测模式", typeLabel))
        {
            foreach (var mode in Enum.GetValues<PlayerCountMode>())
            {
                if (ImGui.Selectable(PlayerCounter.GetModeLabel(mode), profile.CountType == mode))
                {
                    profile.CountType = mode;
                    this.plugin.SaveConfiguration();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SetNextItemWidth(280);
        var triggerCount = profile.TriggerCount;
        if (ImGui.InputInt("触发人数", ref triggerCount))
        {
            profile.TriggerCount = Math.Max(0, triggerCount);
            this.plugin.SaveConfiguration();
        }

        ImGui.SetNextItemWidth(420);
        var startCommand = profile.StartCommand;
        if (ImGui.InputTextWithHint("开始命令", "/命令（可留空）", ref startCommand, 500))
        {
            profile.StartCommand = startCommand;
            this.plugin.SaveConfiguration();
        }

        ImGui.SetNextItemWidth(420);
        var endCommand = profile.EndCommand;
        if (ImGui.InputTextWithHint("结束命令", "/命令（可留空）", ref endCommand, 500))
        {
            profile.EndCommand = endCommand;
            this.plugin.SaveConfiguration();
        }

        ImGui.TextDisabled("运行时：人数 ≤ 触发人数执行开始命令，否则执行结束命令。");
        ImGui.Unindent();
    }
}
