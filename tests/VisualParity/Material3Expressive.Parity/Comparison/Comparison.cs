using SkiaSharp;
using System.Globalization;

namespace Material3Expressive.Parity;

public static class ParityComparison
{
    public static FieldDifference Field(string name, Measurement? expected, Measurement? actual)
    {
        if (expected is null || actual is null)
            return new(name, "notCompared", expected, actual, Reason: "Measurement absent on one side.",
                Issue: new("measurementAbsent", [expected is null ? "expected" : "actual"]));
        if (expected.Status != "measured" || actual.Status != "measured")
            return new(name, "notCompared", expected, actual, Reason: $"compose={expected.Status}; avalonia={actual.Status}",
                Issue: new("measurementUnavailable",
                    new[] { expected.Status != "measured" ? "expected.status" : null, actual.Status != "measured" ? "actual.status" : null }
                        .OfType<string>().ToArray()));
        var mismatches = new[]
            {
                ("category", expected.Category, actual.Category), ("kind", expected.Kind, actual.Kind),
                ("unit", expected.Unit, actual.Unit), ("space", expected.Space, actual.Space),
                ("definition", expected.Definition, actual.Definition)
            }
            .Where(x => x.Item2 != x.Item3).Select(x => x.Item1)
            .Concat(expected.ComponentNames().SequenceEqual(actual.ComponentNames()) ? [] : new[] { "components" }).ToArray();
        if (mismatches.Length != 0)
            return new(name, "notCompared", expected, actual, Reason: "Measurement descriptors differ: " + string.Join(", ", mismatches),
                Issue: new("descriptorMismatch", mismatches));
        if (expected.Kind == "boolean") return new(name, expected.Boolean == actual.Boolean ? "equal" : "different", expected, actual);
        if (expected.Numbers is { } numbers)
        {
            var delta = actual.Numbers!.Zip(numbers, (a, e) => a - e).ToArray();
            return new(name, delta.All(x => x == 0) ? "equal" : "different", expected, actual, delta);
        }

        return new(name, expected.Strings!.SequenceEqual(actual.Strings!) ? "equal" : "different", expected, actual);
    }

