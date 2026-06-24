using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FpsGodPc.App.Controls;

/// <summary>
/// Lightweight sparkline / area graph. Bind <see cref="Values"/> to an
/// ObservableCollection&lt;double&gt; (or any IEnumerable of numbers); the
/// graph redraws on collection changes and resize.
/// </summary>
public partial class LiveGraph : UserControl
{
    public LiveGraph()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
        Host.SizeChanged += (_, _) => Redraw();
    }

    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IEnumerable), typeof(LiveGraph), new PropertyMetadata(null, OnValuesChanged));
    public IEnumerable? Values { get => (IEnumerable?)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(LiveGraph), new PropertyMetadata(0d, OnVisualChanged));
    /// <summary>Fixed Y maximum; 0 = auto-scale to data.</summary>
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((LiveGraph)d).Redraw();

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var g = (LiveGraph)d;
        if (e.OldValue is INotifyCollectionChanged oldColl)
            oldColl.CollectionChanged -= g.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newColl)
            newColl.CollectionChanged += g.OnCollectionChanged;
        g.Redraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        Line.Points = new PointCollection();
        AreaPath.Data = null;
        if (Values is null) return;

        var data = new List<double>();
        foreach (var item in Values)
        {
            try { data.Add(Convert.ToDouble(item)); } catch { /* skip non-numeric */ }
        }
        if (data.Count < 2) return;

        double w = Host.ActualWidth, h = Host.ActualHeight;
        if (w <= 0 || h <= 0) return;

        double max = Maximum > 0 ? Maximum : double.MinValue;
        double min = 0;
        if (Maximum <= 0)
        {
            foreach (var v in data) if (v > max) max = v;
            if (max <= 0) max = 1;
        }
        double range = max - min;
        if (range <= 0) range = 1;

        const double pad = 2;
        var pts = new PointCollection(data.Count);
        for (int i = 0; i < data.Count; i++)
        {
            double x = data.Count == 1 ? 0 : i / (double)(data.Count - 1) * w;
            double norm = Math.Clamp((data[i] - min) / range, 0d, 1d);
            double y = h - pad - norm * (h - pad * 2);
            pts.Add(new Point(x, y));
        }
        Line.Points = pts;

        // area = line points + bottom-right + bottom-left
        var fig = new PathFigure { StartPoint = new Point(pts[0].X, h), IsClosed = true };
        var seg = new PolyLineSegment();
        foreach (var p in pts) seg.Points.Add(p);
        seg.Points.Add(new Point(pts[^1].X, h));
        fig.Segments.Add(seg);
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        AreaPath.Data = geo;
    }
}
