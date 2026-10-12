using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;
using MaterialSymbols;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class DropDownButtonThemeTests
{
    static readonly string[] Themes =
        ["MaterialFilledButtonTheme", "MaterialElevatedButtonTheme", "MaterialTonalButtonTheme", "MaterialOutlinedButtonTheme", "MaterialTextButtonTheme"];
    static readonly string?[] Sizes = [null, "xsmall", "medium", "large", "xlarge"];

    [AvaloniaFact]
    public void ImplicitTheme_IsTheFilledButtonWithATrailingChevron()
    {
        var dropDown = new DropDownButton { Content = "Options" };
        var reference = new Button { Content = "Options" };
        var (window, _) = Show(dropDown, reference);
        Assert.Same(window.FindResource("MaterialFilledButtonTheme"), ((ControlTheme)window.FindResource(typeof(DropDownButton))!).BasedOn);
        Assert.True(MaterialInteraction.GetIsInteractive(dropDown));
        var chevron = Chevron(dropDown);
        Assert.Equal(MaterialSymbolCodepoint.KeyboardArrowDown, chevron.Symbol);
        AssertMatchesButton(reference, dropDown);
        // Same row metrics as the leading icon: the chevron box is IconSize and the gap IconSpacing.
        var row = Row(dropDown);
        Assert.Equal(row.IconSize, Trailing(dropDown).Bounds.Width);
        Assert.Equal(row.IconSize, chevron.Bounds.Width, 3);
        var label = Label(dropDown);
        var labelRight = label.TranslatePoint(new Point(label.Bounds.Width, 0), dropDown)!.Value.X;
        var chevronLeft = Trailing(dropDown).TranslatePoint(default, dropDown)!.Value.X;
        Assert.Equal(row.IconSpacing, chevronLeft - labelRight, 1);
        // Outer padding is unchanged: the chevron's far edge sits at the owner's trailing padding.
        Assert.Equal(dropDown.Padding.Right, Container(dropDown).Bounds.Width - (chevronLeft + row.IconSize) + Container(dropDown).Bounds.X, 1);
        Assert.Equal(Brush(dropDown.Foreground), Brush(chevron.GetValue(TextElement.ForegroundProperty)));
        window.Close();
    }

    [AvaloniaFact]
    public void Rtl_PutsTheChevronOnTheTrailingSide()
    {
        var ltr = new DropDownButton { Content = "Options" };
        var rtl = new DropDownButton { Content = "Options", FlowDirection = FlowDirection.RightToLeft };
        MaterialButton.SetIcon(rtl, new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Sort });
        MaterialButton.SetIcon(ltr, new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Sort });
        var (window, _) = Show(ltr, rtl);
        // Measure in the parent's (unmirrored) space: RTL mirroring is the owner's own transform.
        double Center(Visual v, Visual owner) => v.TranslatePoint(new Point(v.Bounds.Width / 2, 0), window)!.Value.X
            - Math.Min(owner.TranslatePoint(default, window)!.Value.X, owner.TranslatePoint(new Point(owner.Bounds.Width, 0), window)!.Value.X);
        Assert.Equal(ltr.Bounds.Size, rtl.Bounds.Size);
        var ltrIcon = (Visual)MaterialButton.GetIcon(ltr)!; var rtlIcon = (Visual)MaterialButton.GetIcon(rtl)!;
        // LTR: icon < label < chevron. RTL mirrors to chevron < label < icon.
        Assert.True(Center(ltrIcon, ltr) < Center(Label(ltr), ltr) && Center(Label(ltr), ltr) < Center(Chevron(ltr), ltr));
        Assert.True(Center(Chevron(rtl), rtl) < Center(Label(rtl), rtl) && Center(Label(rtl), rtl) < Center(rtlIcon, rtl));
        Assert.Equal(ltr.Bounds.Width - Center(Chevron(ltr), ltr), Center(Chevron(rtl), rtl), 1);
        window.Close();
    }

    [AvaloniaFact]
    public void KeyedVariantsAndSizeClasses_KeepTheChevron()
    {
        foreach (var theme in Themes)
        foreach (var size in Sizes)
        {
            var dropDown = new DropDownButton { Content = "Menu", Tag = theme };
            var reference = new Button { Content = "Menu", Tag = theme };
            if (size is not null) { dropDown.Classes.Add(size); reference.Classes.Add(size); }
            var (window, _) = Show(dropDown, reference);
            Assert.Same(window.FindResource(theme), dropDown.Theme);
            AssertMatchesButton(reference, dropDown);
            var chevron = Chevron(dropDown);
            Assert.Equal(Row(reference).IconSize, Trailing(dropDown).Bounds.Width);
            Assert.Equal(Row(reference).IconSize, chevron.Bounds.Width, 3);
            Assert.Equal(Brush(reference.Foreground), Brush(chevron.GetValue(TextElement.ForegroundProperty)));
            Assert.Equal(reference.Bounds.Width + Row(reference).IconSize + Row(reference).IconSpacing, dropDown.Bounds.Width, 1);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OtherButtons_PayNothingForTheTrailingBox()
    {
        var button = new Button { Content = "Plain" };
        var repeat = new RepeatButton { Content = "Repeat" };
        var link = new HyperlinkButton { Content = "Link" };
        var tonal = new Button { Content = "Tonal", Tag = "MaterialTonalButtonTheme" };
        var xlarge = new Button { Content = "XL", Classes = { "xlarge" } };
        var (window, _) = Show(button, repeat, link, tonal, xlarge);
        foreach (var b in new Button[] { button, repeat, link, tonal, xlarge })
        {
            var trailing = Trailing(b);
            Assert.Null(trailing.Child);
            Assert.Equal(default, trailing.Bounds.Size);
            Assert.Equal(0, TokenBindingCounter.Count(trailing));
            Assert.Equal(BindingPriority.Unset, trailing.GetDiagnostic(ContentPresenter.ContentTemplateProperty).Priority);
            Assert.Equal(BindingPriority.Unset, trailing.GetDiagnostic(MaterialIcon.DefaultSizeProperty).Priority);
            Assert.Null(Animated.GetProperties(trailing));
        }
        // A plain Button and a RepeatButton carry the same {m3:Token} bindings.
        Assert.Equal(TokenBindingCounter.CountTree(button), TokenBindingCounter.CountTree(repeat));
        window.Close();

        // A DropDownButton costs exactly its chevron's own bindings on top of the same-theme Button,
        // so everything else in the shared template is identical.
        foreach (var (theme, size) in new (string?, string?)[] { (null, null), ("MaterialTonalButtonTheme", null), (null, "xlarge") })
        {
            var dropDown = new DropDownButton { Content = "Same", Tag = theme };
            var reference = new Button { Content = "Same", Tag = theme };
            if (size is not null) { dropDown.Classes.Add(size); reference.Classes.Add(size); }
            (window, _) = Show(dropDown, reference);
            var chevronBindings = TokenBindingCounter.CountTree(Trailing(dropDown));
            Assert.True(chevronBindings > 0);
            Assert.Equal(chevronBindings, TokenBindingCounter.CountTree(dropDown) - TokenBindingCounter.CountTree(reference));
            Assert.Equal(0, TokenBindingCounter.CountTree(Trailing(reference)));
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Click_OpensTheMenuAndRotatesTheChevronUntilClosed()
    {
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "First" }, new MenuItem { Header = "Second" } } };
        var dropDown = new DropDownButton { Content = "Options", Flyout = flyout };
        var (window, clock) = Show(dropDown);
        Assert.Equal(0, ChevronRotation.GetAngle(Trailing(dropDown)));
        var center = dropDown.TranslatePoint(new Point(dropDown.Bounds.Width / 2, dropDown.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
        Draw(window);
        Assert.True(flyout.IsOpen);
        Assert.Contains(":flyout-open", dropDown.Classes);
        Assert.IsType<MenuFlyoutPresenter>(flyout.Popup.Child);
        // Mid-flight, the spring is between the two destinations.
        Advance(window, clock, 2);
        var midway = ChevronRotation.GetAngle(Trailing(dropDown));
        Assert.InRange(midway, 1, 179);
        Settle(window, clock);
        Assert.Equal(180, ChevronRotation.GetAngle(Trailing(dropDown)), 3);
        Assert.Equal(180, Assert.IsType<RotateTransform>(Trailing(dropDown).RenderTransform).Angle, 3);

        flyout.Hide(); Draw(window);
        Assert.DoesNotContain(":flyout-open", dropDown.Classes);
        Settle(window, clock);
        Assert.Equal(0, ChevronRotation.GetAngle(Trailing(dropDown)), 3);
        window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_EnterAndSpaceOpenTheMenu()
    {
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "First" } } };
        var dropDown = new DropDownButton { Content = "Options", Flyout = flyout };
        var (window, clock) = Show(dropDown);
        dropDown.Focus(NavigationMethod.Tab); Draw(window);
        Assert.Contains(":focus-visible", dropDown.Classes);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Draw(window);
        Assert.True(flyout.IsOpen);
        Settle(window, clock);
        Assert.Equal(180, ChevronRotation.GetAngle(Trailing(dropDown)), 3);
        flyout.Hide(); Settle(window, clock);
        Assert.Equal(0, ChevronRotation.GetAngle(Trailing(dropDown)), 3);

        dropDown.Focus(NavigationMethod.Tab); Draw(window);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Draw(window);
        Assert.True(flyout.IsOpen);
        flyout.Hide(); Draw(window);
        window.Close();
    }

    [AvaloniaFact]
    public void FocusVisible_UsesTheButtonStateLayer()
    {
        var dropDown = new DropDownButton { Content = "Options" };
        var reference = new Button { Content = "Options" };
        var (window, _) = Show(dropDown, reference);
        dropDown.Focus(NavigationMethod.Tab); Draw(window);
        var focused = Brush(Container(dropDown).StateLayerBrush);
        reference.Focus(NavigationMethod.Tab); Draw(window);
        Assert.Equal(Brush(Container(reference).StateLayerBrush), focused);
        window.Close();
    }

    [AvaloniaFact]
    public void DisabledAndDark_FollowTheButton()
    {
        var dropDown = new DropDownButton { Content = "Options", Tag = "MaterialOutlinedButtonTheme" };
        var reference = new Button { Content = "Options", Tag = "MaterialOutlinedButtonTheme" };
        var (window, _) = Show(dropDown, reference);
        window.RequestedThemeVariant = ThemeVariant.Dark; Draw(window);
        AssertMatchesButton(reference, dropDown);
        Assert.Equal(Brush(reference.Foreground), Brush(Chevron(dropDown).GetValue(TextElement.ForegroundProperty)));
        dropDown.IsEnabled = false; reference.IsEnabled = false; Draw(window);
        AssertMatchesButton(reference, dropDown);
        Assert.Equal(Brush(reference.Foreground), Brush(Chevron(dropDown).GetValue(TextElement.ForegroundProperty)));
        window.Close();
    }

    [AvaloniaFact]
    public void RuntimeThemeSwitch_KeepsTheChevron()
    {
        // Keyed themes share the Filled template, so a switch keeps the template children; the
        // chevron comes from global styles, so it survives and keeps its full trailing box.
        var dropDown = new DropDownButton { Content = "Switch" };
        var (window, _) = Show(dropDown);
        var presenter = Trailing(dropDown);
        foreach (var theme in Themes.Reverse())
        {
            dropDown.Theme = (ControlTheme)window.FindResource(theme)!; Draw(window);
            Assert.Same(presenter, Trailing(dropDown));
            Assert.Equal(MaterialSymbolCodepoint.KeyboardArrowDown, Chevron(dropDown).Symbol);
            Assert.Equal(Row(dropDown).IconSize, presenter.Bounds.Width);
        }
        window.Close();
    }

    static void AssertMatchesButton(Button expected, Button actual)
    {
        Assert.Equal(expected.Bounds.Height, actual.Bounds.Height);
        Assert.Equal(expected.CornerRadius, actual.CornerRadius);
        Assert.Equal(expected.Padding, actual.Padding);
        Assert.Equal(expected.BorderThickness, actual.BorderThickness);
        Assert.Equal(Brush(expected.BorderBrush), Brush(actual.BorderBrush));
        Assert.Equal(Brush(expected.Background), Brush(actual.Background));
        Assert.Equal(Brush(expected.Foreground), Brush(actual.Foreground));
        Assert.Equal(Brush(Container(expected).StateLayerBrush), Brush(Container(actual).StateLayerBrush));
        Assert.Equal(MaterialButton.GetElevation(expected), MaterialButton.GetElevation(actual));
        Assert.Equal(Label(expected).FontSize, Label(actual).FontSize);
    }

    static ContentPresenter Trailing(Button button) => button.GetTemplateDescendants().OfType<ContentPresenter>()
        .Single(p => p.Name == "PART_TrailingIconPresenter");

    static MaterialSymbol Chevron(Button button) => Assert.IsType<MaterialSymbol>(Trailing(button).Child);

    static ButtonContentRow Row(Button button) => button.GetTemplateDescendants().OfType<ButtonContentRow>().Single();

    static TextBlock Label(Button button) => button.GetTemplateDescendants().OfType<ContentPresenter>()
        .Single(p => p.Name == "PART_ContentPresenter").GetVisualDescendants().OfType<TextBlock>().First();

    static MaterialSurface Container(Button button) =>
        button.GetTemplateDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_Container");

    static Color? Brush(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    static (AnimationTestWindow, ManualAnimationFrameClock) Show(params Control[] controls)
    {
        var clock = new ManualAnimationFrameClock(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(clock) { Width = 640, Height = 480 };
        // Headless popups need an overlay layer; the bare test window has no theme template.
        window.Template = new FuncControlTemplate<Window>((owner, scope) =>
        {
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter", [!ContentPresenter.ContentProperty] = owner[!ContentControl.ContentProperty] };
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            scope.Register(presenter.Name, presenter); scope.Register(layers.Name, layers);
            return layers;
        });
        window.Styles.Add(new MaterialTheme());
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 16 };
        foreach (var control in controls) panel.Children.Add(control);
        window.Content = panel;
        window.Show();
        foreach (var button in panel.Children.OfType<Button>().Where(b => b.Tag is string))
            button.Theme = (ControlTheme)window.FindResource((string)button.Tag!)!;
        Draw(window); return (window, clock);
    }

    static void Advance(Window window, ManualAnimationFrameClock clock, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Draw(window);
        }
    }

    static void Settle(Window window, ManualAnimationFrameClock clock) => Advance(window, clock, 90);

    static void Draw(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
