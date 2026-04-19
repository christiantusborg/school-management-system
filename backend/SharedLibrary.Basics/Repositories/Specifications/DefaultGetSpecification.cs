using System.Linq.Expressions;
using QuVian.SharedLibrary.Basics.Filters;
using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace QuVian.SharedLibrary.Basics.Repositories.Specifications;

/// <summary>
///     Class DefaultGetSpecification.
///     Implements the <see cref="Specification{TEntity}" />
/// </summary>
/// <typeparam name="TEntity">Where TEntity is a Database Entity.</typeparam>
/// <seealso cref="Specification{TEntity}" />
[SuppressMessage("Design", "CA1002:Do not expose generic lists")]
public class DefaultGetSpecification<TEntity> : Specification<TEntity> where TEntity : class, IEntity
{
    // int

    /// <inheritdoc />
    public DefaultGetSpecification(int id,
        List<Expression<Func<TEntity, object>>>? includeExpressions,
        Expression<Func<TEntity, bool>>? whereExpression,
        bool includeDeleted = false,
        bool includePropertyName = true)
    {
        CreateGetSpecification(id, includeExpressions, whereExpression, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(int id, List<Expression<Func<TEntity, object>>>? includeExpressions,
        bool includeDeleted = false, bool includePropertyName = true)
    {
        if (includeExpressions is null)
        {
            includeExpressions = new List<Expression<Func<TEntity, object>>>();
        }

        CreateGetSpecification(id, includeExpressions, null, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(int id, Expression<Func<TEntity, bool>>? whereExpression,
        bool includeDeleted = false, bool includePropertyName = true)
    {
        CreateGetSpecification(id, null, whereExpression, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(int id, bool includeDeleted = false, bool includePropertyName = true)
    {
        CreateGetSpecification(id, null, null, includeDeleted, includePropertyName);
    }

    // string

    /// <inheritdoc />
    public DefaultGetSpecification(string id,
        List<Expression<Func<TEntity, object>>>? includeExpressions,
        Expression<Func<TEntity, bool>>? whereExpression,
        bool includeDeleted = false,
        bool includePropertyName = true)
    {
        CreateGetSpecification(id, includeExpressions, whereExpression, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(string id, List<Expression<Func<TEntity, object>>>? includeExpressions,
        bool includeDeleted = false, bool includePropertyName = true)
    {
        if (includeExpressions is null)
        {
            includeExpressions = new List<Expression<Func<TEntity, object>>>();
        }

        CreateGetSpecification(id, includeExpressions, null, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(string id, Expression<Func<TEntity, bool>>? whereExpression,
        bool includeDeleted = false, bool includePropertyName = true)
    {
        CreateGetSpecification(id, null, whereExpression, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(string id, bool includeDeleted = false, bool includePropertyName = true)
    {
        CreateGetSpecification(id, null, null, includeDeleted, includePropertyName);
    }

    // Guid

    /// <inheritdoc />
    public DefaultGetSpecification(Guid id,
        List<Expression<Func<TEntity, object>>>? includeExpressions,
        Expression<Func<TEntity, bool>>? whereExpression,
        bool includeDeleted = false,
        bool includePropertyName = true)
    {
        CreateGetSpecification(id, includeExpressions, whereExpression, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(Guid id, List<Expression<Func<TEntity, object>>>? includeExpressions,
        bool includeDeleted = false, bool includePropertyName = true)
    {
        if (includeExpressions is null)
        {
            includeExpressions = new List<Expression<Func<TEntity, object>>>();
        }

        CreateGetSpecification(id, includeExpressions, null, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(Guid id, Expression<Func<TEntity, bool>>? whereExpression,
        bool includeDeleted = false, bool includePropertyName = true)
    {
        CreateGetSpecification(id, null, whereExpression, includeDeleted, includePropertyName);
    }

    /// <inheritdoc />
    public DefaultGetSpecification(Guid id, bool includeDeleted = false, bool includePropertyName = true)
    {
        CreateGetSpecification(id, null, null, includeDeleted, includePropertyName);
    }

    /// <summary>
    /// Creates a specification for retrieving a single entity by its ID.
    /// </summary>
    /// <typeparam name="T">The type of the ID parameter.</typeparam>
    /// <param name="id">The ID of the entity to retrieve.</param>
    /// <param name="includeExpressions">An optional list of include expressions to include related entities.</param>
    /// <param name="whereExpression">An optional expression to filter the query results.</param>
    /// <param name="includeDeleted">Specifies whether to include deleted entities. Defaults to false.</param>
    /// <param name="includePropertyName">Specifies whether to include the property name in the filter. Defaults to true.</param>
    private void CreateGetSpecification<T>(T id,
        List<Expression<Func<TEntity, object>>>? includeExpressions,
        Expression<Func<TEntity, bool>>? whereExpression,
        bool includeDeleted = false,
        bool includePropertyName = true)
    {
        var filters = new List<Filter>
        {
            new()
            {
                Operation = Operation.Equals, Value = id, PropertyName = includePropertyName ? typeof(TEntity).Name + "Id" : "Id"
            }
        };

        // Check if TEntity has a property named "DeletedAt" and includeDeleted is false
        var propertyInfo = typeof(TEntity).GetProperty("DeletedAt");
        if (includeDeleted is false && propertyInfo != null)
        {
            filters.Add(new Filter
            {
                Operation = Operation.NotEqual, PropertyName = "DeletedAt", Value = null
            });
        }

        if (includeExpressions is not null)
        {
            foreach (var includeExpression in includeExpressions)
            {
                AddInclude(includeExpression);
            }
        }

        if (whereExpression is not null)
        {
            AddWhere(whereExpression);
        }

        AddFilters(filters);
        AddTake(1);
    }
}

