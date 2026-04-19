using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.OpaqueService;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RecoveryCodesV1InitCommandHandler(
    OpaqueServer opaqueServer,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider)
    : ICommandHandler<RecoveryCodesV1InitCommand, RecoveryCodesV1InitCommandResult,
        OpaqueRecoveryCode, IOpaqueRecoveryCodeRepository>
{
    public async Task<SuccessOrFailure<RecoveryCodesV1InitCommandResult>> HandleAsync(
        RecoveryCodesV1InitCommand command, CancellationToken cancellationToken)
    {
        if (command.CodeIds.Length != 8 || command.BlindedElements.Length != 8)
            return SuccessOrFailureHelper<RecoveryCodesV1InitCommandResult>.Create(
                $"{nameof(RecoveryCodesV1InitCommand)} - Exactly 8 codes are required.");

        var blindedElementBytes = new byte[8][];
        for (var i = 0; i < 8; i++)
        {
            if (!opaqueServer.TryConvertToBlindedElement(command.BlindedElements[i], out blindedElementBytes[i], out var err))
                return SuccessOrFailureHelper<RecoveryCodesV1InitCommandResult>.Create(
                    $"{nameof(RecoveryCodesV1InitCommand)} - {err} at index {i}.");
        }

        var seeds = new byte[8][];
        var evaluatedElements = new byte[8][];
        for (var i = 0; i < 8; i++)
        {
            if (!opaqueServer.TryGenerateSeed(out seeds[i], out var err))
                return SuccessOrFailureHelper<RecoveryCodesV1InitCommandResult>.Create(
                    $"{nameof(RecoveryCodesV1InitCommand)} - {err}");

            if (!opaqueServer.TryBlindEvaluate(seeds[i], blindedElementBytes[i], out evaluatedElements[i], out err))
                return SuccessOrFailureHelper<RecoveryCodesV1InitCommandResult>.Create(
                    $"{nameof(RecoveryCodesV1InitCommand)} - {err}");
        }

        var batchId = guidProvider.NewId();
        var state = new RecoveryCodesBatchInitState
        {
            UserId = command.UserId,
            CodeIds = command.CodeIds,
            OprfSeeds = seeds
        };

        await transientStateCache.SetAsync($"rcbatch:{batchId}", state, TimeSpan.FromMinutes(5));

        return new SuccessOrFailure<RecoveryCodesV1InitCommandResult>(
            new RecoveryCodesV1InitCommandResult
            {
                BatchId = batchId,
                EvaluatedElements = evaluatedElements.Select(Convert.ToBase64String).ToArray()
            });
    }
}
