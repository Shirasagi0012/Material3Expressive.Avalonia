/*
 * Copyright 2022 The Android Open Source Project
 * Copyright 2026 @Shirasagi0012
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// Ported from androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\FloatMapping.kt by Shirasagi0012

namespace MaterialShapes;

internal class DoubleMapper
{
    internal static bool IsProgressInRange(double progress, double progressFrom, double progressTo)
    {
        return progressTo >= progressFrom
            ? progress >= progressFrom && progress <= progressTo
            : progress >= progressFrom || progress <= progressTo;
    }

    private static double LinearMap(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues, double x)
    {
        if (x is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(x));

        var segmentStartIndex = Enumerable.Range(0, xValues.Count)
            .First(i => IsProgressInRange(x, xValues[i], xValues[(i + 1) % xValues.Count]));
        var segmentEndIndex = (segmentStartIndex + 1) % xValues.Count;
        var segmentSizeX = Utils.PositiveModulo(xValues[segmentEndIndex] - xValues[segmentStartIndex], 1d);
        var segmentSizeY = Utils.PositiveModulo(yValues[segmentEndIndex] - yValues[segmentStartIndex], 1d);
        var positionInSegment = segmentSizeX < 0.001d
            ? 0.5d
            : Utils.PositiveModulo(x - xValues[segmentStartIndex], 1d) / segmentSizeX;
        return Utils.PositiveModulo(yValues[segmentStartIndex] + segmentSizeY * positionInSegment, 1d);
    }

    private static void ValidateProgress(List<double> p)
    {
        var prev = p.Last();
        var wraps = 0;
        foreach (var curr in p)
        {
            if (curr is < 0D or >= 1D)
                throw new ArgumentException(
                    $"FloatMapping - Progress outside of range: {String.Join(", ", p)}");

            if (ProgressDistance(curr, prev) <= Utils.DistanceEpsilon)
                throw new ArgumentException(
                    $"FloatMapping - Progress repeats a value: {String.Join(", ", p)}");

            if (curr < prev)
            {
                wraps++;
                if (wraps > 1)
                    throw new ArgumentException(
                        $"FloatMapping - Progress wraps more than once: {String.Join(", ", p)}");
            }

            prev = curr;
        }
    }

    internal static double ProgressDistance(double a, double b)
    {
        var diff = Math.Abs(a - b);
        return Math.Min(diff, 1 - diff);
    }

    private ValueTuple<double, double>[] _mappings;
    private List<double> _sourceValues;
    private List<double> _targetValues;

    public DoubleMapper(params ValueTuple<double, double>[] mappings)
    {
        _mappings = mappings;
        _sourceValues = new List<double>(_mappings.Length);
        _targetValues = new List<double>(_mappings.Length);
        foreach (var m in mappings)
        {
            _sourceValues.Add(m.Item1);
            _targetValues.Add(m.Item2);
        }

        // Both source values and target values should be monotonically increasing, with the
        // exception of maybe one time (since progress wraps around).
        ValidateProgress(_sourceValues);
        ValidateProgress(_targetValues);
    }

    public double Map(double x)
    {
        return LinearMap(_sourceValues, _targetValues, x);
    }

    public double MapBack(double x)
    {
        return LinearMap(_targetValues, _sourceValues, x);
    }

    public static DoubleMapper Identity { get; } = new((0, 0), (0.5, 0.5));
}