using SkiaSharp;

namespace Material3Expressive.Parity;

public sealed record ComparisonProfile(int SchemaVersion, string Name, string Provenance, double LayoutDip,
    double MotionAbsolute, int ChannelDelta, int MaximumDifferentPixels)
{
    public void Validate()
    {
        if (SchemaVersion != 1 || !double.IsFinite(LayoutDip) || LayoutDip < 0 || !double.IsFinite(MotionAbsolute)
            || MotionAbsolute < 0 || ChannelDelta is < 0 or > 255 || MaximumDifferentPixels < 0 || string.IsNullOrWhiteSpace(Provenance))
            throw new InvalidDataException("Invalid comparison profile.");
    }
}
public sealed record RasterMetrics(int DifferentPixels, int MaximumChannelDelta, Bounds? DifferenceBounds,
    double ExpectedInk, double ActualInk, double ContourDistance);

public static class ImageComparison
{
    // Difference of the real full draw and paint-suppressed draw. This is a text paint
    // contribution map, not an inferred alpha mask or a substitute for shaping evidence.
    public static SKBitmap TextPaint(SKBitmap full, SKBitmap withoutText)
    {
        if (full.Width != withoutText.Width || full.Height != withoutText.Height)
            throw new InvalidDataException("Text diagnostic dimensions differ.");
        var result = new SKBitmap(full.Width, full.Height);
        for (var y = 0; y < full.Height; y++)
        for (var x = 0; x < full.Width; x++)
        {
            var a = full.GetPixel(x, y); var b = withoutText.GetPixel(x, y);
            var d = (byte)Math.Max(Math.Max(Math.Abs(a.Red - b.Red), Math.Abs(a.Green - b.Green)),
                Math.Max(Math.Abs(a.Blue - b.Blue), Math.Abs(a.Alpha - b.Alpha)));
            result.SetPixel(x, y, new SKColor(d, d, d));
        }
        return result;
    }
    public static void Save(SKBitmap bitmap, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path); data.SaveTo(stream);
    }
    // Integer sRGB colors are decoded to unpremultiplied RGBA by Skia. Coordinates are never aligned.
    public static RasterMetrics Compare(SKBitmap expected, SKBitmap actual, SKColor background,
        int channelDelta, SKBitmap? difference = null, Bounds? region = null)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            throw new InvalidDataException("Image dimensions differ.");
        var w = expected.Width; var h = expected.Height;
        var different = 0; var maximum = 0; var minX = w; var minY = h; var maxX = -1; var maxY = -1;
        var expectedMask = new bool[w * h]; var actualMask = new bool[w * h];
        double expectedInk = 0, actualInk = 0;
        int Delta(SKColor x, SKColor y) => Math.Max(Math.Max(Math.Abs(x.Red - y.Red), Math.Abs(x.Green - y.Green)),
            Math.Max(Math.Abs(x.Blue - y.Blue), Math.Abs(x.Alpha - y.Alpha)));
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            if (region is not null && (x < region.X || y < region.Y || x >= region.X + region.Width || y >= region.Y + region.Height)) continue;
            var e = expected.GetPixel(x, y); var a = actual.GetPixel(x, y);
            var d = Delta(e, a);
            maximum = Math.Max(maximum, d);
            if (d > channelDelta)
            {
                different++; minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            }
            difference?.SetPixel(x, y, d > channelDelta ? new SKColor(255, 0, (byte)d) : new SKColor(235, 235, 235));
            var ei = Delta(e, background); var ai = Delta(a, background);
            expectedInk += ei / 255d; actualInk += ai / 255d;
            expectedMask[y * w + x] = ei > 16; actualMask[y * w + x] = ai > 16;
        }
        var eEdges = Edges(expectedMask, w, h); var aEdges = Edges(actualMask, w, h);
        var eDistances = Distances(eEdges, w, h); var aDistances = Distances(aEdges, w, h);
        var contour = Math.Max(eEdges.Select((v, i) => v ? aDistances[i] : 0).Max(),
            aEdges.Select((v, i) => v ? eDistances[i] : 0).Max());
        return new(different, maximum, maxX < 0 ? null : new(minX, minY, maxX - minX + 1, maxY - minY + 1),
            expectedInk, actualInk, contour);
    }
    private static bool[] Edges(bool[] mask, int w, int h) => mask.Select((v, i) => v &&
        (i % w == 0 || i % w == w - 1 || i < w || i >= mask.Length - w || !mask[i - 1] || !mask[i + 1] || !mask[i - w] || !mask[i + w])).ToArray();
    private static double[] Distances(bool[] edges, int w, int h)
    {
        // Chamfer distance is diagnostic only; no acceptance threshold is inferred from it.
        var d = edges.Select(x => x ? 0d : (double)(w + h)).ToArray();
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = y * w + x;
            if (x > 0) d[i] = Math.Min(d[i], d[i - 1] + 1);
            if (y > 0) d[i] = Math.Min(d[i], d[i - w] + 1);
            if (x > 0 && y > 0) d[i] = Math.Min(d[i], d[i - w - 1] + Math.Sqrt(2));
            if (x + 1 < w && y > 0) d[i] = Math.Min(d[i], d[i - w + 1] + Math.Sqrt(2));
        }
        for (var y = h - 1; y >= 0; y--)
        for (var x = w - 1; x >= 0; x--)
        {
            var i = y * w + x;
            if (x + 1 < w) d[i] = Math.Min(d[i], d[i + 1] + 1);
            if (y + 1 < h) d[i] = Math.Min(d[i], d[i + w] + 1);
            if (x + 1 < w && y + 1 < h) d[i] = Math.Min(d[i], d[i + w + 1] + Math.Sqrt(2));
            if (x > 0 && y + 1 < h) d[i] = Math.Min(d[i], d[i + w - 1] + Math.Sqrt(2));
        }
        return d;
    }
}

