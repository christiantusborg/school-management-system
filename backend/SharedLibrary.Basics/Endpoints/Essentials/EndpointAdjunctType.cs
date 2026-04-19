namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Represents the different types of adjuncts that can be associated with an endpoint.
/// </summary>
public enum EndpointAdjunctType
{
    /// <summary>
    /// Represents the URI adjunct type for an endpoint.
    /// </summary>
    Uri,

    /// <summary>
    /// Represents a summary of the endpoint.
    /// </summary>
    Summary,

    /// <summary>
    /// Represents the description of the endpoint.
    /// </summary>
    Description,

    /// <summary>
    /// Represents a categorization label assigned to the endpoint.
    /// </summary>
    Tag,

    /// <summary>
    /// The name of the endpoint.
    /// </summary>
    Name
}

