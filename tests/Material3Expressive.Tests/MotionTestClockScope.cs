using System.Runtime.ExceptionServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using ComposeAnimation.TestSupport;

namespace Material3Expressive.Tests;

/// <summary>Test context; root initialization and manual frame advancement remain separate.</summary>
internal sealed class MotionTestClockScope : IDisposable
{
    private static readonly AsyncLocal<MotionTestClockScope?> Current = new();
    private readonly ManualAnimationFrameClock _clock = new(Dispatcher.UIThread.VerifyAccess);
    private readonly List<Window> _windows = [];
    private readonly Queue<ExceptionDispatchInfo> _failures = [];
    private VisualAnimationScope? _standalone;
    private bool _disposed;
    internal TimeSpan Now => _clock.Now;
    internal int PendingFrameCount => _clock.PendingFrameCount;
    internal static IAnimationFrameClock? CurrentClock => Current.Value?._clock;
    internal VisualAnimationScope Scope
    {
        get
        {
            if (_standalone is null)
            {
                var window = new Window();
                _windows.Add(window);
                _standalone = VisualAnimationScope.Initialize(window, _clock);
            }
            return _standalone;
        }
    }

    internal MotionTestClockScope()
    {
        if (Current.Value is not null) throw new InvalidOperationException("Nested test clocks are not supported.");
        Current.Value = this;
    }

    internal static void Register(Window window) => Current.Value?._windows.Add(window);
    internal static Exception? Error(Visual owner) => ((MotionTestWindow)TopLevel.GetTopLevel(owner)!).MotionRoot.LastError;
    internal void Track(Task task) => _ = ObserveAsync(task);

    private async Task ObserveAsync(Task task)
    {
        try { await task; }
        catch (OperationCanceledException) when (task.IsCanceled) { }
        catch (Exception error) { _failures.Enqueue(ExceptionDispatchInfo.Capture(error)); }
    }

    private void ThrowIfFailed()
    {
        if (_failures.TryDequeue(out var failure))
            failure.Throw();
    }

    internal void SendFrame(TimeSpan timestamp)
    {
        Dispatcher.UIThread.RunJobs();
        _clock.SendFrame(timestamp);
        Dispatcher.UIThread.RunJobs();
        ThrowIfFailed();
    }

    internal void AdvanceBy(TimeSpan delta)
    {
        Dispatcher.UIThread.RunJobs();
        _clock.AdvanceBy(delta);
        Dispatcher.UIThread.RunJobs();
        ThrowIfFailed();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Current.Value = null;
        foreach (var window in _windows) window.Close();
        _standalone?.Dispose();
        _clock.Dispose();
        Dispatcher.UIThread.RunJobs();
        ThrowIfFailed();
    }
}

internal sealed class MotionTestWindow : Window
{
    internal VisualAnimationScope MotionRoot { get; }
    public MotionTestWindow()
    {
        MotionRoot = VisualAnimationScope.Initialize(this, MotionTestClockScope.CurrentClock);
        MotionTestClockScope.Register(this);
    }
}
