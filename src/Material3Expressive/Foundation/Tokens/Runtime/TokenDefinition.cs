using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace Material3Expressive.Foundation.Tokens.Runtime;

internal interface ITokenDefinition
{
    Type ValueType { get; }
    TokenDefinitionKind Kind { get; }
    object? LiteralValue { get; }
    MaterialToken? AliasTarget { get; }
    IReadOnlyList<MaterialToken> Dependencies { get; }
    object? EvaluateRecipe(TokenRecipeContext context);
}

internal enum TokenDefinitionKind
{
    Unsupported,
    Literal,
    Alias,
    Recipe,
}

/// <summary>Describes how a token obtains a value in a resource scope.</summary>
/// <typeparam name="T">The resolved token value type.</typeparam>
[TypeConverter(typeof(TokenDefinitionTypeConverter))]
[Obsolete(
    "Legacy descriptor-owned token definition. Use " +
    "MaterialDesignTokens.Avalonia.TokenDefinition<T>.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract record TokenDefinition<T> : ITokenDefinition
{
    // Resolution is intentionally closed over the three definition kinds below. Recipes are the
    // public extension point for custom computations; arbitrary derived definitions could not be
    // evaluated without weakening the typed, inspectable dependency contract.
    private protected TokenDefinition()
    {
    }

    Type ITokenDefinition.ValueType => typeof(T);
    TokenDefinitionKind ITokenDefinition.Kind => Kind;
    object? ITokenDefinition.LiteralValue => LiteralValue;
    MaterialToken? ITokenDefinition.AliasTarget => AliasTarget;
    IReadOnlyList<MaterialToken> ITokenDefinition.Dependencies => Dependencies;
    object? ITokenDefinition.EvaluateRecipe(TokenRecipeContext context) => EvaluateRecipe(context);

    internal virtual TokenDefinitionKind Kind => TokenDefinitionKind.Unsupported;
    internal virtual object? LiteralValue => null;
    internal virtual MaterialToken? AliasTarget => null;
    internal virtual IReadOnlyList<MaterialToken> Dependencies => Array.Empty<MaterialToken>();
    internal virtual object? EvaluateRecipe(TokenRecipeContext context) =>
        throw new InvalidOperationException($"Unsupported token definition '{GetType().FullName}'.");
}

/// <summary>Converts a XAML literal to the value type of a typed token-definition property.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TokenDefinitionTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object? ConvertFrom(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value)
    {
        var propertyType = context?.PropertyDescriptor?.PropertyType;
        if (value is string text &&
            propertyType is { IsGenericType: true } &&
            propertyType.GetGenericTypeDefinition() == typeof(TokenDefinition<>))
        {
            var valueType = propertyType.GetGenericArguments()[0];
            var literal = TokenLiteralConversion.ConvertFromString(valueType, context, culture, text);
            return typeof(Token).GetMethod(nameof(Token.Literal))!
                .MakeGenericMethod(valueType).Invoke(null, [literal]);
        }

        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>
/// Converts a XAML literal when the generated override property supplies its closed token value
/// type. Avalonia's compiled-XAML converter calls do not provide a property descriptor, so the
/// non-generic converter cannot reliably discover <typeparamref name="T" /> from the context.
/// </summary>
/// <typeparam name="T">The token's resolved value type.</typeparam>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public class TokenDefinitionTypeConverter<T> : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object? ConvertFrom(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value)
    {
        if (value is string text)
        {
            var literal = TokenLiteralConversion.ConvertFromString(typeof(T), context, culture, text);
            return Token.Literal((T)literal!);
        }

        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>Converts XAML color literals to token definitions.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ColorTokenDefinitionTypeConverter : TokenDefinitionTypeConverter<Color>
{
}

/// <summary>Converts XAML numeric literals to token definitions.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DoubleTokenDefinitionTypeConverter : TokenDefinitionTypeConverter<double>
{
}

/// <summary>Converts XAML Boolean literals to token definitions.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class BooleanTokenDefinitionTypeConverter : TokenDefinitionTypeConverter<bool>
{
}

/// <summary>Converts XAML shape-role literals to token definitions.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ShapeTokenDefinitionTypeConverter :
    TokenDefinitionTypeConverter<global::Material3Expressive.Foundation.ShapeRole>
{
}

/// <summary>Converts XAML typography-role literals to token definitions.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TypographyTokenDefinitionTypeConverter :
    TokenDefinitionTypeConverter<global::Material3Expressive.Foundation.TypographyRole>
{
}

/// <summary>Converts XAML motion-role literals to token definitions.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MotionTokenDefinitionTypeConverter :
    TokenDefinitionTypeConverter<global::Material3Expressive.Foundation.MotionRole>
{
}

/// <summary>Converts XAML typeface-family token literals to token definitions.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FontFamilyTokenDefinitionTypeConverter :
    TokenDefinitionTypeConverter<global::Material3Expressive.Foundation.Tokens.FontFamilyToken>
{
}

/// <summary>Converts XAML typeface-weight token literals to token definitions.</summary>
[Obsolete(
    "Legacy token-definition converter. Use the shared MaterialDesignTokens.Avalonia contract.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FontWeightTokenDefinitionTypeConverter :
    TokenDefinitionTypeConverter<global::Material3Expressive.Foundation.Tokens.FontWeightToken>
{
}

internal static class TokenLiteralConversion
{
    public static object? ConvertFromString(
        Type valueType,
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        string text)
    {
        if (valueType == typeof(Color))
            return Color.Parse(text);

        return TypeDescriptor.GetConverter(valueType).ConvertFrom(context, culture, text);
    }
}

/// <summary>A token definition containing a concrete value.</summary>
[Obsolete(
    "Legacy descriptor-owned token definition. Use MaterialDesignTokens.Avalonia.Token.Literal.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record LiteralTokenDefinition<T>(T Value) : TokenDefinition<T>
{
    internal override TokenDefinitionKind Kind => TokenDefinitionKind.Literal;
    internal override object? LiteralValue => Value;
}

/// <summary>A token definition which resolves another token from the same consumer scope.</summary>
[Obsolete(
    "Legacy descriptor-owned token definition. Use MaterialDesignTokens.Avalonia.Token.Alias.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record AliasTokenDefinition<T> : TokenDefinition<T>
{
    /// <summary>Creates an alias definition.</summary>
    public AliasTokenDefinition(MaterialToken<T> target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    /// <summary>Gets the referenced token.</summary>
    public MaterialToken<T> Target { get; }

    internal override TokenDefinitionKind Kind => TokenDefinitionKind.Alias;
    internal override MaterialToken AliasTarget => Target;
}

/// <summary>A token definition evaluated from an explicit, typed dependency set.</summary>
[Obsolete(
    "Legacy descriptor-owned token definition. Use MaterialDesignTokens.Avalonia.Token.Recipe.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record RecipeTokenDefinition<T> : TokenDefinition<T>
{
    /// <summary>Creates a recipe definition.</summary>
    public RecipeTokenDefinition(ITokenRecipe<T> recipe)
    {
        Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
    }

    /// <summary>Gets the recipe.</summary>
    public ITokenRecipe<T> Recipe { get; }

    internal override TokenDefinitionKind Kind => TokenDefinitionKind.Recipe;
    internal override IReadOnlyList<MaterialToken> Dependencies => Recipe.Dependencies;
    internal override object? EvaluateRecipe(TokenRecipeContext context) => Recipe.Evaluate(context);
}

/// <summary>A pure, inspectable computation used to derive one token from other tokens.</summary>
[Obsolete(
    "Legacy descriptor-owned recipe. Use MaterialDesignTokens.Avalonia.ITokenRecipe<T>.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITokenRecipe<out T>
{
    /// <summary>Gets the dependencies the recipe may request from <see cref="TokenRecipeContext" />.</summary>
    IReadOnlyList<MaterialToken> Dependencies { get; }

    /// <summary>Evaluates the recipe.</summary>
    T Evaluate(TokenRecipeContext context);
}

/// <summary>Provides requester-local dependency values to a token recipe.</summary>
[Obsolete(
    "Legacy requester-local recipe context. Use the shared Raw token recipe context.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TokenRecipeContext
{
    private readonly Func<MaterialToken, object?> _resolve;
    private readonly HashSet<MaterialToken> _dependencies;

    internal TokenRecipeContext(IReadOnlyList<MaterialToken> dependencies, Func<MaterialToken, object?> resolve)
    {
        _dependencies = new HashSet<MaterialToken>(dependencies);
        _resolve = resolve;
    }

    /// <summary>Resolves a declared dependency from the same consumer scope.</summary>
    public T Get<T>(MaterialToken<T> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!_dependencies.Contains(token))
            throw new InvalidOperationException(
                $"Token recipe requested undeclared dependency '{token.Path}'. " +
                "Every dependency must be listed by ITokenRecipe.Dependencies.");

        return (T)_resolve(token)!;
    }
}

/// <summary>Creates strongly typed token definitions.</summary>
[Obsolete(
    "Legacy descriptor-owned token factory. Use MaterialDesignTokens.Avalonia.Token.",
    DiagnosticId = "M3E1001")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Token
{
    /// <summary>Creates a literal definition.</summary>
    public static TokenDefinition<T> Literal<T>(T value) => new LiteralTokenDefinition<T>(value);

    /// <summary>Creates an alias resolved from the eventual consumer scope.</summary>
    public static TokenDefinition<T> Alias<T>(MaterialToken<T> target) => new AliasTokenDefinition<T>(target);

    /// <summary>Creates a definition backed by a pure typed recipe.</summary>
    public static TokenDefinition<T> Recipe<T>(ITokenRecipe<T> recipe) => new RecipeTokenDefinition<T>(recipe);
}
