using QuVian.SharedLibrary.Basics.Endpoints.Interfaces;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Provides functionality to determine if a specified class implements the IEndpointAppend interface
/// and retrieve the corresponding file URI if applicable.
/// </summary>
public static class EndpointHasFileAppend
{
    /// Retrieves the URI for a given class name that implements the IEndpointAppend interface.
    /// <param name="uri">An output parameter that receives the URI if the class is found; otherwise, null.</param>
    /// <param name="className">The fully qualified name of the class to search for.</param>
    /// <returns>True if the class is found and implements the IEndpointAppend interface; otherwise, false.</returns>
    public static bool Get(out string? uri, string className)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var foundResult = assemblies
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == className && t.IsClass);

        var interfaceType = typeof(IEndpointAppend);
        if (foundResult is null && !interfaceType.IsAssignableFrom(foundResult))
        {
            uri = null;
            return false;
        }

        var overwriteClass = (IEndpointAppend)Activator.CreateInstance(foundResult)!;

        overwriteClass.TryGet(foundResult,out uri,null!);
        return true;
    }
}

