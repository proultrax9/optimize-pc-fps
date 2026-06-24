using System.Windows;
using FpsGodPc.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FpsGodPc.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<ShellViewModel>();
    }
}