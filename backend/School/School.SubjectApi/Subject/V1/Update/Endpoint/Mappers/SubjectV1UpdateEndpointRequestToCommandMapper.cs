using School.SubjectApi.Subject.V1.Update.Command;
using School.SubjectApi.Subject.V1.Update.Endpoint;

namespace School.SubjectApi.Subject.V1.Update.Endpoint.Mappers;

public sealed class SubjectV1UpdateEndpointRequestToCommandMapper
    : IMapper<SubjectV1UpdateEndpointRequest, SubjectV1UpdateCommand>
{
    public SubjectV1UpdateCommand MapFrom(SubjectV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new SubjectV1UpdateCommand
        {
            SubjectId = Guid.Empty, // overwritten in endpoint from route parameter
            MajorId = input.MajorId,
            Code = input.Code,
            Name = input.Name,
            Ects = input.Ects,
        };
    }
}
