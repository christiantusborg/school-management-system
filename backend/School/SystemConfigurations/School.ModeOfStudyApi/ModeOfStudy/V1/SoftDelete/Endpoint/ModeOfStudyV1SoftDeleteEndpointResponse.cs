namespace School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Endpoint;

public sealed class ModeOfStudyV1SoftDeleteEndpointResponse : HateoasBaseResponse
{
    public required int ModeOfStudyId { get; init; }
}
