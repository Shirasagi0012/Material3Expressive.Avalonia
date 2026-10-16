/* ButtonGroup.kt: EnlargeOnPressNode and waitUntil. Fixed AndroidX/Core baseline, Apache-2.0. */
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

internal sealed class ButtonGroupWidthMotion : IDisposable
{
    private readonly ButtonGroupItem _item;
    private MaterialInteractionController _interaction;
    private readonly Animatable<float> _progress = new(new FloatConverter(), 0, VisibilityThresholds.Float);
    private AnimationChildScope _scope;
    private CancellationTokenSource? _releaseWait;
    private Task? _releaseTask;
    private int _collectionGeneration;
    private bool _pressed;

    internal ButtonGroupWidthMotion(ButtonGroupItem item, Avalonia.Controls.Control source)
    {
        _item = item;
        _interaction = MaterialInteraction.GetOrCreate(source);
        _scope = CreateScope(AnimationScope.Get(item));
        _interaction.Changed += OnInteractionChanged;
        AnimationScope.AddScopeChangedHandler(item, OnScopeChanged);
        OnInteractionChanged();
    }

    internal float Progress => _progress.Value;

    internal void SetSource(Avalonia.Controls.Control source)
    {
        var interaction = MaterialInteraction.GetOrCreate(source);
        if (ReferenceEquals(_interaction, interaction))
            return;

        // EnlargeOnPressNode changes its collection job without recreating its Animatable.
        _interaction.Changed -= OnInteractionChanged;
        _collectionGeneration++;
        _releaseWait?.Cancel();
        _releaseWait?.Dispose();
        _releaseWait = null;
        _progress.Stop();
        _interaction = interaction;
        _interaction.Changed += OnInteractionChanged;
        _pressed = false;
        OnInteractionChanged();
    }

    private AnimationChildScope CreateScope(AnimationScope parent)
    {
        var scope = parent.CreateChild("ButtonGroup width");
        scope.SubscribeValue(_progress, _ => _item.FindAncestorOfType<ButtonGroupLayout>()?.InvalidateItemsMeasure());
        return scope;
    }

    private void OnScopeChanged(object? sender, AnimationScopeChangedEventArgs e)
    {
        _scope.Dispose();
        if (e.NewScope is not { } parent)
            return;

        _scope = CreateScope(parent);
        if (_pressed || _progress.Value != 0)
            StartMotion();
    }

    private void OnInteractionChanged()
    {
        var pressed = (_interaction.Interaction & InteractionState.Pressed) != 0;
        if (_pressed == pressed)
            return;

        _pressed = pressed;
        StartMotion();
    }

    private void StartMotion()
    {
        var generation = ++_collectionGeneration;
        var previousRelease = _releaseTask;
        _releaseTask = null;
        _releaseWait?.Cancel();
        _releaseWait?.Dispose();
        _releaseWait = null;

        if (_pressed)
        {
            _scope.Launch(async token =>
            {
                // collectLatest cancels AND joins its previous release before launching
                // expansion. Otherwise AnimateTo captures the returning run's old velocity
                // and frame origin before its cancellation cleanup has reset them.
                if (previousRelease is not null)
                {
                    try
                    {
                        await previousRelease;
                    }
                    catch (OperationCanceledException) when (previousRelease.IsCanceled) { }
                }

                token.ThrowIfCancellationRequested();
                if (generation == _collectionGeneration && _pressed)
                    await AnimateTo(1, token);
            }, "Expand");
        }
        else
        {
            _releaseWait = CancellationTokenSource.CreateLinkedTokenSource(_scope.Lifetime);
            var token = _releaseWait.Token;
            _releaseTask = _scope.Launch(_ => ReleaseAfterExpansion(token), "Release after expansion");
        }
    }

    private async Task ReleaseAfterExpansion(CancellationToken token)
    {
        // A release does not cancel the expansion. Upstream always awaits an initial frame,
        // then waits for progress > .75 or strictly more than 1,000 ms of frame time.
        var start = await _scope.NextFrameAsync(token);
        var startMilliseconds = start.Ticks / TimeSpan.TicksPerMillisecond;
        while (_progress.Value <= 0.75f)
        {
            var now = await _scope.NextFrameAsync(token);
            if (now.Ticks / TimeSpan.TicksPerMillisecond - startMilliseconds > 1000)
                break;
        }

        await AnimateTo(0, token);
    }

    private Task AnimateTo(float target, CancellationToken token)
    {
        var group = _item.FindAncestorOfType<ButtonGroup>()!;
        var specification = group.WidthAnimation;
        var spring = new SpringSpec<float>((float)specification.DampingRatio,
            (float)specification.Stiffness, (float)specification.VisibilityThreshold);
        return _progress.AnimateToAsync(_scope, target, spring, cancellationToken: token);
    }

    public void Dispose()
    {
        _collectionGeneration++;
        _interaction.Changed -= OnInteractionChanged;
        AnimationScope.RemoveScopeChangedHandler(_item, OnScopeChanged);
        _releaseWait?.Cancel();
        _releaseWait?.Dispose();
        _scope.Dispose();
    }
}
