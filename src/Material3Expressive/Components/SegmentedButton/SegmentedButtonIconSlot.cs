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

// SegmentedButtonDefaults.Icon consumes the library's AnimatedVisibility / Crossfade.
internal sealed class SegmentedButtonIconSlot : Decorator
{
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<SegmentedButtonIconSlot, double>(nameof(IconSize));
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> EffectsAnimationProperty = AvaloniaProperty.Register<SegmentedButtonIconSlot, MaterialSpring?>(nameof(EffectsAnimation));
    public MaterialSpring? EffectsAnimation { get => GetValue(EffectsAnimationProperty); set => SetValue(EffectsAnimationProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> SpatialAnimationProperty = AvaloniaProperty.Register<SegmentedButtonIconSlot, MaterialSpring?>(nameof(SpatialAnimation));
    public MaterialSpring? SpatialAnimation { get => GetValue(SpatialAnimationProperty); set => SetValue(SpatialAnimationProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_owner is { } owner && (change.Property == IconSizeProperty || change.Property == EffectsAnimationProperty || change.Property == SpatialAnimationProperty))
            Update(owner);
    }

    // Stable state objects prevent AnimatedContent from rebuilding an equal bool slot
    // and trying to reparent a caller-owned icon during an interrupted crossfade.
    private static readonly object ActiveState = true, InactiveState = false;
    private SegmentedButton? _owner;
    private bool _crossfade;
    private object? _active, _inactive;
    private IDataTemplate? _activeTemplate, _inactiveTemplate;
    private Material3Expressive.Foundation.Motion.SpringSpec _effects, _spatial;
    private readonly Control?[] _icons = new Control?[2];
    private VisualAnimationScope? _motion;
    private Transition<bool>? _selection;
    private CancellationTokenSource? _loopCancellation;
    private Task? _loop;
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Stop(); base.OnDetachedFromVisualTree(e);
    }
    internal void Stop()
    {
        // A retired Crossfade slot can be absent from the visual tree while its nested
        // presenter still owns caller content. Release our own presenters explicitly.
        foreach (var presenter in _icons.OfType<ContentPresenter>())
        { presenter.Content = null; presenter.ContentTemplate = null; presenter.UpdateChild(); }
        Child = null; _owner = null;
        _motion?.Dispose(); _motion = null; _selection = null;
        _loopCancellation?.Dispose(); _loopCancellation = null; _loop = null;
        Array.Clear(_icons);
    }
    internal void Update(SegmentedButton owner)
    {
        if (!owner.ShowSelectionIcon) { Stop(); return; }
        // Retain the requested owner before waiting for theme input. A resource-scope move
        // can withdraw old bindings; the property update then retries this same target.
        _owner = owner;
        if (EffectsAnimation is not { } effects || SpatialAnimation is not { } spatial) return;
        var crossfade = owner.InactiveIcon is not null || owner.InactiveIconTemplate is not null;
        var rebuild = _selection is null || _crossfade != crossfade || !Equals(_active, owner.ActiveIcon) || !Equals(_inactive, owner.InactiveIcon)
            || _activeTemplate != owner.ActiveIconTemplate || _inactiveTemplate != owner.InactiveIconTemplate;
        _owner = owner;
        if (rebuild)
        {
            Stop(); _owner = owner; _crossfade = crossfade;
            _active = owner.ActiveIcon; _inactive = owner.InactiveIcon;
            _activeTemplate = owner.ActiveIconTemplate; _inactiveTemplate = owner.InactiveIconTemplate;
            _motion = VisualAnimationScope.Create(owner);
            _selection = _motion.CreateTransition(owner.IsSelected, "Segmented selection icon");
            _effects = effects;
            _spatial = spatial;
            if (crossfade)
                Child = new Crossfade
                {
                    ClipToBounds = false,
                    Content = owner.IsSelected ? ActiveState : InactiveState, Spec = _effects.ToAnimationSpec(),
                    Source = ContentSource.From(_selection, active => active ? ActiveState : InactiveState),
                    ContentTemplate = new FuncDataTemplate<bool>((active, _) => Icon(active)),
                };
            else
                Child = new AnimatedVisibility
                {
                    ClipToBounds = false,
                    Visible = owner.IsSelected, Content = Icon(true),
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
            if (_icons[1] is Checkmark checkmark) checkmark.Size = IconSize;
            foreach (var presenter in _icons.OfType<ContentPresenter>())
            {
                presenter.FlowDirection = owner.FlowDirection;
                MaterialIcon.SetDefaultSize(presenter, IconSize);
            }
            if (_selection!.TargetState != owner.IsSelected)
            {
                var defer = _crossfade || owner.IsSelected;
                if (defer) _loopCancellation?.Cancel();
                _selection.UpdateTarget(owner.IsSelected);
                if (defer) StartLoop(defer: true);
            }
        }
    }
    private void StartLoop(bool defer)
    {
        var previous = _loop; var motion = _motion!; var transition = _selection!;
        var cancellation = new CancellationTokenSource(); var old = _loopCancellation;
        _loopCancellation = cancellation;
        _loop = motion.Launch(async ct =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellation.Token);
            if (previous is not null)
                try { await previous; } catch (OperationCanceledException) { }
            old?.Dispose(); linked.Token.ThrowIfCancellationRequested();
            // Compose commits updateTransition in recomposition. Realize the icon now,
            // then start sampling after that commit frame, on the same shared clock.
            if (defer) await motion.Clock.NextFrameAsync(linked.Token);
            await transition.RunAsync(linked.Token);
        }, "Segmented icon transition");
    }
    private Control Icon(bool active)
    {
        var index = active ? 1 : 0;
        if (_icons[index] is { } existing) return existing;
        var owner = _owner!;
        if (active && owner.ActiveIcon is null && owner.ActiveIconTemplate is null)
        {
            // Icons.Filled.Check: 24x24 viewport, same path as the fixed upstream vector.
            return _icons[index] = new Checkmark(IconSize);
        }
        var presenter = new ContentPresenter { FlowDirection = owner.FlowDirection, Content = active ? owner.ActiveIcon : owner.InactiveIcon,
            ContentTemplate = active ? owner.ActiveIconTemplate : owner.InactiveIconTemplate };
        MaterialIcon.SetDefaultSize(presenter, IconSize);
        return _icons[index] = presenter;
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
