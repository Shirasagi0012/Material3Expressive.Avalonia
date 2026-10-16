using Avalonia.Controls.Primitives;

namespace Material3Expressive.Components;

// Retain the native toggle's template and automation, but leave state to the descriptor.
internal sealed class ButtonGroupToggleButton : ToggleButton
{
    protected override Type StyleKeyOverride => typeof(ToggleButton);
    protected override void Toggle() { }
}
