namespace QuVian.SharedLibrary.Basics.Repositories.Interfaces;

/// <summary>
/// Read-only operations for <typeparamref name="TEntity"/>.
/// </summary>
public interface IRepositoryReader<TEntity> where TEntity : class, IEntity
{
    Task<TEntity?> GetAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> SearchAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> specification);
}