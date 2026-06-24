using CommunityToolkit.Mvvm.ComponentModel;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using System.Windows;

namespace FpsGodPc.App.ViewModels;

public abstract partial class PageViewModelBase : ViewModelBase, IDisposable
{
    private readonly string _pageKey;
    private bool _disposed;

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
        return DialogService.Confirm(message, title ?? L10n.ConfirmTitle(), DialogService.FromImage(icon));
    }

    private void OnLanguageChanged() => ApplyPageStrings();

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            L10n.LanguageChanged -= OnLanguageChanged;
        }

        _disposed = true;
    }
}
