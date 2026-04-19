using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using QuVian.SharedLibrary.Basics.Repositories.Specifications;

namespace QuVian.SharedLibrary.Basics.Repositories;

/// <summary>
/// Entity Framework Core implementation of the generic repository.
/// </summary>
public class EntityFrameworkCoreRepository<TEntity> : IRootRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly DbContext _dbContext;

    public EntityFrameworkCoreRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TEntity?> GetAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TEntity?> FirstOrDefaultAsync(IQueryable<TEntity> queryable,
        CancellationToken cancellationToken = default)
    {
        return await queryable.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<List<TEntity>> SearchAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public void Add(TEntity entity)
    {
        _dbContext.Set<TEntity>().Add(entity);
    }

    public void Remove(TEntity entity)
    {
        _dbContext.Set<TEntity>().Remove(entity);
    }

    public void RemoveRange(List<TEntity> entity)
    {
        _dbContext.Set<TEntity>().RemoveRange(entity);
    }

    public async Task<int> CountAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> specification)
    {
        return SpecificationEvaluator.GetQuery(_dbContext.Set<TEntity>(), specification);
    }
}
