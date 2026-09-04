using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ComposeAnimationRuntime.Demo;

internal sealed class MainWindow : Window
{
    private readonly Border _springBox;
    private readonly TextBlock _status;
    private readonly Animatable<double> _position = new(0, new DoubleVectorConverter());
    private readonly Animatable<double> _opacity = new(1, new DoubleVectorConverter());
    private AvaloniaMotionHost? _host;

    public MainWindow()
    {
        Title = "Compose Animation Runtime";
        Width = 720;
        Height = 460;
        MinWidth = 560;
        MinHeight = 360;

        _springBox = new Border
        {
            Width = 96,
            Height = 96,
            CornerRadius = new CornerRadius(20),
            Background = Brushes.CornflowerBlue,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _status = new TextBlock { Text = "Ready", Margin = new Thickness(0, 12, 0, 0) };

        var springButton = new Button { Content = "Spring", Width = 110 };
        springButton.Click += (_, _) => AnimateSpring();
        var decayButton = new Button { Content = "Decay", Width = 110 };
        decayButton.Click += (_, _) => AnimateDecay();
        var resetButton = new Button { Content = "Reset", Width = 110 };
        resetButton.Click += (_, _) => Reset();

        var stage = new Canvas
        {
            Background = Brushes.Gainsboro,
            Children = { _springBox },
        };

        Content = new StackPanel
        {
            Margin = new Thickness(32),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "ComposeAnimationRuntime", FontSize = 24, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = "A small Avalonia host for spring, tween and decay motion." },
                new Border
                {
                    Height = 230,
                    Margin = new Thickness(0, 20, 0, 0),
                    Background = Brushes.Gainsboro,
                    Child = stage,
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children = { springButton, decayButton, resetButton },
                },
                _status,
            },
        };

        Opened += OnOpened;
        Closed += (_, _) => _host?.Dispose();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _host = new AvaloniaMotionHost(this);
        MotionBinding.Bind(_springBox, Canvas.LeftProperty, _position);
        MotionBinding.Bind(_springBox, Visual.OpacityProperty, _opacity);
        _host.Register(_position);
        _host.Register(_opacity);
    }

    private void AnimateSpring()
    {
        _position.AnimateTo(_position.Value > 100 ? 0 : 420, new SpringSpec<double>(new DoubleVectorConverter(), 0.72, 260));
        _status.Text = $"Spring  position={_position.Value:0}";
        _host?.RequestFrame();
    }

    private void AnimateDecay()
    {
        _position.AnimateDecay(360, new DecaySpec<double>(new DoubleVectorConverter(), 3.4));
        _status.Text = "Decay  releasing with initial velocity";
        _host?.RequestFrame();
    }

    private void Reset()
    {
        _position.SnapTo(0);
        _opacity.SnapTo(1);
        _status.Text = "Reset";
        _host?.RequestFrame();
    }
}
