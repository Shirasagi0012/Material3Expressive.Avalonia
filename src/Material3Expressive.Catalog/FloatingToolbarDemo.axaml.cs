using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ComposeAnimation;
using Material3Expressive.Components;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Catalog;

public partial class FloatingToolbarDemo : Window
{
    public static readonly StyledProperty<MaterialSpring> TransitionSpringProperty =
        AvaloniaProperty.Register<FloatingToolbarDemo, MaterialSpring>(nameof(TransitionSpring));
    public MaterialSpring TransitionSpring { get => GetValue(TransitionSpringProperty); set => SetValue(TransitionSpringProperty, value); }
    public FloatingToolbarDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        UpdateTransitions();
        ScrollingToolbar.ScrollBehavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(FloatingToolbarExitDirection.Bottom);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TransitionSpringProperty) UpdateTransitions();
    }
    private void UpdateTransitions()
    {
        if (HorizontalTransition is null || VerticalTransition is null || TransitionSpring.Stiffness <= 0) return;
        HorizontalTransition.Enter = FloatingToolbarDefaults.HorizontalEnterTransition(HorizontalTransition, VisibilityHorizontalAlignment.Start, TransitionSpring);
        HorizontalTransition.Exit = FloatingToolbarDefaults.HorizontalExitTransition(HorizontalTransition, VisibilityHorizontalAlignment.End, TransitionSpring);
        VerticalTransition.Enter = FloatingToolbarDefaults.VerticalEnterTransition(VerticalTransition, VerticalAlignment.Bottom, TransitionSpring);
        VerticalTransition.Exit = FloatingToolbarDefaults.VerticalExitTransition(VerticalTransition, VerticalAlignment.Top, TransitionSpring);
    }
}
