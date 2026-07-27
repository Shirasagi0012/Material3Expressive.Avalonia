using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Time-independent overlay, search, picker and carousel scenes.</summary>
internal static class OverlayAndPickerCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        BadgesTooltipsSnackbar(VisualTheme.Light),
        BadgesTooltipsSnackbar(VisualTheme.Dark),
        DialogAndSheet(VisualTheme.Light),
        DialogAndSheet(VisualTheme.Dark),
        Search(VisualTheme.Light),
        Search(VisualTheme.Dark),
        Pickers(VisualTheme.Light),
        Pickers(VisualTheme.Dark),
        Carousels(VisualTheme.Light),
        Carousels(VisualTheme.Dark)
    ];

    private static VisualCase BadgesTooltipsSnackbar(VisualTheme theme) =>
        new(
            $"overlay/badge-tooltip-snackbar/representative/{ThemeName(theme)}",
            new PixelSize(900, 270),
            theme,
            _ => CaseLayout.Horizontal(
                new StackPanel
                {
                    Spacing = 18,
                    Children =
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 18,
                            Children =
                            {
                                new MaterialBadge { Variant = BadgeVariant.Small },
                                new MaterialBadge { Count = 3 },
                                new MaterialBadge { Count = 128 },
                                new MaterialBadge { Count = "999+" }
                            }
                        },
                        new MaterialSnackbar
                        {
                            IsOpen = true,
                            Duration = TimeSpan.Zero,
                            Message = "Conversation archived",
                            Action = "Undo"
                        }
                    }
                },
                new StackPanel
                {
                    Spacing = 14,
                    Children =
                    {
                        new MaterialTooltip
                        {
                            Style = TooltipVariant.Plain,
                            Content = "Plain tooltip"
                        },
                        new MaterialTooltip
                        {
                            Style = TooltipVariant.Rich,
                            Title = "Rich tooltip",
                            Content = "Supporting explanation"
                        }
                    }
                }),
            ComparisonProfile.Shadow,
            ["smoke", "full", "overlay", "badge", "tooltip", "snackbar"]);

    private static VisualCase DialogAndSheet(VisualTheme theme) =>
        new(
            $"overlay/dialog-sheet/open/{ThemeName(theme)}",
            new PixelSize(980, 420),
            theme,
            context => CaseLayout.Horizontal(
                DialogHost(context),
                SheetHost(context)),
            ComparisonProfile.Shadow,
            ["full", "overlay", "dialog", "bottom-sheet"]);

    private static VisualCase Search(VisualTheme theme) =>
        new(
            $"search/closed-open/{ThemeName(theme)}",
            new PixelSize(900, 280),
            theme,
            context => CaseLayout.Horizontal(
                SearchBar(isOpen: false, context),
                SearchBar(isOpen: true, context)),
            ComparisonProfile.Shadow,
            ["full", "input", "search"]);

    private static VisualCase Pickers(VisualTheme theme) =>
        new(
            $"picker/date-time/fixed-value/{ThemeName(theme)}",
            new PixelSize(840, 500),
            theme,
            _ => CaseLayout.Horizontal(
                new MaterialDatePicker
                {
                    DisplayMonth = new DateTime(2026, 7, 1),
                    SelectedDate = new DateTime(2026, 7, 27),
                    Headline = "Select date"
                },
                new MaterialTimePicker
                {
                    Time = new TimeSpan(10, 10, 0),
                    Field = TimePickerField.Hour
                }),
            ComparisonProfile.Exact,
            ["full", "picker", "date-picker", "time-picker"]);

    private static VisualCase Carousels(VisualTheme theme) =>
        new(
            $"carousel/all-variants/fixed-offset/{ThemeName(theme)}",
            new PixelSize(900, 430),
            theme,
            context => new StackPanel
            {
                Width = 760,
                Spacing = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    Carousel(CarouselVariant.MultiBrowse, context),
                    Carousel(CarouselVariant.Hero, context)
                }
            },
            ComparisonProfile.Exact,
            ["full", "carousel"]);

    private static Border DialogHost(VisualCaseContext context) =>
        Host(
            new MaterialDialog
            {
                IsOpen = true,
                Content = new StackPanel
                {
                    Spacing = 14,
                    Children =
                    {
                        CaseLayout.Label("Delete this draft?", context, 22),
                        CaseLayout.Label(
                            "The draft and its contents will be removed.",
                            context,
                            14),
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Children =
                            {
                                new MaterialButton
                                {
                                    Content = "Cancel",
                                    Variant = ButtonVariant.Text
                                },
                                new MaterialButton
                                {
                                    Content = "Delete",
                                    Variant = ButtonVariant.Filled
                                }
                            }
                        }
                    }
                }
            });

    private static Border SheetHost(VisualCaseContext context) =>
        Host(
            new MaterialBottomSheet
            {
                IsOpen = true,
                Content = new StackPanel
                {
                    Margin = new Thickness(24, 0, 24, 28),
                    Spacing = 8,
                    Children =
                    {
                        CaseLayout.Label("Share", context, 22),
                        CaseLayout.Label(
                            "Choose where this item should be sent.",
                            context,
                            14)
                    }
                }
            });

    private static Border Host(Control overlay) =>
        new()
        {
            Width = 420,
            Height = 330,
            ClipToBounds = true,
            Child = new Grid
            {
                Children =
                {
                    new Border
                    {
                        Padding = new Thickness(20),
                        Child = new TextBlock { Text = "Background content" }
                    },
                    overlay
                }
            }
        };

    private static MaterialSearchBar SearchBar(bool isOpen, VisualCaseContext context) =>
        new()
        {
            Width = 360,
            Text = isOpen ? "quarterly" : null,
            Placeholder = "Search mail",
            LeadingIcon = new Border { Width = 24, Height = 24 },
            TrailingIcon = new Border { Width = 24, Height = 24 },
            IsOpen = isOpen,
            Results = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    CaseLayout.Label("Quarterly report", context, 14),
                    CaseLayout.Label("Quarterly planning", context, 14)
                }
            }
        };

    private static MaterialCarousel Carousel(
        CarouselVariant variant,
        VisualCaseContext context)
    {
        var carousel = new MaterialCarousel
        {
            Variant = variant,
            Offset = 0,
            Height = 160
        };
        carousel.Children.Add(Item(context, SysColorToken.PrimaryContainer));
        carousel.Children.Add(Item(context, SysColorToken.SecondaryContainer));
        carousel.Children.Add(Item(context, SysColorToken.TertiaryContainer));
        carousel.Children.Add(Item(context, SysColorToken.PrimaryContainer));
        return carousel;
    }

    private static Border Item(VisualCaseContext context, SysColorToken role) =>
        new()
        {
            Background = CaseLayout.Brush(context, role),
            CornerRadius = new CornerRadius(28)
        };

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";
}
