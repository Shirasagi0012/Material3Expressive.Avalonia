// ScrollField.kt / PagerState.kt at docs/upstream-baseline.json. Apache-2.0.
using Avalonia;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

/// <summary>Selection and scrolling state for a cyclic <see cref="ScrollField"/>.</summary>
public sealed class ScrollFieldState : AvaloniaObject
{
    internal const int PageCount = 100_000;
    public static readonly DirectProperty<ScrollFieldState, int> SelectedOptionProperty =
        AvaloniaProperty.RegisterDirect<ScrollFieldState, int>(nameof(SelectedOption), s => s.SelectedOption, (s, v) => s.SelectedOption = v);
    public static readonly DirectProperty<ScrollFieldState, int> TargetOptionProperty =
        AvaloniaProperty.RegisterDirect<ScrollFieldState, int>(nameof(TargetOption), s => s.TargetOption);
    public static readonly DirectProperty<ScrollFieldState, bool> IsScrollInProgressProperty =
        AvaloniaProperty.RegisterDirect<ScrollFieldState, bool>(nameof(IsScrollInProgress), s => s.IsScrollInProgress);
    private int _selected, _target;
    private bool _scrolling;
    private bool _userScrolling;
    private ScrollField? _owner;
    private AnimationChildScope? _scope;
    private CancellationTokenSource? _cancellation;
    private int? _programmaticTarget;
    private float _accumulator;
    internal int CurrentPage { get; private set; }
    internal int TargetPage { get; private set; }
    internal float OffsetFraction { get; private set; }
    internal int PageSize { get; private set; }
    internal int ViewportHeight { get; private set; }
    internal int SnapPosition => ViewportHeight / 2 - PageSize / 2;
    internal int FirstVisiblePage => Math.Clamp(CurrentPage + (int)MathF.Floor((OffsetFraction * PageSize - SnapPosition) / PageSize), 0, PageCount - 1);
    internal int LastVisiblePage => Math.Clamp(CurrentPage + (int)MathF.Ceiling((ViewportHeight + OffsetFraction * PageSize - SnapPosition) / PageSize) - 1, 0, PageCount - 1);
    internal event Action? LayoutChanged;

