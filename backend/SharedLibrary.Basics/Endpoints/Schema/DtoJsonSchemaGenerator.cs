using System.Reflection;
using System.Text.Json.Nodes;
using QuVian.SharedLibrary.Basics.Endpoints.Hateoas;

namespace QuVian.SharedLibrary.Basics.Endpoints.Schema;

/// <summary>
/// Generates JSON Schema objects from .NET DTO types using reflection and nullability metadata.
/// </summary>
public static class DtoJsonSchemaGenerator
{
    private static readonly NullabilityInfoContext Nullability = new();

    public static JsonNode Generate(Type type) =>
        BuildObjectSchema(type, []);

    public static JsonNode GenerateListOf(Type itemType) =>
        new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["total"] = new JsonObject { ["type"] = "integer" },
                ["items"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = BuildObjectSchema(itemType, [])
                }
            }
        };

    private static JsonNode BuildObjectSchema(Type type, HashSet<Type> visited)
    {
        if (!visited.Add(type))
            return new JsonObject { ["type"] = "object" };

        var required = new JsonArray();
        var properties = new JsonObject();

        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !IsInfrastructureProperty(p));

        foreach (var prop in props)
        {
            var key = ToCamelCase(prop.Name);
            var nullInfo = Nullability.Create(prop);
            var isNullable = nullInfo.ReadState == NullabilityState.Nullable;

            if (!isNullable)
                required.Add(JsonValue.Create(key)!);

            properties[key] = isNullable
                ? WrapNullable(BuildTypeSchema(Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType, visited))
                : BuildTypeSchema(prop.PropertyType, visited);
        }

        visited.Remove(type);

        var schema = new JsonObject { ["type"] = "object" };
        if (required.Count > 0) schema["required"] = required;
        schema["properties"] = properties;
        return schema;
    }

    private static JsonNode WrapNullable(JsonNode inner) =>
        new JsonObject
        {
            ["oneOf"] = new JsonArray { inner, new JsonObject { ["type"] = "null" } }
        };

    private static JsonNode BuildTypeSchema(Type type, HashSet<Type> visited)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(string))                                              return new JsonObject { ["type"] = "string" };
        if (t == typeof(int) || t == typeof(long) || t == typeof(short))     return new JsonObject { ["type"] = "integer" };
        if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return new JsonObject { ["type"] = "number" };
        if (t == typeof(bool))                                                return new JsonObject { ["type"] = "boolean" };
        if (t == typeof(Guid))                                                return new JsonObject { ["type"] = "string", ["format"] = "uuid" };
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset))            return new JsonObject { ["type"] = "string", ["format"] = "date-time" };

        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(IList<>) || def == typeof(List<>) ||
                def == typeof(ICollection<>) || def == typeof(IEnumerable<>) ||
                def == typeof(IReadOnlyList<>))
            {
                return new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = BuildTypeSchema(t.GetGenericArguments()[0], visited)
                };
            }
        }

        if (t.IsClass)
            return BuildObjectSchema(t, visited);

        return new JsonObject { ["type"] = "object" };
    }

    private static bool IsInfrastructureProperty(PropertyInfo p) =>
        p.DeclaringType == typeof(HateoasBaseResponse);

    private static string ToCamelCase(string name) =>
        char.ToLowerInvariant(name[0]) + name[1..];
}
