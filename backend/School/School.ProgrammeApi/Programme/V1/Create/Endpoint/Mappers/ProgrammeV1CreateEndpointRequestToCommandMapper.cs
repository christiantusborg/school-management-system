using School.ProgrammeApi.Programme.V1.Create.Command;
using School.ProgrammeApi.Programme.V1.Create.Endpoint;

namespace School.ProgrammeApi.Programme.V1.Create.Endpoint.Mappers;

public sealed class ProgrammeV1CreateEndpointRequestToCommandMapper
    : IMapper<ProgrammeV1CreateEndpointRequest, ProgrammeV1CreateCommand>
{
    public ProgrammeV1CreateCommand MapFrom(ProgrammeV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProgrammeV1CreateCommand
        {
            Name = input.Name,
            Code = input.Code,
            PathwayIds = input.PathwayIds is null
                ? []
                : input.PathwayIds.Where(id => id > 0).Distinct().ToList(),
        };
    }
}
