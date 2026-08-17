using System.ComponentModel;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>A sparse, XAML-friendly override provider for one system type-scale role.</summary>
/// <remarks>
/// Unset properties do not publish a resource, so each field continues independently through the
/// resource cascade. Properties accept either a literal or a token definition such as
/// <c>{m3:TokenRef ...}</c>.
/// </remarks>
[Obsolete(
    "Legacy Runtime.MaterialToken type-scale overrides. Use typed shared Raw overrides.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SystemTypeScaleOverrides : MaterialTokenOverrides
{
    private TypographyRole _role;
    private TokenDefinition<double>? _fontSize;
    private TokenDefinition<double>? _lineHeight;
    private TokenDefinition<double>? _letterSpacing;
    private TokenDefinition<FontFamilyToken>? _font;
    private TokenDefinition<FontWeightToken>? _weight;

    /// <summary>Creates overrides for <see cref="TypographyRole.DisplayLarge"/>.</summary>
    public SystemTypeScaleOverrides()
    {
    }

    /// <summary>Creates overrides for <paramref name="role"/>.</summary>
    public SystemTypeScaleOverrides(TypographyRole role)
    {
        _ = SystemTypeScaleTokens.For(role);
        _role = role;
    }

    /// <summary>The role whose keys this provider overrides.</summary>
    public TypographyRole Role
    {
        get => _role;
        set
        {
            if (_role == value)
                return;

            _ = SystemTypeScaleTokens.For(value);
            using var update = BeginUpdate();
            RemoveDefinitions(_role);
            _role = value;
            PublishDefinitions();
        }
    }

    [TypeConverter(typeof(DoubleTokenDefinitionTypeConverter))]
    public TokenDefinition<double>? FontSize
    {
        get => _fontSize;
        set => Set(ref _fontSize, value, SystemTypeScaleTokens.For(Role).FontSize);
    }

    [TypeConverter(typeof(DoubleTokenDefinitionTypeConverter))]
    public TokenDefinition<double>? LineHeight
    {
        get => _lineHeight;
        set => Set(ref _lineHeight, value, SystemTypeScaleTokens.For(Role).LineHeight);
    }

    [TypeConverter(typeof(DoubleTokenDefinitionTypeConverter))]
    public TokenDefinition<double>? LetterSpacing
    {
        get => _letterSpacing;
        set => Set(ref _letterSpacing, value, SystemTypeScaleTokens.For(Role).LetterSpacing);
    }

    [TypeConverter(typeof(FontFamilyTokenDefinitionTypeConverter))]
    public TokenDefinition<FontFamilyToken>? Font
    {
        get => _font;
        set => Set(ref _font, value, SystemTypeScaleTokens.For(Role).Font);
    }

    [TypeConverter(typeof(FontWeightTokenDefinitionTypeConverter))]
    public TokenDefinition<FontWeightToken>? Weight
    {
        get => _weight;
        set => Set(ref _weight, value, SystemTypeScaleTokens.For(Role).Weight);
    }

    private void Set<T>(
        ref TokenDefinition<T>? field,
        TokenDefinition<T>? value,
        MaterialToken<T> token)
    {
        if (EqualityComparer<TokenDefinition<T>?>.Default.Equals(field, value))
            return;

        field = value;
        if (value is null)
            Clear(token);
        else
            SetDefinition(token, value);
    }

    private void RemoveDefinitions(TypographyRole role)
    {
        var tokens = SystemTypeScaleTokens.For(role);
        Clear(tokens.FontSize);
        Clear(tokens.LineHeight);
        Clear(tokens.LetterSpacing);
        Clear(tokens.Font);
        Clear(tokens.Weight);
    }

    private void PublishDefinitions()
    {
        var tokens = SystemTypeScaleTokens.For(Role);
        Publish(tokens.FontSize, _fontSize);
        Publish(tokens.LineHeight, _lineHeight);
        Publish(tokens.LetterSpacing, _letterSpacing);
        Publish(tokens.Font, _font);
        Publish(tokens.Weight, _weight);
    }

    private void Publish<T>(MaterialToken<T> token, TokenDefinition<T>? definition)
    {
        if (definition is not null)
            SetDefinition(token, definition);
    }
}
