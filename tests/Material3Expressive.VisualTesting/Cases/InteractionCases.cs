using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Stable final interaction states produced by the real Headless input pipeline.</summary>
internal static class InteractionCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        ButtonState("hovered", Hover),
        ButtonState("focused", Focus),
        ButtonState("pressed", Press),
        CheckboxHovered(),
        SliderPressed(),
        TextFieldFocused(),
        NavigationSelected()
    ];

    private static VisualCase ButtonState(
        string state,
        Action<VisualContext, MaterialButton> input) =>
        new(
            $"button/interaction/{state}/light",
            new PixelSize(360, 180),
            VisualTheme.Light,
            _ => Center(
                new MaterialButton
                {
                    Name = "target",
                    Content = state,
                    Variant = ButtonVariant.Filled,
                    Size = ButtonSize.Medium
                }),
            ComparisonProfile.Exact,
            ["full", "interaction", "button"],
            Prepare: context =>
            {
                input(context, context.Require<MaterialButton>("target"));
                context.SettleAnimations();
            });

    private static VisualCase CheckboxHovered() =>
        new(
            "selection/checkbox/interaction/hovered/light",
            new PixelSize(360, 180),
            VisualTheme.Light,
            _ => Center(
                new MaterialCheckbox
                {
                    Name = "target",
                    Content = "Hovered checkbox",
                    IsChecked = true
                }),
            ComparisonProfile.Exact,
            ["full", "interaction", "selection", "checkbox"],
            Prepare: context =>
            {
                var target = context.Require<MaterialCheckbox>("target");
                context.Window.MouseMove(PointIn(target, context.Window, 20, target.Bounds.Height / 2));
                context.SettleAnimations();
            });

    private static VisualCase SliderPressed() =>
        new(
            "selection/slider/interaction/pressed/light",
            new PixelSize(520, 180),
            VisualTheme.Light,
            _ => Center(
                new MaterialSlider
                {
                    Name = "target",
                    Width = 380,
                    Maximum = 1,
                    Value = 0.25
                }),
            ComparisonProfile.Exact,
            ["full", "interaction", "selection", "slider"],
            Prepare: context =>
            {
                var target = context.Require<MaterialSlider>("target");
                var point = PointIn(
                    target,
                    context.Window,
                    target.Bounds.Width * 0.7,
                    target.Bounds.Height / 2);
                context.Window.MouseMove(point);
                context.Window.MouseDown(point, MouseButton.Left);
                context.SettleAnimations();
            });

    private static VisualCase TextFieldFocused() =>
        new(
            "textfield/outlined/interaction/focused-empty/light",
            new PixelSize(420, 180),
            VisualTheme.Light,
            _ => Center(
                new MaterialTextField
                {
                    Name = "target",
                    Width = 280,
                    Label = "Focused field",
                    Placeholder = "Type here",
                    Variant = TextFieldVariant.Outlined
                }),
            ComparisonProfile.Exact,
            ["full", "interaction", "input", "textfield"],
            Prepare: context =>
            {
                var target = context.Require<MaterialTextField>("target");
                var point = PointIn(target, context.Window);
                context.Window.MouseMove(point);
                context.Window.MouseDown(point, MouseButton.Left);
                context.Window.MouseUp(point, MouseButton.Left);
                context.SettleAnimations();
            });

    private static VisualCase NavigationSelected() =>
        new(
            "navigation/bar/interaction/select-second/light",
            new PixelSize(520, 180),
            VisualTheme.Light,
            _ =>
            {
                var navigation = new MaterialNavigationBar
                {
                    Name = "navigation",
                    Width = 420,
                    Height = 80,
                    SelectedIndex = 0,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                navigation.Items.Add(Destination("One"));
                navigation.Items.Add(Destination("Two", "target"));
                navigation.Items.Add(Destination("Three"));
                return navigation;
            },
            ComparisonProfile.Exact,
            ["full", "interaction", "navigation"],
            Prepare: context =>
            {
                PressAndRelease(context.Window, Key.Tab, PhysicalKey.Tab);
                PressAndRelease(context.Window, Key.Tab, PhysicalKey.Tab);
                PressAndRelease(context.Window, Key.Space, PhysicalKey.Space);
                context.SettleAnimations();
                var navigation = context.Require<MaterialNavigationBar>("navigation");
                if (navigation.SelectedIndex != 1)
                {
                    throw new InvalidOperationException(
                        $"Keyboard input selected index {navigation.SelectedIndex}, expected 1.");
                }
            });

    private static void Hover(VisualContext context, MaterialButton target)
    {
        context.Window.MouseMove(PointIn(target, context.Window));
    }

    private static void Focus(VisualContext context, MaterialButton target)
    {
        _ = target;
        context.Window.KeyPress(
            Key.Tab,
            RawInputModifiers.None,
            PhysicalKey.Tab,
            null);
    }

    private static void Press(VisualContext context, MaterialButton target)
    {
        var point = PointIn(target, context.Window);
        context.Window.MouseMove(point);
        context.Window.MouseDown(point, MouseButton.Left);
        if (!target.IsPressed)
            throw new InvalidOperationException("Headless pointer input did not press the button.");
    }

    private static void PressAndRelease(TopLevel window, Key key, PhysicalKey physicalKey)
    {
        window.KeyPress(key, RawInputModifiers.None, physicalKey, null);
        window.KeyRelease(key, RawInputModifiers.None, physicalKey, null);
    }

    private static Point PointIn(
        Control target,
        Visual relativeTo,
        double? x = null,
        double? y = null) =>
        target.TranslatePoint(
            new Point(x ?? target.Bounds.Width / 2, y ?? target.Bounds.Height / 2),
            relativeTo)
        ?? throw new InvalidOperationException($"'{target.Name}' is not mounted in the visual tree.");

    private static Grid Center(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        return new Grid
        {
            Children = { control }
        };
    }

    private static MaterialNavigationItem Destination(string label, string? name = null) =>
        new()
        {
            Name = name,
            Icon = new Border { Width = 24, Height = 24 },
            Content = label
        };
}
