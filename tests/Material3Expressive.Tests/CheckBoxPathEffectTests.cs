using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Avalonia.Markup.Xaml;
using SkiaSharp;
using Xunit;

namespace Material3Expressive.Tests;

public class CheckBoxPathEffectTests
{
    [AvaloniaTheory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void PathEffectChangesActualDrawing_AndClearingRestoresOriginal(bool selected, double density)
    {
        using var clock = new MotionTestClockScope();
        using var effect = SKPathEffect.CreateDash([(float)(2 * density), (float)(3 * density)], 0);
        var box = new CheckBox { IsChecked = selected, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var window = new MotionTestWindow { Width = 100, Height = 100 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { box } };
        window.SetRenderScaling(density); window.Show();
        window.UpdateLayout(); clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromSeconds(1));
        try
        {
            var plain = Capture(); var size = box.Bounds.Size;
            var stroke = new CheckBoxStroke { Cap = selected ? PenLineCap.Square : PenLineCap.Flat, PathEffect = effect };
            if (selected) CheckBoxAppearance.SetCheckmarkStroke(box, stroke); else CheckBoxAppearance.SetOutlineStroke(box, stroke);
            var changed = Capture();
            var part = box.GetVisualDescendants().OfType<CheckBoxIndicator>().Single();
            Assert.Same(effect, (selected ? part.CheckmarkStroke : part.OutlineStroke)?.PathEffect);
            if (plain.SequenceEqual(changed))
            {
                var directory = Path.Combine("artifacts", "checkbox-path-test"); Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, $"plain-{selected}-{density}.png"), plain);
                File.WriteAllBytes(Path.Combine(directory, $"changed-{selected}-{density}.png"), changed);
            }
            Assert.False(plain.SequenceEqual(changed));
            Assert.Equal(size, box.Bounds.Size);
            Assert.Equal(selected, box.IsChecked);
            Assert.NotEqual(IntPtr.Zero, effect.Handle); // Drawing must not dispose caller-owned effects.
            if (selected) CheckBoxAppearance.SetCheckmarkStroke(box, null); else CheckBoxAppearance.SetOutlineStroke(box, null);
            Assert.Equal(plain, Capture());
            Assert.NotEqual(IntPtr.Zero, effect.Handle);
        }
        finally { window.Close(); }

        byte[] Capture()
        {
            for (var i = 0; i < 4; i++) { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); using (window.CaptureRenderedFrame()) { } }
            using var frame = window.CaptureRenderedFrame()!;
            using var data = new MemoryStream(); frame.Save(data, PngBitmapEncoderOptions.Default); return data.ToArray();
        }
    }

    [AvaloniaFact]
    public void ExplicitPathEffectOverridesDashConvenience_AndDisplayUsesSameRenderer()
    {
        using var clock = new MotionTestClockScope();
        using var effect = SKPathEffect.CreateCorner(4);
        var box = new CheckBoxDisplay { IsChecked = true, CheckmarkStroke = new CheckBoxStroke { PathEffect = effect } };
        var window = new MotionTestWindow { Width = 100, Height = 100 };
        box.HorizontalAlignment = HorizontalAlignment.Center; box.VerticalAlignment = VerticalAlignment.Center;
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { box } }; window.Show();
        window.UpdateLayout(); clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromSeconds(1));
        try
        {
            var first = Capture();
            box.CheckmarkStroke = box.CheckmarkStroke with { Dashes = [1, 20], DashOffset = 10 };
            Assert.Equal(first, Capture());
            box.CheckmarkStroke = box.CheckmarkStroke with { PathEffect = null };
            Assert.False(first.SequenceEqual(Capture()));
        }
        finally { window.Close(); }
        byte[] Capture()
        {
            for (var i = 0; i < 4; i++) { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); using (window.CaptureRenderedFrame()) { } }
            using var frame = window.CaptureRenderedFrame()!;
            using var data = new MemoryStream(); frame.Save(data, PngBitmapEncoderOptions.Default); return data.ToArray();
        }
    }
}
