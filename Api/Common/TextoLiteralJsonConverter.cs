using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Common;

/// <summary>
/// Todo lo que llegue en un campo de texto se trata como texto, aunque venga como objeto, arreglo, número o booleano.
/// Ejemplo: {"texto": {"$ne": null}} se recibe como el texto literal {"$ne":null} (no como un operador, no como error de tipo).
/// Los campos numéricos o de fecha siguen siendo estrictos.
/// </summary>
public sealed class TextoLiteralJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return reader.GetString();
        using var doc = JsonDocument.ParseValue(ref reader);   // consume el valor completo (objeto, arreglo, número...)
        return doc.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
