namespace ComposeAnimationRuntime;

public enum VisibilityState { Hidden, Visible }

/// <summary>Coordinates enter and exit motion while retaining content during exit.</summary>
public sealed class VisibilityMotion
{
    private readonly Animatable<double> _progress;
    private readonly IAnimationVectorConverter<double> _converter = new DoubleVectorConverter();
    private readonly IAnimationSpec<double> _enterSpec;
    private readonly IAnimationSpec<double> _exitSpec;

    public VisibilityMotion(bool initiallyVisible, IAnimationSpec<double>? enterSpec = null, IAnimationSpec<double>? exitSpec = null)
    {
        _enterSpec = enterSpec ?? new SpringAnimationSpec<double>(_converter, 1, 1500);
        _exitSpec = exitSpec ?? new TweenSpec<double>(_converter, TimeSpan.FromMilliseconds(120));
        _progress = new Animatable<double>(initiallyVisible ? 1 : 0, _converter);
        State = initiallyVisible ? VisibilityState.Visible : VisibilityState.Hidden;
    }
    public VisibilityState State { get; private set; }
    public double Progress => _progress.Value;
    public bool IsRunning => _progress.IsRunning;
    public void Show(IAnimationSpec<double>? spec = null) { State = VisibilityState.Visible; _progress.AnimateTo(1, spec ?? _enterSpec); }
    public void Hide(IAnimationSpec<double>? spec = null) { _progress.AnimateTo(0, spec ?? _exitSpec); }
    public bool Tick(TimeSpan delta) { var running = _progress.Tick(delta); if (!running && _progress.Value <= 0) State = VisibilityState.Hidden; return running; }
    public void SnapTo(bool visible) { _progress.SnapTo(visible ? 1 : 0); State = visible ? VisibilityState.Visible : VisibilityState.Hidden; }
    public void Pause() => _progress.Pause();
    public void Resume() => _progress.Resume();
    public void Complete() { _progress.Settle(); if (_progress.Value <= 0) State = VisibilityState.Hidden; }
    public void Cancel() => _progress.Stop();
}
