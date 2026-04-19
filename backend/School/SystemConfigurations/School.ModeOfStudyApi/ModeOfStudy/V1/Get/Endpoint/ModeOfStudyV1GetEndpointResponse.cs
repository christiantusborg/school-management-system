namespace School.ModeOfStudyApi.ModeOfStudy.V1.Get.Endpoint;

public sealed class ModeOfStudyV1GetEndpointResponse : HateoasBaseResponse
{
    public required int ModeOfStudyId { get; init; }
    public required string Name { get; init; }

    public DateTime? DeletedAt { get; init; }
}
