using School.ProgrammeApi.Programme.V1.Update.Command;
using School.ProgrammeApi.Programme.V1.Update.Endpoint;

namespace School.ProgrammeApi.Programme.V1.Update.Endpoint.Mappers;

public sealed class ProgrammeV1UpdateEndpointRequestToCommandMapper
    : IMapper<ProgrammeV1UpdateEndpointRequest, ProgrammeV1UpdateCommand>
{
    public ProgrammeV1UpdateCommand MapFrom(ProgrammeV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProgrammeV1UpdateCommand
        {
            ProgrammeId = Guid.Empty, // overwritten in endpoint from route parameter
            Name = input.Name,
            Code = input.Code,
            PathwayIds = input.PathwayIds is null
                ? []
                : input.PathwayIds.Where(id => id > 0).Distinct().ToList(),
        };
    }
}
