// Port of AndroidX carousel/Arrangement.kt, f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea.
// Copyright 2023 The Android Open Source Project. Licensed under the Apache License, Version 2.0.
namespace Material3Expressive.Components;

internal sealed record CarouselArrangement(int Priority, float SmallSize, int SmallCount,
    float MediumSize, int MediumCount, float LargeSize, int LargeCount)
{
    internal int ItemCount => LargeCount + MediumCount + SmallCount;
    private bool IsValid => LargeCount > 0 && SmallCount > 0 && MediumCount > 0
        ? LargeSize > MediumSize && MediumSize > SmallSize
        : LargeCount <= 0 || SmallCount <= 0 || LargeSize > SmallSize;
    private float Cost(float targetLarge) => IsValid ? MathF.Abs(targetLarge - LargeSize) * Priority : float.MaxValue;

    internal static CarouselArrangement? Find(float space, float spacing, float targetSmall, float minSmall,
        float maxSmall, int[] smallCounts, float targetMedium, int[] mediumCounts, float targetLarge, int[] largeCounts)
    {
        CarouselArrangement? best = null;
        var priority = 1;
        foreach (var large in largeCounts)
        foreach (var medium in mediumCounts)
        foreach (var small in smallCounts)
        {
            var a = Fit(priority, space, spacing, small, targetSmall, minSmall, maxSmall, medium, targetMedium, large, targetLarge);
            if (best is null || a.Cost(targetLarge) < best.Cost(targetLarge))
            {
                best = a;
                if (best.Cost(targetLarge) == 0) return best;
            }
            priority++;
        }
        return best;
    }

    private static CarouselArrangement Fit(int priority, float space, float spacing, int smallCount,
        float small, float minSmall, float maxSmall, int mediumCount, float medium, int largeCount, float large)
    {
        var available = space - (largeCount + mediumCount + smallCount - 1) * spacing;
        var arrangedSmall = Math.Clamp(small, minSmall, maxSmall);
        var taken = large * largeCount + medium * mediumCount + arrangedSmall * smallCount;
        var delta = available - taken;
        if (smallCount > 0 && delta > 0) arrangedSmall += MathF.Min(delta / smallCount, maxSmall - arrangedSmall);
        else if (smallCount > 0 && delta < 0) arrangedSmall += MathF.Max(delta / smallCount, minSmall - arrangedSmall);
        arrangedSmall = smallCount > 0 ? arrangedSmall : 0;
        var arrangedLarge = (available - (smallCount + mediumCount / 2f) * arrangedSmall) / (largeCount + mediumCount / 2f);
        var arrangedMedium = (arrangedLarge + arrangedSmall) / 2f;
        if (mediumCount > 0 && arrangedLarge != large)
        {
            var adjustment = (large - arrangedLarge) * largeCount;
            var flex = arrangedMedium * .1f * mediumCount;
            var distribute = MathF.Min(MathF.Abs(adjustment), flex);
            if (adjustment > 0)
            {
                arrangedMedium -= distribute / mediumCount;
                arrangedLarge += distribute / largeCount;
            }
            else
            {
                arrangedMedium += distribute / mediumCount;
                arrangedLarge -= distribute / largeCount;
            }
        }
        return new(priority, arrangedSmall, smallCount, arrangedMedium, mediumCount, arrangedLarge, largeCount);
    }
}
