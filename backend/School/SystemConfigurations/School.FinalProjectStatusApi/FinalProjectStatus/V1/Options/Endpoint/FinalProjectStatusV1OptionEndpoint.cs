using System.Text.Json.Nodes;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Options.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Options.Endpoint;

[Route("/v1/school/system-config/final-project-statuses")]
[EndpointTag("School.SystemConfig.FinalProjectStatus")]
public sealed class FinalProjectStatusV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "integer", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<FinalProjectStatusV1OptionCommand, FinalProjectStatusV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new FinalProjectStatusV1OptionEndpointResponse
        {
            List = new FinalProjectStatusV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(FinalProjectStatusV1ListEndpointResponseItem)),
            },
            Get = new FinalProjectStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = DtoJsonSchemaGenerator.Generate(typeof(FinalProjectStatusV1GetEndpointResponse)),
            },
            Create = new FinalProjectStatusV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(FinalProjectStatusV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(FinalProjectStatusV1CreateEndpointResponse)),
            },
            Update = new FinalProjectStatusV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(FinalProjectStatusV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(FinalProjectStatusV1UpdateEndpointResponse)),
            },
            SoftDelete = new FinalProjectStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            Restore = new FinalProjectStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            PermanentDelete = new FinalProjectStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
