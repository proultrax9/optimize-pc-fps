using System;
using System.Windows;

namespace FpsGodPc.App.ViewModels;

public enum DialogKind { Info, Warning, Error, Question }

/// <summary>
/// Cross-assembly dialog hook. The App registers themed handlers at startup;
/// if none are set it falls back to a native MessageBox so the VMs always work.
/// </summary>
public static class DialogService
{
    public static Action<string, string, DialogKind>? AlertHandler;
    public static Func<string, string, DialogKind, bool>? ConfirmHandler;

    public static void Alert(string message, string title, DialogKind kind = DialogKind.Info)
    {
        if (AlertHandler is not null) { AlertHandler(message, title, kind); return; }
        MessageBox.Show(message, title, MessageBoxButton.OK, ToImage(kind));
    }

    public static bool Confirm(string message, string title, DialogKind kind = DialogKind.Warning)
    {
        if (ConfirmHandler is not null) return ConfirmHandler(message, title, kind);
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, ToImage(kind)) == MessageBoxResult.Yes;
    }

    public static DialogKind FromImage(MessageBoxImage image) => image switch
    {
        MessageBoxImage.Error => DialogKind.Error,
        MessageBoxImage.Question => DialogKind.Question,
        MessageBoxImage.Warning => DialogKind.Warning,
        _ => DialogKind.Info,
    };

    private static MessageBoxImage ToImage(DialogKind kind) => kind switch
    {
        DialogKind.Error => MessageBoxImage.Error,
        DialogKind.Question => MessageBoxImage.Question,
        DialogKind.Warning => MessageBoxImage.Warning,
        _ => MessageBoxImage.Information,
    };
}
