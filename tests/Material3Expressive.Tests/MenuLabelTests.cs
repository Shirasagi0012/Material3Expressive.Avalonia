using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class MenuLabelTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void GroupLabel_UsesConstrainedSizeAndLogicalPadding(bool rtl)
    {
        var content = new Border { Width = 24, Height = 12 };
        var label = new DropdownMenuGroupLabel { Content = content };
        var root = new StackPanel { Children = { label }, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = DialogTests.Show(root);
        try
        {
            Assert.Equal(new Size(112, 32), label.Bounds.Size);
            double Left(Control control) => Math.Min(control.TranslatePoint(default, window)!.Value.X,
                control.TranslatePoint(new Point(control.Bounds.Width, 0), window)!.Value.X);
            Assert.Equal(rtl ? 76 : 12, Left(content) - Left(label));
            Assert.Equal(10, content.TranslatePoint(default, label)!.Value.Y);
            Assert.False(label.Focusable);
            root.Width = 80; window.UpdateLayout();
            Assert.Equal(80, label.Bounds.Width);
            root.Width = 400; label.Width = 350; label.Height = 20; window.UpdateLayout();
            Assert.Equal(new Size(350, 20), label.Bounds.Size);
            label.ClearValue(Control.WidthProperty); label.ClearValue(Control.HeightProperty);
            content.Width = 320; window.UpdateLayout();
            Assert.Equal(280, label.Bounds.Width);
            window.Resources[MaterialTokens.Comp.Menu.GroupLabelMinHeight] = 40d; window.UpdateLayout();
            Assert.Equal(40, label.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TrailingLabel_InheritsTypographyColorAndPrecision_WithoutReplacingOverrides()
    {
        var text = new TextBlock { Text = "Ctrl+C" };
        var label = new DropdownMenuItemTrailingLabel { Content = text };
        var root = new StackPanel { Children = { label } };
        TextElement.SetForeground(root, Brushes.Red);
        var window = DialogTests.Show(root);
        try
        {
            Assert.Equal(14, text.FontSize);
            Assert.Equal(Brushes.Red, text.Foreground);
            Assert.Equal(default, label.Padding);
            MaterialPrecisionPointer.SetIsEnabled(root, true); window.UpdateLayout();
            Assert.Equal(new Thickness(0, 0, 6, 0), label.Padding);
            label.Styles.Add(new Style(s => s.OfType<DropdownMenuItemTrailingLabel>())
                { Setters = { new Setter(ContentControl.PaddingProperty, new Thickness(2, 3, 4, 5)) } });
            MaterialPrecisionPointer.SetIsEnabled(root, false); window.UpdateLayout();
            Assert.Equal(new Thickness(2, 3, 4, 5), label.Padding);
            label.Styles.Clear();
            label.Content = "new";
            label.ContentTemplate = new FuncDataTemplate<string>((value, _) => new TextBlock { Text = value, FontSize = 19 });
            MaterialPrecisionPointer.SetIsEnabled(root, true); window.UpdateLayout();
            Assert.Equal(19, ((TextBlock)label.Presenter!.Child!).FontSize);
            Assert.Equal(new Thickness(0, 0, 6, 0), label.Padding);
        }
        finally { window.Close(); }
    }
}
