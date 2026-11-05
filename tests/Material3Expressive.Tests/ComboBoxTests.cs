using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Xunit;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;

namespace Material3Expressive.Tests;

public class ComboBoxTests
{
    [AvaloniaTheory]
    [InlineData("MaterialFilledComboBoxTheme")]
    [InlineData("MaterialOutlinedComboBoxTheme")]
    public void OpenMenuKeepsFocusedColorsAndErrorDisabledPrecedence(string theme)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        try
        {
            box.Theme = (ControlTheme)window.FindResource(theme)!;
            MaterialComboBox.SetLabel(box, "Choice");
            box.Items.Add("First");
            var other = new Button { Content = "Other" };
            ((StackPanel)window.Content!).Children.Add(other);
            window.UpdateLayout(); other.Focus(); Settle(clock, window);
            var field = box.GetVisualDescendants().OfType<ComboBoxField>().Single();
            var unfocused = Assert.IsAssignableFrom<ISolidColorBrush>(field.LabelBrush).Color;
            box.Focus(); Settle(clock, window);
            var focused = Assert.IsAssignableFrom<ISolidColorBrush>(field.LabelBrush).Color;
            Assert.NotEqual(unfocused, focused);
            box.IsDropDownOpen = true; Settle(clock, window);
            Assert.IsType<ComboBoxItem>(box.ContainerFromIndex(0)).Focus(); Settle(clock, window);
            Assert.False(box.IsFocused);
            Assert.Equal(focused, Assert.IsAssignableFrom<ISolidColorBrush>(field.LabelBrush).Color);
            MaterialComboBox.SetIsError(box, true); Settle(clock, window);
            Assert.Equal((Color)window.FindResource(Material3Expressive.Tokens.MaterialTokens.Sys.Color.Error)!,
                Assert.IsAssignableFrom<ISolidColorBrush>(field.LabelBrush).Color);
            box.IsEnabled = false; Settle(clock, window);
            Assert.Equal(97, Assert.IsAssignableFrom<ISolidColorBrush>(field.LabelBrush).Color.A);
            box.IsDropDownOpen = false; box.IsEnabled = true; MaterialComboBox.SetIsError(box, false);
            other.Focus(); Settle(clock, window);
            Assert.Equal(unfocused, Assert.IsAssignableFrom<ISolidColorBrush>(field.LabelBrush).Color);
            var arrow = box.GetVisualDescendants().OfType<Viewbox>().Single(control => control.Name == "DropDownArrow");
            Assert.Equal(0, Assert.IsType<RotateTransform>(arrow.RenderTransform).Angle);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionIcon_ReversesAndRestoresOpacityContributionOnDetach(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        try
        {
            box.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            box.Items.Add("First"); box.Items.Add("Second");
            box.IsDropDownOpen = true; window.UpdateLayout();
            var item = Assert.IsType<ComboBoxItem>(box.ContainerFromIndex(0));
            var icon = item.GetVisualDescendants().OfType<ComboBoxSelectionIcon>().Single();
            icon.Opacity = .37; // Keep a caller's value under the animation contribution.
            Assert.Equal(0, icon.Opacity);
            Assert.IsType<Viewbox>(icon.Child);
            Assert.Empty(icon.GetVisualDescendants().OfType<MaterialSymbol>());
            box.SelectedIndex = 0; Advance(64);
            Assert.True(box.IsDropDownOpen); Assert.True(item.IsSelected);
            Assert.InRange(icon.Bounds.Width, .01, 19.99); Assert.InRange(icon.Opacity, .01, .99);
            box.SelectedIndex = 1; Advance(32);
            box.SelectedIndex = 0; Advance(800);
            Assert.Equal(20, icon.Bounds.Width); Assert.Equal(1, icon.Opacity);
            box.IsDropDownOpen = false; window.UpdateLayout();
            Assert.Equal(.37, icon.Opacity);
            box.IsDropDownOpen = true; window.UpdateLayout();
            // Virtualization can recycle a different container when the popup reopens.
            item = Assert.IsType<ComboBoxItem>(box.ContainerFromIndex(0));
            icon = item.GetVisualDescendants().OfType<ComboBoxSelectionIcon>().Single();
            icon.Opacity = .37;
            Assert.Equal(1, icon.Opacity); Assert.Equal(20, icon.Bounds.Width);
            box.SelectedIndex = 1; Advance(800);
            Assert.Equal(1, box.SelectedIndex);
            Assert.False(item.IsSelected);
            Assert.Equal(0, icon.Opacity); Assert.Equal(0, icon.Bounds.Width);
            var chrome = item.GetVisualDescendants().OfType<MenuItemChrome>().Single();
            Assert.Equal(Assert.IsAssignableFrom<ISolidColorBrush>(item.Background).Color,
                Assert.IsAssignableFrom<ISolidColorBrush>(chrome.PaintedBrush).Color);
            window.Close(); Advance(16);
            Assert.Equal(.37, icon.Opacity);
            Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
            Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveTaskCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        void Advance(int ms)
        {
            for (var t = 0; t < ms; t += 16) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
        }
    }

    [AvaloniaFact]
    public void ImplicitThemeIsFilledAndOutlinedThemeCutsTheLabel()
    {
        var window = Show(out var filled);
        try
        {
            var filledTheme = (ControlTheme)window.FindResource("MaterialFilledComboBoxTheme")!;
            var implicitTheme = (ControlTheme)window.FindResource(typeof(ComboBox))!;
            Assert.Same(filledTheme, implicitTheme.BasedOn);
            Assert.False(MaterialComboBox.GetOutlined(filled));
            Assert.Equal(TextFieldLabelPosition.Inside, MaterialComboBox.GetLabelPosition(filled));

            var outlined = new ComboBox { Theme = (ControlTheme)window.FindResource("MaterialOutlinedComboBoxTheme")! };
            ((StackPanel)window.Content!).Children.Add(outlined);
            window.UpdateLayout();
            Assert.True(MaterialComboBox.GetOutlined(outlined));
            Assert.Equal(TextFieldLabelPosition.Cutout, MaterialComboBox.GetLabelPosition(outlined));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void OpenMenuUsesOverlayAndAssignsSingleGroupShapes()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        box.Items.Add("One");
        window.UpdateLayout();
        try
        {
            var popup = box.GetVisualDescendants().OfType<Popup>().Single(p => p.Name == "PART_Popup");
            Assert.True(popup.ShouldUseOverlayLayer);
            Assert.False(popup.WindowManagerAddShadowHint);
            box.IsDropDownOpen = true;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
            Assert.Contains(popup.Styles.OfType<Style>(), style =>
                style.Setters.OfType<Setter>().Any(setter => setter.Property == Visual.ClipToBoundsProperty && Equals(setter.Value, false)));
            var only = Assert.IsType<ComboBoxItem>(box.ContainerFromIndex(0));
            Assert.Null(MaterialComboBox.GetItemShapes(only)!.Shape);

            box.Items.Add("Two");
            box.Items.Add("Three");
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
            Assert.Equal(new CornerRadius(12, 12, 4, 4), ShapeAt(box, 0));
            Assert.Null(ShapeAt(box, 1));
            Assert.Equal(new CornerRadius(4, 4, 12, 12), ShapeAt(box, 2));
            var arrow = box.GetVisualDescendants().OfType<Viewbox>().Single(symbol => symbol.Name == "DropDownArrow");
            Assert.Equal(180, Assert.IsType<RotateTransform>(arrow.RenderTransform).Angle);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void EmptySelectionDoesNotMaterializeTheItemTemplate()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        box.ItemTemplate = new FuncDataTemplate<string>((_, _) => new Border { Width = 48, Height = 16 });
        box.Items.Add("One");
        box.Items.Add("Two");
        window.UpdateLayout();
        try
        {
            Assert.Empty(SelectionMarkers(window));
            box.IsDropDownOpen = true;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
            Assert.Empty(SelectionMarkers(window));
            Assert.Equal(2, MenuMarkers(window).Count());
            box.SelectedIndex = 0;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
            Assert.Single(SelectionMarkers(window));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SelectingWhileOpenDoesNotDropTheLabelToTheCenter()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        MaterialComboBox.SetLabel(box, "Required");
        box.Items.Add("One");
        box.Items.Add("Two");
        window.UpdateLayout();
        try
        {
            var field = box.GetVisualDescendants().OfType<ComboBoxField>().Single();
            Settle(clock, window);
            Assert.Equal(0, field.LabelProgress, precision: 2);

            box.Focus();
            Settle(clock, window);
            Assert.Equal(1, field.LabelProgress, precision: 2);

            box.IsDropDownOpen = true;
            Settle(clock, window);
            var item = Assert.IsType<ComboBoxItem>(box.ContainerFromIndex(0));
            item.Focus();
            Settle(clock, window);
            Assert.False(box.IsFocused);
            Assert.Equal(1, field.LabelProgress, precision: 2);

            box.SelectedIndex = 0;
            box.IsDropDownOpen = false;
            Settle(clock, window);
            Assert.Equal(1, field.LabelProgress, precision: 2);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void EditableTextBoxDoesNotNestTextFieldChrome()
    {
        var window = Show(out var box);
        box.IsEditable = true;
        window.UpdateLayout();
        try
        {
            var editor = box.GetVisualDescendants().OfType<TextBox>().Single(text => text.Name == "PART_EditableTextBox");
            Assert.Same(window.FindResource("MaterialComboBoxInputTheme"), editor.Theme);
            Assert.DoesNotContain(editor.GetVisualDescendants(), visual => visual is TextFieldLayout);
            var presenter = editor.GetVisualDescendants().OfType<TextPresenter>().Single(text => text.Name == "PART_TextPresenter");
            Assert.Equal(16, presenter.FontSize);
            Assert.Equal(FontWeight.Regular, presenter.FontWeight);
            Assert.Equal(0.5, presenter.LetterSpacing);
            Assert.Equal(24, presenter.LineHeight);
        }
        finally { window.Close(); }
    }

    private static void Settle(MotionTestClockScope clock, Window window)
    {
        clock.SendFrame(clock.Now);
        for (var i = 0; i < 40; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        window.UpdateLayout();
    }

    private static IEnumerable<Border> SelectionMarkers(Window window) =>
        window.GetVisualDescendants().OfType<Border>().Where(border => border.Width == 48 && !border.GetVisualAncestors().OfType<ComboBoxItem>().Any());

    private static IEnumerable<Border> MenuMarkers(Window window) =>
        window.GetVisualDescendants().OfType<Border>().Where(border => border.Width == 48 && border.GetVisualAncestors().OfType<ComboBoxItem>().Any());

    private static CornerRadius? ShapeAt(ComboBox box, int index) =>
        MaterialComboBox.GetItemShapes(Assert.IsType<ComboBoxItem>(box.ContainerFromIndex(index)))!.Shape;

    private static MotionTestWindow Show(out ComboBox box)
    {
        var window = new MotionTestWindow { Width = 420, Height = 280 };
        window.Styles.Add(new FluentTheme());
        window.Styles.Add(new MaterialTheme());
        box = new ComboBox { Width = 280 };
        window.Content = new StackPanel { Margin = new Thickness(24), Children = { box } };
        window.Show();
        window.UpdateLayout();
        return window;
    }
}
