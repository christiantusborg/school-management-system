using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials.EndpointsAdjunctsTags;

/// <summary>
/// Provides functionality for extracting endpoint tags from classes
/// decorated with the <see cref="EndpointTagAttribute"/>.
/// </summary>
public class EndpointAttributeTag : IEndpointTag
{
    /// <summary>
    /// Tries to retrieve the endpoint tag associated with a specified class type.
    /// </summary>
    /// <param name="classType">The type of the class to inspect for the <see cref="EndpointTagAttribute"/>.</param>
    /// <param name="name">When this method returns, contains the tag value associated with the class type, if a tag exists. Otherwise, contains null.</param>
    /// <param name="handler">A delegate that provides additional context for the operation, though it is not directly used in this method.</param>
    /// <returns>
    /// Returns true if the <see cref="EndpointTagAttribute"/> is found and its tag is successfully retrieved; otherwise, false.
    /// </returns>
    public bool TryGet(Type classType,out string? name, Delegate handler)
    {
        var attribute = classType.GetCustomAttribute<EndpointTagAttribute>();

        // If the attribute exists, return the URI; otherwise, return null or throw an exception
        if (attribute != null)
        {
            name =  attribute.Tag;
            return true;
        }

        name = null;
        return false;
    }
}