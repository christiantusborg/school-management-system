using System.Text.Json.Nodes;
using School.PathwayApi.EducationLevel.V1.Create.Endpoint;
using School.PathwayApi.EducationLevel.V1.List.Endpoint;
using School.PathwayApi.EducationLevel.V1.Options.Command;
using School.PathwayApi.EducationLevel.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.PathwayApi.EducationLevel.V1.Options.Endpoint;

[Route("/v1/school/system-config/education-levels")]
[EndpointTag("School.SystemConfig.EducationLevel")]
public sealed class EducationLevelV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<EducationLevelV1OptionCommand, EducationLevelV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new EducationLevelV1OptionEndpointResponse
        {
            List = new EducationLevelV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(EducationLevelV1ListEndpointResponseItem)),
            },
            Create = new EducationLevelV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(EducationLevelV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(EducationLevelV1CreateEndpointResponse)),
            },
            Update = new EducationLevelV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(EducationLevelV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(EducationLevelV1UpdateEndpointResponse)),
            },
            SoftDelete = new EducationLevelV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
