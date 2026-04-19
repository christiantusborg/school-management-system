using School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Command;
using School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Endpoint;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Endpoint.Mappers;

public sealed class EnrollmentStatusV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<EnrollmentStatusV1ListCommandResultItem>, BaseGetAllResponse<EnrollmentStatusV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<EnrollmentStatusV1ListEndpointResponseItem> MapFrom(CommandSearchResult<EnrollmentStatusV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new EnrollmentStatusV1ListEndpointResponseItem
        {
            EnrollmentStatusId = x.EnrollmentStatusId,
            Name = x.Name,
            AllowSetByPartner = x.AllowSetByPartner,
            Links = []
        }).ToList();

        return new BaseGetAllResponse<EnrollmentStatusV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
