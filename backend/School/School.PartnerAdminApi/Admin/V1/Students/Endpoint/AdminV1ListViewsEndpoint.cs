using System.Security.Claims;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Per-user saved column views for list pages (the admission Students
/// overview): unlimited named views, each an ordered set of column keys.
/// Views belong to the calling user only.
/// </summary>
[Route("/v1/admin/list-views")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1ListViewsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/list-views", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/list-views", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/list-views/{viewId:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/list-views/{viewId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static string? CallerId(HttpContext httpContext) =>
        httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public sealed class ViewBody
    {
        public string? Page { get; init; }
        public string? Name { get; init; }
        public List<string>? Columns { get; init; }
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct, [FromQuery] string? page = null)
    {
        var userId = CallerId(httpContext);
        if (userId is null) return Results.Unauthorized();
        var views = await db.UserListViews
            .Where(v => v.UserId == userId && (page == null || v.Page == page))
            .OrderBy(v => v.SortOrder).ThenBy(v => v.CreatedAt)
            .ToListAsync(ct);
        var ids = views.Select(v => v.UserListViewId).ToList();
        var cols = await db.UserListViewColumns
            .Where(c => ids.Contains(c.UserListViewId))
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
        return Results.Ok(new
        {
            items = views.Select(v => new
            {
                id = v.UserListViewId,
                name = v.Name,
                page = v.Page,
                columns = cols.Where(c => c.UserListViewId == v.UserListViewId).Select(c => c.ColumnKey).ToList(),
            }).ToList(),
        });
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext, [FromBody] ViewBody body, OdinDbContext db, CancellationToken ct)
    {
        var userId = CallerId(httpContext);
        if (userId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Page))
            return Results.BadRequest(new { error = "Name and page are required." });
        var view = new UserListView { UserId = userId, Page = body.Page.Trim(), Name = body.Name.Trim() };
        db.UserListViews.Add(view);
        SetColumns(db, view.UserListViewId, body.Columns);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = view.UserListViewId });
    }

    private static async Task<IResult> UpdateAsync(
        Guid viewId, HttpContext httpContext, [FromBody] ViewBody body, OdinDbContext db, CancellationToken ct)
    {
        var userId = CallerId(httpContext);
        var view = await db.UserListViews.FirstOrDefaultAsync(v => v.UserListViewId == viewId && v.UserId == userId, ct);
        if (view is null) return Results.NotFound();
        if (!string.IsNullOrWhiteSpace(body.Name)) view.Name = body.Name.Trim();
        var old = await db.UserListViewColumns.Where(c => c.UserListViewId == viewId).ToListAsync(ct);
        db.UserListViewColumns.RemoveRange(old);
        SetColumns(db, viewId, body.Columns);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = viewId });
    }

    private static async Task<IResult> DeleteAsync(
        Guid viewId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var userId = CallerId(httpContext);
        var view = await db.UserListViews.FirstOrDefaultAsync(v => v.UserListViewId == viewId && v.UserId == userId, ct);
        if (view is null) return Results.NotFound();
        var cols = await db.UserListViewColumns.Where(c => c.UserListViewId == viewId).ToListAsync(ct);
        db.UserListViewColumns.RemoveRange(cols);
        db.UserListViews.Remove(view);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = viewId });
    }

    private static void SetColumns(OdinDbContext db, Guid viewId, List<string>? columns)
    {
        var sort = 0;
        foreach (var key in (columns ?? []).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct())
            db.UserListViewColumns.Add(new UserListViewColumn
            {
                UserListViewId = viewId,
                ColumnKey = key.Trim(),
                SortOrder = sort++,
            });
    }
}
