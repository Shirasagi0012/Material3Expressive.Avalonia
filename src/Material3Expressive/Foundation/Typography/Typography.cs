using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Foundation.Typography;

public enum TypographyRole
{
    DisplayLarge,
    DisplayMedium,
    DisplaySmall,
    HeadlineLarge,
    HeadlineMedium,
    HeadlineSmall,
    TitleLarge,
    TitleMedium,
    TitleSmall,
    BodyLarge,
    BodyMedium,
    BodySmall,
    LabelLarge,
    LabelMedium,
    LabelSmall,
    DisplayLargeEmphasized,
    DisplayMediumEmphasized,
    DisplaySmallEmphasized,
    HeadlineLargeEmphasized,
    HeadlineMediumEmphasized,
    HeadlineSmallEmphasized,
    TitleLargeEmphasized,
    TitleMediumEmphasized,
    TitleSmallEmphasized,
    BodyLargeEmphasized,
    BodyMediumEmphasized,
    BodySmallEmphasized,
    LabelLargeEmphasized,
    LabelMediumEmphasized,
    LabelSmallEmphasized,
}

/// <summary>Consumer-scoped access to Material typography. GetStyle is a snapshot; CreateBinding stays live.</summary>
public static class Typography
{
    public static TextStyle GetStyle(IResourceHost host, TypographyRole role)
    {
        var key = Key(role);
        if (TokenResolver.TryResolve(host, key, (host as IThemeVariantHost)?.ActualThemeVariant, out var value)
            && value is TextStyle style)
            return style;
        throw new InvalidOperationException($"Cannot resolve typography role {role} at this resource scope.");
    }

    public static BindingBase CreateBinding(TypographyRole role) =>
        new TypographyExtension(role).ProvideValue(EmptyServices.Instance);

    internal static MaterialTokenKey<TextStyle> Key(TypographyRole role) => role switch
    {
        TypographyRole.DisplayLarge => Sys.TypeScale.DisplayLarge.Style,
        TypographyRole.DisplayMedium => Sys.TypeScale.DisplayMedium.Style,
        TypographyRole.DisplaySmall => Sys.TypeScale.DisplaySmall.Style,
        TypographyRole.HeadlineLarge => Sys.TypeScale.HeadlineLarge.Style,
        TypographyRole.HeadlineMedium => Sys.TypeScale.HeadlineMedium.Style,
        TypographyRole.HeadlineSmall => Sys.TypeScale.HeadlineSmall.Style,
        TypographyRole.TitleLarge => Sys.TypeScale.TitleLarge.Style,
        TypographyRole.TitleMedium => Sys.TypeScale.TitleMedium.Style,
        TypographyRole.TitleSmall => Sys.TypeScale.TitleSmall.Style,
        TypographyRole.BodyLarge => Sys.TypeScale.BodyLarge.Style,
        TypographyRole.BodyMedium => Sys.TypeScale.BodyMedium.Style,
        TypographyRole.BodySmall => Sys.TypeScale.BodySmall.Style,
        TypographyRole.LabelLarge => Sys.TypeScale.LabelLarge.Style,
        TypographyRole.LabelMedium => Sys.TypeScale.LabelMedium.Style,
        TypographyRole.LabelSmall => Sys.TypeScale.LabelSmall.Style,
        TypographyRole.DisplayLargeEmphasized => Sys.TypeScale.DisplayLargeEmphasized.Style,
        TypographyRole.DisplayMediumEmphasized => Sys.TypeScale.DisplayMediumEmphasized.Style,
        TypographyRole.DisplaySmallEmphasized => Sys.TypeScale.DisplaySmallEmphasized.Style,
        TypographyRole.HeadlineLargeEmphasized => Sys.TypeScale.HeadlineLargeEmphasized.Style,
        TypographyRole.HeadlineMediumEmphasized => Sys.TypeScale.HeadlineMediumEmphasized.Style,
        TypographyRole.HeadlineSmallEmphasized => Sys.TypeScale.HeadlineSmallEmphasized.Style,
        TypographyRole.TitleLargeEmphasized => Sys.TypeScale.TitleLargeEmphasized.Style,
        TypographyRole.TitleMediumEmphasized => Sys.TypeScale.TitleMediumEmphasized.Style,
        TypographyRole.TitleSmallEmphasized => Sys.TypeScale.TitleSmallEmphasized.Style,
        TypographyRole.BodyLargeEmphasized => Sys.TypeScale.BodyLargeEmphasized.Style,
        TypographyRole.BodyMediumEmphasized => Sys.TypeScale.BodyMediumEmphasized.Style,
        TypographyRole.BodySmallEmphasized => Sys.TypeScale.BodySmallEmphasized.Style,
        TypographyRole.LabelLargeEmphasized => Sys.TypeScale.LabelLargeEmphasized.Style,
        TypographyRole.LabelMediumEmphasized => Sys.TypeScale.LabelMediumEmphasized.Style,
        TypographyRole.LabelSmallEmphasized => Sys.TypeScale.LabelSmallEmphasized.Style,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private sealed class EmptyServices : IServiceProvider
    {
        public static readonly EmptyServices Instance = new();
        public object? GetService(Type serviceType) => null;
    }
}

/// <summary>Dynamic typography role lookup, including in setters and control templates.</summary>
public sealed class TypographyExtension : MarkupExtension
{
    public TypographyExtension() { }
    public TypographyExtension(TypographyRole role) => Role = role;
    [ConstructorArgument("role")]
    public TypographyRole Role { get; set; }

    public override BindingBase ProvideValue(IServiceProvider serviceProvider) =>
        new TokenExtension(Typography.Key(Role)).ProvideValue(serviceProvider);
}
