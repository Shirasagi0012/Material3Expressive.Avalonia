using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Material3ExpressiveRefactor.Foundation.Tokens;

/// <summary>
/// <c>{m3:Token}</c> — consumes a token with consumption-side multi-hop resolution (design doc §5).
/// Implemented as a publicly composed <see cref="MultiBinding"/>: a Self binding supplies the
/// consuming target, a <see cref="DynamicResourceExtension"/> on the same key supplies the
/// re-resolution signal (resource changes, theme-variant switches, tree moves), and the converter
/// performs the actual hop-by-hop resolution. Custom binding expressions are closed to third
/// parties (BindingBase.CreateInstance is internal), so this public bridge is the only shape
/// that gets full binding treatment in Setter.Value, ControlTemplate and plain property contexts.
/// </summary>
public class TokenExtension : MarkupExtension
{
    public TokenExtension(MaterialTokenKey key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public MaterialTokenKey? Key { get; set; }

    // Declared as BindingBase so the XAML compiler treats the produced value as a binding (§10).
    public override BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key ?? throw new InvalidOperationException("TokenExtension requires a key.");

        // The DynamicResource child is only a change signal; calling ProvideValue lets the
        // platform extension capture anchor/theme/priority exactly as a plain {DynamicResource}
        // would. (MultiBinding discards the outer anchor when instantiating children, so this
        // captured state is the only anchor for non-StyledElement targets such as resource
        // dictionary entries.)
        var trigger = new DynamicResourceExtension(key);
        trigger.ProvideValue(serviceProvider);

        var binding = new MultiBinding
        {
            Converter = new TokenBindingConverter(
                key,
                CaptureAnchor(serviceProvider),
                CaptureThemeVariant(serviceProvider)),
        };

        // values[0]: the actual consuming target.
        binding.Bindings.Add(new CompiledBinding(new CompiledBindingPathBuilder().Self().Build()));
        // values[1]: the re-resolution trigger; its value is never read.
        binding.Bindings.Add(trigger);

        // Inside control templates the XAML compiler discards BindingBase priorities, so the
        // Template priority must be requested by the extension itself — the same recipe
        // DynamicResourceExtension uses, via the public IAvaloniaXamlIlControlTemplateProvider
        // service. (In Setter.Value the style machinery overrides the priority anyway.)
        if (serviceProvider.GetService(typeof(IAvaloniaXamlIlControlTemplateProvider)) is not null)
            binding.Priority = BindingPriority.Template;

        return binding;
    }

    // The capture helpers below replicate DynamicResourceExtension.ProvideValue (whose
    // GetFirstParent<T>/GetDictionaryVariant helpers are internal) using public runtime services.

    private static object? CaptureAnchor(IServiceProvider serviceProvider)
    {
        var provideTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        if (provideTarget?.TargetObject is StyledElement)
            return null; // the target itself is the resource host at runtime

        return GetFirstParent<StyledElement>(serviceProvider) ??
            (object?)GetFirstParent<IResourceProvider>(serviceProvider) ??
            GetFirstParent<IResourceHost>(serviceProvider);
    }

    private static ThemeVariant? CaptureThemeVariant(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider)) is not IAvaloniaXamlIlParentStackProvider stack)
            return null;

        foreach (var parent in stack.Parents)
        {
            if (parent is IThemeVariantProvider { Key: { } themeVariant })
                return themeVariant;
        }

        return null;
    }

    private static T? GetFirstParent<T>(IServiceProvider serviceProvider) where T : class
    {
        if (serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider)) is not IAvaloniaXamlIlParentStackProvider stack)
            return null;

        foreach (var parent in stack.Parents)
        {
            if (parent is T match)
                return match;
        }

        return null;
    }
}
