namespace SharedLibrary.Basics.TransientStateCache;

public interface ITransientStateCache
{
    Task SetAsync<T>(string key, T value, TimeSpan expiry);
    Task<T?> GetAsync<T>(string key);
    Task RemoveAsync(string key);
}
