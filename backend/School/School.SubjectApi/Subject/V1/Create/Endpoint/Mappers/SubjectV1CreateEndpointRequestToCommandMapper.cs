using School.SubjectApi.Subject.V1.Create.Command;
using School.SubjectApi.Subject.V1.Create.Endpoint;

namespace School.SubjectApi.Subject.V1.Create.Endpoint.Mappers;

public sealed class SubjectV1CreateEndpointRequestToCommandMapper
    : IMapper<SubjectV1CreateEndpointRequest, SubjectV1CreateCommand>
{
    public SubjectV1CreateCommand MapFrom(SubjectV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new SubjectV1CreateCommand
        {
            MajorId = input.MajorId,
            Code = input.Code,
            Name = input.Name,
            Ects = input.Ects,
        };
    }
}
