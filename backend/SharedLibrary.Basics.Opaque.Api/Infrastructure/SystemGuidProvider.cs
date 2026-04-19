using QuVian.SharedLibrary.Basics.MockProviders;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

internal sealed class SystemGuidProvider : IGuidProvider
{
    public Guid NewId() => Guid.NewGuid();
}
