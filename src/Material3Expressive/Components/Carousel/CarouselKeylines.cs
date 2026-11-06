// Port of AndroidX carousel/KeylineList.kt and Keylines.kt, f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea.
// Copyright 2024 The Android Open Source Project. Licensed under the Apache License, Version 2.0.
namespace Material3Expressive.Components;

internal readonly record struct CarouselKeyline(float Size, float Offset, float UnadjustedOffset,
    bool IsFocal, bool IsAnchor, bool IsPivot, float Cutoff)
{
    internal static float Lerp(float start, float end, float fraction) => (1 - fraction) * start + fraction * end;

    internal static CarouselKeyline Lerp(CarouselKeyline start, CarouselKeyline end, float fraction) => new(
        Lerp(start.Size, end.Size, fraction), Lerp(start.Offset, end.Offset, fraction),
        Lerp(start.UnadjustedOffset, end.UnadjustedOffset, fraction),
        fraction < .5f ? start.IsFocal : end.IsFocal, fraction < .5f ? start.IsAnchor : end.IsAnchor,
        fraction < .5f ? start.IsPivot : end.IsPivot, Lerp(start.Cutoff, end.Cutoff, fraction));
}

internal sealed class CarouselKeylineList : IReadOnlyList<CarouselKeyline>
{
    private readonly CarouselKeyline[] _items;
    internal static readonly CarouselKeylineList Empty = new([]);
    internal CarouselKeylineList(IEnumerable<CarouselKeyline> items)
    {
        _items = items.ToArray();
        MinSize = _items.Select(k => k.Size).DefaultIfEmpty(float.MaxValue).Min();
        MaxSize = _items.Select(k => k.Size).DefaultIfEmpty(0).Max();
        PivotIndex = Array.FindIndex(_items, k => k.IsPivot);
        FirstNonAnchorIndex = Array.FindIndex(_items, k => !k.IsAnchor);
        LastNonAnchorIndex = Array.FindLastIndex(_items, k => !k.IsAnchor);
        FirstFocalIndex = Array.FindIndex(_items, k => k.IsFocal);
        LastFocalIndex = Array.FindLastIndex(_items, k => k.IsFocal);
    }
    public int Count => _items.Length;
    public CarouselKeyline this[int index] => _items[index];
    public IEnumerator<CarouselKeyline> GetEnumerator() => ((IEnumerable<CarouselKeyline>)_items).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    internal float MinSize { get; }
    internal float MaxSize { get; }
    internal int PivotIndex { get; }
    internal int FirstNonAnchorIndex { get; }
    internal int LastNonAnchorIndex { get; }
    internal int FirstFocalIndex { get; }
    internal int LastFocalIndex { get; }
    internal int FocalCount => LastFocalIndex - FirstFocalIndex + 1;
    internal CarouselKeyline Pivot => this[PivotIndex];
    internal CarouselKeyline FirstFocal => this[FirstFocalIndex];
    internal CarouselKeyline LastFocal => this[LastFocalIndex];
    internal bool IsFirstFocalAtStart => FirstFocal.Offset - FirstFocal.Size / 2 >= 0 && FirstFocalIndex == FirstNonAnchorIndex;
    internal bool IsLastFocalAtEnd(float space) => LastFocal.Offset + LastFocal.Size / 2 <= space && LastFocalIndex == LastNonAnchorIndex;
    internal int FirstIndexAfterFocalRangeWithSize(float size) =>
        Enumerable.Range(LastFocalIndex, Count - LastFocalIndex).FirstOrDefault(i => this[i].Size == size, Count - 1);
    internal int LastIndexBeforeFocalRangeWithSize(float size) =>
        Enumerable.Range(0, FirstFocalIndex).Reverse().FirstOrDefault(i => this[i].Size == size, 0);
    internal CarouselKeyline Before(float offset) => _items.LastOrDefault(k => k.UnadjustedOffset < offset, this[0]);
    internal CarouselKeyline After(float offset) => _items.FirstOrDefault(k => k.UnadjustedOffset >= offset, this[Count - 1]);
    internal static CarouselKeylineList Lerp(CarouselKeylineList from, CarouselKeylineList to, float fraction) =>
        new(from.Select((k, i) => CarouselKeyline.Lerp(k, to[i], fraction)));

    internal static CarouselKeylineList Create(float space, float spacing,
        IReadOnlyList<(float Size, bool Anchor)> items, bool centered = false, int? pivotIndex = null, float pivotOffset = 0)
    {
        var focalSize = items.Where(k => !k.Anchor).Max(k => k.Size);
        var firstFocal = Enumerable.Range(0, items.Count).First(i => !items[i].Anchor && items[i].Size == focalSize);
        var lastFocal = firstFocal;
        while (lastFocal < items.Count - 1 && items[lastFocal + 1].Size == focalSize) lastFocal++;
        var pivot = pivotIndex ?? firstFocal;
        if (pivotIndex is null)
        {
            var focalCount = lastFocal - firstFocal;
            var spacingSplit = spacing == 0 || focalCount % 2 == 0 ? 0 : spacing / 2;
            pivotOffset = centered
                ? space / 2 - focalSize / 2 * focalCount - spacingSplit - (focalCount / 2) * spacing
                : focalSize / 2;
        }
        var result = new CarouselKeyline[items.Count];
        var p = items[pivot];
        var pivotCutoff = CutoffLeft(p.Size, pivotOffset) ? pivotOffset - p.Size / 2
            : CutoffRight(p.Size, pivotOffset) ? pivotOffset + p.Size / 2 - space : 0;
        result[pivot] = new(p.Size, pivotOffset, pivotOffset, pivot >= firstFocal && pivot <= lastFocal, p.Anchor, true, pivotCutoff);
        var offset = pivotOffset - focalSize / 2 - spacing;
        var unadjusted = offset;
        for (var i = pivot - 1; i >= 0; i--)
        {
            var item = items[i];
            var center = offset - item.Size / 2;
            result[i] = new(item.Size, center, unadjusted - focalSize / 2, i >= firstFocal && i <= lastFocal,
                item.Anchor, false, CutoffLeft(item.Size, center) ? MathF.Abs(center - item.Size / 2) : 0);
            offset -= item.Size + spacing;
            unadjusted -= focalSize + spacing;
        }
        offset = pivotOffset + focalSize / 2 + spacing;
        unadjusted = offset;
        for (var i = pivot + 1; i < items.Count; i++)
        {
            var item = items[i];
            var center = offset + item.Size / 2;
            result[i] = new(item.Size, center, unadjusted + focalSize / 2, i >= firstFocal && i <= lastFocal,
                item.Anchor, false, CutoffRight(item.Size, center) ? center + item.Size / 2 - space : 0);
            offset += item.Size + spacing;
            unadjusted += focalSize + spacing;
        }
        return new(result);

        static bool CutoffLeft(float size, float center) => center - size / 2 < 0 && center + size / 2 > 0;
        bool CutoffRight(float size, float center) => center - size / 2 < space && center + size / 2 > space;
    }
}

