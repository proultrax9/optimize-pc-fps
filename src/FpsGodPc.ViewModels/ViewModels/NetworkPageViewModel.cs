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
    private string pingTestLabel = string.Empty;

    [ObservableProperty]
    private string hostPrefix = string.Empty;

    [ObservableProperty]
    private string latencyPrefix = string.Empty;

    [ObservableProperty]
    private string packetLossPrefix = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        FlushDnsLabel = L10n.NetworkFlushDns;
        TuneAdapterLabel = L10n.NetworkTuneAdapter;
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
    public void FlushDns()
    {
        StatusMessage = Services.SetTweakState("net-dns-flush", true).Message;
    }

    [RelayCommand]
    public void TuneAdapter()
    {
        StatusMessage = Services.SetTweakState("net-adapter-power", true).Message;
    }

    [RelayCommand]
    public void RunPing()
    {
        var result = Services.RunPing(Host);
        PingHost = result.Host;
        PingLatency = result.LatencyMs is null ? "—" : $"{result.LatencyMs:F1} ms";
        PingPacketLoss = result.PacketLoss is null ? "—" : $"{result.PacketLoss:F0}%";
        PingMessage = result.Message;
    }
}
