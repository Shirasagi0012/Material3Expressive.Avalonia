using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Reply;

public partial class MainWindow : Window
{
    private readonly ReplyViewModel _viewModel = new();

    public MainWindow()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.Dispose();
        KeyDown += OnKeyDown;
    }

    private void OpenEmail(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Email email })
        {
            _viewModel.Open(email);
            ThreadScroll.Offset = default;
        }
    }

    private void SelectEmail(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Email email }) _viewModel.ToggleSelection(email);
        e.Handled = true;
    }

    private void ToggleStar(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Email email }) email.IsStarred = !email.IsStarred;
        e.Handled = true;
    }

    private void ClearSelection(object? sender, RoutedEventArgs e) => _viewModel.ClearSelection();
    private void RemoveEmails(object? sender, RoutedEventArgs e) => _viewModel.RemoveSelection();
    private void BackToList(object? sender, RoutedEventArgs e) => _viewModel.DetailOpen = false;

    private async void GoToInbox(object? sender, RoutedEventArgs e)
    {
        _viewModel.SearchText = "";
        _viewModel.DetailOpen = false;
        _viewModel.ClearSelection();
        await SetDrawerOpenAsync(false);
    }

    private async void OpenDrawer(object? sender, RoutedEventArgs e) => await SetDrawerOpenAsync(true);
    private async void CloseDrawer(object? sender, RoutedEventArgs e) => await SetDrawerOpenAsync(false);

    private async Task SetDrawerOpenAsync(bool open)
    {
        try
        {
            if (open) await ModalDrawer.State.OpenAsync();
            else await ModalDrawer.State.CloseAsync();
        }
        catch (OperationCanceledException)
        {
            // A subsequent drawer action or window close cancels the current animation.
        }
    }

    private void UseLightTheme(object? sender, RoutedEventArgs e) => RequestedThemeVariant = ThemeVariant.Light;
    private void UseDarkTheme(object? sender, RoutedEventArgs e) => RequestedThemeVariant = ThemeVariant.Dark;
    private void UseSystemTheme(object? sender, RoutedEventArgs e) => RequestedThemeVariant = ThemeVariant.Default;

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _viewModel.DetailOpen = false;
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (ModalDrawer.State.TargetValue == DrawerValue.Open) _ = SetDrawerOpenAsync(false);
            else if (_viewModel.HasSelection) _viewModel.ClearSelection();
            else if (_viewModel.SearchText.Length != 0) _viewModel.SearchText = "";
            else _viewModel.DetailOpen = false;
            e.Handled = true;
        }
    }
}
