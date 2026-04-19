using System.Text.Json.Nodes;
using School.ProgrammeApi.Programme.V1.Create.Endpoint;
using School.ProgrammeApi.Programme.V1.Get.Endpoint;
using School.ProgrammeApi.Programme.V1.List.Endpoint;
using School.ProgrammeApi.Programme.V1.Options.Command;
using School.ProgrammeApi.Programme.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.ProgrammeApi.Programme.V1.Options.Endpoint;

[Route("/v1/school/programmes")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<ProgrammeV1OptionCommand, ProgrammeV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new ProgrammeV1OptionEndpointResponse
        {
            List = new ProgrammeV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(ProgrammeV1ListEndpointResponseItem)),
            },
            Get = new ProgrammeV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = DtoJsonSchemaGenerator.Generate(typeof(ProgrammeV1GetEndpointResponse)),
            },
            Create = new ProgrammeV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(ProgrammeV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(ProgrammeV1CreateEndpointResponse)),
            },
            Update = new ProgrammeV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(ProgrammeV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(ProgrammeV1UpdateEndpointResponse)),
            },
            SoftDelete = new ProgrammeV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            Restore = new ProgrammeV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            PermanentDelete = new ProgrammeV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
