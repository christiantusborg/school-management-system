namespace QuVian.SharedLibrary.Basics.Endpoints.Hateoas;

/// <summary>
///     Represents a HATEOAS link used for hypermedia navigation.
/// </summary>
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
public record HateoasLink(string? Href, string Rel, string Method);

