using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Catalog.Framework;

/// <summary>
/// 交互环境（实时预览）：逐帧等待环境帧推进；截图为 no-op（仅 Debug 输出）；
/// 记录断言；DemoControlBase.Assert 在失败时抛出异常。生命周期令牌取消离开页面后的动作。
/// </summary>
internal sealed class InteractiveEnvironment(CancellationToken cancellationToken = default) : IDemoEnvironment
{
    public async Task AdvanceAsync(DemoControlBase demo, int frames)
    {
        var topLevel = TopLevel.GetTopLevel(demo);
        for (var i = 0; i < frames; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (topLevel is null)
                await Task.Delay(16, cancellationToken);
            else
                await demo.EnvironmentClock.NextFrameAsync(cancellationToken);
        }
    }

    public Task CaptureAsync(DemoControlBase demo, string name)
    {
        Debug.WriteLine($"[Catalog] 交互模式不输出截图：{name}");
        return Task.CompletedTask;
    }

    public void RecordAssertion(bool success, string message)
    {
        if (!success) Debug.WriteLine($"[Catalog] 断言失败：{message}");
    }

}
