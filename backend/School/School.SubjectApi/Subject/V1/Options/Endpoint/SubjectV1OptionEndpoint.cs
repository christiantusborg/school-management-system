using System.Text.Json.Nodes;
using School.SubjectApi.Subject.V1.Create.Endpoint;
using School.SubjectApi.Subject.V1.Get.Endpoint;
using School.SubjectApi.Subject.V1.List.Endpoint;
using School.SubjectApi.Subject.V1.Options.Command;
using School.SubjectApi.Subject.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.SubjectApi.Subject.V1.Options.Endpoint;

[Route("/v1/school/subjects")]
[EndpointTag("School.Subject")]
public sealed class SubjectV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<SubjectV1OptionCommand, SubjectV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new SubjectV1OptionEndpointResponse
        {
            List = new SubjectV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(SubjectV1ListEndpointResponseItem)),
            },
            Get = new SubjectV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = DtoJsonSchemaGenerator.Generate(typeof(SubjectV1GetEndpointResponse)),
            },
            Create = new SubjectV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(SubjectV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(SubjectV1CreateEndpointResponse)),
            },
            Update = new SubjectV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(SubjectV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(SubjectV1UpdateEndpointResponse)),
            },
            SoftDelete = new SubjectV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            Restore = new SubjectV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            PermanentDelete = new SubjectV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
