namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsSummary;

/// <summary>
/// Represents an attribute that allows adding a summary description to a class or other member.
/// </summary>
public class SummaryAttribute(string? summary) : Attribute
{
    // Property to hold the URI
    public string? Summary { get; } = summary;
}