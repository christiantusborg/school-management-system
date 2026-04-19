using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Command;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Endpoint;
using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class AdminUsersV1ResetPasswordMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<AdminUsersV1ResetPasswordCommandResult, AdminUsersV1ResetPasswordEndpointResponse>,
            AdminUsersV1ResetPasswordCommandResultToEndpointResponseMapper>();
        return services;
    }
}
