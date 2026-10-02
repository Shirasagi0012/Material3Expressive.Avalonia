using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonTextLayoutTests
{
    [AvaloniaFact]
    public void NarrowButton_WrapsItsLabelAndGrowsToKeepVerticalPadding()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            window.Width = 320;
            window.Height = 240;
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.Width = 150;
            button.Content = "Save all pending changes";
            window.UpdateLayout();

            var label = button.GetVisualDescendants().OfType<TextBlock>().Single();
            var container = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.True(label.TextLayout.TextLines.Count > 1);
            Assert.Equal(label.Bounds.Height + button.Padding.Top + button.Padding.Bottom, container.Bounds.Height);
            Assert.Equal(button.Width - button.Padding.Left - button.Padding.Right, label.Bounds.Width);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HeightConstrainedButton_KeepsTheSoftWrappedParagraphWidth()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            button.Width = 150;
            button.Height = 40;
            button.Content = "Save all pending changes";
            window.UpdateLayout();

            var label = button.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(button.Width - button.Padding.Left - button.Padding.Right, label.Bounds.Width);
            Assert.Equal(2, label.TextLayout.TextLines.Count);
            Assert.Equal(20, label.Bounds.Height);
            Assert.Equal(40, label.TextLayout.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AutomaticLabel_RecyclesContentAndRespectsCustomTemplatesAndControls()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().Single();
            var originalLabel = Assert.IsAssignableFrom<TextBlock>(presenter.Child);
            button.Content = "Updated";
            window.UpdateLayout();
            Assert.Same(originalLabel, presenter.Child);
            Assert.Equal("Updated", originalLabel.Text);

            var ancestorContent = new Border { Width = 70, Height = 30 };
            window.DataTemplates.Add(new FuncDataTemplate<string>((_, _) => ancestorContent));
            button.Content = "Uses ancestor template";
            window.UpdateLayout();
            Assert.Same(ancestorContent, presenter.Child);

            var replacementContent = new Border { Width = 72, Height = 30 };
            window.DataTemplates.Clear();
            window.DataTemplates.Add(new FuncDataTemplate<string>((_, _) => replacementContent));
            presenter.UpdateChild();
            window.UpdateLayout();
            Assert.Same(replacementContent, presenter.Child);

            var explicitContent = new Border { Width = 80, Height = 32 };
            button.ContentTemplate = new FuncDataTemplate<string>((_, _) => explicitContent);
            window.UpdateLayout();
            Assert.Same(explicitContent, presenter.Child);

            button.ContentTemplate = null;
            window.UpdateLayout();
            Assert.Same(replacementContent, presenter.Child);

            var directContent = new Border { Width = 90, Height = 34 };
            button.Content = directContent;
            window.UpdateLayout();
            Assert.Same(directContent, presenter.Child);

            window.DataTemplates.Clear();
            button.Content = "Automatic again";
            window.UpdateLayout();
            Assert.Equal("Automatic again", Assert.IsAssignableFrom<TextBlock>(presenter.Child).Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("xsmall")]
    [InlineData(null)]
    [InlineData("medium")]
    [InlineData("large")]
    [InlineData("xlarge")]
    public void MultilineButton_KeepsContentPaddingAtEverySize(string? size)
    {
        var window = ButtonThemeFixture.ShowWindow(out var button, sizeClass: size);
        try
        {
            window.Width = 320;
            window.Height = 240;
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.Content = "Save\nchanges";
            window.UpdateLayout();

            var label = button.GetVisualDescendants().OfType<TextBlock>().Single();
            var container = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(2, label.TextLayout.TextLines.Count);
            Assert.Equal(label.Bounds.Height + button.Padding.Top + button.Padding.Bottom, container.Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }
}
