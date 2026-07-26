using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class CornerSizeTests
{
    [Fact]
    public void PercentCorners_ResolveAgainstTheSmallerSide()
    {
        var full = CornerSize.Full;

        Assert.Equal(20, full.Resolve(new Size(120, 40)));
        Assert.Equal(20, full.Resolve(new Size(40, 40)));
    }

    [Fact]
    public void Lerp_IsDeferredUntilResolution()
    {
        // The whole point: a percentage and an absolute corner cannot be averaged up front,
        // because the percentage has no value until the shape has a size.
        var half = CornerSize.Lerp(CornerSize.Full, CornerSize.Absolute(8), 0.5);

        Assert.Equal(14, half.Resolve(new Size(40, 40)));
        Assert.Equal(54, half.Resolve(new Size(200, 200)));
    }

    [Fact]
    public void Lerp_CollapsesAtTheBoundariesRatherThanNesting()
    {
        var start = CornerSize.Absolute(4);
        var stop = CornerSize.Absolute(8);

        Assert.Same(start, CornerSize.Lerp(start, stop, 0));
        Assert.Same(stop, CornerSize.Lerp(start, stop, 1));
    }

    [Fact]
    public void ShapeLerp_InterpolatesEveryCorner()
    {
        var lerped = RoundedCornerShape.Lerp(
            RoundedCornerShape.Uniform(0),
            RoundedCornerShape.Uniform(10),
            0.25);

        Assert.Equal(new CornerRadius(2.5), lerped.ToCornerRadius(new Size(100, 100)));
    }

    [Fact]
    public void CornerRadius_SwapsStartAndEndUnderRightToLeft()
    {
        var shape = new RoundedCornerShape(
            CornerSize.Absolute(1),
            CornerSize.Absolute(2),
            CornerSize.Absolute(3),
            CornerSize.Absolute(4));

        Assert.Equal(new CornerRadius(1, 2, 3, 4), shape.ToCornerRadius(new Size(100, 100)));
        Assert.Equal(new CornerRadius(2, 1, 4, 3), shape.ToCornerRadius(new Size(100, 100), isRightToLeft: true));
    }
}

public class ShapeSchemeTests
{
    [Fact]
    public void DirectionalRoles_DeriveFromTheirBaseRole()
    {
        var scheme = new ShapeScheme { Large = RoundedCornerShape.Uniform(16) };
        var resolved = new ResolvedShapeScheme(scheme);

        Assert.Equal(new CornerRadius(16, 16, 0, 0), resolved.GetCornerRadius(ShapeRole.CornerLargeTop, Size.Infinity));
        Assert.Equal(new CornerRadius(0, 16, 16, 0), resolved.GetCornerRadius(ShapeRole.CornerLargeEnd, Size.Infinity));
        Assert.Equal(new CornerRadius(16, 0, 0, 16), resolved.GetCornerRadius(ShapeRole.CornerLargeStart, Size.Infinity));
    }

    [Fact]
    public void NoneAndFull_AreNotOverridable()
    {
        var scheme = new ShapeScheme { ExtraSmall = RoundedCornerShape.Uniform(99) };
        var resolved = new ResolvedShapeScheme(scheme);

        Assert.Equal(new CornerRadius(0), resolved.GetCornerRadius(ShapeRole.CornerNone, new Size(40, 40)));
        Assert.Equal(new CornerRadius(20), resolved.GetCornerRadius(ShapeRole.CornerFull, new Size(40, 40)));
    }

    [Fact]
    public void DefaultScale_MatchesTheMaterialTokens()
    {
        var resolved = ResolvedShapeScheme.Default;
        var size = new Size(1000, 1000);

        Assert.Equal(new CornerRadius(4), resolved.GetCornerRadius(ShapeRole.CornerExtraSmall, size));
        Assert.Equal(new CornerRadius(8), resolved.GetCornerRadius(ShapeRole.CornerSmall, size));
        Assert.Equal(new CornerRadius(12), resolved.GetCornerRadius(ShapeRole.CornerMedium, size));
        Assert.Equal(new CornerRadius(16), resolved.GetCornerRadius(ShapeRole.CornerLarge, size));
        Assert.Equal(new CornerRadius(20), resolved.GetCornerRadius(ShapeRole.CornerLargeIncreased, size));
        Assert.Equal(new CornerRadius(28), resolved.GetCornerRadius(ShapeRole.CornerExtraLarge, size));
        Assert.Equal(new CornerRadius(32), resolved.GetCornerRadius(ShapeRole.CornerExtraLargeIncreased, size));
        Assert.Equal(new CornerRadius(48), resolved.GetCornerRadius(ShapeRole.CornerExtraExtraLarge, size));
    }
}

public class MotionSchemeTests
{
    [Fact]
    public void EffectsSprings_AreIdenticalAcrossSchemesAndCriticallyDamped()
    {
        var standard = new ResolvedMotionScheme(MotionScheme.CreateStandard());
        var expressive = new ResolvedMotionScheme(MotionScheme.CreateExpressive());

        foreach (var role in new[] { MotionRole.DefaultEffects, MotionRole.FastEffects, MotionRole.SlowEffects })
        {
            Assert.Equal(standard.Get(role), expressive.Get(role));
            Assert.True(standard.Get(role).IsCriticallyDamped);
        }
    }

