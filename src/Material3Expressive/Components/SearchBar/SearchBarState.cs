// SearchBar.kt / SearchBarState, fixed AndroidX baseline. Apache-2.0.
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public enum SearchBarValue { Collapsed, Expanded }

/// <summary>Application-owned progress for a collapsed search bar and its expanded presentation.</summary>
public sealed class SearchBarState : AvaloniaObject
{
    public static readonly DirectProperty<SearchBarState, float> ProgressProperty = AvaloniaProperty.RegisterDirect<SearchBarState, float>(nameof(Progress), s => s.Progress);
    public static readonly DirectProperty<SearchBarState, SearchBarValue> CurrentValueProperty = AvaloniaProperty.RegisterDirect<SearchBarState, SearchBarValue>(nameof(CurrentValue), s => s.CurrentValue);
    public static readonly DirectProperty<SearchBarState, SearchBarValue> TargetValueProperty = AvaloniaProperty.RegisterDirect<SearchBarState, SearchBarValue>(nameof(TargetValue), s => s.TargetValue);
    public static readonly DirectProperty<SearchBarState, bool> IsAnimatingProperty = AvaloniaProperty.RegisterDirect<SearchBarState, bool>(nameof(IsAnimating), s => s.IsAnimating);

    private readonly Animatable<float> _animation, _content;
    private readonly IAnimationSpec<float>? _expandSpec, _collapseSpec;
    private readonly IAnimationSpec<float> _contentInSpec, _contentOutSpec;
    private readonly HashSet<Control> _owners = [];
    private AnimationChildScope? _scope;
    private TopLevel? _topLevel;
    private float _progress;
    private SearchBarValue _current, _target;
    private bool _running;
    private enum DefaultMotion { Standard, Contained, WithGap }
    private DefaultMotion _defaultMotion;
    internal bool ExpandsToFullScreen { get; private set; }
    internal void MarkFullScreen() { if (ExpandsToFullScreen) return; ExpandsToFullScreen = true; NotifyLayoutChanged(); }
    internal SearchBar? CollapsedBar { get; set; }
    internal event EventHandler? LayoutChanged;
    internal void NotifyLayoutChanged() => LayoutChanged?.Invoke(this, EventArgs.Empty);
    internal float RawProgress => _animation.Value;
    internal float ContentProgress => Math.Clamp(_content.Value, 0, 1);
    internal event EventHandler? ContentProgressChanged;

