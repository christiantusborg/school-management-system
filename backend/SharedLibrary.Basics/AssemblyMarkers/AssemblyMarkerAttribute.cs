using System.Diagnostics.CodeAnalysis;

namespace QuVian.SharedLibrary.Basics.AssemblyMarkers;

/// <summary>
/// Marks an assembly with a custom attribute.
/// </summary>
[AttributeUsage(AttributeTargets.ReturnValue)]
[SuppressMessage("Performance", "CA1813:Avoid unsealed attributes")]
public class AssemblyMarkerAttribute : Attribute;