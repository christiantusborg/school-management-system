namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsTags;

/// <summary>
/// Represents an attribute that can be used to tag an endpoint with a specific identifier.
/// </summary>
public class EndpointTagAttribute : Attribute
{
    // Property to hold the URI
    /// <summary>
    /// Represents the tag associated with an endpoint, typically used to assign or retrieve a categorical
    /// identifier or label for a specific endpoint.
    /// </summary>
    /// <remarks>
    /// This property is read-only and its value is set via the constructor of the containing class.
    /// It allows grouping or categorizing endpoints within a system.
    /// </remarks>
    public string? Tag { get; }

    /// <summary>
    /// Attribute used to associate a specific tag with an endpoint.
    /// </summary>
    public EndpointTagAttribute(string? tag)
    {
        Tag = tag;
    }
}