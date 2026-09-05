namespace ComposeAnimationRuntime;

public interface ITransitionTrack
{
    bool IsRunning { get; }
    bool Tick(TimeSpan delta);
    void Seek(TimeSpan playTime) { }
    void Pause();
    void Resume();
    void Complete();
    void Cancel();
}
