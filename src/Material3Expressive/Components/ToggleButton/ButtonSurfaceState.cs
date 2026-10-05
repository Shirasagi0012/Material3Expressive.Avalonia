using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Motion;

namespace Material3Expressive.Components;

// Template-only inputs to the existing Surface shape engine. No extra input or ripple mechanism.
internal static class ButtonSurfaceState
{
    public static readonly AttachedProperty<bool?> IsCheckedProperty = AvaloniaProperty.RegisterAttached<MaterialSurface, bool?>("IsChecked", typeof(ButtonSurfaceState));
    public static bool? GetIsChecked(MaterialSurface owner) => owner.GetValue(IsCheckedProperty);
    public static void SetIsChecked(MaterialSurface owner, bool? value) => owner.SetValue(IsCheckedProperty, value);
    public static readonly AttachedProperty<CornerRadius?> CheckedCornerRadiusProperty = AvaloniaProperty.RegisterAttached<MaterialSurface, CornerRadius?>("CheckedCornerRadius", typeof(ButtonSurfaceState));
    public static CornerRadius? GetCheckedCornerRadius(MaterialSurface owner) => owner.GetValue(CheckedCornerRadiusProperty);
    public static void SetCheckedCornerRadius(MaterialSurface owner, CornerRadius? value) => owner.SetValue(CheckedCornerRadiusProperty, value);
    public static readonly AttachedProperty<SpringSpec?> ShapeSpecProperty = AvaloniaProperty.RegisterAttached<MaterialSurface, SpringSpec?>("ShapeSpec", typeof(ButtonSurfaceState));
    public static SpringSpec? GetShapeSpec(MaterialSurface owner) => owner.GetValue(ShapeSpecProperty);
    public static void SetShapeSpec(MaterialSurface owner, SpringSpec? value) => owner.SetValue(ShapeSpecProperty, value);
    public static readonly AttachedProperty<SpringSpec?> BorderWidthAnimationProperty = AvaloniaProperty.RegisterAttached<MaterialSurface, SpringSpec?>("BorderWidthAnimation", typeof(ButtonSurfaceState));
    public static SpringSpec? GetBorderWidthAnimation(MaterialSurface owner) => owner.GetValue(BorderWidthAnimationProperty);
    public static void SetBorderWidthAnimation(MaterialSurface owner, SpringSpec? value) => owner.SetValue(BorderWidthAnimationProperty, value);
    public static readonly AttachedProperty<SpringSpec?> BorderColorAnimationProperty = AvaloniaProperty.RegisterAttached<MaterialSurface, SpringSpec?>("BorderColorAnimation", typeof(ButtonSurfaceState));
    public static SpringSpec? GetBorderColorAnimation(MaterialSurface owner) => owner.GetValue(BorderColorAnimationProperty);
    public static void SetBorderColorAnimation(MaterialSurface owner, SpringSpec? value) => owner.SetValue(BorderColorAnimationProperty, value);
    public static readonly AttachedProperty<bool> ResetOnShapeChangeProperty = AvaloniaProperty.RegisterAttached<MaterialSurface, bool>("ResetOnShapeChange", typeof(ButtonSurfaceState), true);
    public static bool GetResetOnShapeChange(MaterialSurface owner) => owner.GetValue(ResetOnShapeChangeProperty);
    public static void SetResetOnShapeChange(MaterialSurface owner, bool value) => owner.SetValue(ResetOnShapeChangeProperty, value);
    public static readonly AttachedProperty<bool> AnimateBorderProperty = AvaloniaProperty.RegisterAttached<MaterialSurface, bool>("AnimateBorder", typeof(ButtonSurfaceState));
    public static bool GetAnimateBorder(MaterialSurface owner) => owner.GetValue(AnimateBorderProperty);
    public static void SetAnimateBorder(MaterialSurface owner, bool value) => owner.SetValue(AnimateBorderProperty, value);
    public static readonly AttachedProperty<double> CheckedOverlayOpacityProperty = AvaloniaProperty.RegisterAttached<MaterialSurface, double>("CheckedOverlayOpacity", typeof(ButtonSurfaceState));
    public static double GetCheckedOverlayOpacity(MaterialSurface owner) => owner.GetValue(CheckedOverlayOpacityProperty);
    public static void SetCheckedOverlayOpacity(MaterialSurface owner, double value) => owner.SetValue(CheckedOverlayOpacityProperty, value);

    static ButtonSurfaceState()
    {
        IsCheckedProperty.Changed.AddClassHandler<MaterialSurface>((s, _) => s.OnButtonStateChanged());
        CheckedCornerRadiusProperty.Changed.AddClassHandler<MaterialSurface>((s, _) => s.OnButtonStateChanged());
        ShapeSpecProperty.Changed.AddClassHandler<MaterialSurface>((s, _) => s.OnButtonStateChanged());
        BorderWidthAnimationProperty.Changed.AddClassHandler<MaterialSurface>((s, _) => s.OnBorderAnimationConfigurationChanged());
        BorderColorAnimationProperty.Changed.AddClassHandler<MaterialSurface>((s, _) => s.OnBorderAnimationConfigurationChanged());
        ResetOnShapeChangeProperty.Changed.AddClassHandler<MaterialSurface>((s, _) => s.OnButtonStateChanged());
        CheckedOverlayOpacityProperty.Changed.AddClassHandler<MaterialSurface>((s, _) => s.OnButtonStateChanged());
    }
}
