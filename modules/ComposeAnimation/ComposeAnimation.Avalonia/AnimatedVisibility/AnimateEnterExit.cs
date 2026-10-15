using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>A child effect that participates in the nearest AnimatedVisibility's completion.</summary>
public class AnimateEnterExit : Decorator, ILookaheadLayoutPolicy, ILookaheadPreparationParticipant
{
    public static readonly StyledProperty<EnterTransition> EnterProperty =
        AvaloniaProperty.Register<AnimateEnterExit, EnterTransition>(nameof(Enter), new FadeIn(),
            validate: value => value is { });

    public static readonly StyledProperty<ExitTransition> ExitProperty =
        AvaloniaProperty.Register<AnimateEnterExit, ExitTransition>(nameof(Exit), new FadeOut(), validate: value => value is { });

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AnimateEnterExit, string?>(nameof(Label));

    private readonly EnterExitLayout _layout;
    private EnterExitMotion? _motion;

    static AnimateEnterExit()
    {
        ChildProperty.Changed.AddClassHandler<AnimateEnterExit>((host, _) => host.MoveVisualChild());
    }

    public AnimateEnterExit()
    {
        LookaheadLayout.RegisterInherited(this);
        _layout = new EnterExitLayout();
        _layout.Surface.OwnsLogicalChild = false;
        VisualChildren.Add(_layout);
    }

    public EnterTransition Enter
    {
        get => GetValue(EnterProperty);
        set => SetValue(EnterProperty, value);
    }

    public ExitTransition Exit
    {
        get => GetValue(ExitProperty);
        set => SetValue(ExitProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private void MoveVisualChild()
    {
        _layout.SetMotion(null);
        _motion?.Dispose();
        _motion = null;
        _layout.Surface.Child = null;
        if (Child is { }) VisualChildren.Remove(Child);
        _layout.Surface.Child = Child;
        InvalidateMeasure();
    }

    private void SynchronizeMotion()
    {
        AnimatedVisibilityScope? scope = this.GetVisualAncestors().OfType<IEnterExitScopeHost>().FirstOrDefault()?.Scope;
        if (scope?.IsDisposed == true) scope = null;
        if (_motion?.Scope != scope)
        {
            _layout.SetMotion(null);
            _motion?.Dispose();
            _motion = scope is null ? null : new EnterExitMotion(scope, Label);
            _layout.SetMotion(_motion);
        }

        _motion?.UpdateTransitions(Enter, Exit);
    }

    void ILookaheadPreparationParticipant.PrepareLookahead() => SynchronizeMotion();
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => [_layout];
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => [PaddingProperty];

    LookaheadMeasureResult ILookaheadLayoutPolicy.MeasureLayout(LookaheadMeasureContext context, LookaheadConstraints constraints)
    {
        LookaheadPlaceable content = context.Measure(_layout, constraints.Deflate(Padding));
        return context.Layout(content.DesiredSize.Inflate(Padding),
            placement => placement.Arrange(content, new Rect(placement.FinalSize).Deflate(Padding)));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        SynchronizeMotion();
        return LookaheadPolicyRuntime.MeasureActual(this, this, LookaheadPolicyRuntime.ActualPhase(this),
            LookaheadConstraints.AtMost(availableSize));
    }

    protected override Size ArrangeOverride(Size finalSize) => LookaheadPolicyRuntime.ArrangeActual(this, finalSize);

    protected override Size MeasureCore(Size availableSize)
    {
        SynchronizeMotion();
        return LookaheadLayout.TryMeasureCore(this, availableSize, out Size desired) ? desired : base.MeasureCore(availableSize);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadLayout.TryArrangeCore(this, finalRect, out Rect bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) LookaheadLayout.VisualParentChanged(this);
        if (change.Property == EnterProperty || change.Property == ExitProperty)
            _motion?.UpdateTransitions(Enter, Exit);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _layout.SetMotion(null);
        _motion?.Dispose();
        _motion = null;
        LookaheadLayout.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }
}
