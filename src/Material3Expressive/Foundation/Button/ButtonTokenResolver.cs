using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;

namespace Material3Expressive.Foundation;

internal static class ButtonTokenResolver
{
    public static BindingBase ProvideBinding(IAvaloniaXamlIlParentStackProvider parentStack, ButtonToken token)
    {
        return token switch
        {
            ButtonToken.ContainerBrush => CreateBinding(parentStack, new SolidColorBrush(Color.Parse("#FF6750A4")), tokens => tokens?.ContainerBrush),
            ButtonToken.ForegroundBrush => CreateBinding(parentStack, new SolidColorBrush(Colors.White), tokens => tokens?.ForegroundBrush),
            ButtonToken.DisabledContainerBrush => CreateBinding(parentStack, new SolidColorBrush(Color.Parse("#FFE7E0EC")), tokens => tokens?.DisabledContainerBrush),
            ButtonToken.DisabledForegroundBrush => CreateBinding(parentStack, new SolidColorBrush(Color.Parse("#FF49454F")), tokens => tokens?.DisabledForegroundBrush),
            ButtonToken.StateLayerBrush => CreateBinding(parentStack, new SolidColorBrush(Colors.White), tokens => tokens?.StateLayerBrush),
            ButtonToken.CornerRadius => CreateBinding(parentStack, new CornerRadius(20), tokens => tokens?.CornerRadius ?? new CornerRadius(20)),
            ButtonToken.Padding => CreateBinding(parentStack, new Thickness(24, 10), tokens => tokens?.Padding ?? new Thickness(24, 10)),
            ButtonToken.MinHeight => CreateBinding(parentStack, 40d, tokens => tokens?.MinHeight ?? 40d),
            ButtonToken.HoverStateLayerOpacity => CreateBinding(parentStack, 0.08d, tokens => tokens?.HoverStateLayerOpacity ?? 0.08d),
            ButtonToken.PressedStateLayerOpacity => CreateBinding(parentStack, 0.12d, tokens => tokens?.PressedStateLayerOpacity ?? 0.12d),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    private static BindingBase CreateBinding<T>(
        IAvaloniaXamlIlParentStackProvider parentStack,
        T fallback,
        Func<ButtonTokens?, T> resolveValue)
    {
        object? source = null;
        if (parentStack is { Parents: { } parents })
            foreach (var parent in parents)
                if (parent is StyledElement styled)
                {
                    source = styled;
                    break;
                }

        var localBinding = CreateBindingInternal(source);
        var appBinding = CreateBindingInternal(Application.Current);

        if (source is { })
        {
            return new MultiBinding
            {
                Mode = BindingMode.OneWay,
                Priority = BindingPriority.Style,
                FallbackValue = fallback!,
                TargetNullValue = fallback!,
                Bindings = [localBinding, appBinding],
                Converter = new FuncMultiValueConverter<object?, T>(values =>
                    (values as IList<object?> ?? values.ToList()) switch
                    {
                        [var localHost, var appHost, ..] => resolveValue(localHost as ButtonTokens ?? appHost as ButtonTokens),
                        _ => fallback
                    })
            };
        }

        appBinding.Converter = new FuncValueConverter<object?, T>(value =>
            resolveValue(value as ButtonTokens));

        return appBinding;

        static CompiledBindingExtension CreateBindingInternal(object? source)
        {
            return new CompiledBindingExtension
            {
                Mode = BindingMode.OneWay,
                Priority = BindingPriority.Style,
                Source = source,
                Path = new CompiledBindingPathBuilder()
                    .Property(ButtonTokens.ButtonHostProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
                    .Build()
            };
        }
    }
}
