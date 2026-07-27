using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Persistent selection states and time-independent progress scenes.</summary>
internal static class SelectionAndProgressCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        CheckboxStates(VisualTheme.Light),
        CheckboxStates(VisualTheme.Dark),
        RadioAndSwitchStates(VisualTheme.Light),
        RadioAndSwitchStates(VisualTheme.Dark),
        SliderStates(VisualTheme.Light),
        SliderStates(VisualTheme.Dark),
        ProgressStates(VisualTheme.Light),
        ProgressStates(VisualTheme.Dark)
    ];

    private static VisualCase CheckboxStates(VisualTheme theme) =>
        new(
            $"selection/checkbox/all-states/{ThemeName(theme)}",
            new PixelSize(760, 190),
            theme,
            _ => CaseLayout.Horizontal(
                Checkbox("Unchecked", false),
                Checkbox("Checked", true),
                new MaterialCheckbox
                {
                    Content = "Indeterminate",
                    IsThreeState = true,
                    IsChecked = null
                },
                new MaterialCheckbox
                {
                    Content = "Error",
                    IsError = true
                },
                new MaterialCheckbox
                {
                    Content = "Disabled",
                    IsChecked = true,
                    IsEnabled = false
                }),
            ComparisonProfile.Exact,
            ["full", "selection", "checkbox"]);

    private static VisualCase RadioAndSwitchStates(VisualTheme theme) =>
        new(
            $"selection/radio-switch/all-states/{ThemeName(theme)}",
            new PixelSize(820, 260),
            theme,
            _ =>
            {
                var radios = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new MaterialRadioButton
                        {
                            GroupName = "visual-radio",
                            Content = "Selected",
                            IsChecked = true
                        },
                        new MaterialRadioButton
                        {
                            GroupName = "visual-radio",
                            Content = "Unselected"
                        },
                        new MaterialRadioButton
                        {
                            Content = "Disabled",
                            IsEnabled = false
                        }
                    }
                };
                var switches = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new MaterialSwitch { Content = "Off" },
                        new MaterialSwitch { Content = "On", IsChecked = true },
                        new MaterialSwitch
                        {
                            Content = "Disabled off",
                            IsEnabled = false
                        },
                        new MaterialSwitch
                        {
                            Content = "Disabled on",
                            IsChecked = true,
                            IsEnabled = false
                        }
                    }
                };
                return CaseLayout.Horizontal(radios, switches);
            },
            ComparisonProfile.Exact,
            ["smoke", "full", "selection", "radio", "switch"]);

    private static VisualCase SliderStates(VisualTheme theme) =>
        new(
            $"selection/slider/representative/{ThemeName(theme)}",
            new PixelSize(520, 250),
            theme,
            context =>
            {
                var panel = Vertical();
                panel.Children.Add(CaseLayout.Label("25%", context));
                panel.Children.Add(new MaterialSlider
                {
                    Width = 380,
                    Maximum = 1,
                    Value = 0.25
                });
                panel.Children.Add(CaseLayout.Label("75%", context));
                panel.Children.Add(new MaterialSlider
                {
                    Width = 380,
                    Maximum = 1,
                    Value = 0.75
                });
                panel.Children.Add(CaseLayout.Label("Disabled", context));
                panel.Children.Add(new MaterialSlider
                {
                    Width = 380,
                    Maximum = 1,
                    Value = 0.5,
                    IsEnabled = false
                });
                return panel;
            },
            ComparisonProfile.Exact,
            ["full", "selection", "slider"]);

    private static VisualCase ProgressStates(VisualTheme theme) =>
        new(
            $"progress/determinate/representative/{ThemeName(theme)}",
            new PixelSize(800, 280),
            theme,
            context =>
            {
                var linear = Vertical();
                linear.Width = 420;
                linear.Children.Add(CaseLayout.Label("Linear 35%", context));
                linear.Children.Add(new MaterialLinearProgressIndicator
                {
                    Width = 400,
                    Value = 0.35
                });
                linear.Children.Add(CaseLayout.Label("Wavy 70%", context));
                linear.Children.Add(new MaterialLinearProgressIndicator
                {
                    Width = 400,
                    Value = 0.7,
                    IsWavy = true
                });

                var circular = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 22,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new MaterialCircularProgressIndicator { Value = 0.35 },
                        new MaterialCircularProgressIndicator { Value = 0.75 },
                        new MaterialLoadingIndicator
                        {
                            Value = 0.25,
                            IsIndeterminate = false
                        },
                        new MaterialLoadingIndicator
                        {
                            Variant = LoadingIndicatorVariant.Contained,
                            Value = 0.75,
                            IsIndeterminate = false
                        }
                    }
                };

                return CaseLayout.Horizontal(linear, circular);
            },
            ComparisonProfile.Exact,
            ["smoke", "full", "progress", "loading-indicator"]);

    private static MaterialCheckbox Checkbox(string text, bool value) =>
        new()
        {
            Content = text,
            IsChecked = value
        };

    private static StackPanel Vertical() =>
        new()
        {
            Spacing = 8,
            Margin = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}
