namespace SharedLibrary.Basics.TransientStateCache;

public interface ITransientStateCacheFactory
{
    ITransientStateCache GetCache();
}
