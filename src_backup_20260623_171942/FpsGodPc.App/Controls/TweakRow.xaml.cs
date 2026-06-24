using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FpsGodPc.App.Controls;

public partial class TweakRow : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TweakRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(TweakRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RiskProperty =
        DependencyProperty.Register(nameof(Risk), typeof(string), typeof(TweakRow),
            new PropertyMetadata(string.Empty, OnRiskChanged));

    public static readonly DependencyProperty IsToggledProperty =
        DependencyProperty.Register(nameof(IsToggled), typeof(bool), typeof(TweakRow), new PropertyMetadata(false));

    public static readonly DependencyProperty ToggleCommandProperty =
        DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(TweakRow), new PropertyMetadata(null));

    public TweakRow()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyRiskBadgeStyle();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string Risk
    {
        get => (string)GetValue(RiskProperty);
        set => SetValue(RiskProperty, value);
    }

    public bool IsToggled
    {
        get => (bool)GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    public ICommand? ToggleCommand
    {
        get => (ICommand?)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    private static void OnRiskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TweakRow row)
        {
            row.ApplyRiskBadgeStyle();
        }
    }

    private void ApplyRiskBadgeStyle()
    {
        if (RiskBadge is null)
        {
            return;
        }

        var (bg, border, fg) = Risk switch
        {
            "Moderate" or "ปานกลาง" or "MEDIUM" => ("#1AFBBF24", "#4DFBBF24", "#FFFBBF24"),
            "Advanced" or "ขั้นสูง" or "HIGH" or "EXTREME" => ("#1AF87171", "#4DF87171", "#FFF87171"),
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