public static class ParityComparison
{
    public static Comparison Run(Scenario s, Capture expected, Capture actual, ComparisonProfile profile,
        string expectedDirectory, string actualDirectory, string resultDirectory)
    {
        profile.Validate();
        var findings = new List<Finding>();
        void Number(int frame, string subject, string metric, double e, double a, double tolerance)
        {
            if (!double.IsFinite(e) || !double.IsFinite(a)) findings.Add(new("ERROR", frame, subject, metric, Detail: "Nonfinite observation."));
            else if (Math.Abs(e - a) > tolerance) findings.Add(new("FAIL", frame, subject, metric, e, a, a - e));
        }
        foreach (var key in new[] { "platform", "architecture", "density", "fontScale", "fontSha256", "symbolSha256", "colorSpace" })
            if (!expected.Environment.TryGetValue(key, out var e) || !actual.Environment.TryGetValue(key, out var a) || e != a)
                findings.Add(new("ERROR", -1, "environment", key, Detail: "Reference and actual environments differ or omit required identity."));
        if (expected.Runner != "compose" || actual.Runner != "avalonia" || expected.ScenarioSha256 != actual.ScenarioSha256
            || expected.BaselineSha256 != actual.BaselineSha256)
            findings.Add(new("ERROR", -1, "manifest", "identity", Detail: "Wrong runner, scenario or baseline."));
        if (expected.Frames.Length != s.FramesUs.Length || actual.Frames.Length != s.FramesUs.Length)
            findings.Add(new("ERROR", -1, "timeline", "frameCount", Detail: "Missing/extra frames."));
        foreach (var capability in s.Required)
            if (!expected.Capabilities.Contains(capability) || !actual.Capabilities.Contains(capability))
                findings.Add(new("UNVERIFIED", -1, "capability", capability, Detail: "Required observation is unsupported by one or both runners."));
        foreach (var error in expected.Errors.Concat(actual.Errors)) findings.Add(new("ERROR", -1, "runner", "failure", Detail: error));
        if (findings.Any(x => x.Status == "ERROR")) return new(s.Id, "ERROR", findings.ToArray());
        Directory.CreateDirectory(Path.Combine(resultDirectory, "diff"));
        for (var i = 0; i < s.FramesUs.Length; i++)
        {
            var e = expected.Frames[i]; var a = actual.Frames[i];
            if (e.Index != i || a.Index != i || e.TimeUs != s.FramesUs[i] || a.TimeUs != s.FramesUs[i])
            {
                findings.Add(new("ERROR", i, "timeline", "timestamp", Detail: "Frame order or timestamps differ.")); continue;
            }
            var requiredImage = s.Checkpoints.Contains(i) ? $"frames/{i:D4}.png" : null;
            if ((s.Required.Contains("raster") || s.Required.Contains("textLayers")) && (e.Image != requiredImage || a.Image != requiredImage))
            {
                findings.Add(new("ERROR", i, "capture", "imagePath", Detail: "Capture paths must match the explicit checkpoint schedule.")); continue;
            }
            if (s.Required.Contains("behavior"))
            {
                Number(i, "component", "clicks", e.Clicks, a.Clicks, 0);
                if (e.Enabled != a.Enabled) findings.Add(new("FAIL", i, "component", "enabled"));
            }
            if (s.Required.Contains("motion") && expected.Capabilities.Contains("motion") && actual.Capabilities.Contains("motion"))
            {
                if (e.Values.Count == 0 || a.Values.Count == 0) findings.Add(new("UNVERIFIED", i, "motion", "samples", Detail: "Missing required animation samples."));
                foreach (var key in new[] { "value", "velocity", "running" })
                    if (!e.Values.ContainsKey(key) || !a.Values.ContainsKey(key)) findings.Add(new("UNVERIFIED", i, "motion", key));
                foreach (var (key, value) in e.Values)
                    if (a.Values.TryGetValue(key, out var v)) Number(i, "motion", key, value, v, profile.MotionAbsolute);
                    else findings.Add(new("UNVERIFIED", i, "motion", key));
            }
            foreach (var node in e.Nodes)
            {
                var observed = a.Nodes.SingleOrDefault(x => x.Id == node.Id);
                if (observed is null) { findings.Add(new("FAIL", i, node.Id, "missingNode")); continue; }
                if (s.Required.Contains("layout"))
                {
                    Number(i, node.Id, "x", node.Bounds.X, observed.Bounds.X, profile.LayoutDip);
                    Number(i, node.Id, "y", node.Bounds.Y, observed.Bounds.Y, profile.LayoutDip);
                    Number(i, node.Id, "width", node.Bounds.Width, observed.Bounds.Width, profile.LayoutDip);
                    Number(i, node.Id, "height", node.Bounds.Height, observed.Bounds.Height, profile.LayoutDip);
                }
                if (s.Required.Contains("text") && node.Id == "label")
                {
                    if (node.Text is null || observed.Text is null) { findings.Add(new("UNVERIFIED", i, node.Id, "textLayout")); continue; }
                    var et = node.Text; var at = observed.Text;
                    if (et.Text != at.Text) findings.Add(new("FAIL", i, node.Id, "textContent"));
                    Number(i, node.Id, "fontSize", et.FontSize, at.FontSize, profile.LayoutDip);
                    Number(i, node.Id, "fontWeight", et.FontWeight, at.FontWeight, 0);
                    Number(i, node.Id, "letterSpacing", et.LetterSpacing, at.LetterSpacing, profile.LayoutDip);
                    Number(i, node.Id, "lineCount", et.Lines.Length, at.Lines.Length, 0);
                    for (var line = 0; line < Math.Min(et.Lines.Length, at.Lines.Length); line++)
                    {
                        Number(i, node.Id, $"line[{line}].baseline", et.Lines[line].Baseline, at.Lines[line].Baseline, profile.LayoutDip);
                        Number(i, node.Id, $"line[{line}].width", et.Lines[line].Width, at.Lines[line].Width, profile.LayoutDip);
                        Number(i, node.Id, $"line[{line}].height", et.Lines[line].Height, at.Lines[line].Height, profile.LayoutDip);
                        if (et.Lines[line].Ellipsized != at.Lines[line].Ellipsized) findings.Add(new("FAIL", i, node.Id, $"line[{line}].ellipsis"));
                    }
                }
                if (s.Required.Contains("resolvedFont") && node.Id == "label")
                {
                    if (node.Text?.ResolvedFonts.Length is not > 0 || observed.Text?.ResolvedFonts.Length is not > 0)
                        findings.Add(new("UNVERIFIED", i, node.Id, "resolvedFont", Detail: "Runtime-resolved font facts are missing."));
                    else if (!node.Text.ResolvedFonts.SequenceEqual(observed.Text.ResolvedFonts))
                        findings.Add(new("FAIL", i, node.Id, "resolvedFont", Detail: "Runtime-resolved font families differ."));
                }
            }
            foreach (var extra in a.Nodes.Where(x => e.Nodes.All(n => n.Id != x.Id))) findings.Add(new("FAIL", i, extra.Id, "extraNode"));
            if (s.Checkpoints.Contains(i) && (s.Required.Contains("raster") || s.Required.Contains("textLayers")))
            {
                if (e.Image is null || a.Image is null) { findings.Add(new("UNVERIFIED", i, "raster", "capture")); continue; }
                using var eb = SKBitmap.Decode(Path.Combine(expectedDirectory, e.Image)) ?? throw new InvalidDataException("Invalid reference image.");
                using var ab = SKBitmap.Decode(Path.Combine(actualDirectory, a.Image)) ?? throw new InvalidDataException("Invalid actual image.");
                if (eb.Width != s.ViewportWidth || eb.Height != s.ViewportHeight || ab.Width != eb.Width || ab.Height != eb.Height)
                { findings.Add(new("ERROR", i, "raster", "dimensions")); continue; }
                using var diff = new SKBitmap(eb.Width, eb.Height);
                var metrics = ImageComparison.Compare(eb, ab, SKColor.Parse(s.Background), profile.ChannelDelta, diff);
                ParityJson.Write(Path.Combine(resultDirectory, "diff", $"{i:D4}.json"), metrics);
                var regions = e.Nodes.Select(node =>
                {
                    var other = a.Nodes.SingleOrDefault(x => x.Id == node.Id)?.Bounds ?? node.Bounds;
                    var left = Math.Min(node.Bounds.X, other.X); var top = Math.Min(node.Bounds.Y, other.Y);
                    var right = Math.Max(node.Bounds.X + node.Bounds.Width, other.X + other.Width);
                    var bottom = Math.Max(node.Bounds.Y + node.Bounds.Height, other.Y + other.Height);
                    var region = new Bounds(left, top, right - left, bottom - top);
                    return new { node.Id, Bounds = region, Metrics = ImageComparison.Compare(eb, ab, SKColor.Parse(s.Background), profile.ChannelDelta, region: region) };
                }).ToArray();
                ParityJson.Write(Path.Combine(resultDirectory, "diff", $"{i:D4}.regions.json"), regions);
                ImageComparison.Save(diff, Path.Combine(resultDirectory, "diff", $"{i:D4}.png"));
                if (s.Required.Contains("raster") && metrics.DifferentPixels > profile.MaximumDifferentPixels)
                    findings.Add(new("FAIL", i, "raster", "differentPixels", profile.MaximumDifferentPixels, metrics.DifferentPixels,
                        Detail: $"Maximum channel delta {metrics.MaximumChannelDelta}; contour distance {metrics.ContourDistance:F3}px (diagnostic)."));
                if (s.Required.Contains("textLayers"))
                {
                    var path = $"without-text/{i:D4}.png";
                    if (e.WithoutTextImage is null || a.WithoutTextImage is null)
                        findings.Add(new("UNVERIFIED", i, "textLayers", "capture", Detail: "Paint-suppressed captures are missing."));
                    else if (e.WithoutTextImage != path || a.WithoutTextImage != path)
                        findings.Add(new("ERROR", i, "textLayers", "imagePath"));
                    else
                    {
                        using var ew = SKBitmap.Decode(Path.Combine(expectedDirectory, path)) ?? throw new InvalidDataException("Invalid reference text diagnostic.");
                        using var aw = SKBitmap.Decode(Path.Combine(actualDirectory, path)) ?? throw new InvalidDataException("Invalid actual text diagnostic.");
                        using var backgroundDiff = new SKBitmap(eb.Width, eb.Height);
                        var background = ImageComparison.Compare(ew, aw, SKColor.Parse(s.Background), profile.ChannelDelta, backgroundDiff);
                        using var ep = ImageComparison.TextPaint(eb, ew); using var ap = ImageComparison.TextPaint(ab, aw);
                        using var paintDiff = new SKBitmap(eb.Width, eb.Height);
                        var paint = ImageComparison.Compare(ep, ap, SKColors.Black, profile.ChannelDelta, paintDiff);
                        ParityJson.Write(Path.Combine(resultDirectory, "diff", $"{i:D4}.text-layers.json"), new { withoutText = background, textPaint = paint });
                        ImageComparison.Save(backgroundDiff, Path.Combine(resultDirectory, "diff-without-text", $"{i:D4}.png"));
                        ImageComparison.Save(ep, Path.Combine(resultDirectory, "text-paint-expected", $"{i:D4}.png"));
                        ImageComparison.Save(ap, Path.Combine(resultDirectory, "text-paint-actual", $"{i:D4}.png"));
                        ImageComparison.Save(paintDiff, Path.Combine(resultDirectory, "diff-text-paint", $"{i:D4}.png"));
                        if (background.DifferentPixels > profile.MaximumDifferentPixels)
                            findings.Add(new("FAIL", i, "withoutText", "differentPixels", profile.MaximumDifferentPixels, background.DifferentPixels));
                        // Text paint residuals remain governed by the full raster gate; no generous
                        // glyph exception is created from this uncalibrated diagnostic.
                    }
                }
            }
        }
        if (s.Required.Contains("text") && !expected.Frames.Any(x => x.Nodes.Any(n => n.Id == "label" && n.Text is not null)))
            findings.Add(new("UNVERIFIED", -1, "label", "textLayout", Detail: "No text observations were collected."));
        if (s.Required.Contains("resolvedFont") && !expected.Frames.Any(x => x.Nodes.Any(n => n.Id == "label" && n.Text?.ResolvedFonts.Length is > 0)))
            findings.Add(new("UNVERIFIED", -1, "label", "resolvedFont", Detail: "No runtime-resolved font observations were collected."));
        var status = new[] { "ERROR", "FAIL", "UNVERIFIED" }.FirstOrDefault(x => findings.Any(f => f.Status == x)) ?? "PASS";
        return new(s.Id, status, findings.ToArray());
    }
}
