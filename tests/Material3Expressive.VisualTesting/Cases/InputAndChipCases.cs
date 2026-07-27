using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Text input, chips and compound button controls.</summary>
internal static class InputAndChipCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        TextFields(VisualTheme.Light),
        TextFields(VisualTheme.Dark),
        Chips(VisualTheme.Light),
        Chips(VisualTheme.Dark),
        CompoundButtons(VisualTheme.Light),
        CompoundButtons(VisualTheme.Dark)
    ];

    private static VisualCase TextFields(VisualTheme theme) =>
        new(
            $"textfield/variants-and-states/{ThemeName(theme)}",
            new PixelSize(1180, 210),
            theme,
            _ => CaseLayout.Horizontal(
                Field(TextFieldVariant.Filled, "Empty"),
                new MaterialTextField
                {
                    Variant = TextFieldVariant.Filled,
                    Label = "Email",
                    Text = "ada@example.com",
                    SupportingText = "Supporting text",
                    Width = 250
                },
                new MaterialTextField
                {
                    Variant = TextFieldVariant.Outlined,
                    Label = "Password",
                    Text = "Not secure",
                    SupportingText = "Error message",
                    IsError = true,
                    Width = 250
                },
                new MaterialTextField
                {
                    Variant = TextFieldVariant.Outlined,
                    Label = "Disabled",
                    Text = "Locked",
                    IsEnabled = false,
                    Width = 250
                }),
            ComparisonProfile.Exact,
            ["smoke", "full", "input", "textfield"]);

    private static VisualCase Chips(VisualTheme theme) =>
        new(
            $"chip/variants-and-states/{ThemeName(theme)}",
            new PixelSize(860, 190),
            theme,
            _ => CaseLayout.Horizontal(
                Chip("Assist", ChipVariant.Assist),
                Chip("Filter", ChipVariant.Filter),
                Chip("Selected", ChipVariant.Filter, selected: true),
                Chip("Input", ChipVariant.Input, trailing: true),
                Chip("Suggestion", ChipVariant.Suggestion),
                Chip("Disabled", ChipVariant.Assist, enabled: false)),
            ComparisonProfile.Exact,
            ["smoke", "full", "input", "chip"]);

    private static VisualCase CompoundButtons(VisualTheme theme) =>
        new(
            $"button-group/representative/{ThemeName(theme)}",
            new PixelSize(820, 240),
            theme,
            _ =>
            {
                var connected = new MaterialButtonGroup
                {
                    Variant = ButtonGroupVariant.Connected
                };
                connected.Children.Add(new MaterialToggleButton
                {
                    Content = "Day",
                    Variant = ButtonVariant.Tonal,
                    IsChecked = true
                });
                connected.Children.Add(new MaterialToggleButton
                {
                    Content = "Week",
                    Variant = ButtonVariant.Tonal
                });
                connected.Children.Add(new MaterialToggleButton
                {
                    Content = "Month",
                    Variant = ButtonVariant.Tonal
                });

                var standard = new MaterialButtonGroup
                {
                    Variant = ButtonGroupVariant.Standard
                };
                standard.Children.Add(new MaterialButton
                {
                    Content = "Cancel",
                    Variant = ButtonVariant.Outlined
                });
                standard.Children.Add(new MaterialButton
                {
                    Content = "Confirm",
                    Variant = ButtonVariant.Filled
                });

                return CaseLayout.Horizontal(
                    new StackPanel
                    {
                        Spacing = 18,
                        Children = { connected, standard }
                    },
                    new MaterialSplitButton
                    {
                        Content = "Save",
                        TrailingIcon = "▾"
                    },
                    new MaterialSplitButton
                    {
                        Content = "Expanded",
                        TrailingIcon = "▴",
                        IsExpanded = true
                    });
            },
            ComparisonProfile.Exact,
            ["full", "input", "button", "button-group", "split-button"]);

    private static MaterialTextField Field(TextFieldVariant variant, string label) =>
        new()
        {
            Variant = variant,
            Label = label,
            Placeholder = "Placeholder",
            Width = 250
        };

    private static MaterialChip Chip(
        string label,
        ChipVariant variant,
        bool selected = false,
        bool trailing = false,
        bool enabled = true) =>
        new()
        {
            Content = label,
            Variant = variant,
            IsChecked = selected,
            IsEnabled = enabled,
            LeadingIcon = trailing ? null : new Border { Width = 18, Height = 18 },
            TrailingIcon = trailing ? new Border { Width = 18, Height = 18 } : null
        };

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}
