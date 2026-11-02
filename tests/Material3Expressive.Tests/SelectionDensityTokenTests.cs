using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class SelectionDensityTokenTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndicatorTokens_ReachCanvasSurfaceLayoutAndStandaloneHitArea_AndUpdate(bool radio)
    {
        using var clock = new MotionTestClockScope();
        ToggleButton control = radio ? new RadioButton() : new CheckBox();
        var sizeKey = radio ? MaterialTokens.Comp.RadioButton.IndicatorSize : MaterialTokens.Comp.CheckBox.IndicatorSize;
        var paddingKey = radio ? MaterialTokens.Comp.RadioButton.IndicatorPadding : MaterialTokens.Comp.CheckBox.IndicatorPadding;
        control.Resources[sizeKey] = 32d;
        control.Resources[paddingKey] = new Thickness(3, 5, 7, 9);
        var window = Show(control);
        try
        {
            Verify(32, new Thickness(3, 5, 7, 9), new Size(48, 48));
            control.Resources[sizeKey] = 56d;
            control.Resources[paddingKey] = new Thickness(4, 6, 8, 10);
            window.UpdateLayout();
            Verify(56, new Thickness(4, 6, 8, 10), new Size(68, 72));
        }
        finally { window.Close(); }

        void Verify(double size, Thickness padding, Size expectedLayout)
        {
            var indicator = control.GetVisualDescendants().OfType<Control>().Single(x => x is CheckBoxIndicator or RadioButtonIndicator);
            var surface = control.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(new Size(size, size), indicator.Bounds.Size);
            Assert.Equal(padding, surface.Padding);
            Assert.Equal(new Size(size + padding.Left + padding.Right, size + padding.Top + padding.Bottom), surface.Bounds.Size);
            Assert.Equal(expectedLayout, control.Bounds.Size);
            Assert.Equal(new Point(padding.Left, padding.Top), indicator.TranslatePoint(default, surface)!.Value);
            // This point is in the expanded surface padding and outside the old 24-DIP hit square.
            var point = surface.TranslatePoint(new Point(1, 1), window)!.Value;
            var clicks = 0;
            control.Click += Count;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            control.Click -= Count;
            Assert.Equal(1, clicks);
            void Count(object? sender, Avalonia.Interactivity.RoutedEventArgs args) => clicks++;
        }
    }

    [AvaloniaTheory]
    [InlineData(false, Orientation.Horizontal)]
    [InlineData(false, Orientation.Vertical)]
    [InlineData(true, Orientation.Horizontal)]
    public void SliderTokens_ReachActualTemplateMeasurementConstraints_AndUpdate(bool range, Orientation orientation)
    {
        using var clock = new MotionTestClockScope();
        Control control = range ? new RangeSlider() : new Slider { Orientation = orientation };
        control.Resources[MaterialTokens.Comp.Slider.MinimumMeasureLength] = 30d;
        control.Resources[MaterialTokens.Comp.Slider.MinimumMeasureThickness] = 50d;
        var window = Show(control);
        try
        {
            var layout = control.GetVisualDescendants().OfType<Panel>().Single(x => x is MaterialSliderLayout or SliderLayout);
            var measuredChild = new MeasureProbe();
            layout.Children.Clear();
            if (range)
            {
                layout.Children.Add(new MeasureProbe());
                layout.Children.Add(measuredChild);
                layout.Children.Add(new MeasureProbe());
            }
            else
            {
                layout.Children.Add(new MeasureProbe());
                layout.Children.Add(measuredChild);
            }
            Verify(30, 50);
            control.Resources[MaterialTokens.Comp.Slider.MinimumMeasureLength] = 60d;
            control.Resources[MaterialTokens.Comp.Slider.MinimumMeasureThickness] = 70d;
            Verify(60, 70);

            void Verify(double length, double thickness)
            {
                measuredChild.InvalidateMeasure();
                layout.InvalidateMeasure();
                layout.Measure(new Size(2, 3));
                Assert.Equal(orientation == Orientation.Vertical ? new Size(thickness, length) : new Size(length, thickness), measuredChild.Available);
                // Preserve the final clamp to the caller's constraints after expanded child measurement.
                Assert.Equal(new Size(2, 3), layout.DesiredSize);
            }
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 300, Height = 200 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } };
        window.Show();
        window.UpdateLayout();
        return window;
    }

    private sealed class MeasureProbe : Control
    {
        public Size Available { get; private set; }
        protected override Size MeasureOverride(Size availableSize)
        {
            Available = availableSize;
            return default;
        }
    }
}
