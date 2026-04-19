using Swashbuckle.AspNetCore.SwaggerGen;

namespace QuVian.SharedLibrary.Basics.Extensions.Swagger;

/// <summary>
/// Populates operation parameter default values and descriptions using API metadata.
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        foreach (var responseType in apiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys.ToList())
            {
                if (!responseType.ApiResponseFormats.Any(x => x.MediaType == contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);
            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema is OpenApiSchema mutableSchema && mutableSchema.Default == null &&
                description.DefaultValue != null && description.DefaultValue is not DBNull)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(description.DefaultValue, description.ModelMetadata!.ModelType);
                mutableSchema.Default = System.Text.Json.Nodes.JsonNode.Parse(json);
            }

            if (parameter is OpenApiParameter mutableParameter)
                mutableParameter.Required |= description.IsRequired;
        }
    }
}