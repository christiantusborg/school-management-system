using System.Diagnostics;
using System.Text.Json;
using QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.OutboxingCommandResults.Models;
using QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.OutboxingCommandResults.Repositories;
using QuVian.SharedLibrary.Basics.Extensions.StringExtensions.HybridCryptography;
using QuVian.SharedLibrary.Basics.MockProviders;

namespace QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.OutboxingCommandResults;

/// <summary>
///     Class PermissionBehaviour. This class cannot be inherited.
///     Implements the <see cref="IPipelineBehavior{TRequest,TResult}" />.
/// </summary>
/// <typeparam name="TRequest">The type of the t request.</typeparam>
/// <typeparam name="TResponse">The type of the t response.</typeparam>
/// <seealso cref="IPipelineBehavior{TRequest, TResponse}" />
public sealed class
    OutboxingCommandResultPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IOutboxingCommandResultsRepository _outboxingCommandResultsRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGuidProvider _guidProvider;
    private readonly IHybridStringCryptography _hybridStringCryptography;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxingCommandResultPipelineBehaviour{TRequest,TResponse}" /> class.
    /// </summary>
    /// <param name="outboxingCommandResultsRepository"></param>
    /// <param name="dateTimeProvider"></param>
    /// <param name="guidProvider"></param>
    /// <param name="asymmetricStringCryptography"></param>
    public OutboxingCommandResultPipelineBehaviour(IOutboxingCommandResultsRepository outboxingCommandResultsRepository,
        IDateTimeProvider dateTimeProvider, IGuidProvider guidProvider, IHybridStringCryptography hybridStringCryptography)
    {
        _outboxingCommandResultsRepository = outboxingCommandResultsRepository;
        _dateTimeProvider = dateTimeProvider;
        _guidProvider = guidProvider;
        _hybridStringCryptography = hybridStringCryptography;
    }




    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>> next)
    {
        var response = await next().ConfigureAwait(false);

        Debug.Assert(response != null, nameof(response) + " != null");

        // Get the inner type from the generic type of the response
        var innerType = response.GetType().GetGenericArguments()[0];

        // Get interfaces that are assignable from IMessageQueue excluding IMessageQueue itself
        var messageQueueInterfaces = innerType.GetInterfaces()
            .Where(i => typeof(IMessageQueue).IsAssignableFrom(i) && i != typeof(IMessageQueue)).ToList();

        // Exit if no valid interfaces are found
        if (messageQueueInterfaces.Count == 0)
        {
            return response;
        }

        // Get the 'success' field (assuming it's a private field in the response object)
        var successField = response.GetType().GetField("success", BindingFlags.NonPublic | BindingFlags.Instance);

        // Ensure the field exists and has a value
        if (successField == null)
        {
            return response;
        }

        var successValue = successField.GetValue(response); // Get the actual value of the 'success' field

        if (successValue == null)
        {
            return response; // Exit if successValue is null
        }

        foreach (var messageQueueInterface in messageQueueInterfaces)
        {
            var serializedValue = JsonSerializer.Serialize(successValue, messageQueueInterface);

            var serializedValueEncrypted = await _hybridStringCryptography.HybridEncryptAsync(serializedValue, messageQueueInterface);

            var outbox = new OutboxingCommandResult
            {
                OutboxingId = _guidProvider.NewId(),
                Interface = messageQueueInterface.Name!,
                InterfaceResult = serializedValueEncrypted,
                CreatedAt = _dateTimeProvider.UtcNow,
            };

            _outboxingCommandResultsRepository.Add(outbox);
        }

        return response;
    }
}