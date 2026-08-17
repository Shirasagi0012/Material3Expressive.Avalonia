using System;
using Avalonia;
using Material3Expressive.Foundation.Tokens;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// The six springs of a theme. Defaults to the expressive scheme, which this library treats as
/// standard behaviour rather than an opt-in variant.
/// </summary>
public partial class MotionScheme : AvaloniaObject
{
    // Keep defaults in metadata: the compatibility Raw bridge treats only IsSet slots as local
    // authoring, so an untouched nested scheme remains sparse and can inherit ancestor Raw facts.
    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultSpatial))]
    public partial SpringSpec DefaultSpatial { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetFastSpatial))]
    public partial SpringSpec FastSpatial { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetSlowSpatial))]
    public partial SpringSpec SlowSpatial { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultEffects))]
    public partial SpringSpec DefaultEffects { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetFastEffects))]
    public partial SpringSpec FastEffects { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetSlowEffects))]
    public partial SpringSpec SlowEffects { get; set; }

    private static SpringSpec GetDefaultSpatial() => MotionTokens.Expressive.DefaultSpatial;
    private static SpringSpec GetFastSpatial() => MotionTokens.Expressive.FastSpatial;
    private static SpringSpec GetSlowSpatial() => MotionTokens.Expressive.SlowSpatial;
    private static SpringSpec GetDefaultEffects() => MotionTokens.Expressive.DefaultEffects;
    private static SpringSpec GetFastEffects() => MotionTokens.Expressive.FastEffects;
    private static SpringSpec GetSlowEffects() => MotionTokens.Expressive.SlowEffects;

    /// <summary>The restrained scheme: same effects springs, stiffer and less bouncy spatially.</summary>
    public static MotionScheme CreateStandard()
    {
        return new MotionScheme
        {
            DefaultSpatial = MotionTokens.Standard.DefaultSpatial,
            FastSpatial = MotionTokens.Standard.FastSpatial,
            SlowSpatial = MotionTokens.Standard.SlowSpatial,
            DefaultEffects = MotionTokens.Standard.DefaultEffects,
            FastEffects = MotionTokens.Standard.FastEffects,
            SlowEffects = MotionTokens.Standard.SlowEffects
        };
    }

    /// <summary>The default scheme.</summary>
    public static MotionScheme CreateExpressive()
    {
        // Unlike a plain constructor, the named factory represents an explicit dense choice.
        return new MotionScheme
        {
            DefaultSpatial = MotionTokens.Expressive.DefaultSpatial,
            FastSpatial = MotionTokens.Expressive.FastSpatial,
            SlowSpatial = MotionTokens.Expressive.SlowSpatial,
            DefaultEffects = MotionTokens.Expressive.DefaultEffects,
            FastEffects = MotionTokens.Expressive.FastEffects,
            SlowEffects = MotionTokens.Expressive.SlowEffects
        };
    }

    public object ProvideValue()
    {
        return this;
    }
}

/// <summary>Immutable spring lookup for consumers.</summary>
public sealed class ResolvedMotionScheme
{
    private readonly SpringSpec[] _springs;

    public ResolvedMotionScheme(MotionScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        _springs = new SpringSpec[Enum.GetValues<MotionRole>().Length];
        _springs[(int)MotionRole.DefaultSpatial] = scheme.DefaultSpatial;
        _springs[(int)MotionRole.FastSpatial] = scheme.FastSpatial;
        _springs[(int)MotionRole.SlowSpatial] = scheme.SlowSpatial;
        _springs[(int)MotionRole.DefaultEffects] = scheme.DefaultEffects;
        _springs[(int)MotionRole.FastEffects] = scheme.FastEffects;
        _springs[(int)MotionRole.SlowEffects] = scheme.SlowEffects;
    }

    public static ResolvedMotionScheme Default { get; } = new(new MotionScheme());

    public SpringSpec Get(MotionRole role)
    {
        return _springs[(int)role];
    }
}
