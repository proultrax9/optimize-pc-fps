using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FpsGodPc.App.Controls;

/// <summary>Game Hub tile: vector accent banner + game name/status + Optimize action.</summary>
public partial class GameCard : UserControl
{
    public GameCard() => InitializeComponent();

    public static readonly DependencyProperty GameNameProperty = DependencyProperty.Register(
        nameof(GameName), typeof(string), typeof(GameCard), new PropertyMetadata(string.Empty, OnGameNameChanged));
    public string GameName { get => (string)GetValue(GameNameProperty); set => SetValue(GameNameProperty, value); }

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(string), typeof(GameCard), new PropertyMetadata(string.Empty));
    public string Status { get => (string)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    public static readonly DependencyProperty AccentColorProperty = DependencyProperty.Register(
        nameof(AccentColor), typeof(Brush), typeof(GameCard),
        new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x13, 0xF2, 0x87))));
    public Brush AccentColor { get => (Brush)GetValue(AccentColorProperty); set => SetValue(AccentColorProperty, value); }

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(GameCard), new PropertyMetadata(null));
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(GameCard), new PropertyMetadata(null));
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private static void OnGameNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (GameCard)d;
        var name = e.NewValue as string;
        c.InitialText.Text = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[..1].ToUpperInvariant();
    }
}
