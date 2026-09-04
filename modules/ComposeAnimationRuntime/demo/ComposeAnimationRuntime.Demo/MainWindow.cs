using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimationRuntime.Avalonia;

namespace ComposeAnimationRuntime.Demo;

internal sealed class MainWindow : Window
{
    private readonly ContentControl _content;
    private readonly DoubleVectorConverter _scalar = new();
    private AvaloniaMotionHost? _host;
    private readonly HashSet<object> _hostRegistered = [];
    private readonly List<IDisposable> _pageBindings = [];
    private readonly Animatable<double> _spring = new(0, new DoubleVectorConverter());
    private readonly Animatable<double> _decay = new(0, new DoubleVectorConverter());
    private readonly Animatable<double> _keyframes = new(0, new DoubleVectorConverter());
    private readonly Animatable<double> _offset = new(0, new DoubleVectorConverter());
    private readonly Animatable<double> _opacity = new(1, new DoubleVectorConverter());
    private Border? _springBox, _decayBox, _keyframeBox, _transitionBox;
    private Border? _visibilityBox;
    private readonly Animatable<double> _visibilityOpacity = new(1, new DoubleVectorConverter());
    private TextBlock? _status;

    public MainWindow()
    {
        Title = "Compose Animation Runtime · Catalog";
        Width = 1080; Height = 700; MinWidth = 820; MinHeight = 520;
        _content = new ContentControl { Content = Overview() };
        var navigation = new ListBox { ItemsSource = new[] { "Overview", "Spring", "Decay", "Keyframes", "Visibility", "Transition" }, SelectedIndex = 0, Margin = new Thickness(12) };
        navigation.SelectionChanged += (_, _) =>
        {
            ClearPageBindings();
            _status = null;
            _content.Content = Page((string?)navigation.SelectedItem ?? "Overview");
        };
        var shell = new Grid { ColumnDefinitions = new ColumnDefinitions("220,*") };
        var navigationPane = new Border { Background = Brush.Parse("#F3F4F6"), Child = navigation };
        var contentPane = new Border { Padding = new Thickness(28), Child = _content };
        Grid.SetColumn(contentPane, 1);
        shell.Children.Add(navigationPane);
        shell.Children.Add(contentPane);
        Content = shell;
        Opened += OnOpened;
        Closed += (_, _) =>
        {
            ClearPageBindings();
            _host?.Dispose();
        };
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _host = new AvaloniaMotionHost(this);
        Register(_spring, _springBox, Canvas.LeftProperty);
        Register(_decay, _decayBox, Canvas.LeftProperty);
        Register(_keyframes, _keyframeBox, Canvas.LeftProperty);
        Register(_offset, _transitionBox, Canvas.LeftProperty);
        Register(_opacity, _transitionBox, Visual.OpacityProperty);
    }

    private void Register<T>(Animatable<T> animation, AvaloniaObject? owner, AvaloniaProperty<T> property)
    {
        if (_host is null || owner is null) return;
        _pageBindings.Add(MotionBinding.Bind(owner, property, animation));
        if (_hostRegistered.Add(animation))
            _host.Register(animation);
    }

    private Control Page(string name) => name switch { "Spring" => Spring(), "Decay" => Decay(), "Keyframes" => Keyframes(), "Visibility" => Visibility(), "Transition" => Transition(), _ => Overview() };

    private Control Overview() => Layout("Animation catalog", "Interactive examples of the runtime and its Avalonia adapter.",
        new Border { Background = Brush.Parse("#E8F0FE"), CornerRadius = new CornerRadius(16), Padding = new Thickness(24), Child = new StackPanel { Spacing = 8, Children = { new TextBlock { Text = "Compose-inspired motion for .NET", FontSize = 24, FontWeight = FontWeight.SemiBold }, new TextBlock { Text = "Use the navigation to inspect live Spring, Decay, Keyframes, Visibility, and Transition samples." } } } },
        new TextBlock { Text = "All samples use Animatable<T>, explicit animation specs, and the same Avalonia frame host.", Margin = new Thickness(0, 16, 0, 0) });

