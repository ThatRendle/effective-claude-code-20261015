using Avalonia.Controls;
using Avalonia.Interactivity;
using NotIrc.App.ViewModels;

namespace NotIrc.App;

/// <summary>Asks for another nickname. <c>ShowDialog&lt;string?&gt;</c> returns the trimmed choice, or null if cancelled or closed.</summary>
public partial class NicknamePromptWindow : Window
{
    public NicknamePromptWindow()
        : this("")
    {
    }

    public NicknamePromptWindow(string rejectedNickname)
    {
        InitializeComponent();
        DataContext = new NicknamePromptViewModel(rejectedNickname);
        Opened += (_, _) =>
        {
            NicknameBox.Focus();
            NicknameBox.SelectAll();
        };
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        var vm = (NicknamePromptViewModel)DataContext!;
        if (vm.CanConfirm)
        {
            Close(vm.Nickname.Trim());
        }
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
}
