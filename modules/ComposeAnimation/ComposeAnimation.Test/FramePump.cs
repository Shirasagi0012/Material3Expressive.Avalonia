using Avalonia.Threading;
using ComposeAnimation.TestSupport;

namespace ComposeAnimation.Test;

/// <summary>
/// Runs driven through a window scope resume from the FrameClock's dispatcher post, so a
/// test that advances a <see cref="ManualFrameSource"/> also drains the UI queue.
/// </summary>
internal static class FramePump
{
    public static void Pump(this ManualFrameSource source, TimeSpan delta)
    {
        source.AdvanceBy(delta);
        Dispatcher.UIThread.RunJobs();
    }

    public static void PumpTo(this ManualFrameSource source, TimeSpan timestamp)
    {
        source.SendFrame(timestamp);
        Dispatcher.UIThread.RunJobs();
    }
}
