namespace QuVian.SharedLibrary.Basics.MockProviders;

/// <summary>
/// Provides an implementation for generating new Guids in a way that can be mocked or replaced for testing purposes.
/// </summary>
public class GuidProvider : IGuidProvider
{
    /// <summary>
    /// Generates a new Guid.
    /// </summary>
    /// <returns>A new Guid.</returns>
    public Guid NewId()
    {
        return Guid.NewGuid();
    }
}

