using Avalonia.Controls;
using Avalonia.Interactivity;
using NotIrc.App.ViewModels;

namespace NotIrc.App;

public partial class StartupErrorWindow : Window
{
    public StartupErrorWindow()
        : this(new StartupErrorViewModel([]))
    {
    }

    public StartupErrorWindow(StartupErrorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
