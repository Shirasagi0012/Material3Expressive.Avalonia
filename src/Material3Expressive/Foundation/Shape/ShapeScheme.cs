using System;
using Avalonia;
using Material3Expressive.Foundation.Tokens;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// The eight configurable corner sizes of a theme. Authoring surface: mutable and bindable, like
/// every other scheme, and snapshotted into a <see cref="ResolvedShapeScheme" /> for consumers.
/// </summary>
public partial class ShapeScheme : AvaloniaObject
{
    public ShapeScheme()
    {
        ExtraSmall = ShapeTokens.CornerExtraSmall;
        Small = ShapeTokens.CornerSmall;
        Medium = ShapeTokens.CornerMedium;
        Large = ShapeTokens.CornerLarge;
        LargeIncreased = ShapeTokens.CornerLargeIncreased;
        ExtraLarge = ShapeTokens.CornerExtraLarge;
        ExtraLargeIncreased = ShapeTokens.CornerExtraLargeIncreased;
        ExtraExtraLarge = ShapeTokens.CornerExtraExtraLarge;
    }

    [GeneratedStyledProperty] public partial RoundedCornerShape ExtraSmall { get; set; }
    [GeneratedStyledProperty] public partial RoundedCornerShape Small { get; set; }
    [GeneratedStyledProperty] public partial RoundedCornerShape Medium { get; set; }
    [GeneratedStyledProperty] public partial RoundedCornerShape Large { get; set; }
    [GeneratedStyledProperty] public partial RoundedCornerShape LargeIncreased { get; set; }
    [GeneratedStyledProperty] public partial RoundedCornerShape ExtraLarge { get; set; }
    [GeneratedStyledProperty] public partial RoundedCornerShape ExtraLargeIncreased { get; set; }
    [GeneratedStyledProperty] public partial RoundedCornerShape ExtraExtraLarge { get; set; }

    public object ProvideValue()
    {
        return this;
    }
}

/// <summary>
/// Immutable shape lookup: all fifteen roles resolved once, including the derived directional
/// ones. This is the shape half of the per-theme memo bundle upstream keeps as fifteen fields on
/// <c>Shapes</c>.
/// </summary>
public sealed class ResolvedShapeScheme
{
    private static readonly int RoleCount = Enum.GetValues<ShapeRole>().Length;

    private readonly RoundedCornerShape[] _shapes;

    public ResolvedShapeScheme(ShapeScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        _shapes = new RoundedCornerShape[RoleCount];
        _shapes[(int)ShapeRole.CornerNone] = RoundedCornerShape.Rectangle;
        _shapes[(int)ShapeRole.CornerExtraSmall] = scheme.ExtraSmall;
        _shapes[(int)ShapeRole.CornerExtraSmallTop] = scheme.ExtraSmall.Top();
        _shapes[(int)ShapeRole.CornerSmall] = scheme.Small;
        _shapes[(int)ShapeRole.CornerMedium] = scheme.Medium;
        _shapes[(int)ShapeRole.CornerLarge] = scheme.Large;
        _shapes[(int)ShapeRole.CornerLargeStart] = scheme.Large.Start();
        _shapes[(int)ShapeRole.CornerLargeEnd] = scheme.Large.End();
        _shapes[(int)ShapeRole.CornerLargeTop] = scheme.Large.Top();
        _shapes[(int)ShapeRole.CornerLargeIncreased] = scheme.LargeIncreased;
        _shapes[(int)ShapeRole.CornerExtraLarge] = scheme.ExtraLarge;
        _shapes[(int)ShapeRole.CornerExtraLargeIncreased] = scheme.ExtraLargeIncreased;
        _shapes[(int)ShapeRole.CornerExtraLargeTop] = scheme.ExtraLarge.Top();
        _shapes[(int)ShapeRole.CornerExtraExtraLarge] = scheme.ExtraExtraLarge;
        _shapes[(int)ShapeRole.CornerFull] = RoundedCornerShape.Circle;
    }

    public static ResolvedShapeScheme Default { get; } = new(new ShapeScheme());

    public RoundedCornerShape Get(ShapeRole role)
    {
        return _shapes[(int)role];
    }

    /// <summary>Resolves a role straight to corner radii for a control of the given size.</summary>
    public CornerRadius GetCornerRadius(ShapeRole role, Size shapeSize, bool isRightToLeft = false)
    {
        return Get(role).ToCornerRadius(shapeSize, isRightToLeft);
    }
}
