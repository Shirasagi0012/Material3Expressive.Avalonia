using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation;

internal static class TypographyResolver
{
    public static IBinding ProvideFontFamilyBinding(
        IAvaloniaXamlIlParentStackProvider parentStack,
        FontFamilyToken token
    )
    {
        return CreateBinding(
            parentStack,
            TypefaceTokens.Brand,
            typography => token switch
            {
                FontFamilyToken.Brand => typography?.BrandFontFamily,
                FontFamilyToken.Plain => typography?.PlainFontFamily,
                FontFamilyToken.EmphasizedBrand => typography?.EmphasizedBrandFontFamily,
                FontFamilyToken.EmphasizedPlain => typography?.EmphasizedPlainFontFamily,
                _ => throw new ArgumentOutOfRangeException(nameof(typography), typography, null)
            });
    }

    public static IBinding ProvideFontWeightBinding(
        IAvaloniaXamlIlParentStackProvider parentStack,
        FontWeightToken token
    )
    {
        return CreateBinding(
            parentStack,
            TypefaceTokens.WeightRegular,
            typography => token switch
            {
                FontWeightToken.Regular => typography?.WeightRegular,
                FontWeightToken.Medium => typography?.WeightMedium,
                FontWeightToken.Bold => typography?.WeightBold,
                _ => throw new ArgumentOutOfRangeException(nameof(typography), typography, null)
            });
    }

    public static IBinding ProvideTypeScaleFontSizeBinding(
        IAvaloniaXamlIlParentStackProvider parentStack,
        TypographyRole token
    )
    {
        return CreateBinding(
            parentStack,
            TypeScaleTokens.BodyMedium.FontSize,
            typography => typography?.GetTypeScale(token).FontSize);
    }

    public static IBinding ProvideTypeScaleLineHeightBinding(
        IAvaloniaXamlIlParentStackProvider parentStack,
        TypographyRole token
    )
    {
        return CreateBinding(
            parentStack,
            TypeScaleTokens.BodyMedium.LineHeight,
            typography => typography?.GetTypeScale(token).LineHeight);
    }

    public static IBinding ProvideTypeScaleLetterSpacingBinding(
        IAvaloniaXamlIlParentStackProvider parentStack,
        TypographyRole token
    )
    {
        return CreateBinding(
            parentStack,
            TypeScaleTokens.BodyMedium.LetterSpacing,
            typography => typography?.GetTypeScale(token).LetterSpacing);
    }

    private static IBinding CreateBinding<T>(
        IAvaloniaXamlIlParentStackProvider parentStack,
        T fallback,
        Func<TypographyScheme?, T> resolveValue
    )
    {
        object? source = null;
        if (parentStack is { Parents: { } parents })
            foreach (var parent in parents)
                if (parent is StyledElement styled)
                {
                    source = styled;
                    break;
                }

        var typographyBinding = CreateBindingInternal(source);
        var appTypographyBinding = CreateBindingInternal(Application.Current);

        if (source is { })
        {
            var binding = new MultiBinding
            {
                Mode = BindingMode.OneWay,
                Priority = BindingPriority.Style,
                FallbackValue = fallback!,
                TargetNullValue = fallback!,
                Bindings = [typographyBinding, appTypographyBinding],
                Converter = new FuncMultiValueConverter<object?, T>(values =>
                    (values as IList<object?> ?? values.ToList()) switch
                    {
                        [var typography, var appTypography, ..] => resolveValue(
                            typography as TypographyScheme ?? appTypography as TypographyScheme),
                        _ => fallback
                    })
            };
            return binding;
        }

        appTypographyBinding.Converter = new FuncValueConverter<object?, T>(value =>
            resolveValue(value as TypographyScheme));
        return appTypographyBinding;

        static CompiledBindingExtension CreateBindingInternal(object? source)
        {
            return new CompiledBindingExtension
            {
                Mode = BindingMode.OneWay,
                Priority = BindingPriority.Style,
                Source = source,
                Path = new CompiledBindingPathBuilder()
                    .Property(TypographyScheme.TypographyHostProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
                    .Build()
            };
        }
    }
}
