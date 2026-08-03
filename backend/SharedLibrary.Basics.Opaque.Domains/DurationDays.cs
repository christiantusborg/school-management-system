namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Study-duration handling. The per-enrolment override is stored as a VALUE
/// plus a UNIT (Enrollment.ApprovedDurationValue / ApprovedDurationUnit,
/// "Month" or "Day") so the entered intent is preserved verbatim: months stay
/// whole months forever and never round-trip through days. Conversion to days
/// happens only when comparing or computing dates, using REAL calendar month
/// lengths from the commencement date (30.44-day average fallback).
/// </summary>
public static class DurationDays
{
    public const double AvgMonthDays = 30.44;

    public const string UnitMonth = "Month";
    public const string UnitDay = "Day";

    public static int MonthsToDays(DateTime? commencement, int months) =>
        commencement is { } c
            ? (int)(c.Date.AddMonths(months) - c.Date).TotalDays
            : (int)Math.Round(months * AvgMonthDays);

    /// <summary>Day equivalent of a stored (value, unit) pair, for range
    /// checks and installment math. Null when there is no override.</summary>
    public static int? ToDays(DateTime? commencement, int? value, string? unit) =>
        value is not { } v ? null
        : string.Equals(unit, UnitDay, StringComparison.OrdinalIgnoreCase) ? v
        : MonthsToDays(commencement, v);

    public static string Display(int? value, string? unit) =>
        value is not { } v ? string.Empty
        : string.Equals(unit, UnitDay, StringComparison.OrdinalIgnoreCase)
            ? $"{v} day{(v == 1 ? "" : "s")}"
            : $"{v} month{(v == 1 ? "" : "s")}";

    /// <summary>Expected completion: commencement + the override (or the
    /// specialization's month default), inclusive end date.</summary>
    public static DateTime? ExpectedCompletion(DateTime? commencement, int? value, string? unit, int? specMonths)
    {
        if (commencement is not { } c) return null;
        if (value is { } v)
            return string.Equals(unit, UnitDay, StringComparison.OrdinalIgnoreCase)
                ? c.Date.AddDays(v - 1)
                : c.Date.AddMonths(v).AddDays(-1);
        if (specMonths is { } m) return c.Date.AddMonths(m).AddDays(-1);
        return null;
    }
}
