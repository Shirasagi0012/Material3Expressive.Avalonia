namespace ComposeAnimationRuntime;

public sealed class InteractiveTransition
{
    private readonly Animatable<double> _progress = new(0, new DoubleVectorConverter());
    private readonly TweenSpec<double> _settleSpec;

    public InteractiveTransition(TimeSpan settleDuration)
    {
        if (settleDuration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(settleDuration));
        _settleSpec = new TweenSpec<double>(new DoubleVectorConverter(), settleDuration, Easings.FastOutSlowIn);
    }

    public double Progress => _progress.Value;
    public bool IsRunning => _progress.IsRunning;
    public event Action<double>? ProgressChanged;

    public void SetProgress(double progress)
    {
        _progress.SnapTo(Math.Clamp(progress, 0, 1));
        ProgressChanged?.Invoke(Progress);
    }

    public void Release(double velocity = 0, double completionThreshold = 0.5)
    {
        var target = Progress >= completionThreshold || velocity > 0 ? 1 : 0;
        _progress.AnimateTo(target, _settleSpec);
    }

    public bool Tick(TimeSpan delta)
    {
        var running = _progress.Tick(delta);
        ProgressChanged?.Invoke(Progress);
        return running;
    }

    public void Cancel() => _progress.Stop();
    public void Complete() => _progress.SnapTo(1);
}
