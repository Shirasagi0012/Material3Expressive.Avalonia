using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class NavigationRailHitTestingTests
{
    [AvaloniaTheory]
    [InlineData("regular", false)]
    [InlineData("regular", true)]
    [InlineData("wide-collapsed", false)]
    [InlineData("wide-collapsed", true)]
    [InlineData("wide-expanded", false)]
    [InlineData("wide-expanded", true)]
    public void WholeItemRectangle_ReceivesHoverAndGapActivation(string variant, bool selected)
    {
        using var clock = new MotionTestClockScope();
        var icon = new Border { Width = 24, Height = 24 };
        var label = new Border { Width = 32, Height = 16 };
        Button item = variant == "regular"
            ? new NavigationRailItem { Icon = icon, Label = label, IsSelected = selected }
            : new WideNavigationRailItem { Icon = icon, Label = label, IsSelected = selected, RailExpanded = variant == "wide-expanded" };
        item.HorizontalAlignment = HorizontalAlignment.Left;
        item.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 320, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Border { Padding = new Thickness(16), Child = item };
        window.Show(); window.UpdateLayout();
        var clicks = 0; item.Click += (_, _) => clicks++;
        try
        {
            var ripple = item.GetVisualDescendants().OfType<MaterialSurface>().Single(control => control.Name == "PART_IndicatorRipple");
            var labelOrigin = label.TranslatePoint(default, item)!.Value;
            var gap = variant == "wide-expanded"
                ? new Point(labelOrigin.X - 4, labelOrigin.Y + label.Bounds.Height / 2)
                : new Point(item.Bounds.Width / 2, labelOrigin.Y - 2);
            var controller = MaterialInteraction.GetOrCreate(item);
            void Move(Point local) => window.MouseMove(item.TranslatePoint(local, window)!.Value);

            // Enter the empty gap directly, then cross painted content and empty edge padding.
            // PointerEntered is not raised manually: rendering and real input must hit the template.
            foreach (var point in new[] { gap, ripple.TranslatePoint(new Rect(ripple.Bounds.Size).Center, item)!.Value,
                         label.TranslatePoint(new Rect(label.Bounds.Size).Center, item)!.Value, new Point(1, 1), gap })
            {
                Move(point);
                Assert.True(item.IsPointerOver, $"{variant}: {point}");
                Assert.True(controller.Interaction.HasFlag(InteractionState.Hovered));
            }

            Move(new Point(-1, item.Bounds.Height / 2));
            Assert.False(item.IsPointerOver);
            Assert.False(controller.Interaction.HasFlag(InteractionState.Hovered));

            var position = item.TranslatePoint(gap, window)!.Value;
            window.MouseDown(position, MouseButton.Left); window.MouseUp(position, MouseButton.Left);
            Assert.Equal(1, clicks);
            Assert.Equal(selected, item is NavigationRailItem regular ? regular.IsSelected : ((WideNavigationRailItem)item).IsSelected);

            item.IsEnabled = false;
            Move(new Point(-1, item.Bounds.Height / 2)); Move(gap);
            Assert.False(controller.Interaction.HasFlag(InteractionState.Hovered));
            window.MouseDown(position, MouseButton.Left); window.MouseUp(position, MouseButton.Left);
            Assert.Equal(1, clicks);
        }
        finally { window.Close(); }
    }
}
