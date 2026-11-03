using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;

namespace Reply;

public sealed class Email : INotifyPropertyChanged
{
    public int Id { get; init; }
    public string Sender { get; init; } = "";
    public string FullName { get; init; } = "";
    public string AvatarFile { get; init; } = "";
    public string Subject { get; init; } = "";
    public string Body { get; init; } = "";
    public string CreatedAt { get; init; } = "";

    [JsonIgnore] public Bitmap? Avatar { get; set; }
    [JsonIgnore] public IReadOnlyList<Email> Threads { get; set; } = [];
    [JsonIgnore] public string MessageCount => $"{Threads.Count} Messages";

    private bool _isStarred;
    public bool IsStarred
    {
        get => _isStarred;
        set { _isStarred = value; OnPropertyChanged(); }
    }

    private bool _isSelected;
    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    private bool _isOpened;
    [JsonIgnore]
    public bool IsOpened
    {
        get => _isOpened;
        set { _isOpened = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
