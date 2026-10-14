using Avalonia;
using Avalonia.Media;
using ComposeAnimation;
using Material3Expressive.Foundation.Motion;

namespace Material3Expressive.Components;

/// <summary>
/// The four independent selection tracks used by navigation indicators.
/// Owners choose target colors and geometry; AnimatedState owns scheduling and interruption.
/// </summary>
internal sealed class SelectionIndicatorAnimation : IDisposable
{
    public AnimatedState<float> IndicatorOpacity { get; }
    public AnimatedState<float> IndicatorSize { get; }
    public AnimatedState<Color> IconColor { get; }
    public AnimatedState<Color> TextColor { get; }

    public SelectionIndicatorAnimation(Visual owner, bool isSelected, Color iconColor, Color textColor)
    {
        var selectionProgress = isSelected ? 1f : 0f;
        IndicatorOpacity = AnimatedState.Create(owner, selectionProgress, label: "Selection indicator opacity");
        IndicatorSize = AnimatedState.Create(owner, selectionProgress, label: "Selection indicator size");
        IconColor = AnimatedState.Create(owner, iconColor, label: "Selection icon color");
        TextColor = AnimatedState.Create(owner, textColor, label: "Selection text color");
    }

    public void UpdateTargets(
        bool isSelected,
        Color iconColor,
        Color textColor,
        SpringSpec effectsAnimation,
        SpringSpec sizeAnimation)
    {
        // Unresolved tokens must not submit a fallback spring. Existing runs retain their specs.
        if (effectsAnimation.Stiffness <= 0 || sizeAnimation.Stiffness <= 0)
            return;

        const float progressThreshold = 0.01f;
        var selectionProgress = isSelected ? 1f : 0f;
        var opacitySpec = new ComposeAnimation.Runtime.SpringSpec<float>(
            effectsAnimation.DampingRatio, effectsAnimation.Stiffness, progressThreshold);
        var sizeSpec = new ComposeAnimation.Runtime.SpringSpec<float>(
            sizeAnimation.DampingRatio, sizeAnimation.Stiffness, progressThreshold);
        var colorSpec = new ComposeAnimation.Runtime.SpringSpec<Color>(
            effectsAnimation.DampingRatio, effectsAnimation.Stiffness);

        IndicatorOpacity.UpdateTarget(selectionProgress, opacitySpec);
        IndicatorSize.UpdateTarget(selectionProgress, sizeSpec);
        IconColor.UpdateTarget(iconColor, colorSpec);
        TextColor.UpdateTarget(textColor, colorSpec);
    }

    public void Dispose()
    {
        IndicatorOpacity.Dispose();
        IndicatorSize.Dispose();
        IconColor.Dispose();
        TextColor.Dispose();
    }
}
