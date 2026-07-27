using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>
/// Typography, text-field and navigation scenes shared with the first expanded Compose capture.
/// They are intentionally one state or one type-scale group per image so diagnostics stay local.
/// </summary>
internal static class ComposeWaveOneParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Typography(
            "display-headline",
            new PixelSize(480, 320),
            VisualTheme.Light,
            [
                ("Display large", TypographyRole.DisplayLarge),
                ("Display medium", TypographyRole.DisplayMedium),
                ("Display small", TypographyRole.DisplaySmall),
                ("Headline large", TypographyRole.HeadlineLarge),
                ("Headline medium", TypographyRole.HeadlineMedium),
                ("Headline small", TypographyRole.HeadlineSmall)
            ]),
        Typography(
            "display-headline",
            new PixelSize(480, 320),
            VisualTheme.Dark,
            [
                ("Display large", TypographyRole.DisplayLarge),
                ("Display medium", TypographyRole.DisplayMedium),
                ("Display small", TypographyRole.DisplaySmall),
                ("Headline large", TypographyRole.HeadlineLarge),
                ("Headline medium", TypographyRole.HeadlineMedium),
                ("Headline small", TypographyRole.HeadlineSmall)
            ]),
        Typography(
            "title-body",
            new PixelSize(360, 184),
            VisualTheme.Light,
            [
                ("Title large", TypographyRole.TitleLarge),
                ("Title medium", TypographyRole.TitleMedium),
                ("Title small", TypographyRole.TitleSmall),
                ("Body large", TypographyRole.BodyLarge),
                ("Body medium", TypographyRole.BodyMedium),
                ("Body small", TypographyRole.BodySmall)
            ]),
        Typography(
            "title-body",
            new PixelSize(360, 184),
            VisualTheme.Dark,
            [
                ("Title large", TypographyRole.TitleLarge),
                ("Title medium", TypographyRole.TitleMedium),
                ("Title small", TypographyRole.TitleSmall),
                ("Body large", TypographyRole.BodyLarge),
                ("Body medium", TypographyRole.BodyMedium),
                ("Body small", TypographyRole.BodySmall)
            ]),
        Typography(
            "label",
            new PixelSize(320, 92),
            VisualTheme.Light,
            [
                ("Label large", TypographyRole.LabelLarge),
                ("Label medium", TypographyRole.LabelMedium),
                ("Label small", TypographyRole.LabelSmall)
            ]),
        Typography(
            "label",
            new PixelSize(320, 92),
            VisualTheme.Dark,
            [
                ("Label large", TypographyRole.LabelLarge),
                ("Label medium", TypographyRole.LabelMedium),
                ("Label small", TypographyRole.LabelSmall)
            ]),

        TextField(
            "textfield/filled/empty/light",
            VisualTheme.Light,
            TextFieldVariant.Filled),
        TextField(
            "textfield/filled/populated/light",
            VisualTheme.Light,
            TextFieldVariant.Filled,
            text: "Text"),
        TextField(
            "textfield/filled/populated/dark",
            VisualTheme.Dark,
            TextFieldVariant.Filled,
            text: "Text"),
        TextField(
            "textfield/filled/focused-empty/light",
            VisualTheme.Light,
            TextFieldVariant.Filled,
            focused: true,
            placeholder: "Placeholder"),
        TextField(
            "textfield/filled/error/light",
            VisualTheme.Light,
            TextFieldVariant.Filled,
            text: "Input",
            isError: true,
            supportingText: "Error message"),
        TextField(
            "textfield/filled/disabled/light",
            VisualTheme.Light,
            TextFieldVariant.Filled,
            text: "Text",
            enabled: false),
        TextField(
            "textfield/outlined/empty/light",
            VisualTheme.Light,
            TextFieldVariant.Outlined),
        TextField(
            "textfield/outlined/populated/dark",
            VisualTheme.Dark,
            TextFieldVariant.Outlined,
            text: "Text"),
        TextField(
            "textfield/outlined/focused-empty/light",
            VisualTheme.Light,
            TextFieldVariant.Outlined,
            focused: true,
            placeholder: "Placeholder"),
        TextField(
            "textfield/outlined/error/light",
            VisualTheme.Light,
            TextFieldVariant.Outlined,
            text: "Input",
            isError: true,
            supportingText: "Error message"),
        TextField(
            "textfield/outlined/disabled/light",
            VisualTheme.Light,
            TextFieldVariant.Outlined,
            text: "Text",
            enabled: false),

        NavigationBar(
            "navigation/bar/three-items/selected/dark",
            VisualTheme.Dark,
            selectedIndex: 0),
        NavigationBar(
            "navigation/bar/three-items/selected-second/light",
            VisualTheme.Light,
            selectedIndex: 1),
        NavigationRail(VisualTheme.Light),
        NavigationRail(VisualTheme.Dark),
        Tabs(TabVariant.Primary, VisualTheme.Light),
        Tabs(TabVariant.Primary, VisualTheme.Dark),
        Tabs(TabVariant.Secondary, VisualTheme.Light),
        Tabs(TabVariant.Secondary, VisualTheme.Dark)
    ];

    private static VisualCase Typography(
        string group,
        PixelSize viewport,
        VisualTheme theme,
        IReadOnlyList<(string Text, TypographyRole Role)> samples) =>
        new(
            $"foundation/typography/{group}/{ThemeName(theme)}",
            viewport,
            theme,
            context =>
            {
                var panel = new StackPanel
                {
                    Margin = new Thickness(16),
                    Spacing = 4
                };
                foreach (var sample in samples)
                {
                    var line = new TextBlock
                    {
                        Text = sample.Text,
                        MaxLines = 1,
                        Foreground = CaseLayout.Brush(context, SysColorToken.OnSurface)
                    };
                    TypographyScheme.SetTextStyle(line, sample.Role);
                    panel.Children.Add(line);
                }

                return panel;
            },
            RenderScaling: 1.0);

    private static VisualCase TextField(
        string id,
        VisualTheme theme,
        TextFieldVariant variant,
        string? text = null,
        bool focused = false,
        string? placeholder = null,
        bool isError = false,
        string? supportingText = null,
        bool enabled = true)
    {
        var height = supportingText is null ? 56 : 80;
        var field = new MaterialTextField
        {
            Name = "target",
            Variant = variant,
            Label = "Label",
            Text = text,
            Placeholder = placeholder,
            SupportingText = supportingText,
            IsError = isError,
            IsEnabled = enabled,
            Width = 240,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        return new VisualCase(
            id,
            new PixelSize(280, supportingText is null ? 96 : 112),
            theme,
            _ => new Grid { Children = { field } },
            RenderScaling: 1.0,
            Prepare: focused
                ? context =>
                {
                    var target = context.Require<MaterialTextField>("target");
                    var point = PointIn(target, context.Window);
                    context.Window.MouseMove(point);
                    context.Window.MouseDown(point, MouseButton.Left);
                    context.Window.MouseUp(point, MouseButton.Left);
                    context.SettleAnimations();

                    var editor = context.Require<TextBox>("PART_TextBox");
                    if (!editor.IsFocused)
                        throw new InvalidOperationException($"'{id}' did not focus its editor.");
                }
                : null);
    }

    private static VisualCase NavigationBar(
        string id,
        VisualTheme theme,
        int selectedIndex)
    {
        var navigation = CreateNavigation(
            NavigationVariant.Bar,
            selectedIndex,
            width: 360,
            height: 80);
        return ComponentScene(
            id,
            new PixelSize(360, 112),
            theme,
            navigation,
            "navigation");
    }

    private static VisualCase NavigationRail(VisualTheme theme)
    {
        var navigation = CreateNavigation(
            NavigationVariant.Rail,
            selectedIndex: 0,
            width: 96,
            height: 280);
        return ComponentScene(
            $"navigation/rail/three-items/selected/{ThemeName(theme)}",
            new PixelSize(128, 312),
            theme,
            navigation,
            "navigation");
    }

    private static VisualCase Tabs(TabVariant variant, VisualTheme theme)
    {
        var tabs = new MaterialTabs
        {
            Variant = variant,
            SelectedIndex = 0,
            Width = 360,
            Height = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        tabs.Items.Add(new MaterialTab { Content = "One" });
        tabs.Items.Add(new MaterialTab { Content = "Two" });
        tabs.Items.Add(new MaterialTab { Content = "Three" });

        return ComponentScene(
            $"navigation/tab/{variant.ToString().ToLowerInvariant()}/three-items/selected/" +
            ThemeName(theme),
            new PixelSize(360, 80),
            theme,
            tabs,
            "navigation");
    }

    private static MaterialNavigationBar CreateNavigation(
        NavigationVariant variant,
        int selectedIndex,
        double width,
        double height)
    {
        var navigation = new MaterialNavigationBar
        {
            Variant = variant,
            SelectedIndex = selectedIndex,
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        navigation.Items.Add(Destination("One"));
        navigation.Items.Add(Destination("Two"));
        navigation.Items.Add(Destination("Three"));
        return navigation;
    }

    private static MaterialNavigationItem Destination(string label) =>
        new()
        {
            Icon = new Border { Width = 24, Height = 24 },
            Content = label
        };

    private static VisualCase ComponentScene(
        string id,
        PixelSize viewport,
        VisualTheme theme,
        Control component,
        string componentTag) =>
        new(
            id,
            viewport,
            theme,
            _ => new Grid { Children = { component } },
            RenderScaling: 1.0);

    private static Point PointIn(Control target, Visual relativeTo) =>
        target.TranslatePoint(
            new Point(target.Bounds.Width / 2, target.Bounds.Height / 2),
            relativeTo)
        ?? throw new InvalidOperationException($"'{target.Name}' is not mounted.");

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}