/// <summary>The three horizontal keyline carousel arrangements.</summary>
public enum CarouselVariant { MultiBrowse, Uncontained, CenteredHero }

internal static class CarouselKeylines
{
    internal static CarouselKeylineList Create(CarouselVariant variant, float space, float preferredSize,
        float spacing, int itemCount, float minSmall, float maxSmall, float anchorSize)
    {
        if (space == 0 || preferredSize == 0) return CarouselKeylineList.Empty;
        if (variant == CarouselVariant.Uncontained)
        {
            var largeSize = MathF.Min(preferredSize + spacing, space);
            var largeCount = Math.Max(1, (int)MathF.Floor(space / largeSize));
            var remaining = space - largeCount * largeSize;
            var mediumSize = MathF.Max(remaining * 1.5f, anchorSize);
            var threshold = largeSize * .85f;
            if (mediumSize > threshold) mediumSize = MathF.Min(MathF.Max(threshold, remaining * 1.2f), largeSize);
            var arrangement = new CarouselArrangement(0, 0, 0, mediumSize, remaining > 0 ? 1 : 0, largeSize, largeCount);
            return Build(arrangement, false, MathF.Max(MathF.Min(anchorSize, preferredSize), mediumSize * .5f));
        }

        var centered = variant == CarouselVariant.CenteredHero && itemCount >= 3;
        var smallCounts = variant == CarouselVariant.MultiBrowse ? new[] { 1 }
            : itemCount <= 1 ? [0] : centered ? [2] : [1];
        int[] mediumCounts = variant == CarouselVariant.MultiBrowse ? [1, 0] : [0];
        var targetLarge = MathF.Min(preferredSize, space);
        var targetSmall = Math.Clamp(targetLarge / 3, minSmall, maxSmall);
        var targetMedium = variant == CarouselVariant.MultiBrowse ? (targetLarge + targetSmall) / 2 : 0;
        var fullscreenThreshold = variant == CarouselVariant.MultiBrowse ? minSmall * 2
            : minSmall * smallCounts.Max() + minSmall * 1.25f;
        if (space < fullscreenThreshold) smallCounts = [0];
        var minAvailable = variant == CarouselVariant.MultiBrowse
            ? space - targetMedium * mediumCounts.Max() - maxSmall * smallCounts.Max()
            : space - minSmall * smallCounts.Max();
        var minLarge = Math.Max(1, (int)MathF.Floor(minAvailable / targetLarge));
        var maxLarge = (int)MathF.Ceiling(space / targetLarge);
        var largeCounts = Enumerable.Range(minLarge, maxLarge - minLarge + 1).Reverse().ToArray();
        var best = CarouselArrangement.Find(space, spacing, targetSmall, minSmall, maxSmall,
            smallCounts, targetMedium, mediumCounts, targetLarge, largeCounts);
        if (variant == CarouselVariant.MultiBrowse && best is not null && best.ItemCount > itemCount)
        {
            var surplus = best.ItemCount - itemCount;
            var small = best.SmallCount;
            var medium = best.MediumCount;
            while (surplus-- > 0)
            {
                if (small > 0) small--;
                else if (medium > 1) medium--;
            }
            best = CarouselArrangement.Find(space, spacing, targetSmall, minSmall, maxSmall,
                [small], targetMedium, [medium], targetLarge, largeCounts);
        }
        return best is null ? CarouselKeylineList.Empty : Build(best, centered && itemCount >= best.ItemCount, anchorSize);

        CarouselKeylineList Build(CarouselArrangement a, bool center, float leftAnchor)
        {
            List<(float, bool)> items = [(leftAnchor, true)];
            if (center)
            {
                items.AddRange(Enumerable.Repeat((a.SmallSize, false), a.SmallCount / 2));
                items.AddRange(Enumerable.Repeat((a.MediumSize, false), a.MediumCount / 2));
            }
            items.AddRange(Enumerable.Repeat((a.LargeSize, false), a.LargeCount));
            items.AddRange(Enumerable.Repeat((a.MediumSize, false), center ? a.MediumCount / 2 : a.MediumCount));
            items.AddRange(Enumerable.Repeat((a.SmallSize, false), center ? a.SmallCount / 2 : a.SmallCount));
            items.Add((anchorSize, true));
            return CarouselKeylineList.Create(space, spacing, items, center);
        }
    }
}
