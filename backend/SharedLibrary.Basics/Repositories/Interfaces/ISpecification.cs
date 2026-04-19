using System.Linq.Expressions;
using QuVian.SharedLibrary.Basics.Filters;

namespace QuVian.SharedLibrary.Basics.Repositories.Interfaces;

public interface ISpecification<TEntity> where TEntity : IEntity
{
    IReadOnlyList<Filter> Filters { get; }

    IReadOnlyList<FilterGroup> FilterGroups { get; }

    IReadOnlyList<Expression<Func<TEntity, object>>> IncludeExpressions { get; }

    IReadOnlyList<(Expression<Func<TEntity, object>> KeySelector, SortDirection Direction)> OrderExpressions { get; }

    Expression<Func<TEntity, bool>>? WhereExpression { get; }

    int? Skip { get; }

    int? Take { get; }
}