    public static Comparison Run(
        CaseMetadata metadata,
        Capture? expected,
        Capture? actual,
        string expectedDirectory,
        string actualDirectory,
        string output,
        string kind = "parity",
        string? executionError = null
    )
    {
        expected?.Validate();
        actual?.Validate();
        var errors = (expected?.Errors ?? []).Select(x => "compose: " + x).Concat((actual?.Errors ?? []).Select(x => "avalonia: " + x))
            .ToList();
        if (executionError is not null) errors.Add(executionError);
        if (expected is not null && expected.Runner != "compose" || actual is not null && actual.Runner != "avalonia")
            errors.Add("Capture runner roles differ.");
        if (expected is not null && actual is not null &&
            (expected.CaseId != metadata.Id || actual.CaseId != metadata.Id || expected.MetadataSha256 != actual.MetadataSha256 ||
             expected.BaselineSha256 != actual.BaselineSha256))
            errors.Add("Capture identity, metadata or baseline differs.");
        var environments = Keys(expected?.Environment.Keys, actual?.Environment.Keys)
            .Select(key => Field(key, expected?.Environment.TryGetValue(key, out var e) == true ? Measurement.Text(key, e) : null,
                actual?.Environment.TryGetValue(key, out var a) == true ? Measurement.Text(key, a) : null)).ToArray();
        // Runtime provenance differs intentionally; it is evidence, not component parity coverage.
        var samples = Keys(expected?.Samples.Select(x => x.Id), actual?.Samples.Select(x => x.Id)).Select(id =>
        {
            var e = expected?.Samples.SingleOrDefault(x => x.Id == id);
            var a = actual?.Samples.SingleOrDefault(x => x.Id == id);
            var sampleMembers = new[]
            {
                e is null ? "expected" : null, a is null ? "actual" : null,
                e is not null && a is not null && e.TimeUs != a.TimeUs ? "timeUs" : null,
                e is not null && a is not null && e.FrameTimeUs != a.FrameTimeUs ? "frameTimeUs" : null,
                e is not null && a is not null && e.Stage != a.Stage ? "stage" : null,
                executionError is null ? null : "executionError",
                errors.Any(x => x is "Capture identity, metadata or baseline differs." or "Capture runner roles differ.")
                    ? "captureIdentity"
                    : null
            }.OfType<string>().ToArray();
            var sampleIssue = sampleMembers.Length == 0 ? null : new ComparisonIssue("sampleMismatch", sampleMembers);
            var sampleReason = sampleIssue is null ? null : "Sample cannot be paired: " + string.Join(", ", sampleMembers);
            var points = Keys(e?.Points.Select(x => x.Id), a?.Points.Select(x => x.Id)).Select(pid =>
            {
                var ep = e?.Points.SingleOrDefault(x => x.Id == pid);
                var ap = a?.Points.SingleOrDefault(x => x.Id == pid);
                var pointMembers = new[]
                {
                    ep is null ? "expected" : null, ap is null ? "actual" : null,
                    ep is not null && ap is not null && ep.Role != ap.Role ? "role" : null,
                    ep is not null && ap is not null && ep.ParentId != ap.ParentId ? "parentId" : null
                }.OfType<string>().ToArray();
                var issue = sampleIssue ?? (pointMembers.Length == 0 ? null : new ComparisonIssue("pointMismatch", pointMembers));
                var reason = sampleReason ?? (issue is null ? null : "Point cannot be paired: " + string.Join(", ", pointMembers));
                var fields = Keys(ep?.Measurements.Select(x => x.Name), ap?.Measurements.Select(x => x.Name))
                    .Select(name => reason is null
                        ? Field(name, ep?.Measurements.SingleOrDefault(x => x.Name == name),
                            ap?.Measurements.SingleOrDefault(x => x.Name == name))
                        : new FieldDifference(name, "notCompared", ep?.Measurements.SingleOrDefault(x => x.Name == name),
                            ap?.Measurements.SingleOrDefault(x => x.Name == name), Reason: reason, Issue: issue)).ToArray();
                return new PointDifference(pid, ep?.Role ?? ap?.Role, ep?.ParentId ?? ap?.ParentId,
                    reason is null ? Status(fields.Select(x => x.Status)) : "notCompared", fields, reason, ep?.Role, ap?.Role, ep?.ParentId,
                    ap?.ParentId, issue);
            }).ToArray();
            var rasters = Keys(e?.Rasters.Select(x => x.Id), a?.Rasters.Select(x => x.Id)).Select(rid => sampleReason is not null
                    ? new RasterDifference(rid, "notCompared", e?.Rasters.SingleOrDefault(x => x.Id == rid),
                        a?.Rasters.SingleOrDefault(x => x.Id == rid), Reason: sampleReason, Issue: sampleIssue)
                    : Raster(id, rid, e!, a!, expectedDirectory, actualDirectory, output, expected!.Environment, actual!.Environment))
                .ToArray();
            return new SampleDifference(id, e?.TimeUs, a?.TimeUs, e?.Stage ?? a?.Stage,
                sampleReason is null ? Status(points.Select(x => x.Status).Concat(rasters.Select(x => x.Status))) : "notCompared", points,
                rasters, sampleReason, e?.Stage, a?.Stage, e?.FrameTimeUs, a?.FrameTimeUs, sampleIssue);
        }).OrderBy(x => x.ExpectedTimeUs ?? x.ActualTimeUs).ThenBy(x => x.Id).ToArray();
        var statuses = samples.SelectMany(s => s.Status == "notCompared" && s.Points.Length == 0 && s.Rasters.Length == 0
            ? ["notCompared"]
            : s.Points.SelectMany(p => p.Fields.Length == 0 ? ["notCompared"] : p.Fields.Select(f => f.Status))
                .Concat(s.Rasters.Select(x => x.Status))).ToArray();
        return new(2, metadata.Id, errors.Count != 0 ? "error" : Status(statuses),
            new(statuses.Count(x => x == "equal"), statuses.Count(x => x == "different"), statuses.Count(x => x == "notCompared"),
                statuses.Count(x => x == "withinTolerance")),
            samples, errors.ToArray(), environments, kind);
    }

    public static IEnumerable<string> Keys(IEnumerable<string>? a, IEnumerable<string>? b) => (a ?? []).Concat(b ?? []).Distinct().Order();

    public static string Status(IEnumerable<string> statuses)
    {
        var s = statuses.ToArray();
        return s.Contains("different") ? "different" : s.Contains("notCompared") || s.Length == 0 ? "notCompared"
            : s.Contains("withinTolerance") ? "withinTolerance" : "equal";
    }

