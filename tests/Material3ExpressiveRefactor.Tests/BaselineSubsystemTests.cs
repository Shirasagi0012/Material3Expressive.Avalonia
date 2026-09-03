using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Baseline;
using Material3ExpressiveRefactor.Foundation;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Tokens;
using Xunit;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// The static baseline subsystem sources (State/Elevation/Shape/Motion/Typography): spec values,
/// stock value types, facade↔source completeness (every declared key answers), alias hops through
/// the resolver, and miss behavior.
/// </summary>
public class BaselineSubsystemTests
{
    private static Button HostIn(params ITokenSource[] sources)
    {
        var theme = new MaterialResource();
        foreach (var source in sources)
            theme.Children.Add(source);
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);
        return new Button { Resources = resources };
    }

    // ---------- completeness sweeps ----------

    [AvaloniaFact]
    public void State_AnswersEveryFacadeKey() =>
        TokenScan.AssertComplete(new StateTokenSource(), typeof(MaterialTokens.Sys.State));

    [AvaloniaFact]
    public void Elevation_AnswersEveryFacadeKey() =>
        TokenScan.AssertComplete(new ElevationTokenSource(), typeof(MaterialTokens.Sys.Elevation));

    [AvaloniaFact]
    public void Shape_AnswersEveryFacadeKey() =>
        TokenScan.AssertComplete(new ShapeTokenSource(), typeof(MaterialTokens.Sys.Shape), typeof(MaterialTokens.Ref.Shape));

    [AvaloniaFact]
    public void Motion_AnswersEveryFacadeKey() =>
        TokenScan.AssertComplete(new MotionTokenSource(), typeof(MaterialTokens.Sys.Motion));

    [AvaloniaFact]
    public void Typography_AnswersEveryFacadeKey() =>
        TokenScan.AssertComplete(new TypographyTokenSource(), typeof(MaterialTokens.Sys.TypeScale), typeof(MaterialTokens.Ref.Typeface));

    // ---------- value spot checks ----------

    [AvaloniaFact]
    public void State_SpecValues()
    {
        var source = new StateTokenSource();

        Assert.Equal(0.08d, Get<double>(source, MaterialTokens.Sys.State.HoverStateLayerOpacity));
        Assert.Equal(0.10d, Get<double>(source, MaterialTokens.Sys.State.FocusStateLayerOpacity));
        Assert.Equal(0.10d, Get<double>(source, MaterialTokens.Sys.State.PressedStateLayerOpacity));
        Assert.Equal(0.16d, Get<double>(source, MaterialTokens.Sys.State.DraggedStateLayerOpacity));
        Assert.Equal(0.12d, Get<double>(source, MaterialTokens.Sys.State.DisabledContainerOpacity));
        Assert.Equal(0.38d, Get<double>(source, MaterialTokens.Sys.State.DisabledContentOpacity));
    }

    [AvaloniaFact]
    public void State_IgnoresThemeVariant()
    {
        var source = new StateTokenSource();

        Assert.True(source.TryGetToken(MaterialTokens.Sys.State.HoverStateLayerOpacity, ThemeVariant.Dark, out var dark));
        Assert.True(source.TryGetToken(MaterialTokens.Sys.State.HoverStateLayerOpacity, ThemeVariant.Light, out var light));
        Assert.Equal(light, dark);
    }

    [AvaloniaFact]
    public void Elevation_Levels()
    {
        // Elevation tokens are plain DIPs; pixel-level shadow derivation lives in the
        // MaterialSkiaShadow renderer, not in the token stock.
        var source = new ElevationTokenSource();

        Assert.Equal(0d, Get<double>(source, MaterialTokens.Sys.Elevation.Level0));
        Assert.Equal(1d, Get<double>(source, MaterialTokens.Sys.Elevation.Level1));
        Assert.Equal(3d, Get<double>(source, MaterialTokens.Sys.Elevation.Level2));
        Assert.Equal(6d, Get<double>(source, MaterialTokens.Sys.Elevation.Level3));
        Assert.Equal(8d, Get<double>(source, MaterialTokens.Sys.Elevation.Level4));
        Assert.Equal(12d, Get<double>(source, MaterialTokens.Sys.Elevation.Level5));
    }

    [AvaloniaFact]
    public void Shape_ScaleAndDirectionVariants()
    {
        var source = new ShapeTokenSource();

        Assert.Equal(new CornerRadius(0), Get<CornerRadius>(source, MaterialTokens.Sys.Shape.None));
        Assert.Equal(new CornerRadius(4), Get<CornerRadius>(source, MaterialTokens.Sys.Shape.ExtraSmall));
        Assert.Equal(new CornerRadius(4, 4, 0, 0), Get<CornerRadius>(source, MaterialTokens.Sys.Shape.ExtraSmallTop));
        Assert.Equal(new CornerRadius(16, 16, 0, 0), Get<CornerRadius>(source, MaterialTokens.Sys.Shape.LargeTop));
        Assert.Equal(new CornerRadius(0, 16, 16, 0), Get<CornerRadius>(source, MaterialTokens.Sys.Shape.LargeEnd));
        Assert.Equal(new CornerRadius(48), Get<CornerRadius>(source, MaterialTokens.Sys.Shape.ExtraExtraLarge));
        Assert.Equal(new CornerRadius(1000), Get<CornerRadius>(source, MaterialTokens.Sys.Shape.Full));

        Assert.Equal(28d, Get<double>(source, MaterialTokens.Ref.Shape.CornerExtraLarge));
        Assert.Equal(20d, Get<double>(source, MaterialTokens.Ref.Shape.CornerLargeIncreased));
    }

    [AvaloniaFact]
    public void Motion_DurationsAndSprings()
    {
        var source = new MotionTokenSource();

        Assert.Equal(TimeSpan.FromMilliseconds(50), Get<TimeSpan>(source, MaterialTokens.Sys.Motion.Duration.Short1));
        Assert.Equal(TimeSpan.FromMilliseconds(400), Get<TimeSpan>(source, MaterialTokens.Sys.Motion.Duration.Medium4));
        Assert.Equal(TimeSpan.FromMilliseconds(1000), Get<TimeSpan>(source, MaterialTokens.Sys.Motion.Duration.ExtraLong4));

        Assert.Equal(0.8d, Get<double>(source, MaterialTokens.Sys.Motion.Spring.SpatialDefaultDamping));
        Assert.Equal(380d, Get<double>(source, MaterialTokens.Sys.Motion.Spring.SpatialDefaultStiffness));
        Assert.Equal(0.6d, Get<double>(source, MaterialTokens.Sys.Motion.Spring.SpatialFastDamping));
        Assert.Equal(800d, Get<double>(source, MaterialTokens.Sys.Motion.Spring.SpatialFastStiffness));
        Assert.Equal(1.0d, Get<double>(source, MaterialTokens.Sys.Motion.Spring.EffectsDefaultDamping));
        Assert.Equal(1600d, Get<double>(source, MaterialTokens.Sys.Motion.Spring.EffectsDefaultStiffness));
        Assert.Equal(1.0d, Get<double>(source, MaterialTokens.Sys.Motion.Spring.EffectsFastDamping));
        Assert.Equal(3800d, Get<double>(source, MaterialTokens.Sys.Motion.Spring.EffectsFastStiffness));
    }

    [AvaloniaFact]
    public void Typography_BodyLargeSlot()
    {
        var source = new TypographyTokenSource();

        Assert.Equal(16d, Get<double>(source, MaterialTokens.Sys.TypeScale.BodyLarge.Size));
        Assert.Equal(FontWeight.Regular, Get<FontWeight>(source, MaterialTokens.Sys.TypeScale.BodyLarge.Weight));
        Assert.Equal(0.5d, Get<double>(source, MaterialTokens.Sys.TypeScale.BodyLarge.LetterSpacing));
        Assert.Equal(24d, Get<double>(source, MaterialTokens.Sys.TypeScale.BodyLarge.LineHeight));
    }

    [AvaloniaFact]
    public void Typography_EmphasizedSlotsCarryHeavierWeights()
    {
        var source = new TypographyTokenSource();

        Assert.Equal(FontWeight.Medium, Get<FontWeight>(source, MaterialTokens.Sys.TypeScale.DisplayLargeEmphasized.Weight));
        Assert.Equal(FontWeight.Bold, Get<FontWeight>(source, MaterialTokens.Sys.TypeScale.TitleMediumEmphasized.Weight));
        Assert.Equal(FontWeight.Bold, Get<FontWeight>(source, MaterialTokens.Sys.TypeScale.LabelMediumEmphasized.Weight));
        // Emphasized roles keep the base metrics.
        Assert.Equal(14d, Get<double>(source, MaterialTokens.Sys.TypeScale.BodyMediumEmphasized.Size));
    }

    [AvaloniaFact]
    public void Typography_FontAliasesResolveThroughTheChain()
    {
        // sys.typescale font slots are TokenRef entries into ref.typeface — the baseline's own
        // first real alias customer (§7.3). Resolving from a host must follow the hop.
        var host = HostIn(new TypographyTokenSource());

        Assert.True(TokenResolver.TryResolve(host, MaterialTokens.Sys.TypeScale.BodyLarge.Font, null, out var value));
        Assert.Equal("Roboto", Assert.IsType<FontFamily>(value).Name);

        Assert.True(TokenResolver.TryResolve(host, MaterialTokens.Sys.TypeScale.DisplayLarge.Font, null, out var display));
        Assert.Equal("Roboto", Assert.IsType<FontFamily>(display).Name);
    }

    [AvaloniaFact]
    public void Typography_RefTypefaceOverride_RestylesEveryRole()
    {
        // R2' on the baseline alias: an inner scope re-pointing ref.typeface.plain must win over
        // the baseline entry even though the sys.typescale TokenRef itself is untouched.
        var overrides = new TestTokenSource();
        overrides.Set(MaterialTokens.Ref.Typeface.Plain, new FontFamily("Comic Sans MS"));
        var host = HostIn(new TypographyTokenSource(), overrides);

        Assert.True(TokenResolver.TryResolve(host, MaterialTokens.Sys.TypeScale.BodyLarge.Font, null, out var value));
        Assert.Equal("Comic Sans MS", Assert.IsType<FontFamily>(value).Name);
    }

    // ---------- miss behavior ----------

    [AvaloniaFact]
    public void Sources_AnswerOnlyTheirOwnSubsystem()
    {
        var unknown = new MaterialTokenKey<double>("md.sys.state.not-a-token");

        Assert.False(new StateTokenSource().TryGetToken(unknown, null, out _));
        Assert.False(new ElevationTokenSource().TryGetToken(unknown, null, out _));
        Assert.False(new ShapeTokenSource().TryGetToken(unknown, null, out _));
        Assert.False(new MotionTokenSource().TryGetToken(unknown, null, out _));
        Assert.False(new TypographyTokenSource().TryGetToken(unknown, null, out _));

        // Cross-subsystem honesty: state does not answer color, color does not answer state.
        Assert.False(new StateTokenSource().TryGetToken(MaterialTokens.Sys.Color.Primary, null, out _));
        Assert.False(new ColorScheme().TryGetToken(MaterialTokens.Sys.State.HoverStateLayerOpacity, null, out _));
    }

    [AvaloniaFact]
    public void Baseline_ComposesAcrossSubsystems()
    {
        // The §6.3 menu end to end: one MaterialResource carrying every baseline source answers a
        // key from each subsystem through the resolver.
        var host = HostIn(
            new ColorScheme(),
            new StateTokenSource(),
            new ElevationTokenSource(),
            new ShapeTokenSource(),
            new MotionTokenSource(),
            new TypographyTokenSource());

        Assert.True(TokenResolver.TryResolve(host, MaterialTokens.Sys.Color.Primary, ThemeVariant.Light, out var color));
        Assert.IsType<Color>(color);
        Assert.Equal(0.08d, Assert.IsType<double>(TokenScan.Resolve(host, MaterialTokens.Sys.State.HoverStateLayerOpacity)));
        Assert.Equal(6d, Assert.IsType<double>(TokenScan.Resolve(host, MaterialTokens.Sys.Elevation.Level3)));
        Assert.Equal(new CornerRadius(12), Assert.IsType<CornerRadius>(TokenScan.Resolve(host, MaterialTokens.Sys.Shape.Medium)));
        Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.IsType<TimeSpan>(TokenScan.Resolve(host, MaterialTokens.Sys.Motion.Duration.Medium2)));
        Assert.Equal(FontWeight.Medium, Assert.IsType<FontWeight>(TokenScan.Resolve(host, MaterialTokens.Sys.TypeScale.LabelLarge.Weight)));
    }

    private static T Get<T>(ITokenSource source, MaterialTokenKey<T> key)
    {
        Assert.True(source.TryGetToken(key, null, out var value), $"miss: {key.DebugName}");
        return Assert.IsType<T>(value);
    }
}
