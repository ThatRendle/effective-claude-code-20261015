using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

    // Insert at the caret, replacing any selection, then put focus back so the user can keep typing.
    // The picker stays open for further picks; the two-way text binding feeds the byte counter and Send state.
    private void OnEmojiClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string emoji })
        {
            return;
        }

        var (text, caret) = EmojiInsertion.Insert(InputBox.Text, InputBox.SelectionStart, InputBox.SelectionEnd, emoji);
        InputBox.Text = text;
        InputBox.CaretIndex = caret;
        InputBox.Focus();
    }

    // The log keeps everything. Follow new entries only if the user was already at the bottom, so reading back is not interrupted.
    // This runs before the new entry is laid out, so the extent is still the old one.
    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (LogFollow.ShouldFollow(LogScroll.Offset.Y, LogScroll.Viewport.Height, LogScroll.Extent.Height))
        {
            Dispatcher.UIThread.Post(() => LogScroll.ScrollToEnd(), DispatcherPriority.Background);
        }
    }
}
