using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>Converts a XAML literal to a shared Raw-token definition.</summary>
public class RawTokenDefinitionTypeConverter<T> : TypeConverter
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
            var literal = Runtime.TokenLiteralConversion.ConvertFromString(
                typeof(T), context, culture, text);
            return Token.Literal((T)literal!);
        }

        return base.ConvertFrom(context, culture, value);
    }
}

public sealed class RawColorTokenDefinitionTypeConverter :
    RawTokenDefinitionTypeConverter<Color>
{
}

public sealed class RawDoubleTokenDefinitionTypeConverter :
    RawTokenDefinitionTypeConverter<double>
{
}

public sealed class RawBooleanTokenDefinitionTypeConverter :
    RawTokenDefinitionTypeConverter<bool>
{
}

public sealed class RawShapeTokenDefinitionTypeConverter :
    RawTokenDefinitionTypeConverter<global::Material3Expressive.Foundation.ShapeRole>
{
}

public sealed class RawTypographyTokenDefinitionTypeConverter :
    RawTokenDefinitionTypeConverter<global::Material3Expressive.Foundation.TypographyRole>
{
}

public sealed class RawMotionTokenDefinitionTypeConverter :
    RawTokenDefinitionTypeConverter<global::Material3Expressive.Foundation.MotionRole>
{
}
