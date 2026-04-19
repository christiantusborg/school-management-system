namespace QuVian.SharedLibrary.Basics.Dispatchers;

public class CommandSearchResult<T> : IMessageQueue where T : IMessageQueue

{
    public required int Total { get; set; }
    public required IList<T> Items { get; init; }
}