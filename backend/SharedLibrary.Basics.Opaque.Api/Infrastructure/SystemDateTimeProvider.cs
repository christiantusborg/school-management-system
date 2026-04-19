using QuVian.SharedLibrary.Basics.MockProviders;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
