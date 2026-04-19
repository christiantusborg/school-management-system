using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;
using QuVian.SharedLibrary.Basics.Repositories.Specifications;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.OpaqueService;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RecoveryCodesV1FinalizeCommandHandler(
    ITransientStateCache transientStateCache,
    OpaqueServer opaqueServer,
    IOpaqueRecoveryCodeRepository repository)
    : ICommandHandler<RecoveryCodesV1FinalizeCommand, RecoveryCodesV1FinalizeCommandResult,
        OpaqueRecoveryCode, IOpaqueRecoveryCodeRepository>
{
    public async Task<SuccessOrFailure<RecoveryCodesV1FinalizeCommandResult>> HandleAsync(
        RecoveryCodesV1FinalizeCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"rcbatch:{command.BatchId}";
        var state = await transientStateCache.GetAsync<RecoveryCodesBatchInitState>(cacheKey);
        if (state is null)
            return SuccessOrFailureHelper<RecoveryCodesV1FinalizeCommandResult>.EntityNotFound(
                typeof(RecoveryCodesV1FinalizeCommand));

        await transientStateCache.RemoveAsync(cacheKey);

        if (command.ClientPublicKeys.Length != 8 || command.EncryptedPrivateKeys.Length != 8 || command.Nonces.Length != 8)
            return SuccessOrFailureHelper<RecoveryCodesV1FinalizeCommandResult>.Create(
                $"{nameof(RecoveryCodesV1FinalizeCommand)} - Exactly 8 codes are required.");

        var clientPublicKeys = new byte[8][];
        var encryptedPrivateKeys = new byte[8][];
        var nonces = new byte[8][];

        for (var i = 0; i < 8; i++)
        {
            if (!opaqueServer.TryConvertToClientPublicKey(command.ClientPublicKeys[i], out clientPublicKeys[i], out var err))
                return SuccessOrFailureHelper<RecoveryCodesV1FinalizeCommandResult>.Create(
                    $"{nameof(RecoveryCodesV1FinalizeCommand)} - {err} at index {i}.");

            try
            {
                encryptedPrivateKeys[i] = Convert.FromBase64String(command.EncryptedPrivateKeys[i]);
                if (encryptedPrivateKeys[i].Length == 0)
                    return SuccessOrFailureHelper<RecoveryCodesV1FinalizeCommandResult>.Create(
                        $"{nameof(RecoveryCodesV1FinalizeCommand)} - Empty encrypted private key at index {i}.");
            }
            catch
            {
                return SuccessOrFailureHelper<RecoveryCodesV1FinalizeCommandResult>.Create(
                    $"{nameof(RecoveryCodesV1FinalizeCommand)} - Invalid encrypted private key encoding at index {i}.");
            }

            try
            {
                nonces[i] = Convert.FromBase64String(command.Nonces[i]);
                if (nonces[i].Length != 12)
                    return SuccessOrFailureHelper<RecoveryCodesV1FinalizeCommandResult>.Create(
                        $"{nameof(RecoveryCodesV1FinalizeCommand)} - Invalid nonce at index {i}.");
            }
            catch
            {
                return SuccessOrFailureHelper<RecoveryCodesV1FinalizeCommandResult>.Create(
                    $"{nameof(RecoveryCodesV1FinalizeCommand)} - Invalid nonce encoding at index {i}.");
            }
        }

        var existingSpec = new Specification<OpaqueRecoveryCode>()
            .AddWhere(x => x.UserId == state.UserId);
        var existing = await repository.SearchAsync(existingSpec, cancellationToken).ConfigureAwait(false);
        repository.RemoveRange(existing);

        for (var i = 0; i < 8; i++)
        {
            repository.Add(new OpaqueRecoveryCode
            {
                UserId = state.UserId,
                CodeId = state.CodeIds[i],
                OprfSeed = state.OprfSeeds[i],
                ClientPublicKey = clientPublicKeys[i],
                EncryptedPrivateKey = encryptedPrivateKeys[i],
                Nonce = nonces[i]
            });
        }


        return new SuccessOrFailure<RecoveryCodesV1FinalizeCommandResult>(
            new RecoveryCodesV1FinalizeCommandResult { BatchId = command.BatchId });
    }
}
