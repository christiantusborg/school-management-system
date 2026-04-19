using QuVian.CaseApi.CaseFiles.V1.Create.Command;

namespace QuVian.CaseApi.CaseFiles.V1.Create.Endpoint;

[Route("/v1/cases/{caseId:guid}/files")]
[EndpointTag("CaseFiles")]
public sealed class CaseFilesV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<CaseFilesV1CreateCommand, CaseFilesV1CreateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId, [FromBody] CaseFilesV1CreateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseFilesV1CreateCommandResult, CaseFilesV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseFilesV1CreateCommand
        {
            CaseId          = caseId,
            Name            = request.Name,
            ContentType     = request.ContentType,
            SizeBytes       = request.SizeBytes,
            StoragePath     = request.StoragePath,
            MinLevel        = request.MinLevel,
            AccessMode      = request.AccessMode,
            CreatedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            LevelKeys       = request.LevelKeys.Select(lk => new FileLevelKeyCommand
            {
                Level            = lk.Level,
                KemCiphertext    = Convert.FromBase64String(lk.KemCiphertext),
                EncryptedFileKey = Convert.FromBase64String(lk.EncryptedFileKey),
                Nonce            = Convert.FromBase64String(lk.Nonce)
            }).ToList()
        };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToCreatedResult(responseMapper);
    }
}
