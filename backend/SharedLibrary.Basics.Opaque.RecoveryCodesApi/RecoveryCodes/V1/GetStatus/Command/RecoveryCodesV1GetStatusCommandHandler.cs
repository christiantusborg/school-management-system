using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using QuVian.SharedLibrary.Basics.Repositories.Specifications;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RecoveryCodesV1GetStatusCommandHandler(IOpaqueRecoveryCodeRepository repository)
    : ICommandHandler<RecoveryCodesV1GetStatusCommand, RecoveryCodesV1GetStatusCommandResult,
        OpaqueRecoveryCode, IOpaqueRecoveryCodeRepository>
{
    public async Task<SuccessOrFailure<RecoveryCodesV1GetStatusCommandResult>> HandleAsync(
        RecoveryCodesV1GetStatusCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<OpaqueRecoveryCode>()
            .AddWhere(x => x.UserId == command.UserId);
        var count = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new SuccessOrFailure<RecoveryCodesV1GetStatusCommandResult>(
            new RecoveryCodesV1GetStatusCommandResult { RemainingCount = (int)count });
    }
}
