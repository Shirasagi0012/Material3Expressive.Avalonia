using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;

namespace Material3Expressive.TestSupport;

// Fixed Avalonia.Headless 12.1.5 uses Stopwatch for MouseDown/Move/Up timestamps.
// Send the caller's clock through the same platform device and raw-input route instead.
// No frame, layout, rendering, or wall-clock wait is performed here.
internal sealed class HeadlessMouseInput(TopLevel window)
{
    private bool _pressed;

    public void Send(ulong timestampMs, string action, Point position)
    {
        Dispatcher.UIThread.VerifyAccess();
        var type = action switch
        {
            "down" => RawPointerEventType.LeftButtonDown,
            "up" => RawPointerEventType.LeftButtonUp,
            "move" => RawPointerEventType.Move,
            _ => throw new ArgumentException("Expected down, up or move.", nameof(action))
        };
        var platform = window.PlatformImpl;
        if (platform?.GetType().FullName != "Avalonia.Headless.HeadlessWindowImpl" ||
            platform.GetType().GetProperty("MouseDevice")?.GetValue(platform) is not IMouseDevice mouse ||
            platform.GetType().GetProperty("InputRoot")?.GetValue(platform) is not IInputRoot root ||
            platform.GetType().GetProperty("Input")?.GetValue(platform) is not Action<RawInputEventArgs> input)
            throw new NotSupportedException($"Review the pinned Headless input adapter for {typeof(AvaloniaHeadlessPlatform).Assembly.GetName().Version}.");

        // RawPointerEventArgs is a version-sensitive API; keep its construction here,
        // rather than enabling private API access across all parity component scenarios.
        var raw = (RawInputEventArgs)Activator.CreateInstance(typeof(RawPointerEventArgs), mouse,
            timestampMs, root, type, position,
            action == "move" && _pressed ? RawInputModifiers.LeftMouseButton : RawInputModifiers.None)!;
        input(raw);
        if (action == "down") _pressed = true;
        else if (action == "up") _pressed = false;
    }
}
