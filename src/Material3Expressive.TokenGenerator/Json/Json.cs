using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Material3Expressive.TokenGenerator.Json;

/// <summary>
/// A parsed JSON value.
/// </summary>
/// <remarks>
/// <see cref="System.Text.Json.JsonDocument"/> owns the syntax and decoding rules. This small
/// immutable-shaped adapter keeps the model/emitter independent of a DOM whose lifetime is tied to
/// a document, and gives token tables one place to enforce duplicate-property semantics.
/// </remarks>
internal sealed class JsonValue
{
    private readonly Dictionary<string, JsonValue>? _members;
    private readonly List<JsonValue>? _items;
    private readonly string? _text;
    private readonly double _number;
    private readonly bool _boolean;

    private JsonValue(JsonKind kind, Dictionary<string, JsonValue>? members = null, List<JsonValue>? items = null,
        string? text = null, double number = 0, bool boolean = false)
    {
        Kind = kind;
        _members = members;
        _items = items;
        _text = text;
        _number = number;
        _boolean = boolean;
    }

    public JsonKind Kind { get; }

    public bool IsNull => Kind == JsonKind.Null;

    public static JsonValue Object(Dictionary<string, JsonValue> members)
    {
        return new JsonValue(JsonKind.Object, members);
    }

    public static JsonValue Array(List<JsonValue> items)
    {
        return new JsonValue(JsonKind.Array, items: items);
    }

    public static JsonValue String(string text)
    {
        return new JsonValue(JsonKind.String, text: text);
    }

    public static JsonValue Number(double value)
    {
        return new JsonValue(JsonKind.Number, number: value);
    }

    public static JsonValue Boolean(bool value)
    {
        return new JsonValue(JsonKind.Boolean, boolean: value);
    }

    public static JsonValue Null { get; } = new(JsonKind.Null);

    /// <summary>Members in source order — the emitters keep the table's ordering.</summary>
    public IEnumerable<KeyValuePair<string, JsonValue>> Members =>
        _members ?? (IEnumerable<KeyValuePair<string, JsonValue>>)System.Array.Empty<KeyValuePair<string, JsonValue>>();

    public IReadOnlyList<JsonValue> Items =>
        (IReadOnlyList<JsonValue>?)_items ?? System.Array.Empty<JsonValue>();

    public JsonValue this[string name] =>
        _members is { } members && members.TryGetValue(name, out var value) ? value : Null;

    public string? AsString()
    {
        return Kind switch
        {
            JsonKind.String => _text,
            JsonKind.Number => _number.ToString("R", CultureInfo.InvariantCulture),
            JsonKind.Boolean => _boolean ? "true" : "false",
            _ => null
        };
    }

    public double? AsNumber()
    {
        return Kind == JsonKind.Number ? _number : null;
    }

    public bool? AsBoolean()
    {
        return Kind == JsonKind.Boolean ? _boolean : null;
    }
}

internal enum JsonKind
{
    Null,
    Boolean,
    Number,
    String,
    Array,
    Object
}

/// <summary>Thrown for malformed JSON; the generator turns it into a diagnostic.</summary>
internal sealed class JsonException : Exception
{
    public JsonException(string message) : base(message)
    {
    }

    public JsonException(string message, int line, int column)
        : base($"({line},{column}): {message}")
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }

    public int Column { get; }
}

/// <summary>
/// Reads the relaxed JSON dialect used by hand-maintained token tables.
/// </summary>
/// <remarks>
/// Comments and trailing commas are deliberately enabled. Duplicate properties remain an error:
/// silently accepting one would let a token take the wrong value. The resulting <see cref="JsonValue"/>
/// tree is detached from the disposed <see cref="JsonDocument"/> before parsing returns.
/// </remarks>
internal static class JsonParser
{
    private static readonly JsonDocumentOptions Options = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        MaxDepth = 64
    };

    public static JsonValue Parse(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text, Options);
            return Convert(document.RootElement);
        }
        catch (System.Text.Json.JsonException exception)
        {
            var line = checked((int)(exception.LineNumber ?? 0) + 1);
            var column = checked((int)(exception.BytePositionInLine ?? 0) + 1);
            throw new JsonException(exception.Message, line, column);
        }
    }

    private static JsonValue Convert(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return JsonValue.Null;

            case JsonValueKind.True:
                return JsonValue.Boolean(true);

            case JsonValueKind.False:
                return JsonValue.Boolean(false);

            case JsonValueKind.Number:
                try
                {
                    return JsonValue.Number(element.GetDouble());
                }
                catch (FormatException exception)
                {
                    throw new JsonException($"The number could not be represented as a double: {exception.Message}");
                }

            case JsonValueKind.String:
                return JsonValue.String(element.GetString() ?? string.Empty);

            case JsonValueKind.Array:
            {
                var items = new List<JsonValue>();
                foreach (var item in element.EnumerateArray())
                    items.Add(Convert(item));

                return JsonValue.Array(items);
            }

            case JsonValueKind.Object:
            {
                var members = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (members.ContainsKey(property.Name))
                        throw new JsonException($"'{property.Name}' is declared twice in the same object.");

                    members.Add(property.Name, Convert(property.Value));
                }

                return JsonValue.Object(members);
            }

            default:
                throw new JsonException($"Unsupported JSON value kind '{element.ValueKind}'.");
        }
    }
}
