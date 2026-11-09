// PullToRefresh.kt / PullToRefreshState, pinned AndroidX baseline; Apache-2.0.
using System.ComponentModel;

namespace Material3Expressive.Components;

/// <summary>Observable pull distance and the operations used by indicators and nested-scroll hosts.</summary>
/// <remarks>Publish property changes on the UI thread. Implementations own their animation lifetime;
/// operations must honor cancellation from their caller. Distance is not clamped to the threshold.</remarks>
public interface IPullToRefreshState : INotifyPropertyChanged
{
    float DistanceFraction { get; }
    bool IsAnimating { get; }
    Task AnimateToThresholdAsync(CancellationToken cancellationToken = default);
    Task AnimateToHiddenAsync(CancellationToken cancellationToken = default);
    Task SnapToAsync(float distanceFraction, CancellationToken cancellationToken = default);
}
