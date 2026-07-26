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
    public MotionScheme()
    {
        DefaultSpatial = MotionTokens.Expressive.DefaultSpatial;
        FastSpatial = MotionTokens.Expressive.FastSpatial;
        SlowSpatial = MotionTokens.Expressive.SlowSpatial;
        DefaultEffects = MotionTokens.Expressive.DefaultEffects;
        FastEffects = MotionTokens.Expressive.FastEffects;
        SlowEffects = MotionTokens.Expressive.SlowEffects;
    }

    [GeneratedStyledProperty] public partial SpringSpec DefaultSpatial { get; set; }
    [GeneratedStyledProperty] public partial SpringSpec FastSpatial { get; set; }
    [GeneratedStyledProperty] public partial SpringSpec SlowSpatial { get; set; }
    [GeneratedStyledProperty] public partial SpringSpec DefaultEffects { get; set; }
    [GeneratedStyledProperty] public partial SpringSpec FastEffects { get; set; }
    [GeneratedStyledProperty] public partial SpringSpec SlowEffects { get; set; }

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
        return new MotionScheme();
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
