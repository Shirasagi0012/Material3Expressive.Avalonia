using Avalonia.Controls;
using Avalonia.Media;
using MaterialTypography.Roboto;

namespace Material3Expressive.Parity;

public sealed record TextConfig(string Text, string Stage);

[ParitySuite("text")]
public sealed class TextCases
{
    public static IEnumerable<CaseData<TextConfig>> Configurations() =>
    [
        new("wrapped", "Multiline wrapped text", new("Compose parity\nWrap a longer line with spaces.", "wrapped")),
        new("paragraph-breaks", "CRLF, empty lines, UTF-16 and paragraph separators", new("A \r\n\r\nB 😀  \nC\u2028D\u2029", "paragraph-breaks"))
    ];


    [ParityScenario("layout", Data = nameof(Configurations))]
    public async Task TextLayout(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<TextConfig>();
        await using var host = new AvaloniaHost(writer);
        var text = new TextBlock
        {
            Text = config.Text,
            Width = 120,
            FontFamily = new(RobotoFontCollection.FamilyUri),
            FontSize = 14,
            FontWeight = FontWeight.Normal,
            LineHeight = 20,
            LetterSpacing = 0,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#6750A4")
        };
        host.Mount(CaseDefaults.Center(text));
        host.Frame(-2000);
        host.Frame(-1000);
        host.Frame(0);
        writer.Submit(host.Snapshot("initial", config.Stage, [AvaloniaMeasurements.Text("label", null, text, host.Window)]));
    }
}
