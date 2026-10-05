using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;
using MaterialSymbols;
using Xunit;

namespace Material3Expressive.Tests;

public class IconSlotTests
{
    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 800, Height = 400 };
        window.Styles.Add(new FluentTheme());
        window.Styles.Add(new MaterialTheme());
        window.Content = new Grid { Children = { content }, HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top };
        window.Show();
        window.UpdateLayout();
        return window;
    }

    private static IDataTemplate SymbolTemplate() => new FuncDataTemplate<object?>((_, _) =>
        new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Home }, supportsRecycling: false);

    [AvaloniaFact]
    public void UnifiedAndCompatibilityEntrances_CompileInXaml()
    {
        var panel = (StackPanel)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/IconSlots.axaml"));
        Assert.IsType<MaterialSymbol>(MaterialButton.GetIcon((Button)panel.Children[0]));
        Assert.IsType<MaterialSymbol>(MaterialButton.GetIcon((Button)panel.Children[1]));
        Assert.IsType<MaterialSymbol>(MaterialButton.GetIcon((Button)panel.Children[2]));
        Assert.IsType<MaterialSymbol>(((FloatingActionButton)panel.Children[3]).Icon);
        Assert.IsType<MaterialSymbol>(((ExtendedFloatingActionButton)panel.Children[4]).Icon);
        Assert.IsType<MaterialSymbol>(((ToggleFloatingActionButton)panel.Children[5]).Icon);
        Assert.IsType<MaterialSymbol>(MaterialTextField.GetLeadingIcon((TextBox)panel.Children[6]));
        Assert.IsType<MaterialSymbol>(MaterialTextField.GetTrailingIcon((TextBox)panel.Children[6]));
        Assert.IsType<MaterialSymbol>(SwitchAppearance.GetThumbIcon((ToggleSwitch)panel.Children[7]));
        Assert.IsType<MaterialSymbol>(SwitchAppearance.GetThumbIcon((ToggleSwitch)panel.Children[8]));
    }

    [AvaloniaTheory]
    [InlineData("", 20)]
    [InlineData("xsmall", 20)]
    [InlineData("medium", 24)]
    [InlineData("large", 32)]
    [InlineData("xlarge", 40)]
    public void NativeButtonAndToggle_TemplateOnlyIconsFollowSizeClass(string sizeClass, double expected)
    {
        foreach (Button button in new Button[] { new(), new ToggleButton() })
        {
            button.Content = "Label";
            if (sizeClass.Length > 0) button.Classes.Add(sizeClass);
            MaterialButton.SetIconTemplate(button, SymbolTemplate());
            var window = Show(button);
            try
            {
                var symbol = button.GetVisualDescendants().OfType<MaterialSymbol>().Single();
                Assert.Equal(new Size(expected, expected), symbol.Bounds.Size);
                Assert.Equal(MaterialButton.IconProperty, ToggleButtonAppearance.IconProperty);
                Assert.Equal(MaterialButton.IconTemplateProperty, ToggleButtonAppearance.IconTemplateProperty);
                Assert.Same(MaterialButton.IconProperty,
                    AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(ToggleButtonAppearance), "Icon"));
                Assert.Same(MaterialButton.IconTemplateProperty,
                    AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(ToggleButtonAppearance), "IconTemplate"));
            }
            finally { window.Close(); }
        }
    }

    [AvaloniaTheory]
    [InlineData(FloatingActionButtonSize.Default, 24)]
    [InlineData(FloatingActionButtonSize.Small, 24)]
    [InlineData(FloatingActionButtonSize.Medium, 28)]
    [InlineData(FloatingActionButtonSize.Large, 36)]
    public void FabIconTemplate_UsesCorrectedSizesAndTakesPriorityOverContent(FloatingActionButtonSize size, double expected)
    {
        var fallback = new Border { Width = 10, Height = 10 };
        var fab = new FloatingActionButton { Size = size, Content = fallback, IconTemplate = SymbolTemplate() };
        var window = Show(fab);
        try
        {
            var symbol = fab.GetVisualDescendants().OfType<MaterialSymbol>().Single();
            Assert.Equal(new Size(expected, expected), symbol.Bounds.Size);
            Assert.DoesNotContain(fallback, fab.GetVisualDescendants());
            fab.IconTemplate = null;
            window.UpdateLayout();
            Assert.Contains(fallback, fab.GetVisualDescendants());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TextField_NewTemplateSlotHas24IconInside48Region_AndNullPairRestoresNativeFallback()
    {
        var fallback = new Border { Width = 12, Height = 12 };
        var box = new TextBox { InnerLeftContent = fallback };
        MaterialIcon.SetDefaultSize(box, 64);
        var window = Show(box);
        try
        {
            var leading = box.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "Leading");
            Assert.Same(fallback, leading.Content);
            MaterialTextField.SetLeadingIconTemplate(box, SymbolTemplate());
            window.UpdateLayout();
            var symbol = box.GetVisualDescendants().OfType<MaterialSymbol>().Single();
            Assert.Equal(new Size(24, 24), symbol.Bounds.Size);
            Assert.Equal(48, leading.Bounds.Width);
            MaterialTextField.SetLeadingIcon(box, null);
            MaterialTextField.SetLeadingIconTemplate(box, null);
            window.UpdateLayout();
            Assert.Same(fallback, leading.Content);
            Assert.Null(leading.ContentTemplate);
            box.ClearValue(MaterialTextField.LeadingIconProperty);
            box.ClearValue(MaterialTextField.LeadingIconTemplateProperty);
            window.UpdateLayout();
            Assert.Same(fallback, leading.Content);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Switch_ThumbIconTemplateGets16AndRetainsCompatibilityPropertyIdentity()
    {
        var box = new ToggleSwitch();
        SwitchAppearance.SetThumbIconTemplate(box, SymbolTemplate());
        var window = Show(box);
        try
        {
            Assert.Equal(new Size(16, 16), box.GetVisualDescendants().OfType<MaterialSymbol>().Single().Bounds.Size);
            Assert.Same(SwitchAppearance.ThumbIconProperty, SwitchAppearance.ThumbContentProperty);
            Assert.Same(SwitchAppearance.ThumbIconTemplateProperty, SwitchAppearance.ThumbContentTemplateProperty);
            Assert.Same(SwitchAppearance.ThumbIconProperty,
                AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(SwitchAppearance), "ThumbIcon"));
            Assert.Same(SwitchAppearance.ThumbIconTemplateProperty,
                AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(SwitchAppearance), "ThumbIconTemplate"));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TemplateOnlyChipDrawerAndTab_ArePresentWithComponentDefaults()
    {
        Control[] controls = [
            new AssistChip { Content = "Chip", LeadingIconTemplate = SymbolTemplate() },
            new NavigationDrawerItem { Content = "Drawer", IconTemplate = SymbolTemplate() },
            new Tab { Content = "Tab", IconTemplate = SymbolTemplate() }
        ];
        for (var i = 0; i < controls.Length; i++)
        {
            var window = Show(controls[i]);
            try
            {
                var symbol = controls[i].GetVisualDescendants().OfType<MaterialSymbol>().Single();
                Assert.True(symbol.IsEffectivelyVisible);
                Assert.Equal(new Size(i == 0 ? 18 : 24, i == 0 ? 18 : 24), symbol.Bounds.Size);
                if (controls[i] is Tab) Assert.Equal(72, controls[i].Bounds.Height);
            }
            finally { window.Close(); }
        }
    }

    [AvaloniaFact]
    public void Navigation_CustomExplicitIconSizeStillDrivesIndicatorGeometry()
    {
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Home, Width = 32, Height = 32 };
        var item = new NavigationRailItem { Icon = symbol, Label = "Home" };
        var window = Show(item);
        try
        {
            Assert.Equal(new Size(32, 32), symbol.Bounds.Size);
            Assert.Equal(64, item.GetVisualDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_IndicatorRipple").Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Button_DirectControlInheritsContext_WhileTemplateUsesIconDataIncludingNull()
    {
        var context = new object();
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Home };
        var button = new Button { Content = "Label", DataContext = context };
        MaterialButton.SetIcon(button, symbol);
        var window = Show(button);
        try
        {
            Assert.Same(context, symbol.DataContext);
            var data = new object();
            object? builtData = context;
            MaterialButton.SetIcon(button, data);
            MaterialButton.SetIconTemplate(button, new FuncDataTemplate<object?>((value, _) =>
            {
                builtData = value;
                return new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Search };
            }));
            window.UpdateLayout();
            Assert.Same(data, builtData);
            Assert.Same(data, button.GetVisualDescendants().OfType<MaterialSymbol>().Single().DataContext);
            MaterialButton.SetIcon(button, null);
            window.UpdateLayout();
            Assert.Null(builtData);
            Assert.Null(button.GetVisualDescendants().OfType<MaterialSymbol>().Single().DataContext);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("", 22)]
    [InlineData("medium", 26)]
    [InlineData("large", 38)]
    [InlineData("xlarge", 50)]
    public void SplitTrailingTheme_ConsumesSharedIconApiWithItsOwnSize(string sizeClass, double expected)
    {
        var leading = new Button { Content = "Save" };
        var trailing = new ToggleButton();
        if (sizeClass.Length > 0) { leading.Classes.Add(sizeClass); trailing.Classes.Add(sizeClass); }
        MaterialButton.SetIconTemplate(trailing, SymbolTemplate());
        var split = new Material3Expressive.Components.SplitButton { LeadingButton = leading, TrailingButton = trailing };
        var window = Show(split);
        try
        {
            leading.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonLeadingTheme")!;
            trailing.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonTrailingTheme")!;
            window.UpdateLayout();
            Assert.Equal(new Size(expected, expected), trailing.GetVisualDescendants().OfType<MaterialSymbol>().Single().Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SegmentedButton_TemplateOnlyActiveIconUses18()
    {
        var segment = new SegmentedButton { Content = "Day", IsSelected = true, ActiveIconTemplate = SymbolTemplate() };
        var row = new SingleChoiceSegmentedButtonRow();
        row.Items.Add(segment);
        var window = Show(row);
        try { Assert.Equal(new Size(18, 18), segment.GetVisualDescendants().OfType<MaterialSymbol>().Single().Bounds.Size); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void InputChip_TemplateOnlyAvatarTakesPriority_AndTemplateExitReleasesItsControl()
    {
        using var clock = new MotionTestClockScope();
        var leading = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Search };
        MaterialSymbol? avatar = null;
        var chip = new InputChip { Content = "Person", LeadingIcon = leading,
            AvatarTemplate = new FuncDataTemplate<object?>((_, _) => avatar = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Person }) };
        var window = Show(chip);
        try
        {
            Assert.NotNull(avatar);
            Assert.DoesNotContain(leading, chip.GetVisualDescendants());
            var shown = avatar!;
            chip.LeadingIcon = null;
            chip.AvatarTemplate = null;
            for (var i = 0; i < 80; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
            Assert.Null(shown.GetVisualParent());
            Assert.Empty(chip.GetVisualDescendants().OfType<MaterialSymbol>());
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void IconButton_SpecialSymbolApiIgnoresAmbientSizeHint()
    {
        var button = new IconButton { Symbol = MaterialSymbolCodepoint.Home };
        MaterialIcon.SetDefaultSize(button, 64);
        var window = Show(button);
        try { Assert.Equal(new Size(24, 24), button.GetVisualDescendants().OfType<MaterialSymbol>().Single().Bounds.Size); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void FabMenu_TemplateOnlyIconUsesItsOwnDefaultSize()
    {
        var item = new FloatingActionButtonMenuItem { Content = "Create", IconTemplate = SymbolTemplate() };
        var menu = new FloatingActionButtonMenu { Expanded = true, Items = { item } };
        MaterialIcon.SetDefaultSize(menu, 64);
        var window = Show(menu);
        try { Assert.Equal(new Size(24, 24), item.GetVisualDescendants().OfType<MaterialSymbol>().Single().Bounds.Size); }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ButtonIcon_IsInsidePadding_AndMirrorsWithItsLabel(bool rtl)
    {
        var label = new Border { Width = 32, Height = 16 };
        var icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Save };
        var button = new Button { Content = label, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MaterialButton.SetIcon(button, icon);
        var window = Show(button);
        try
        {
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_Container");
            // Compare physical rectangles in the window: local coordinates within the same RTL
            // subtree cancel its mirror transform and still read as logical LTR coordinates.
            double Left(Control control) => new Rect(control.Bounds.Size)
                .TransformToAABB(control.TransformToVisual(window)!.Value).Left;
            var surfaceLeft = Left(surface);
            var iconLeft = Left(icon) - surfaceLeft;
            var labelLeft = Left(label) - surfaceLeft;
            Assert.Equal(rtl ? surface.Bounds.Width - 16 - 20 : 16, iconLeft);
            Assert.Equal(rtl ? 16 : 44, labelLeft);
            Assert.Equal(new Thickness(16, 10), button.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ToggleFab_DefaultIconKeepsItsSizeDuringSelectionAnimation()
    {
        using var clock = new MotionTestClockScope();
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Menu };
        var fab = new ToggleFloatingActionButton { Icon = symbol };
        var window = Show(fab);
        try
        {
            Assert.Equal(new Size(24, 24), symbol.Bounds.Size);
            fab.IsChecked = true;
            for (var i = 0; i < 80; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
            Assert.Equal(20, fab.IconSize);
            Assert.Equal(new Size(24, 24), symbol.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TextField_CompleteIconButtonRetainsItsInteractiveAllocation()
    {
        var box = new TextBox();
        var button = new IconButton { Symbol = MaterialSymbolCodepoint.Visibility };
        MaterialTextField.SetTrailingIcon(box, button);
        var window = Show(box);
        try
        {
            Assert.Equal(new Size(48, 48), button.Bounds.Size);
            Assert.Equal(new Size(24, 24), button.GetVisualDescendants().OfType<MaterialSymbol>().Single().Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SplitTrailing_IconOnlyMatchesExistingCustomContentGeometry()
    {
        var oldGraphic = new Border { Width = 22, Height = 22 };
        var newGraphic = new Border { Width = 22, Height = 22 };
        var oldButton = new Button { Content = oldGraphic };
        var newButton = new Button();
        MaterialButton.SetIcon(newButton, newGraphic);
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Children = { oldButton, newButton } };
        var window = Show(panel);
        try
        {
            var theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonTrailingTheme")!;
            oldButton.Theme = newButton.Theme = theme;
            window.UpdateLayout();
            Assert.Equal(oldButton.Bounds.Size, newButton.Bounds.Size);
            Assert.Equal(oldGraphic.TranslatePoint(default, oldButton), newGraphic.TranslatePoint(default, newButton));
        }
        finally { window.Close(); }
    }
}
