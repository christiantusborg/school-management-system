using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsDescription;

/// <summary>
/// Represents a description for an endpoint file, providing functionality to retrieve a file URI
/// with an overwrite mechanism if applicable.
/// </summary>
public class EndpointFileDescription : IEndpointDescriptionOverwrite
{
    /// Attempts to retrieve the name for a specified class type utilizing a handler.
    /// <param name="classType">
    /// The type of the class for which the name is being resolved.
    /// </param>
    /// <param name="name">
    /// An output parameter that will contain the resolved name if successful, otherwise null.
    /// </param>
    /// <param name="handler">
    /// A delegate that assists in the resolution process.
    /// </param>
    /// <returns>
    /// Returns a boolean indicating whether the name was successfully resolved.
    /// </returns>
    public bool TryGet(Type classType, out string? name, Delegate handler)
    {
        var className = $"{classType}Uri";
        return EndpointHasFileOverwrite.Get(out name, className, handler);
    }
}