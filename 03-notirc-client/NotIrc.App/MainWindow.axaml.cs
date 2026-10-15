using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using NotIrc.App.ViewModels;

namespace NotIrc.App;

public partial class MainWindow : Window
{
    /// <summary>Parameterless constructor for the XAML loader and previewer; the app uses the view-model overload.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.Entries.CollectionChanged += OnEntriesChanged;
        Closed += (_, _) => viewModel.Entries.CollectionChanged -= OnEntriesChanged;
    }

    // The log keeps everything, so keep the newest entry in view.
    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() => LogScroll.ScrollToEnd(), DispatcherPriority.Background);
}
