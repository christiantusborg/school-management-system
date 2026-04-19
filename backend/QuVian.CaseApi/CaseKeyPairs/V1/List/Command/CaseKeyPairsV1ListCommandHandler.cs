namespace QuVian.CaseApi.CaseKeyPairs.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class CaseKeyPairsV1ListCommandHandler(
    ICaseKeyPairRepository keyPairRepository,
    IAccessLevelDefinitionRepository levelDefinitionRepository,
    ICaseLevelLabelOverrideRepository labelOverrideRepository,
    ILogger<CaseKeyPairsV1ListCommandHandler> logger)
    : ICommandHandler<CaseKeyPairsV1ListCommand, CaseKeyPairsV1ListCommandResult, CaseKeyPair, ICaseKeyPairRepository>
{
    public async Task<SuccessOrFailure<CaseKeyPairsV1ListCommandResult>> HandleAsync(
        CaseKeyPairsV1ListCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("[CaseKeyPairs:List] CaseId={CaseId}", command.CaseId);

        var keyPairSpec = new Specification<CaseKeyPair>()
            .AddWhere(x => x.CaseId == command.CaseId);
        var keyPairs = await keyPairRepository.SearchAsync(keyPairSpec, cancellationToken).ConfigureAwait(false);

        var levelDefs = await levelDefinitionRepository.SearchAsync(new Specification<AccessLevelDefinition>(), cancellationToken).ConfigureAwait(false);
        var levelNames = levelDefs.ToDictionary(d => d.Level, d => d.Name);

        var overrideSpec = new Specification<CaseLevelLabelOverride>()
            .AddWhere(x => x.CaseId == command.CaseId);
        var overrides = await labelOverrideRepository.SearchAsync(overrideSpec, cancellationToken).ConfigureAwait(false);

        var items = keyPairs.Select(kp => new CaseKeyPairItem
        {
            Level     = kp.Level,
            Name      = levelNames.GetValueOrDefault(kp.Level, $"Level {kp.Level}"),
            PublicKey = Convert.ToBase64String(kp.PublicKey)
        }).OrderBy(x => x.Level).ToList();

        var labelOverrideItems = overrides.Select(o => new LabelOverrideItem
        {
            Level = o.Level,
            Label = o.Label
        }).ToList();

        return new SuccessOrFailure<CaseKeyPairsV1ListCommandResult>(
            new CaseKeyPairsV1ListCommandResult { Items = items, LabelOverrides = labelOverrideItems });
    }
}
