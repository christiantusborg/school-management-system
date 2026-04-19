namespace QuVian.SharedLibrary.Basics.Repositories.Interfaces;

/// <summary>
/// Combines read and write repository operations.
/// </summary>
public interface IRootRepository<TEntity> :
    IRepositoryReader<TEntity>,
    IRepositoryWriter<TEntity>
    where TEntity : class, IEntity
{
}