    private Control Spring()
    {
        _springBox = Box(Brush.Parse("#4F7CFF"));
        Register(_spring, _springBox, Canvas.LeftProperty);
        var button = new Button { Content = "Animate spring", HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => { _spring.AnimateTo(_spring.Value > 100 ? 0 : 560, new SpringSpec<double>(_scalar, 0.72, 260)); SetStatus($"target = {_spring.TargetValue:0}"); _host?.RequestFrame(); };
        return Layout("Spring", "A physics-based animation preserves motion when its target changes.", Stage(_springBox), button, Status());
    }

    private Control Decay()
    {
        _decayBox = Box(Brush.Parse("#E87532"));
        Register(_decay, _decayBox, Canvas.LeftProperty);
        var button = new Button { Content = "Release with velocity", HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => { _decay.SnapTo(0); _decay.AnimateDecay(620, new DecaySpec<double>(_scalar, 3.2)); SetStatus("initial velocity = 620 px/s"); _host?.RequestFrame(); };
        return Layout("Decay", "Inertial motion continues after a gesture is released.", Stage(_decayBox), button, Status());
    }

    private Control Keyframes()
    {
        _keyframeBox = Box(Brush.Parse("#2D9D78"));
        Register(_keyframes, _keyframeBox, Canvas.LeftProperty);
        var button = new Button { Content = "Play keyframes", HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => { _keyframes.SnapTo(0); _keyframes.AnimateTo(0, new KeyframesSpec<double>(_scalar, TimeSpan.FromSeconds(1), new(TimeSpan.Zero, 0), new(TimeSpan.FromMilliseconds(300), 420, Easings.EaseOut), new(TimeSpan.FromMilliseconds(700), 160, Easings.EaseIn), new(TimeSpan.FromSeconds(1), 560))); SetStatus("4 keyframes · per-segment easing"); _host?.RequestFrame(); };
        return Layout("Keyframes", "Keyframes describe a precise multi-segment trajectory.", Stage(_keyframeBox), button, Status());
    }

    private Control Visibility()
    {
        _visibilityBox = new Border { Width = 260, Height = 130, Background = Brush.Parse("#8C5DE8"), CornerRadius = new CornerRadius(18), Child = new TextBlock { Text = "VisibilityMotion", Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
        Register(_visibilityOpacity, _visibilityBox, Visual.OpacityProperty);
        var show = new Button { Content = "Show" }; var hide = new Button { Content = "Hide" };
        show.Click += (_, _) => { _visibilityOpacity.AnimateTo(1, new SpringSpec<double>(_scalar)); _host?.RequestFrame(); };
        hide.Click += (_, _) => { _visibilityOpacity.AnimateTo(0, new TweenSpec<double>(_scalar, TimeSpan.FromMilliseconds(180))); _host?.RequestFrame(); };
        return Layout("Visibility", "Enter and exit motion can retain content while it fades out.", _visibilityBox, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { show, hide } });
    }

    private Control Transition()
    {
        _transitionBox = Box(Brush.Parse("#D14B72"));
        Register(_offset, _transitionBox, Canvas.LeftProperty);
        Register(_opacity, _transitionBox, Visual.OpacityProperty);
        var state = false; var transition = new MotionTransition<bool>(false);
        transition.Register("offset", _offset, value => value ? 480 : 0, _ => new SpringSpec<double>(_scalar, 0.8, 320));
        transition.Register("opacity", _opacity, value => value ? 0.35 : 1, _ => new TweenSpec<double>(_scalar, TimeSpan.FromMilliseconds(240), Easings.FastOutSlowIn));
        var button = new Button { Content = "Toggle state", HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => { state = !state; transition.AnimateTo(state); SetStatus($"state = {(state ? "active" : "resting")}"); _host?.RequestFrame(); };
        return Layout("Transition", "One logical state change coordinates multiple animated values.", Stage(_transitionBox), button, Status());
    }

    private static Control Layout(string title, string description, params Control[] content)
    {
        var children = new List<Control>
        {
            new TextBlock { Text = title, FontSize = 28, FontWeight = FontWeight.SemiBold },
            new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap },
            new Separator(),
        };
        children.AddRange(content);
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.AddRange(children);
        return panel;
    }
    private static Border Stage(Control child)
    {
        var canvas = new Canvas();
        canvas.Children.Add(child);
        return new Border { Height = 240, Background = Brush.Parse("#F5F6F8"), CornerRadius = new CornerRadius(14), Child = canvas };
    }
    private static Border Box(IBrush background) => new() { Width = 96, Height = 96, CornerRadius = new CornerRadius(20), Background = background };
    private TextBlock Status()
    {
        var status = new TextBlock { Text = "Ready", Foreground = Brushes.Gray };
        _status = status;
        return status;
    }

    private void SetStatus(string value)
    {
        (_status ??= new TextBlock { Foreground = Brushes.Gray }).Text = value;
    }

    private void ClearPageBindings()
    {
        foreach (var binding in _pageBindings)
            binding.Dispose();
        _pageBindings.Clear();
    }
}
