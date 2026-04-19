namespace QuVian.SharedLibrary.Basics.MockProviders;

/// <summary>
/// Interface representing a provider for generating new Guids, allowing for mockability in unit testing scenarios.
/// </summary>
public interface IGuidProvider
{
    /// <summary>
    /// Generates a new Guid.
    /// </summary>
    /// <returns>A new Guid.</returns>
    Guid NewId();
}

