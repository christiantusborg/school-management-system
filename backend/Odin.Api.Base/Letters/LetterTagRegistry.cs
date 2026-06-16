namespace Odin.Api.Base.Letters;

public static class LetterTagRegistry
{
    public static readonly IReadOnlyList<LetterTag> All = new[]
    {
        new LetterTag("[student full name]",   "student.fullName"),
        new LetterTag("[student firstname]",   "student.firstName"),
        new LetterTag("[student surname]",     "student.surname"),
        new LetterTag("[student address]",     "student.address"),
        new LetterTag("[student number]",      "student.number"),
        new LetterTag("[partner name]",        "partner.name"),
        new LetterTag("[program name]",        "programme.name"),
        new LetterTag("[specialization name]", "specialization.name"),
        new LetterTag("[grade]",               "enrollment.finalGrade"),
        new LetterTag("[transcript]",          "enrollment.transcriptTable"),
        new LetterTag("[date]",                "today"),
        new LetterTag("[ref]",                 "letter.reference"),
        new LetterTag("[passport id]",         "student.passportId"),
        new LetterTag("[commencement date]",   "enrollment.commencementDate"),
        new LetterTag("[duration of study]",   "specialization.durationOfStudyMonths"),
        new LetterTag("[mode of study]",       "enrollment.modeOfStudy"),
        new LetterTag("[instruction language]", "specialization.instructionLanguage"),
        new LetterTag("[completion date]",     "enrollment.completionDate"),
        new LetterTag("[date of birth]",       "student.dateOfBirth"),
        new LetterTag("[ects achieved]",       "enrollment.ectsAchieved"),
        new LetterTag("[graduation date]",     "enrollment.graduationDate"),
    };
}
