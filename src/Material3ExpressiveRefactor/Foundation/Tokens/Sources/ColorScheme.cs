using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Tokens;
using MaterialColorUtilities.DynamicColors;
using MaterialColorUtilities.HCT;
using MaterialColorUtilities.Palettes;
using MaterialColorUtilities.Scheme;
using MaterialColorUtilities.Utils;

namespace Material3ExpressiveRefactor.Foundation.Tokens.Sources;

/// <summary>
/// Seed-driven color token source (design doc §6.3): wraps material-color-utilities and serves
/// <c>md.sys.color.*</c> roles and <c>md.ref.palette.*</c> tones as literal <see cref="Color"/>
/// values (§5.6 — colors leave the scheme fully computed; no sys→ref dependency exists inside the
/// color subsystem).
/// <para>
/// The default parameter set IS the M3 baseline (seed #6750A4, TonalSpot, Spec2026, standard
/// contrast), so an unconfigured instance hung in a theme doubles as the baseline layer (§6.4).
/// </para>
/// <para>
/// Caching follows §5.4: the built schemes are the one sanctioned cache, keyed by input version.
/// Changing any input rebuilds lazily and raises <see cref="Changed"/> exactly once.
/// </para>
/// </summary>
public class ColorScheme : AvaloniaObject, ITokenSource
{
    private Hct? _seedHct;
    private DynamicScheme? _light;
    private DynamicScheme? _dark;

    /// <summary>The seed color the whole scheme is generated from. Bindable, so an image-extraction
    /// pipeline can drive it (§9.4).</summary>
    public static readonly StyledProperty<Color> SeedProperty =
        AvaloniaProperty.Register<ColorScheme, Color>(nameof(Seed), Color.FromRgb(0x67, 0x50, 0xA4));

    /// <summary>Which MCU scheme family to build. Defaults to the M3 baseline, TonalSpot.</summary>
    public static readonly StyledProperty<Variant> VariantProperty =
        AvaloniaProperty.Register<ColorScheme, Variant>(nameof(Variant), Variant.TonalSpot);

    /// <summary>
    /// MCU spec version. Defaults to Spec2026; note that MCU automatically falls back (e.g.
    /// TonalSpot resolves to Spec2025 behavior), see DynamicScheme.MaybeFallbackSpecVersion.
    /// </summary>
    public static readonly StyledProperty<ColorSpec.SpecVersion> SpecVersionProperty =
        AvaloniaProperty.Register<ColorScheme, ColorSpec.SpecVersion>(nameof(SpecVersion), ColorSpec.SpecVersion.Spec2026);

    /// <summary>
    /// Contrast level, -1.0 (low) through 1.0 (high); 0 is standard. Applied uniformly to every
    /// theme variant — per-variant contrast is deliberately shelved for now.
    /// </summary>
    public static readonly StyledProperty<double> ContrastLevelProperty =
        AvaloniaProperty.Register<ColorScheme, double>(nameof(ContrastLevel), 0.0);

    public Color Seed
    {
        get => GetValue(SeedProperty);
        set => SetValue(SeedProperty, value);
    }

    public Variant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public ColorSpec.SpecVersion SpecVersion
    {
        get => GetValue(SpecVersionProperty);
        set => SetValue(SpecVersionProperty, value);
    }

    public double ContrastLevel
    {
        get => GetValue(ContrastLevelProperty);
        set => SetValue(ContrastLevelProperty, value);
    }

    public event EventHandler? Changed;

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        var scheme = IsDark(theme) ? Dark : Light;

        if (SysColorRoles.TryGetValue(key, out var role))
        {
            value = ToColor(role(scheme));
            return true;
        }

        if (RefPaletteTones.TryGetValue(key, out var tone))
        {
            value = ToColor(tone.Palette(scheme).Get(tone.Tone));
            return true;
        }

