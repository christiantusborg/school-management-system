using System.Text.Json.Nodes;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Options.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Options.Endpoint;

[Route("/v1/school/system-config/tuition-fee-statuses")]
[EndpointTag("School.SystemConfig.TuitionFeeStatus")]
public sealed class TuitionFeeStatusV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "integer", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<TuitionFeeStatusV1OptionCommand, TuitionFeeStatusV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new TuitionFeeStatusV1OptionEndpointResponse
        {
            List = new TuitionFeeStatusV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(TuitionFeeStatusV1ListEndpointResponseItem)),
            },
            Get = new TuitionFeeStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = DtoJsonSchemaGenerator.Generate(typeof(TuitionFeeStatusV1GetEndpointResponse)),
            },
            Create = new TuitionFeeStatusV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(TuitionFeeStatusV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(TuitionFeeStatusV1CreateEndpointResponse)),
            },
            Update = new TuitionFeeStatusV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(TuitionFeeStatusV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(TuitionFeeStatusV1UpdateEndpointResponse)),
            },
            SoftDelete = new TuitionFeeStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            Restore = new TuitionFeeStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            PermanentDelete = new TuitionFeeStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
