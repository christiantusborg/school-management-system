using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using QuVian.SharedLibrary.Basics.SuccessOrFailures;

namespace QuVian.SharedLibrary.Basics.Dispatchers;

/// <summary>
/// Represents a handler for executing commands of type <typeparamref name="TCommand"/>,
/// producing results of type <typeparamref name="TResult"/> while managing entities of type <typeparamref name="TEntity"/>
/// through a repository of type <typeparamref name="TRepository"/>.
/// </summary>
/// <typeparam name="TCommand">
/// The type of the command being handled. Must implement <see cref="IRequest{TResult}"/>
/// where TResult is an instance of <see cref="SuccessOrFailure{TSuccess}"/>.
/// </typeparam>
/// <typeparam name="TResult">
/// The type of the result produced as an outcome of handling the command.
/// </typeparam>
/// <typeparam name="TEntity">
/// The type of the entity associated with operations within the repository.
/// This must inherit from <see cref="IEntity"/>.
/// </typeparam>
/// <typeparam name="TRepository">
/// The type of the repository utilized for handling operations related to <typeparamref name="TEntity"/>.
/// Must implement <see cref="IRootRepository{TEntity}"/>.
/// </typeparam>
public interface ICommandHandler<TCommand, TResult, TEntity, TRepository> :
    IRequestHandler<TCommand, SuccessOrFailure<TResult>>
    where TCommand : IRequest<SuccessOrFailure<TResult>>
    where TEntity : class, IEntity
    where TRepository : IRootRepository<TEntity>
{
}