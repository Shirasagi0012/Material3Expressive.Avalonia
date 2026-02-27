using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Material3Expressive.Tokens;

namespace Material3Expressive.Themes;

internal static class TypographyHelper
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
        TypographyRoleToken token
    )
    {
        return CreateBinding(
            parentStack,
            TypeScaleTokens.BodyMedium.FontSize,
            typography => typography?.GetTypeScale(token).FontSize);
    }

    public static IBinding ProvideTypeScaleLineHeightBinding(
        IAvaloniaXamlIlParentStackProvider parentStack,
        TypographyRoleToken token
    )
    {
        return CreateBinding(
            parentStack,
            TypeScaleTokens.BodyMedium.LineHeight,
            typography => typography?.GetTypeScale(token).LineHeight);
    }

    public static IBinding ProvideTypeScaleLetterSpacingBinding(
        IAvaloniaXamlIlParentStackProvider parentStack,
        TypographyRoleToken token
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
        Func<Typography?, T> resolveValue
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
                            typography as Typography ?? appTypography as Typography),
                        _ => fallback
                    })
            };
            return binding;
        }

        appTypographyBinding.Converter = new FuncValueConverter<object?, T>(value =>
            resolveValue(value as Typography));
        return appTypographyBinding;

        static CompiledBindingExtension CreateBindingInternal(object? source)
        {
            return new CompiledBindingExtension
            {
                Mode = BindingMode.OneWay,
                Priority = BindingPriority.Style,
                Source = source,
                Path = new CompiledBindingPathBuilder()
                    .Property(Typography.TypographyHostProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
                    .Build()
            };
        }
    }
}

// Obsolete code for reference, is being removed.

// internal static class TypographyHelperObsolete
// {
//     private static readonly ClrPropertyInfo ThemeVariantPropertyInfo =
//         new(
//             nameof(IThemeVariantHost.ActualThemeVariant),
//             target => target is IThemeVariantHost host ? host.ActualThemeVariant : null,
//             null,
//             typeof(ThemeVariant));
//
//     private static readonly ClrPropertyInfo TypographyPropertyInfo =
//         new(
//             "Internal",
//             target => (target as Typography)?.Internal,
//             null,
//             typeof(Typography));
//
//     private static readonly ClrPropertyInfo TypographyInternalPropertyInfo =
//         new(
//             string.Empty,
//             target => target,
//             null,
//             typeof(Typography.TypographyInternal));
//
//     public static (IProvideValueTarget, IAvaloniaXamlIlParentStackProvider) GetContextServices(IServiceProvider services)
//     {
//         if (services.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget target)
//             throw new InvalidOperationException($"Service '{nameof(IProvideValueTarget)}' not found.");
//
//         if (services.GetService(typeof(IAvaloniaXamlIlParentStackProvider)) is not IAvaloniaXamlIlParentStackProvider parentStack)
//             throw new InvalidOperationException($"Service '{nameof(IAvaloniaXamlIlParentStackProvider)}' not found.");
//
//         return (target, parentStack);
//     }
//
//     public static IBinding ProvideMetricBinding<T>(
//         IAvaloniaXamlIlParentStackProvider parentStack,
//         TypographyRoleToken token,
//         TypographyMetric metric,
//         T fallback,
//         ThemeVariant? themeVariant = null)
//     {
//         var themeHost = ResolveThemeHost(parentStack);
//         return CreateBinding(
//             parentStack,
//             themeHost,
//             fallback,
//             (typography, theme) => ResolveMetric(typography, token, metric, theme, fallback),
//             themeVariant);
//     }
//
//     private static T ResolveMetric<T>(
//         Typography.TypographyInternal? typography,
//         TypographyRoleToken token,
//         TypographyMetric metric,
//         ThemeVariant theme,
//         T fallback)
//     {
//         var value = typography?.ResolveMetric(token, metric, theme)
//                     ?? TypographyTokens.ResolveMetric(token, metric);
//
//         return value is T typedValue ? typedValue : fallback;
//     }
//
//     private static IThemeVariantHost? ResolveThemeHost(IAvaloniaXamlIlParentStackProvider parentStack)
//     {
//         if (parentStack is not { Parents: { } parents })
//             return null;
//
//         foreach (var context in parents)
//             if (context is IThemeVariantHost host)
//                 return host;
//
//         return null;
//     }
//
//     private static IBinding CreateBinding<T>(
//         IAvaloniaXamlIlParentStackProvider parentStack,
//         IThemeVariantHost? themeHost,
//         T fallback,
//         Func<Typography.TypographyInternal?, ThemeVariant, T> resolveValue,
//         ThemeVariant? themeVariant = null)
//     {
//         var defaultTheme = themeHost?.ActualThemeVariant ?? ThemeVariant.Light;
//
//         object? source = null;
//         if (parentStack is { Parents: { } parents })
//             foreach (var parent in parents)
//                 if (parent is StyledElement styled)
//                 {
//                     source = styled;
//                     break;
//                 }
//
//         var typographyBinding = new CompiledBindingExtension
//         {
//             Mode = BindingMode.OneWay,
//             Priority = BindingPriority.Style,
//             Source = source ?? Application.Current,
//             Path = new CompiledBindingPathBuilder()
//                 .Property(Typography.TypographyHostProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
//                 .Property(TypographyPropertyInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
//                 .Property(TypographyInternalPropertyInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
//                 .Build(),
//         };
//
//         var appTypographyBinding = new CompiledBindingExtension
//         {
//             Mode = BindingMode.OneWay,
//             Priority = BindingPriority.Style,
//             Source = Application.Current,
//             Path = new CompiledBindingPathBuilder()
//                 .Property(Typography.TypographyHostProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
//                 .Property(TypographyPropertyInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
//                 .Property(TypographyInternalPropertyInfo, PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
//                 .Build(),
//         };
//
//         var binding = new MultiBinding
//         {
//             Mode = BindingMode.OneWay,
//             Priority = BindingPriority.Style,
//             FallbackValue = fallback!,
//             TargetNullValue = fallback!,
//         };
//
//         binding.Bindings = [typographyBinding, appTypographyBinding];
//         binding.Converter = new FuncMultiValueConverter<object?, T>(values =>
//             (values as IList<object?> ?? values.ToList()) switch
//             {
//                 [var typography, var appTypography, ..] => resolveValue(
//                     typography as Typography.TypographyInternal ?? appTypography as Typography.TypographyInternal,
//                     themeVariant),
//                 _ => fallback
//             });
//         
//
//         return binding;
//     }
// }
