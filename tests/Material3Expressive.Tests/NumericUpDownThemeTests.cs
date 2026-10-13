using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Interaction;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using MaterialSymbols;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class NumericUpDownThemeTests
{
    [AvaloniaFact]
    public void TemplateParts_HostSpinnerInMaterialTextFieldTrailingSlot()
    {
        var nud = new NumericUpDown { Value = 5 };
        var window = Show(nud);
        var box = TextBox(nud);
        Assert.Same(box, nud.GetVisualChildren().Single());
        Assert.NotNull(box.GetTemplateDescendants().OfType<Control>().SingleOrDefault(c => c.Name == "PART_Decoration"));
        var spinner = Spinner(nud);
        Assert.Same(spinner, MaterialTextField.GetTrailingIcon(box));
        Assert.Null(MaterialTextField.GetLeadingIcon(box));
        Assert.Same(spinner, Slot(box, "Trailing").Content);
        Assert.True(spinner.GetVisualAncestors().Contains(box));
        var (decrease, increase) = Steppers(spinner);
        Assert.Equal(MaterialSymbolCodepoint.Remove, Assert.IsType<MaterialSymbol>(decrease.Content).Symbol);
        Assert.Equal(MaterialSymbolCodepoint.Add, Assert.IsType<MaterialSymbol>(increase.Content).Symbol);
        Assert.True(MaterialInteraction.GetIsInteractive(increase));
        Assert.False(increase.Focusable); Assert.False(decrease.Focusable);
        // Side-by-side standard icon buttons keep the 56 DIP field height.
        Assert.Equal(56, box.Bounds.Height);
        Assert.Equal(56, nud.Bounds.Height);
        Assert.Equal(48, increase.Bounds.Height);
        Assert.True(decrease.TranslatePoint(default, box)!.Value.X < increase.TranslatePoint(default, box)!.Value.X);
        window.Close();
    }

    [AvaloniaFact]
    public void Container_IsDrawnOnlyByTheTextFieldEvenWithFluent()
    {
        var nud = new NumericUpDown { Value = 5, Width = 280 };
        var reference = new TextBox { Width = 280 };
        var window = Show(nud, reference, fluent: true);
        var box = TextBox(nud);
        Assert.Same(box, nud.GetVisualChildren().Single());
        Assert.Null(nud.Background); Assert.Null(nud.BorderBrush);
        Assert.Equal(default, nud.BorderThickness);
        Assert.DoesNotContain(nud.GetVisualDescendants(), v => v is Border border && border.TemplatedParent == nud);
        Assert.DoesNotContain(nud.GetVisualDescendants(), v => v is DataValidationErrors);
        Assert.Equal(reference.Bounds.Size, box.Bounds.Size);

        // Pixels: edges and interior match a plain Material TextBox exactly (Fluent would add its
        // own border and background around the field).
        var nudOrigin = nud.TranslatePoint(default, window)!.Value;
        var refOrigin = reference.TranslatePoint(default, window)!.Value;
        using var frame = window.CaptureRenderedFrame()!;
        foreach (var (x, y) in new[] { (0, 28), (1, 28), (8, 8), (40, 50), (0, 54) })
        {
            Assert.Equal(PixelAt(frame, (int)refOrigin.X + x, (int)refOrigin.Y + y), PixelAt(frame, (int)nudOrigin.X + x, (int)nudOrigin.Y + y));
        }
        var container = Brush(reference.Background)!.Value;
        Assert.Equal((container.R, container.G, container.B), PixelAt(frame, (int)nudOrigin.X + 1, (int)nudOrigin.Y + 28));
        window.Close();
    }

    [AvaloniaFact]
    public void Steppers_SpinValueAndDisableAtBounds()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 3, Increment = 1, Value = 1 };
        var window = Show(nud);
        var (decrease, increase) = Steppers(Spinner(nud));
        increase.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Draw(window);
        Assert.Equal(2, nud.Value);
        increase.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Draw(window);
        Assert.Equal(3, nud.Value);
        Assert.False(increase.IsEnabled); Assert.True(decrease.IsEnabled);
        nud.Value = 0; Draw(window);
        Assert.True(increase.IsEnabled); Assert.False(decrease.IsEnabled);
        decrease.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Draw(window);
        Assert.Equal(0, nud.Value);
        nud.AllowSpin = false; nud.Value = 1; Draw(window);
        Assert.False(increase.IsEnabled); Assert.False(decrease.IsEnabled);
        window.Close();
    }

    [AvaloniaFact]
    public void Steppers_RespondToPointerClicks()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 10, Value = 4 };
        var window = Show(nud);
        var (decrease, increase) = Steppers(Spinner(nud));
        var box = TextBox(nud);
        box.Focus(); Draw(window);
        Click(window, increase); Click(window, increase); Click(window, decrease);
        Assert.Equal(5, nud.Value);
        // Steppers are not focusable, so focus (and the focused TextField look) stays in the editor.
        Assert.True(box.IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_UpDownSpinFromTheEditorAndRespectAllowSpinAndBounds()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 2, Value = 1 };
        var window = Show(nud);
        TextBox(nud).Focus(); Draw(window);
        window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null); Draw(window);
        Assert.Equal(2, nud.Value);
        window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null); Draw(window);
        Assert.Equal(2, nud.Value);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); Draw(window);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); Draw(window);
        Assert.Equal(0, nud.Value);
        nud.AllowSpin = false;
        window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null); Draw(window);
        Assert.Equal(0, nud.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void MouseWheel_SpinsOnlyWhileFocusedWithin()
    {
        var nud = new NumericUpDown { Minimum = 0, Maximum = 10, Value = 5 };
        var other = new Button { Content = "Other" };
        var window = Show(nud, other);
        var center = nud.TranslatePoint(new Point(60, 28), window)!.Value;
        other.Focus(); Draw(window);
        window.MouseWheel(center, new Vector(0, 1)); Draw(window);
        Assert.Equal(5, nud.Value);
        TextBox(nud).Focus(); Draw(window);
        window.MouseWheel(center, new Vector(0, 1)); Draw(window);
        Assert.Equal(6, nud.Value);
        // Over the steppers themselves too.
        var stepper = Steppers(Spinner(nud)).Decrease;
        window.MouseWheel(stepper.TranslatePoint(new Point(24, 24), window)!.Value, new Vector(0, -1)); Draw(window);
        Assert.Equal(5, nud.Value);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void ShowButtonSpinnerAndLocation_MoveOrRemoveTheSpinnerSlot(FlowDirection direction)
    {
        var icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Search };
        var nud = new NumericUpDown { Value = 1, FlowDirection = direction };
        MaterialTextField.SetLeadingIcon(nud, icon);
        var window = Show(nud);
        var box = TextBox(nud); var spinner = Spinner(nud);
        Assert.Same(icon, Slot(box, "Leading").Content);
        Assert.Same(spinner, Slot(box, "Trailing").Content);
        // Trailing is logical: the steppers sit at the far end in LTR, the near end in RTL.
        var x = spinner.TranslatePoint(default, window)!.Value.X - nud.TranslatePoint(default, window)!.Value.X;
        if (direction == FlowDirection.LeftToRight) Assert.True(x > nud.Bounds.Width / 2); else Assert.True(x < nud.Bounds.Width / 2);

        nud.ButtonSpinnerLocation = Location.Left; Draw(window);
        Assert.Same(spinner, Slot(box, "Leading").Content);
        Assert.Null(Slot(box, "Trailing").Content);
        Assert.Contains(":left", spinner.Classes);

        nud.ShowButtonSpinner = false; Draw(window);
        Assert.Same(icon, Slot(box, "Leading").Content);
        Assert.Null(Slot(box, "Trailing").Content);
        Assert.Null(spinner.GetVisualParent());

        nud.InnerRightContent = new TextBlock { Text = "kg" }; Draw(window);
        Assert.IsType<TextBlock>(Slot(box, "Trailing").Content);
        nud.ShowButtonSpinner = true; nud.ButtonSpinnerLocation = Location.Right; Draw(window);
        Assert.Same(spinner, Slot(box, "Trailing").Content);
        Assert.Same(icon, Slot(box, "Leading").Content);
        window.Close();
    }

    [AvaloniaFact]
    public void MaterialTextFieldAndNativeProperties_ForwardToTheInnerTextBox()
    {
        var nud = new NumericUpDown { Value = 3, PlaceholderText = "Qty", IsReadOnly = true, TextAlignment = TextAlignment.Right };
        MaterialTextField.SetLabel(nud, "Quantity");
        MaterialTextField.SetSupportingText(nud, "Whole numbers");
        MaterialTextField.SetPrefix(nud, "#");
        MaterialTextField.SetSuffix(nud, "pcs");
        var left = new TextBlock { Text = "L" };
        nud.InnerLeftContent = left;
        var window = Show(nud);
        var box = TextBox(nud);
        Assert.Equal("Quantity", MaterialTextField.GetLabel(box));
        Assert.Equal("Whole numbers", MaterialTextField.GetSupportingText(box));
        Assert.Equal("#", MaterialTextField.GetPrefix(box));
        Assert.Equal("pcs", MaterialTextField.GetSuffix(box));
        Assert.Equal("Qty", box.PlaceholderText);
        Assert.True(box.IsReadOnly);
        Assert.Equal(TextAlignment.Right, box.TextAlignment);
        Assert.Same(left, box.InnerLeftContent);
        Assert.Same(left, Slot(box, "Leading").Content);
        Assert.Equal("3", box.Text);
        // IsReadOnly also removes both spin directions (NumericUpDown.SetValidSpinDirection).
        var (decrease, increase) = Steppers(Spinner(nud));
        Assert.False(increase.IsEnabled); Assert.False(decrease.IsEnabled);

        MaterialTextField.SetLabel(nud, "Count"); Draw(window);
        Assert.Equal("Count", MaterialTextField.GetLabel(box));
        Assert.Contains(box.GetVisualDescendants().OfType<ContentPresenter>(), p => p.Name == "Label" && Equals(p.Content, "Count"));
        window.Close();
    }

    [AvaloniaFact]
    public void ErrorLook_FollowsIsErrorAndDataValidationErrors()
    {
        var nud = new NumericUpDown { Value = 3 };
        var errorReference = new TextBox();
        MaterialTextField.SetIsError(errorReference, true);
        var window = Show(nud, errorReference);
        var box = TextBox(nud);
        var normal = Brush(box.BorderBrush);
        Assert.False(MaterialTextField.GetIsError(box));

        MaterialTextField.SetIsError(nud, true); Draw(window);
        Assert.True(MaterialTextField.GetIsError(box));
        Assert.Equal(Brush(errorReference.BorderBrush), Brush(box.BorderBrush));
        Assert.Equal(Brush(errorReference.Foreground), Brush(box.Foreground));
        MaterialTextField.SetIsError(nud, false); Draw(window);
        Assert.Equal(normal, Brush(box.BorderBrush));

        DataValidationErrors.SetError(nud, new InvalidOperationException("Too large")); Draw(window);
        Assert.True(DataValidationErrors.GetHasErrors(nud));
        Assert.True(MaterialTextField.GetIsError(box));
        Assert.Equal(Brush(errorReference.BorderBrush), Brush(box.BorderBrush));
        // Error colors only: no message text is added.
        Assert.DoesNotContain(nud.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Too large");
        DataValidationErrors.ClearErrors(nud); Draw(window);
        Assert.False(MaterialTextField.GetIsError(box));
        window.Close();
    }

    [AvaloniaFact]
    public void OutlinedVariant_UsesOutlinedTextField()
    {
        var nud = new NumericUpDown { Value = 1 };
        var window = Show(nud);
        nud.Theme = (ControlTheme)window.FindResource("MaterialOutlinedNumericUpDownTheme")!; Draw(window);
        var box = TextBox(nud);
        Assert.True(MaterialTextField.GetOutlined(box));
        Assert.Equal(TextFieldLabelPosition.Cutout, MaterialTextField.GetLabelPosition(box));
        Assert.Same(window.FindResource("MaterialOutlinedTextFieldTheme"), box.Theme);
        Assert.Same(Spinner(nud), Slot(box, "Trailing").Content);
        Assert.Null(nud.Background);
        nud.Theme = null; Draw(window);
        Assert.False(MaterialTextField.GetOutlined(TextBox(nud)));
        window.Close();
    }

    [AvaloniaFact]
    public void DisabledAndDarkMode_FollowTheTextField()
    {
        var nud = new NumericUpDown { Value = 1 };
        var reference = new TextBox();
        var window = Show(nud, reference);
        var box = TextBox(nud);
        window.RequestedThemeVariant = ThemeVariant.Dark; Draw(window);
        Assert.Equal(Brush(reference.Background), Brush(box.Background));
        Assert.Equal(Brush(reference.Foreground), Brush(box.Foreground));
        nud.IsEnabled = false; reference.IsEnabled = false; Draw(window);
        Assert.Equal(Brush(reference.Background), Brush(box.Background));
        Assert.Equal(Brush(reference.BorderBrush), Brush(box.BorderBrush));
        // The trailing slot (and so the steppers' inherited icon color) uses the disabled icon color.
        var increase = Steppers(Spinner(nud)).Increase;
        Assert.Equal(Brush(Slot(box, "Trailing").Foreground), Brush(increase.Foreground));
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Location.Right)]
    [InlineData(Location.Left)]
    public void StandaloneButtonSpinner_UsesOutlinedSurfaceAndSpins(Location location)
    {
        var spinner = new ButtonSpinner { Content = "Item", ButtonSpinnerLocation = location, Width = 240 };
        var spins = new List<SpinDirection>();
        spinner.Spin += (_, e) => spins.Add(e.Direction);
        var window = Show(spinner);
        var surface = spinner.GetTemplateDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_Container");
        Assert.Equal(new CornerRadius(4), spinner.CornerRadius);
        Assert.Equal(new Thickness(1), surface.BorderThickness);
        Assert.Equal(56, spinner.Bounds.Height);
        var (decrease, increase) = Steppers(spinner);
        Assert.Equal(location == Location.Left, increase.TranslatePoint(default, spinner)!.Value.X < 120);
        Assert.Equal(location == Location.Left, spinner.Classes.Contains(":left"));
        increase.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        decrease.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal([SpinDirection.Increase, SpinDirection.Decrease], spins);
        spinner.ValidSpinDirection = ValidSpinDirections.Decrease;
        Assert.False(increase.IsEnabled); Assert.True(decrease.IsEnabled);
        spinner.ShowButtonSpinner = false; Draw(window);
        Assert.False(increase.IsEffectivelyVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void FluentControlsOutsideTheSpinner_KeepTheirThemes()
    {
        // A plain RepeatButton is themed by RepeatButtonThemeTests; the pickers' keyed ones stay Fluent.
        var date = new DatePicker();
        var time = new TimePicker();
        var window = Show(new StackPanel { Children = { date, time } }, fluent: true);
        Assert.All(time.GetVisualDescendants().OfType<RepeatButton>(), b => Assert.False(MaterialInteraction.GetIsInteractive(b)));
        Assert.True(date.Bounds.Width > 0); Assert.True(time.Bounds.Width > 0);
        Assert.DoesNotContain(date.GetVisualDescendants(), v => v is ButtonSpinner);
        window.Close();
    }

    [AvaloniaFact]
    public void TextBoxThemeSwitch_RehostsIconControls()
    {
        // TextFieldLayout releases icon controls on detach so a replacement template can host them.
        var leading = new Border { Width = 24, Height = 24 };
        var trailing = new Border { Width = 24, Height = 24 };
        var box = new TextBox();
        MaterialTextField.SetLeadingIcon(box, leading);
        MaterialTextField.SetTrailingIcon(box, trailing);
        var window = Show(box);
        box.Theme = (ControlTheme)window.FindResource("MaterialOutlinedTextFieldTheme")!; Draw(window);
        Assert.Same(leading, Slot(box, "Leading").Content);
        Assert.Same(trailing, Slot(box, "Trailing").Content);
        box.Theme = null; Draw(window);
        Assert.Same(trailing, Slot(box, "Trailing").Content);
        window.Close();
    }

    static TextBox TextBox(NumericUpDown nud) => nud.GetTemplateDescendants().OfType<TextBox>().Single(t => t.Name == "PART_TextBox");
    static ButtonSpinner Spinner(NumericUpDown nud) => nud.GetVisualDescendants().OfType<ButtonSpinner>().Single(s => s.Name == "PART_Spinner");
    static ContentPresenter Slot(TextBox box, string name) => box.GetTemplateDescendants().OfType<ContentPresenter>().Single(p => p.Name == name);
    static (RepeatButton Decrease, RepeatButton Increase) Steppers(ButtonSpinner spinner)
    {
        var buttons = spinner.GetTemplateDescendants().OfType<RepeatButton>().ToList();
        return (buttons.Single(b => b.Name == "PART_DecreaseButton"), buttons.Single(b => b.Name == "PART_IncreaseButton"));
    }
    static Color? Brush(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    static void Click(Window window, Control target)
    {
        var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
        Draw(window);
    }

    static (int R, int G, int B) PixelAt(Avalonia.Media.Imaging.WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var row = new byte[buffer.RowBytes];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, buffer.RowBytes);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (row[x * 4], row[x * 4 + 1], row[x * 4 + 2])
            : (row[x * 4 + 2], row[x * 4 + 1], row[x * 4]);
    }

    static AnimationTestWindow Show(Control control, Control? sibling = null, bool fluent = false)
    {
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(clock) { Width = 480, Height = 320 };
        if (fluent) window.Styles.Add(new FluentTheme());
        window.Styles.Add(new MaterialTheme());
        control.HorizontalAlignment = HorizontalAlignment.Left;
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 16, Children = { control } };
        if (sibling is not null) panel.Children.Add(sibling);
        window.Content = panel;
        window.Show(); Draw(window); return window;
    }

    static void Draw(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
