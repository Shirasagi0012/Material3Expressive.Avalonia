// Port of AndroidX carousel/Strategy.kt and KeylineSnapPosition.kt, f3ed195f0c9ef9eafe437351d974c9ec49ecc2ea.
// Copyright 2024 The Android Open Source Project. Licensed under the Apache License, Version 2.0.
namespace Material3Expressive.Components;

internal sealed class CarouselStrategy
{
    internal CarouselKeylineList DefaultKeylines { get; }
    internal IReadOnlyList<CarouselKeylineList> StartSteps { get; }
    internal IReadOnlyList<CarouselKeylineList> EndSteps { get; }
    internal float AvailableSpace { get; }
    internal float ItemSpacing { get; }
    internal float ItemSize => IsValid ? DefaultKeylines.FirstFocal.Size : 0;
    internal bool IsValid => DefaultKeylines.Count > 0 && AvailableSpace != 0 && DefaultKeylines.FirstFocal.Size != 0;
    internal float MinItemSize { get; }
    internal float MaxItemSize { get; }
    private readonly float _startShiftDistance;
    private readonly float _endShiftDistance;
    private readonly float[] _startPoints;
    private readonly float[] _endPoints;
    private (float Offset, float Max, bool Round, CarouselKeylineList Keylines)? _cache;
    private IReadOnlyList<CarouselKeylineList>? _startAndEndSteps;

    internal CarouselStrategy(CarouselKeylineList keylines, float space, float spacing, float beforePadding, float afterPadding)
    {
        DefaultKeylines = keylines;
        AvailableSpace = space;
        ItemSpacing = spacing;
        StartSteps = GetStartSteps(keylines, space, spacing, beforePadding);
        EndSteps = GetEndSteps(keylines, space, spacing, afterPadding);
        _startShiftDistance = StartSteps.Count == 0 ? 0 : MathF.Max(StartSteps[^1][0].UnadjustedOffset - StartSteps[0][0].UnadjustedOffset, beforePadding);
        _endShiftDistance = EndSteps.Count == 0 ? 0 : MathF.Max(EndSteps[0][^1].UnadjustedOffset - EndSteps[^1][^1].UnadjustedOffset, afterPadding);
        _startPoints = GetPoints(_startShiftDistance, StartSteps, true);
        _endPoints = GetPoints(_endShiftDistance, EndSteps, false);
        var all = StartSteps.Concat(EndSteps).Prepend(keylines).ToArray();
        MinItemSize = all.Min(k => k.MinSize);
        MaxItemSize = all.Max(k => k.MaxSize);
    }

    internal CarouselKeylineList GetKeylines(float scrollOffset, float maxScrollOffset, bool roundToNearestStep = false)
    {
        if (!IsValid) return DefaultKeylines;
        if (_cache is { } cache && cache.Offset == scrollOffset && cache.Max == maxScrollOffset && cache.Round == roundToNearestStep)
            return cache.Keylines;
        var result = Calculate();
        _cache = (scrollOffset, maxScrollOffset, roundToNearestStep, result);
        return result;

        CarouselKeylineList Calculate()
        {
            var positive = MathF.Max(0, scrollOffset);
            var endOffset = MathF.Max(0, maxScrollOffset - _endShiftDistance);
            if (positive >= _startShiftDistance && positive <= endOffset) return DefaultKeylines;
            var interpolation = Lerp(1, 0, 0, _startShiftDistance, positive);
            var points = _startPoints;
            var steps = StartSteps;
            if (positive > endOffset)
            {
                interpolation = Lerp(0, 1, endOffset, maxScrollOffset, positive);
                points = _endPoints;
                steps = EndSteps;
                if (endOffset < .01f && StartSteps.Count == 2 && EndSteps.Count == 2)
                    steps = _startAndEndSteps ??= [StartSteps[^1], EndSteps[^1]];
            }
            var from = 0;
            var to = 0;
            var fraction = 0f;
            var lower = points[0];
            for (var i = 1; i < steps.Count; i++)
            {
                var upper = points[i];
                if (interpolation <= upper)
                {
                    from = i - 1;
                    to = i;
                    fraction = Lerp(0, 1, lower, upper, interpolation);
                    break;
                }
                lower = upper;
            }
            return roundToNearestStep ? steps[Round(fraction) == 0 ? from : to]
                : CarouselKeylineList.Lerp(steps[from], steps[to], fraction);
        }
    }

    internal int SnapPosition(int itemIndex, int itemCount)
    {
        if (!IsValid) return 0;
        var offset = Round(DefaultKeylines.FirstFocal.UnadjustedOffset - ItemSize / 2);
        if (itemIndex <= StartSteps.Count - 1)
        {
            var index = Math.Clamp(StartSteps.Count - 1 - itemIndex, 0, StartSteps.Count - 1);
            offset = Round(StartSteps[index].FirstFocal.UnadjustedOffset - ItemSize / 2);
        }
        var last = itemCount - 1;
        if (itemIndex >= last - (EndSteps.Count - 1) && itemCount > DefaultKeylines.FocalCount)
        {
            var index = Math.Clamp(EndSteps.Count - 1 - (last - itemIndex), 0, EndSteps.Count - 1);
            offset = Round(EndSteps[index].LastFocal.UnadjustedOffset - ItemSize / 2);
        }
        return offset;
    }

