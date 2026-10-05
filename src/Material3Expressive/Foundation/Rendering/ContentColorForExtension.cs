using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Tokens;
using Color = Material3Expressive.Tokens.MaterialTokens.Sys.Color;

namespace Material3Expressive.Foundation.Rendering;

// Fixed Material contentColorFor role matching, with the ambient content color fallback.
internal sealed class ContentColorForExtension : MarkupExtension, IMultiValueConverter
{
    public string BackgroundProperty { get; set; } = "Background";
    public BindingBase? AmbientContentColor { get; set; }
    private static readonly (MaterialTokenKey<Avalonia.Media.Color> Background, MaterialTokenKey<Avalonia.Media.Color> Content)[] Roles =
    [
        (Color.Primary, Color.OnPrimary), (Color.Secondary, Color.OnSecondary),
        (Color.Tertiary, Color.OnTertiary), (Color.Background, Color.OnBackground),
        (Color.Error, Color.OnError), (Color.PrimaryContainer, Color.OnPrimaryContainer),
        (Color.SecondaryContainer, Color.OnSecondaryContainer), (Color.TertiaryContainer, Color.OnTertiaryContainer),
        (Color.ErrorContainer, Color.OnErrorContainer), (Color.InverseSurface, Color.InverseOnSurface),
        (Color.Surface, Color.OnSurface), (Color.SurfaceVariant, Color.OnSurfaceVariant),
        (Color.SurfaceBright, Color.OnSurface), (Color.SurfaceContainer, Color.OnSurface),
        (Color.SurfaceContainerHigh, Color.OnSurface), (Color.SurfaceContainerHighest, Color.OnSurface),
        (Color.SurfaceContainerLow, Color.OnSurface), (Color.SurfaceContainerLowest, Color.OnSurface),
        (Color.SurfaceDim, Color.OnSurface), (Color.PrimaryFixed, Color.OnPrimaryFixed),
        (Color.PrimaryFixedDim, Color.OnPrimaryFixed), (Color.SecondaryFixed, Color.OnSecondaryFixed),
        (Color.SecondaryFixedDim, Color.OnSecondaryFixed), (Color.TertiaryFixed, Color.OnTertiaryFixed),
        (Color.TertiaryFixedDim, Color.OnTertiaryFixed),
    ];

    public override BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new MultiBinding
        {
            Converter = this,
            Priority = serviceProvider.GetService(typeof(IAvaloniaXamlIlControlTemplateProvider)) is null
                ? BindingPriority.LocalValue : BindingPriority.Template,
        };
        binding.Bindings.Add(new Binding(BackgroundProperty) { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
        binding.Bindings.Add(AmbientContentColor ?? new Binding("(TextElement.Foreground)")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(Control) },
            TypeResolver = (_, _) => typeof(Avalonia.Controls.Documents.TextElement),
        });
        foreach (var (background, content) in Roles)
        {
            // Each role remains a live token binding, including intermediate overrides.
            binding.Bindings.Add(new TokenExtension(background).ProvideValue(serviceProvider));
            binding.Bindings.Add(new TokenExtension(content).ProvideValue(serviceProvider));
        }
        return binding;
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        static Avalonia.Media.Color? SolidColor(object? value) => value switch
        {
            Avalonia.Media.Color color => color,
            ISolidColorBrush brush => brush.Color,
            _ => null,
        };
        if (values.Count != 2 + Roles.Length * 2) return AvaloniaProperty.UnsetValue;
        if (SolidColor(values[0]) is { } background)
            for (var index = 2; index < values.Count; index += 2)
                if (SolidColor(values[index]) == background && SolidColor(values[index + 1]) is { } content)
                    return new ImmutableSolidColorBrush(content);
        return values[1] is IBrush ambient ? ambient : AvaloniaProperty.UnsetValue;
    }
}
