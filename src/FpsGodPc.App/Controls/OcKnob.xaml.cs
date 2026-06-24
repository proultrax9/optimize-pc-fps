using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FpsGodPc.App.Controls;

/// <summary>
/// HyperTune-style overclock knob. A rotary dial (270° travel) driven by
/// dragging vertically; the pointer rotates and the centre shows the value.
/// </summary>
public partial class OcKnob : UserControl
{
    private const double MinAngle = -135d;
    private const double MaxAngle = 135d;
    private const double DragSensitivity = 180d; // pixels of vertical drag for full range

    private bool _dragging;
    private Point _dragStart;
    private double _dragStartValue;

    public OcKnob()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
        KnobArea.MouseLeftButtonDown += OnDown;
        KnobArea.MouseMove += OnMove;
        KnobArea.MouseLeftButtonUp += OnUp;
        KnobArea.MouseWheel += OnWheel;
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(OcKnob),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVisualChanged));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(OcKnob), new PropertyMetadata(0d, OnVisualChanged));
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(OcKnob), new PropertyMetadata(100d, OnVisualChanged));
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step), typeof(double), typeof(OcKnob), new PropertyMetadata(1d));
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(
        nameof(Suffix), typeof(string), typeof(OcKnob), new PropertyMetadata(string.Empty, OnVisualChanged));
    public string Suffix { get => (string)GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(OcKnob), new PropertyMetadata(string.Empty, OnVisualChanged));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(OcKnob), new PropertyMetadata("0", OnVisualChanged));
    public string ValueFormat { get => (string)GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((OcKnob)d).Render();

    private double Fraction()
    {
        var range = Maximum - Minimum;
        if (range <= 0) return 0;
        return Math.Clamp((Value - Minimum) / range, 0d, 1d);
    }

    private void Render()
    {
        ValueText.Text = Value.ToString(ValueFormat, CultureInfo.InvariantCulture);
        SuffixText.Text = Suffix;
        KnobLabel.Text = Label;
        PointerRotate.Angle = MinAngle + Fraction() * (MaxAngle - MinAngle);
    }

    private void SetValueClamped(double raw)
    {
        if (Step > 0) raw = Math.Round(raw / Step) * Step;
        Value = Math.Clamp(raw, Minimum, Maximum);
    }

    private void OnDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _dragStart = e.GetPosition(this);
        _dragStartValue = Value;
        KnobArea.CaptureMouse();
        e.Handled = true;
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        double dy = _dragStart.Y - e.GetPosition(this).Y; // up = increase
        double range = Maximum - Minimum;
        SetValueClamped(_dragStartValue + dy / DragSensitivity * range);
    }

    private void OnUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        KnobArea.ReleaseMouseCapture();
    }

    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        double delta = Math.Sign(e.Delta) * (Step > 0 ? Step : 1d);
        SetValueClamped(Value + delta);
        e.Handled = true;
    }
}
