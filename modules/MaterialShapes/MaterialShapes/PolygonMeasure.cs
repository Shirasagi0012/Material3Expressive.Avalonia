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

// Ported from androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\PolygonMeasure.kt by Shirasagi0012

using System.Collections;
using Avalonia;

namespace MaterialShapes;

internal sealed class MeasuredPolygon : IReadOnlyList<MeasuredPolygon.MeasuredCubic>
{
    private readonly IMeasurer _measurer;
    private readonly List<MeasuredCubic> _cubics;

    internal IReadOnlyList<ProgressableFeature> Features { get; }

    private MeasuredPolygon(
        IMeasurer measurer,
        IReadOnlyList<ProgressableFeature> features,
        IReadOnlyList<CubicBezier> cubics,
        IReadOnlyList<double> outlineProgress
    )
    {
        if (cubics.Count == 0)
            throw new ArgumentException("MeasuredPolygon expects at least one cubic.", nameof(cubics));

        if (outlineProgress.Count != cubics.Count + 1)
            throw new ArgumentException("Outline progress size is expected to be the cubics size + 1",
                nameof(outlineProgress));

        if (outlineProgress[0] != 0.0)
            throw new ArgumentException("First outline progress value is expected to be zero", nameof(outlineProgress));

        if (outlineProgress[^1] != 1.0)
            throw new ArgumentException("Last outline progress value is expected to be one", nameof(outlineProgress));

        _measurer = measurer;
        Features = features;

        var measuredCubics = new List<MeasuredCubic>(cubics.Count);
        var startOutlineProgress = 0.0;

        for (var index = 0; index < cubics.Count; index++)
            if (outlineProgress[index + 1] - outlineProgress[index] > Utils.DistanceEpsilon)
            {
                measuredCubics.Add(new MeasuredCubic(
                    measurer,
                    cubics[index],
                    startOutlineProgress,
                    outlineProgress[index + 1]));

                startOutlineProgress = outlineProgress[index + 1];
            }

        if (measuredCubics.Count == 0)
            measuredCubics.Add(new MeasuredCubic(measurer, cubics[0], 0.0, 1.0));
        else
            measuredCubics[^1].UpdateProgressRange(endOutlineProgress: 1.0);

        _cubics = measuredCubics;
    }

    internal sealed class MeasuredCubic
    {
        private readonly IMeasurer _measurer;

        internal CubicBezier Cubic { get; }
        internal double MeasuredSize { get; }

        internal double StartOutlineProgress { get; private set; }
        internal double EndOutlineProgress { get; private set; }

        internal MeasuredCubic(
            IMeasurer measurer,
            CubicBezier cubic,
            double startOutlineProgress,
            double endOutlineProgress
        )
        {
            if (endOutlineProgress < startOutlineProgress)
                throw new ArgumentException(
                    "endOutlineProgress is expected to be equal or greater than startOutlineProgress");

            _measurer = measurer;
            Cubic = cubic;

            StartOutlineProgress = startOutlineProgress;
            EndOutlineProgress = endOutlineProgress;

            var size = measurer.MeasureCubic(cubic);
            if (size < 0)
                throw new ArgumentException("Measured cubic is expected to be greater or equal to zero");

            MeasuredSize = size;
        }

        internal void UpdateProgressRange(double? startOutlineProgress = null, double? endOutlineProgress = null)
        {
            var start = startOutlineProgress ?? StartOutlineProgress;
            var end = endOutlineProgress ?? EndOutlineProgress;

            if (end < start)
                throw new ArgumentException(
                    "endOutlineProgress is expected to be equal or greater than startOutlineProgress");

            StartOutlineProgress = start;
            EndOutlineProgress = end;
        }

        internal (MeasuredCubic Before, MeasuredCubic After) CutAtProgress(double cutOutlineProgress)
        {
            var boundedCutOutlineProgress = Math.Clamp(cutOutlineProgress, StartOutlineProgress, EndOutlineProgress);

            var outlineProgressSize = EndOutlineProgress - StartOutlineProgress;
            if (outlineProgressSize <= 0)
                return (
                    new MeasuredCubic(_measurer, Cubic, StartOutlineProgress, boundedCutOutlineProgress),
                    new MeasuredCubic(_measurer, Cubic, boundedCutOutlineProgress, EndOutlineProgress)
                );

            var progressFromStart = boundedCutOutlineProgress - StartOutlineProgress;
            var relativeProgress = progressFromStart / outlineProgressSize;

            var t = _measurer.FindCubicCutPoint(Cubic, relativeProgress * MeasuredSize);
            if (t is < 0.0 or > 1.0)
                throw new ArgumentOutOfRangeException(nameof(cutOutlineProgress),
                    "Cubic cut point is expected to be between 0 and 1");

            var (c1, c2) = Cubic.Split(t);
            return (
                new MeasuredCubic(_measurer, c1, StartOutlineProgress, boundedCutOutlineProgress),
                new MeasuredCubic(_measurer, c2, boundedCutOutlineProgress, EndOutlineProgress)
            );
        }

        public override string ToString()
        {
            return
                $"MeasuredCubic(outlineProgress=[{StartOutlineProgress} .. {EndOutlineProgress}], size={MeasuredSize}, cubic={Cubic})";
        }
    }

