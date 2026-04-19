using System.Text.Json.Nodes;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Endpoint;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Options.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Options.Endpoint;

[Route("/v1/school/system-config/enrollment-statuses")]
[EndpointTag("School.SystemConfig.EnrollmentStatus")]
public sealed class EnrollmentStatusV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "integer", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<EnrollmentStatusV1OptionCommand, EnrollmentStatusV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new EnrollmentStatusV1OptionEndpointResponse
        {
            List = new EnrollmentStatusV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(EnrollmentStatusV1ListEndpointResponseItem)),
            },
            Get = new EnrollmentStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = DtoJsonSchemaGenerator.Generate(typeof(EnrollmentStatusV1GetEndpointResponse)),
            },
            Create = new EnrollmentStatusV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(EnrollmentStatusV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(EnrollmentStatusV1CreateEndpointResponse)),
            },
            Update = new EnrollmentStatusV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(EnrollmentStatusV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(EnrollmentStatusV1UpdateEndpointResponse)),
            },
            SoftDelete = new EnrollmentStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            Restore = new EnrollmentStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
            PermanentDelete = new EnrollmentStatusV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
