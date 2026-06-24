using System.Windows;
using System.Windows.Controls;

namespace FpsGodPc.App.Controls;

/// <summary>Card for text-heavy values (OS name, drive, CPU) — white value, not the big-green KPI style.</summary>
public partial class InfoTile : UserControl
{
    public InfoTile() => InitializeComponent();

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(InfoTile), new PropertyMetadata(string.Empty));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(InfoTile), new PropertyMetadata(string.Empty));
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty DetailProperty = DependencyProperty.Register(
        nameof(Detail), typeof(string), typeof(InfoTile), new PropertyMetadata(string.Empty));
    public string Detail { get => (string)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }
}
