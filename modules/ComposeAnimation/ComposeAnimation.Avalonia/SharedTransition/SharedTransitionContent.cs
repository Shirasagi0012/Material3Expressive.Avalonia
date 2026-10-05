using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal enum SharedContentKind { Element, Bounds }

/// <summary>
/// Chooses the bounds animation spec from the shared element's initial and target rectangles.
/// </summary>
public class SharedBoundsTransform
{
    public IFiniteAnimationSpec<Rect> Spec { get; set; } = new SpringSpec<Rect>();
    public virtual IFiniteAnimationSpec<Rect> Create(Rect from, Rect to) => Spec;
}

/// <summary>
/// Registers with the nearest <see cref="SharedTransitionHost"/> when an
/// <see cref="AnimatedVisibility"/> scope is created. Does not start a parent Transition.
/// </summary>
public abstract class SharedTransitionContent : ContentControl
{
    public static readonly StyledProperty<object?> KeyProperty =
        AvaloniaProperty.Register<SharedTransitionContent, object?>(nameof(Key));
    public static readonly StyledProperty<SharedBoundsTransform?> BoundsTransformProperty =
        AvaloniaProperty.Register<SharedTransitionContent, SharedBoundsTransform?>(nameof(BoundsTransform));
    public static readonly StyledProperty<double> ZIndexInOverlayProperty =
        AvaloniaProperty.Register<SharedTransitionContent, double>(nameof(ZIndexInOverlay));

    private SharedSuppressLayer? _layer;
    private SharedTransitionHost? _host;
    private AnimatedVisibility? _visibility;
    private SharedTransitionParticipant? _participant;
    private bool _attached;
    internal static readonly IControlTemplate DefaultSharedTemplate;

    static SharedTransitionContent()
    {
        HorizontalContentAlignmentProperty.OverrideDefaultValue<SharedTransitionContent>(HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<SharedTransitionContent>(VerticalAlignment.Stretch);
        DefaultSharedTemplate = new FuncControlTemplate<SharedTransitionContent>((host, names) =>
        {
            var presenter = new SharedContentPresenter
            {
                UseLayoutRounding = false,
                Name = "PART_ContentPresenter",
                [~ContentProperty] = new TemplateBinding(ContentProperty),
                [~ContentTemplateProperty] = new TemplateBinding(ContentTemplateProperty),
                [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty),
                [~BorderThicknessProperty] = new TemplateBinding(BorderThicknessProperty),
                [~CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty),
                [~PaddingProperty] = new TemplateBinding(PaddingProperty),
                [~HorizontalContentAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty),
                [~VerticalContentAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty)
            };
            names.Register(presenter.Name, presenter);
            var layer = new SharedSuppressLayer { Name = "PART_SharedLayer" };
            layer.Surface.Child = presenter;
            names.Register(layer.Name, layer);
            names.Register("PART_Capture", layer.Surface);
            return layer;
        });
        TemplateProperty.OverrideDefaultValue<SharedTransitionContent>(DefaultSharedTemplate);
    }

    public object? Key { get => GetValue(KeyProperty); set => SetValue(KeyProperty, value); }
    public SharedBoundsTransform? BoundsTransform { get => GetValue(BoundsTransformProperty); set => SetValue(BoundsTransformProperty, value); }
    public double ZIndexInOverlay { get => GetValue(ZIndexInOverlayProperty); set => SetValue(ZIndexInOverlayProperty, value); }
    internal abstract SharedContentKind Kind { get; }
    internal SharedCaptureSurface? Capture => _layer?.Surface;
    internal SharedSuppressLayer? Suppress => _layer;
    internal SharedTransitionParticipant? Participant => _participant;
    internal SharedTransitionHost? SharedHost => _host;
    internal int TreeDepth
    {
        get
        {
            var depth = 0;
            for (var parent = this.GetVisualParent(); parent is not null; parent = parent.GetVisualParent())
                if (parent is SharedTransitionContent) depth++;
            return depth;
        }
    }
    internal SharedTransitionContent? ParentShared =>
        this.GetVisualAncestors().OfType<SharedTransitionContent>().FirstOrDefault();
    internal bool IsLayoutAffectedByAncestorFlight() =>
        this is not SharedBounds { Remeasures: true } &&
        this.GetVisualAncestors().OfType<SharedBounds>().Any(bounds => bounds.Remeasures && bounds.FlightBounds is not null);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _layer = e.NameScope.Find<SharedSuppressLayer>("PART_SharedLayer")
            ?? throw new InvalidOperationException("Shared transition content requires its default suppress/capture template.");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        Connect();
        if (_host is { IsOverlayActive: false })
            _layer?.SetSuppressed(false, _layer.SuppressGeneration);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        if (_visibility is not null) _visibility.ScopeCreated -= ScopeCreated;
        // Only the current preparation/session may retain a detached endpoint. It must not
        // enroll in a replacement visibility scope while its visual is no longer present.
        _participant?.Detach();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KeyProperty)
        {
            // A recycled endpoint must not keep matching under its former registration key.
            if (!_attached) Unregister();
            else Connect();
        }
    }

    private void Connect()
    {
        if (!_attached) return;
        ApplyTemplate();
        var host = this.GetVisualAncestors().OfType<SharedTransitionHost>().FirstOrDefault();
        var visibility = this.GetVisualAncestors().OfType<AnimatedVisibility>().FirstOrDefault();
        if (!ReferenceEquals(_host, host) || !ReferenceEquals(_visibility, visibility))
        {
            Disconnect();
            _host = host;
            _visibility = visibility;
        }
        if (_visibility is not null)
        {
            _visibility.ScopeCreated -= ScopeCreated;
            _visibility.ScopeCreated += ScopeCreated;
        }
        if (_visibility?.Scope is { } scope) Register(scope);
    }

    private void Disconnect()
    {
        if (_visibility is not null) _visibility.ScopeCreated -= ScopeCreated;
        Unregister();
        _layer?.SetSuppressed(false, _layer.SuppressGeneration);
        _visibility = null;
        _host = null;
    }

    private void ScopeCreated(object? sender, VisibilityScopeEventArgs e) => Register(e.Scope);

    private void Register(AnimatedVisibilityScope scope)
    {
        if (!_attached) return;
        Unregister();
        if (Key is null || _host is null || scope.IsDisposed) return;
        _participant = _host.Register(this, scope);
        // Detached registrations survive only while shared preparation/session work owns them.
    }

    private void Unregister()
    {
        var participant = _participant;
        _participant = null;
        _host?.Unregister(this, participant);
        participant?.Dispose();
    }

    internal virtual Rect? ReadLayoutBounds()
    {
        if (_host is null || Capture is null || !Capture.IsMeasureValid || !Capture.IsArrangeValid) return null;
        return SharedLayoutBounds.ReadLayout(Capture, _host);
    }
}
