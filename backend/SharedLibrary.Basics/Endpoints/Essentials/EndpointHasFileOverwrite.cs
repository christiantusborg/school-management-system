using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Provides functionality to determine if a file overwrite behavior is associated with a given endpoint.
/// </summary>
public static class EndpointHasFileOverwrite
{
    /// Retrieves a URI for a specific class using a designated handler and checks
    /// if the requested endpoint has a file overwrite mechanism.
    /// <param name="uri">
    /// The output parameter that, upon successful execution, contains the URI as a string.
    /// If no URI is found or determined, this will be null.
    /// </param>
    /// <param name="className">
    /// The fully qualified name of the class whose URI information is being requested.
    /// </param>
    /// <param name="handler">
    /// A delegate that assists with the process of determining the validity or retrieval
    /// of the endpoint information.
    /// </param>
    /// <returns>
    /// Returns a boolean value indicating whether the operation succeeded.
    /// True if the URI is successfully retrieved; otherwise, false.
    /// </returns>
    public static bool Get(out string? uri, string className, Delegate handler)
    {

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var foundResultGetTypes = assemblies
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Return only the types that were successfully loaded
                    return ex.Types.Where(t => t != null);
                }
            });

        var foundResult = foundResultGetTypes.FirstOrDefault(t => t.FullName == className && t.IsClass);

        var interfaceType = typeof(IEndpointOverwrite);
        if (foundResult is null && !interfaceType.IsAssignableFrom(foundResult))
        {
            uri = null;
            return false;
        }

        var overwriteClass = (IEndpointOverwrite)Activator.CreateInstance(foundResult)!;

        overwriteClass.TryGet(foundResult,out uri,handler);
        return true;
    }
}