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

// Ported from androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\Features.kt by Shirasagi0012

using System.Collections.Immutable;
using Avalonia;

namespace MaterialShapes;

public abstract class Feature(IReadOnlyList<CubicBezier> cubics)
{
    private readonly ImmutableArray<CubicBezier> _cubics = [..cubics];
    public IReadOnlyList<CubicBezier> Cubics => _cubics;
    public abstract bool IsIgnorableFeature { get; }
    public abstract bool IsEdge { get; }
    public abstract bool IsConvexCorner { get; }
    public abstract bool IsConcaveCorner { get; }

    public abstract Feature Transformed(Func<Point, Point> f);

    public abstract Feature Reversed();

    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    public static Feature BuildIgnorableFeature(IReadOnlyList<CubicBezier> cubics)
    {
        return Validated(new EdgeFeature(cubics));
    }

    public static Feature BuildEdge(CubicBezier cubic)
    {
        return new EdgeFeature([cubic]);
    }

    public static Feature BuildConvexCorner(IReadOnlyList<CubicBezier> cubics)
    {
        return Validated(new CornerFeature(cubics, true));
    }

    public static Feature BuildConcaveCorner(IReadOnlyList<CubicBezier> cubics)
    {
        return Validated(new CornerFeature(cubics, false));
    }

    private static Feature Validated(Feature feature)
    {
        if (feature.Cubics.Count == 0)
            throw new ArgumentException("Features need at least one cubic.");

        if (!IsContinuous(feature))
            throw new ArgumentException(
                "Feature must be continuous, with the anchor points of all cubics " +
                "matching the anchor points of the preceding and succeeding cubics");

        return feature;
    }

    private static bool IsContinuous(Feature feature)
    {
        var prevCubic = feature.Cubics[0];
        for (var i = 1; i < feature.Cubics.Count; i++)
        {
            var cubic = feature.Cubics[i];
            if (Math.Abs(cubic.Anchor0.X - prevCubic.Anchor1.X) > Utils.DistanceEpsilon ||
                Math.Abs(cubic.Anchor0.Y - prevCubic.Anchor1.Y) > Utils.DistanceEpsilon)
                return false;

            prevCubic = cubic;
        }

        return true;
    }
}

internal sealed class EdgeFeature(IReadOnlyList<CubicBezier> cubics) : Feature(cubics)
{
    public override bool IsIgnorableFeature => true;
    public override bool IsEdge => true;
    public override bool IsConvexCorner => false;
    public override bool IsConcaveCorner => false;

    public override Feature Transformed(Func<Point, Point> f)
    {
        return new EdgeFeature([..Cubics.Select(c => c.Transformed(f))]);
    }

    public override Feature Reversed()
    {
        return new EdgeFeature([..Cubics.Reverse().Select(x => x.Reversed())]);
    }

    public override bool Equals(object? obj)
    {
        return obj is EdgeFeature other && Cubics.SequenceEqual(other.Cubics);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var cubic in Cubics)
            hash.Add(cubic);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return $"Edge(Cubics={Cubics.Count})";
    }
}

internal sealed class CornerFeature(IReadOnlyList<CubicBezier> cubics, bool convex) : Feature(cubics)
{
    public bool Convex { get; } = convex;

    public override bool IsIgnorableFeature => false;
    public override bool IsEdge => false;
    public override bool IsConvexCorner => Convex;
    public override bool IsConcaveCorner => !Convex;

    public override Feature Transformed(Func<Point, Point> f)
    {
        return new CornerFeature([..Cubics.Select(c => c.Transformed(f))], Convex);
    }

    public override Feature Reversed()
    {
        return new CornerFeature([..Cubics.Reverse().Select(x => x.Reversed())], !Convex);
    }

    public override bool Equals(object? obj)
    {
        return obj is CornerFeature other && Convex == other.Convex && Cubics.SequenceEqual(other.Cubics);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Convex);
        foreach (var cubic in Cubics)
            hash.Add(cubic);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return $"Corner(Convex={Convex}, Cubics={Cubics.Count})";
    }
}
