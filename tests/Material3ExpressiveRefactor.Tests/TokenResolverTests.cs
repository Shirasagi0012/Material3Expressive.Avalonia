using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Baseline;
using Material3ExpressiveRefactor.Foundation;
using Material3ExpressiveRefactor.Foundation.Motion;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Tokens;
using MaterialColorUtilities.DynamicColors;
using MaterialColorUtilities.HCT;
using MaterialColorUtilities.Scheme;
using MaterialColorUtilities.Utils;
using Xunit;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// Consumption-side multi-hop resolution (design doc §5): TokenRef/WithAlpha/SpringSpecRef
/// chains, per-hop restart from the requester's position (R1'/R2'), cycle/budget termination
/// and misses.
/// </summary>
public class TokenResolverTests
{
    private static readonly MaterialTokenKey<Color> CompColor = new("md.comp.test-widget.container-color");

    private static Button HostIn(params ITokenSource[] sources)
    {
        var theme = new MaterialResource();
        foreach (var source in sources)
            theme.Children.Add(source);
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(theme);
        return new Button { Resources = resources };
    }

    private static Color SchemeColor(Func<DynamicScheme, ArgbColor> pick) =>
        ToColor(pick(ReferenceScheme()));

    private static DynamicScheme ReferenceScheme() =>
        new SchemeTonalSpot(
            Hct.From(new ArgbColor(0xFF6750A4u)),
            false, 0.0, ColorSpec.SpecVersion.Spec2026, DynamicScheme.DefaultPlatform);

    private static Color ToColor(ArgbColor argb) => Color.FromArgb(argb.Alpha, argb.Red, argb.Green, argb.Blue);

