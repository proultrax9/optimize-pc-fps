using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FpsGodPc.App.ViewModels;

namespace FpsGodPc.App.Windows;

/// <summary>HyperTune-style dark dialog used in place of the native MessageBox.</summary>
public partial class ThemedDialog : Window
{
    private ThemedDialog() => InitializeComponent();

    public static void Alert(Window? owner, string message, string title, DialogKind kind)
        => Create(owner, message, title, kind, confirm: false).ShowDialog();

    public static bool Confirm(Window? owner, string message, string title, DialogKind kind)
        => Create(owner, message, title, kind, confirm: true).ShowDialog() == true;

    private static ThemedDialog Create(Window? owner, string message, string title, DialogKind kind, bool confirm)
    {
        var d = new ThemedDialog();
        if (owner is not null && !ReferenceEquals(owner, d) && owner.IsLoaded)
        {
            d.Owner = owner;
            d.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            d.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        d.TitleText.Text = title;
        d.MessageText.Text = message;
        d.ApplyKind(kind);

        if (confirm)
        {
            d.PrimaryBtn.Content = "Yes";
            d.SecondaryBtn.Visibility = Visibility.Visible;
        }

        return d;
    }

    private void ApplyKind(DialogKind kind)
    {
        var (glyph, brushKey) = kind switch
        {
            DialogKind.Error => ("✕", "DangerBrush"),
            DialogKind.Warning => ("!", "WarningBrush"),
            DialogKind.Question => ("?", "AccentBrush"),
            _ => ("i", "AccentBrush"),
        };

        IconGlyph.Text = glyph;
        var fg = (Brush)FindResource(brushKey);
        IconGlyph.Foreground = fg;

        if (fg is SolidColorBrush scb)
        {
            var c = scb.Color;
            IconBadge.Background = new SolidColorBrush(Color.FromArgb(0x22, c.R, c.G, c.B));
        }
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void Primary_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Secondary_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
