using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>
/// What a component colour token holds: either a reference to a system colour role, or a literal
/// colour.
/// </summary>
/// <remarks>
/// Defaults are always references — <c>ContainerColor = Primary</c>, not <c>#6750A4</c> — which is
/// what makes dark mode and dynamic colour work with no wiring at the app level: the role resolves
/// against whichever scheme and variant are live where the control actually sits. Literals exist
/// so an app author can override one token without leaving the system.
/// </remarks>
[TypeConverter(typeof(ColorTokenValueConverter))]
public readonly record struct ColorTokenValue
{
    private ColorTokenValue(SysColorToken role, string? customKey, Color? literal)
    {
        RoleToken = role;
        CustomKey = customKey;
        LiteralColor = literal;
    }

    /// <summary>The referenced role. Meaningless when <see cref="IsLiteral" /> is true.</summary>
    public SysColorToken RoleToken { get; }

    /// <summary>Names the custom colour for the four <c>Custom*</c> roles.</summary>
    public string? CustomKey { get; }

    /// <summary>The literal colour, when this token was overridden with one.</summary>
    public Color? LiteralColor { get; }

    public bool IsLiteral => LiteralColor.HasValue;

    public static ColorTokenValue Role(SysColorToken role, string? customKey = null)
    {
        return new ColorTokenValue(role, customKey, null);
    }

    public static ColorTokenValue Literal(Color color)
    {
        return new ColorTokenValue(default, null, color);
    }

    /// <summary>
    /// Resolves against a scheme. <paramref name="opacity" /> scales the alpha channel, which is
    /// how the disabled states are specified upstream (a role plus an opacity, not a second role).
    /// </summary>
    public Color Resolve(ResolvedColorScheme? scheme, bool isDark, double opacity = 1.0)
    {
        var color = LiteralColor ?? scheme?.GetColor(RoleToken, isDark, CustomKey) ?? Colors.Transparent;

        if (opacity >= 1.0)
            return color;

        return new Color((byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)), color.R, color.G, color.B);
    }

    /// <inheritdoc cref="Resolve" />
    public IImmutableBrush ResolveBrush(ResolvedColorScheme? scheme, bool isDark, double opacity = 1.0)
    {
        // Fully opaque role lookups hit the scheme's brush cache; anything else has to allocate.
        if (!IsLiteral && opacity >= 1.0 && scheme is { })
            return scheme.GetBrush(RoleToken, isDark, CustomKey);

        return new ImmutableSolidColorBrush(Resolve(scheme, isDark, opacity));
    }

    public static implicit operator ColorTokenValue(SysColorToken role)
    {
        return Role(role);
    }

    public static implicit operator ColorTokenValue(Color color)
    {
        return Literal(color);
    }

    public override string ToString()
    {
        if (LiteralColor is { } literal)
            return literal.ToString();

        return CustomKey is { Length: > 0 } ? $"{RoleToken}:{CustomKey}" : RoleToken.ToString();
    }
}

/// <summary>
/// Parses <c>Primary</c> (a role), <c>Custom:Brand</c> (a role plus custom-colour key) and
/// <c>#FF5722</c> (a literal).
/// </summary>
public sealed class ColorTokenValueConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
            return base.ConvertFrom(context, culture, value);

        var trimmed = text.Trim();

        var separator = trimmed.IndexOf(':');
        if (separator > 0)
        {
            var roleText = trimmed[..separator];
            var key = trimmed[(separator + 1)..];

            if (Enum.TryParse<SysColorToken>(roleText, true, out var keyedRole))
                return ColorTokenValue.Role(keyedRole, key);
        }

        if (Enum.TryParse<SysColorToken>(trimmed, true, out var role))
            return ColorTokenValue.Role(role);

        if (Color.TryParse(trimmed, out var color))
            return ColorTokenValue.Literal(color);

        throw new FormatException(
            $"'{text}' is neither a system colour role nor a colour. Expected e.g. 'Primary', 'Custom:Brand' or '#FF5722'.");
    }
}
