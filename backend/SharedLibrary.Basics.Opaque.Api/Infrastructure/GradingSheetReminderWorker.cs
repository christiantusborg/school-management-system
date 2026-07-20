using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.Api.Infrastructure;

/// <summary>
/// Grading-sheet reminders for Module Cohorts: while "Date Grading Sheet
/// Uploaded" is blank, the assigned teacher is emailed 2 weeks before the
/// due date (EndDate + 1 month unless overridden), 1 week before, and 1
/// week after. One email per stage, tracked on the cohort; the schedule
/// resets when the end date changes. Falls back to the partner's primary
/// contact email when the teacher has no login email.
/// </summary>
internal sealed class GradingSheetReminderWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<GradingSheetReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("[CohortReminders] Started — checking every 6 hours.");
        while (!ct.IsCancellationRequested)
        {
            try { await RunOnceAsync(ct); }
            catch (Exception ex) { logger.LogError(ex, "[CohortReminders] Sweep failed"); }
            try { await Task.Delay(TimeSpan.FromHours(6), ct); }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OdinDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
        var cohorts = await db.ModuleCohorts
            .Where(c => c.DeletedAt == null && c.GradingSheetUploadedDate == null
                && (c.EndDate != null || c.GradingSheetDueOverride != null)
                && (!c.Reminder2WeeksSent || !c.Reminder1WeekSent || !c.ReminderOverdueSent))
            .ToListAsync(ct);

        var sent = 0;
        foreach (var cohort in cohorts)
        {
            var due = cohort.GradingSheetDueOverride ?? cohort.EndDate!.Value.AddMonths(1);
            string? stage = null;
            if (!cohort.Reminder2WeeksSent && today >= due.AddDays(-14) && today < due.AddDays(-7))
                stage = "2 weeks before";
            else if (!cohort.Reminder1WeekSent && today >= due.AddDays(-7) && today <= due)
                stage = "1 week before";
            else if (!cohort.ReminderOverdueSent && today >= due.AddDays(7))
                stage = "1 week overdue";
            if (stage is null) continue;

            // Teacher login email, else partner primary contact.
            var to = await (
                from t in db.Teachers
                join u in db.Users on t.UserId equals u.Id
                where t.TeacherId == cohort.TeacherId && t.DeletedAt == null && u.Email != null && u.Email != ""
                select u.Email).FirstOrDefaultAsync(ct);
            var teacherName = await db.Teachers
                .Where(t => t.TeacherId == cohort.TeacherId)
                .Select(t => t.DisplayName).FirstOrDefaultAsync(ct) ?? "the assigned teacher";
            to ??= await db.PartnerContactEmails
                .Where(e => e.PartnerId == cohort.PartnerId && e.DeletedAt == null && e.Email != null)
                .OrderByDescending(e => e.IsPrimary)
                .Select(e => e.Email).FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(to))
            {
                logger.LogWarning("[CohortReminders] No recipient for cohort {Number}", cohort.CohortNumber);
                continue;
            }

            var subject = $"Grading sheet reminder — {cohort.CohortNumber} ({stage})";
            var body = $"""
                <p>Dear {teacherName},</p>
                <p>This is a reminder to upload the grading sheet for
                Cohort (Section) Number: <strong>{cohort.CohortNumber}</strong>.</p>
                <p>Due date: <strong>{due:dd MMMM yyyy}</strong> ({stage}).</p>
                <p>Please upload it in the partner portal under Module Cohorts.</p>
                """;
            try
            {
                await email.SendAsync(to, subject, body, ct);
                if (stage == "2 weeks before") cohort.Reminder2WeeksSent = true;
                else if (stage == "1 week before") { cohort.Reminder2WeeksSent = true; cohort.Reminder1WeekSent = true; }
                else { cohort.Reminder2WeeksSent = true; cohort.Reminder1WeekSent = true; cohort.ReminderOverdueSent = true; }
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[CohortReminders] Send failed for {Number} → {To}", cohort.CohortNumber, to);
            }
        }
        if (sent > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("[CohortReminders] Sent {Count} reminder(s)", sent);
        }
    }
}
