using System.Text.Json;
using System.Text.Json.Serialization;

namespace Material3Expressive.Parity;

public sealed record CaseMetadata(
    int SchemaVersion,
    string Id,
    string Title,
    string Suite,
    string Scenario,
    JsonElement Config
)
{
    public static CaseMetadata Create<T>(string id, string title, string suite, string scenario, T config)
        where T : notnull => new(4, id, title, suite, scenario,
            JsonSerializer.SerializeToElement(config, ParityJson.Options));

    public T ReadConfig<T>() where T : notnull
    {
        Validate();
        return Config.Deserialize<T>(ConfigOptions) ?? throw new InvalidDataException("Empty case config.");
    }

    static readonly JsonSerializerOptions ConfigOptions = new(ParityJson.Options)
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        PropertyNameCaseInsensitive = false,
        NumberHandling = JsonNumberHandling.Strict
    };

    public static bool ValidId(string? id) => id is not null && System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9][a-z0-9.-]*$");

    public void Validate()
    {
        if (SchemaVersion != 4 || !ValidId(Id) || !ValidId(Suite) || !ValidId(Scenario)
                               || !Id.StartsWith(Suite + ".", StringComparison.Ordinal)
                               || Config.ValueKind != JsonValueKind.Object || string.IsNullOrWhiteSpace(Title))
            throw new InvalidDataException("Invalid case metadata (schema 4 with suite/scenario and object config required).");
    }
}

