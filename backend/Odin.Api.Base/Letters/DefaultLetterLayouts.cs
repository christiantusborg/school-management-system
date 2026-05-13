using System.Text.Json;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace Odin.Api.Base.Letters;

/// <summary>
/// Builds the canonical IBSS Konva layouts for the three text-heavy letters
/// (Offer / Admission / Transcript). The seeder writes these into
/// <c>LetterTemplate.CertificateLayoutJson</c> for every programme that
/// doesn't already have a layout, preserving admin edits.
///
/// Coordinates are in PDF points (1 design unit = 1 point) on an A4 portrait
/// canvas (595 × 842). Body text 10pt, headings 14–18pt. Most prose blocks
/// use width-constrained text fields so QuestPDF wraps them inside the box.
/// </summary>
internal static class DefaultLetterLayouts
{
    private const int PageW = 595;
    private const int PageH = 842;
    private const int LeftX = 50;          // margin
    private const int RightX = 545;        // page - margin
    private const int ContentW = 495;      // RightX - LeftX
    private const int LineH = 14;          // body line height (10pt font + leading)
    private const int ParaGap = 6;         // gap between blocks

    private static int NextId;
    private static string Id(string prefix) => $"{prefix}-{System.Threading.Interlocked.Increment(ref NextId):x}";

    /// <summary>
    /// Bumped whenever an existing seeded layout's structure changes in a way
    /// that warrants overwriting previously-seeded rows. The seeder uses this
    /// fingerprint to decide whether to refresh templates that haven't been
    /// edited by an admin.
    /// </summary>
    public const int CurrentTranscriptFingerprint = 2;

    public static string OfferLetterJson()    => Serialize(BuildOffer());
    public static string AdmissionLetterJson() => Serialize(BuildAdmission());
    public static string TranscriptJson()     => Serialize(BuildTranscript(CurrentTranscriptFingerprint));

    private static string Serialize(CertificateLayout layout)
        => JsonSerializer.Serialize(layout, CertificateLayout.JsonOpts);

    // ── Field constructors ────────────────────────────────────────────────

    private static CertificateField Body(int x, int y, int w, string? text = null, string? tag = null,
        bool bold = false, bool italic = false, int fontSize = 10, string align = "left",
        string? prefix = null, string? suffix = null)
        => new()
        {
            Id = Id("t"), Kind = "text",
            Tag = tag, Text = text, Prefix = prefix, Suffix = suffix,
            X = x, Y = y, Width = w, FontSize = fontSize,
            Color = "#000000", Align = align, Bold = bold, Italic = italic,
        };

    private static CertificateField ImageField(Guid assetId, int x, int y, int w, int h)
        => new()
        {
            Id = Id("i"), Kind = "image",
            ImageAssetId = assetId,
            X = x, Y = y, Width = w, Height = h,
        };

    private static CertificateLayout NewLayout() => new()
    {
        Width = PageW, Height = PageH,
        PageSize = "A4", Orientation = "portrait",
        MarginTop = 30, MarginRight = 30, MarginBottom = 30, MarginLeft = 30,
    };

    // ── Offer Letter (2 pages) ────────────────────────────────────────────

    private static CertificateLayout BuildOffer()
    {
        var layout = NewLayout();
        layout.Pages = new() { OfferPage1(), OfferPage2() };
        return layout;
    }

