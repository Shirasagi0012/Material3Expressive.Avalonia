using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>
/// A sparse Raw-token source containing one explicit definition.
/// </summary>
/// <remarks>
/// Add several instances to <see cref="MaterialTheme.Children"/> to override several tokens.
/// A null <see cref="Key"/> or <see cref="Value"/> means that this source contains no entry.
/// </remarks>
public sealed class TokenOverride : ResourceProvider, IMaterialTokenSource
{
    private MaterialToken? _key;
    private object? _value;
    private TokenDefinition? _definition;
    private bool _definitionValid;
    private event EventHandler? SourceChanged;

    /// <summary>Gets or sets the canonical Raw-token identity to override.</summary>
    public MaterialToken? Key
    {
        get => _key;
        set
        {
            if (ReferenceEquals(_key, value))
                return;

            _key = value;
            InvalidateDefinition();
        }
    }

    /// <summary>
    /// Gets or sets a literal value or a typed <see cref="TokenDefinition"/> such as TokenRef.
    /// </summary>
    public object? Value
    {
        get => _value;
        set
        {
            if (Equals(_value, value))
                return;

            _value = value;
            InvalidateDefinition();
        }
    }

    /// <inheritdoc />
    public override bool HasResources => _key is not null && _value is not null;

    /// <inheritdoc />
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (_key is not null && ReferenceEquals(key, _key) && TryGetDefinition(out var definition))
        {
            value = definition;
            return true;
        }

        value = null;
        return false;
    }

    bool IMaterialTokenSource.TryGetRawToken(
        MaterialToken token,
        ThemeVariant? queryTheme,
        out object? definition)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (_key is not null && ReferenceEquals(token, _key) && TryGetDefinition(out var typed))
        {
            definition = typed;
            return true;
        }

        definition = null;
        return false;
    }

    event EventHandler? IMaterialTokenSource.Changed
    {
        add => SourceChanged += value;
        remove => SourceChanged -= value;
    }

    private bool TryGetDefinition(out TokenDefinition? definition)
    {
        if (_key is null || _value is null)
        {
            definition = null;
            return false;
        }

        if (!_definitionValid)
        {
            _definition = CreateDefinition(_key, _value);
            _definitionValid = true;
        }

        definition = _definition;
        return true;
    }

    private static TokenDefinition CreateDefinition(MaterialToken key, object value)
    {
        if (value is TokenDefinition definition)
        {
            if (definition.ValueType != key.ValueType)
            {
                throw new InvalidOperationException(
                    $"TokenOverride for '{key.Path}' requires TokenDefinition<{key.ValueType.Name}>, " +
                    $"but received TokenDefinition<{definition.ValueType.Name}>.");
            }

            return definition;
        }

        var literal = value is string text
            ? ConvertLiteral(key, text)
            : value;

        try
        {
            return key.CreateLiteralDefinition(literal);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"TokenOverride value for '{key.Path}' is not assignable to " +
                $"'{key.ValueType.FullName}'.",
                exception);
        }
    }

    private static object ConvertLiteral(MaterialToken key, string text)
    {
        var valueType = key.ValueType;
        try
        {
            if (valueType == typeof(Color))
                return Color.Parse(text);

            if (valueType == typeof(FontFamily))
                return new FontFamily(text);

            if (valueType == typeof(RoundedCornerShape))
                return ParseShape(text);

            if (valueType == typeof(SpringSpec))
                return ParseSpring(text);

            var converter = TypeDescriptor.GetConverter(valueType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFrom(
                           null,
                           CultureInfo.InvariantCulture,
                           text)
                       ?? throw new FormatException("The converter returned null.");
            }
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"TokenOverride could not convert '{text}' to '{valueType.FullName}' for " +
                $"'{key.Path}'.",
                exception);
        }

        throw new InvalidOperationException(
            $"TokenOverride requires a '{valueType.FullName}' object or TokenDefinition for " +
            $"'{key.Path}'; no string converter is available.");
    }

    private static RoundedCornerShape ParseShape(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Equals("none", StringComparison.OrdinalIgnoreCase))
            return RoundedCornerShape.Rectangle;
        if (trimmed.Equals("full", StringComparison.OrdinalIgnoreCase))
            return RoundedCornerShape.Circle;

        var parts = trimmed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return RoundedCornerShape.Uniform(ParseCornerSize(parts[0]));
        if (parts.Length == 4)
        {
            return new RoundedCornerShape(
                ParseCornerSize(parts[0]),
                ParseCornerSize(parts[1]),
                ParseCornerSize(parts[2]),
                ParseCornerSize(parts[3]));
        }

        throw new FormatException(
            "A shape literal must be 'none', 'full', one corner size, or four comma-separated corner sizes.");
    }

    private static CornerSize ParseCornerSize(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.EndsWith('%'))
        {
            return CornerSize.Percent(double.Parse(
                trimmed[..^1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture));
        }

        return CornerSize.Absolute(double.Parse(
            trimmed,
            NumberStyles.Float,
            CultureInfo.InvariantCulture));
    }

    private static SpringSpec ParseSpring(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not (2 or 3))
        {
            throw new FormatException(
                "A spring literal must contain damping ratio, stiffness, and optionally visibility threshold.");
        }

        var damping = double.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture);
        var stiffness = double.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);
        var threshold = parts.Length == 3
            ? double.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture)
            : SpringSpec.DefaultVisibilityThreshold;
        return new SpringSpec(damping, stiffness, threshold);
    }

    private void InvalidateDefinition()
    {
        _definition = null;
        _definitionValid = false;
        RaiseResourcesChanged();
        SourceChanged?.Invoke(this, EventArgs.Empty);
    }
}
