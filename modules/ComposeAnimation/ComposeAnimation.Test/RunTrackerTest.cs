namespace ComposeAnimation.Test;

public sealed class RunTrackerTest
{
    [Fact]
    public Task Track_AfterScopeEnds_ReportsOwningScope() => OnUi(async () =>
    {
        var window = new AnimationTestWindow(new ManualFrameSource(), new ManualTimeProvider());
        var child = window.AnimationRoot.CreateChild();
        var typed = child.Track(() => Task.FromResult(7));
        Assert.Equal(7, await typed);
        child.Dispose();
        await child.Diagnostics.EndAsync();
        var error = Assert.Throws<ObjectDisposedException>(() => { _ = child.Track(() => Task.CompletedTask); });
        Assert.Equal(child.GetType().FullName, error.ObjectName);
        window.Close();
    });
}
