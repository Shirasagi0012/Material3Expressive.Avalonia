// Executable reference: Compose Multiplatform Material3 1.5.0-alpha22 OutlinedTextField,
// matching AndroidX ff0a0a99 / Material3 1.5.0-alpha25 DateInput.kt:116-250.

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class PickerInputParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        new VisualCase(
            "picker-input/date/populated/light",
            new PixelSize(280, 96),
            VisualTheme.Light,
            _ => new Grid
            {
                Children =
                {
                    new MaterialDateInput
                    {
                        Name = "component",
                        Label = "Date",
                        State = new MaterialDatePickerState(new DateTime(2024, 7, 4)),
                        Culture = CultureInfo.GetCultureInfo("en-US"),
                        DateFormat = "MM/dd/yyyy",
                        Width = 240,
                        Height = 64,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            },
            RenderScaling: 1,
            Prepare: context =>
            {
                var editor = context.Require<TextBox>("PART_TextBox");
                editor.CaretBlinkInterval = TimeSpan.Zero;
                if (editor.IsFocused)
                    throw new InvalidOperationException("Date input unexpectedly acquired focus.");
                context.SettleAnimations();
            })
    ];
}
