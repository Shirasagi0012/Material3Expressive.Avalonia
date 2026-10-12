using System.Reflection;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class FrameScopeTest
{
    private static Task OnUi(Func<Task> action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None);

    [Fact]
    public void FrameScope_ExposesOnlyLifetimeDurationScaleAndNextFrame_AndCannotBeDerivedPublicly()
    {
        var members = typeof(FrameScope)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m is not MethodInfo { IsSpecialName: true })
            .Select(m => m.Name).Order().ToArray();
        Assert.Equal(["DurationScale", "Lifetime", "NextFrameAsync"], members);
        Assert.All(typeof(FrameScope).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            c => Assert.True(c.IsAssembly));
        Assert.True(typeof(FrameScope).IsAssignableFrom(typeof(AnimationScope)));
    }

    [Fact]
    public Task Scope_SuppliesFramesTrackingAndEffectiveScale() => OnUi(async () =>
    {
        using var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source);
        FrameScope scope = window.AnimationRoot.CreateChild();
        scope.DurationScale = 2;
        ((AnimationScope)scope).Host.SetValue(AnimationScope.DurationScaleProperty, 1.5);
        Assert.Equal(2 * 1.5, scope.EffectiveDurationScale, 7);

        var frame = scope.NextFrameAsync();
        var tracked = scope.Track(async () => await scope.NextFrameAsync());
        Assert.Equal(1, ((AnimationScope)scope).Diagnostics.PendingAnimationCount);
        source.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs();
        await frame;
        await tracked;
        Assert.Equal(0, ((AnimationScope)scope).Diagnostics.PendingAnimationCount);

        ((AnimationChildScope)scope).Dispose();
        Assert.True(scope.IsEnding);
        Assert.Throws<ObjectDisposedException>(scope.VerifyAccess);
        await Assert.ThrowsAnyAsync<Exception>(async () => await scope.NextFrameAsync());
        window.Close();
    });
}
