using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FpsGodPc.App.Controls;

/// <summary>Linear meter (temperature / utilisation) with a threshold-coloured fill.</summary>
public partial class TempBar : UserControl
{
    public TempBar()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
        Track.SizeChanged += (_, _) => Redraw();
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(TempBar), new PropertyMetadata(0d, OnVisualChanged));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(TempBar), new PropertyMetadata(100d, OnVisualChanged));
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(TempBar), new PropertyMetadata(string.Empty, OnVisualChanged));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(
        nameof(Suffix), typeof(string), typeof(TempBar), new PropertyMetadata(string.Empty, OnVisualChanged));
    public string Suffix { get => (string)GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }

    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(TempBar), new PropertyMetadata("0", OnVisualChanged));
    public string ValueFormat { get => (string)GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }

    /// <summary>Fixed fill colour; when null the fill follows value thresholds.</summary>
    public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register(
        nameof(FillBrush), typeof(Brush), typeof(TempBar), new PropertyMetadata(null, OnVisualChanged));
    public Brush? FillBrush { get => (Brush?)GetValue(FillBrushProperty); set => SetValue(FillBrushProperty, value); }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TempBar)d).Redraw();

    private void Redraw()
    {
        double frac = Maximum > 0 ? Math.Clamp(Value / Maximum, 0d, 1d) : 0d;
        ValueText.Text = Value.ToString(ValueFormat, CultureInfo.InvariantCulture) + Suffix;
        BarLabel.Text = Label;

        double trackWidth = Track.ActualWidth;
        Fill.Width = trackWidth > 0 ? trackWidth * frac : 0d;
        Fill.Background = FillBrush ?? PickBrush(frac);
    }

    private Brush PickBrush(double f)
    {
        string key = f < 0.7 ? "AccentBrush" : f < 0.85 ? "WarningBrush" : "DangerBrush";
        return (Brush)FindResource(key);
    }
}
