using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;

namespace Material3Expressive.Foundation.Typography;

/// <summary>Creates a live derived style. A field accepts a literal or a binding.</summary>
public sealed class TextStyleExtension : MarkupExtension
{
    public object? BasedOn { get; set; }
    public object? FontFamily { get; set; }
    public object? FontSize { get; set; }
    public object? FontWeight { get; set; }
    public object? FontStyle { get; set; }
    public object? FontStretch { get; set; }
    public object? LetterSpacing { get; set; }
    public object? LineHeight { get; set; }
    public object? TextAlignment { get; set; }
    public object? Foreground { get; set; }

    public override BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var fields = new (string Name, object? Value)[]
        {
            (nameof(BasedOn), BasedOn ?? TextStyle.Empty),
            (nameof(FontFamily), FontFamily), (nameof(FontSize), FontSize),
            (nameof(FontWeight), FontWeight), (nameof(FontStyle), FontStyle),
            (nameof(FontStretch), FontStretch), (nameof(LetterSpacing), LetterSpacing),
            (nameof(LineHeight), LineHeight), (nameof(TextAlignment), TextAlignment),
            (nameof(Foreground), Foreground),
        }.Where(field => field.Value is not null).ToArray();

        var binding = new MultiBinding
        {
            Converter = new MergeConverter(fields.Select(field => field.Name).ToArray(),
                (serviceProvider.GetService(typeof(IUriContext)) as IUriContext)?.BaseUri),
            Priority = serviceProvider.GetService(typeof(IAvaloniaXamlIlControlTemplateProvider)) is null
                ? BindingPriority.LocalValue : BindingPriority.Template,
        };
        foreach (var field in fields)
            binding.Bindings.Add(field.Value is BindingBase child ? child : new Binding { Source = field.Value });
        return binding;
    }

    private sealed class MergeConverter(string[] fields, Uri? baseUri) : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 0 || values[0] is not TextStyle basedOn)
                return AvaloniaProperty.UnsetValue;
            var result = basedOn;
            for (var i = 1; i < fields.Length; i++)
            {
                var value = values[i];
                if (value is null || value == AvaloniaProperty.UnsetValue)
                    continue;
                result = fields[i] switch
                {
                    nameof(FontFamily) => result with { FontFamily = value is FontFamily family ? family : Avalonia.Media.FontFamily.Parse((string)value, baseUri) },
                    nameof(FontSize) => result with { FontSize = Read<double>(value) },
                    nameof(FontWeight) => result with { FontWeight = Read<FontWeight>(value) },
                    nameof(FontStyle) => result with { FontStyle = Read<FontStyle>(value) },
                    nameof(FontStretch) => result with { FontStretch = Read<FontStretch>(value) },
                    nameof(LetterSpacing) => result with { LetterSpacing = Read<double>(value) },
                    nameof(LineHeight) => result with { LineHeight = Read<double>(value) },
                    nameof(TextAlignment) => result with { TextAlignment = Read<TextAlignment>(value) },
                    nameof(Foreground) => result with
                    {
                        Foreground = value switch
                        {
                            IBrush brush => brush,
                            Color color => new Avalonia.Media.Immutable.ImmutableSolidColorBrush(color),
                            string text => Brush.Parse(text),
                            _ => throw new InvalidOperationException($"Cannot convert {value} to a foreground brush."),
                        },
                    },
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
            return result;
        }

        private static T Read<T>(object value) => value is T typed ? typed
            : typeof(T) == typeof(double) ? (T)(object)System.Convert.ToDouble(value, CultureInfo.InvariantCulture)
            : (T)(TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(null, CultureInfo.InvariantCulture, value)
                ?? throw new InvalidOperationException($"Cannot convert {value} to {typeof(T).Name}."));
    }
}
