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

// Ported from androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\FeatureMapping.kt by Shirasagi0012

using Avalonia;
using MeasuredFeatures = System.Collections.Generic.List<MaterialShapes.ProgressableFeature>;

namespace MaterialShapes;

internal record class ProgressableFeature(double Progress, Feature Feature);

internal record class DistanceVertex(double Distance, ProgressableFeature F1, ProgressableFeature F2);

public static class FeatureMapping
{
    internal static DoubleMapper FeatureMapper(MeasuredFeatures features1, MeasuredFeatures features2)
    {
        var filteredFeatures1 = features1
            .Where(f => f.Feature is CornerFeature)
            .ToList();

        var filteredFeatures2 = features2
            .Where(f => f.Feature is CornerFeature)
            .ToList();

        var featureProgressMapping = DoMapping(filteredFeatures1, filteredFeatures2);
        var dm = new DoubleMapper(featureProgressMapping.ToArray());
        return dm;
    }

    internal static List<ValueTuple<double, double>> DoMapping(MeasuredFeatures features1, MeasuredFeatures features2)
    {
        var distanceVertexList =
            features1
                .SelectMany(f1 => features2, (f1, f2) => new { f1, f2 })
                .Select(t => new { t, d = FeatureDistSquared(t.f1.Feature, t.f2.Feature) })
                .Where(t => t.d != Double.MaxValue)
                .Select(t => new DistanceVertex(t.d, t.t.f1, t.t.f2)).ToList();

        distanceVertexList.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        if (distanceVertexList.Count == 0)
            return IdentityMapping;

        if (distanceVertexList.Count == 1)
        {
            var it = distanceVertexList[0];
            var f1 = it.F1.Progress;
            var f2 = it.F2.Progress;
            return
            [
                (f1, f2),
                ((f1 + 0.5D) % 1D, (f2 + 0.5D) % 1D)
            ];
        }

        var helper = new MappingHelper();
        foreach (var vertex in distanceVertexList)
            helper.AddMapping(vertex.F1, vertex.F2);

        return helper.Mapping;
    }

    private static readonly List<ValueTuple<double, double>> IdentityMapping = [(0.0, 0.0), (0.5, 0.5)];

    private class MappingHelper
    {
        public List<ValueTuple<double, double>> Mapping { get; } = [];
        private List<ProgressableFeature> _usedF1 = [];
        private List<ProgressableFeature> _usedF2 = [];

        public void AddMapping(ProgressableFeature f1, ProgressableFeature f2)
        {
            if (_usedF1.Contains(f1) || _usedF2.Contains(f2))
                return;

            var index = Mapping.Select(m => m.Item1)
                .ToList().BinarySearch(f1.Progress);

            if (index >= 0)
                throw new InvalidOperationException("There can't be two features with the same progress");

            var insertionIndex = ~index;
            var n = Mapping.Count;

            if (n >= 1)
            {
                var (before1, before2) = Mapping[(insertionIndex + n - 1) % n];
                var (after1, after2) = Mapping[insertionIndex % n];

                if (DoubleMapper.ProgressDistance(f1.Progress, before1) < Utils.DistanceEpsilon ||
                    DoubleMapper.ProgressDistance(f1.Progress, after1) < Utils.DistanceEpsilon ||
                    DoubleMapper.ProgressDistance(f2.Progress, before2) < Utils.DistanceEpsilon ||
                    DoubleMapper.ProgressDistance(f2.Progress, after2) < Utils.DistanceEpsilon)
                    return;

                if (n > 1 && !DoubleMapper.IsProgressInRange(f2.Progress, before2, after2))
                    return;
            }

            Mapping.Insert(insertionIndex, (f1.Progress, f2.Progress));
            _usedF1.Add(f1);
            _usedF2.Add(f2);
        }
    }

    internal static double FeatureDistSquared(Feature f1, Feature f2)
    {
        if (f1 is CornerFeature c1 && f2 is CornerFeature c2 && c1.Convex != c2.Convex)
            return Double.MaxValue;

        var p1 = FeatureRepresentativePoint(f1);
        var p2 = FeatureRepresentativePoint(f2);

        return (p1 - p2).GetDistanceSquared();
    }

    internal static Point FeatureRepresentativePoint(Feature feature)
    {
        var firstCubic = feature.Cubics.First();
        var lastCubic = feature.Cubics.Last();

        var x = (firstCubic.Anchor0.X + lastCubic.Anchor1.X) / 2D;
        var y = (firstCubic.Anchor0.Y + lastCubic.Anchor1.Y) / 2D;

        return new Point(x, y);
    }
}