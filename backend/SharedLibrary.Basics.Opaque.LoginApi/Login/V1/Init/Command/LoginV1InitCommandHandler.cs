using Microsoft.AspNetCore.Identity;
using Odin.Api.Base.Authentication;
using Odin.Api.Base.Crypto;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class LoginV1InitCommandHandler(
    UserManager<ApplicationUser> userManager,
    IOpaqueCredentialRepository opaqueCredentialRepository,
    OpaqueServer opaqueServer,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider)
    : ICommandHandler<LoginV1InitCommand, LoginV1InitCommandResult,
        OpaqueCredential, IOpaqueCredentialRepository>
{
    public async Task<SuccessOrFailure<LoginV1InitCommandResult>> HandleAsync(
        LoginV1InitCommand command, CancellationToken cancellationToken)
    {
        byte[]? blindedElement = null;
        try
        {
            blindedElement = Convert.FromBase64String(command.BlindedElement);
            if (blindedElement.Length != 32) blindedElement = null;
        }
        catch { /* leave null */ }

        if (blindedElement is null)
            return TimingSafeFakeResult(null);

        var user = await userManager.FindByNameAsync(command.Username);
        if (user is null || !user.IsEnabled || !user.EmailConfirmed || user.DeletedAt is not null)
            return TimingSafeFakeResult(blindedElement);

        var spec = new Specification<OpaqueCredential>()
            .AddWhere(x => x.UserId == user.Id);

        var credential = await opaqueCredentialRepository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (credential is null)
            return TimingSafeFakeResult(blindedElement);

        var evaluatedElement = opaqueServer.BlindEvaluate(credential.OprfSeed, blindedElement);
        var challenge = opaqueServer.GenerateChallenge();
        var loginId = guidProvider.NewId();

        var state = new OpaqueLoginInitState
        {
            UserId = user.Id,
            Challenge = challenge,
            ClientPublicKey = credential.ClientPublicKey,
            DeviceInfo = command.DeviceInfo
        };

        await transientStateCache.SetAsync($"login:{loginId:N}", state, TimeSpan.FromMinutes(2));

        return new SuccessOrFailure<LoginV1InitCommandResult>(new LoginV1InitCommandResult
        {
            LoginId = loginId.ToString("N"),
            EvaluatedElement = Convert.ToBase64String(evaluatedElement),
            Challenge = Convert.ToBase64String(challenge)
        });
    }

    private SuccessOrFailure<LoginV1InitCommandResult> TimingSafeFakeResult(byte[]? blindedElement)
    {
        // Produce a syntactically-valid Ristretto255 evaluated element so the client
        // doesn't get a decode error (which would distinguish unknown-username from
        // wrong-password). When the client's blinded element is itself malformed, we
        // can't compute a real point and fall back to random bytes — that path already
        // implies a broken request, and the user will only see the generic
        // "invalid credentials" error from the finish step.
        byte[] fakeEvaluated;
        try
        {
            if (blindedElement is null) throw new InvalidOperationException("no blinded element");
            var fakeSeed = opaqueServer.GenerateOprfSeed();
            fakeEvaluated = opaqueServer.BlindEvaluate(fakeSeed, blindedElement);
        }
        catch
        {
            fakeEvaluated = opaqueServer.GenerateChallenge();
        }

        var fakeChallenge = opaqueServer.GenerateChallenge();
        var fakeLoginId = guidProvider.NewId();

        return new SuccessOrFailure<LoginV1InitCommandResult>(new LoginV1InitCommandResult
        {
            LoginId = fakeLoginId.ToString("N"),
            EvaluatedElement = Convert.ToBase64String(fakeEvaluated),
            Challenge = Convert.ToBase64String(fakeChallenge)
        });
    }
}
