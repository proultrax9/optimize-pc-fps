using CommunityToolkit.Mvvm.ComponentModel;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using System.Windows;

namespace FpsGodPc.App.ViewModels;

public abstract partial class PageViewModelBase : ViewModelBase
{
    private readonly string _pageKey;

    protected PageViewModelBase(AppServices services, LocalizationService l10n, string pageKey)
    {
        Services = services;
        L10n = l10n;
        _pageKey = pageKey;
        L10n.LanguageChanged += OnLanguageChanged;
        ApplyPageStrings();
    }

    protected AppServices Services { get; }
    protected LocalizationService L10n { get; }

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private string? statusMessage;

    protected virtual void ApplyPageStrings()
    {
        var (title, subtitle) = L10n.Page(_pageKey);
        Title = title;
        Subtitle = subtitle;
    }

    protected bool Confirm(string message, string? title = null, MessageBoxImage icon = MessageBoxImage.Warning)
    {
        return MessageBox.Show(message, title ?? L10n.ConfirmTitle(), MessageBoxButton.YesNo, icon) == MessageBoxResult.Yes;
    }

    private void OnLanguageChanged() => ApplyPageStrings();
}
