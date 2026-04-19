using System.Diagnostics;
using QuVian.SharedLibrary.Basics.Filters;
using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace QuVian.SharedLibrary.Basics.Repositories.Specifications;

/// <summary>
/// Evaluates specifications to build LINQ queries translated to SQL by EF Core.
/// </summary>
public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(
        IQueryable<TEntity> inputQueryable,
        ISpecification<TEntity> specification)
        where TEntity : class, IEntity
    {
        Debug.Assert(specification != null, nameof(specification) + " != null");

        var queryable = inputQueryable;

        if (specification.Filters.Count > 0 || specification.FilterGroups.Count > 0)
        {
            var where = WhereBuilderExtension.GetExpression<TEntity>(specification.Filters, specification.FilterGroups);
            if (where is not null)
                queryable = queryable.Where(where);
        }

        if (specification.WhereExpression is not null)
            queryable = queryable.Where(specification.WhereExpression);

        queryable = specification.IncludeExpressions
            .Aggregate(queryable, (current, include) => current.Include(include));

        if (specification.OrderExpressions.Count > 0)
        {
            IOrderedQueryable<TEntity>? ordered = null;

            foreach (var (keySelector, direction) in specification.OrderExpressions)
            {
                ordered = (ordered, direction) switch
                {
                    (null, SortDirection.Ascending)    => queryable.OrderBy(keySelector),
                    (null, SortDirection.Descending)   => queryable.OrderByDescending(keySelector),
                    (_, SortDirection.Ascending)       => ordered!.ThenBy(keySelector),
                    _                                  => ordered!.ThenByDescending(keySelector)
                };
            }

            queryable = ordered ?? queryable;
        }

        if (specification.Skip is not null)
            queryable = queryable.Skip(specification.Skip.Value);

        if (specification.Take is > 0)
            queryable = queryable.Take(specification.Take.Value);

        return queryable;
    }
}
