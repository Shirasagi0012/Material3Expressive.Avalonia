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

// Ported from androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\Morph.kt by Shirasagi0012

using Avalonia;

namespace MaterialShapes;

public sealed class Morph
{
    private readonly RoundedPolygon _start;
    private readonly RoundedPolygon _end;

    private readonly List<(CubicBezier Start, CubicBezier End)> _morphMatch;

    internal IReadOnlyList<(CubicBezier Start, CubicBezier End)> MorphMatch => _morphMatch;

    public Morph(RoundedPolygon start, RoundedPolygon end)
    {
        _start = start;
        _end = end;
        _morphMatch = Match(start, end);
    }

    public Rect CalculateBounds(bool approximate = true)
    {
        var b1 = _start.CalculateBounds(approximate);
        var b2 = _end.CalculateBounds(approximate);
        return Utils.UnionBounds(b1, b2);
    }

    public Rect CalculateMaxBounds()
    {
        var b1 = _start.CalculateMaxBounds();
        var b2 = _end.CalculateMaxBounds();
        return Utils.UnionBounds(b1, b2);
    }

    public List<CubicBezier> AsCubics(double progress)
    {
        var result = new List<CubicBezier>(_morphMatch.Count);

        // The first/last mechanism here ensures that the final anchor point in the shape
        // exactly matches the first anchor point.
        CubicBezier? firstCubic = null;
        CubicBezier? lastCubic = null;

        for (var i = 0; i < _morphMatch.Count; i++)
        {
            var cubic = CubicBezier.Interpolate(_morphMatch[i].Start, _morphMatch[i].End, progress);
            firstCubic ??= cubic;

            if (lastCubic is { })
                result.Add(lastCubic.Value);

            lastCubic = cubic;
        }

        if (lastCubic is { } && firstCubic is { })
        {
            var last = lastCubic.Value;
            var first = firstCubic.Value;
            result.Add(new CubicBezier(
                last.Anchor0,
                last.Control0,
                last.Control1,
                first.Anchor0));
        }

        return result;
    }

    /// <summary>
    /// The same as <see cref="ForEachCubic"/> but returns an enumerable.
    /// </summary>
    public IEnumerable<CubicBezier> EnumerateCubics(double progress)
    {
        for (var i = 0; i < _morphMatch.Count; i++)
            yield return CubicBezier.Interpolate(_morphMatch[i].Start, _morphMatch[i].End, progress);
    }

    public void ForEachCubic(double progress, Action<CubicBezier> callback)
    {
        foreach (var cubic in EnumerateCubics(progress))
        {
            callback(cubic);
        }
    }

    internal static List<(CubicBezier Start, CubicBezier End)> Match(RoundedPolygon p1, RoundedPolygon p2)
    {
        var measuredPolygon1 = MeasuredPolygon.MeasurePolygon(new LengthMeasurer(), p1);
        var measuredPolygon2 = MeasuredPolygon.MeasurePolygon(new LengthMeasurer(), p2);

        var features1 = new List<ProgressableFeature>(measuredPolygon1.Features);
        var features2 = new List<ProgressableFeature>(measuredPolygon2.Features);

        var doubleMapper = FeatureMapping.FeatureMapper(features1, features2);

        var polygon2CutPoint = doubleMapper.Map(0.0);

        var bs1 = measuredPolygon1;
        var bs2 = measuredPolygon2.CutAndShift(polygon2CutPoint);

        var ret = new List<(CubicBezier Start, CubicBezier End)>();

        var i1 = 0;
        var i2 = 0;

        var b1 = GetOrNull(bs1, i1++);
        var b2 = GetOrNull(bs2, i2++);

        while (b1 is { } && b2 is { })
        {
            var b1a = i1 == bs1.Count ? 1.0 : b1.EndOutlineProgress;

            var b2a =
                i2 == bs2.Count
                    ? 1.0
                    : doubleMapper.MapBack(Utils.PositiveModulo(b2.EndOutlineProgress + polygon2CutPoint, 1.0));

            var minb = Math.Min(b1a, b2a);

            (MeasuredPolygon.MeasuredCubic Seg1, MeasuredPolygon.MeasuredCubic? NewB1) seg1Result =
                b1a > minb + Utils.AngleEpsilon
                    ? CutAndKeepRemainder(b1, minb)
                    : (b1, GetOrNull(bs1, i1++));

            (MeasuredPolygon.MeasuredCubic Seg2, MeasuredPolygon.MeasuredCubic? NewB2) seg2Result =
                b2a > minb + Utils.AngleEpsilon
                    ? CutAndKeepRemainder(
                        b2,
                        Utils.PositiveModulo(doubleMapper.Map(minb) - polygon2CutPoint, 1.0))
                    : (b2, GetOrNull(bs2, i2++));

            ret.Add((seg1Result.Seg1.Cubic, seg2Result.Seg2.Cubic));

            b1 = seg1Result.NewB1;
            b2 = seg2Result.NewB2;
        }

        if (b1 is { } || b2 is { })
            throw new InvalidOperationException("Expected both Polygon's Cubics to be fully matched");

        return ret;
    }

    private static MeasuredPolygon.MeasuredCubic? GetOrNull(MeasuredPolygon polygon, int index)
    {
        return index < 0 || index >= polygon.Count ? null : polygon[index];
    }

    private static (MeasuredPolygon.MeasuredCubic Seg, MeasuredPolygon.MeasuredCubic Remainder) CutAndKeepRemainder(
        MeasuredPolygon.MeasuredCubic cubic,
        double cutOutlineProgress
    )
    {
        var (before, after) = cubic.CutAtProgress(cutOutlineProgress);
        return (before, after);
    }
}
