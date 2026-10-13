using System.Runtime.ExceptionServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.TestSupport;

namespace Material3Expressive.Tests;

/// <summary>Test context; root initialization and manual frame advancement remain separate.</summary>
internal sealed class MotionTestClockScope : IDisposable
{
    private static readonly AsyncLocal<MotionTestClockScope?> Current = new();
    private readonly ManualFrameSource _clock = new(Dispatcher.UIThread.VerifyAccess);
    private readonly List<Window> _windows = [];
    private readonly Queue<ExceptionDispatchInfo> _failures = [];
    private MotionTestWindow? _standalone;
    private bool _disposed;
    internal TimeSpan Now => _clock.Now;
    internal int PendingFrameCount => _clock.PendingFrameCount;
    internal static ManualFrameSource? CurrentFrameSource => Current.Value?._clock;

    /// <summary>A hidden window root for tests that need a scope without a control.</summary>
    internal AnimationScope Scope => (_standalone ??= new MotionTestWindow()).MotionRoot;

    internal MotionTestClockScope()
    {
        if (Current.Value is not null) throw new InvalidOperationException("Nested test clocks are not supported.");
        Current.Value = this;
    }

    internal static void Register(Window window) => Current.Value?._windows.Add(window);
    internal static Exception? Error(Visual owner) => ((MotionTestWindow)TopLevel.GetTopLevel(owner)!).MotionRoot.Diagnostics.LastError;
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
        foreach (var window in _windows)
        {
            window.Close();
            // Close ends a shown window. A hidden root used only as a scope still has to end here.
            if (window is MotionTestWindow motion && !motion.MotionRoot.Diagnostics.IsEnding)
                _ = motion.MotionRoot.Diagnostics.EndAsync();
        }
        _clock.Dispose();
        Dispatcher.UIThread.RunJobs();
        ThrowIfFailed();
    }
}

internal sealed class MotionTestWindow : Window
{
    internal AnimationScope MotionRoot { get; }
    public MotionTestWindow()
    {
        MotionRoot = AnimationScope.Initialize(this, MotionTestClockScope.CurrentFrameSource);
        MotionTestClockScope.Register(this);
    }
}
