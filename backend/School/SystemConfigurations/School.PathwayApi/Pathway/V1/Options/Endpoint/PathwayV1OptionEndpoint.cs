using System.Text.Json.Nodes;
using School.PathwayApi.Pathway.V1.Create.Endpoint;
using School.PathwayApi.Pathway.V1.Get.Endpoint;
using School.PathwayApi.Pathway.V1.List.Endpoint;
using School.PathwayApi.Pathway.V1.Options.Command;
using School.PathwayApi.Pathway.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.PathwayApi.Pathway.V1.Options.Endpoint;

[Route("/v1/school/system-config/pathways")]
[EndpointTag("School.SystemConfig.Pathway")]
public sealed class PathwayV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "integer", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<PathwayV1OptionCommand, PathwayV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new PathwayV1OptionEndpointResponse
        {
            List = new PathwayV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(PathwayV1ListEndpointResponseItem)),
            },
            Get = new PathwayV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = DtoJsonSchemaGenerator.Generate(typeof(PathwayV1GetEndpointResponse)),
            },
            Create = new PathwayV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(PathwayV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(PathwayV1CreateEndpointResponse)),
            },
            Update = new PathwayV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(PathwayV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(PathwayV1UpdateEndpointResponse)),
            },
            SoftDelete = new PathwayV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            Restore = new PathwayV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            PermanentDelete = new PathwayV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
