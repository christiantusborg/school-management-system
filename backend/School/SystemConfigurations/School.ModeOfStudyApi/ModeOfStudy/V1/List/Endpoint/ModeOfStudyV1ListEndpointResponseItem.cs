namespace School.ModeOfStudyApi.ModeOfStudy.V1.List.Endpoint;

public sealed class ModeOfStudyV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required int ModeOfStudyId { get; init; }
    public required string Name { get; init; }

}
