using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuVian.SharedLibrary.Basics.Extensions.Swagger;

/// <summary>
/// Adds enum value descriptions to the Swagger schema documentation.
/// </summary>
public class SwaggerExamplesFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
            return;

        var values = Enum.GetValues(context.Type);
        var description = new List<string>();

        foreach (var value in values)
        {
            var intValue = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
            description.Add($"{intValue} = {value}");
        }

        schema.Description = string.Join(", ", description);
    }


}