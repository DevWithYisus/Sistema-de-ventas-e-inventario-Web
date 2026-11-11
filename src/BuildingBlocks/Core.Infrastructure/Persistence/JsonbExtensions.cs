using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Core.Infrastructure.Persistence;

public static class JsonbExtensions
{
    /// <summary>Guarda un Dictionary&lt;string,string&gt; en una columna jsonb de PostgreSQL.</summary>
    public static PropertyBuilder<Dictionary<string, string>> HasJsonbDictionary(
        this PropertyBuilder<Dictionary<string, string>> builder)
    {
        var converter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                 ?? new Dictionary<string, string>());

        var comparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null)
                      == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
            v => new Dictionary<string, string>(v));

        return builder
            .HasConversion(converter, comparer)
            .HasColumnType("jsonb");
    }
}
