using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Typography;
using Xunit;

namespace Material3Expressive.Tests;

public class TextBaselineIntegrationTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void BadgedBoxNativeTextAndTemplateReplacementKeepMeasuredBaselineOffsets(bool nested)
    {
        using var clock = new MotionTestClockScope();
        var text = new TextBlock { Text = "First\nLast", Margin = new Thickness(0, 3, 0, 1) };
        var inner = new BadgedBox { Content = text, Badge = new Badge { Content = "999" } };
        var box = nested ? new BadgedBox { Content = inner, Badge = new Badge() } : inner;
        box.Height = 80;
        box.Margin = new Thickness(0, 4, 0, 2);
        var window = DialogTests.Show(box);
        try
        {
            void AssertActualLines()
            {
                var pair = MaterialTextBaseline.Read(box, 1);
                var origin = box.Margin.Top + text.TranslatePoint(default, box)!.Value.Y;
                var lines = text.TextLayout.TextLines;
                Assert.Equal(MathUtils.DipToPx(origin + lines[0].Baseline, 1), pair.First);
                Assert.Equal(MathUtils.DipToPx(origin + lines.Take(lines.Count - 1).Sum(l => l.Height) + lines[^1].Baseline, 1), pair.Last);
            }
            AssertActualLines();
            box.Height = 96; window.UpdateLayout(); AssertActualLines();
            MaterialTextBaseline.SetLast(box, 70); window.UpdateLayout();
            Assert.Equal(74, MaterialTextBaseline.Read(box, 1).Last);
            box.ClearValue(MaterialTextBaseline.LastProperty); window.UpdateLayout(); AssertActualLines();
            inner.Content = null;
            inner.ContentTemplate = new FuncDataTemplate<object>((_, _) => text = new TextBlock { Text = "Template\nContent" });
            window.UpdateLayout(); AssertActualLines();
            var template = box.Template;
            box.Template = null; window.UpdateLayout();
            Assert.Equal((int.MinValue, int.MinValue), MaterialTextBaseline.Read(box, 1));
            box.Template = template; window.UpdateLayout(); AssertActualLines();
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void BadgedBoxForwardsOnlyAnchorBaselinesAndInvalidatesConsumers(bool tooltipHost)
    {
        using var clock = new MotionTestClockScope();
        var anchor = new Border { Width = 60, Height = 32 };
        MaterialTextBaseline.SetFirst(anchor, 10);
        MaterialTextBaseline.SetLast(anchor, 26);
        var badgeContent = new Border { Width = 8, Height = 16 };
        MaterialTextBaseline.SetFirst(badgeContent, 1);
        MaterialTextBaseline.SetLast(badgeContent, 15);
        var box = new BadgedBox { Content = anchor, Badge = new Badge { Content = badgeContent } };
        Control host = tooltipHost ? new RichTooltip { Title = box, Content = new Border { Width = 40, Height = 8 } }
            : new Snackbar { Content = box, Action = new Border { Width = 24, Height = 24 }, ActionOnNewLine = true };
        var window = DialogTests.Show(host);
        try
        {
            void AssertBaseline(double first, double last)
            {
                Assert.Equal((first, last), (MaterialTextBaseline.Read(box, 1).First, MaterialTextBaseline.Read(box, 1).Last));
                Assert.Equal(tooltipHost ? 28 : 30, anchor.TranslatePoint(default, host)!.Value.Y + first);
            }
            AssertBaseline(10, 26);
            MaterialTextBaseline.SetFirst(anchor, 6); Flush(); AssertBaseline(6, 26);
            MaterialTextBaseline.SetLast(anchor, 20); Flush(); AssertBaseline(6, 20);
            MaterialTextBaseline.SetFirst(badgeContent, 30); Flush(); AssertBaseline(6, 20);
            MaterialTextBaseline.SetFirst(box, 12); Flush();
            Assert.Equal((12, 12), MaterialTextBaseline.Read(box, 1));
            box.ClearValue(MaterialTextBaseline.FirstProperty); Flush(); AssertBaseline(6, 20);
            box.Content = new Border { Width = 60, Height = 32 }; Flush();
            Assert.Equal((int.MinValue, int.MinValue), MaterialTextBaseline.Read(box, 1));
            box.Content = anchor; Flush(); AssertBaseline(6, 20);
            window.Content = null; window.Content = host; Flush(); AssertBaseline(6, 20);
        }
        finally { window.Close(); }
        void Flush() { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    }

    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(3)]
    public void Snackbar_LastOnlyOnWrapperPreservesAutomaticFirstBaseline(double marginTop)
    {
        using var clock = new MotionTestClockScope();
        var text = new TextBlock { Text = "First\nLast" };
        var content = new Grid { Margin = new Thickness(0, marginTop, 0, 0), Children = { text } };
        var snackbar = new Snackbar { Content = content, Action = new Border { Width = 24, Height = 24 } };
        var window = DialogTests.Show(snackbar);
        try
        {
            var height = snackbar.Bounds.Height;
            var top = text.TranslatePoint(default, snackbar)!.Value.Y;
            var lines = text.TextLayout.TextLines;
            var last = lines.Take(lines.Count - 1).Sum(line => line.Height) + lines[^1].Baseline;
            MaterialTextBaseline.SetLast(content, last);
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            Assert.Equal(height, snackbar.Bounds.Height);
            Assert.Equal(top, text.TranslatePoint(default, snackbar)!.Value.Y);
            Assert.Equal(30, top + MathUtils.DipToPx((float)lines[0].Baseline, 1));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Snackbar_CustomBaselinesUpdateWithoutSizeChanges(bool newLine)
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Width = 80, Height = 40 };
        var action = new Border { Width = 40, Height = 24 };
        MaterialTextBaseline.SetFirst(content, 10);
        MaterialTextBaseline.SetLast(content, 30);
        MaterialTextBaseline.SetFirst(action, 16);
        var snackbar = new Snackbar { Content = content, Action = action, ActionOnNewLine = newLine };
        var window = DialogTests.Show(snackbar);
        try
        {
            Assert.Equal(30, content.TranslatePoint(default, snackbar)!.Value.Y + 10);
            if (newLine) Assert.Equal(62, action.TranslatePoint(default, snackbar)!.Value.Y);
            else Assert.Equal(68, snackbar.Bounds.Height);
            MaterialTextBaseline.SetFirst(content, 14);
            Flush();
            Assert.Equal(30, content.TranslatePoint(default, snackbar)!.Value.Y + 14);
            if (newLine) Assert.Equal(58, action.TranslatePoint(default, snackbar)!.Value.Y);
            MaterialTextBaseline.SetLast(content, 14);
            Flush();
            if (!newLine)
            {
                Assert.Equal(48, snackbar.Bounds.Height);
                Assert.Equal(content.TranslatePoint(default, snackbar)!.Value.Y + 14,
                    action.TranslatePoint(default, snackbar)!.Value.Y + 16);
            }
            window.Content = null; window.Content = snackbar; Flush();
            Assert.Equal(new Size(80, 40), content.Bounds.Size);
        }
        finally { window.Close(); }
        void Flush() { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    }

    [AvaloniaFact]
    public void RichTooltip_ExplicitBaselineOverridesNativeAndClearsBackToIt()
    {
        using var clock = new MotionTestClockScope();
        var title = new Border { Width = 40, Height = 20, Margin = new Thickness(0, 3, 0, 0) };
        TextBlock.SetBaselineOffset(title, 8);
        MaterialTextBaseline.SetFirst(title, 12);
        var tooltip = new RichTooltip { Title = new ContentPresenter { Content = title, Padding = new Thickness(0, 2, 0, 0) }, Content = "Text" };
        var window = DialogTests.Show(tooltip);
        try
        {
            Assert.Equal(28, title.TranslatePoint(default, tooltip)!.Value.Y + 12);
            MaterialTextBaseline.SetFirst(title, 6); Flush();
            Assert.Equal(28, title.TranslatePoint(default, tooltip)!.Value.Y + 6);
            title.ClearValue(MaterialTextBaseline.FirstProperty); Flush();
            Assert.Equal(28, title.TranslatePoint(default, tooltip)!.Value.Y + 8);
        }
        finally { window.Close(); }
        void Flush() { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    }

    [AvaloniaFact]
    public void AttachedBaselinesSupportXamlAndBindingsWithoutInheritingIntoChildren()
    {
        var root = (Border)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/TextBaseline.axaml"));
        Assert.Equal(12.25, MaterialTextBaseline.GetFirst(root));
        Assert.Equal(32.25, MaterialTextBaseline.GetLast(root));
        Assert.Null(MaterialTextBaseline.GetFirst(root.Child!));
        var target = new Border();
        using var binding = target.Bind(MaterialTextBaseline.FirstProperty, root.GetObservable(MaterialTextBaseline.FirstProperty));
        MaterialTextBaseline.SetFirst(root, -2);
        Assert.Equal(-2, MaterialTextBaseline.GetFirst(target));
        Assert.Throws<ArgumentException>(() => { MaterialTextBaseline.SetFirst(root, double.NaN); });
    }
}