    [Fact]
    public void SpatialSprings_AreSofterAndSlowerInTheExpressiveScheme()
    {
        var standard = new ResolvedMotionScheme(MotionScheme.CreateStandard());
        var expressive = new ResolvedMotionScheme(MotionScheme.CreateExpressive());

        foreach (var role in new[] { MotionRole.DefaultSpatial, MotionRole.FastSpatial, MotionRole.SlowSpatial })
        {
            Assert.True(expressive.Get(role).DampingRatio <= standard.Get(role).DampingRatio);
            Assert.True(expressive.Get(role).Stiffness < standard.Get(role).Stiffness);
        }
    }

    [Fact]
    public void DefaultScheme_IsExpressive()
    {
        Assert.Equal(
            MotionTokens.Expressive.DefaultSpatial,
            ResolvedMotionScheme.Default.Get(MotionRole.DefaultSpatial));
    }
}

public class ColorTokenValueTests
{
    [Fact]
    public void RoleReferences_ResolveAgainstTheSchemeAndVariant()
    {
        var theme = new MaterialTheme { ColorScheme = new MaterialColorUtilities.Avalonia.TonalSpotScheme(Colors.Red) };
        var scheme = theme.Resolved.ColorScheme!;
        var value = ColorTokenValue.Role(SysColorToken.Primary);

        Assert.Equal(scheme.GetColor(SysColorToken.Primary, isDark: false), value.Resolve(scheme, isDark: false));
        Assert.NotEqual(value.Resolve(scheme, isDark: true), value.Resolve(scheme, isDark: false));
    }

    [Fact]
    public void Literals_IgnoreTheSchemeEntirely()
    {
        var theme = new MaterialTheme { ColorScheme = new MaterialColorUtilities.Avalonia.TonalSpotScheme(Colors.Red) };
        var value = ColorTokenValue.Literal(Colors.Magenta);

        Assert.Equal(Colors.Magenta, value.Resolve(theme.Resolved.ColorScheme, isDark: false));
        Assert.Equal(Colors.Magenta, value.Resolve(theme.Resolved.ColorScheme, isDark: true));
    }

    [Fact]
    public void Opacity_ScalesTheAlphaChannel()
    {
        var value = ColorTokenValue.Literal(Colors.Black);

        Assert.Equal(97, value.Resolve(null, isDark: false, opacity: 0.38).A);
    }

    [Theory]
    [InlineData("Primary")]
    [InlineData("  OnSurfaceVariant ")]
    [InlineData("#FF5722")]
    [InlineData("Custom:Brand")]
    public void Converter_ParsesRolesKeyedRolesAndLiterals(string text)
    {
        var converter = new ColorTokenValueConverter();

        Assert.IsType<ColorTokenValue>(converter.ConvertFrom(text));
    }

    [Fact]
    public void Converter_ParsesTheThreeFormsCorrectly()
    {
        var converter = new ColorTokenValueConverter();

        var role = Assert.IsType<ColorTokenValue>(converter.ConvertFrom("Primary"));
        Assert.Equal(SysColorToken.Primary, role.RoleToken);
        Assert.False(role.IsLiteral);

        var keyed = Assert.IsType<ColorTokenValue>(converter.ConvertFrom("Custom:Brand"));
        Assert.Equal(SysColorToken.Custom, keyed.RoleToken);
        Assert.Equal("Brand", keyed.CustomKey);

        var literal = Assert.IsType<ColorTokenValue>(converter.ConvertFrom("#FF5722"));
        Assert.True(literal.IsLiteral);
        Assert.Equal(Color.Parse("#FF5722"), literal.LiteralColor);
    }
}

public class ThemeSnapshotTests
{
    [Fact]
    public void MutatingATheme_ProducesANewSnapshot()
    {
        var theme = new MaterialTheme { ColorScheme = new MaterialColorUtilities.Avalonia.TonalSpotScheme(Colors.Red) };
        var before = theme.Resolved;

        ((MaterialColorUtilities.Avalonia.TonalSpotScheme)theme.ColorScheme!).Color = Colors.Blue;

        Assert.NotSame(before, theme.Resolved);
    }

    [Fact]
    public void AnUnchangedTheme_KeepsItsSnapshot()
    {
        var theme = new MaterialTheme();

        Assert.Same(theme.Resolved, theme.Resolved);
    }

    [Fact]
    public void ButtonColorBundles_AreMemoizedPerVariant()
    {
        var theme = new MaterialTheme();
        var snapshot = theme.Resolved;

        Assert.Same(snapshot.GetButtonColors(isDark: false), snapshot.GetButtonColors(isDark: false));
        Assert.NotSame(snapshot.GetButtonColors(isDark: false), snapshot.GetButtonColors(isDark: true));
    }

    [Fact]
    public void DisabledColors_CarryTheirTokenOpacity()
    {
        var theme = new MaterialTheme();
        var colors = theme.Resolved.GetButtonColors(isDark: false);

        Assert.Equal(
            (byte)Math.Round(255 * theme.ButtonTokens.DisabledContainerOpacity),
            ((ISolidColorBrush)colors.DisabledContainer).Color.A);
    }
}
