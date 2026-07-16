namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// Stable Guids for the 5 IBSS-branded images that ship with the application
/// and are pre-seeded into the asset library. Lets the default letter
/// templates reference them by id without needing a name lookup.
/// </summary>
public static class SystemLetterAssetIds
{
    public static readonly Guid IbssLogo           = Guid.Parse("33333333-3333-3333-3333-100000000001");
    public static readonly Guid IbssSecondaryLogo  = Guid.Parse("33333333-3333-3333-3333-100000000002");
    public static readonly Guid IbssStamp          = Guid.Parse("33333333-3333-3333-3333-100000000003");
    public static readonly Guid IbssSignatureLine  = Guid.Parse("33333333-3333-3333-3333-100000000004");
    public static readonly Guid IbssFooter         = Guid.Parse("33333333-3333-3333-3333-100000000005");
    public static readonly Guid IbssCertificateBg  = Guid.Parse("33333333-3333-3333-3333-100000000006");
    public static readonly Guid StudentCardBg      = Guid.Parse("33333333-3333-3333-3333-100000000007");

    /// <summary>VIRTUAL asset: never seeded/stored. A layout image field with
    /// this id is filled at render time with the student's uploaded
    /// "Student Card Picture" document (photo on the digital student card).</summary>
    public static readonly Guid StudentPhoto       = Guid.Parse("33333333-3333-3333-3333-1000000000aa");

    public sealed record Seed(Guid Id, string Name, string MimeType, string ResourceFileName);

    public static readonly Seed[] All =
    {
        new(IbssLogo,           "ibss-logo",            "image/png",  "ibss-logo.png"),
        new(IbssSecondaryLogo,  "ibss-secondary-logo",  "image/png",  "ibss-secondary-logo.png"),
        new(IbssStamp,          "ibss-stamp",           "image/jpeg", "ibss-stamp.jpg"),
        new(IbssSignatureLine,  "ibss-signature-line",  "image/png",  "ibss-signature-line.png"),
        new(IbssFooter,         "ibss-footer",          "image/png",  "ibss-footer.png"),
        new(IbssCertificateBg,  "ibss-certificate-bg",  "image/png",  "ibss-certificate-bg.png"),
        new(StudentCardBg,      "student-card-bg",      "image/png",  "student-card-bg.png"),
    };
}
