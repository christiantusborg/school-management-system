using System.Text.Json.Nodes;
using School.DocumentTypeApi.DocumentType.V1.Create.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.List.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.Options.Command;
using School.DocumentTypeApi.DocumentType.V1.Update.Endpoint;
using QuVian.SharedLibrary.Basics.Endpoints.Schema;

namespace School.DocumentTypeApi.DocumentType.V1.Options.Endpoint;

[Route("/v1/school/system-config/document-types")]
[EndpointTag("School.SystemConfig.DocumentType")]
public sealed class DocumentTypeV1OptionEndpoint : IEndpointMarker
{
    private static readonly JsonNode PathIdParam = new JsonObject
    {
        ["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid", ["in"] = "path" }
    };

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapOption<DocumentTypeV1OptionCommand, DocumentTypeV1OptionEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new DocumentTypeV1OptionEndpointResponse
        {
            List = new DocumentTypeV1OptionInnerEndpointResponse
            {
                Request = null,
                Result = DtoJsonSchemaGenerator.GenerateListOf(typeof(DocumentTypeV1ListEndpointResponseItem)),
            },
            Create = new DocumentTypeV1OptionInnerEndpointResponse
            {
                Request = DtoJsonSchemaGenerator.Generate(typeof(DocumentTypeV1CreateEndpointRequest)),
                Result = DtoJsonSchemaGenerator.Generate(typeof(DocumentTypeV1CreateEndpointResponse)),
            },
            Update = new DocumentTypeV1OptionInnerEndpointResponse
            {
                Request = new JsonObject
                {
                    ["pathParams"] = PathIdParam.DeepClone(),
                    ["body"] = DtoJsonSchemaGenerator.Generate(typeof(DocumentTypeV1UpdateEndpointRequest)),
                },
                Result = DtoJsonSchemaGenerator.Generate(typeof(DocumentTypeV1UpdateEndpointResponse)),
            },
            SoftDelete = new DocumentTypeV1OptionInnerEndpointResponse
            {
                Request = PathIdParam.DeepClone(),
                Result = null,
            },
        });
}
