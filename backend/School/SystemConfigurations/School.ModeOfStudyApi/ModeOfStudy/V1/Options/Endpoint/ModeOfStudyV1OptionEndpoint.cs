using System.Text.Json.Nodes;
using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.Get.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.List.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.Options.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Options.Endpoint;

[Route("/v1/school/system-config/modes-of-study")]
[EndpointTag("School.SystemConfig.ModeOfStudy")]
public sealed class ModeOfStudyV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "integer", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<ModeOfStudyV1OptionCommand, ModeOfStudyV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new ModeOfStudyV1OptionEndpointResponse
        {
            List = new ModeOfStudyV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(ModeOfStudyV1ListEndpointResponseItem)),
            },
            Get = new ModeOfStudyV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = DtoJsonSchemaGenerator.Generate(typeof(ModeOfStudyV1GetEndpointResponse)),
            },
            Create = new ModeOfStudyV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(ModeOfStudyV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(ModeOfStudyV1CreateEndpointResponse)),
            },
            Update = new ModeOfStudyV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(ModeOfStudyV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(ModeOfStudyV1UpdateEndpointResponse)),
            },
            SoftDelete = new ModeOfStudyV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            Restore = new ModeOfStudyV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            PermanentDelete = new ModeOfStudyV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
