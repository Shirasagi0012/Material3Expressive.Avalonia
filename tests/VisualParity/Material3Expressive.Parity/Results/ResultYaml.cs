using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Material3Expressive.Parity;

// YAML is the public result format. JSON nodes only bridge existing strongly typed capture records.
// Quote strings, keep numbers round-trip precise, and use flow sequences for short scalar vectors.
public static class ResultYaml
{
    public static JsonNode? Node<T>(T value) => JsonSerializer.SerializeToNode(value, ParityJson.Options);

    public static void Write(string path, JsonNode node)
    {
        YamlNode Convert(JsonNode? value) => value switch
        {
            JsonObject map => new YamlMappingNode(map.Select(p => new KeyValuePair<YamlNode, YamlNode>(
                String(p.Key), Convert(p.Value)))),
            JsonArray array => new YamlSequenceNode(array.Select(Convert))
            {
                Style = array.All(x => x is not (JsonArray or JsonObject)) ? SequenceStyle.Flow : SequenceStyle.Block
            },
            JsonValue scalar when scalar.GetValueKind() == JsonValueKind.String =>
                String(scalar.GetValue<string>()),
            _ => new YamlScalarNode(Number(value)) { Style = ScalarStyle.Plain }
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var writer = File.CreateText(path);
        new YamlStream(new YamlDocument(Convert(node))).Save(writer, assignAnchors: false);

        static YamlScalarNode String(string value) => new(value)
        {
            Style = System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-zA-Z_][a-zA-Z0-9._/-]*$")
                    && value.ToLowerInvariant() is not ("true" or "false" or "null" or "yes" or "no" or "on" or "off" or "y" or "n")
                ? ScalarStyle.Plain : ScalarStyle.DoubleQuoted
        };

        // YAML 1.1 readers such as PyYAML require a decimal point in exponential floats.
        static string Number(JsonNode? value)
        {
            var text = value?.ToJsonString() ?? "null";
            var exponent = text.IndexOf('E');
            return exponent > 0 && !text[..exponent].Contains('.') ? text.Insert(exponent, ".0") : text;
        }
    }

    public static JsonObject Read(string path)
    {
        var yaml = new YamlStream();
        using var reader = File.OpenText(path);
        yaml.Load(reader);
        if (yaml.Documents.Count != 1) throw new InvalidDataException("Expected one YAML document.");
        JsonNode? Convert(YamlNode node) => node switch
        {
            YamlMappingNode map => new JsonObject(map.Children.Select(p => new KeyValuePair<string, JsonNode?>(
                ((YamlScalarNode)p.Key).Value ?? throw new InvalidDataException("Empty key."), Convert(p.Value)))),
            YamlSequenceNode sequence => new JsonArray(sequence.Children.Select(Convert).ToArray()),
            YamlScalarNode scalar when scalar.Style is ScalarStyle.Plain or ScalarStyle.Any => Plain(scalar.Value),
            YamlScalarNode scalar => JsonValue.Create(scalar.Value),
            _ => throw new InvalidDataException("Unsupported YAML node.")
        };
        return Convert(yaml.Documents[0].RootNode) as JsonObject ?? throw new InvalidDataException("Expected YAML mapping.");

        static JsonNode? Plain(string? value)
        {
            if (value is null or "null" or "~") return null;
            // Most plain scalars are identifiers, not JSON. Avoid an exception per field.
            if (value is "true" or "false") return JsonValue.Create(value == "true");
            if (value.Length == 0 || value[0] is not ('-' or >= '0' and <= '9')) return JsonValue.Create(value);
            try { return JsonNode.Parse(value); }
            catch (JsonException) { return JsonValue.Create(value); }
        }
    }
}
