using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Rendering;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

/// <summary>The reusable selection icon from SegmentedButtonDefaults.Icon.</summary>
/// <remarks>Without inactive content, selection enters with fade and scale and exits immediately.
/// With inactive content, the two slots crossfade. The control owns no selection input.</remarks>
public sealed class SegmentedButtonIcon : Decorator
{
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<SegmentedButtonIcon, double>(nameof(IconSize));
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    internal static readonly StyledProperty<MaterialSpring?> EffectsAnimationProperty = AvaloniaProperty.Register<SegmentedButtonIcon, MaterialSpring?>(nameof(EffectsAnimation));
    internal MaterialSpring? EffectsAnimation { get => GetValue(EffectsAnimationProperty); set => SetValue(EffectsAnimationProperty, value); }
    internal static readonly StyledProperty<MaterialSpring?> SpatialAnimationProperty = AvaloniaProperty.Register<SegmentedButtonIcon, MaterialSpring?>(nameof(SpatialAnimation));
    internal MaterialSpring? SpatialAnimation { get => GetValue(SpatialAnimationProperty); set => SetValue(SpatialAnimationProperty, value); }
    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<SegmentedButtonIcon, bool>(nameof(IsActive));
    public static readonly StyledProperty<object?> ActiveContentProperty = AvaloniaProperty.Register<SegmentedButtonIcon, object?>(nameof(ActiveContent));
    public static readonly StyledProperty<object?> InactiveContentProperty = AvaloniaProperty.Register<SegmentedButtonIcon, object?>(nameof(InactiveContent));
    public static readonly StyledProperty<IDataTemplate?> ActiveContentTemplateProperty = AvaloniaProperty.Register<SegmentedButtonIcon, IDataTemplate?>(nameof(ActiveContentTemplate));
    public static readonly StyledProperty<IDataTemplate?> InactiveContentTemplateProperty = AvaloniaProperty.Register<SegmentedButtonIcon, IDataTemplate?>(nameof(InactiveContentTemplate));
    public static readonly StyledProperty<IBrush?> ForegroundProperty = Avalonia.Controls.Documents.TextElement.ForegroundProperty.AddOwner<SegmentedButtonIcon>();
    public bool IsActive { get => GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
    /// <summary>Null, without a template, uses the default vector checkmark.</summary>
    public object? ActiveContent { get => GetValue(ActiveContentProperty); set => SetValue(ActiveContentProperty, value); }
    public object? InactiveContent { get => GetValue(InactiveContentProperty); set => SetValue(InactiveContentProperty, value); }
    public IDataTemplate? ActiveContentTemplate { get => GetValue(ActiveContentTemplateProperty); set => SetValue(ActiveContentTemplateProperty, value); }
    public IDataTemplate? InactiveContentTemplate { get => GetValue(InactiveContentTemplateProperty); set => SetValue(InactiveContentTemplateProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconSizeProperty || change.Property == EffectsAnimationProperty || change.Property == SpatialAnimationProperty
            || change.Property == IsActiveProperty || change.Property == ActiveContentProperty || change.Property == InactiveContentProperty
            || change.Property == ActiveContentTemplateProperty || change.Property == InactiveContentTemplateProperty
            || change.Property == FlowDirectionProperty || change.Property == IsVisibleProperty)
            Update();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Update();
    }

    // Stable state objects prevent AnimatedContent from rebuilding an equal bool slot
    // and trying to reparent a caller-owned icon during an interrupted crossfade.
    private static readonly object ActiveState = true, InactiveState = false;
    private bool _crossfade;
    private Material3Expressive.Foundation.Motion.SpringSpec _effects, _spatial;
    private readonly Control?[] _icons = new Control?[2];
    private Checkmark? _checkmark;
    private Transition<bool>? _selection;
    private CancellationTokenSource? _loopCancellation;
    private Task? _loop;
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Stop(); base.OnDetachedFromVisualTree(e);
    }
    internal void Stop()
    {
        // Cancel the transition before removing its animation hosts. Disposing a CTS alone
        // leaves the loop awaiting the owner's next frame, including after root teardown.
        _loopCancellation?.Cancel();
        // A retired Crossfade slot can be absent from the visual tree while its nested
        // presenter still owns caller content. Release our own presenters explicitly.
        foreach (var presenter in _icons.OfType<ContentPresenter>())
        { presenter.Content = null; presenter.ContentTemplate = null; presenter.UpdateChild(); }
        Child = null;
        _selection = null;
        _loopCancellation?.Dispose(); _loopCancellation = null; _loop = null;
        Array.Clear(_icons);
        _checkmark = null;
    }
    private void Update()
    {
        if (!IsVisible) { Stop(); return; }
        if (!this.IsAttachedToVisualTree()) return;
        // Missing theme input defers the current target until property bindings arrive.
        if (EffectsAnimation is not { } effects || SpatialAnimation is not { } spatial) return;
        var crossfade = InactiveContent is not null || InactiveContentTemplate is not null;
        var rebuild = _selection is null || _crossfade != crossfade;
        if (rebuild)
        {
            Stop(); _crossfade = crossfade;
            _selection = new Transition<bool>(IsActive, "Segmented selection icon");
            _effects = effects;
            _spatial = spatial;
            if (crossfade)
                Child = new Crossfade
                {
                    ClipToBounds = false,
                    Content = IsActive ? ActiveState : InactiveState, Spec = _effects.ToAnimationSpec(),
                    Source = ContentSource.From(_selection, active => active ? ActiveState : InactiveState),
                    ContentTemplate = new FuncDataTemplate<bool>((active, _) => Icon(active)),
                };
            else
                Child = new AnimatedVisibility
                {
                    ClipToBounds = false,
                    // Compose scaleIn's (0, 1) origin is physical. Keep this animation
                    // host unmirrored; each content presenter still receives the caller's RTL.
                    FlowDirection = FlowDirection.LeftToRight,
                    Visible = IsActive, Content = Icon(true),
                    Exit = ExitTransition.None,
                    Source = VisibilitySource.From(_selection, active => active),
                    Enter = new FadeIn { Spec = _effects.ToAnimationSpec() } + new ScaleIn
                    {
                        InitialScale = 0, TransformOrigin = new RelativePoint(0, 1, RelativeUnit.Relative),
                        Spec = _spatial.ToAnimationSpec(),
                    },
                };
            StartLoop(defer: false);
        }
        else
        {
            if (_effects != effects || _spatial != spatial)
            {
                _effects = effects; _spatial = spatial;
                if (Child is Crossfade crossfadeHost) crossfadeHost.Spec = effects.ToAnimationSpec();
                if (Child is AnimatedVisibility visibility) visibility.Enter = new FadeIn { Spec = effects.ToAnimationSpec() }
                    + new ScaleIn { InitialScale = 0, TransformOrigin = new RelativePoint(0, 1, RelativeUnit.Relative), Spec = spatial.ToAnimationSpec() };
            }
            if (_checkmark is { } checkmark) checkmark.Size = IconSize;
            for (var i = 0; i < _icons.Length; i++)
                if (_icons[i] is ContentPresenter presenter) UpdateIconContent(presenter, i == 1);
            if (_selection!.TargetState != IsActive)
            {
                var defer = _crossfade || IsActive;
                if (defer) _loopCancellation?.Cancel();
                _selection.UpdateTarget(IsActive);
                if (defer) StartLoop(defer: true);
            }
        }
    }
    private void StartLoop(bool defer)
    {
        if (!this.IsAttachedToVisualTree()) return;
        var motion = AnimationScope.Get(this);
        var previous = _loop; var transition = _selection!;
        var cancellation = new CancellationTokenSource(); var old = _loopCancellation;
        _loopCancellation = cancellation;
        _loop = motion.Launch(async ct =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellation.Token);
            if (previous is not null)
                try { await previous; } catch (OperationCanceledException) { }
            old?.Dispose(); if (linked.Token.IsCancellationRequested) return;
            // Compose commits updateTransition in recomposition. Realize the icon now,
            // then start sampling after that commit frame, on the same shared clock.
            if (defer && !await motion.WaitForNextFrameAsync(linked.Token)) return;
            await transition.RunAsync(motion, linked.Token);
        }, "Segmented icon transition");
    }
    private Control Icon(bool active)
    {
        var index = active ? 1 : 0;
        if (_icons[index] is { } existing) return existing;
        var presenter = new ContentPresenter();
        UpdateIconContent(presenter, active);
        return _icons[index] = presenter;
    }
    private void UpdateIconContent(ContentPresenter presenter, bool active)
    {
        presenter.FlowDirection = FlowDirection;
        presenter.ContentTemplate = active ? ActiveContentTemplate : InactiveContentTemplate;
        // Updating content does not recreate the remembered visibility/crossfade transition.
        presenter.Content = active && ActiveContent is null && ActiveContentTemplate is null
            ? _checkmark ??= new Checkmark(IconSize)
            : active ? ActiveContent : InactiveContent;
        MaterialSymbol.SetDefaultSize(presenter, IconSize);
    }
    private sealed class Checkmark(double size) : Control
    {
        public double Size { get => size; set { if (size != value) { size = value; InvalidateMeasure(); } } }
        protected override bool BypassFlowDirectionPolicies => true;
        private static readonly Geometry Path = Geometry.Parse("M9,16.17 L4.83,12 l-1.42,1.41 L9,19 L21,7 l-1.41,-1.41 Z");
        static Checkmark() => AffectsRender<Checkmark>(Avalonia.Controls.Documents.TextElement.ForegroundProperty);
        // Modifier.size is constrained by the parent's maximum. Explicit Avalonia Height
        // would instead arrange an overflowing 18dp visual inside the 16dp slot.
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(size, availableSize.Width), Math.Min(size, availableSize.Height));
        public override void Render(DrawingContext context)
        {
            var scale = Math.Min(Bounds.Width, Bounds.Height) / 24;
            using var transform = context.PushTransform(Matrix.CreateScale(scale, scale)
                * Matrix.CreateTranslation((Bounds.Width - 24 * scale) / 2, (Bounds.Height - 24 * scale) / 2));
            context.DrawGeometry(Avalonia.Controls.Documents.TextElement.GetForeground(this), null, Path);
        }
    }
}
