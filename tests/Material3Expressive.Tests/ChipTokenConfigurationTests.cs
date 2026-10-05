using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class ChipTokenConfigurationTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void CompleteShapes_AreDefaultConfiguration_AndNullOnlyDisablesShapeMorphing(bool input)
    {
        using var clock = new MotionTestClockScope();
        SelectableChip chip = input ? new InputChip() : new FilterChip();
        chip.Content = new Border { Width = 32, Height = 16 };
        chip.LeadingIcon = new Border { Width = 18, Height = 18 };
        var window = Show(chip);
        try
        {
            Assert.Equal(new ChipShapes(new CornerRadius(12), new CornerRadius(1000), new CornerRadius(8)), chip.Shapes);
            Assert.Equal(new ChipSpacing(8, 8), chip.Spacing);
            var icon = chip.GetVisualDescendants().OfType<ChipIconSlot>().First();
            var color = chip.LeadingIconColor;
            var enter = icon.AlphaEnterAnimation; var exit = icon.AlphaExitAnimation;
            var sizeEnter = icon.SizeEnterAnimation; var sizeExit = icon.SizeExitAnimation;
            var bounds = chip.Bounds.Size;
            chip.Shapes = null; Flush(window);
            Assert.Null(chip.Shapes); Assert.Equal(new ChipSpacing(8, 8), chip.Spacing);
            Assert.Equal(color, chip.LeadingIconColor); Assert.Equal(bounds, chip.Bounds.Size);
            Assert.Equal(enter, icon.AlphaEnterAnimation); Assert.Equal(exit, icon.AlphaExitAnimation);
            Assert.Equal(sizeEnter, icon.SizeEnterAnimation); Assert.Equal(sizeExit, icon.SizeExitAnimation);
            chip.Resources[Comp.Chip.UnselectedShape] = new CornerRadius(20); Flush(window); Assert.Null(chip.Shapes);
            chip.ClearValue(SelectableChip.ShapesProperty); Flush(window); Assert.Equal(new CornerRadius(20), chip.Shapes!.Shape);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ShapeAndWholeValueConfiguration_RespectStylesAndLiveTokenDefaults()
    {
        using var clock = new MotionTestClockScope();
        var chip = new FilterChip { Content = new Border { Width = 32, Height = 16 } };
        var window = Show(chip);
        try
        {
            var configured = new ChipShapes(new CornerRadius(6), new CornerRadius(14), new CornerRadius(3));
            var style = new Style { Setters = { new Setter(SelectableChip.ShapesProperty, configured) } };
            chip.Styles.Add(style); chip.ApplyStyling(); Flush(window); Assert.Equal(configured, chip.Shapes);
            chip.Resources[Comp.Chip.SelectedShape] = new CornerRadius(22); chip.IsSelected = true; Flush(window);
            Assert.Equal(configured, chip.Shapes);
            chip.Shapes = null; Flush(window); Assert.Null(chip.Shapes);
            chip.ClearValue(SelectableChip.ShapesProperty); Flush(window); Assert.Equal(configured, chip.Shapes);
            chip.Styles.Remove(style); Flush(window); Assert.Equal(new CornerRadius(22), chip.Shapes!.SelectedShape);

            chip.Resources[Comp.Chip.FlatFilterHoverElevation] = 4d;
            chip.Resources[Comp.Chip.IconLabelSpace] = 10d; Flush(window);
            Assert.Equal(4, chip.Elevation.Hovered); Assert.Equal(new ChipSpacing(10, 10), chip.Spacing);
            var elevation = new ChipElevation(1, 2, 3, 4, 5, 6); var spacing = new ChipSpacing(3, 7);
            chip.Elevation = elevation; chip.Spacing = spacing;
            chip.Resources[Comp.Chip.FlatFilterHoverElevation] = 9d; chip.Resources[Comp.Chip.IconLabelSpace] = 12d;
            chip.IsElevated = true; Flush(window);
            Assert.Equal(elevation, chip.Elevation); Assert.Equal(spacing, chip.Spacing);
            chip.ClearValue(Chip.ElevationProperty); chip.ClearValue(Chip.SpacingProperty); Flush(window);
            Assert.Equal(3, chip.Elevation.Hovered); Assert.Equal(new ChipSpacing(12, 12), chip.Spacing);
            chip.IsElevated = false; Flush(window); Assert.Equal(9, chip.Elevation.Hovered);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ColorsAndPadding_StayEffectiveUserInputsAcrossStateChanges()
    {
        using var clock = new MotionTestClockScope();
        var chip = new FilterChip { Content = new Border { Width = 32, Height = 16 } };
        var window = Show(chip);
        try
        {
            var style = new Style
            {
                Setters =
                {
                    new Setter(Chip.BackgroundProperty, Brushes.Blue), new Setter(Chip.ForegroundProperty, Brushes.Red),
                    new Setter(Chip.LeadingIconColorProperty, Brushes.Green), new Setter(Chip.TrailingIconColorProperty, Brushes.Purple),
                    new Setter(Chip.PaddingProperty, new Thickness(11)),
                },
            };
            chip.Styles.Add(style); chip.ApplyStyling(); Flush(window);
            void Check()
            {
                Assert.Same(Brushes.Blue, chip.Background); Assert.Same(Brushes.Red, chip.Foreground);
                Assert.Same(Brushes.Green, chip.LeadingIconColor); Assert.Same(Brushes.Purple, chip.TrailingIconColor);
                Assert.Equal(new Thickness(11), chip.Padding);
            }
            Check(); chip.IsSelected = true; chip.IsElevated = true; chip.IsEnabled = false; Flush(window); Check();
            chip.Resources[Comp.Chip.DisabledContentColor] = Colors.Orange; Flush(window); Check();
            chip.Styles.Remove(style); Flush(window);
            Assert.Equal(Colors.Orange, Assert.IsAssignableFrom<ISolidColorBrush>(chip.Foreground).Color);
            Assert.Equal(Colors.Orange, Assert.IsAssignableFrom<ISolidColorBrush>(chip.LeadingIconColor).Color);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SegmentedStateAndLayout_DefaultsFollowResourcesAndPreserveOrdinaryStyles()
    {
        using var clock = new MotionTestClockScope();
        var row = new SingleChoiceSegmentedButtonRow();
        var item = new SegmentedButton { Content = new Border { Width = 24, Height = 16 }, IsSelected = true };
        row.Items.Add(item); var window = Show(row);
        try
        {
            var part = item.GetVisualDescendants().OfType<SegmentedButtonContentLayout>().Single();
            item.Resources[Comp.SegmentedButton.IconSpacing] = 12d;
            item.Resources[Comp.SegmentedButton.IconSize] = 24d; Flush(window);
            Assert.Equal(12, part.IconSpacing); Assert.Equal(24, part.IconSize);
            var width = item.Bounds.Width;
            item.Resources[Comp.SegmentedButton.IconSpacing] = 20d; Flush(window);
            Assert.Equal(width + 8, item.Bounds.Width);
            var style = new Style { Setters = { new Setter(SegmentedButton.BackgroundProperty, Brushes.Green), new Setter(SegmentedButton.CornerRadiusProperty, new CornerRadius(7)) } };
            item.Styles.Add(style); item.ApplyStyling(); item.IsEnabled = false; row.BaseShape = new CornerRadius(20); Flush(window);
            Assert.Same(Brushes.Green, item.Background); Assert.Equal(new CornerRadius(7), item.CornerRadius);
            item.Resources[Comp.SegmentedButton.DisabledActiveContainerColor] = Colors.Red; Flush(window);
            Assert.Same(Brushes.Green, item.Background);
            item.Styles.Remove(style); Flush(window);
            Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(item.Background).Color);
            Assert.Equal(new CornerRadius(20), item.CornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SegmentedSelectionFacts_DoNotRequireTheDefaultIconTemplate()
    {
        using var clock = new MotionTestClockScope();
        var item = new SegmentedButton
        {
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<SegmentedButton>((_, _) => new Border()),
        };
        item.Resources[Comp.SegmentedButton.ActiveContainerColor] = Colors.Blue;
        item.Resources[Comp.SegmentedButton.DisabledActiveContainerColor] = Colors.Red;
        item.Resources[Comp.SegmentedButton.DisabledInactiveContainerColor] = Colors.Green;
        var window = Show(item);
        try
        {
            item.IsSelected = true; Flush(window);
            Assert.Equal(Colors.Blue, Assert.IsAssignableFrom<ISolidColorBrush>(item.Background).Color);
            item.IsEnabled = false; Flush(window);
            Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(item.Background).Color);
            item.IsSelected = false; Flush(window);
            Assert.Equal(Colors.Green, Assert.IsAssignableFrom<ISolidColorBrush>(item.Background).Color);
        }
        finally { window.Close(); }
    }

    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 600, Height = 300 };
        window.Styles.Add(new MaterialTheme());
        window.Content = new Grid { Children = { content } };
        window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
