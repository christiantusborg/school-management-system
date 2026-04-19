using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace QuVian.SharedLibrary.Basics.Endpoints.Hateoas;

/// <summary>
///     Retrieves a list of HateoasLinks for a given path.
/// </summary>
/// <returns>A list of HateoasLink objects.</returns>
public static class HateoasLinksHelper
{
    /// <summary>
    ///     Retrieves a list of HateoasLinks for a given path.
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="id"></param>
    /// <param name="isUndeleted">Optional. Specifies whether the links should include an "undelete" link. Defaults to false.</param>
    /// <returns>A list of HateoasLink objects.</returns>
    public static IList<HateoasLink> AsGet(IHttpContextAccessor httpContextAccessor, Guid id, bool isUndeleted = false)
    {
        return Get(httpContextAccessor, id, HateoasType.Get, isUndeleted);
    }

    /// <summary>
    ///     Returns a list of HateoasLink objects for a specified id using the GetAll HateoasType.
    /// </summary>
    /// <param name="httpContextAccessor">An instance of IHttpContextAccessor used to access the current HttpContext.</param>
    /// <param name="id">The unique identifier used to retrieve the HateoasLink objects.</param>
    /// <param name="isUndeleted">
    ///     A boolean value indicating whether to include undeleted HateoasLink objects. Defaults to
    ///     false.
    /// </param>
    /// <returns>
    ///     A list of HateoasLink objects for the specified id using the GetAll HateoasType.
    /// </returns>
    public static IList<HateoasLink> AsGetAll(IHttpContextAccessor httpContextAccessor, Guid id, bool isUndeleted = false)
    {
        return Get(httpContextAccessor, id, HateoasType.GetAll, isUndeleted);
    }

    /// <summary>
    ///     Retrieves a collection of Hateoas links for creating a new item.
    /// </summary>
    /// <param name="httpContextAccessor">The HttpContextAccessor instance used to access the current HttpContext.</param>
    /// <param name="id">The unique identifier of the item.</param>
    /// <returns>A collection of HateoasLink objects for creating a new item.</returns>
    public static IList<HateoasLink> AsCreate(IHttpContextAccessor httpContextAccessor, Guid id)
    {
        return Get(httpContextAccessor, id, HateoasType.Post, false);
    }

    /// <summary>
    ///     Retrieves the Hateoas links for updating a resource.
    /// </summary>
    /// <param name="httpContextAccessor">The HttpContextAccessor to access the current HTTP context.</param>
    /// <param name="id">The GUID of the resource to be updated.</param>
    /// <param name="isUndeleted">A flag indicating whether the resource is undeleted.</param>
    /// <returns>A list of Hateoas links for updating the specified resource.</returns>
    public static IList<HateoasLink> AsUpdate(IHttpContextAccessor httpContextAccessor, Guid id, bool isUndeleted = false)
    {
        return Get(httpContextAccessor, id, HateoasType.Put, isUndeleted);
    }

    /// <summary>
    ///     Converts the given HTTP context accessor and ID to a list of HateoasLink objects,
    ///     indicating that the resource should be deleted.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="id">The ID of the resource to be deleted.</param>
    /// <returns>A list of HateoasLink objects.</returns>
    public static IList<HateoasLink> AsDelete(IHttpContextAccessor httpContextAccessor, Guid id)
    {
        return Get(httpContextAccessor, id, HateoasType.Delete, true);
    }

    /// <summary>
    ///     Converts the given HTTP context accessor and ID to a list of HateoasLink objects,
    ///     indicating that the resource should be undeleted.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="id">The ID of the resource to be deleted.</param>
    /// <returns>A list of HateoasLink objects.</returns>
    public static IList<HateoasLink> AsUnDelete(IHttpContextAccessor httpContextAccessor, Guid id)
    {
        return Get(httpContextAccessor, id, HateoasType.UnDelete, false);
    }

    /// <summary>
    ///     Gets a list of HateoasLinks for the specified parameters.
    /// </summary>
    /// <param name="httpContextAccessor">The IHttpContextAccessor instance used to access the current HttpContext.</param>
    /// <param name="id">The unique identifier.</param>
    /// <param name="type">The HateoasType enum value representing the type of operation.</param>
    /// <param name="isUndeleted">A boolean value indicating whether the resource is undeleted.</param>
    /// <returns>
    ///     An IList of HateoasLink objects representing the HATEOAS links for the specified parameters.
    /// </returns>
    private static List<HateoasLink> Get(IHttpContextAccessor httpContextAccessor, Guid id, HateoasType type, bool isUndeleted)
    {

        Debug.Assert(httpContextAccessor != null, nameof(httpContextAccessor) + " != null");
        Debug.Assert(httpContextAccessor.HttpContext != null, "httpContextAccessor.HttpContext != null");

        var path = httpContextAccessor.HttpContext.Request.Path.Value ?? string.Empty;

        var result = new List<HateoasLink>();
        if (type is HateoasType.Post)
        {
            result.Add(new HateoasLink( path + "/" + id, "self","GET"));
            result.Add(new HateoasLink( path + "/" + id, "update","PUT"));
            result.Add(isUndeleted
                ? new HateoasLink( path + "/" + id + "/undelete","undelete", "PUT")
                : new HateoasLink( path + "/" + id, "delete", "DELETE"));
        }

        if (type is HateoasType.Get or HateoasType.Put or HateoasType.Delete)
        {

            result.Add(new HateoasLink(path + "/", "self", "GET"));
            result.Add(new HateoasLink(path + "/", "update", "PUT"));

            result.Add(isUndeleted
                ? new HateoasLink( path + "/" + "/undelete","undelete", "PUT")
                : new HateoasLink( path + "/", "delete", "DELETE"));
        }

        return result;
    }
}

