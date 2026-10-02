using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonLayoutTests
{
    [AvaloniaTheory]
    [InlineData(null, null, true, 40)]
    [InlineData(null, "xsmall", true, 32)]
    [InlineData("MaterialTextButtonTheme", null, true, 40)]
    [InlineData(null, null, false, 40)]
    public void EmptyButton_SeparatesContainerMinimumFromInteractiveSpacing(
        string? theme, string? size, bool enabled, double height)
    {
        var window = ButtonThemeFixture.ShowWindow(out var button, theme, size);
        try
        {
            button.Content = "";
            button.IsEnabled = enabled;
            window.UpdateLayout();
            var container = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(new Size(58, height), container.Bounds.Size);
            Assert.Equal(new Size(58, 48), button.Bounds.Size);
            Assert.Equal((48 - height) / 2, container.Bounds.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitWidth_CanBeSmallerThanDefaultContainerMinimum()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            button.Content = "";
            button.Padding = default;
            button.Width = 30;
            window.UpdateLayout();
            Assert.Equal(30, button.Bounds.Width);
            Assert.Equal(30, button.GetVisualDescendants().OfType<MaterialSurface>().Single().Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void UserMinimum_ReplacesDefaultButRetainsInteractiveSpacing()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            button.Content = new Border { Width = 10, Height = 10 };
            button.Padding = default;
            button.MinWidth = 35;
            button.MinHeight = 35;
            window.UpdateLayout();
            var container = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(new Size(48, 48), button.Bounds.Size);
            Assert.Equal(new Rect(7, 7, 35, 35), container.Bounds);

            button.MinWidth = button.MinHeight = 0;
            window.UpdateLayout();
            Assert.Equal(new Size(58, 40), container.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitHeight_OverridesDefaultContainerMinimum()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            button.Content = new Border { Width = 10, Height = 10 };
            button.Padding = default;
            button.Height = 20;
            window.UpdateLayout();
            Assert.Equal(20, button.Bounds.Height);
            Assert.Equal(20, button.GetVisualDescendants().OfType<MaterialSurface>().Single().Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(36, 0)]
    [InlineData(37, 1)]
    public void ConstrainedParent_ClampsDefaultContainerHeight(double height, double offset)
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            button.Content = new Border { Width = 10, Height = 10 };
            button.Padding = default;
            window.Content = null;
            window.Content = new Grid { Height = height, Children = { button } };
            window.UpdateLayout();
            Assert.Equal(height, button.Bounds.Height);
            var container = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(height, container.Bounds.Height);
            Assert.Equal(offset, container.Bounds.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(44)]
    [InlineData(48)]
    [InlineData(80)]
    public void Stretch_FillsTheAllocatedSpace(double height)
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.VerticalAlignment = VerticalAlignment.Stretch;
            window.Content = null;
            window.Content = new Grid { Width = 200, Height = height, Children = { button } };
            window.UpdateLayout();
            Assert.Equal(new Size(200, height), button.Bounds.Size);
            Assert.Equal(button.Bounds.Size, button.GetVisualDescendants().OfType<MaterialSurface>().Single().Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void InteractiveSpacing_DoesNotMakeTheMouseGapClickable()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            var clicks = 0;
            button.Click += (_, _) => clicks++;
            using var frame = window.CaptureRenderedFrame();
            var container = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var origin = container.TranslatePoint(default, window)!.Value;
            var gap = origin + new Vector(container.Bounds.Width / 2, -2);
            window.MouseDown(gap, MouseButton.Left);
            window.MouseUp(gap, MouseButton.Left);
            Assert.Equal(0, clicks);
            var inside = origin + new Vector(container.Bounds.Width / 2, container.Bounds.Height / 2);
            window.MouseDown(inside, MouseButton.Left);
            window.MouseUp(inside, MouseButton.Left);
            Assert.Equal(1, clicks);
        }
        finally { window.Close(); }
    }
}