    private static CertificatePage OfferPage1() => new()
    {
        Fields = new()
        {
            ImageField(SystemLetterAssetIds.IbssLogo,           LeftX, 30, 200, 60),
            ImageField(SystemLetterAssetIds.IbssSecondaryLogo,  RightX - 70, 30, 70, 70),

            Body(LeftX, 110, ContentW, "Offer Letter", bold: true, fontSize: 18, align: "center"),

            Body(LeftX, 145, ContentW, prefix: "Date: ",  tag: "[date]"),
            Body(LeftX, 145 + LineH,     ContentW, text:   "Ref: "),
            Body(LeftX, 175 + LineH,     ContentW, prefix: "Name: ",             tag: "[student full name]", bold: true),
            Body(LeftX, 175 + LineH * 2, ContentW, prefix: "Passport/ID No.: ",  tag: "[passport id]",       bold: true),
            Body(LeftX, 175 + LineH * 3, ContentW, prefix: "Address: ",          tag: "[student address]",   bold: true),

            Body(LeftX, 245, ContentW, prefix: "Dear ", suffix: ",", tag: "[student full name]"),
            Body(LeftX, 270, ContentW,
                "Congratulations. We are pleased to inform you that your application for International Business " +
                "School of Scandinavia (IBSS) is approved. We look forward to having you with us. Our records " +
                "for your admission will carry the following information:"),

            // Programme details (Y stacked manually because the wrapped welcome paragraph above is ~3 lines).
            Body(LeftX,         335, 160, "Programme:",            bold: true),
            Body(LeftX + 165,   335, ContentW - 165, tag: "[program name]"),
            Body(LeftX,         335 + LineH,     160, "Specialization in:",   bold: true),
            Body(LeftX + 165,   335 + LineH,     ContentW - 165, tag: "[specialization name]"),
            Body(LeftX,         335 + LineH * 2, 160, "Commencement date:",   bold: true),
            Body(LeftX + 165,   335 + LineH * 2, ContentW - 165, tag: "[commencement date]"),
            Body(LeftX,         335 + LineH * 3, 160, "Expected completion date:", bold: true),
            Body(LeftX + 165,   335 + LineH * 3, ContentW - 165, tag: "[completion date]"),
            Body(LeftX,         335 + LineH * 4, 160, "Duration of study:",   bold: true),
            Body(LeftX + 165,   335 + LineH * 4, ContentW - 165, tag: "[duration of study]"),
            Body(LeftX,         335 + LineH * 5, 160, "Learning center:",     bold: true),
            Body(LeftX + 165,   335 + LineH * 5, ContentW - 165, tag: "[partner name]"),
            Body(LeftX,         335 + LineH * 6, 160, "Mode of study:",       bold: true),
            Body(LeftX + 165,   335 + LineH * 6, ContentW - 165, tag: "[mode of study]"),
            Body(LeftX,         335 + LineH * 7, 160, "Instruction language:", bold: true),
            Body(LeftX + 165,   335 + LineH * 7, ContentW - 165, tag: "[instruction language]"),

            // Numbered terms (compressed for page 1; remaining go on page 2).
            Body(LeftX, 470, ContentW,
                "1. If you choose to accept/decline our offer, kindly respond by filling out the attached reply " +
                "form within five (5) days of the date of this letter. We cannot guarantee a place in the " +
                "programme and this offer may then be withdrawn if we do not receive any feedback within the " +
                "stipulated time."),
            Body(LeftX, 540, ContentW,
                "2. In the event that any information you had provided earlier is inaccurate or false, this " +
                "offer of admission is considered null and void."),
            Body(LeftX, 580, ContentW,
                prefix: "3. Upon acceptance to our offer, you are required to make the necessary payment to our partner ",
                tag: "[partner name]",
                suffix: "."),
            Body(LeftX, 615, ContentW,
                prefix: "4. Any refund after or before the class starts will be requested to our partner ",
                tag: "[partner name]",
                suffix: "."),
            Body(LeftX, 650, ContentW,
                prefix: "5. The duration of study is a maximum of ",
                tag: "[duration of study]",
                suffix: ". Should you exceed this study period, you will be charged a penalty fee."),
            Body(LeftX, 695, ContentW,
                "6. The tuition fee is not covering the supervisor fee for the final project / dissertation " +
                "project. Supervisor is not mandatory while doing final project / dissertation project. If the " +
                "student wishes to have a supervisor from the school, please contact the school's registrar to " +
                "have the updated supervisor fee."),

            ImageField(SystemLetterAssetIds.IbssFooter, LeftX, 780, ContentW, 50),
        },
    };

