using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using ComposeAnimationRuntime.Avalonia;
using Avalonia.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class SharedBoundsPage : MotionPageBase
{
    private SharedBounds? _source;
    private SharedBounds? _destination;
    private readonly DispatcherTimer _traceTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _traceTime = new();
    private readonly StringBuilder _trace = new();
    private int _completionCount;

    public SharedBoundsPage()
    {
        InitializeComponent();
        _traceTimer.Tick += OnTraceTick;
        Root.TransitionCompleted += (_, _) =>
        {
            _completionCount++;
            Status.Text = "Completed source → destination";
        };
    }

    protected override void OnAttached()
    {
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        _source = CreateBounds("Source", "#4F7CFF", SharedParticipantRole.Source);
        _destination = CreateBounds("Destination", "#E87532", SharedParticipantRole.Destination);
        Grid.SetColumn(_source, 0);
        Grid.SetColumn(_destination, 1);
        layout.Children.Add(_source);
        layout.Children.Add(_destination);
        Root.ContentLayer.Children.Add(layout);
        _source.AttachClock(Host);
        _destination.AttachClock(Host);
        if (Environment.GetEnvironmentVariable("COMPOSE_SHARED_BOUNDS_TRACE") is not null)
            DispatcherTimer.RunOnce(() => OnRun(this, new RoutedEventArgs()), TimeSpan.FromMilliseconds(500));
    }

    protected override void OnDetached()
    {
        _traceTimer.Stop();
        Root.ContentLayer.Children.Clear();
        _source = null;
        _destination = null;
    }

    private void OnRun(object? sender, RoutedEventArgs e)
    {
        if (_source is null || _destination is null)
            return;
        _completionCount = 0;
        _trace.Clear();
        _trace.AppendLine("ms,phase,clock_ms,running,completed,model_x,model_y,model_w,model_h,visual_x,visual_y,visual_w,visual_h,source_attached,opacity");
        _traceTime.Restart();
        RecordTrace("before");
        var pairs = Root.RunTransition(static () => { });
        RecordTrace("start");
        _traceTimer.Start();
        Status.Text = pairs.Count == 0 ? "No matching participants" : "Animating source → destination";
    }

    private void OnTraceTick(object? sender, EventArgs e)
    {
        RecordTrace("sample");
        if (_traceTime.Elapsed < TimeSpan.FromSeconds(2)) return;
        _traceTimer.Stop();
        var path = Environment.GetEnvironmentVariable("COMPOSE_SHARED_BOUNDS_TRACE")
            ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "shared-bounds-trajectory.csv");
        System.IO.File.WriteAllText(path, _trace.ToString());
        if (Environment.GetEnvironmentVariable("COMPOSE_SHARED_BOUNDS_TRACE") is not null)
            (TopLevel.GetTopLevel(this) as Window)?.Close();
    }

    private void RecordTrace(string phase)
    {
        if (_destination is null || _source is null) return;
        var model = _destination.CurrentBounds;
        var visual = LayoutSnapshot.Capture(_destination, Root).RootBounds;
        _trace.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{_traceTime.Elapsed.TotalMilliseconds:F2},{phase},{Host.Time.TotalMilliseconds:F2},{Root.IsTransitioning},{_completionCount},{model.X:F3},{model.Y:F3},{model.Width:F3},{model.Height:F3},{visual.X:F3},{visual.Y:F3},{visual.Width:F3},{visual.Height:F3},{_source.Parent is not null},{_destination.Opacity:F3}"));
    }

    private static SharedBounds CreateBounds(string text, string color, SharedParticipantRole role)
        => new()
        {
            Key = "card",
            Role = role,
            Width = role == SharedParticipantRole.Source ? 150 : 230,
            Height = role == SharedParticipantRole.Source ? 76 : 130,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new Border
            {
                Background = Brush.Parse(color),
                CornerRadius = new CornerRadius(16),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };
}
