using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using FpsGodPc.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FpsGodPc.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var shellVm = App.Services.GetRequiredService<ShellViewModel>();
        DataContext = shellVm;
        shellVm.PropertyChanged += OnShellPropertyChanged;
    }

    // Replay a subtle fade + slide-up on the content host every time the page changes.
    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.CurrentPage))
        {
            PlayPageTransition();
        }
    }

    private void PlayPageTransition()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(180);
        PageHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
        PageHostTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty,
            new DoubleAnimation(12, 0, duration) { EasingFunction = ease });
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        => SystemCommands.MinimizeWindow(this);

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => SystemCommands.CloseWindow(this);
}
