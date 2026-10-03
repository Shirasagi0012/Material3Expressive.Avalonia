using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Material3Expressive.Foundation.Interaction;
using Avalonia.Threading;
using Xunit;

namespace Material3Expressive.Tests;

public class ChipTests
{
    [AvaloniaTheory]
    [InlineData("assist", 64)]
    [InlineData("suggestion", 64)]
    [InlineData("filter", 64)]
    [InlineData("input", 56)]
    public void Defaults_SeparateContainerAndInteractiveHeight(string kind, double width)
    {
        Chip chip = kind switch { "assist" => new AssistChip(), "suggestion" => new SuggestionChip(), "filter" => new FilterChip(), _ => new InputChip() };
        chip.Content = new Border { Width = 32, Height = 16 };
        var window = Show(chip);
        try
        {
            Assert.Equal(new Size(width, 48), chip.Bounds.Size);
            Assert.Equal(new Size(width, 32), Part<MaterialSurface>(chip, "PART_Container").Bounds.Size);
            Assert.Equal(new CornerRadius(8), chip.CornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void InputAvatar_TakesPrecedenceAndDisabledOpacityIsLocalToAvatar()
    {
        var avatar = new Border { Width = 24, Height = 24, Background = Brushes.Red };
        var icon = new Border { Width = 18, Height = 18 };
        var chip = new InputChip { Content = new Border { Width = 32, Height = 16 }, Avatar = avatar, LeadingIcon = icon, IsEnabled = false };
        var window = Show(chip);
        try
        {
            var slot = Part<ChipIconSlot>(chip, "PART_LeadingIcon");
            var border = Assert.IsType<Border>(slot.Child);
            Assert.Equal(0.38, border.Opacity); Assert.True(border.ClipToBounds);
            Assert.Same(avatar, Assert.IsType<ContentPresenter>(border.Child).Content);
            Assert.Equal(80, chip.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ActivationAndAutomation_RequestSelectionWithoutToggling()
    {
        var chip = new FilterChip { Content = "Selected externally" };
        var window = Show(chip); var clicks = 0; chip.Click += (_, _) => clicks++;
        try
        {
            var center = chip.TranslatePoint(new Point(chip.Bounds.Width / 2, 24), window)!.Value;
            window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
            Assert.Equal(1, clicks); Assert.False(chip.IsSelected);
            var peer = ControlAutomationPeer.CreatePeerForElement(chip)!;
            Assert.Equal(AutomationControlType.CheckBox, peer.GetAutomationControlType());
            var toggle = Assert.IsAssignableFrom<IToggleProvider>(peer);
            toggle.Toggle(); Assert.Equal(2, clicks); Assert.False(chip.IsSelected);
            var changes = new List<(ToggleState Old, ToggleState New)>();
            peer.PropertyChanged += (_, change) =>
            {
                if (change.Property == TogglePatternIdentifiers.ToggleStateProperty)
                    changes.Add(((ToggleState)change.OldValue!, (ToggleState)change.NewValue!));
            };
            chip.IsSelected = true; Assert.Equal(ToggleState.On, toggle.ToggleState);
            Assert.Equal([(ToggleState.Off, ToggleState.On)], changes);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void StateColorsAndTokenOverrides_UpdateWithoutReplacingCallerPaint()
    {
        var chip = new FilterChip { Content = new Border { Width = 32, Height = 16 } };
        var window = Show(chip);
        try
        {
            var color = Color.Parse("#126E60");
            chip.Resources[MaterialTokens.Sys.Color.SecondaryContainer] = color;
            chip.IsSelected = true;
            Assert.Equal(color, Assert.IsType<SolidColorBrush>(chip.Background).Color);
            Assert.Equal(new Thickness(0), chip.BorderThickness);
            chip.Background = Brushes.Red;
            chip.IsEnabled = false;
            Assert.Same(Brushes.Red, chip.Background);
            Assert.Equal(97, Assert.IsType<SolidColorBrush>(chip.Foreground).Color.A);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SurfaceInk_FollowsBackgroundTokensAndOuterContentEnvironment()
    {
        var chip = new FilterChip { Content = "Label", Colors = new ChipColors { LabelColor = Colors.Red } };
        var window = Show(chip);
        try
        {
            var parent = (Grid)window.Content!;
            var surface = Part<MaterialSurface>(chip, "PART_Container");
            Avalonia.Controls.Documents.TextElement.SetForeground(parent, Brushes.Blue);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.Blue, Assert.IsAssignableFrom<ISolidColorBrush>(surface.StateLayerBrush).Color);
            chip.Background = new SolidColorBrush((Color)window.FindResource(MaterialTokens.Sys.Color.SecondaryContainer)!);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal((Color)window.FindResource(MaterialTokens.Sys.Color.OnSecondaryContainer)!, Assert.IsAssignableFrom<ISolidColorBrush>(surface.StateLayerBrush).Color);
            chip.Background = Brushes.Transparent;
            Avalonia.Controls.Documents.TextElement.SetForeground(parent, Brushes.Green);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.Green, Assert.IsAssignableFrom<ISolidColorBrush>(surface.StateLayerBrush).Color);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Configuration_IsUsableFromCompiledXaml()
    {
        var fixture = (StackPanel)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/ChipConfiguration.axaml"));
        var chip = Assert.IsType<FilterChip>(fixture.Children.Single());
        Assert.Equal(new CornerRadius(16), chip.Shapes!.SelectedShape);
        Assert.Equal(new ChipSpacing(4, 8), chip.Spacing);
        Assert.Equal(4, chip.Elevation!.Hovered);
        fixture.Children.Remove(chip);
        var window = Show(chip);
        try { Assert.Equal(Color.Parse("#126E60"), Assert.IsType<SolidColorBrush>(chip.Background).Color); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DetachingDuringShapeAndIconAnimation_CancelsAndReattachesAtCurrentState()
    {
        using var clock = new MotionTestClockScope();
        var chip = new FilterChip { Content = new Border { Width = 32, Height = 16 }, Shapes = new ChipShapes(), IsElevated = true };
        var window = Show(chip); var root = (Grid)window.Content!;
        var old = Part<MaterialSurface>(chip, "PART_Container");
        chip.IsSelected = true; chip.LeadingIcon = new Border { Width = 18, Height = 18 };
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(64));
        root.Children.Remove(chip); Dispatcher.UIThread.RunJobs();
        var oldCorner = old.CornerRadius;
        clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(oldCorner, old.CornerRadius); Assert.Equal(0, clock.PendingFrameCount);
        root.Children.Add(chip); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        Assert.Equal(78, chip.Bounds.Width); // compact leading gap in the shapes overload
        Assert.Equal(16, Part<MaterialSurface>(chip, "PART_Container").EffectiveCornerRadius.TopLeft);
        Assert.Null(MotionTestClockScope.Error(chip)); window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void Elevation_UsesLastActiveInteractionAndDisabledStateSnaps()
    {
        using var clock = new MotionTestClockScope();
        var chip = new AssistChip { Content = new Border { Width = 32, Height = 16 }, IsElevated = true };
        var window = Show(chip); var surface = Part<MaterialSurface>(chip, "PART_Container");
        var controller = MaterialInteraction.GetOrCreate(chip);
        controller.Set(InteractionState.Hovered, true);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(3, surface.ShadowElevation);
        controller.Set(InteractionState.Dragged, true);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(8, surface.ShadowElevation);
        controller.Set(InteractionState.Dragged, false);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(3, surface.ShadowElevation);
        chip.IsEnabled = false; clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, surface.ShadowElevation); window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FlatFilterHover_DefaultHasNoShadowAndRetainsStateLayerAndOverrides(bool morph, bool selected)
    {
        using var clock = new MotionTestClockScope();
        var chip = new FilterChip { Content = new Border { Width = 32, Height = 16 }, IsSelected = selected,
            Shapes = morph ? new ChipShapes() : null };
        var window = Show(chip);
        try
        {
            var surface = Part<MaterialSurface>(chip, "PART_Container");
            var center = chip.TranslatePoint(new Point(chip.Bounds.Width / 2, chip.Bounds.Height / 2), window)!.Value;
            window.MouseMove(center);
            void Settle()
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                clock.AdvanceBy(TimeSpan.FromMilliseconds(160));
            }
            Settle();
            Assert.Equal(0, surface.ShadowElevation);
            Assert.Equal(0.08, MaterialInteraction.GetOrCreate(chip).StateLayerOpacity);
            chip.Elevation = new ChipElevation(Hovered: 1); Settle();
            Assert.Equal(1, surface.ShadowElevation);
            chip.Elevation = null; Settle();
            Assert.Equal(0, surface.ShadowElevation);
            chip.Resources[MaterialTokens.Comp.Chip.FlatFilterHoverElevation] = 1d; Settle();
            Assert.Equal(1, surface.ShadowElevation);
            Assert.Equal(selected, chip.IsSelected);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SelectableWidthLimit_PreservesExplicitMinimumAndWidth()
    {
        var chip = new FilterChip { Content = new Border { Width = 1200, Height = 16 } };
        var window = Show(chip, 1400);
        try
        {
            Assert.Equal(1000, chip.Bounds.Width);
            chip.Width = 1200; window.UpdateLayout(); Assert.Equal(1200, chip.Bounds.Width);
            chip.Width = double.NaN; chip.MinWidth = 1100; window.UpdateLayout(); Assert.Equal(1100, chip.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CompletedSelectedShape_RecomputesItsOutlineWhenResized()
    {
        using var clock = new MotionTestClockScope();
        var chip = new FilterChip { Content = new Border { Width = 32, Height = 16 }, Shapes = new ChipShapes() };
        var window = Show(chip);
        chip.IsSelected = true;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromSeconds(1));
        var surface = Part<MaterialSurface>(chip, "PART_Container");
        Assert.Equal(16, surface.EffectiveCornerRadius.TopLeft);
        chip.Height = 64; window.UpdateLayout();
        Assert.Equal(32, surface.EffectiveCornerRadius.TopLeft);
        chip.Height = 24; window.UpdateLayout();
        Assert.Equal(12, surface.EffectiveCornerRadius.TopLeft);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ReplacingShapesDuringAnimation_CancelsTheOldEffectAndCanDisableMorphing()
    {
        using var clock = new MotionTestClockScope();
        var chip = new InputChip { Content = new Border { Width = 32, Height = 16 }, Shapes = new ChipShapes() };
        var window = Show(chip);
        chip.IsSelected = true;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(64));
        chip.Shapes = new ChipShapes { Shape = new CornerRadius(6), SelectedShape = new CornerRadius(14) };
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(14, Part<MaterialSurface>(chip, "PART_Container").EffectiveCornerRadius.TopLeft);
        chip.IsSelected = false;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        var before = Part<MaterialSurface>(chip, "PART_Container").EffectiveCornerRadius.TopLeft;
        Assert.InRange(before, 6.01, 13.99);
        chip.Shapes = new ChipShapes { Shape = new CornerRadius(6), SelectedShape = new CornerRadius(14) };
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.InRange(Part<MaterialSurface>(chip, "PART_Container").EffectiveCornerRadius.TopLeft, 6.01, before);
        chip.Shapes = null; clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(8, Part<MaterialSurface>(chip, "PART_Container").EffectiveCornerRadius.TopLeft);
        Assert.Null(MotionTestClockScope.Error(chip));
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ReplacingTemplateDuringAnimation_ReleasesOldSlotsAndMotion()
    {
        using var clock = new MotionTestClockScope();
        var icon = new Border { Width = 18, Height = 18 };
        var chip = new FilterChip { Content = "Template", LeadingIcon = icon, Shapes = new ChipShapes() };
        var window = Show(chip);
        var old = Part<MaterialSurface>(chip, "PART_Container");
        chip.IsSelected = true;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        var template = chip.Template;
        chip.Template = null; window.UpdateLayout();
        chip.Template = template; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        Assert.NotSame(old, Part<MaterialSurface>(chip, "PART_Container"));
        var slot = Part<ChipIconSlot>(chip, "PART_LeadingIcon");
        Assert.Same(icon, ((ContentPresenter)((Border)slot.Child!).Child!).Content);
        Assert.Null(MotionTestClockScope.Error(chip));
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    private static T Part<T>(Control owner, string name) where T : Control => owner.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    private static Window Show(Control control, double width = 240)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = width, Height = 160 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); return window;
    }
}
