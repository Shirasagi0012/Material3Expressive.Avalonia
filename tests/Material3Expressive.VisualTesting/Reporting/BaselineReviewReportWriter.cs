using System.Net;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting.Reporting;

/// <summary>Produces the human review surface; it never grants approval itself.</summary>
public sealed class BaselineReviewReportWriter(RepositoryPaths paths)
{
    public string Write(
        IReadOnlyList<VisualCase> cases,
        IReadOnlyDictionary<string, BaselineApprovalState> approvals)
    {
        var reportRoot = Path.Combine(paths.Root, "artifacts", "visual-review");
        Directory.CreateDirectory(reportRoot);
        var baselineStore = new BaselineStore(paths);

        string Card(VisualCase visualCase)
        {
            var baseline = baselineStore.GetBaselinePath(visualCase.Id);
            var state = approvals[visualCase.Id];
            var stateClass = state == BaselineApprovalState.Approved ? "approved" : "pending";
            var image = File.Exists(baseline)
                ? $"<img src=\"{WebUtility.HtmlEncode(
                    Path.GetRelativePath(reportRoot, baseline).Replace('\\', '/'))}\" " +
                  $"alt=\"{WebUtility.HtmlEncode(visualCase.Id)}\">"
                : "<div class=\"missing\">No candidate image has been recorded.</div>";
            return $$"""
                     <article>
                       <header>
                         <code>{{WebUtility.HtmlEncode(visualCase.Id)}}</code>
                         <span class="{{stateClass}}">{{WebUtility.HtmlEncode(state.ToString())}}</span>
                       </header>
                       {{image}}
                       <footer>{{visualCase.Viewport.Width}}×{{visualCase.Viewport.Height}} ·
                         {{WebUtility.HtmlEncode(visualCase.Theme.IsDark ? "dark" : "light")}} ·
                         {{visualCase.RenderScaling:0.##}}× ·
                         {{WebUtility.HtmlEncode(visualCase.Culture)}} ·
                         {{WebUtility.HtmlEncode(visualCase.FlowDirection.ToString())}} ·
                         {{WebUtility.HtmlEncode(visualCase.Theme.SchemeVariant)}} /
                         {{WebUtility.HtmlEncode(visualCase.Theme.SpecVersion.ToString())}} /
                         {{WebUtility.HtmlEncode(visualCase.Theme.Platform.ToString())}} ·
                         seed #{{visualCase.Theme.SeedColor.R:X2}}{{visualCase.Theme.SeedColor.G:X2}}{{visualCase.Theme.SeedColor.B:X2}} ·
                         {{WebUtility.HtmlEncode(visualCase.Comparison.Name)}}</footer>
                     </article>
                     """;
        }

        var staticCards = cases
            .Where(visualCase => !visualCase.HasTag("motion"))
            .Select(Card);
        var motionSections = cases
            .Where(visualCase => visualCase.HasTag("motion"))
            .GroupBy(MotionTimelineId)
            .Select(group => $$"""
                               <section class="motion-sequence">
                                 <h3><code>{{WebUtility.HtmlEncode(group.Key)}}</code></h3>
                                 <div class="timeline">{{string.Join(Environment.NewLine, group.Select(Card))}}</div>
                               </section>
                               """);
        var motion = string.Join(Environment.NewLine, motionSections);
        var motionBlock = string.IsNullOrEmpty(motion)
            ? string.Empty
            : $$"""
                <h2>Motion timelines</h2>
                <p>Each row is one input transition with time advanced by the deterministic manual clock.</p>
                {{motion}}
                """;

        var environment = File.Exists(baselineStore.EnvironmentPath)
            ? WebUtility.HtmlEncode(File.ReadAllText(baselineStore.EnvironmentPath))
            : "No environment has been recorded.";
        var output = Path.Combine(reportRoot, "index.html");
        File.WriteAllText(
            output,
            $$"""
              <!doctype html>
              <html lang="en">
              <head>
                <meta charset="utf-8">
                <title>Material3Expressive baseline candidates</title>
                <style>
                  body { margin: 24px; font: 14px Roboto, sans-serif; background: #f7f2fa; color: #1d1b20; }
                  main { max-width: 1600px; margin: auto; }
                  .notice { padding: 16px; border-radius: 12px; background: #fff8e1; border: 1px solid #f2c037; }
                  .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(420px, 1fr)); gap: 20px; margin-top: 24px; }
                  .motion-sequence { margin-top: 24px; }
                  .timeline { display: flex; gap: 16px; overflow-x: auto; padding: 4px 0 16px; }
                  .timeline article { flex: 0 0 360px; }
                  article { background: white; border: 1px solid #cac4d0; border-radius: 14px; overflow: hidden; }
                  header, footer { display: flex; justify-content: space-between; gap: 12px; padding: 12px 14px; }
                  footer { color: #49454f; }
                  img { display: block; width: 100%; height: auto; border-block: 1px solid #e7e0ec; }
                  .approved, .pending { padding: 3px 8px; border-radius: 999px; font-size: 12px; }
                  .approved { background: #c4eed0; color: #07391c; }
                  .pending { background: #ffdf9a; color: #442b00; }
                  .missing { padding: 48px; text-align: center; color: #b3261e; }
                  details { margin-top: 24px; }
                  pre { overflow: auto; padding: 16px; background: #211f26; color: #f5eff7; border-radius: 10px; }
                </style>
              </head>
              <body><main>
                <h1>Baseline candidates</h1>
                <p class="notice">
                  These images are candidates only. Strict regression enforcement starts for an
                  image only after the user reviews it and runs the explicit approval command.
                </p>
                {{motionBlock}}
                <h2>Static and settled-state candidates</h2>
                <div class="grid">{{string.Join(Environment.NewLine, staticCards)}}</div>
                <details><summary>Recorded environment</summary><pre>{{environment}}</pre></details>
              </main></body>
              </html>
              """);
        return output;
    }

    private static string MotionTimelineId(VisualCase visualCase)
    {
        var segments = visualCase.Id.Split('/');
        return segments.Length > 2
            ? string.Join('/', segments.Take(segments.Length - 2))
            : visualCase.Id;
    }
}
