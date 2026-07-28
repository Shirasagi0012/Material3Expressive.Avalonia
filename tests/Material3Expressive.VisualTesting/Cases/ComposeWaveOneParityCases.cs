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
            "textfield/outlined/populated/light",
            VisualTheme.Light,
            TextFieldVariant.Outlined,
            text: "Text"),
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
            "navigation/bar/three-items/selected/light",
            VisualTheme.Light,
            selectedIndex: 0),
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
                    Name = "component",
                    Margin = new Thickness(16),
                    Spacing = 4
                };
                foreach (var sample in samples)
                {
                    var line = new TextBlock
                    {
                        Name = TypographyNodeId(sample.Role),
                        Text = sample.Text,
                        MaxLines = 1,
                        HorizontalAlignment = HorizontalAlignment.Left,
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
        // Compose's outlined field includes an 8dp label-cutout overhang above its visible
        // 56dp container. Keep the parity scene's outer bounds aligned with that contract.
        var height = supportingText is null && variant == TextFieldVariant.Outlined ? 64 :
            supportingText is null ? 56 :
            variant == TextFieldVariant.Outlined ? 84 : 80;

        return new VisualCase(
            id,
            new PixelSize(280, supportingText is null ? 96 : 112),
            theme,
            _ => new Grid
            {
                Children =
                {
                    new MaterialTextField
                    {
                        Name = "component",
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
                    }
                }
            },
            RenderScaling: 1.0,
            Prepare: context =>
            {
                var target = context.Require<MaterialTextField>("component");
                var editor = context.Require<TextBox>("PART_TextBox");
                // Match Compose's test-only LocalCursorBlinkEnabled=false contract: keep the
                // focused caret visibly on, rather than sampling either toolkit's wall-clock
                // blink phase. This does not hide the caret or synthesize focus.
                editor.CaretBlinkInterval = TimeSpan.Zero;
                if (focused)
                {
                    var point = PointIn(target, context.Window);
                    context.Window.MouseMove(point);
                    context.Window.MouseDown(point, MouseButton.Left);
                    context.Window.MouseUp(point, MouseButton.Left);
                }

                if (editor.IsFocused != focused)
                {
                    throw new InvalidOperationException(
                        $"'{id}' editor focus state is {editor.IsFocused}, expected {focused}.");
                }

                // The interaction above is real input. Settling only moves the control-owned
                // motion to its resulting semantic state, matching Compose's terminal frame.
                context.SettleAnimations();
            });
    }

    private static VisualCase NavigationBar(
        string id,
        VisualTheme theme,
        int selectedIndex)
    {
        return ComponentScene(
            id,
            new PixelSize(360, 112),
            theme,
            _ => CreateNavigation(
                NavigationVariant.Bar,
                selectedIndex,
                width: 360,
                height: 80));
    }

    private static VisualCase NavigationRail(VisualTheme theme)
    {
        return ComponentScene(
            $"navigation/rail/three-items/selected/{ThemeName(theme)}",
            new PixelSize(128, 312),
            theme,
            _ => CreateNavigation(
                NavigationVariant.Rail,
                selectedIndex: 0,
                width: 96,
                height: 280));
    }

    private static VisualCase Tabs(TabVariant variant, VisualTheme theme)
    {
        return ComponentScene(
            $"navigation/tab/{variant.ToString().ToLowerInvariant()}/three-items/selected/" +
            ThemeName(theme),
            new PixelSize(360, 80),
            theme,
            _ => CreateTabs(variant),
            prepare: context => context.SettleAnimations());
    }

    private static MaterialTabs CreateTabs(TabVariant variant)
    {
        var tabs = new MaterialTabs
        {
            Name = "component",
            Variant = variant,
            SelectedIndex = 0,
            Width = 360,
            Height = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        tabs.Items.Add(new MaterialTab { Content = new TextBlock { Name = "label-0", Text = "One" } });
        tabs.Items.Add(new MaterialTab { Content = new TextBlock { Name = "label-1", Text = "Two" } });
        tabs.Items.Add(new MaterialTab { Content = new TextBlock { Name = "label-2", Text = "Three" } });
        return tabs;
    }

    private static MaterialNavigationBar CreateNavigation(
        NavigationVariant variant,
        int selectedIndex,
        double width,
        double height)
    {
        var navigation = new MaterialNavigationBar
        {
            Name = "component",
            Variant = variant,
            SelectedIndex = selectedIndex,
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        navigation.Items.Add(Destination("One", "label-0"));
        navigation.Items.Add(Destination("Two", "label-1"));
        navigation.Items.Add(Destination("Three", "label-2"));
        return navigation;
    }

    private static MaterialNavigationItem Destination(string label, string nodeId) =>
        new()
        {
            Icon = new Border { Width = 24, Height = 24 },
            Content = new TextBlock { Name = nodeId, Text = label }
        };

    private static VisualCase ComponentScene(
        string id,
        PixelSize viewport,
        VisualTheme theme,
        Func<VisualCaseContext, Control> createComponent,
        Action<VisualContext>? prepare = null) =>
        new(
            id,
            viewport,
            theme,
            context => new Grid { Children = { createComponent(context) } },
            RenderScaling: 1.0,
            Prepare: prepare);

    private static Point PointIn(Control target, Visual relativeTo) =>
        target.TranslatePoint(
            new Point(target.Bounds.Width / 2, target.Bounds.Height / 2),
            relativeTo)
        ?? throw new InvalidOperationException($"'{target.Name}' is not mounted.");

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";

    private static string TypographyNodeId(TypographyRole role) => role switch
    {
        TypographyRole.DisplayLarge => "display-large",
        TypographyRole.DisplayMedium => "display-medium",
        TypographyRole.DisplaySmall => "display-small",
        TypographyRole.HeadlineLarge => "headline-large",
        TypographyRole.HeadlineMedium => "headline-medium",
        TypographyRole.HeadlineSmall => "headline-small",
        TypographyRole.TitleLarge => "title-large",
        TypographyRole.TitleMedium => "title-medium",
        TypographyRole.TitleSmall => "title-small",
        TypographyRole.BodyLarge => "body-large",
        TypographyRole.BodyMedium => "body-medium",
        TypographyRole.BodySmall => "body-small",
        TypographyRole.LabelLarge => "label-large",
        TypographyRole.LabelMedium => "label-medium",
        TypographyRole.LabelSmall => "label-small",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}
