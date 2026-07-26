using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Material3Expressive.TokenGenerator.Json;

/// <summary>
/// A parsed JSON value.
/// </summary>
/// <remarks>
/// Source generators run inside the compiler's load context, where taking a dependency means
/// shipping that assembly and everything it drags in alongside the analyzer. The token tables are
/// small, hand-written and read exactly once per build, so a few hundred lines of parser is a far
/// cheaper answer than <c>System.Text.Json</c>.
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

    public IReadOnlyList<JsonValue> Items => (IReadOnlyList<JsonValue>?)_items ?? System.Array.Empty<JsonValue>();

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
/// A recursive-descent JSON reader. Accepts <c>//</c> and <c>/* */</c> comments and trailing
/// commas, because the token tables are hand-maintained and want annotating.
/// </summary>
internal static class JsonParser
{
    public static JsonValue Parse(string text)
    {
        var reader = new Reader(text);
        reader.SkipTrivia();
        var value = reader.ReadValue();
        reader.SkipTrivia();
        if (!reader.AtEnd)
            throw reader.Error("Unexpected trailing content.");

        return value;
    }

    private struct Reader
    {
        private readonly string _text;
        private int _index;
        private int _line;
        private int _lineStart;

        public Reader(string text)
        {
            _text = text;
            _index = 0;
            _line = 1;
            _lineStart = 0;
        }

        public bool AtEnd => _index >= _text.Length;

        private char Current => _text[_index];

        public JsonException Error(string message)
        {
            return new JsonException(message, _line, _index - _lineStart + 1);
        }

        public void SkipTrivia()
        {
            while (_index < _text.Length)
            {
                var c = _text[_index];

                if (c == '\n')
                {
                    _index++;
                    _line++;
                    _lineStart = _index;
                }
                else if (c == ' ' || c == '\t' || c == '\r')
                {
                    _index++;
                }
                else if (c == '/' && _index + 1 < _text.Length && _text[_index + 1] == '/')
                {
                    while (_index < _text.Length && _text[_index] != '\n')
                        _index++;
                }
                else if (c == '/' && _index + 1 < _text.Length && _text[_index + 1] == '*')
                {
                    _index += 2;
                    while (_index + 1 < _text.Length && !(_text[_index] == '*' && _text[_index + 1] == '/'))
                    {
                        if (_text[_index] == '\n')
                        {
                            _line++;
                            _lineStart = _index + 1;
                        }

                        _index++;
                    }

                    _index = Math.Min(_index + 2, _text.Length);
                }
                else
                {
                    return;
                }
            }
        }

        public JsonValue ReadValue()
        {
            if (AtEnd)
                throw Error("Unexpected end of input.");

            switch (Current)
            {
                case '{': return ReadObject();
                case '[': return ReadArray();
                case '"': return JsonValue.String(ReadString());
                case 't': return ReadLiteral("true", JsonValue.Boolean(true));
                case 'f': return ReadLiteral("false", JsonValue.Boolean(false));
                case 'n': return ReadLiteral("null", JsonValue.Null);
                default: return ReadNumber();
            }
        }

        private JsonValue ReadObject()
        {
            _index++; // '{'
            var members = new Dictionary<string, JsonValue>(StringComparer.Ordinal);

            SkipTrivia();
            if (!AtEnd && Current == '}')
            {
                _index++;
                return JsonValue.Object(members);
            }

            while (true)
            {
                SkipTrivia();
                if (AtEnd)
                    throw Error("Unterminated object.");

                // Tolerate the trailing comma before the closing brace.
                if (Current == '}')
                {
                    _index++;
                    return JsonValue.Object(members);
                }

                if (Current != '"')
                    throw Error("Expected a member name.");

                var name = ReadString();
                SkipTrivia();
                if (AtEnd || Current != ':')
                    throw Error($"Expected ':' after member '{name}'.");

                _index++;
                SkipTrivia();
                members[name] = ReadValue();

                SkipTrivia();
                if (AtEnd)
                    throw Error("Unterminated object.");

                if (Current == ',')
                {
                    _index++;
                    continue;
                }

                if (Current == '}')
                {
                    _index++;
                    return JsonValue.Object(members);
                }

                throw Error("Expected ',' or '}'.");
            }
        }

        private JsonValue ReadArray()
        {
            _index++; // '['
            var items = new List<JsonValue>();

            while (true)
            {
                SkipTrivia();
                if (AtEnd)
                    throw Error("Unterminated array.");

                if (Current == ']')
                {
                    _index++;
                    return JsonValue.Array(items);
                }

                items.Add(ReadValue());

                SkipTrivia();
                if (AtEnd)
                    throw Error("Unterminated array.");

                if (Current == ',')
                {
                    _index++;
                    continue;
                }

                if (Current == ']')
                {
                    _index++;
                    return JsonValue.Array(items);
                }

                throw Error("Expected ',' or ']'.");
            }
        }

        private string ReadString()
        {
            _index++; // opening quote
            var builder = new StringBuilder();

            while (true)
            {
                if (AtEnd)
                    throw Error("Unterminated string.");

                var c = _text[_index++];

                if (c == '"')
                    return builder.ToString();

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                if (AtEnd)
                    throw Error("Unterminated escape sequence.");

                var escape = _text[_index++];
                switch (escape)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (_index + 4 > _text.Length)
                            throw Error("Truncated \\u escape.");

                        var hex = _text.Substring(_index, 4);
                        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                            throw Error($"Invalid \\u escape '\\u{hex}'.");

                        builder.Append((char)code);
                        _index += 4;
                        break;
                    default:
                        throw Error($"Unrecognised escape '\\{escape}'.");
                }
            }
        }

        private JsonValue ReadLiteral(string literal, JsonValue value)
        {
            if (_index + literal.Length > _text.Length ||
                string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0)
                throw Error($"Expected '{literal}'.");

            _index += literal.Length;
            return value;
        }

        private JsonValue ReadNumber()
        {
            var start = _index;

            if (!AtEnd && (Current == '-' || Current == '+'))
                _index++;

            while (!AtEnd && (char.IsDigit(Current) || Current == '.' || Current == 'e' || Current == 'E' ||
                              ((Current == '-' || Current == '+') && (_text[_index - 1] == 'e' || _text[_index - 1] == 'E'))))
                _index++;

            var text = _text.Substring(start, _index - start);
            if (text.Length == 0 ||
                !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                throw Error($"'{text}' is not a number.");

            return JsonValue.Number(number);
        }
    }
}
