using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.AuditLogs.Models;
using QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.AuditLogs.Repositories;
using QuVian.SharedLibrary.Basics.Extensions.ClaimsPrincipalExtensions;
using QuVian.SharedLibrary.Basics.Extensions.StringExtensions;
using QuVian.SharedLibrary.Basics.Extensions.StringExtensions.HybridCryptography;
using QuVian.SharedLibrary.Basics.MockProviders;

namespace QuVian.SharedLibrary.Basics.Dispatchers.Pipelines.AuditLogs;

public sealed class AuditLogPipelineBehaviour<TRequest, TResponse>(
    IAuditLogsRepository auditLogsRepository,
    IDateTimeProvider dateTimeProvider,
    IGuidProvider guidProvider,
    IHybridStringCryptography hybridStringCryptography,
    IHttpContextAccessor httpContextAccessor)
    : Basics.Dispatchers.IPipelineBehavior<TRequest, TResponse>
    where TRequest : Basics.Dispatchers.IRequest<TResponse>
{

    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken,
        Func<Task<TResponse>> next)
    {
        var claimsPrincipal = httpContextAccessor.HttpContext?.User;
        var userId = "System".AsDeterministicGuid();

        var claimsPrincipalUserId = claimsPrincipal!.GetUserId();

        if (claimsPrincipalUserId != Guid.Empty)
        {
            userId = claimsPrincipalUserId;
        }

        var response = await next().ConfigureAwait(false);

        // Get the 'success' field (assuming it's a private field in the response object)
        var successField = response!.GetType().GetField("success", BindingFlags.NonPublic | BindingFlags.Instance);

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

        var serializedCommand = JsonSerializer.Serialize(request);
        var serializedCommandEncrypted =
            await hybridStringCryptography.HybridEncryptAsync(serializedCommand,
                nameof(AuditLog) + "Command");

        var serializedResult = JsonSerializer.Serialize(successValue);
        var serializedResultEncrypted =
            await hybridStringCryptography.HybridEncryptAsync(serializedResult,
                nameof(AuditLog) + "Result");

        var outbox = new AuditLog
        {
            AuditLogId = guidProvider.NewId(),
            Command = typeof(TRequest).Name,
            CommandData = serializedCommandEncrypted,
            ResultData = serializedResultEncrypted,
            UserId = userId,
            CreatedAt = dateTimeProvider.UtcNow,
            TraceIdentifier = httpContextAccessor.HttpContext?.TraceIdentifier ?? string.Empty,
        };

        auditLogsRepository.Add(outbox);

        return response;
    }
}