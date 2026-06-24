using System.Windows;
using System.Windows.Controls;

namespace FpsGodPc.App.Controls;

/// <summary>HyperTune-style feature row: title + description + optional risk badge + pill toggle.</summary>
public partial class FeatureToggle : UserControl
{
    public FeatureToggle() => InitializeComponent();

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(FeatureToggle), new PropertyMetadata(string.Empty));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(FeatureToggle), new PropertyMetadata(string.Empty));
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    public static readonly DependencyProperty RiskProperty = DependencyProperty.Register(
        nameof(Risk), typeof(string), typeof(FeatureToggle), new PropertyMetadata(string.Empty));
    public string Risk { get => (string)GetValue(RiskProperty); set => SetValue(RiskProperty, value); }

    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
        nameof(IsOn), typeof(bool), typeof(FeatureToggle),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public bool IsOn { get => (bool)GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }
}
