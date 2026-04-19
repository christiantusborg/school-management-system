namespace QuVian.SharedLibrary.Basics.Repositories.Interfaces;

/// <summary>
/// Write-side operations for <typeparamref name="TEntity"/>.
/// </summary>
public interface IRepositoryWriter<TEntity> where TEntity : class, IEntity
{
    void Add(TEntity entity);
    void Remove(TEntity entity);
    void RemoveRange(List<TEntity> entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}