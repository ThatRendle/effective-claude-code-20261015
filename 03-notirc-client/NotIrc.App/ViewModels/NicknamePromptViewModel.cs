using CommunityToolkit.Mvvm.ComponentModel;

namespace NotIrc.App.ViewModels;

public sealed partial class NicknamePromptViewModel : ObservableObject
{
    public NicknamePromptViewModel(string rejectedNickname)
    {
        Message = $"The nickname \"{rejectedNickname}\" is taken. Choose another one.";
        _nickname = rejectedNickname;
    }

    public string Message { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _nickname;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Nickname);
}