    private static CertificatePage OfferPage2() => new()
    {
        Fields = new()
        {
            ImageField(SystemLetterAssetIds.IbssLogo, LeftX, 30, 200, 60),

            Body(LeftX, 110, ContentW,
                "International Business School of Scandinavia (IBSS) would like to congratulate you to join the " +
                "programme in your quest towards academic and career advancement.", bold: true),
            Body(LeftX, 160, ContentW, "We wish you every success!", italic: true),

            ImageField(SystemLetterAssetIds.IbssStamp,         LeftX,        200, 110, 110),
            ImageField(SystemLetterAssetIds.IbssSignatureLine, LeftX + 280,  280, 200, 4),
            Body(LeftX + 280, 290, 215, "Anna Phan",            bold: true),
            Body(LeftX + 280, 310, 215, "Head of Administration"),

            Body(LeftX, 370, ContentW, "(Please fill up this part)",       italic: true, fontSize: 11),
            Body(LeftX, 395, ContentW, "Applicant's Confirmation",          bold: true,  fontSize: 14),
            Body(LeftX, 425, ContentW,
                prefix: "By paying the tuition fee of the program, I, ",
                tag: "[student full name]",
                suffix: ", accept the offer to study the programme above in International Business School of " +
                        "Scandinavia (IBSS). I hereby acknowledge that I have read and understand the terms and " +
                        "conditions of this offer letter and on the website (https://ibss.edu.eu/)."),

            Body(LeftX, 530, 200, "Signature: __________________________"),
            Body(LeftX, 560, 200, "Date:      __________________________"),

            ImageField(SystemLetterAssetIds.IbssFooter, LeftX, 780, ContentW, 50),
        },
    };

    // ── Admission Letter (1 page) ─────────────────────────────────────────

    private static CertificateLayout BuildAdmission()
    {
        var layout = NewLayout();
        layout.Pages = new() { AdmissionPage1() };
        return layout;
    }

    private static CertificatePage AdmissionPage1() => new()
    {
        Fields = new()
        {
            ImageField(SystemLetterAssetIds.IbssLogo,          LeftX,        30, 200, 60),
            ImageField(SystemLetterAssetIds.IbssSecondaryLogo, RightX - 70,  30,  70, 70),

            Body(LeftX, 110, ContentW, "Admission Letter", bold: true, fontSize: 18, align: "center"),

            Body(LeftX, 145, ContentW, prefix: "Date: ", tag: "[date]"),
            Body(LeftX, 145 + LineH,     ContentW, text:   "Ref: "),
            Body(LeftX, 175 + LineH,     ContentW, prefix: "Name: ",            tag: "[student full name]", bold: true),
            Body(LeftX, 175 + LineH * 2, ContentW, prefix: "Passport/ID No.: ", tag: "[passport id]",       bold: true),
            Body(LeftX, 175 + LineH * 3, ContentW, prefix: "Address: ",         tag: "[student address]",   bold: true),

            Body(LeftX, 245, ContentW, prefix: "Dear ", suffix: ",", tag: "[student full name]"),

            Body(LeftX, 270, ContentW,
                "International Business School of Scandinavia (IBSS) would like to take this opportunity to " +
                "congratulate and welcome you to the programme in your quest towards academic and career " +
                "advancement. It is our pleasure that you have been accepted into the programme."),

            Body(LeftX,       340, 160, "Programme:",            bold: true),
            Body(LeftX + 165, 340, ContentW - 165, tag: "[program name]"),
            Body(LeftX,       340 + LineH,     160, "Specialization in:",   bold: true),
            Body(LeftX + 165, 340 + LineH,     ContentW - 165, tag: "[specialization name]"),
            Body(LeftX,       340 + LineH * 2, 160, "Student ID:",          bold: true),
            Body(LeftX + 165, 340 + LineH * 2, ContentW - 165, tag: "[student number]"),
            Body(LeftX,       340 + LineH * 3, 160, "Commencement date:",   bold: true),
            Body(LeftX + 165, 340 + LineH * 3, ContentW - 165, tag: "[commencement date]"),
            Body(LeftX,       340 + LineH * 4, 160, "Duration of study:",   bold: true),
            Body(LeftX + 165, 340 + LineH * 4, ContentW - 165, tag: "[duration of study]"),
            Body(LeftX,       340 + LineH * 5, 160, "Learning center:",     bold: true),
            Body(LeftX + 165, 340 + LineH * 5, ContentW - 165, tag: "[partner name]"),
            Body(LeftX,       340 + LineH * 6, 160, "Mode of study:",       bold: true),
            Body(LeftX + 165, 340 + LineH * 6, ContentW - 165, tag: "[mode of study]"),
            Body(LeftX,       340 + LineH * 7, 160, "Instruction language:", bold: true),
            Body(LeftX + 165, 340 + LineH * 7, ContentW - 165, tag: "[instruction language]"),

            Body(LeftX, 460, ContentW,
                "We hereby confirm to register you as our active student for our programme as mentioned above."),
            Body(LeftX, 490, ContentW,
                "Participation in this programme is governed by IBSS Terms & Conditions (see https://ibss.edu.eu/)."),

            Body(LeftX, 545, ContentW, "Thank you,"),
            Body(LeftX, 565, ContentW, "Yours sincerely,"),

            ImageField(SystemLetterAssetIds.IbssStamp,         RightX - 110, 600, 100, 100),
            ImageField(SystemLetterAssetIds.IbssSignatureLine, LeftX,        680, 200, 4),
            Body(LeftX, 690, 215, "Anna Phan",            bold: true),
            Body(LeftX, 710, 215, "Head of Administration"),

            ImageField(SystemLetterAssetIds.IbssFooter, LeftX, 780, ContentW, 50),
        },
    };

