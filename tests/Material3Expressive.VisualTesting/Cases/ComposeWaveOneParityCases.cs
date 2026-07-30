// Implementation-source basis: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/{ListItem,ListItemDefaults}.kt
// and Scaffold.kt, plus tokens/ListTokens.kt (VERSION 29.0.0). The matching Compose scenes use the pinned
// Material3 1.5.0-alpha22 executable interaction overloads at explicit 56/72/88dp sizes.
// Alpha22 has no alpha25 non-interactive ListItem(content) overload, and alpha25's text-line-height
// minimum-height behavior is intentionally parity unavailable here. Scaffold geometry is executable
// against alpha22; alpha25's additional explicit slot z-indices are locked structurally.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
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
        WideNavigationRail(expanded: false, theme: VisualTheme.Light),
        WideNavigationRail(expanded: true, theme: VisualTheme.Light),
        WideNavigationRail(expanded: false, theme: VisualTheme.Dark),
        WideNavigationRail(expanded: true, theme: VisualTheme.Dark),
        WideNavigationRail(expanded: false, theme: VisualTheme.Light, enabled: false),
        Tabs(TabVariant.Primary, VisualTheme.Light),
        Tabs(TabVariant.Primary, VisualTheme.Dark),
        Tabs(TabVariant.Secondary, VisualTheme.Light),
        Tabs(TabVariant.Secondary, VisualTheme.Dark),
        ScrollablePrimaryTabs(),
        ScrollablePrimaryTabsAllItems(),
        ListItemClickEnabled(),
        ListItemClickDisabled(),
        ListItemSingleSelectionSelectedDisabled(),
        ListItemClickDragged(),
        ListItemClickDisabledDragged(),
        ListItemClickTwoLineSlots(),
        ListItemClickTwoLineStringSlots(),
        ListItemClickThreeLineOverlineSupporting(),
        ListItemSingleSelectionSelected(VisualTheme.Light),
        ListItemSingleSelectionSelected(VisualTheme.Dark),
        ListItemMultiSelectionSelected(),
        SegmentedListThreeItemsSelectedMiddle(),
        SegmentedListSingleDark(),
        ScaffoldBarsFabSnackbar(),
        ScaffoldEndOverlayRtlDark()
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

    private static VisualCase WideNavigationRail(
        bool expanded,
        VisualTheme theme,
        bool enabled = true)
    {
        var state = enabled ? "selected" : "disabled";
        return ComponentScene(
            $"navigation/wide-rail/{(expanded ? "expanded" : "collapsed")}/three-items/" +
            $"{state}/{ThemeName(theme)}",
            new PixelSize(expanded ? 252 : 128, 312),
            theme,
            _ => CreateWideNavigationRail(expanded, enabled));
    }

    private static MaterialWideNavigationRail CreateWideNavigationRail(bool expanded, bool enabled)
    {
        var rail = new MaterialWideNavigationRail
        {
            Name = "component",
            IsExpanded = expanded,
            SelectedIndex = 0,
            Width = expanded ? 220 : 96,
            Height = 280,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var (label, index) in new[] { "One", "Two", "Three" }.Select((label, index) => (label, index)))
        {
            rail.Items.Add(
                new MaterialNavigationItem
                {
                    IsEnabled = enabled,
                    Icon = new Border { Width = 24, Height = 24 },
                    Content = new TextBlock { Name = $"label-{index}", Text = label }
                });
        }
        return rail;
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

    private static VisualCase ScrollablePrimaryTabs()
    {
        return ComponentScene(
            "navigation/tab/primary/scrollable/selected-last/light",
            new PixelSize(240, 80),
            VisualTheme.Light,
            _ => CreateScrollablePrimaryTabs(240),
            prepare: context => context.SettleAnimations());
    }

    private static VisualCase ScrollablePrimaryTabsAllItems()
    {
        return ComponentScene(
            "navigation/tab/primary/scrollable/all-items/selected-last/light",
            new PixelSize(600, 80),
            VisualTheme.Light,
            _ => CreateScrollablePrimaryTabs(600),
            prepare: context => context.SettleAnimations());
    }

    private static MaterialTabs CreateScrollablePrimaryTabs(double width)
    {
        var tabs = new MaterialTabs
        {
            Name = "component",
            Variant = TabVariant.Primary,
            IsScrollable = true,
            SelectedIndex = 3,
            Width = width,
            Height = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        tabs.Items.Add(new MaterialTab { Name = "tab-0", Content = new TextBlock { Name = "label-0", Text = "Overview" } });
        tabs.Items.Add(new MaterialTab { Name = "tab-1", Content = new TextBlock { Name = "label-1", Text = "Specifications" } });
        tabs.Items.Add(new MaterialTab { Name = "tab-2", Content = new TextBlock { Name = "label-2", Text = "Reviews" } });
        tabs.Items.Add(new MaterialTab { Name = "tab-3", Content = new TextBlock { Name = "label-3", Text = "Related items" } });
        return tabs;
    }

    private static VisualCase ListItemClickEnabled() => ListItemScene(
        "listitem/click/enabled/light",
        VisualTheme.Light,
        headline: "Settings",
        interactionMode: ListItemInteractionMode.Click);

    private static VisualCase ListItemClickDisabled() => ListItemScene(
        "listitem/click/disabled/light",
        VisualTheme.Light,
        headline: "Offline",
        interactionMode: ListItemInteractionMode.Click,
        enabled: false);

    private static VisualCase ListItemSingleSelectionSelectedDisabled() => ListItemScene(
        "listitem/single-selection/selected-disabled/light",
        VisualTheme.Light,
        headline: "Default network",
        interactionMode: ListItemInteractionMode.SingleSelection,
        enabled: false,
        selected: true);

    private static VisualCase ListItemClickDragged() => ListItemScene(
        "listitem/click/dragged/light",
        VisualTheme.Light,
        headline: "Reorder item",
        interactionMode: ListItemInteractionMode.Click,
        dragged: true);

    private static VisualCase ListItemClickDisabledDragged() => ListItemScene(
        "listitem/click/disabled-dragged/light",
        VisualTheme.Light,
        headline: "Reorder item",
        interactionMode: ListItemInteractionMode.Click,
        enabled: false,
        dragged: true);

    private static VisualCase ListItemClickTwoLineSlots() => ListItemScene(
        "listitem/click/two-line/slots/light",
        VisualTheme.Light,
        height: 72,
        headline: "Network",
        interactionMode: ListItemInteractionMode.Click,
        leading: true,
        trailing: "On",
        supporting: "Wi-Fi, mobile data");

    // AndroidX alpha22's ListItem takes composable slot lambdas, so the Compose scene has the
    // same Text nodes as the direct-TextBlock case. This Avalonia-specific construction path
    // exercises ContentPresenter-generated TextBlocks for string Content/Supporting/Trailing
    // values. StyledElement.Name is immutable after styling, so Prepare instead verifies every
    // generated text control is uniquely resolvable; the parity adapter resolves these declared
    // string slots by that same exact contract text rather than by visual-tree order.
    private static VisualCase ListItemClickTwoLineStringSlots() => ComponentScene(
        "listitem/click/two-line/string-slots/light",
        new PixelSize(360, 96),
        VisualTheme.Light,
        _ => new MaterialListItem
        {
            Name = "component",
            Width = 360,
            Height = 72,
            InteractionMode = ListItemInteractionMode.Click,
            LeadingContent = new Border { Name = "leading", Width = 24, Height = 24 },
            TrailingContent = "On",
            SupportingContent = "Wi-Fi, mobile data",
            Content = "Network",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        },
        prepare: context =>
        {
            var item = context.Require<MaterialListItem>("component");
            _ = RequireGeneratedListItemText(item, "Network");
            _ = RequireGeneratedListItemText(item, "Wi-Fi, mobile data");
            _ = RequireGeneratedListItemText(item, "On");
        });

    private static VisualCase ListItemClickThreeLineOverlineSupporting() => ListItemScene(
        "listitem/click/three-line/overline-supporting/light",
        VisualTheme.Light,
        height: 88,
        headline: "Cloud backup",
        interactionMode: ListItemInteractionMode.Click,
        overline: "Storage",
        supporting: "Back up photos and videos");

    private static VisualCase ListItemSingleSelectionSelected(VisualTheme theme) => ListItemScene(
        $"listitem/single-selection/selected/{ThemeName(theme)}",
        theme,
        headline: "Default network",
        interactionMode: ListItemInteractionMode.SingleSelection,
        selected: true);

    private static VisualCase ListItemMultiSelectionSelected() => ListItemScene(
        "listitem/multi-selection/selected/light",
        VisualTheme.Light,
        headline: "Sync over Wi-Fi",
        interactionMode: ListItemInteractionMode.MultiSelection,
        selected: true);

    private static VisualCase SegmentedListThreeItemsSelectedMiddle() =>
        SegmentedListScene(
            "listitem/segmented/three-items/selected-middle/light",
            VisualTheme.Light,
            ["Account", "Notifications", "Privacy"],
            selectedIndex: 1);

    private static VisualCase SegmentedListSingleDark() =>
        SegmentedListScene(
            "listitem/segmented/single-selected/dark",
            VisualTheme.Dark,
            ["Appearance"],
            selectedIndex: 0);

    private static VisualCase ScaffoldBarsFabSnackbar() =>
        ScaffoldScene(
            "scaffold/bars-fab-snackbar/light",
            VisualTheme.Light,
            ScaffoldFabPosition.End,
            showTopBar: true,
            showSnackbar: true,
            rightToLeft: false);

    private static VisualCase ScaffoldEndOverlayRtlDark() =>
        ScaffoldScene(
            "scaffold/end-overlay/rtl/dark",
            VisualTheme.Dark,
            ScaffoldFabPosition.EndOverlay,
            showTopBar: false,
            showSnackbar: false,
            rightToLeft: true);

    private static VisualCase ScaffoldScene(
        string id,
        VisualTheme theme,
        ScaffoldFabPosition fabPosition,
        bool showTopBar,
        bool showSnackbar,
        bool rightToLeft) =>
        new(
            id,
            new PixelSize(360, 400),
            theme,
            context =>
            {
                var paddedBody = new Border
                {
                    Name = "padded-body",
                    Background = CaseLayout.Brush(context, SysColorToken.SurfaceVariant)
                };
                var body = new Border
                {
                    Name = "body",
                    Background = CaseLayout.Brush(context, SysColorToken.Surface),
                    Child = paddedBody
                };
                var scaffold = new MaterialScaffold
                {
                    Name = "component",
                    Width = 360,
                    Height = 400,
                    FlowDirection = rightToLeft
                        ? FlowDirection.RightToLeft
                        : FlowDirection.LeftToRight,
                    ContentWindowInsets = new Thickness(10, 12, 14, 16),
                    FloatingActionButtonPosition = fabPosition,
                    Content = body,
                    TopBar = showTopBar
                        ? Slot("top-bar", 360, 48, CaseLayout.Brush(context, SysColorToken.PrimaryContainer))
                        : null,
                    BottomBar = Slot(
                        "bottom-bar",
                        360,
                        80,
                        CaseLayout.Brush(context, SysColorToken.SecondaryContainer)),
                    SnackbarHost = showSnackbar
                        ? Slot("snackbar", 240, 48, CaseLayout.Brush(context, SysColorToken.InverseSurface))
                        : null,
                    FloatingActionButton = Slot(
                        "fab",
                        56,
                        56,
                        CaseLayout.Brush(context, SysColorToken.TertiaryContainer))
                };
                paddedBody.Bind(
                    Layoutable.MarginProperty,
                    new Binding(nameof(MaterialScaffold.ContentPadding))
                    {
                        Source = scaffold
                    });

                return scaffold;
            },
            RenderScaling: 1.0);

    private static Border Slot(string name, double width, double height, IBrush background) =>
        new()
        {
            Name = name,
            Width = width,
            Height = height,
            Background = background
        };

    private static VisualCase SegmentedListScene(
        string id,
        VisualTheme theme,
        IReadOnlyList<string> labels,
        int? selectedIndex = null)
    {
        var height = labels.Count * 56 + (labels.Count - 1) * 2;
        return ComponentScene(
            id,
            new PixelSize(360, height + 24),
            theme,
            _ =>
            {
                var list = new MaterialSegmentedList
                {
                    Name = "component",
                    Width = 360,
                    Height = height,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                for (var index = 0; index < labels.Count; index++)
                {
                    var selected = index == selectedIndex;
                    list.Children.Add(new MaterialListItem
                    {
                        Name = $"item-{index}",
                        Height = 56,
                        InteractionMode = selected
                            ? ListItemInteractionMode.SingleSelection
                            : ListItemInteractionMode.Click,
                        IsSelected = selected,
                        Content = new TextBlock
                        {
                            Name = $"headline-{index}",
                            Text = labels[index]
                        }
                    });
                }

                return list;
            });
    }

    private static VisualCase ListItemScene(
        string id,
        VisualTheme theme,
        string headline,
        int height = 56,
        ListItemInteractionMode interactionMode = ListItemInteractionMode.None,
        bool enabled = true,
        bool selected = false,
        bool dragged = false,
        bool leading = false,
        string? trailing = null,
        string? overline = null,
        string? supporting = null) => ComponentScene(
        id,
        new PixelSize(360, height + 24),
        theme,
        _ => new MaterialListItem
        {
            Name = "component",
            Width = 360,
            Height = height,
            IsEnabled = enabled,
            InteractionMode = interactionMode,
            IsSelected = selected,
            IsDragged = dragged,
            LeadingContent = leading ? new Border { Name = "leading", Width = 24, Height = 24 } : null,
            TrailingContent = trailing is null ? null : new TextBlock { Name = "trailing", Text = trailing },
            OverlineContent = overline is null ? null : new TextBlock { Name = "overline", Text = overline },
            SupportingContent = supporting is null ? null : new TextBlock { Name = "supporting", Text = supporting },
            Content = new TextBlock { Name = "headline", Text = headline },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        },
        prepare: dragged ? context => context.SettleAnimations() : null);

    private static TextBlock RequireGeneratedListItemText(MaterialListItem item, string text)
    {
        var matches = item.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(candidate => string.Equals(candidate.Text, text, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected one generated ListItem TextBlock for '{text}', found {matches.Length}.");
        }

        return matches[0];
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