    internal MeasuredPolygon CutAndShift(double cuttingPoint)
    {
        if (cuttingPoint is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(cuttingPoint),
                "Cutting point is expected to be between 0 and 1");

        if (cuttingPoint < Utils.DistanceEpsilon || 1.0 - cuttingPoint < Utils.DistanceEpsilon)
            return this;

        var targetIndex = -1;
        for (var i = 0; i < _cubics.Count; i++)
            if (cuttingPoint >= _cubics[i].StartOutlineProgress && cuttingPoint <= _cubics[i].EndOutlineProgress)
            {
                targetIndex = i;
                break;
            }

        if (targetIndex < 0)
            throw new InvalidOperationException("Could not find a cubic that crosses the cutting point.");

        var target = _cubics[targetIndex];
        var (b1, b2) = target.CutAtProgress(cuttingPoint);

        var retCubics = new List<CubicBezier>(_cubics.Count + 1) { b2.Cubic };
        for (var i = 1; i < _cubics.Count; i++) retCubics.Add(_cubics[(i + targetIndex) % _cubics.Count].Cubic);

        retCubics.Add(b1.Cubic);

        var retOutlineProgress = new List<double>(_cubics.Count + 2);
        for (var index = 0; index < _cubics.Count + 2; index++)
            retOutlineProgress.Add(index switch
            {
                0 => 0.0,
                var x when x == _cubics.Count + 1 => 1.0,
                _ =>
                    Utils.PositiveModulo(
                        _cubics[(targetIndex + index - 1) % _cubics.Count].EndOutlineProgress - cuttingPoint, 1.0)
            });

        var newFeatures = new List<ProgressableFeature>(Features.Count);
        for (var i = 0; i < Features.Count; i++)
        {
            var f = Features[i];
            newFeatures.Add(new ProgressableFeature(
                Utils.PositiveModulo(f.Progress - cuttingPoint, 1.0),
                f.Feature));
        }

        return new MeasuredPolygon(_measurer, newFeatures, retCubics, retOutlineProgress);
    }

    public int Count => _cubics.Count;

    public MeasuredCubic this[int index] => _cubics[index];

    public IEnumerator<MeasuredCubic> GetEnumerator()
    {
        return _cubics.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal static MeasuredPolygon MeasurePolygon(IMeasurer measurer, RoundedPolygon polygon)
    {
        var cubics = new List<CubicBezier>();
        var featureToCubic = new List<(Feature feature, int cubicIndex)>();

        if (polygon.Features.Count == 0)
            cubics.AddRange(polygon.Cubics);
        else
            foreach (var feature in polygon.Features)
                for (var cubicIndex = 0; cubicIndex < feature.Cubics.Count; cubicIndex++)
                {
                    if (feature is CornerFeature && cubicIndex == feature.Cubics.Count / 2)
                        featureToCubic.Add((feature, cubics.Count));

                    cubics.Add(feature.Cubics[cubicIndex]);
                }

        var measures = new double[cubics.Count + 1];
        for (var i = 0; i < cubics.Count; i++)
        {
            var size = measurer.MeasureCubic(cubics[i]);
            if (size < 0)
                throw new ArgumentException("Measured cubic is expected to be greater or equal to zero");

            measures[i + 1] = measures[i] + size;
        }

        var totalMeasure = measures[^1];

        var outlineProgress = new double[measures.Length];
        if (totalMeasure <= 0)
        {
            var denom = Math.Max(1, outlineProgress.Length - 1);
            for (var i = 0; i < outlineProgress.Length; i++)
                outlineProgress[i] = (double)i / denom;
        }
        else
        {
            for (var i = 0; i < measures.Length; i++)
                outlineProgress[i] = measures[i] / totalMeasure;
        }

        outlineProgress[0] = 0.0;
        outlineProgress[^1] = 1.0;

        var features = new List<ProgressableFeature>(featureToCubic.Count);
        for (var i = 0; i < featureToCubic.Count; i++)
        {
            var (feature, ix) = featureToCubic[i];
            var p = Utils.PositiveModulo((outlineProgress[ix] + outlineProgress[ix + 1]) / 2.0, 1.0);
            features.Add(new ProgressableFeature(p, feature));
        }

        return new MeasuredPolygon(measurer, features, cubics, outlineProgress);
    }
}

internal interface IMeasurer
{
    double MeasureCubic(CubicBezier c);
    double FindCubicCutPoint(CubicBezier c, double m);
}

internal sealed class LengthMeasurer : IMeasurer
{
    // Minimum number needed to achieve the expected accuracy (see upstream implementation).
    private const int Segments = 3;

    public double MeasureCubic(CubicBezier c)
    {
        return ClosestProgressTo(c, Double.PositiveInfinity).total;
    }

    public double FindCubicCutPoint(CubicBezier c, double m)
    {
        if (Double.IsNaN(m) || m <= 0)
            return 0.0;

        return ClosestProgressTo(c, m).progress;
    }

    private static (double progress, double total) ClosestProgressTo(CubicBezier cubic, double threshold)
    {
        var total = 0.0;
        var remainder = threshold;
        var prev = new Point(cubic.Anchor0.X, cubic.Anchor0.Y);

        for (var i = 1; i <= Segments; i++)
        {
            var progress = (double)i / Segments;
            var point = cubic.PointOnCurve(progress);
            var segment = (point - prev).GetDistance();

            if (remainder <= 0)
                return (Math.Max(0.0, progress - 1.0 / Segments), threshold);

            if (segment > 0 && segment >= remainder)
            {
                var t = progress - (1.0 - remainder / segment) / Segments;
                return (Math.Clamp(t, 0.0, 1.0), threshold);
            }

            remainder -= segment;
            total += segment;
            prev = point;
        }

        return (1.0, total);
    }
}