/*
 * Translated from AndroidX Material3 carousel/MultiAspectCarousel.kt.
 * Copyright 2025 The Android Open Source Project. Licensed under the Apache License, Version 2.0.
 * Fixed AndroidX / Core commits: docs/upstream-baseline.json.
 */
using Avalonia.Layout;

namespace Material3Expressive.Components;

/// <summary>The mask and content translation of one multi-aspect carousel item, in DIP.</summary>
public readonly record struct MultiAspectCarouselItemDrawInfo(
    float MaskStart, float MaskEnd, float MinSize, float MaxSize, float Parallax, Orientation Orientation)
{
    public float Size => MaskEnd - MaskStart;

    /// <summary>Computes the upstream mask from viewport and item offsets along the layout main axis.
    /// List layouts use the viewport cross-axis size; grids use the item's cross-axis size.
    /// All arguments must use the same units. No layout or scrolling is performed.</summary>
    public static MultiAspectCarouselItemDrawInfo Calculate(float viewportStart, float viewportEnd,
        float itemOffset, float mainAxisSize, float crossAxisSize, bool isVisible = true,
        Orientation orientation = Orientation.Horizontal)
    {
        if (!isVisible) return new(0, 0, 0, mainAxisSize, 0, orientation);

        var aspectRatio = mainAxisSize / crossAxisSize;
        // The strict comparisons (including the discontinuities at exactly 1 and 16/9)
        // are intentional: preserve the fixed upstream getMaskIntensity branches.
        var intensity = aspectRatio switch
        {
            > 16 / 9f => 1 / 2f,
            > 1f and < 16 / 9f => Lerp(1 / 3f, 1 / 2f, 1f, 16 / 9f, aspectRatio),
            > 9 / 16f and < 1f => Lerp(1 / 4f, 1 / 3f, 9 / 16f, 1f, aspectRatio),
            _ => 1 / 4f,
        };
        var minSize = mainAxisSize * (1f - intensity);
        if (itemOffset < viewportStart)
        {
            var distance = viewportStart - itemOffset;
            return new(Lerp(0, mainAxisSize * (1f - intensity), 0, mainAxisSize, distance),
                mainAxisSize, minSize, mainAxisSize,
                Lerp(0, mainAxisSize * intensity, 0, mainAxisSize, distance), orientation);
        }
        if (itemOffset > viewportEnd - mainAxisSize)
        {
            var distance = -(viewportEnd - itemOffset - mainAxisSize);
            return new(0, Lerp(mainAxisSize, mainAxisSize * intensity, 0, mainAxisSize, distance),
                minSize, mainAxisSize, -Lerp(0, mainAxisSize * intensity, 0, mainAxisSize, distance), orientation);
        }
        return new(0, mainAxisSize, minSize, mainAxisSize, 0, orientation);
    }

    // carousel/Strategy.kt lerp: bounded input, and Float operation order.
    private static float Lerp(float outputMin, float outputMax, float inputMin, float inputMax, float value)
    {
        if (value <= inputMin) return outputMin;
        if (value >= inputMax) return outputMax;
        var fraction = (value - inputMin) / (inputMax - inputMin);
        return (1 - fraction) * outputMin + fraction * outputMax;
    }
}