    public ScrollFieldState(int itemCount, int index = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemCount);
        ItemCount = itemCount;
        CurrentPage = Math.Clamp(PageCount / 2 - PageCount / 2 % itemCount + Math.Clamp(index, 0, itemCount - 1), 0, PageCount - 1);
        TargetPage = CurrentPage;
        _selected = _target = CurrentPage % itemCount;
    }

    public int ItemCount { get; }
    /// <summary>Current option. Assignment cancels scrolling and requests this option in the current cycle.</summary>
    public int SelectedOption
    {
        get => _selected;
        set { VerifyAccess(); SnapToPage(CalculateTargetPage(value)); }
    }
    public int TargetOption => _target;
    public bool IsScrollInProgress => _scrolling;
    public Task ScrollToOptionAsync(int option, CancellationToken cancellationToken = default)
    {
        VerifyAccess(); cancellationToken.ThrowIfCancellationRequested();
        if (_userScrolling) return Task.FromCanceled(new CancellationToken(true));
        SnapToPage(CalculateTargetPage(option));
        return Task.CompletedTask;
    }
    public Task AnimateScrollToOptionAsync(int option, CancellationToken cancellationToken = default)
        => AnimateToPageAsync(CalculateTargetPage(option), cancellationToken);
    private int CalculateTargetPage(int option) => CurrentPage + Math.Clamp(option, 0, ItemCount - 1) - CurrentPage % ItemCount;

    internal void Attach(ScrollField owner)
    {
        if (_owner is not null && _owner != owner) throw new InvalidOperationException("A ScrollFieldState can be attached to only one field at a time.");
        _owner = owner;
    }
    internal void Detach(ScrollField owner)
    {
        if (_owner != owner) return;
        CancelScroll(); _owner = null;
    }
    internal void Configure(int pageSize, int height)
    {
        PageSize = Math.Max(1, pageSize); ViewportHeight = height;
    }
    private void SnapToPage(int page)
    {
        CancelScroll();
        CurrentPage = Math.Clamp(page, 0, PageCount - 1); OffsetFraction = 0; _accumulator = 0;
        Publish();
    }
    private void Publish(bool forward = false)
    {
        TargetPage = !_scrolling ? CurrentPage : _programmaticTarget ??
            (PageSize > 0 && MathF.Abs(OffsetFraction) >= MathF.Min(56 * Density, PageSize / 2f) / PageSize
                ? Math.Clamp(FirstVisiblePage + (forward ? 1 : 0), 0, PageCount - 1) : CurrentPage);
        SetAndRaise(SelectedOptionProperty, ref _selected, CurrentPage % ItemCount);
        SetAndRaise(TargetOptionProperty, ref _target, TargetPage % ItemCount);
        LayoutChanged?.Invoke();
    }
    internal float Density => (float)(Avalonia.Controls.TopLevel.GetTopLevel(_owner!)?.RenderScaling ?? 1);
    internal void BeginDrag() { CancelScroll(); _userScrolling = true; SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true); }
    internal float Consume(float delta)
    {
        var accumulation = delta + _accumulator;
        var integer = MathUtils.RoundToInt(accumulation);
        _accumulator = accumulation - integer;
        if (MathF.Abs(delta) < 1e-4f) return delta;
        // Keep the position relative to the current page: a Float absolute position loses
        // pixels near page 50,000. Pager stores integer page + local pixel offset as well.
        var oldOffset = MathUtils.RoundToInt(OffsetFraction * PageSize);
        var requested = (long)CurrentPage * PageSize + oldOffset + integer;
        var clamped = Math.Clamp(requested, 0, (long)(PageCount - 1) * PageSize);
        var consumed = clamped - ((long)CurrentPage * PageSize + oldOffset);
        var page = (int)(clamped / PageSize);
        var offset = (int)(clamped % PageSize);
        // minBy distance chooses the earlier page for an exact tie.
        if (offset * 2 > PageSize) { page++; offset -= PageSize; }
        CurrentPage = page;
        OffsetFraction = (float)offset / PageSize;
        Publish(delta > 0);
        return requested == clamped ? delta : consumed;
    }
    internal void CancelScroll()
    {
        var cancellation = _cancellation; _cancellation = null;
        cancellation?.Cancel(); _scope?.Dispose(); _scope = null;
        _programmaticTarget = null;
        _userScrolling = false;
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
        Publish();
    }

    internal Task AnimateToPageAsync(int page, CancellationToken cancellationToken = default)
    {
        VerifyAccess(); cancellationToken.ThrowIfCancellationRequested();
        page = Math.Clamp(page, 0, PageCount - 1);
        if (page == CurrentPage && OffsetFraction == 0) return Task.CompletedTask;
        if (_userScrolling) return Task.FromCanceled(new CancellationToken(true));
        var owner = RequireOwner();
        owner.CancelPointer(); CancelScroll();
        var first = FirstVisiblePage; var last = LastVisiblePage;
        var forward = page > first;
        if (((forward && page > last) || (!forward && page < first)) && Math.Abs(page - first) >= 3)
        {
            CurrentPage = forward ? Math.Max(page - (last - first + 1), first) : Math.Min(page + (last - first + 1), first);
            OffsetFraction = 0;
        }
        _programmaticTarget = page;
        return RunScroll(async (scope, token) =>
        {
            var displacement = MathUtils.RoundToInt((page - CurrentPage) * PageSize - OffsetFraction * PageSize);
            var previous = 0f;
            await SuspendAnimation.AnimateAsync(scope, new FloatConverter(), 0, (float)displacement,
                animationSpec: new SpringSpec<float>(), block: (value, _) => previous += Consume(value - previous), cancellationToken: token);
        }, cancellationToken);
    }

    internal Task FlingAsync(float velocity, float gestureDelta, CancellationToken cancellationToken = default)
    {
        RequireOwner(); CancelScroll();
        return RunScroll(async (scope, token) =>
        {
            // PagerSnapDistance.atMost(1) always has a zero approach. Its snap spring uses
            // the integer visibility threshold, unlike Carousel's default Float threshold.
            await SnapFling.RunAsync(scope, velocity, DecayAnimationSpec.SplineBasedDecay<float>(Density),
                new SpringSpec<float>(stiffness: 400, visibilityThreshold: 1), Consume, (_, _) => 0,
                speed => SnapOffset(speed, gestureDelta), token, remaining =>
                {
                    _programmaticTarget = Math.Clamp(CurrentPage + MathUtils.RoundToInt(remaining / PageSize), 0, PageCount - 1);
                    Publish();
                });
        }, cancellationToken);
    }
    internal Task WheelScrollAsync(Func<AnimationChildScope, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        RequireOwner().CancelPointer(); CancelScroll();
        _userScrolling = true;
        return RunScroll(action, cancellationToken);
    }
    private float SnapOffset(float velocity, float gestureDelta)
    {
        var lower = float.NegativeInfinity; var upper = float.PositiveInfinity;
        for (var page = FirstVisiblePage; page <= LastVisiblePage; page++)
        {
            var distance = (page - CurrentPage) * PageSize - MathUtils.RoundToInt(OffsetFraction * PageSize);
            if (distance <= 0 && distance > lower) lower = distance;
            if (distance >= 0 && distance < upper) upper = distance;
        }
        if (float.IsNegativeInfinity(lower)) lower = upper;
        if (float.IsPositiveInfinity(upper)) upper = lower;
        var forward = (int)gestureDelta == 0 ? velocity >= 0 : gestureDelta <= 0;
        if (CurrentPage == PageCount - 1 && OffsetFraction == 0) { upper = 0; if (forward) lower = 0; }
        if (CurrentPage == 0 && OffsetFraction == 0) { lower = 0; if (!forward) upper = 0; }
        float result;
        if (MathF.Abs(velocity) >= 400 * Density) result = velocity > 0 ? upper : lower;
        else
        {
            var fromSnap = gestureDelta / PageSize;
            if (MathF.Abs(fromSnap - (int)fromSnap) > .5f) result = forward ? upper : lower;
            else if (MathF.Abs(fromSnap) >= MathF.Min(56 * Density, PageSize / 2f) / PageSize) result = forward ? lower : upper;
            else result = MathF.Abs(lower) < MathF.Abs(upper) ? lower : upper;
        }
        return float.IsFinite(result) ? result : 0;
    }
    private ScrollField RequireOwner() => _owner is { } owner && PageSize > 0 ? owner
        : throw new InvalidOperationException("Mount and lay out the ScrollField before starting a scroll animation.");
    private Task RunScroll(Func<AnimationChildScope, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var scope = _scope = AnimationScope.Get(RequireOwner()).CreateChild("Scroll field movement");
        var cancellation = _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.Lifetime);
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true); Publish();
        return Run();
        async Task Run()
        {
            try { await action(scope, cancellation.Token); }
            finally
            {
                if (ReferenceEquals(_cancellation, cancellation))
                {
                    _cancellation = null; _scope = null; _programmaticTarget = null;
                    _userScrolling = false;
                    SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false); Publish();
                }
                cancellation.Dispose(); scope.Dispose();
            }
        }
    }
}
