// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// foundation/text/TextFieldCursor.kt and CMP desktop DefaultCursorThickness = 1.dp.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialTextFieldCaretTests
{
    [AvaloniaFact]
    public void FocusedCollapsedSelectionUsesFullBodyLineRectangle()
    {
        var field = new MaterialTextField
        {
            Width = 240,
            Height = 56,
            Label = "Label",
            Placeholder = "Placeholder"
        };
        var window = ThemeTestHelper.ShowWindow(field);
        Dispatcher.UIThread.RunJobs();

        var editor = Assert.Single(field.GetVisualDescendants().OfType<TextBox>());
        editor.CaretBlinkInterval = TimeSpan.Zero;
        Assert.True(editor.Focus());
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(
            Colors.Transparent,
            Assert.IsAssignableFrom<ISolidColorBrush>(editor.CaretBrush).Color);
        Assert.True(field.IsMaterialCaretVisible);
        Assert.False(field.IsMaterialCaretBlinking);

        var caret = Assert.IsType<Rect>(field.MaterialCaretBounds);
        Assert.Equal(1, caret.Width);
        Assert.Equal(24, caret.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void SelectionHidesMaterialCaretAndCollapsedSelectionRestoresIt()
    {
        var field = new MaterialTextField
        {
            Width = 240,
            Height = 56,
            Text = "Text"
        };
        var window = ThemeTestHelper.ShowWindow(field);
        Dispatcher.UIThread.RunJobs();

        var editor = Assert.Single(field.GetVisualDescendants().OfType<TextBox>());
        editor.CaretBlinkInterval = TimeSpan.Zero;
        Assert.True(editor.Focus());
        editor.SelectionStart = 0;
        editor.SelectionEnd = 2;

        Assert.False(field.IsMaterialCaretVisible);

        editor.SelectionStart = 2;
        editor.SelectionEnd = 2;

        Assert.True(field.IsMaterialCaretVisible);
        Assert.Equal(1, field.MaterialCaretBounds?.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void PositiveBlinkIntervalStartsAndLostFocusStopsTimer()
    {
        var panel = new StackPanel();
        var field = new MaterialTextField { Width = 240, Height = 56 };
        var next = new Button { Content = "Next" };
        panel.Children.Add(field);
        panel.Children.Add(next);
        var window = ThemeTestHelper.ShowWindow(panel);
        Dispatcher.UIThread.RunJobs();

        var editor = Assert.Single(field.GetVisualDescendants().OfType<TextBox>());
        editor.CaretBlinkInterval = TimeSpan.FromSeconds(1);
        Assert.True(editor.Focus());

        Assert.True(field.IsMaterialCaretVisible);
        Assert.True(field.IsMaterialCaretBlinking);

        Assert.True(next.Focus());

        Assert.False(field.IsMaterialCaretVisible);
        Assert.False(field.IsMaterialCaretBlinking);
        window.Close();
    }
}
