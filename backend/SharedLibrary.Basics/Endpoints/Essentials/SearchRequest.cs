using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuVian.SharedLibrary.Basics.Filters;

namespace QuVian.SharedLibrary.Basics.Endpoints.Essentials;

/// <summary>
/// Encapsulates the details of search and paging parameters, including filters, sorting, page number, page size,
/// and additional configurations such as whether to include soft-deleted records.
/// Designed for use with query-string parsing in web applications.
/// </summary>
public sealed class SearchRequest
{
    // ────────── filter / paging fields ──────────
    /// <summary>
    /// Represents a collection of filters used for querying or refining the results
    /// in a <see cref="SearchRequest"/> object. Each filter defines specific
    /// conditions or rules to apply when processing the search request.
    /// </summary>
    public List<Filter>?  Filters    { get; init; }

    /// <summary>
    /// Represents a collection of sorting fields applied to the search request.
    /// Each sorting field specifies a field and its sort direction (ascending or descending).
    /// </summary>
    public List<Sorting>? SortField  { get; init; }

    /// <summary>
    /// Gets or sets the current page index for the search or paging request.
    /// </summary>
    /// <remarks>
    /// This property is used to determine the set of items to be retrieved based on the requested page.
    /// Page numbering is typically expected to start from 1 (first page).
    /// </remarks>
    public int            Page       { get; set; }

    /// <summary>
    /// Gets or initializes the number of items displayed per page in a search or paging request.
    /// </summary
    public int            PageSize   { get; init; }

    /// <summary>
    /// Gets a value indicating whether soft-deleted records should be included in the search results.
    /// </summary>
    /// <remarks>
    /// This property determines if entities marked as soft-deleted should be included when fetching results.
    /// </remarks>
    public bool           IncludeDeletedSoft { get; init; }

    // ────────── helpers so ASP-NET can bind it from the URL ──────────
    /// <summary>
    /// Attempts to parse a string into a <see cref="SearchRequest"/> object.
    /// </summary>
    /// <param name="value">The string value to be parsed, commonly in query-string format.</param>
    /// <param name="_">An optional parameter for format provider, not used in this implementation.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="SearchRequest"/> object if the parsing was successful; otherwise, null.</param>
    /// <returns>
    /// True if the string was successfully parsed into a <see cref="SearchRequest"/> object; otherwise, false.
    /// </returns>
    public static bool TryParse(
        string? value,
        IFormatProvider? _,
        out SearchRequest? result)
    {
        result = ParseQueryString(value);
        return true;
    }

    /// <summary>
    /// Asynchronously binds and parses the query string from the HTTP request into a <see cref="SearchRequest"/> object.
    /// </summary>
    /// <param name="ctx">The HTTP context containing the request details.</param>
    /// <param name="_">The parameter information related to the binding, not used in this implementation.</param>
    /// <returns>A <see cref="ValueTask{SearchRequest}"/> that resolves to the parsed <see cref="SearchRequest"/> object or null if the binding fails.</returns>
    public static ValueTask<SearchRequest?> BindAsync(
        HttpContext ctx,
        ParameterInfo _)
    {
        Debug.Assert(ctx != null, nameof(ctx) + " != null");
        var req = ParseQueryString(ctx.Request.QueryString.Value);
        return ValueTask.FromResult<SearchRequest?>(req);
    }

    // ────────── robust parser (works for “”, “?Page=1”, or URL-encoded JSON) ──────────
    /// <summary>
    /// Parses the provided query string into a <see cref="SearchRequest"/> instance.
    /// Supports deserializing JSON or handling “key=value” style query strings, returning
    /// a default <see cref="SearchRequest"/> instance for invalid inputs.
    /// </summary>
    /// <param name="qs">The query string to parse, which may be URL-encoded or a JSON representation.</param>
    /// <returns>A <see cref="SearchRequest"/> instance representing the parsed query string. If the input is null, empty, or invalid, a default instance is returned.</returns
    private static SearchRequest ParseQueryString(string? qs)
    {
        if (string.IsNullOrWhiteSpace(qs))
            return new SearchRequest();

        if (qs![0] == '?')
            qs = qs[1..];

        var json = Uri.UnescapeDataString(qs);

        try
        {
            return JsonSerializer.Deserialize<SearchRequest>(json)
                   ?? new SearchRequest();
        }
        catch (JsonException)
        {
            // bad JSON or “key=value” style → default object instead of 500
            return new SearchRequest();
        }
    }

    // ────────── ‘legacy’ Parse helper kept for API parity ──────────
    /// <summary>
    /// Converts the specified string representation of a value to an object of the specified type, using a type converter.
    /// </summary>
    /// <param name="dataType">The type to which the value should be converted. If null, the method returns the input value.</param>
    /// <param name="value">The string representation of the value to convert. Can be null.</param>
    /// <returns>
    /// An object of the specified type if the conversion succeeds or the original value if the data type is null.
    /// Returns null if the input value is null.
    /// </returns>
    public static object? Parse(Type? dataType, string? value)
    {
        if (dataType == null)
            return value;

        var conv = TypeDescriptor.GetConverter(dataType);
        return value == null
            ? null
            : conv.ConvertFromString(null, CultureInfo.InvariantCulture, value);
    }
}
