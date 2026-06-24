using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FpsGodPc.Core.Models;

namespace FpsGodPc.App.Controls;

public partial class BoostCard : UserControl
{
    public static readonly DependencyProperty PresetProperty =
        DependencyProperty.Register(nameof(Preset), typeof(PresetBundle), typeof(BoostCard),
            new PropertyMetadata(null, OnPresetChanged));

    public static readonly DependencyProperty ApplyCommandProperty =
        DependencyProperty.Register(nameof(ApplyCommand), typeof(ICommand), typeof(BoostCard), new PropertyMetadata(null));

    public static readonly DependencyProperty MoreTweaksLabelProperty =
        DependencyProperty.Register(nameof(MoreTweaksLabel), typeof(string), typeof(BoostCard), new PropertyMetadata("more"));

    public static readonly DependencyProperty RestoreRecommendedLabelProperty =
        DependencyProperty.Register(nameof(RestoreRecommendedLabel), typeof(string), typeof(BoostCard), new PropertyMetadata(string.Empty));

    public BoostCard()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyRiskBadgeStyle();
    }

    public PresetBundle? Preset
    {
        get => (PresetBundle?)GetValue(PresetProperty);
        set => SetValue(PresetProperty, value);
    }

    public ICommand? ApplyCommand
    {
        get => (ICommand?)GetValue(ApplyCommandProperty);
        set => SetValue(ApplyCommandProperty, value);
    }

    public string MoreTweaksLabel
    {
        get => (string)GetValue(MoreTweaksLabelProperty);
        set => SetValue(MoreTweaksLabelProperty, value);
    }

    public string RestoreRecommendedLabel
    {
        get => (string)GetValue(RestoreRecommendedLabelProperty);
        set => SetValue(RestoreRecommendedLabelProperty, value);
    }

    private static void OnPresetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BoostCard card)
        {
            card.ApplyRiskBadgeStyle();
        }
    }

    private void ApplyRiskBadgeStyle()
    {
        if (RiskBadge is null || Preset is null)
        {
            return;
        }

        var (bg, border, fg) = Preset.RiskLevel switch
        {
            "medium" => ("#1AFBBF24", "#4DFBBF24", "#FFFBBF24"),
            "high" => ("#1AF87171", "#4DF87171", "#FFF87171"),
            "extreme" => ("#26F87171", "#80F87171", "#FFF87171"),
            _ => ("#1A34D399", "#4D34D399", "#FF34D399"),
        };

        RiskBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)!);
        RiskBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border)!);
        RiskBadge.BorderThickness = new Thickness(1);
        if (RiskBadge.Child is TextBlock tb)
        {
            tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)!);
        }
    }
}
