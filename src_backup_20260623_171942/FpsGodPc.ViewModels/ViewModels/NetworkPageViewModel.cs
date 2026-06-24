using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;

namespace FpsGodPc.App.ViewModels;

public partial class NetworkPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "network")
{
    [ObservableProperty]
    private string host = "8.8.8.8";

    [ObservableProperty]
    private string pingHost = "—";

    [ObservableProperty]
    private string pingLatency = "—";

    [ObservableProperty]
    private string pingPacketLoss = "—";

    [ObservableProperty]
    private string pingMessage = string.Empty;

    [ObservableProperty]
    private string flushDnsLabel = string.Empty;

    [ObservableProperty]
    private string tuneAdapterLabel = string.Empty;

    [ObservableProperty]
    private string godModePowerPlanLabel = string.Empty;

    [ObservableProperty]
    private string pingTestLabel = string.Empty;

    [ObservableProperty]
    private string hostPrefix = string.Empty;

    [ObservableProperty]
    private string latencyPrefix = string.Empty;

    [ObservableProperty]
    private string packetLossPrefix = string.Empty;

    [ObservableProperty]
    private string powerPlanStatus = "Balanced (Windows default)";

    [ObservableProperty]
    private bool isPowerGodModeActive;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        FlushDnsLabel = L10n.NetworkFlushDns;
        TuneAdapterLabel = L10n.NetworkTuneAdapter;
        GodModePowerPlanLabel = L10n.NetworkGodModePowerPlan;
        PingTestLabel = L10n.NetworkPingTest;
        HostPrefix = L10n.NetworkHostPrefix;
        LatencyPrefix = L10n.NetworkLatencyPrefix;
        PacketLossPrefix = L10n.NetworkPacketLossPrefix;
        if (PingMessage is "No test yet." or "ยังไม่ได้ทดสอบ" or "")
        {
            PingMessage = L10n.NetworkNoTestYet;
        }
    }

    [RelayCommand]
    public async Task FlushDns()
    {
        var result = await Task.Run(() => Services.SetTweakState("net-dns-flush", true));
        StatusMessage = result.Message;
    }

    [RelayCommand]
    public async Task TuneAdapter()
    {
        var result = await Task.Run(() => Services.SetTweakState("net-adapter-power", true));
        StatusMessage = result.Message;
    }

    [RelayCommand]
    public async Task ApplyGodModePowerPlan()
    {
        var result = await Task.Run(() => Services.ApplyGodModePowerPlan());
        StatusMessage = result.Message;
        IsPowerGodModeActive = result.Success;
        PowerPlanStatus = result.Success
            ? "God Mode — max performance, display stays on"
            : result.Message;
    }

    [RelayCommand]
    public async Task RunPing()
    {
        var host = Host;
        var result = await Task.Run(() => Services.RunPing(host));
        PingHost = result.Host;
        PingLatency = result.LatencyMs is null ? "—" : $"{result.LatencyMs:F1} ms";
        PingPacketLoss = result.PacketLoss is null ? "—" : $"{result.PacketLoss:F0}%";
        PingMessage = result.Message;
    }
}
