using School.MajorApi.Major.V1.Update.Command;
using School.MajorApi.Major.V1.Update.Endpoint;

namespace School.MajorApi.Major.V1.Update.Endpoint.Mappers;

public sealed class MajorV1UpdateEndpointRequestToCommandMapper
    : IMapper<MajorV1UpdateEndpointRequest, MajorV1UpdateCommand>
{
    public MajorV1UpdateCommand MapFrom(MajorV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MajorV1UpdateCommand
        {
            MajorId = Guid.Empty, // overwritten in endpoint from route parameter
            ProgrammeId = input.ProgrammeId,
            Name = input.Name,
        };
    }
}
