using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using Xunit;

namespace Material3Expressive.VisualTests;

public sealed class OverlayLayoutTests
{
    [AvaloniaFact]
    public void OpenDialogAndSheetArrangeTheirContent()
    {
        var dialogText = new TextBlock { Text = "Dialog content" };
        var sheetText = new TextBlock { Text = "Sheet content" };
        var dialog = new MaterialDialog
        {
            IsOpen = true,
            Content = dialogText
        };
        var sheet = new MaterialBottomSheet
        {
            IsOpen = true,
            Content = sheetText
        };
        var host = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            Children =
            {
                dialog,
                sheet
            }
        };
        Grid.SetColumn(sheet, 1);

        var window = new Window
        {
            Width = 840,
            Height = 330,
            Content = host
        };
        MaterialThemeScope.SetTheme(window, VisualTheme.Light.CreateMaterialTheme());
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(840, 330));
        window.Arrange(new Rect(0, 0, 840, 330));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
        Dispatcher.UIThread.RunJobs();

        Assert.True(
            dialogText.Bounds.Width > 0 && dialogText.Bounds.Height > 0,
            $"Dialog content bounds were {dialogText.Bounds}.");
        Assert.True(
            sheetText.Bounds.Width > 0 && sheetText.Bounds.Height > 0,
            $"Sheet bounds={sheet.Bounds}, desired={sheet.DesiredSize}; " +
            $"presenters={string.Join("; ", sheet.GetVisualDescendants()
                .OfType<ContentPresenter>()
                .Select(item => $"bounds={item.Bounds}, desired={item.DesiredSize}, margin={item.Margin}"))}; " +
            $"content bounds={sheetText.Bounds}, desired={sheetText.DesiredSize}.");

        window.Close();
    }
}
