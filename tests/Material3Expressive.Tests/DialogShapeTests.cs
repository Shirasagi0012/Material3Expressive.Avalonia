using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Overlays;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class DialogShapeTests
{
    [AvaloniaTheory]
    [InlineData("alert")] [InlineData("date")] [InlineData("time")] [InlineData("vibrant")]
    public void DialogOutlineTracksConfigurationAndReopenWithoutTakingOwnershipOfActions(string kind)
    {
        using var clock = new MotionTestClockScope();
        var confirm = new Button { Content = "OK" };
        Dialog dialog = kind switch
        {
            "alert" => new AlertDialog { ConfirmButton = confirm },
            "date" => new DatePickerDialog { ConfirmButton = confirm },
            "time" => new TimePickerDialog { ConfirmButton = confirm, Title = new Border { Width = 100, Height = 24 } },
            _ => new VibrantTimePickerDialog { ConfirmButton = confirm }
        };
        void Shape(MaterialShape? value)
        {
            switch (dialog) { case AlertDialog a: a.Shape = value; break; case DatePickerDialog d: d.Shape = value; break;
                case TimePickerDialog t: t.Shape = value; break; case VibrantTimePickerDialog v: v.Shape = value; break; }
        }
        var shape = new CutCornerShape(24); Shape(shape);
        dialog.Content = new Border { Width = 180, Height = 100, Background = Brushes.Blue };
        dialog.IsOpen = true; dialog.AnimateTransition = false;
        var window = DialogTests.Show(new Grid { Children = { dialog } }, 640);
        MaterialSurface Surface() => window.GetVisualDescendants().OfType<MaterialSurface>().Single(x => x.Name == "PART_Container" && x.TemplatedParent == dialog);
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        try
        {
            Assert.Same(shape, Surface().Shape); Assert.False(Surface().ContainerGeometry!.FillContains(new(1, 1)));
            var size = Surface().Bounds.Size; shape.TopStart = new(0, RelativeUnit.Absolute);
            Assert.True(Surface().ContainerGeometry!.FillContains(new(1, 1))); Assert.Equal(size, Surface().Bounds.Size);
            var clicks = 0; confirm.Click += (_, _) => clicks++;
            confirm.Focus(NavigationMethod.Tab); window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Assert.Equal(1, clicks); Assert.True(dialog.IsOpen);
            dialog.AnimateTransition = true; dialog.IsOpen = false; Frames(3);
            Shape(new RoundedCornerShape(new RelativeScalar(.25, RelativeUnit.Relative)));
            dialog.IsOpen = true; Frames(45);
            Assert.Null(Surface().ContainerGeometry);
            Assert.Equal(Math.Min(Surface().Bounds.Width, Surface().Bounds.Height) * .25, Surface().EffectiveCornerRadius.TopLeft);
            Shape(null); dialog.CornerRadius = new CornerRadius(7); Assert.Equal(new CornerRadius(7), Surface().EffectiveCornerRadius);
            dialog.IsOpen = false; Frames(45); Assert.False(dialog.IsPresented);
            Shape(shape); dialog.IsOpen = true; Frames(45); Assert.Same(shape, Surface().Shape);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
}
