namespace SharedLibrary.Basics.Opaque.AdminApi.SchoolSystemConfig.V1.Meta;

[EndpointTag("AdminSchoolSystemConfig")]
public sealed class AdminSchoolSystemConfigV1MetaEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/school/system-config/meta", EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly")
            .WithTags("AdminSchoolSystemConfig")
            .Produces<AdminSchoolSystemConfigV1MetaEndpointResponse>(StatusCodes.Status200OK);
        return app;
    }

    private static IResult EndpointHandlerAsync() =>
        Results.Ok(new AdminSchoolSystemConfigV1MetaEndpointResponse
        {
            Entities =
            [
                new()
                {
                    Key = "documentTypes",
                    Label = "Document Types",
                    Endpoint = "/v1/school/system-config/document-types",
                    IdKey = "documentTypeId",
                    Columns =
                    [
                        new() { Key = "name", Label = "Name" },
                        new() { Key = "description", Label = "Description" },
                    ],
                    Fields =
                    [
                        new() { Key = "name", Label = "Name", Type = "text", Required = true },
                        new() { Key = "description", Label = "Description", Type = "text", Required = false },
                    ],
                },
                new()
                {
                    Key = "enrollmentStatuses",
                    Label = "Enrollment Statuses",
                    Endpoint = "/v1/school/system-config/enrollment-statuses",
                    IdKey = "enrollmentStatusId",
                    Columns =
                    [
                        new() { Key = "name", Label = "Name" },
                    ],
                    Fields =
                    [
                        new() { Key = "name", Label = "Name", Type = "text", Required = true },
                    ],
                },
                new()
                {
                    Key = "modesOfStudy",
                    Label = "Modes of Study",
                    Endpoint = "/v1/school/system-config/modes-of-study",
                    IdKey = "modeOfStudyId",
                    Columns =
                    [
                        new() { Key = "name", Label = "Name" },
                    ],
                    Fields =
                    [
                        new() { Key = "name", Label = "Name", Type = "text", Required = true },
                    ],
                },
                new()
                {
                    Key = "pathways",
                    Label = "Pathways",
                    Endpoint = "/v1/school/system-config/pathways",
                    IdKey = "pathwayId",
                    Columns =
                    [
                        new() { Key = "name", Label = "Name" },
                    ],
                    Fields =
                    [
                        new() { Key = "name", Label = "Name", Type = "text", Required = true },
                    ],
                },
                new()
                {
                    Key = "tuitionFeeStatuses",
                    Label = "Tuition Fee Statuses",
                    Endpoint = "/v1/school/system-config/tuition-fee-statuses",
                    IdKey = "tuitionFeeStatusId",
                    Columns =
                    [
                        new() { Key = "name", Label = "Name" },
                    ],
                    Fields =
                    [
                        new() { Key = "name", Label = "Name", Type = "text", Required = true },
                    ],
                },
            ],
        });
}
