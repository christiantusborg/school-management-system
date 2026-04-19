using School.MajorApi.Major.V1.Create.Command;
using School.MajorApi.Major.V1.Create.Endpoint;

namespace School.MajorApi.Major.V1.Create.Endpoint.Mappers;

public sealed class MajorV1CreateEndpointRequestToCommandMapper
    : IMapper<MajorV1CreateEndpointRequest, MajorV1CreateCommand>
{
    public MajorV1CreateCommand MapFrom(MajorV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MajorV1CreateCommand
        {
            ProgrammeId = input.ProgrammeId,
            Name = input.Name,
        };
    }
}
