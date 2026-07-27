using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class TextFieldTemplateTests
{
    [AvaloniaFact]
    public void FocusedEditorDoesNotAddStockFluentChrome()
    {
        var field = new MaterialTextField
        {
            Width = 280,
            Label = "Label",
            Variant = TextFieldVariant.Outlined
        };
        var window = new Window
        {
            Width = 420,
            Height = 180,
            Content = field
        };
        MaterialThemeScope.SetTheme(window, VisualTheme.Light.CreateMaterialTheme());
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(420, 180));
        window.Arrange(new Rect(0, 0, 420, 180));
        var point = field.TranslatePoint(new Point(140, field.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
        Dispatcher.UIThread.RunJobs();

        var editor = field.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.Equal(new Thickness(), editor.BorderThickness);
        Assert.Null(editor.FocusAdorner);

        window.Close();
    }
}
