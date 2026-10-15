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
        Layer = new EnterExitLayout();
        VisualChildren.Add(Layer);
        LogicalChildren.Add(Layer);
        Layer.Surface.Child = Presenter;
    }

    internal AnimatedContent Host { get; }
    internal object? State { get; set; }
    internal object? Key { get; set; }
    internal ContentPresenter Presenter { get; }
    internal EnterExitLayout Layer { get; }
    private EnterExitMotion? _motion;
    private EnterTransition _enter = EnterTransition.None;
    private ExitTransition _exit = ExitTransition.None;
    internal EnterTransition Enter
    {
        get => _enter;
        set { _enter = value; UpdateMotionTransitions(); }
    }
    internal ExitTransition Exit
    {
        get => _exit;
        set { _exit = value; UpdateMotionTransitions(); }
    }
    internal float ZIndexFloat { get; set; }
    internal bool IsTarget { get; set; }
    internal IDataTemplate? FrozenTemplate { get; set; }
    internal EnterExitStateMapper<object?>? Mapper { get; set; }
    public AnimatedVisibilityScope? Scope { get; private set; }

    internal void AttachScope(AnimatedVisibilityScope scope)
    {
        Scope = scope;
        _motion = new EnterExitMotion(scope, Host.Label ?? "AnimatedContent");
        UpdateMotionTransitions();
        Layer.SetMotion(_motion);
        scope.Transition.PhaseChanged += Host.OnPhaseChanged;
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

    internal bool Hold => EnterExitEffects.Read(Exit).Hold;

    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateMotionTransitions();
        Layer.FlowDirection = FlowDirection;
        Layer.Measure(availableSize);
        Scope?.MarkInitialLayoutReady();
        return Layer.MeasuredContentSize;
    }

    private void UpdateMotionTransitions()
        => _motion?.UpdateTransitions(Enter, Exit, Host is Crossfade crossfade ? crossfade.Spec : null);

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
            scope.Transition.PhaseChanged -= Host.OnPhaseChanged;
            Layer.SetMotion(null);
            _motion?.Dispose();
            _motion = null;
            scope.Dispose();
            Scope = null;
        }
        Presenter.Content = null;
        Presenter.ContentTemplate = null;
        Presenter.UpdateChild();
    }
}
