namespace QuVian.SharedLibrary.Basics.Endpoints.Hateoas;

/// <summary>
///     Base class for an endpoint.
/// </summary>
[SuppressMessage("Design", "CA1002:Do not expose generic lists")]
[SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
public abstract class HateoasBaseResponse
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="HateoasBaseResponse" /> class.
    /// </summary>
    protected HateoasBaseResponse()
    {
    }

    /// <summary>
    ///     Gets or sets the list of links.
    ///     Hypermedia as the Engine of Application State (HATEOAS) is a component of the REST application architecture that
    ///     distinguishes it from other network application architectures.
    /// </summary>
    /// <remarks>
    ///     The links property represents a list of Link objects.
    ///     These Link objects contain information about the URLs to other resources.
    ///     This property is required and must be initialized.
    /// </remarks>
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public required IList<HateoasLink> Links { get; set; }
}