    internal float MaxScrollOffset(int count) => MathF.Max(0, ItemSize * count + ItemSpacing * (count - 1) - AvailableSpace);
    internal static int Round(float value) => (int)MathF.Floor(value + .5f);
    internal static float Lerp(float min, float max, float inputMin, float inputMax, float value) =>
        value <= inputMin ? min : value >= inputMax ? max : CarouselKeyline.Lerp(min, max, (value - inputMin) / (inputMax - inputMin));

    private static IReadOnlyList<CarouselKeylineList> GetStartSteps(CarouselKeylineList original, float space, float spacing, float padding)
    {
        if (original.Count == 0) return [];
        List<CarouselKeylineList> steps = [original];
        if (original.IsFirstFocalAtStart)
        {
            if (padding != 0) steps.Add(ShiftForPadding(original, space, spacing, padding, original.FirstFocal, original.FirstFocalIndex));
            return steps;
        }
        var start = original.FirstNonAnchorIndex;
        var count = original.FirstFocalIndex - start;
        if (count <= 0 && original.FirstFocal.Cutoff > 0)
        {
            steps.Add(Move(original, 0, 0, space, spacing));
            return steps;
        }
        for (var i = 0; i < count; i++)
        {
            var previous = steps[^1];
            var originalIndex = start + i;
            var destination = original.Count - 1;
            if (originalIndex > 0)
                destination = previous.FirstIndexAfterFocalRangeWithSize(original[originalIndex - 1].Size) - 1;
            steps.Add(Move(previous, original.FirstNonAnchorIndex, destination, space, spacing));
        }
        if (padding != 0)
            steps[^1] = ShiftForPadding(steps[^1], space, spacing, padding, steps[^1].FirstFocal, steps[^1].FirstFocalIndex);
        return steps;
    }

    private static IReadOnlyList<CarouselKeylineList> GetEndSteps(CarouselKeylineList original, float space, float spacing, float padding)
    {
        if (original.Count == 0) return [];
        List<CarouselKeylineList> steps = [original];
        if (original.IsLastFocalAtEnd(space))
        {
            if (padding != 0) steps.Add(ShiftForPadding(original, space, spacing, -padding, original.LastFocal, original.LastFocalIndex));
            return steps;
        }
        var end = original.LastNonAnchorIndex;
        var count = end - original.LastFocalIndex;
        if (count <= 0 && original.LastFocal.Cutoff > 0)
        {
            steps.Add(Move(original, 0, 0, space, spacing));
            return steps;
        }
        for (var i = 0; i < count; i++)
        {
            var previous = steps[^1];
            var originalIndex = end - i;
            var destination = 0;
            if (originalIndex < original.Count - 1)
                destination = previous.LastIndexBeforeFocalRangeWithSize(original[originalIndex + 1].Size) + 1;
            steps.Add(Move(previous, original.LastNonAnchorIndex, destination, space, spacing));
        }
        if (padding != 0)
            steps[^1] = ShiftForPadding(steps[^1], space, spacing, -padding, steps[^1].LastFocal, steps[^1].LastFocalIndex);
        return steps;
    }

    private static CarouselKeylineList ShiftForPadding(CarouselKeylineList from, float space, float spacing,
        float padding, CarouselKeyline pivot, int pivotIndex)
    {
        var reduction = padding / from.Count(k => !k.IsAnchor);
        var keylines = CarouselKeylineList.Create(space, spacing,
            from.Select(k => (k.Size - MathF.Abs(reduction), k.IsAnchor)).ToArray(),
            pivotIndex: pivotIndex, pivotOffset: pivot.Offset - reduction / 2 + padding);
        return new(keylines.Select((k, i) => k with { UnadjustedOffset = from[i].UnadjustedOffset }));
    }

    private static CarouselKeylineList Move(CarouselKeylineList from, int source, int destination, float space, float spacing)
    {
        var direction = source > destination ? 1 : -1;
        var delta = (from[source].Size - from[source].Cutoff + spacing) * direction;
        var items = from.Select(k => (k.Size, k.IsAnchor)).ToList();
        var moving = items[source];
        items.RemoveAt(source);
        items.Insert(destination, moving);
        return CarouselKeylineList.Create(space, spacing, items,
            pivotIndex: from.PivotIndex + direction, pivotOffset: from.Pivot.Offset + delta);
    }

    private static float[] GetPoints(float distance, IReadOnlyList<CarouselKeylineList> steps, bool start)
    {
        if (distance == 0 || steps.Count == 0) return [0];
        var points = new float[steps.Count];
        for (var i = 1; i < steps.Count; i++)
        {
            var shifted = start ? steps[i][0].UnadjustedOffset - steps[i - 1][0].UnadjustedOffset
                : steps[i - 1][^1].UnadjustedOffset - steps[i][^1].UnadjustedOffset;
            points[i] = i == steps.Count - 1 ? 1 : points[i - 1] + shifted / distance;
        }
        return points;
    }
}
