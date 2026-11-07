using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class PrecisionPointerTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExpressiveMenu_InheritsPrecisionAndHonorsCallerDefaults(bool checkable)
    {
        DropdownMenuItem item = checkable ? new CheckableDropdownMenuItem() : new SelectableDropdownMenuItem();
        item.Content = new Border { Width = 32, Height = 16 };
        var window = DialogTests.Show(item);
        try
        {
            var normal = item.Padding;
            MaterialPrecisionPointer.SetIsEnabled(window, true); window.UpdateLayout();
            Assert.Equal(new Thickness(16, 12, 10, 12), item.Padding);
            Assert.Equal(12, item.LeadingSpacing); Assert.Equal(12, item.TrailingSpacing);
            item.Styles.Add(new Style(s => s.OfType(item.GetType())) { Setters = { new Setter(Button.PaddingProperty, new Thickness(3)) } });
            item.LeadingSpacing = 5;
            MaterialPrecisionPointer.SetIsEnabled(window, false); window.UpdateLayout();
            Assert.Equal(new Thickness(3), item.Padding); Assert.Equal(5, item.LeadingSpacing); Assert.Equal(8, item.TrailingSpacing);
            item.Styles.Clear(); item.ClearValue(DropdownMenuItem.LeadingSpacingProperty); window.UpdateLayout();
            Assert.Equal(normal, item.Padding); Assert.Equal(8, item.LeadingSpacing);
            MaterialPrecisionPointer.SetIsEnabled(window, true);
            window.Resources[MaterialTokens.Comp.Menu.PrecisionContentSpacing] = 14d;
            window.UpdateLayout(); Assert.Equal(14, item.LeadingSpacing); Assert.Equal(14, item.TrailingSpacing);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Dialog_InheritsPrecisionAcrossOverlay_AndRestoresTitleAndSpacing()
    {
        using var clock = new MotionTestClockScope();
        var confirm = new Button { Content = new Border { Width = 32, Height = 16 } };
        var dialog = new AlertDialog { Title = "Title", Content = "Body", ConfirmButton = confirm,
            IsOpen = true, AnimateTransition = false };
        var window = DialogTests.Show(new Grid { Children = { dialog } });
        try
        {
            Control Part(string name) => window.GetVisualDescendants().OfType<Control>().Single(c => c.Name == name);
            var title = Part("PART_Title");
            var normal = MaterialText.GetStyle(title)!;
            MaterialPrecisionPointer.SetIsEnabled(window, true); window.UpdateLayout();
            Assert.Equal(new Thickness(20), window.GetVisualDescendants().OfType<AlertDialogColumn>().Single().Margin);
            Assert.Equal(new Thickness(0, 0, 0, 16), Part("PART_Text").Margin);
            Assert.Equal(normal with { FontSize = 20, LineHeight = 26 }, MaterialText.GetStyle(title));
            Assert.Equal(36, confirm.GetVisualDescendants().OfType<MaterialSurface>().Single().Bounds.Height);
            MaterialPrecisionPointer.SetIsEnabled(dialog, false); window.UpdateLayout();
            Assert.Equal(new Thickness(24), window.GetVisualDescendants().OfType<AlertDialogColumn>().Single().Margin);
            Assert.Equal(new Thickness(0, 0, 0, 24), Part("PART_Text").Margin);
            Assert.Equal(normal, MaterialText.GetStyle(title));
            dialog.ClearValue(MaterialPrecisionPointer.IsEnabledProperty); window.UpdateLayout();
            Assert.Equal(20, MaterialText.GetStyle(title)!.FontSize);
            window.Resources[MaterialTokens.Comp.Dialog.PrecisionContentPadding] = new Thickness(18);
            window.Resources[MaterialTokens.Comp.Dialog.PrecisionHeadlineStyleOverride] = new TextStyle { FontSize = 22, LineHeight = 28 };
            window.UpdateLayout();
            Assert.Equal(new Thickness(18), window.GetVisualDescendants().OfType<AlertDialogColumn>().Single().Margin);
            Assert.Equal(normal with { FontSize = 22, LineHeight = 28 }, MaterialText.GetStyle(title));
            MaterialPrecisionPointer.SetIsEnabled(window, false); window.UpdateLayout();
            Assert.Equal(normal, MaterialText.GetStyle(title));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, 36)]
    [InlineData(true, 40)]
    public void InheritedMode_ChangesSmallDefaultsAndRestoresThem(bool toggle, double preciseHeight)
    {
        Button button = toggle ? new ToggleButton() : new Button();
        button.Content = new Border { Width = 32, Height = 16 };
        var parent = new StackPanel { Children = { button } };
        var window = DialogTests.Show(parent);
        try
        {
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(40, surface.Bounds.Height);
            MaterialPrecisionPointer.SetIsEnabled(parent, true); window.UpdateLayout();
            Assert.Equal(new Thickness(16, 8), button.Padding);
            Assert.Equal(preciseHeight, surface.Bounds.Height);
            Assert.Equal(48, button.Bounds.Height);
            MaterialButton.SetIconTemplate(button, new FuncDataTemplate<object>((_, _) => new Border())); window.UpdateLayout();
            Assert.Equal(new Thickness(12, 8, 16, 8), button.Padding);
            MaterialPrecisionPointer.SetIsEnabled(button, false); window.UpdateLayout();
            Assert.Equal(new Thickness(16, 10), button.Padding);
            button.ClearValue(MaterialPrecisionPointer.IsEnabledProperty); window.UpdateLayout();
            Assert.Equal(new Thickness(12, 8, 16, 8), button.Padding);
            MaterialPrecisionPointer.SetIsEnabled(parent, false); window.UpdateLayout();
            Assert.Equal(40, surface.Bounds.Height);
            Assert.Equal(new Thickness(16, 10), button.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitStyleAndTokenConfiguration_RemainEffective(bool toggle)
    {
        Button button = toggle ? new ToggleButton() : new Button();
        button.Content = new Border { Width = 32, Height = 16 };
        var window = DialogTests.Show(button);
        try
        {
            MaterialPrecisionPointer.SetIsEnabled(window, true);
            window.Resources[MaterialTokens.Comp.ButtonSmall.PrecisionContentPadding] = new Thickness(20, 6);
            window.Resources[MaterialTokens.Comp.ButtonSmall.PrecisionContainerHeight] = 44d;
            window.UpdateLayout();
            Assert.Equal(new Thickness(20, 6), button.Padding);
            Assert.Equal(toggle ? 40 : 44, button.GetVisualDescendants().OfType<MaterialSurface>().Single().Bounds.Height);
            var style = new Style(selector => selector.OfType(button.GetType())) { Setters = { new Setter(Button.PaddingProperty, new Thickness(2, 3)) } };
            button.Styles.Add(style); window.UpdateLayout();
            Assert.Equal(new Thickness(2, 3), button.Padding);
            MaterialPrecisionPointer.SetIsEnabled(window, false); window.UpdateLayout();
            Assert.Equal(new Thickness(2, 3), button.Padding);
            button.Styles.Remove(style); window.UpdateLayout();
            Assert.Equal(new Thickness(16, 10), button.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("xsmall", 32)]
    [InlineData("medium", 56)]
    [InlineData("large", 96)]
    [InlineData("xlarge", 136)]
    public void OtherSizeClasses_KeepTheirExistingMetrics(string size, double height)
    {
        var window = ButtonThemeFixture.ShowWindow(out var button, sizeClass: size);
        try
        {
            button.Content = new Border { Width = 32, Height = 16 };
            window.UpdateLayout();
            var padding = button.Padding;
            MaterialPrecisionPointer.SetIsEnabled(window, true); window.UpdateLayout();
            Assert.Equal(padding, button.Padding);
            Assert.Equal(height, button.GetVisualDescendants().OfType<MaterialSurface>().Single().Bounds.Height);
        }
        finally { window.Close(); }
    }
}
