using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Tokens;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>
/// Consumption-side multi-hop token resolution (design doc §5).
/// Every hop restarts its upward search from the requester's position (§5.1), so overriding
/// any key on the chain — root or intermediate — takes effect (R1'/R2'). The nearest entry
/// for a key wins; if its reference target is missing, cyclic, or over the hop budget,
/// resolution fails and the property falls back via UnsetValue — developer errors surface
/// loudly instead of being silently shadowed by outer definitions (deliberately simpler
/// than the fall-through semantics in §5.5). Results are never cached (§5.4).
/// </summary>
internal static class TokenResolver
{
    /// <summary>
    /// Cycles are bounded by a fixed hop budget: a chain follows at most this many
    /// references/derivations. The typography chain can occupy all five steps:
    /// component → aggregate → em → sp → font scale. Longer chains fail like cycles.
    /// </summary>
    private const int MaxHops = 5;

    public static bool TryResolve(IResourceHost host, MaterialTokenKey rootKey, ThemeVariant? theme, out object? value) =>
        TryResolveCore(host, rootKey, theme, MaxHops, out value);

    /// <summary>
    /// Resolves <paramref name="key"/> from the requester's position. The loop counts the hops:
    /// each TokenRef follow restarts the upward search from the same position and costs one
    /// iteration; running out of iterations means a cycle. The remaining budget is forwarded
    /// into derivation evaluation so that cycles routed through one (e.g. an entry referencing
    /// itself as its WithAlpha color or SpringSpecRef damping) still terminate instead of
    /// overflowing the stack.
    /// </summary>
    private static bool TryResolveCore(IResourceHost host, MaterialTokenKey key, ThemeVariant? theme, int hopsLeft, out object? value)
    {
        while (hopsLeft-- > 0)
        {
            if (!host.TryFindResource(key, theme, out var candidate))
                break;

            switch (candidate)
            {
                case TokenRef reference:
                    key = reference.Key; continue;

                case WithAlpha withAlpha:
                    return TryEvaluateWithAlpha(host, withAlpha, theme, hopsLeft, out value);

                case SpringSpecRef specRef:
                    return TryEvaluateSpringSpec(host, specRef, theme, hopsLeft, out value);

                case TextStyleRef styleRef:
                    return TryEvaluateTextStyle(host, styleRef, theme, hopsLeft, out value);

                case TextUnit unit:
                    value = null;
                    var factorKey = unit.FontSize ?? MaterialTokens.Sys.TypeScale.FontScale;
                    if (!TryResolveCore(host, factorKey, theme, hopsLeft, out var factorValue)
                        || factorValue is not double factor || !double.IsFinite(factor) || factor <= 0)
                        return false;
                    var dip = unit.Value * factor;
                    if (!double.IsFinite(dip))
                        return false;
                    value = dip;
                    return true;

                default:
                    value = candidate;
                    return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryEvaluateWithAlpha(IResourceHost host, WithAlpha withAlpha, ThemeVariant? theme, int hopsLeft, out object? value)
    {
        value = null;

        if (!TryResolveCore(host, withAlpha.Color, theme, hopsLeft, out var colorValue) ||
            colorValue is not Color color)
            return false;

        double opacity;
        if (withAlpha.Opacity is { } opacityKey)
        {
            if (!TryResolveCore(host, opacityKey, theme, hopsLeft, out var opacityValue) ||
                opacityValue is not double literal)
                return false;
            opacity = literal;
        }
        else
        {
            opacity = withAlpha.OpacityValue ?? 1.0;
        }

        var alpha = (byte)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * color.A);
        value = Color.FromArgb(alpha, color.R, color.G, color.B);
        return true;
    }

    private static bool TryEvaluateTextStyle(IResourceHost host, TextStyleRef reference, ThemeVariant? theme, int hopsLeft, out object? value)
    {
        value = null;
        if (!TryResolveCore(host, reference.Font, theme, hopsLeft, out var font) || font is not FontFamily family
            || !TryResolveCore(host, reference.Size, theme, hopsLeft, out var size) || size is not double fontSize
            || !TryResolveCore(host, reference.Weight, theme, hopsLeft, out var weight) || weight is not FontWeight fontWeight
            || !TryResolveCore(host, reference.LetterSpacing, theme, hopsLeft, out var tracking) || tracking is not double letterSpacing
            || !TryResolveCore(host, reference.LineHeight, theme, hopsLeft, out var height) || height is not double lineHeight)
            return false;
        value = new TextStyle
        {
            FontFamily = family, FontSize = fontSize, FontWeight = fontWeight,
            LetterSpacing = letterSpacing, LineHeight = lineHeight,
        };
        return true;
    }

    private static bool TryEvaluateSpringSpec(IResourceHost host, SpringSpecRef specRef, ThemeVariant? theme, int hopsLeft, out object? value)
    {
        value = null;

        if (!TryResolveCore(host, specRef.Damping, theme, hopsLeft, out var dampingValue) ||
            dampingValue is not double damping)
            return false;

        if (!TryResolveCore(host, specRef.Stiffness, theme, hopsLeft, out var stiffnessValue) ||
            stiffnessValue is not double stiffness)
            return false;

        value = new SpringSpec(
            damping,
            stiffness,
            specRef.VisibilityThreshold ?? SpringSpec.DefaultVisibilityThreshold);
        return true;
    }
}