    static RasterDifference Raster(
        string sampleId,
        string id,
        Sample e,
        Sample a,
        string ed,
        string ad,
        string output,
        Dictionary<string, string> ee,
        Dictionary<string, string> ae
    )
    {
        var er = e.Rasters.SingleOrDefault(x => x.Id == id);
        var ar = a.Rasters.SingleOrDefault(x => x.Id == id);
        if (er is null || ar is null)
            return new(id, "notCompared", er, ar, Reason: "Raster absent on one side.",
                Issue: new("rasterAbsent", [er is null ? "expected" : "actual"]));
        var members = new[]
        {
            er.Width != ar.Width ? "width" : null, er.Height != ar.Height ? "height" : null,
            er.Format != ar.Format ? "format" : null, er.Space != ar.Space ? "space" : null,
            er.Contribution != ar.Contribution ? "contribution" : null,
            ee.GetValueOrDefault("density") != ae.GetValueOrDefault("density") ? "density" : null,
            ee.GetValueOrDefault("fontScale") != ae.GetValueOrDefault("fontScale") ? "fontScale" : null
        }.OfType<string>().ToArray();
        if (members.Length != 0)
            return new(id, "notCompared", er, ar, Reason: "Raster descriptors differ: " + string.Join(", ", members),
                Issue: new("rasterDescriptorMismatch", members));
        var eb = PixelData.Read(ed, er);
        var ab = PixelData.Read(ad, ar);
        var metrics = Pixels(eb, ab, er.Width, er.Height, [0, 0, er.Width, er.Height]);
        var regions = new List<RasterRegion>();
        if (double.TryParse(ee.GetValueOrDefault("density"), NumberStyles.Float, CultureInfo.InvariantCulture, out var density)
            && double.IsFinite(density) && density > 0)
            foreach (var pid in Keys(e.Points.Select(x => x.Id), a.Points.Select(x => x.Id)))
            {
                var expectedPoint = e.Points.SingleOrDefault(x => x.Id == pid);
                var actualPoint = a.Points.SingleOrDefault(x => x.Id == pid);
                if (expectedPoint is null || actualPoint is null || expectedPoint.Role != actualPoint.Role ||
                    expectedPoint.ParentId != actualPoint.ParentId)
                    continue;

                double[]? Box(Sample s) => s.Points.SingleOrDefault(x => x.Id == pid)?.Measurements
                    .SingleOrDefault(x => x.Name == "box" && x.Status == "measured" && x.Kind == "rect" && x.Unit == "dip"
                                          && x.Space == "scene" && x.Definition == "layout.box.aabb.unclipped")?.Numbers;

                var x = Box(e);
                var y = Box(a);
                if (x is null || y is null) continue;
                int Clamp(double v, int max) => (int)Math.Clamp(v, 0, max);
                var left = Clamp(Math.Floor(Math.Min(x[0], y[0]) * density), er.Width);
                var top = Clamp(Math.Floor(Math.Min(x[1], y[1]) * density), er.Height);
                var right = Clamp(Math.Ceiling(Math.Max(x[0] + x[2], y[0] + y[2]) * density), er.Width);
                var bottom = Clamp(Math.Ceiling(Math.Max(x[1] + x[3], y[1] + y[3]) * density), er.Height);
                var box = new[] { left, top, Math.Max(0, right - left), Math.Max(0, bottom - top) };
                regions.Add(new(pid, box, Pixels(eb, ab, er.Width, er.Height, box)));
            }

        var diff = new byte[eb.Length];
        using var bitmap = new SKBitmap(new SKImageInfo(er.Width, er.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        for (var i = 0; i < diff.Length; i += 4)
        {
            var m = (byte)Math.Max(
                Math.Max(Math.Abs(ab[i] - eb[i]), Math.Abs(ab[i + 1] - eb[i + 1])),
                Math.Max(Math.Abs(ab[i + 2] - eb[i + 2]), Math.Abs(ab[i + 3] - eb[i + 3])));
            diff[i] = diff[i + 1] = diff[i + 2] = m;
            diff[i + 3] = 255;
        }
        // Opaque grayscale of the maximum RGBA delta; write the packed buffer once,
        // avoiding millions of per-pixel allocations and Skia drawing calls at 4x DPI.
        System.Runtime.InteropServices.Marshal.Copy(diff, 0, bitmap.GetPixels(), diff.Length);

        var image = $"diff/{sampleId}-{id}.png";
        Directory.CreateDirectory(Path.Combine(output, "diff"));
        using (var data = bitmap.Encode(SKEncodedImageFormat.Png, 100)) File.WriteAllBytes(Path.Combine(output, image), data.ToArray());
        var exactStatus = metrics.DifferentPixels == 0 ? "equal" : "different";
        // Only the fixed 4x capture contract can use this tolerance. Other inputs retain exact comparison.
        var tolerance = density == 4 && er.Width % 4 == 0 && er.Height % 4 == 0
            ? AssessRaster(eb, ab, er.Width, er.Height, regions.ToArray()) : null;
        var status = exactStatus == "equal" ? "equal" : tolerance?.Status == "withinTolerance" ? "withinTolerance" : "different";
        return new(id, status, er, ar, metrics, regions.ToArray(), image, ExactStatus: exactStatus, Tolerance: tolerance);
    }

    public static RasterTolerance AssessRaster(byte[] expected, byte[] actual, int width, int height, RasterRegion[] regions)
    {
        const int factor = 4;
        const int maximumLimit = 20;
        const double meanLimit = 1;
        if (width <= 0 || height <= 0 || width % factor != 0 || height % factor != 0 ||
            expected.Length != checked(width * height * 4) || actual.Length != expected.Length)
            throw new InvalidDataException("Raster tolerance requires complete 4x4 pixel blocks.");
        var w = width / factor;
        var h = height / factor;
        var deltas = new byte[w * h];
        // Average encoded straight-sRGBA8 channels independently, including alpha and hidden RGB.
        // Integer sums and half-up rounding make results independent of resize libraries and floating point ties.
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        for (var c = 0; c < 4; c++)
        {
            var e = 0;
            var a = 0;
            for (var dy = 0; dy < factor; dy++)
            for (var dx = 0; dx < factor; dx++)
            {
                var offset = (((y * factor + dy) * width) + x * factor + dx) * 4 + c;
                e += expected[offset];
                a += actual[offset];
            }
            var delta = Math.Abs((a + 8) / 16 - (e + 8) / 16);
            deltas[y * w + x] = (byte)Math.Max(deltas[y * w + x], delta);
        }

        RasterToleranceRegion Measure(string? pointId, int[] box)
        {
            long sum = 0;
            var max = 0;
            for (var y = box[1]; y < box[1] + box[3]; y++)
            for (var x = box[0]; x < box[0] + box[2]; x++)
            {
                var delta = deltas[y * w + x];
                max = Math.Max(max, delta);
                sum += delta;
            }
            var mean = (double)sum / ((long)box[2] * box[3]);
            return new(pointId, box, max, mean, max <= maximumLimit && mean <= meanLimit);
        }

        var full = Measure(null, [0, 0, w, h]);
        var local = new List<RasterToleranceRegion>();
        foreach (var region in regions)
        {
            var b = region.Bounds;
            if (b.Length != 4 || b.Any(v => v < 0) || (long)b[0] + b[2] > width || (long)b[1] + b[3] > height)
                throw new InvalidDataException("Invalid raster tolerance region.");
            if (b[2] == 0 || b[3] == 0) continue;
            var left = b[0] / factor;
            var top = b[1] / factor;
            var right = (b[0] + b[2] + factor - 1) / factor;
            var bottom = (b[1] + b[3] + factor - 1) / factor;
            local.Add(Measure(region.PointId, [left, top, right - left, bottom - top]));
        }
        return new(full.Accepted && local.All(r => r.Accepted) ? "withinTolerance" : "different",
            "box-4x4-straight-srgba8-half-up", 1, maximumLimit, meanLimit, full, local.ToArray());
    }

    public static PixelMetrics Pixels(byte[] e, byte[] a, int width, int height, int[] region)
    {
        if (e.Length != checked(width * height * 4) || a.Length != e.Length || region.Length != 4
            || region[0] < 0 || region[1] < 0 || region[2] < 0 || region[3] < 0 || (long)region[0] + region[2] > width ||
            (long)region[1] + region[3] > height)
            throw new InvalidDataException("Invalid raster buffer or region.");
        long changed = 0;
        var max = new int[4];
        var total = new long[4];
        var left = width;
        var top = height;
        var right = -1;
        var bottom = -1;
        for (var y = region[1]; y < region[1] + region[3]; y++)
        for (var x = region[0]; x < region[0] + region[2]; x++)
        {
            var different = false;
            var offset = (y * width + x) * 4;
            for (var c = 0; c < 4; c++)
            {
                var d = Math.Abs(a[offset + c] - e[offset + c]);
                max[c] = Math.Max(max[c], d);
                total[c] += d;
                different |= d != 0;
            }

            if (!different) continue;
            changed++;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x);
            bottom = Math.Max(bottom, y);
        }

        var count = (long)region[2] * region[3];
        return new(changed, max, total.Select(x => count == 0 ? 0 : (double)x / count).ToArray(),
            changed == 0 ? null : [left, top, right - left + 1, bottom - top + 1]);
    }
}