    [AvaloniaFact]
    public void Literal_Resolves()
    {
        var source = new TestTokenSource();
        source.Set(CompColor, Colors.Red);
        var host = HostIn(source);

        Assert.True(TokenResolver.TryResolve(host, CompColor, null, out var value));
        Assert.Equal(Colors.Red, Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void TokenRef_FollowsIntoScheme()
    {
        var source = new TestTokenSource();
        source.Set(CompColor, new TokenRef(MaterialTokens.Sys.Color.Primary));
        var host = HostIn(new ColorScheme(), source);

        Assert.True(TokenResolver.TryResolve(host, CompColor, null, out var value));
        Assert.Equal(SchemeColor(static s => s.Primary), Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void IntermediateOverride_OnInnerScope_WinsFromRequesterPosition()
    {
        // R2': the root entry (CompColor → TokenRef(Sys.Color.Primary)) lives on the outer scope
        // and never changes; overriding the intermediate key closer to the consumer must win.
        var outer = new TestTokenSource();
        outer.Set(CompColor, new TokenRef(MaterialTokens.Sys.Color.Primary));
        var outerTheme = new MaterialResource { Children = { new ColorScheme(), outer } };
        var outerResources = new ResourceDictionary();
        outerResources.MergedDictionaries.Add(outerTheme);

        var inner = new TestTokenSource();
        inner.Set(MaterialTokens.Sys.Color.Primary, Colors.LimeGreen);
        var innerTheme = new MaterialResource { Children = { inner } };
        var innerResources = new ResourceDictionary();
        innerResources.MergedDictionaries.Add(innerTheme);

        var panel = new Panel { Resources = outerResources };
        var overridden = new Button { Resources = innerResources };
        var plain = new Button();
        panel.Children.Add(overridden);
        panel.Children.Add(plain);

        Assert.True(TokenResolver.TryResolve(overridden, CompColor, null, out var innerValue));
        Assert.Equal(Colors.LimeGreen, Assert.IsType<Color>(innerValue));

        Assert.True(TokenResolver.TryResolve(plain, CompColor, null, out var outerValue));
        Assert.Equal(SchemeColor(static s => s.Primary), Assert.IsType<Color>(outerValue));
    }

    [AvaloniaFact]
    public void WithAlpha_LiteralOpacity_AppliesOverResolvedColor()
    {
        var source = new TestTokenSource();
        source.Set(CompColor, new WithAlpha(MaterialTokens.Sys.Color.OnSurface, 0.12));
        var host = HostIn(new ColorScheme(), source);

        Assert.True(TokenResolver.TryResolve(host, CompColor, null, out var value));
        var onSurface = SchemeColor(static s => s.OnSurface);
        var expected = Color.FromArgb(31, onSurface.R, onSurface.G, onSurface.B); // round(0.12 * 255)
        Assert.Equal(expected, Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void WithAlpha_OpacityKey_ResolvesThroughToken()
    {
        var opacityKey = new MaterialTokenKey<double>("md.sys.state.test-opacity");
        var source = new TestTokenSource();
        source.Set(opacityKey, 0.5);
        source.Set(CompColor, new WithAlpha(MaterialTokens.Sys.Color.Primary, opacityKey));
        var host = HostIn(new ColorScheme(), source);

        Assert.True(TokenResolver.TryResolve(host, CompColor, null, out var value));
        var primary = SchemeColor(static s => s.Primary);
        Assert.Equal(
            Color.FromArgb(128, primary.R, primary.G, primary.B),
            Assert.IsType<Color>(value));
    }

    [AvaloniaFact]
    public void Cycle_TerminatesAsFailure()
    {
        var a = new MaterialTokenKey<Color>("test.a");
        var b = new MaterialTokenKey<Color>("test.b");
        var source = new TestTokenSource();
        source.Set(a, new TokenRef(b));
        source.Set(b, new TokenRef(a));
        var host = HostIn(source);

        Assert.False(TokenResolver.TryResolve(host, a, null, out _));
    }

    [AvaloniaFact]
    public void OverBudgetChain_TerminatesAsFailure()
    {
        var keys = Enumerable.Range(0, 7)
            .Select(i => new MaterialTokenKey<Color>($"test.chain{i}"))
            .ToArray();
        var source = new TestTokenSource();
        for (var i = 0; i < keys.Length - 1; i++)
            source.Set(keys[i], new TokenRef(keys[i + 1]));
        source.Set(keys[^1], Colors.Red);
        var host = HostIn(source);

        Assert.False(TokenResolver.TryResolve(host, keys[0], null, out _));
    }

    [AvaloniaFact]
    public void MissingKey_ReturnsFalse()
    {
        var host = HostIn(new ColorScheme());

        Assert.False(TokenResolver.TryResolve(host, CompColor, null, out _));
    }

    [AvaloniaFact]
    public void SpringSpec_Literal_ResolvesAsIs()
    {
        // A fully literal spec needs no derivation — the entry holds the SpringSpec itself.
        var key = new MaterialTokenKey<SpringSpec>("md.sys.motion.spring.test-literal");
        var spec = new SpringSpec(0.7, 500);
        var source = new TestTokenSource();
        source.Set(key, spec);
        var host = HostIn(source);

        Assert.True(TokenResolver.TryResolve(host, key, null, out var value));
        Assert.Equal(spec, Assert.IsType<SpringSpec>(value));
    }

    [AvaloniaFact]
    public void SpringSpecRef_ComposesFromScalarTokens()
    {
        var host = HostIn(new MotionTokenSource());

        Assert.True(TokenResolver.TryResolve(host, MaterialTokens.Sys.Motion.Spring.EffectsDefault, null, out var value));
        Assert.Equal(new SpringSpec(1.0, 1600), Assert.IsType<SpringSpec>(value));
    }

    [AvaloniaFact]
    public void SpringSpecRef_ExplicitThreshold_OverridesTheDefault()
    {
        var key = new MaterialTokenKey<SpringSpec>("md.sys.motion.spring.test-threshold");
        var source = new TestTokenSource();
        source.Set(key, new SpringSpecRef(
            MaterialTokens.Sys.Motion.Spring.EffectsDefaultDamping,
            MaterialTokens.Sys.Motion.Spring.EffectsDefaultStiffness,
            0.5));
        var host = HostIn(new MotionTokenSource(), source);

        Assert.True(TokenResolver.TryResolve(host, key, null, out var value));
        Assert.Equal(new SpringSpec(1.0, 1600, 0.5), Assert.IsType<SpringSpec>(value));
    }

    [AvaloniaFact]
    public void SpringSpecRef_ComponentOverride_FlowsIntoTheComposedSpec()
    {
        // R2' on a composed spec: the SpringSpecRef entry lives on the outer scope and never
        // changes; overriding the damping scalar closer to the consumer must flow into the spec.
        var outerTheme = new MaterialResource { Children = { new MotionTokenSource() } };
        var outerResources = new ResourceDictionary();
        outerResources.MergedDictionaries.Add(outerTheme);

        var inner = new TestTokenSource();
        inner.Set(MaterialTokens.Sys.Motion.Spring.EffectsDefaultDamping, 0.5d);
        var innerTheme = new MaterialResource { Children = { inner } };
        var innerResources = new ResourceDictionary();
        innerResources.MergedDictionaries.Add(innerTheme);

        var panel = new Panel { Resources = outerResources };
        var overridden = new Button { Resources = innerResources };
        var plain = new Button();
        panel.Children.Add(overridden);
        panel.Children.Add(plain);

        Assert.True(TokenResolver.TryResolve(overridden, MaterialTokens.Sys.Motion.Spring.EffectsDefault, null, out var innerValue));
        Assert.Equal(new SpringSpec(0.5, 1600), Assert.IsType<SpringSpec>(innerValue));

        Assert.True(TokenResolver.TryResolve(plain, MaterialTokens.Sys.Motion.Spring.EffectsDefault, null, out var outerValue));
        Assert.Equal(new SpringSpec(1.0, 1600), Assert.IsType<SpringSpec>(outerValue));
    }

    [AvaloniaFact]
    public void SpringSpecRef_MissingComponent_Fails()
    {
        var key = new MaterialTokenKey<SpringSpec>("md.sys.motion.spring.test-missing");
        var absent = new MaterialTokenKey<double>("md.sys.motion.spring.not-a-token");
        var source = new TestTokenSource();
        source.Set(key, new SpringSpecRef(absent, MaterialTokens.Sys.Motion.Spring.EffectsDefaultStiffness));
        var host = HostIn(new MotionTokenSource(), source);

        Assert.False(TokenResolver.TryResolve(host, key, null, out _));
    }

    [AvaloniaFact]
    public void SpringSpecRef_CycleThroughAComponent_TerminatesAsFailure()
    {
        var d1 = new MaterialTokenKey<double>("test.spec-d1");
        var d2 = new MaterialTokenKey<double>("test.spec-d2");
        var s = new MaterialTokenKey<double>("test.spec-s");
        var spec = new MaterialTokenKey<SpringSpec>("test.spec");
        var source = new TestTokenSource();
        source.Set(spec, new SpringSpecRef(d1, s));
        source.Set(d1, new TokenRef(d2));
        source.Set(d2, new TokenRef(d1));
        source.Set(s, 400d);
        var host = HostIn(source);

        Assert.False(TokenResolver.TryResolve(host, spec, null, out _));
    }
}
