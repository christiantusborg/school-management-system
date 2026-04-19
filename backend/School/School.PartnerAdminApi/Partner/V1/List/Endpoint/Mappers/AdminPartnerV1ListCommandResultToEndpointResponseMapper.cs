using School.PartnerAdminApi.Partner.V1.List.Command;
using School.PartnerAdminApi.Partner.V1.List.Endpoint;

namespace School.PartnerAdminApi.Partner.V1.List.Endpoint.Mappers;

public sealed class AdminPartnerV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor _)
    : IMapper<CommandSearchResult<AdminPartnerV1ListCommandResultItem>, AdminPartnerV1ListEndpointResponse>
{
    public AdminPartnerV1ListEndpointResponse MapFrom(CommandSearchResult<AdminPartnerV1ListCommandResultItem> input)
    {
        return new AdminPartnerV1ListEndpointResponse
        {
            Items = input.Items.Select(x => new AdminPartnerV1ListEndpointResponseItem
            {
                PartnerId = x.PartnerId,
                Name      = x.Name,
                UserCount = x.UserCount,
                IsEnabled = x.IsEnabled,
                DeletedAt = x.DeletedAt,
            }).ToList(),
            Total = input.Total,
            Links = []
        };
    }
}
