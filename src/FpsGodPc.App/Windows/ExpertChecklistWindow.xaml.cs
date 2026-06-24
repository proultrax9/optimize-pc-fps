using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Models;
using FpsGodPc.Core.Services;

namespace FpsGodPc.App.Windows;

public partial class ExpertChecklistWindow : Window
{
    private readonly AppServices _services;
    private readonly LocalizationService _l10n;

    public string? LastStatusMessage { get; private set; }

    public ExpertChecklistWindow(AppServices services, LocalizationService l10n)
    {
        _services = services;
        _l10n = l10n;
        InitializeComponent();
        ApplyStrings();
        BuildGuides();
        UpdateWaivedBanner();
    }

    private void ApplyStrings()
    {
        Title = _l10n.ExpertChecklistTitle;
        TitleText.Text = _l10n.ExpertChecklistTitle;
        WaiveButton.Content = _l10n.ExpertWaiveRisk;
        CompleteButton.Content = _l10n.ExpertMarkComplete;
        CloseButton.Content = _l10n.ExpertClose;
    }

    private void UpdateWaivedBanner()
    {
        var status = _services.GetExpertRiskStatus();
        if (status.Waived)
        {
            WaivedText.Text = _l10n.ExpertRiskWaived;
            WaivedText.Visibility = Visibility.Visible;
        }
    }

    private void BuildGuides()
    {
        GuidesList.Items.Clear();
        foreach (var guide in _services.GetExpertGuides())
        {
            GuidesList.Items.Add(BuildGuidePanel(guide));
        }
    }

    private UIElement BuildGuidePanel(ExpertGuide guide)
    {
        var border = new Border
        {
            Background = (Brush)FindResource("CardBrush"),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10),
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = _l10n.ExpertGuideTitle(guide.Id),
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("TextBrush"),
        });

        stack.Children.Add(new TextBlock
        {
            Text = _l10n.ExpertGuideSummary(guide.Id),
            Margin = new Thickness(0, 6, 0, 8),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            FontSize = 12,
        });

        if (!string.IsNullOrWhiteSpace(guide.Warning))
        {
            var warning = _l10n.ExpertGuideWarning(guide.Id);
            if (!string.IsNullOrWhiteSpace(warning))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = warning,
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)FindResource("WarningBrush"),
                    FontSize = 11,
                });
            }
        }

        var steps = _l10n.ExpertGuideSteps(guide.Id);
        for (var i = 0; i < steps.Count; i++)
        {
            stack.Children.Add(new TextBlock
            {
                Text = $"{i + 1}. {steps[i]}",
                Margin = new Thickness(0, 2, 0, 2),
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("TextBrush"),
                FontSize = 12,
            });
        }

        border.Child = stack;
        return border;
    }

    private void WaiveButton_Click(object sender, RoutedEventArgs e)
    {
        var msg = _l10n.T(
            "Advanced tuning can cause instability or data loss. Accept responsibility and continue?",
            "การปรับขั้นสูงอาจทำให้ไม่เสถียรหรือสูญเสียข้อมูล ยอมรับความเสี่ยงและดำเนินการต่อไหม?");
        if (!ThemedDialog.Confirm(this, msg, _l10n.ConfirmTitle(), FpsGodPc.App.ViewModels.DialogKind.Warning))
        {
            return;
        }

        _services.WaiveExpertRisk();
        LastStatusMessage = _l10n.ExpertRiskWaived;
        UpdateWaivedBanner();
    }

    private void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        var status = _services.GetExpertRiskStatus();
        if (!status.Waived)
        {
            var msg = _l10n.T(
                "Mark expert guides complete only after following the steps or accepting risk. Waive risk first?",
                "ทำเครื่องหมายว่าทำครบหลังทำตามขั้นตอนหรือยอมรับความเสี่ยง ยอมรับความเสี่ยงก่อนไหม?");
            if (ThemedDialog.Confirm(this, msg, _l10n.ConfirmTitle(), FpsGodPc.App.ViewModels.DialogKind.Question))
            {
                _services.WaiveExpertRisk();
            }
            else
            {
                return;
            }
        }

        var result = _services.ApplyBoostPreset("expert");
        LastStatusMessage = _l10n.LocalizeResult(result.Message);
        ThemedDialog.Alert(this, LastStatusMessage, _l10n.ExpertChecklistTitle, FpsGodPc.App.ViewModels.DialogKind.Info);
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
