using System.Text.Json.Nodes;
using School.MajorApi.Major.V1.Create.Endpoint;
using School.MajorApi.Major.V1.Get.Endpoint;
using School.MajorApi.Major.V1.List.Endpoint;
using School.MajorApi.Major.V1.Options.Command;
using School.MajorApi.Major.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.MajorApi.Major.V1.Options.Endpoint;

[Route("/v1/school/majors")]
[EndpointTag("School.Major")]
public sealed class MajorV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<MajorV1OptionCommand, MajorV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new MajorV1OptionEndpointResponse
        {
            List = new MajorV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(MajorV1ListEndpointResponseItem)),
            },
            Get = new MajorV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = DtoJsonSchemaGenerator.Generate(typeof(MajorV1GetEndpointResponse)),
            },
            Create = new MajorV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(MajorV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(MajorV1CreateEndpointResponse)),
            },
            Update = new MajorV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(MajorV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(MajorV1UpdateEndpointResponse)),
            },
            SoftDelete = new MajorV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            Restore = new MajorV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            PermanentDelete = new MajorV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
