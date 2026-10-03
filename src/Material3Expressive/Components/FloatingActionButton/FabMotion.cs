using Avalonia.Controls;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Components;

internal static class FabMotion
{
    internal static SpringSpec<float> Spring(Control owner, MaterialTokenKey<Foundation.Motion.SpringSpec> key,
        Foundation.Motion.SpringSpec fallback, float threshold = 0.01f)
    {
        var spec = TokenResolver.TryResolve(owner, key, owner.ActualThemeVariant, out var value) && value is Foundation.Motion.SpringSpec resolved ? resolved : fallback;
        // Compose's SpringSpec stores both parameters as Float before simulation.
        return new SpringSpec<float>((float)spec.DampingRatio, (float)spec.Stiffness, threshold);
    }
}
