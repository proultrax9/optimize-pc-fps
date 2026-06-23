using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FpsGodPc.App.Controls;

public partial class TweakRow : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TweakRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(TweakRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RiskProperty =
        DependencyProperty.Register(nameof(Risk), typeof(string), typeof(TweakRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsToggledProperty =
        DependencyProperty.Register(nameof(IsToggled), typeof(bool), typeof(TweakRow), new PropertyMetadata(false));

    public static readonly DependencyProperty ToggleCommandProperty =
        DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(TweakRow), new PropertyMetadata(null));

    public TweakRow()
    {
        InitializeComponent();
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
}
