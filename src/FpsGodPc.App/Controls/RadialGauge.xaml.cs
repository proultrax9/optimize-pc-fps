using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FpsGodPc.App.Controls;

/// <summary>
/// HyperTune-style semicircular gauge. Draws a 180° track plus a value arc
/// (sampled as a polyline so it always sweeps over the top), with a centred
/// value/unit readout and a label underneath.
/// </summary>
public partial class RadialGauge : UserControl
{
    public RadialGauge()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
        SizeChanged += (_, _) => Redraw();
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RadialGauge), new PropertyMetadata(0d, OnVisualChanged));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(RadialGauge), new PropertyMetadata(0d, OnVisualChanged));
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RadialGauge), new PropertyMetadata(100d, OnVisualChanged));
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(string), typeof(RadialGauge), new PropertyMetadata(string.Empty, OnVisualChanged));
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(RadialGauge), new PropertyMetadata(string.Empty, OnVisualChanged));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(RadialGauge), new PropertyMetadata("0", OnVisualChanged));
    public string ValueFormat { get => (string)GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }

    public static readonly DependencyProperty ArcBrushProperty = DependencyProperty.Register(
        nameof(ArcBrush), typeof(Brush), typeof(RadialGauge), new PropertyMetadata(null, OnVisualChanged));
    public Brush? ArcBrush { get => (Brush?)GetValue(ArcBrushProperty); set => SetValue(ArcBrushProperty, value); }

    /// <summary>When true the arc colour follows value thresholds (green/amber/red).</summary>
    public static readonly DependencyProperty AutoColorProperty = DependencyProperty.Register(
        nameof(AutoColor), typeof(bool), typeof(RadialGauge), new PropertyMetadata(false, OnVisualChanged));
    public bool AutoColor { get => (bool)GetValue(AutoColorProperty); set => SetValue(AutoColorProperty, value); }

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(RadialGauge), new PropertyMetadata(14d, OnVisualChanged));
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RadialGauge)d).Redraw();

    private double Fraction()
    {
        var range = Maximum - Minimum;
        if (range <= 0) return 0;
        return Math.Clamp((Value - Minimum) / range, 0d, 1d);
    }

    private void Redraw()
    {
        ValueText.Text = Value.ToString(ValueFormat, CultureInfo.InvariantCulture);
        UnitText.Text = Unit;
        GaugeLabel.Text = Label;

        double w = ArcCanvas.ActualWidth;
        double h = ArcCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        double stroke = StrokeThickness;
        double pad = stroke / 2 + 4;
        double r = Math.Min(w / 2 - pad, h - pad);
        if (r <= 0) return;
        double cx = w / 2;
        double cy = h - pad;

        TrackPath.StrokeThickness = stroke;
        ValuePath.StrokeThickness = stroke;
        TrackPath.Data = BuildArc(cx, cy, r, 1d);

        double f = Fraction();
        ValuePath.Data = f <= 0 ? null : BuildArc(cx, cy, r, f);
        ValuePath.Stroke = AutoColor ? PickBrush(f) : (ArcBrush ?? PickBrush(f));

        Glow.Width = r * 1.7;
        Glow.Height = r * 1.1;
    }

    private Brush PickBrush(double f)
    {
        string key = f < 0.75 ? "AccentBrush" : f < 0.9 ? "WarningBrush" : "DangerBrush";
        return (Brush)FindResource(key);
    }

    /// <summary>Builds an upper-semicircle arc from 180° to (180° - fraction*180°) by sampling points.</summary>
    private static Geometry BuildArc(double cx, double cy, double r, double fraction)
    {
        const int steps = 64;
        double startAngle = 180d;
        double endAngle = 180d - fraction * 180d;

        var figure = new PathFigure { StartPoint = PointOnCircle(cx, cy, r, startAngle), IsClosed = false, IsFilled = false };
        var poly = new PolyLineSegment { IsStroked = true };
        for (int i = 1; i <= steps; i++)
        {
            double a = startAngle + (endAngle - startAngle) * (i / (double)steps);
            poly.Points.Add(PointOnCircle(cx, cy, r, a));
        }
        figure.Segments.Add(poly);

        var geo = new PathGeometry();
        geo.Figures.Add(figure);
        geo.Freeze();
        return geo;
    }

    private static Point PointOnCircle(double cx, double cy, double r, double angleDeg)
    {
        double a = angleDeg * Math.PI / 180d;
        return new Point(cx + r * Math.Cos(a), cy - r * Math.Sin(a));
    }
}
