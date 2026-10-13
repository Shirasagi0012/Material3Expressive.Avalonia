using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public sealed class ThreadAffinityTest
{
    [Fact]
    public void Default_IsRejected()
    {
        var affinity = default(ThreadAffinity);
        Assert.Throws<InvalidOperationException>(() => affinity.Verify(this));
    }

    [Fact]
    public async Task OtherThread_MessageNamesOwner()
    {
        var transition = new Transition<bool>(false);
        var error = await Task.Run(() => Assert.Throws<InvalidOperationException>(transition.VerifyThreadAccess));
        Assert.Contains("Transition", error.Message);
        Assert.DoesNotContain("`", error.Message);
    }
}
