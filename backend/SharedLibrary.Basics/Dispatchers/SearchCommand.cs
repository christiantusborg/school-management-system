using QuVian.SharedLibrary.Basics.Filters;

namespace QuVian.SharedLibrary.Basics.Dispatchers;

public class SearchCommand
{
    public List<Filter>? Filters { get; set; }
    public List<Sorting>? SortField { get; set; }
    public bool IncludeDeleted { get; set; }
    public int Page { get; set; }
    public int PageLength { get; set; }
    public int Skip => (Page - 1) * PageLength;
    public int Take => PageLength;
}