using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class FieldDensityTokenTests
{
    [AvaloniaTheory]
    [InlineData("TextField", false)]
    [InlineData("TextField", true)]
    [InlineData("SecureTextField", false)]
    [InlineData("SecureTextField", true)]
    [InlineData("ComboBox", false)]
    [InlineData("ComboBox", true)]
    public void SharedFieldTokens_UpdateExistingSlotsAndPreservePublicPadding(string kind, bool outlined)
    {
        using var clock = new MotionTestClockScope();
        TemplatedControl field = kind == "ComboBox"
            ? new ComboBox { IsEditable = true }
            : new TextBox { FontFamily = FontManager.Current.DefaultFontFamily };
        var icon = new Border { Width = 20, Height = 20 };
        if (field is ComboBox combo)
        {
            MaterialComboBox.SetLeadingIcon(combo, icon);
            MaterialComboBox.SetSupportingText(combo, new Border { Width = 20, Height = 12 });
        }
        else
        {
            MaterialTextField.SetLeadingIcon(field, icon);
            MaterialTextField.SetSupportingText(field, new Border { Width = 20, Height = 12 });
        }
        field.Width = 400;
        var slotSize = new MaterialTokenKey<double>("test.field.icon-slot");
        var supportingPadding = new MaterialTokenKey<Thickness>("test.field.supporting-padding");
        field.Resources[Comp.TextField.IconSlotSize] = new TokenRef(slotSize);
        field.Resources[slotSize] = 48d;
        field.Resources[Comp.TextField.SupportingPadding] = new TokenRef(supportingPadding);
        field.Resources[supportingPadding] = new Thickness(16, 4, 16, 0);
        var window = DialogTests.Show(field, height: 400);
        try
        {
            field.Theme = (ControlTheme)window.FindResource($"Material{(outlined ? "Outlined" : "Filled")}{kind}Theme")!;
            Flush(window);
            var leading = Part<ContentPresenter>(field, "Leading");
            var editor = Part<Control>(field, "Editor");
            var supporting = Part<ContentPresenter>(field, "Supporting");
            Assert.Equal(48, leading.Bounds.Width);
            var editorStart = editor.Bounds.X;

            field.Resources[slotSize] = 72d;
            field.Resources[supportingPadding] = new Thickness(23, 9, 19, 2);
            field.Resources[Comp.TextField.ContentMinHeight] = 38d;
            Flush(window);

            Assert.Same(leading, Part<ContentPresenter>(field, "Leading"));
            Assert.Equal(72, leading.Bounds.Width);
            Assert.Equal(editorStart + 24, editor.Bounds.X);
            Assert.Equal(38, editor.MinHeight);
            Assert.Equal(new Thickness(23, 9, 19, 2), supporting.Padding);
            if (field is ComboBox)
                Assert.Equal(38, Part<TextBox>(field, "PART_EditableTextBox").MinHeight);

            var paddingStyle = new Style { Setters = { new Setter(TemplatedControl.PaddingProperty, new Thickness(7)) } };
            field.Styles.Add(paddingStyle);
            field.ApplyStyling();
            field.Resources[Comp.TextField.ContentPadding] = new Thickness(31, 17);
            Flush(window);
            Assert.Equal(new Thickness(7), field.Padding);
            field.Padding = new Thickness(9);
            field.Resources[Comp.TextField.ContentPadding] = new Thickness(33, 19);
            Flush(window);
            Assert.Equal(new Thickness(9), field.Padding);
            field.ClearValue(TemplatedControl.PaddingProperty);
            Flush(window);
            Assert.Equal(new Thickness(7), field.Padding);
            field.Styles.Remove(paddingStyle);
            Flush(window);
            Assert.Equal(new Thickness(33, 19), field.Padding);
            if (kind == "SecureTextField") Assert.True(MaterialSecureTextField.GetIsSecure((TextBox)field));
        }
        finally { window.Close(); }
    }

    private static T Part<T>(Control owner, string name) where T : Control =>
        owner.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
