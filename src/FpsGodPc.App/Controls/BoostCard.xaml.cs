using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FpsGodPc.App.Controls;

public partial class BoostCard : UserControl
{
    public static readonly DependencyProperty PresetIdProperty =
        DependencyProperty.Register(nameof(PresetId), typeof(string), typeof(BoostCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(BoostCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(BoostCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TweakCountLabelProperty =
        DependencyProperty.Register(nameof(TweakCountLabel), typeof(string), typeof(BoostCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ApplyCommandProperty =
        DependencyProperty.Register(nameof(ApplyCommand), typeof(ICommand), typeof(BoostCard), new PropertyMetadata(null));

    public static readonly DependencyProperty ApplyPresetLabelProperty =
        DependencyProperty.Register(nameof(ApplyPresetLabel), typeof(string), typeof(BoostCard), new PropertyMetadata("Apply Preset"));

    public BoostCard()
    {
        InitializeComponent();
    }

    public string PresetId
    {
        get => (string)GetValue(PresetIdProperty);
        set => SetValue(PresetIdProperty, value);
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

    public string TweakCountLabel
    {
        get => (string)GetValue(TweakCountLabelProperty);
        set => SetValue(TweakCountLabelProperty, value);
    }

    public ICommand? ApplyCommand
    {
        get => (ICommand?)GetValue(ApplyCommandProperty);
        set => SetValue(ApplyCommandProperty, value);
    }

    public string ApplyPresetLabel
    {
        get => (string)GetValue(ApplyPresetLabelProperty);
        set => SetValue(ApplyPresetLabelProperty, value);
    }
}
