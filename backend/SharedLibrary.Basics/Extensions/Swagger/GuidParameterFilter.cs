using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuVian.SharedLibrary.Basics.Extensions.Swagger;

/// <summary>
/// Ensures GUID parameters are displayed as UUIDs in Swagger UI.
/// </summary>
public class GuidParameterFilter : IParameterFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter.Schema?.Type == JsonSchemaType.String &&
            context.ApiParameterDescription.Type == typeof(Guid) &&
            parameter.Schema is OpenApiSchema mutableSchema)
        {
            mutableSchema.Format = "uuid";
        }
    }


}