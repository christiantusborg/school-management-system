namespace School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Endpoint;

public sealed class ModeOfStudyV1PermanentDeleteEndpointResponse : HateoasBaseResponse
{
    public required int ModeOfStudyId { get; init; }
}