    // ── Transcript (2 pages) ──────────────────────────────────────────────

    private static CertificateLayout BuildTranscript(int seedFingerprint)
    {
        var layout = NewLayout();
        layout.SeedFingerprint = seedFingerprint;
        layout.Pages = new() { TranscriptPage1(), TranscriptPage2() };
        return layout;
    }

    private static IEnumerable<CertificateField> IbssHeader()
    {
        // Logo on the left, multi-line IBSS contact info on the right, with
        // a thin horizontal divider underneath. Mirrors the official IBSS
        // letterhead exactly.
        yield return ImageField(SystemLetterAssetIds.IbssLogo, LeftX, 25, 110, 90);
        const int rx = LeftX + 130;
        const int rw = 365;
        yield return Body(rx, 30,  rw, "International Business School Of Scandinavia (IBSS)", bold: true, fontSize: 12);
        yield return Body(rx, 46,  rw, "Part of MY GLOBAL WORLD EDUCATION", fontSize: 10);
        yield return Body(rx, 64,  rw, "Trindsøvej 6, 1. Sal, 8000 Aarhus Centrum, Denmark", fontSize: 9);
        yield return Body(rx, 78,  rw, "+45 78 88 89 12    admission@mgworld.education",     fontSize: 9);
        yield return Body(rx, 92,  rw, "IBSSEDUCATION    www.ibss.edu.eu",                   fontSize: 9);
    }

    /// <summary>Thin horizontal rule rendered as a flat 2pt-tall image asset.</summary>
    private static CertificateField HorizontalRule(int y) =>
        ImageField(SystemLetterAssetIds.IbssSignatureLine, LeftX, y, ContentW, 2);

