using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class MenuArrangementTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void NativeArrangementUpdatesDirectionAndRestoresTheDefault(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var label = new Border { Width = 48, Height = 16 };
        var item = new MenuItem { Header = label, Icon = new Border { Width = 20, Height = 20 }, Width = 220,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MenuAppearance.SetTrailingContent(item, new Border { Width = 12, Height = 12 });
        var window = DialogTests.Show(item);
        item.Theme = (ControlTheme)window.FindResource("MaterialVerticalMenuItemTheme")!; window.UpdateLayout();
        try
        {
            var original = label.TranslatePoint(default, window)!.Value;
            MenuAppearance.SetHorizontalArrangement(item, MaterialHorizontalArrangement.Center); window.UpdateLayout();
            Assert.NotEqual(original, label.TranslatePoint(default, window)!.Value);
            MenuAppearance.SetHorizontalArrangement(item, MaterialHorizontalArrangement.Absolute.Left); window.UpdateLayout();
            var physical = new Rect(label.Bounds.Size).TransformToAABB(label.TransformToVisual(window)!.Value).X;
            item.FlowDirection = rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft; window.UpdateLayout();
            Assert.Equal(physical, new Rect(label.Bounds.Size).TransformToAABB(label.TransformToVisual(window)!.Value).X);
            item.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            MenuAppearance.SetHorizontalArrangement(item, null); window.UpdateLayout();
            Assert.Equal(original, label.TranslatePoint(default, window)!.Value);
            Assert.False(item.IsChecked);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("ordinary")] [InlineData("checkable")] [InlineData("selectable")]
    public void ArrangementReplacesSpacingWithoutChangingActivationOwnership(string kind)
    {
        var window = new MotionTestWindow { Width = 320, Height = 160 };
        window.Styles.Add(new MaterialTheme());
        DropdownMenuItem item = kind switch
        {
            "checkable" => new CheckableDropdownMenuItem(),
            "selectable" => new SelectableDropdownMenuItem(),
            _ => new DropdownMenuItem()
        };
        window.Content = item;
        window.Show(); Flush(window);
        if (kind == "ordinary") item.Theme = (ControlTheme)window.FindResource("MaterialExpressiveDropdownMenuItemTheme")!;
        item.Width = 240;
        item.Content = new Border { Width = 48, Height = 16 };
        item.LeadingIcon = new Border { Width = 20, Height = 20 };
        item.TrailingContent = new Border { Width = 20, Height = 20 };
        item.HorizontalArrangement = MaterialHorizontalArrangement.SpacedBy(8);
        Flush(window);
        try
        {
            var layout = item.GetVisualDescendants().OfType<DropdownMenuItemLayout>().Single();
            Assert.Equal(28, layout.Children[1].Bounds.X);
            item.LeadingSpacing = 40; item.TrailingSpacing = 50; Flush(window);
            Assert.Equal(28, layout.Children[1].Bounds.X);
            item.HorizontalArrangement = MaterialHorizontalArrangement.End; Flush(window);
            Assert.Equal(layout.Bounds.Width - 20 - 48, layout.Children[1].Bounds.X);
            item.HorizontalArrangement = null; Flush(window);
            Assert.Equal(60, layout.Children[1].Bounds.X);
            item.HorizontalArrangement = MaterialHorizontalArrangement.Center; Flush(window);
            var clicks = 0; item.Click += (_, _) => clicks++;
            var point = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.Equal(1, clicks);
            if (item is SelectableDropdownMenuItem selectable) Assert.False(selectable.IsSelected);
            if (item is CheckableDropdownMenuItem checkable) Assert.False(checkable.IsChecked);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CustomArrangementReceivesOnlyComposedSlotsAndLogicalDirection()
    {
        var arrangement = new ObservedArrangement();
        var item = new SelectableDropdownMenuItem { Content = new Border { Width = 48, Height = 16 },
            HorizontalArrangement = arrangement, Width = 240 };
        var window = new MotionTestWindow { Width = 320, Height = 160 };
        window.Styles.Add(new MaterialTheme()); window.Content = item; window.Show(); Flush(window);
        try
        {
            Assert.Equal(new[] { 48 }, arrangement.Sizes);
            item.SelectedLeadingIcon = new Border { Width = 20, Height = 20 }; Flush(window);
            Assert.Equal(new[] { 0, 48 }, arrangement.Sizes);
            item.TrailingContent = new Border { Width = 12, Height = 12 }; Flush(window);
            Assert.Equal(3, arrangement.Sizes.Length);
            item.FlowDirection = FlowDirection.RightToLeft; Flush(window);
            Assert.Equal(FlowDirection.RightToLeft, arrangement.Direction);
            item.SelectedLeadingIcon = null; item.TrailingContent = null; Flush(window);
            Assert.Equal(new[] { 48 }, arrangement.Sizes);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BaselineThemeKeepsItsOwnLayoutContract()
    {
        var item = new DropdownMenuItem { Width = 240, Content = new Border { Width = 48, Height = 16 },
            LeadingIcon = new Border { Width = 24, Height = 24 } };
        var window = new MotionTestWindow { Width = 320, Height = 160 };
        window.Styles.Add(new MaterialTheme()); window.Content = item; window.Show(); Flush(window);
        try
        {
            var layout = item.GetVisualDescendants().OfType<DropdownMenuItemLayout>().Single();
            var original = layout.Children[1].Bounds;
            item.HorizontalArrangement = MaterialHorizontalArrangement.Absolute.Right; Flush(window);
            Assert.Equal(original, layout.Children[1].Bounds);
        }
        finally { window.Close(); }
    }

    private sealed record ObservedArrangement : MaterialHorizontalArrangement
    {
        public int[] Sizes { get; private set; } = [];
        public FlowDirection Direction { get; private set; }
        public override void Arrange(double density, int totalSize, ReadOnlySpan<int> sizes, FlowDirection direction, Span<int> positions)
        {
            Sizes = sizes.ToArray(); Direction = direction;
            Center.Arrange(density, totalSize, sizes, direction, positions);
        }
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
