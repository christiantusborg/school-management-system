namespace QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.TransactionPipelineBehaviours;

/// <summary>
/// Represents a pipeline behavior that manages transactional support in a pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of request handled by this pipeline behavior.</typeparam>
/// <typeparam name="TResult">The type of result returned by the request handler.</typeparam>
/// <remarks>
/// This class ensures that all operations within the pipeline are executed within a database transaction.
/// If the execution completes successfully, the transaction is committed. If an error occurs, the transaction is rolled back.
/// </remarks>
/// QuVian.CaseApi

public sealed class TransactionPipelineBehaviour<TRequest, TResult>(DbContext dbContext)
    : IPipelineBehavior<TRequest, TResult>
{
    public async Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<Task<TResult>> next)    {
        using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                // Call the next delegate in the pipeline
                var response = await next().ConfigureAwait(false);


                await dbContext.SaveChangesAsync(cancellationToken);
                // Commit the transaction
                await transaction.CommitAsync(cancellationToken);

                return response;
            }
            catch
            {
                // Rollback the transaction if there's an error
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}