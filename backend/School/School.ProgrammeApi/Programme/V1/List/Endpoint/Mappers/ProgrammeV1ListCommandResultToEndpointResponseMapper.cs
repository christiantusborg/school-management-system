using School.ProgrammeApi.Programme.V1.List.Command;
using School.ProgrammeApi.Programme.V1.List.Endpoint;

namespace School.ProgrammeApi.Programme.V1.List.Endpoint.Mappers;

public sealed class ProgrammeV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<ProgrammeV1ListCommandResultItem>, BaseGetAllResponse<ProgrammeV1ListEndpointResponseItem>>
{
    public BaseGetAllResponse<ProgrammeV1ListEndpointResponseItem> MapFrom(CommandSearchResult<ProgrammeV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        var items = input.Items.Select(x => new ProgrammeV1ListEndpointResponseItem
        {
            ProgrammeId = x.ProgrammeId,
            Name = x.Name,
            Code = x.Code,
            DeletedAt = x.DeletedAt,
            PartnerId = x.PartnerId,
            PartnerName = x.PartnerName,
            Status = x.Status,
            IsActive = x.IsActive,
            IsDisabledByAdmin = x.IsDisabledByAdmin,
            RejectionReason = x.RejectionReason,
            SubmittedAt = x.SubmittedAt,
            ApprovedAt = x.ApprovedAt,
            HasEnrolments = x.HasEnrolments,
            PathwayIds = x.PathwayIds,
            Links = []
        }).ToList();

        return new BaseGetAllResponse<ProgrammeV1ListEndpointResponseItem>
        {
            Items = items,
            Total = input.Total
        };
    }
}