    public SearchBarState() : this(SearchBarValue.Collapsed) { }
    /// <summary>Creates the contained variant's FastSpatial motion and 50/100ms content fade defaults.</summary>
    public static SearchBarState CreateContained(SearchBarValue initialValue = SearchBarValue.Collapsed) => CreateWithContentFade(initialValue, DefaultMotion.Contained);
    /// <summary>Creates the gap variant's DefaultSpatial expansion, FastSpatial collapse and delayed content fade.</summary>
    public static SearchBarState CreateWithGap(SearchBarValue initialValue = SearchBarValue.Collapsed) => CreateWithContentFade(initialValue, DefaultMotion.WithGap);
    private static SearchBarState CreateWithContentFade(SearchBarValue initialValue, DefaultMotion motion) => new(initialValue,
        animationSpecForContentFadeIn: new TweenSpec<float>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50), new CubicBezierEasing(.3, 0, 1, 1)),
        animationSpecForContentFadeOut: new TweenSpec<float>(TimeSpan.FromMilliseconds(100), easing: new CubicBezierEasing(0, 0, 0, 1))) { _defaultMotion = motion };
    public SearchBarState(SearchBarValue initialValue, IAnimationSpec<float>? animationSpecForExpand = null,
        IAnimationSpec<float>? animationSpecForCollapse = null, IAnimationSpec<float>? animationSpecForContentFadeIn = null,
        IAnimationSpec<float>? animationSpecForContentFadeOut = null)
    {
        _progress = initialValue == SearchBarValue.Expanded ? 1 : 0;
        _current = _target = initialValue;
        _animation = new(new FloatConverter(), _progress, .01f, "Search bar progress");
        _content = new(new FloatConverter(), _progress, .01f, "Search bar content");
        _expandSpec = animationSpecForExpand; _collapseSpec = animationSpecForCollapse;
        _contentInSpec = animationSpecForContentFadeIn ?? new SnapSpec<float>();
        _contentOutSpec = animationSpecForContentFadeOut ?? new SnapSpec<float>();
        _animation.PropertyChanged += AnimationChanged;
        _content.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Animatable<float>.Value)) ContentProgressChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>Progress clamped to 0–1. Layout retains the underlying spring overshoot.</summary>
    public float Progress => _progress;
    /// <summary>Collapsed while raw progress is at most .02; otherwise Expanded, including during exit.</summary>
    public SearchBarValue CurrentValue => _current;
    public SearchBarValue TargetValue => _target;
    public new bool IsAnimating => _running;
    public Task AnimateToExpandedAsync(CancellationToken cancellationToken = default) => Animate(true, cancellationToken);
    public Task AnimateToCollapsedAsync(CancellationToken cancellationToken = default) => Animate(false, cancellationToken);

    /// <summary>Sets raw progress without clamping or changing the independent content animation.</summary>
    public Task SnapToAsync(float fraction, CancellationToken cancellationToken = default) => _animation.SnapToAsync(fraction, cancellationToken);

    internal void Attach(Control owner)
    {
        var topLevel = TopLevel.GetTopLevel(owner) ?? throw new InvalidOperationException("The search bar must be mounted.");
        if (_topLevel is not null && !ReferenceEquals(_topLevel, topLevel))
            throw new InvalidOperationException("A SearchBarState cannot animate in multiple windows at the same time.");
        if (!_owners.Add(owner)) return;
        _topLevel = topLevel;
        // The application-owned state outlives either individual consumer (anchor or popup).
        _scope ??= AnimationScope.GetRoot(topLevel).CreateChild("Search bar state");
    }
    internal void Detach(Control owner)
    {
        if (!_owners.Remove(owner) || _owners.Count != 0) return;
        _scope?.Dispose(); _scope = null;
        _topLevel = null;
    }

    private Task Animate(bool expanded, CancellationToken cancellationToken)
    {
        VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        var scope = _scope ?? throw new InvalidOperationException("Mount a SearchBar or ExpandedDockedSearchBar before animating its state. SnapToAsync can seed an unmounted state.");
        var spec = AnimationSpec(expanded);
        return scope.Launch(async lifetime =>
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellationToken);
            var target = expanded ? 1f : 0f;
            async Task Run(Animatable<float> animation, IAnimationSpec<float> animationSpec)
            {
                try { await animation.AnimateToAsync(scope, target, animationSpec, cancellationToken: cancellation.Token); }
                catch { cancellation.Cancel(); throw; }
            }
            // Preserve the upstream coroutine launch order; either child failure cancels its sibling.
            if (expanded) await Task.WhenAll(Run(_animation, spec), Run(_content, _contentInSpec));
            else await Task.WhenAll(Run(_content, _contentOutSpec), Run(_animation, spec));
        }, expanded ? "Expand search" : "Collapse search");
    }

    internal IAnimationSpec<float> AnimationSpec(bool expanded)
    {
        var spec = expanded ? _expandSpec : _collapseSpec;
        if (spec is null)
        {
            var property = _defaultMotion switch
            {
                DefaultMotion.Contained => SearchBar.ContainedAnimationProperty,
                DefaultMotion.WithGap => expanded ? SearchBar.GapExpandAnimationProperty : SearchBar.GapCollapseAnimationProperty,
                _ => expanded ? SearchBar.ExpandAnimationProperty : SearchBar.CollapseAnimationProperty
            };
            var configured = _owners.Select(owner => owner.GetValue(property)).FirstOrDefault(value => value is not null)
                ?? throw new InvalidOperationException("The search bar motion theme is not ready.");
            spec = new SpringSpec<float>(configured.DampingRatio, configured.Stiffness);
        }
        return spec;
    }

    private void AnimationChanged(object? sender, PropertyChangedEventArgs e)
    {
        SetAndRaise(ProgressProperty, ref _progress, Math.Clamp(_animation.Value, 0, 1));
        SetAndRaise(CurrentValueProperty, ref _current, _animation.Value <= .02f ? SearchBarValue.Collapsed : SearchBarValue.Expanded);
        SetAndRaise(TargetValueProperty, ref _target, _animation.TargetValue == 1 ? SearchBarValue.Expanded : SearchBarValue.Collapsed);
        SetAndRaise(IsAnimatingProperty, ref _running, _animation.IsRunning);
        // Raw spring overshoot still changes layout when public clamped progress remains 1.
        if (e.PropertyName == nameof(Animatable<float>.Value)) NotifyLayoutChanged();
    }
}
