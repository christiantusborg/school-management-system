using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsSummary;

/// <summary>
/// Provides functionality to extract and manage summary attributes for endpoint classes.
/// </summary>
public class EndpointAttributeSummery : IEndpointSummaryOverwrite
{
    /// <summary>
    /// Attempts to retrieve the summary string associated with a given class type's
    /// <see cref="SummaryAttribute"/> and assigns it to the output parameter if found.
    /// </summary>
    /// <param name="classType">
    /// The type of the class to inspect for the <see cref="SummaryAttribute"/>.
    /// </param>
    /// <param name="name">
    /// When this method returns, contains the retrieved summary string if the attribute is found;
    /// otherwise, contains null. This parameter is passed uninitialized.
    /// </param>
    /// <param name="handler">
    /// A delegate that may be used in conjunction with endpoint logic.
    /// </param>
    /// <returns>
    /// true if the <see cref="SummaryAttribute"/> is present and its summary is successfully retrieved;
    /// otherwise, false.
    /// </returns>
    public bool TryGet(Type classType,out string? name, Delegate handler)
    {
        var attribute = classType.GetCustomAttribute<SummaryAttribute>();

        // If the attribute exists, return the URI; otherwise, return null or throw an exception
        if (attribute != null)
        {
            name =  attribute.Summary;
            return true;
        }

        name = null;
        return false;
    }
}