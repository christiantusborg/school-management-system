using QuVian.CaseApi.Cases.V1.Create.Command;

namespace QuVian.CaseApi.Cases.V1.Create.Endpoint.Mappers;

public class CasesV1CreateEndpointRequestToCommandMapper
    : IMapper<CasesV1CreateEndpointRequest, CasesV1CreateCommand>
{
    public CasesV1CreateCommand MapFrom(CasesV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CasesV1CreateCommand
        {
            Name            = input.Name,
            Description     = input.Description,
            Priority        = input.Priority,
            DueDate         = input.DueDate,
            CreatedByUserId = string.Empty, // overwritten in endpoint
            LevelKeyPairs   = input.LevelKeyPairs.Select(lkp => new CaseLevelKeyCommand
            {
                Level                = lkp.Level,
                PublicKey            = Convert.FromBase64String(lkp.PublicKey),
                KemCiphertext        = Convert.FromBase64String(lkp.WrappedPrivateKey.KemCiphertext),
                EncryptedLevelPrivKey = Convert.FromBase64String(lkp.WrappedPrivateKey.EncryptedLevelPrivKey),
                Nonce                = Convert.FromBase64String(lkp.WrappedPrivateKey.Nonce)
            }).ToList()
        };
    }
}
