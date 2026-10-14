// AndroidX DatePicker.kt SwitchableDateEntryContent and DateRangePicker.kt crossfade.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).

using Avalonia;
using Avalonia.Controls;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Spring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

internal sealed class DateEntryContent : AnimatedContent
{
    public static readonly StyledProperty<Spring> SpatialProperty =
        AvaloniaProperty.Register<DateEntryContent, Spring>(nameof(Spatial), new Spring(0.8, 380));

    public static readonly StyledProperty<Spring> EffectsProperty =
        AvaloniaProperty.Register<DateEntryContent, Spring>(nameof(Effects), new Spring(1, 1600));

    public static readonly StyledProperty<Spring> FastEffectsProperty =
        AvaloniaProperty.Register<DateEntryContent, Spring>(nameof(FastEffects), new Spring(1, 3800));

    public Spring Spatial
    {
        get => GetValue(SpatialProperty);
        set => SetValue(SpatialProperty, value);
    }

    public Spring Effects
    {
        get => GetValue(EffectsProperty);
        set => SetValue(EffectsProperty, value);
    }

    public Spring FastEffects
    {
        get => GetValue(FastEffectsProperty);
        set => SetValue(FastEffectsProperty, value);
    }

    internal bool Range { get; set; }

    internal SpringSpec<double> EffectsSpec(bool fast = false)
    {
        Spring s = fast ? FastEffects : Effects;
        return new SpringSpec<double>(s.DampingRatio, s.Stiffness, 0.01);
    }

    internal SpringSpec<Size> SizeSpec(bool effects = false)
    {
        Spring s = effects ? Effects : Spatial;
        return new SpringSpec<Size>(s.DampingRatio, s.Stiffness, new Size(1, 1));
    }

    public DateEntryContent()
    {
        Label = "Date picker display mode";
        ContentKeySelector = value => (value as Control)?.Tag;
        TransitionSpec = scope => {
            if (Range)
            {
                return new FadeIn { Spec = EffectsSpec(true) }
                   .TogetherWith(new FadeOut { Spec = EffectsSpec(true) })
                   .Using(null);
            }

            bool input = (scope.TargetState as Control)?.Tag is DatePickerDisplayMode.Input;

            var slide = new SpringSpec<Vector>(
                Spatial.DampingRatio,
                Spatial.Stiffness,
                new Vector(1, 1));

            return (new SlideInVertically { InitialOffsetSelector = height => input ? height : -48, Spec = slide }
                  + new FadeIn { Spec = EffectsSpec() })
               .TogetherWith(
                    new SlideOutVertically { TargetOffsetSelector = height => input ? -48 : height, Spec = slide }
                  + new FadeOut { Spec = EffectsSpec(true) })
               .Using(new SizeTransform { Clip = true, Spec = SizeSpec() });
        };
    }
}