    private static CertificatePage TranscriptPage1()
    {
        var fields = new List<CertificateField>();
        fields.AddRange(IbssHeader());
        fields.Add(HorizontalRule(125));

        // Title block
        fields.Add(Body(LeftX, 145, ContentW, "STUDENT TRANSCRIPT", bold: true, fontSize: 16, align: "center"));
        fields.Add(Body(LeftX, 165, ContentW, "Official Transcript", bold: true, fontSize: 12, align: "center"));
        fields.Add(Body(LeftX, 182, ContentW, prefix: "Date of issuance: ", tag: "[date]", fontSize: 9, align: "center"));

        // Two-column student info header (4 rows × 2 cols)
        const int row1Y = 210;
        const int rowH = 28;
        const int leftLabelW = 110;
        const int leftValueW = 170;
        const int gap = 25;
        const int leftValueX = LeftX + leftLabelW + 5;
        const int rightLabelX = leftValueX + leftValueW + gap;
        const int rightLabelW = 130;
        const int rightValueX = rightLabelX + rightLabelW + 5;
        const int rightValueW = 95;

        fields.Add(Body(LeftX,       row1Y,            leftLabelW,  "Student's Name"));
        fields.Add(Body(leftValueX,  row1Y,            leftValueW,  prefix: ":  ", tag: "[student full name]"));
        fields.Add(Body(rightLabelX, row1Y,            rightLabelW, "Language of Instruction"));
        fields.Add(Body(rightValueX, row1Y,            rightValueW, prefix: ":  ", tag: "[instruction language]"));

        fields.Add(Body(LeftX,       row1Y + rowH,     leftLabelW,  "Student's ID Number"));
        fields.Add(Body(leftValueX,  row1Y + rowH,     leftValueW,  prefix: ":  ", tag: "[student number]"));
        fields.Add(Body(rightLabelX, row1Y + rowH,     rightLabelW, "DOB"));
        fields.Add(Body(rightValueX, row1Y + rowH,     rightValueW, prefix: ":  ", tag: "[date of birth]"));

        fields.Add(Body(LeftX,       row1Y + rowH * 2, leftLabelW,  "Program of Study"));
        fields.Add(Body(leftValueX,  row1Y + rowH * 2, leftValueW,  prefix: ":  ", tag: "[program name]"));
        fields.Add(Body(rightLabelX, row1Y + rowH * 2, rightLabelW, "ECTS Achieved"));
        fields.Add(Body(rightValueX, row1Y + rowH * 2, rightValueW, prefix: ":  ", tag: "[ects achieved]"));

        fields.Add(Body(LeftX,       row1Y + rowH * 3, leftLabelW,  "Specialization in"));
        fields.Add(Body(leftValueX,  row1Y + rowH * 3, leftValueW,  prefix: ":  ", tag: "[specialization name]"));
        fields.Add(Body(rightLabelX, row1Y + rowH * 3, rightLabelW, "Graduation date"));
        fields.Add(Body(rightValueX, row1Y + rowH * 3, rightValueW, prefix: ":  ", tag: "[graduation date]"));

        // Dynamic grades table (one row per SubjectGrade + Total + GPA).
        fields.Add(new CertificateField
        {
            Id = Id("table"), Kind = "transcriptTable",
            X = LeftX, Y = 340, Width = ContentW, Height = 320, FontSize = 9,
        });

        // Three signature areas (signature line, official's title, stamp).
        const int sigY = 700;
        const int sigW = 160;
        fields.Add(ImageField(SystemLetterAssetIds.IbssSignatureLine, LeftX,             sigY,     sigW, 2));
        fields.Add(ImageField(SystemLetterAssetIds.IbssSignatureLine, LeftX + 175,       sigY,     sigW, 2));
        fields.Add(ImageField(SystemLetterAssetIds.IbssSignatureLine, LeftX + 350,       sigY,     sigW, 2));
        fields.Add(Body(LeftX,       sigY + 8, sigW, "Signature of School Official", fontSize: 9));
        fields.Add(Body(LeftX + 175, sigY + 8, sigW, "Official's Title",             fontSize: 9));
        fields.Add(Body(LeftX + 350, sigY + 8, sigW, "Stamp",                        fontSize: 9));

        fields.Add(ImageField(SystemLetterAssetIds.IbssFooter, LeftX, 770, ContentW, 60));
        return new CertificatePage { Fields = fields };
    }

    private static CertificatePage TranscriptPage2()
    {
        var fields = new List<CertificateField>();
        fields.AddRange(IbssHeader());
        fields.Add(HorizontalRule(125));

        fields.Add(Body(LeftX, 150, ContentW, "GRADE STANDARD", bold: true, fontSize: 16, align: "center"));

        fields.Add(new CertificateField
        {
            Id = Id("gradestd"), Kind = "gradeStandardTable",
            X = LeftX, Y = 190, Width = ContentW, Height = 470, FontSize = 9,
        });

        fields.Add(Body(LeftX, 670, ContentW, "Grade Point = ECTS credit hours x ECTS Grade point", bold: true, fontSize: 10));
        fields.Add(Body(LeftX, 686, ContentW, "Grade Point Average = Total Grade Point / Total ECTS credit hours", bold: true, fontSize: 10));

        fields.Add(ImageField(SystemLetterAssetIds.IbssFooter, LeftX, 770, ContentW, 60));
        return new CertificatePage { Fields = fields };
    }
}
