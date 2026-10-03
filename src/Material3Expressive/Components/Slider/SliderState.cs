/*
 * Portions translated from AndroidX Slider.kt.
 * Copyright 2022 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Avalonia;
using Avalonia.Input;

namespace Material3Expressive.Components;

/// <summary>A closed pair of slider values, in the same float units as SliderState.</summary>
public readonly record struct SliderRange(float Start, float EndInclusive);

// Direct translations of Slider.kt's shared float helpers. Ties select the first tick;
// accessibility setProgress deliberately uses a different, last-tick tie rule.
internal static class SliderMath
{
    internal static float Lerp(float a, float b, float fraction) => (1 - fraction) * a + fraction * b;
    internal static float Fraction(float a, float b, float value) => Math.Clamp(b - a == 0 ? 0 : (value - a) / (b - a), 0, 1);
    internal static float Scale(float a, float b, float value, float c, float d) => Lerp(c, d, Fraction(a, b, value));
    internal static int Round(float value) => (int)MathF.Floor(value + 0.5f);
    internal static float[] Ticks(int steps) => steps == 0 ? [] : Enumerable.Range(0, steps + 2).Select(i => (float)i / (steps + 1)).ToArray();
    internal static float Snap(float value, float[] ticks, float minimum, float maximum)
    {
        if (ticks.Length == 0) return value;
        var best = Lerp(minimum, maximum, ticks[0]);
        var distance = MathF.Abs(best - value);
        foreach (var tick in ticks.Skip(1))
        {
            var candidate = Lerp(minimum, maximum, tick);
            var nextDistance = MathF.Abs(candidate - value);
            if (nextDistance < distance) { best = candidate; distance = nextDistance; }
        }
        return best;
    }

    internal static float Progress(float target, float minimum, float maximum, int steps)
    {
        var value = Math.Clamp(target, minimum, maximum);
        var original = value;
        if (steps > 0)
        {
            // Preserve upstream's distance initialization, including negative ranges.
            var distance = value;
            for (var i = 0; i <= steps + 1; i++)
            {
                var stepValue = Lerp(minimum, maximum, (float)i / (steps + 1));
                if (MathF.Abs(stepValue - original) <= distance)
                { distance = MathF.Abs(stepValue - original); value = stepValue; }
            }
        }
        return value;
    }

    internal static bool KeyValue(Key key, bool vertical, bool reverse, float value, float minimum, float maximum,
        float absoluteMinimum, float absoluteMaximum, int steps, out float result)
    {
        var actualSteps = steps > 0 ? steps + 1 : 100;
        var delta = MathF.Abs(absoluteMaximum - absoluteMinimum) / actualSteps;
        var sign = reverse ? -1 : 1;
        var page = Math.Clamp(actualSteps / 10, 1, 10);
        float? next = key switch
        {
            Key.Home => minimum,
            Key.End => maximum,
            Key.Left when !vertical => value - sign * delta,
            Key.Right when !vertical => value + sign * delta,
            Key.Up when vertical => value - sign * delta,
            Key.Down when vertical => value + sign * delta,
            Key.PageUp => value + (vertical ? -sign : 1) * page * delta,
            Key.PageDown => value + (vertical ? sign : -1) * page * delta,
            _ => null,
        };
        result = next is { } n ? Math.Clamp(n, minimum, maximum) : value;
        return next.HasValue;
    }
}

/// <summary>Hoisted single-value state. Bounds and steps are fixed at construction, as in Compose.</summary>
public sealed class SliderState : AvaloniaObject
{
    public static readonly DirectProperty<SliderState, float> ValueProperty = AvaloniaProperty.RegisterDirect<SliderState, float>(
        nameof(Value), s => s.Value, (s, v) => s.Value = v, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly DirectProperty<SliderState, bool> IsDraggingProperty = AvaloniaProperty.RegisterDirect<SliderState, bool>(nameof(IsDragging), s => s.IsDragging);
    private float _value;
    private bool _isDragging;
    internal float RawOffset, PressOffset;
    internal float TotalWidth, TotalHeight, ThumbWidth, ThumbHeight;
    internal bool IsRtl, ReverseVerticalDirection;
    internal float[] TickFractions { get; }

    public SliderState() : this(0) { }
    public SliderState(float value, int steps = 0, float minimum = 0, float maximum = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        if (minimum > maximum) throw new ArgumentException("Minimum must be at most Maximum.");
        _value = value; Steps = steps; Minimum = minimum; Maximum = maximum; TickFractions = SliderMath.Ticks(steps);
    }
    public int Steps { get; }
    public float Minimum { get; }
    public float Maximum { get; }
    public float Value
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref _value, ShouldAutoSnap
            ? SliderMath.Snap(Math.Clamp(value, Minimum, Maximum), TickFractions, Minimum, Maximum) : value);
    }
    public bool ShouldAutoSnap { get; set; } = true;
    public bool IsVertical { get; internal set; }
    public bool IsDragging { get => _isDragging; internal set => SetAndRaise(IsDraggingProperty, ref _isDragging, value); }
    public float CoercedValueAsFraction => SliderMath.Fraction(Minimum, Maximum, Math.Clamp(Value, Minimum, Maximum));
    /// <summary>When supplied, the callback owns writing Value. Otherwise user input writes Value directly.</summary>
    public Action<float>? OnValueChange { get; set; }
    public Action? OnValueChangeFinished { get; set; }

    internal void Change(float value) { if (OnValueChange is { } callback) callback(value); else Value = value; }
    internal void Press(float x, float y) => PressOffset = (IsVertical
        ? ReverseVerticalDirection ? TotalHeight - y : y
        : IsRtl ? TotalWidth - x : x) - RawOffset;
    internal void DispatchRawDelta(float delta)
    {
        var maxPx = MathF.Max((IsVertical ? TotalHeight - ThumbHeight / 2 : TotalWidth - ThumbWidth / 2), 0);
        var minPx = MathF.Min((IsVertical ? ThumbHeight : ThumbWidth) / 2, maxPx);
        RawOffset += delta + PressOffset; PressOffset = 0;
        var offset = SliderMath.Snap(RawOffset, TickFractions, minPx, maxPx);
        var value = SliderMath.Scale(minPx, maxPx, offset, Minimum, Maximum);
        if (value != Value) Change(value);
    }
    internal void Finish() { if (!IsDragging) OnValueChangeFinished?.Invoke(); }
    internal bool SetProgress(float target)
    {
        var resolved = SliderMath.Progress(target, Minimum, Maximum, Steps);
        if (resolved == Value) return false;
        Change(resolved); OnValueChangeFinished?.Invoke(); return true;
    }
}

/// <summary>Hoisted range state, including independent thumb bounds and upstream snapping order.</summary>
public sealed class RangeSliderState : AvaloniaObject
{
    internal static readonly DirectProperty<RangeSliderState, bool> IsDraggingProperty = AvaloniaProperty.RegisterDirect<RangeSliderState, bool>(nameof(IsDragging), s => s.IsDragging);
    public static readonly DirectProperty<RangeSliderState, float> ActiveRangeStartProperty = AvaloniaProperty.RegisterDirect<RangeSliderState, float>(
        nameof(ActiveRangeStart), s => s.ActiveRangeStart, (s, v) => s.ActiveRangeStart = v, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly DirectProperty<RangeSliderState, float> ActiveRangeEndProperty = AvaloniaProperty.RegisterDirect<RangeSliderState, float>(
        nameof(ActiveRangeEnd), s => s.ActiveRangeEnd, (s, v) => s.ActiveRangeEnd = v, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    private float _start, _end;
    private bool _isDragging;
    internal float StartThumbWidth, StartThumbHeight, EndThumbWidth, EndThumbHeight, TotalWidth;
    internal float RawOffsetStart, RawOffsetEnd, MinPx, MaxPx;
    internal bool IsRtl;
    // Compose's mutableStateOf drag guard invalidates updateMinMaxPx in layout.
    internal bool IsDragging { get => _isDragging; set => SetAndRaise(IsDraggingProperty, ref _isDragging, value); }
    internal float[] TickFractions { get; }
    public RangeSliderState() : this(0, 1) { }
    public RangeSliderState(float activeRangeStart, float activeRangeEnd, int steps = 0, float minimum = 0, float maximum = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        if (minimum > maximum) throw new ArgumentException("Minimum must be at most Maximum.");
        var start = Math.Clamp(activeRangeStart, minimum, maximum); var end = Math.Clamp(activeRangeEnd, minimum, maximum);
        _start = MathF.Min(start, end); _end = MathF.Max(start, end);
        Steps = steps; Minimum = minimum; Maximum = maximum; TickFractions = SliderMath.Ticks(steps);
    }
    public int Steps { get; }
    public float Minimum { get; }
    public float Maximum { get; }
    public float ActiveRangeStart
    {
        get => _start;
        set => SetAndRaise(ActiveRangeStartProperty, ref _start, SliderMath.Snap(Math.Clamp(value, Minimum, ActiveRangeEnd), TickFractions, Minimum, Maximum));
    }
    public float ActiveRangeEnd
    {
        get => _end;
        set => SetAndRaise(ActiveRangeEndProperty, ref _end, SliderMath.Snap(Math.Clamp(value, ActiveRangeStart, Maximum), TickFractions, Minimum, Maximum));
    }
    public Action<SliderRange>? OnValueChange { get; set; }
    public Action? OnValueChangeFinished { get; set; }
    internal float StartFraction => SliderMath.Fraction(Minimum, Maximum, ActiveRangeStart);
    internal float EndFraction => SliderMath.Fraction(Minimum, Maximum, ActiveRangeEnd);
    internal int StartSteps => (int)MathF.Floor(Steps * EndFraction);
    internal int EndSteps => (int)MathF.Floor(Steps * (1 - StartFraction));
    internal void Change(SliderRange value)
    {
        if (OnValueChange is { } callback) callback(value);
        else { ActiveRangeStart = value.Start; ActiveRangeEnd = value.EndInclusive; }
    }
    internal void UpdateMinMaxPx()
    {
        var newMax = MathF.Max(TotalWidth - EndThumbWidth / 2, 0);
        var newMin = MathF.Min(StartThumbWidth / 2, newMax);
        if (!IsDragging && (MinPx != newMin || MaxPx != newMax || ActiveRangeStart != ActiveRangeEnd))
        {
            MinPx = newMin; MaxPx = newMax;
            RawOffsetStart = SliderMath.Scale(Minimum, Maximum, ActiveRangeStart, MinPx, MaxPx);
            RawOffsetEnd = SliderMath.Scale(Minimum, Maximum, ActiveRangeEnd, MinPx, MaxPx);
        }
    }
    internal void Drag(bool start, float offset)
    {
        float offsetStart, offsetEnd;
        if (start)
        {
            RawOffsetStart += offset;
            RawOffsetEnd = SliderMath.Scale(Minimum, Maximum, ActiveRangeEnd, MinPx, MaxPx);
            offsetEnd = RawOffsetEnd;
            offsetStart = MathF.Min(SliderMath.Snap(Math.Clamp(RawOffsetStart, MinPx, offsetEnd), TickFractions, MinPx, MaxPx), offsetEnd);
        }
        else
        {
            RawOffsetEnd += offset;
            RawOffsetStart = SliderMath.Scale(Minimum, Maximum, ActiveRangeStart, MinPx, MaxPx);
            offsetStart = RawOffsetStart;
            offsetEnd = MathF.Max(SliderMath.Snap(Math.Clamp(RawOffsetEnd, offsetStart, MaxPx), TickFractions, MinPx, MaxPx), offsetStart);
        }
        var scaledStart = SliderMath.Scale(MinPx, MaxPx, offsetStart, Minimum, Maximum);
        var scaledEnd = SliderMath.Scale(MinPx, MaxPx, offsetEnd, Minimum, Maximum);
        var value = start ? new SliderRange(MathF.Min(scaledStart, scaledEnd), scaledEnd) : new SliderRange(scaledStart, MathF.Max(scaledStart, scaledEnd));
        if (value != new SliderRange(ActiveRangeStart, ActiveRangeEnd)) Change(value);
    }
    internal bool SetProgress(bool start, float target)
    {
        var resolved = SliderMath.Progress(target, start ? Minimum : ActiveRangeStart, start ? ActiveRangeEnd : Maximum, start ? StartSteps : EndSteps);
        if (resolved == (start ? ActiveRangeStart : ActiveRangeEnd)) return false;
        Change(start ? new(resolved, ActiveRangeEnd) : new(ActiveRangeStart, resolved)); OnValueChangeFinished?.Invoke(); return true;
    }
}
