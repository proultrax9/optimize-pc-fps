using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Core.Models;

namespace FpsGodPc.App.ViewModels;

public partial class CleanerPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "cleaner")
{
    [ObservableProperty]
    private bool tempFiles = true;

    [ObservableProperty]
    private bool shaderCache = true;

    [ObservableProperty]
    private bool dnsCache;

    [ObservableProperty]
    private bool recycleBin;

    [ObservableProperty]
    private string tempFilesLabel = string.Empty;

    [ObservableProperty]
    private string shaderCacheLabel = string.Empty;

    [ObservableProperty]
    private string dnsCacheLabel = string.Empty;

    [ObservableProperty]
    private string recycleBinLabel = string.Empty;

    [ObservableProperty]
    private string runCleanerLabel = string.Empty;

    protected override void ApplyPageStrings()
    {
        base.ApplyPageStrings();
        TempFilesLabel = L10n.CleanerTempFiles;
        ShaderCacheLabel = L10n.CleanerShaderCache;
        DnsCacheLabel = L10n.CleanerDnsCache;
        RecycleBinLabel = L10n.CleanerRecycleBin;
        RunCleanerLabel = L10n.CleanerRun;
    }

    [RelayCommand]
    public void RunCleaner()
    {
        var options = new CleanOptions
        {
            TempFiles = TempFiles,
            ShaderCache = ShaderCache,
            DnsCache = DnsCache,
            RecycleBin = RecycleBin,
        };
        StatusMessage = Services.RunCleaner(options).Message;
    }
}
