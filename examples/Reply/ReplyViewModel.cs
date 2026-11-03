using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Reply;

public sealed class ReplyViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly Dictionary<string, Bitmap> _avatars = new();
    private readonly List<Email> _emails;
    private string _searchText = "";
    private Email? _openedEmail;
    private bool _detailOpen;

    public ReplyViewModel()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Reply/Assets/emails.json"));
        _emails = JsonSerializer.Deserialize<List<Email>>(stream)!;
        foreach (var email in _emails)
        {
            if (!_avatars.TryGetValue(email.AvatarFile, out var avatar))
            {
                using var image = AssetLoader.Open(new Uri($"avares://Reply/Assets/{email.AvatarFile}"));
                _avatars[email.AvatarFile] = avatar = new Bitmap(image);
            }
            email.Avatar = avatar;
        }

        // Keep the upstream sample's seven-message fixture in a repeatable order.
        var threads = new[] { 8, 5, 6, 7, 9, 1, 2 }.Select(id => _emails.Single(email => email.Id == id)).ToArray();
        foreach (var email in _emails)
            email.Threads = threads;

        RefreshEmails();
        OpenedEmail = _emails[0];
    }

    public ObservableCollection<Email> Emails { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value ?? "";
            RefreshEmails();
            Notify(nameof(SearchText));
        }
    }

    public Email? OpenedEmail
    {
        get => _openedEmail;
        private set
        {
            if (_openedEmail != null) _openedEmail.IsOpened = false;
            _openedEmail = value;
            if (_openedEmail != null) _openedEmail.IsOpened = true;
            Notify(nameof(OpenedEmail));
        }
    }

    public bool DetailOpen
    {
        get => _detailOpen;
        set { _detailOpen = value; Notify(nameof(DetailOpen)); }
    }

    public bool HasSelection => _emails.Any(email => email.IsSelected);
    public int SelectionCount => _emails.Count(email => email.IsSelected);
    public bool IsEmpty => Emails.Count == 0;

    public void Open(Email email)
    {
        if (HasSelection)
        {
            ToggleSelection(email);
            return;
        }
        OpenedEmail = email;
        DetailOpen = true;
    }

    public void ToggleSelection(Email email)
    {
        email.IsSelected = !email.IsSelected;
        Notify(nameof(HasSelection));
        Notify(nameof(SelectionCount));
    }

    public void ClearSelection()
    {
        foreach (var email in _emails) email.IsSelected = false;
        Notify(nameof(HasSelection));
        Notify(nameof(SelectionCount));
    }

    public void RemoveSelection()
    {
        var removed = _emails.Where(email => email.IsSelected).ToArray();
        if (removed.Length == 0 && OpenedEmail != null) removed = [OpenedEmail];
        foreach (var email in removed) _emails.Remove(email);
        if (OpenedEmail != null && removed.Contains(OpenedEmail))
            OpenedEmail = _emails.FirstOrDefault();
        DetailOpen = false;
        ClearSelection();
        RefreshEmails();
    }

    private void RefreshEmails()
    {
        Emails.Clear();
        foreach (var email in _emails.Where(email =>
                     email.Subject.StartsWith(_searchText, StringComparison.OrdinalIgnoreCase) ||
                     email.FullName.StartsWith(_searchText, StringComparison.OrdinalIgnoreCase)))
            Emails.Add(email);
        Notify(nameof(IsEmpty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

    public void Dispose()
    {
        foreach (var avatar in _avatars.Values) avatar.Dispose();
    }
}
