namespace Odin.Api.Base.Cache;

public interface ITransientStateCacheFactory
{
    ITransientStateCache GetCache();
}
