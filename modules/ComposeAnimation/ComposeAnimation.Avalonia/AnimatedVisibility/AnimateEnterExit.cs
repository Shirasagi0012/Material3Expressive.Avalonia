using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>A child effect that participates in the nearest AnimatedVisibility's completion.</summary>
public class AnimateEnterExit : Decorator
{
    public static readonly StyledProperty<EnterTransition> EnterProperty =
        AvaloniaProperty.Register<AnimateEnterExit, EnterTransition>(nameof(Enter), new FadeIn(), validate: value => value is not null);
    public static readonly StyledProperty<ExitTransition> ExitProperty =
        AvaloniaProperty.Register<AnimateEnterExit, ExitTransition>(nameof(Exit), new FadeOut(), validate: value => value is not null);
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AnimateEnterExit, string?>(nameof(Label));
    private readonly VisibilityLayer _layer;

    static AnimateEnterExit()
        => ChildProperty.Changed.AddClassHandler<AnimateEnterExit>((host, _) => host.MoveVisualChild());

    public AnimateEnterExit()
    {
        _layer = new VisibilityLayer
        {
            // Visual ancestors only; this is not included. Nested hosts win when closer.
            ResolveScope = () => this.GetVisualAncestors().OfType<IEnterExitScopeHost>().FirstOrDefault()?.Scope,
            Enter = () => Enter,
            Exit = () => Exit,
            Label = () => Label
        };
        _layer.Surface.OwnsLogicalChild = false;
        VisualChildren.Add(_layer);
    }

    public EnterTransition Enter { get => GetValue(EnterProperty); set => SetValue(EnterProperty, value); }
    public ExitTransition Exit { get => GetValue(ExitProperty); set => SetValue(ExitProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    private void MoveVisualChild()
    {
        _layer.Surface.Child = null;
        // The base class handler has now established the logical and visual parents.
        if (Child is not null) VisualChildren.Remove(Child);
        _layer.Surface.Child = Child;
        _layer.ResetMotion();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _layer.FlowDirection = FlowDirection;
        _layer.Measure(availableSize.Deflate(Padding));
        return _layer.DesiredSize.Inflate(Padding);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _layer.Arrange(new Rect(finalSize).Deflate(Padding));
        return finalSize;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _layer.ResetMotion();
        base.OnDetachedFromVisualTree(e);
    }
}