        value = null!;
        return false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SeedProperty ||
            change.Property == VariantProperty ||
            change.Property == SpecVersionProperty ||
            change.Property == ContrastLevelProperty)
        {
            _seedHct = null;
            _light = null;
            _dark = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private Hct SeedHct => _seedHct ??= Hct.From(new ArgbColor(Seed.A, Seed.R, Seed.G, Seed.B));

    private DynamicScheme Light => _light ??= CreateScheme(isDark: false);

    private DynamicScheme Dark => _dark ??= CreateScheme(isDark: true);

    private DynamicScheme CreateScheme(bool isDark, DynamicScheme.Platform platform = DynamicScheme.Platform.Phone) => Variant switch
    {
        Variant.Monochrome => new SchemeMonochrome(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.Neutral => new SchemeNeutral(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.TonalSpot => new SchemeTonalSpot(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.Vibrant => new SchemeVibrant(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.Expressive => new SchemeExpressive(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.Content => new SchemeContent(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.Fidelity => new SchemeFidelity(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.Rainbow => new SchemeRainbow(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.FruitSalad => new SchemeFruitSalad(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        Variant.Cmf => new SchemeCmf(SeedHct, isDark, ContrastLevel, SpecVersion, platform),
        _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
    };

    /// <summary>
    /// Whether a theme variant resolves to dark, following <see cref="ThemeVariant.InheritVariant"/>
    /// for custom variants. Null (an element before its first attach) and anything that resolves
    /// to neither light nor dark is treated as light.
    /// </summary>
    internal static bool IsDark(ThemeVariant? variant)
    {
        for (var current = variant; current is { }; current = current.InheritVariant)
        {
            if (current == ThemeVariant.Dark)
                return true;
            if (current == ThemeVariant.Light)
                return false;
        }

        return false;
    }

    private static Color ToColor(ArgbColor argb) => Color.FromArgb(argb.Alpha, argb.Red, argb.Green, argb.Blue);

    // Reference-identity lookups: MaterialTokenKey keeps default reference equality (§3.1), so the
    // dictionaries below are the whole mapping — one entry per facade key.
    private static readonly Dictionary<MaterialTokenKey, Func<DynamicScheme, ArgbColor>> SysColorRoles =
        new()
        {
            [MaterialTokens.Sys.Color.Background] = static s => s.Background,
            [MaterialTokens.Sys.Color.OnBackground] = static s => s.OnBackground,
            [MaterialTokens.Sys.Color.Surface] = static s => s.Surface,
            [MaterialTokens.Sys.Color.SurfaceDim] = static s => s.SurfaceDim,
            [MaterialTokens.Sys.Color.SurfaceBright] = static s => s.SurfaceBright,
            [MaterialTokens.Sys.Color.SurfaceContainerLowest] = static s => s.SurfaceContainerLowest,
            [MaterialTokens.Sys.Color.SurfaceContainerLow] = static s => s.SurfaceContainerLow,
            [MaterialTokens.Sys.Color.SurfaceContainer] = static s => s.SurfaceContainer,
            [MaterialTokens.Sys.Color.SurfaceContainerHigh] = static s => s.SurfaceContainerHigh,
            [MaterialTokens.Sys.Color.SurfaceContainerHighest] = static s => s.SurfaceContainerHighest,
            [MaterialTokens.Sys.Color.OnSurface] = static s => s.OnSurface,
            [MaterialTokens.Sys.Color.SurfaceVariant] = static s => s.SurfaceVariant,
            [MaterialTokens.Sys.Color.OnSurfaceVariant] = static s => s.OnSurfaceVariant,
            [MaterialTokens.Sys.Color.InverseSurface] = static s => s.InverseSurface,
            [MaterialTokens.Sys.Color.InverseOnSurface] = static s => s.InverseOnSurface,
            [MaterialTokens.Sys.Color.Outline] = static s => s.Outline,
            [MaterialTokens.Sys.Color.OutlineVariant] = static s => s.OutlineVariant,
            [MaterialTokens.Sys.Color.Shadow] = static s => s.Shadow,
            [MaterialTokens.Sys.Color.Scrim] = static s => s.Scrim,
            [MaterialTokens.Sys.Color.SurfaceTint] = static s => s.SurfaceTint,
            [MaterialTokens.Sys.Color.Primary] = static s => s.Primary,
            [MaterialTokens.Sys.Color.PrimaryDim] = static s => s.PrimaryDim,
            [MaterialTokens.Sys.Color.OnPrimary] = static s => s.OnPrimary,
            [MaterialTokens.Sys.Color.PrimaryContainer] = static s => s.PrimaryContainer,
            [MaterialTokens.Sys.Color.OnPrimaryContainer] = static s => s.OnPrimaryContainer,
            [MaterialTokens.Sys.Color.InversePrimary] = static s => s.InversePrimary,
            [MaterialTokens.Sys.Color.Secondary] = static s => s.Secondary,
            [MaterialTokens.Sys.Color.SecondaryDim] = static s => s.SecondaryDim,
            [MaterialTokens.Sys.Color.OnSecondary] = static s => s.OnSecondary,
            [MaterialTokens.Sys.Color.SecondaryContainer] = static s => s.SecondaryContainer,
            [MaterialTokens.Sys.Color.OnSecondaryContainer] = static s => s.OnSecondaryContainer,
            [MaterialTokens.Sys.Color.Tertiary] = static s => s.Tertiary,
            [MaterialTokens.Sys.Color.TertiaryDim] = static s => s.TertiaryDim,
            [MaterialTokens.Sys.Color.OnTertiary] = static s => s.OnTertiary,
            [MaterialTokens.Sys.Color.TertiaryContainer] = static s => s.TertiaryContainer,
            [MaterialTokens.Sys.Color.OnTertiaryContainer] = static s => s.OnTertiaryContainer,
            [MaterialTokens.Sys.Color.Error] = static s => s.Error,
            [MaterialTokens.Sys.Color.ErrorDim] = static s => s.ErrorDim,
            [MaterialTokens.Sys.Color.OnError] = static s => s.OnError,
            [MaterialTokens.Sys.Color.ErrorContainer] = static s => s.ErrorContainer,
            [MaterialTokens.Sys.Color.OnErrorContainer] = static s => s.OnErrorContainer,
            [MaterialTokens.Sys.Color.PrimaryFixed] = static s => s.PrimaryFixed,
            [MaterialTokens.Sys.Color.PrimaryFixedDim] = static s => s.PrimaryFixedDim,
            [MaterialTokens.Sys.Color.OnPrimaryFixed] = static s => s.OnPrimaryFixed,
            [MaterialTokens.Sys.Color.OnPrimaryFixedVariant] = static s => s.OnPrimaryFixedVariant,
            [MaterialTokens.Sys.Color.SecondaryFixed] = static s => s.SecondaryFixed,
            [MaterialTokens.Sys.Color.SecondaryFixedDim] = static s => s.SecondaryFixedDim,
            [MaterialTokens.Sys.Color.OnSecondaryFixed] = static s => s.OnSecondaryFixed,
            [MaterialTokens.Sys.Color.OnSecondaryFixedVariant] = static s => s.OnSecondaryFixedVariant,
            [MaterialTokens.Sys.Color.TertiaryFixed] = static s => s.TertiaryFixed,
            [MaterialTokens.Sys.Color.TertiaryFixedDim] = static s => s.TertiaryFixedDim,
            [MaterialTokens.Sys.Color.OnTertiaryFixed] = static s => s.OnTertiaryFixed,
            [MaterialTokens.Sys.Color.OnTertiaryFixedVariant] = static s => s.OnTertiaryFixedVariant,
        };

    private static readonly Dictionary<MaterialTokenKey, (Func<DynamicScheme, TonalPalette> Palette, int Tone)> RefPaletteTones =
        new()
        {
            [MaterialTokens.Ref.Palette.Primary0] = (static s => s.PrimaryPalette, 0),
            [MaterialTokens.Ref.Palette.Primary10] = (static s => s.PrimaryPalette, 10),
            [MaterialTokens.Ref.Palette.Primary20] = (static s => s.PrimaryPalette, 20),
            [MaterialTokens.Ref.Palette.Primary30] = (static s => s.PrimaryPalette, 30),
            [MaterialTokens.Ref.Palette.Primary40] = (static s => s.PrimaryPalette, 40),
            [MaterialTokens.Ref.Palette.Primary50] = (static s => s.PrimaryPalette, 50),
            [MaterialTokens.Ref.Palette.Primary60] = (static s => s.PrimaryPalette, 60),
            [MaterialTokens.Ref.Palette.Primary70] = (static s => s.PrimaryPalette, 70),
            [MaterialTokens.Ref.Palette.Primary80] = (static s => s.PrimaryPalette, 80),
            [MaterialTokens.Ref.Palette.Primary90] = (static s => s.PrimaryPalette, 90),
            [MaterialTokens.Ref.Palette.Primary95] = (static s => s.PrimaryPalette, 95),
            [MaterialTokens.Ref.Palette.Primary99] = (static s => s.PrimaryPalette, 99),
            [MaterialTokens.Ref.Palette.Primary100] = (static s => s.PrimaryPalette, 100),
            [MaterialTokens.Ref.Palette.Secondary0] = (static s => s.SecondaryPalette, 0),
            [MaterialTokens.Ref.Palette.Secondary10] = (static s => s.SecondaryPalette, 10),
            [MaterialTokens.Ref.Palette.Secondary20] = (static s => s.SecondaryPalette, 20),
            [MaterialTokens.Ref.Palette.Secondary30] = (static s => s.SecondaryPalette, 30),
            [MaterialTokens.Ref.Palette.Secondary40] = (static s => s.SecondaryPalette, 40),
            [MaterialTokens.Ref.Palette.Secondary50] = (static s => s.SecondaryPalette, 50),
            [MaterialTokens.Ref.Palette.Secondary60] = (static s => s.SecondaryPalette, 60),
            [MaterialTokens.Ref.Palette.Secondary70] = (static s => s.SecondaryPalette, 70),
            [MaterialTokens.Ref.Palette.Secondary80] = (static s => s.SecondaryPalette, 80),
            [MaterialTokens.Ref.Palette.Secondary90] = (static s => s.SecondaryPalette, 90),
            [MaterialTokens.Ref.Palette.Secondary95] = (static s => s.SecondaryPalette, 95),
            [MaterialTokens.Ref.Palette.Secondary99] = (static s => s.SecondaryPalette, 99),
            [MaterialTokens.Ref.Palette.Secondary100] = (static s => s.SecondaryPalette, 100),
            [MaterialTokens.Ref.Palette.Tertiary0] = (static s => s.TertiaryPalette, 0),
            [MaterialTokens.Ref.Palette.Tertiary10] = (static s => s.TertiaryPalette, 10),
            [MaterialTokens.Ref.Palette.Tertiary20] = (static s => s.TertiaryPalette, 20),
            [MaterialTokens.Ref.Palette.Tertiary30] = (static s => s.TertiaryPalette, 30),
            [MaterialTokens.Ref.Palette.Tertiary40] = (static s => s.TertiaryPalette, 40),
            [MaterialTokens.Ref.Palette.Tertiary50] = (static s => s.TertiaryPalette, 50),
            [MaterialTokens.Ref.Palette.Tertiary60] = (static s => s.TertiaryPalette, 60),
            [MaterialTokens.Ref.Palette.Tertiary70] = (static s => s.TertiaryPalette, 70),
            [MaterialTokens.Ref.Palette.Tertiary80] = (static s => s.TertiaryPalette, 80),
            [MaterialTokens.Ref.Palette.Tertiary90] = (static s => s.TertiaryPalette, 90),
            [MaterialTokens.Ref.Palette.Tertiary95] = (static s => s.TertiaryPalette, 95),
            [MaterialTokens.Ref.Palette.Tertiary99] = (static s => s.TertiaryPalette, 99),
            [MaterialTokens.Ref.Palette.Tertiary100] = (static s => s.TertiaryPalette, 100),
            [MaterialTokens.Ref.Palette.Neutral0] = (static s => s.NeutralPalette, 0),
            [MaterialTokens.Ref.Palette.Neutral10] = (static s => s.NeutralPalette, 10),
            [MaterialTokens.Ref.Palette.Neutral20] = (static s => s.NeutralPalette, 20),
            [MaterialTokens.Ref.Palette.Neutral30] = (static s => s.NeutralPalette, 30),
            [MaterialTokens.Ref.Palette.Neutral40] = (static s => s.NeutralPalette, 40),
            [MaterialTokens.Ref.Palette.Neutral50] = (static s => s.NeutralPalette, 50),
            [MaterialTokens.Ref.Palette.Neutral60] = (static s => s.NeutralPalette, 60),
            [MaterialTokens.Ref.Palette.Neutral70] = (static s => s.NeutralPalette, 70),
            [MaterialTokens.Ref.Palette.Neutral80] = (static s => s.NeutralPalette, 80),
            [MaterialTokens.Ref.Palette.Neutral90] = (static s => s.NeutralPalette, 90),
            [MaterialTokens.Ref.Palette.Neutral95] = (static s => s.NeutralPalette, 95),
            [MaterialTokens.Ref.Palette.Neutral99] = (static s => s.NeutralPalette, 99),
            [MaterialTokens.Ref.Palette.Neutral100] = (static s => s.NeutralPalette, 100),
            [MaterialTokens.Ref.Palette.NeutralVariant0] = (static s => s.NeutralVariantPalette, 0),
            [MaterialTokens.Ref.Palette.NeutralVariant10] = (static s => s.NeutralVariantPalette, 10),
            [MaterialTokens.Ref.Palette.NeutralVariant20] = (static s => s.NeutralVariantPalette, 20),
            [MaterialTokens.Ref.Palette.NeutralVariant30] = (static s => s.NeutralVariantPalette, 30),
            [MaterialTokens.Ref.Palette.NeutralVariant40] = (static s => s.NeutralVariantPalette, 40),
            [MaterialTokens.Ref.Palette.NeutralVariant50] = (static s => s.NeutralVariantPalette, 50),
            [MaterialTokens.Ref.Palette.NeutralVariant60] = (static s => s.NeutralVariantPalette, 60),
            [MaterialTokens.Ref.Palette.NeutralVariant70] = (static s => s.NeutralVariantPalette, 70),
            [MaterialTokens.Ref.Palette.NeutralVariant80] = (static s => s.NeutralVariantPalette, 80),
            [MaterialTokens.Ref.Palette.NeutralVariant90] = (static s => s.NeutralVariantPalette, 90),
            [MaterialTokens.Ref.Palette.NeutralVariant95] = (static s => s.NeutralVariantPalette, 95),
            [MaterialTokens.Ref.Palette.NeutralVariant99] = (static s => s.NeutralVariantPalette, 99),
            [MaterialTokens.Ref.Palette.NeutralVariant100] = (static s => s.NeutralVariantPalette, 100),
            [MaterialTokens.Ref.Palette.Error0] = (static s => s.ErrorPalette, 0),
            [MaterialTokens.Ref.Palette.Error10] = (static s => s.ErrorPalette, 10),
            [MaterialTokens.Ref.Palette.Error20] = (static s => s.ErrorPalette, 20),
            [MaterialTokens.Ref.Palette.Error30] = (static s => s.ErrorPalette, 30),
            [MaterialTokens.Ref.Palette.Error40] = (static s => s.ErrorPalette, 40),
            [MaterialTokens.Ref.Palette.Error50] = (static s => s.ErrorPalette, 50),
            [MaterialTokens.Ref.Palette.Error60] = (static s => s.ErrorPalette, 60),
            [MaterialTokens.Ref.Palette.Error70] = (static s => s.ErrorPalette, 70),
            [MaterialTokens.Ref.Palette.Error80] = (static s => s.ErrorPalette, 80),
            [MaterialTokens.Ref.Palette.Error90] = (static s => s.ErrorPalette, 90),
            [MaterialTokens.Ref.Palette.Error95] = (static s => s.ErrorPalette, 95),
            [MaterialTokens.Ref.Palette.Error99] = (static s => s.ErrorPalette, 99),
            [MaterialTokens.Ref.Palette.Error100] = (static s => s.ErrorPalette, 100),
        };
}
