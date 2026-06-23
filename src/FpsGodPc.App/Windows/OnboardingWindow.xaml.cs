using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Services;
using FpsGodPc.Services;

namespace FpsGodPc.App.Windows;

public partial class OnboardingWindow : Window
{
    private readonly AppServices _services;
    private readonly LocalizationService _l10n;

    public OnboardingWindow(AppServices services, LocalizationService l10n)
    {
        _services = services;
        _l10n = l10n;
        InitializeComponent();
        Title = _l10n.OnboardingTitle;
        TitleText.Text = _l10n.OnboardingTitle;
        GotItButton.Content = _l10n.OnboardingGotIt;
        BuildBullets();
    }

    private void BuildBullets()
    {
        foreach (var bullet in _l10n.OnboardingBullets)
        {
            BulletsList.Items.Add(new TextBlock
            {
                Text = $"• {bullet}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontSize = 13,
            });
        }
    }

    private void GotItButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = _services.GetSafetySettings();
        settings.OnboardingComplete = true;
        _services.SaveSafetySettings(settings);
        DialogResult = true;
        Close();
    }
}
