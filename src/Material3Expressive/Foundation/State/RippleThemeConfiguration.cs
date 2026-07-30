using Avalonia;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>The focus visual produced by Material 3's ripple node.</summary>
public enum RippleFocusIndication
{
    /// <summary>A translucent state layer using the focus opacity token.</summary>
    Opacity,

    /// <summary>Two animated strokes inset from the component's shape.</summary>
    InsetRing
}

/// <summary>
/// Theme-wide configuration for Material 3 interaction indications.
/// </summary>
/// <remarks>
/// Mirrors AndroidX <c>RippleThemeConfiguration</c>. AndroidX retains opacity focus as a
/// compatibility option; this Material 3 Expressive framework selects the new inset-ring
/// treatment by default.
/// </remarks>
public partial class RippleThemeConfiguration : AvaloniaObject
{
    public RippleThemeConfiguration()
    {
        FocusIndication = RippleFocusIndication.InsetRing;
        OuterStrokeInset = 0;
        OuterStrokeWidth = 2;
        InnerStrokeInset = 1;
        InnerStrokeWidth = 3;
    }

    [GeneratedStyledProperty] public partial RippleFocusIndication FocusIndication { get; set; }
    [GeneratedStyledProperty] public partial double OuterStrokeInset { get; set; }
    [GeneratedStyledProperty] public partial double OuterStrokeWidth { get; set; }
    [GeneratedStyledProperty] public partial double InnerStrokeInset { get; set; }
    [GeneratedStyledProperty] public partial double InnerStrokeWidth { get; set; }

    /// <summary>Creates AndroidX's two-stroke inset focus-ring configuration.</summary>
    public static RippleThemeConfiguration CreateInsetFocusRing()
    {
        return new RippleThemeConfiguration { FocusIndication = RippleFocusIndication.InsetRing };
    }

    /// <summary>Creates the compatibility opacity-focus configuration.</summary>
    public static RippleThemeConfiguration CreateOpacity()
    {
        return new RippleThemeConfiguration { FocusIndication = RippleFocusIndication.Opacity };
    }

    public object ProvideValue() => this;
}

/// <summary>Immutable ripple configuration carried by a resolved Material theme.</summary>
public sealed class ResolvedRippleThemeConfiguration
{
    public ResolvedRippleThemeConfiguration(RippleThemeConfiguration configuration)
    {
        FocusIndication = configuration.FocusIndication;
        OuterStrokeInset = configuration.OuterStrokeInset;
        OuterStrokeWidth = configuration.OuterStrokeWidth;
        InnerStrokeInset = configuration.InnerStrokeInset;
        InnerStrokeWidth = configuration.InnerStrokeWidth;
    }

    public RippleFocusIndication FocusIndication { get; }
    public double OuterStrokeInset { get; }
    public double OuterStrokeWidth { get; }
    public double InnerStrokeInset { get; }
    public double InnerStrokeWidth { get; }
}
