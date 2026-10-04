using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record SnackbarHostConfig(string Action, bool Rtl = false, bool Overlap = false);

[ParitySuite("snackbar-host")]
public sealed class SnackbarHostCases
{
    public static IEnumerable<CaseData<SnackbarHostConfig>> Configurations()
    {
        // Host motion/queue ownership is independent of surface colors and Snackbar layout.
        foreach (var action in new[] { "dismiss", "interrupt", "action-queue", "cancel-current", "cancel-queued", "state", "template", "detach" })
            yield return new($"dynamic.{action}", action, new(action));
        yield return new("dynamic.interrupt.rtl", "interrupt RTL", new("interrupt", true));
        yield return new("dynamic.dismiss.overlap", "overlapping content opacity", new("dismiss", Overlap: true));
    }

    [ParityScenario("dynamic", Data = nameof(Configurations))]
    public async Task Dynamic(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<SnackbarHostConfig>();
        await using var host = new AvaloniaHost(writer, 192, 128);
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        var tasks = new List<Task<SnackbarResult>>();
        var snackbarHost = new SnackbarHost { FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        snackbarHost.SnackbarTemplate = Template(120);
        var root = new Canvas { Background = Brushes.White, FlowDirection = FlowDirection.LeftToRight, Children = { snackbarHost } };
        Canvas.SetLeft(snackbarHost, 16); Canvas.SetTop(snackbarHost, 16);
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        long nextFrame = 16000;
        void Advance(long time)
        {
            while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; }
            host.AdvanceTo(time);
        }
        void Sample(string id)
        {
            var layers = snackbarHost.GetVisualDescendants().OfType<ContentPresenter>()
                .Where(p => p.GetType().Name == "SnackbarPresenter").ToArray();
            var points = new List<ProbePoint>
            {
                new("host", "snackbar-host", null,
                [Measurement.Text("current", snackbarHost.State.CurrentSnackbarData?.Visuals.Message ?? "none", definition: "snackbar-host.current-message"),
                 Measurement.Numeric("layers", layers.Length, "count", definition: "snackbar-host.retained-layer-count"),
                 Measurement.Numeric("completed", tasks.Count(t => t.IsCompletedSuccessfully), "count", definition: "snackbar-host.completed-request-count"),
                 Measurement.Numeric("cancelled", tasks.Count(t => t.IsCanceled), "count", definition: "snackbar-host.cancelled-request-count")])
            };
            foreach (var layer in layers)
                points.Add(new(((ISnackbarData)layer.Content!).Visuals.Message, "snackbar-item", "host",
                    [.. AvaloniaMeasurements.Geometry(layer, host.Window).Where(m => m.Name == "size"),
                    Measurement.Numeric("alpha", layer.Opacity, "ratio", category: "motion"),
                    Measurement.Numeric("scale", layer.RenderTransform!.Value.M11, "ratio", category: "motion")]));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Show(string message, CancellationToken token) => tasks.Add(snackbarHost.State.ShowSnackbarAsync(message,
            "action", duration: SnackbarDuration.Indefinite, cancellationToken: token));
        Sample("initial");
        try
        {
            Change(1000, "show-first", () => Show("first", firstCancellation.Token));
            if (config.Action == "interrupt")
            {
                Times(16000, 32000, 64000);
                Change(65000, "dismiss-early", () => snackbarHost.State.CurrentSnackbarData!.Dismiss());
                Times(80000, 96000);
                Change(97000, "show-second", () => Show("second", secondCancellation.Token));
                Times(112000, 160000, 224000, 400000, 640000);
            }
            else
            {
                if (config.Action is "action-queue" or "cancel-current" or "cancel-queued")
                    Change(2000, "queue-second", () => Show("second", secondCancellation.Token));
                if (config.Action == "cancel-queued") Change(3000, "cancel-queued", secondCancellation.Cancel);
                Times(16000, 32000, 64000, 112000, 224000, 400000);
                switch (config.Action)
                {
                    case "action-queue":
                        Change(401000, "down", () => { host.Input("move", new(76, 40)); host.Input("down", new(76, 40)); });
                        Change(402000, "action", () => host.Input("up", new(76, 40))); break;
                    case "cancel-current": Change(401000, "cancel-current", firstCancellation.Cancel); break;
                    case "state":
                        Change(401000, "replace-state", () => { snackbarHost.State = new(); Show("second", secondCancellation.Token); }); break;
                    case "template": Change(401000, "replace-template", () => snackbarHost.SnackbarTemplate = Template(80)); break;
                    case "detach":
                        Change(401000, "detach", () => root.Children.Remove(snackbarHost));
                        Change(402000, "reattach", () => root.Children.Add(snackbarHost)); break;
                    default: Change(401000, "dismiss", () => snackbarHost.State.CurrentSnackbarData!.Dismiss()); break;
                }
                Times(416000, 448000, 512000, 640000, 800000);
            }
        }
        finally
        {
            firstCancellation.Cancel(); secondCancellation.Cancel(); host.Draw();
            foreach (var task in tasks)
                try { await task; } catch (OperationCanceledException) when (task.IsCanceled) { }
        }

        IDataTemplate Template(double width) => new FuncDataTemplate<ISnackbarData>((data, _) =>
        {
            var button = new Button { Width = width, Height = 48, FocusAdorner = null,
                Theme = new ControlTheme(typeof(Button)),
                Template = new FuncControlTemplate<Button>((_, _) =>
                {
                    var content = new Grid { Background = data!.Visuals.Message == "second" ? Brushes.Blue : Brushes.Red };
                    if (config.Overlap) content.Children.Add(new Border { Width = 40, Height = 24, Background = Brushes.Blue });
                    return content;
                }) };
            button.Click += (_, _) => data!.PerformAction();
            return button;
        });
    }
}
