using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal sealed class ContentSlot : Control, IEnterExitScopeHost
{
    internal ContentSlot(AnimatedContent host, object? state, object? key)
    {
        Host = host;
        State = state;
        Key = key;
        Presenter = new ContentPresenter
        {
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Top
        };
        Layer = new VisibilityLayer
        {
            ResolveScope = () => Scope,
            Enter = () => Enter,
            Exit = () => Exit,
            CrossfadeAlphaSpec = host is Crossfade crossfade ? () => crossfade.Spec : null,
            Label = () => Host.Label ?? "AnimatedContent",
            Measured = () => Scope?.Measured()
        };
        VisualChildren.Add(Layer);
        LogicalChildren.Add(Layer);
        Layer.Surface.Child = Presenter;
    }

    internal AnimatedContent Host { get; }
    internal object? State { get; set; }
    internal object? Key { get; set; }
    internal ContentPresenter Presenter { get; }
    internal VisibilityLayer Layer { get; }
    internal EnterTransition Enter { get; set; } = EnterTransition.None;
    internal ExitTransition Exit { get; set; } = ExitTransition.None;
    internal float ZIndexFloat { get; set; }
    internal bool IsTarget { get; set; }
    internal IDataTemplate? FrozenTemplate { get; set; }
    internal EnterExitStateMapper<object?>? Mapper { get; set; }
    public AnimatedVisibilityScope? Scope { get; private set; }

    internal void AttachScope(AnimatedVisibilityScope scope)
    {
        Scope = scope;
        scope.Transition.PropertyChanged += Host.OnSlotTransitionChanged;
    }

    internal void ApplyZIndex(float z)
    {
        ZIndexFloat = z;
        ZIndex = (int)Math.Round(z, MidpointRounding.AwayFromZero);
    }

    internal void ReplaceState(object? state, object? key)
    {
        State = state;
        Key = key;
        Presenter.Content = state;
        Presenter.UpdateChild();
        Presenter.InvalidateMeasure();
        Layer.InvalidateMeasure();
        InvalidateMeasure();
    }

    internal bool Hold
    {
        get
        {
            var effects = new VisibilityEffects();
            Exit.Apply(effects);
            return effects.Hold;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Layer.FlowDirection = FlowDirection;
        Layer.Measure(availableSize);
        return Layer.FullSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var layoutSize = Layer.DesiredSize;
        if (layoutSize == default) layoutSize = finalSize;
        var offset = Layer.Motion?.ChangesSize == true
            ? Layer.Motion.AlignmentOffset(finalSize, layoutSize, FlowDirection)
            : default;
        Layer.Arrange(new Rect(offset, layoutSize));
        return finalSize;
    }

    internal void Detach()
    {
        if (Scope is { } scope)
        {
            scope.Transition.PropertyChanged -= Host.OnSlotTransitionChanged;
            Layer.ResetMotion();
            scope.Dispose();
            Scope = null;
        }
        Presenter.Content = null;
        Presenter.ContentTemplate = null;
        Presenter.UpdateChild();
    }
}
