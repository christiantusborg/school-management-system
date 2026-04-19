using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Endpoint.Mappers;

public class PhonesV1UpdateEndpointRequestToCommandMapper
    : IMapper<PhonesV1UpdateEndpointRequest, PhonesV1UpdateCommand>
{
    public PhonesV1UpdateCommand MapFrom(PhonesV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1UpdateCommand
        {
            UserPhoneId = Guid.Empty, // overwritten in endpoint
            UserId = string.Empty, // overwritten in endpoint
            Number = input.Number,
            Label = input.Label,
        };
    }
}
