using Avalonia.Controls;
using ComposeAnimationRuntime.Avalonia;
using ComposeAnimationRuntime.Demo.Pages;

namespace ComposeAnimationRuntime.Demo;

internal sealed partial class MainWindow : Window
{
    private AvaloniaMotionHost? _host;
    private MotionPageBase? _currentPage;

    public MainWindow()
    {
        InitializeComponent();
        _currentPage = (MotionPageBase)PageHost.Content!;
        Navigation.SelectionChanged += OnNavigationChanged;
        Closed += OnClosed;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _host = new AvaloniaMotionHost(this);
        AttachCurrentPage();
    }

    private void OnNavigationChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_host is null || Navigation.SelectedItem is not ListBoxItem item)
            return;

        _currentPage?.Dispose();
        _currentPage = CreatePage(item.Content as string);
        PageHost.Content = _currentPage;
        AttachCurrentPage();
    }

    private void AttachCurrentPage()
    {
        if (_host is not null)
            _currentPage?.Attach(_host);
    }

    private static MotionPageBase CreatePage(string? name) => name switch
    {
        "Spring" => new SpringPage(),
        "Decay" => new DecayPage(),
        "Keyframes" => new KeyframesPage(),
        "Visibility" => new VisibilityPage(),
        "Transition" => new TransitionPage(),
        _ => new OverviewPage(),
    };

    private void OnClosed(object? sender, EventArgs e)
    {
        _currentPage?.Dispose();
        _host?.Dispose();
    }
}
