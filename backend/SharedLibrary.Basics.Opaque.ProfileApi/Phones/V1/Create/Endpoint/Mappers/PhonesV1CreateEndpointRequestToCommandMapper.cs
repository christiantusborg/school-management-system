using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Endpoint.Mappers;

public class PhonesV1CreateEndpointRequestToCommandMapper
    : IMapper<PhonesV1CreateEndpointRequest, PhonesV1CreateCommand>
{
    public PhonesV1CreateCommand MapFrom(PhonesV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1CreateCommand
        {
            UserId = string.Empty, // overwritten in endpoint
            Number = input.Number,
            Label = input.Label,
        };
    }
}
