using System.Security.Cryptography;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1VerifyInitCommandHandler(
    IUserContactEmailRepository repository,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider,
    IEmailSender emailSender)
    : ICommandHandler<EmailsV1VerifyInitCommand, EmailsV1VerifyInitCommandResult,
        UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<EmailsV1VerifyInitCommandResult>> HandleAsync(
        EmailsV1VerifyInitCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserContactEmailId == command.UserContactEmailId)
            .AddWhere(x => x.UserId == command.UserId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return SuccessOrFailureHelper<EmailsV1VerifyInitCommandResult>.EntityNotFound(
                typeof(EmailsV1VerifyInitCommand));

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var sessionId = guidProvider.NewId();

        await transientStateCache.SetAsync(
            $"email-verify:{sessionId}",
            new Verify.EmailVerifyState { UserContactEmailId = command.UserContactEmailId, UserId = command.UserId, Code = code },
            TimeSpan.FromMinutes(10));

        await emailSender.SendAsync(
            entity.Email,
            "Verify your email address",
            $"Your verification code is: <strong>{code}</strong>",
            cancellationToken);

        return new SuccessOrFailure<EmailsV1VerifyInitCommandResult>(
            new EmailsV1VerifyInitCommandResult { SessionId = sessionId });
    }
}
