using SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Endpoint.Mappers;

public class RegisterInitV1CreateEndpointRequestToCommandMapper(
    IGuidProvider guidProvider,
    IDateTimeProvider dateTimeProvider)
    : IMapper<RegisterInitV1CreateEndpointRequest,
        RegisterInitV1CreateCommand>
{
    public RegisterInitV1CreateCommand MapFrom(RegisterInitV1CreateEndpointRequest input)
    {
        var result = new RegisterInitV1CreateCommand
        {
            UserId = guidProvider.NewId(),
            Email = input.Email,
            BlindedElement = input.BlindedElement,
            Username =  input.Username,
            InviteCode =  input.InviteCode,
            CreatedAt =  dateTimeProvider.UtcNow,
        };
        

        return result;
    }
}