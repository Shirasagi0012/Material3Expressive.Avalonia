using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class SnackbarDemo : Window
{
    private CancellationTokenSource _requests = new();

    public SnackbarDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        Closed += (_, _) => { _requests.Cancel(); _requests.Dispose(); };
    }

    private async void OnShort(object? sender, RoutedEventArgs e) => await Show("Saved successfully");
    private async void OnLong(object? sender, RoutedEventArgs e) => await Show("Syncing in the background", duration: SnackbarDuration.Long);
    private async void OnUndo(object? sender, RoutedEventArgs e) => await Show("File deleted", "Undo", true);
    private async void OnQueue(object? sender, RoutedEventArgs e) => await Task.WhenAll(Show("First queued message"), Show("Second queued message"));
    private async Task Show(string message, string? action = null, bool dismiss = false, SnackbarDuration? duration = null)
    {
        try
        {
            var result = await Messages.State.ShowSnackbarAsync(message, action, dismiss, duration, _requests.Token);
            Result.Text = $"{message}: {result}";
        }
        catch (OperationCanceledException) { Result.Text = $"{message}: Canceled"; }
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        _requests.Cancel(); _requests.Dispose(); _requests = new();
    }
    private void OnRetry(object? sender, RoutedEventArgs e) => Result.Text = "Custom action: retry";
    private void OnCustomAction(object? sender, RoutedEventArgs e) => Result.Text = "Custom action: got it";
}
