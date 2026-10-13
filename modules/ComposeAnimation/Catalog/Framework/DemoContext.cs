using ComposeAnimation.Runtime;

namespace Catalog.Framework;

/// <summary>
/// demo 的运行上下文。交互模式下 <see cref="FrameSource"/> 为 null，控件走环境默认时钟；
/// 自动化模式下为 <see cref="ProbeFrameSource"/>，由宿主逐帧推进。
/// </summary>
public sealed class DemoContext
{
    /// <summary>交互模式共享的环境上下文（FrameSource = null）。</summary>
    public static DemoContext Ambient { get; } = new();

    public IFrameSource? FrameSource { get; init; }

    public bool IsAutomation { get; init; }
}
