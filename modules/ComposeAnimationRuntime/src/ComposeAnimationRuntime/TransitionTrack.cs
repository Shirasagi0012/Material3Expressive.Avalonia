namespace ComposeAnimationRuntime;

public interface ITransitionTrack
{
    bool IsRunning { get; }
    bool Tick(TimeSpan delta);
    void Pause();
    void Resume();
    void Complete();
    void Cancel();
}
