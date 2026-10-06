using LvApplication.Common;
using LvDomain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Seeding;

/// <summary>
/// The application services stamp CreatedAt, AssignedAt, PaidAt... with "now", so right after
/// seeding every record would be dated the day the seed ran. This pass moves each timestamp to
/// the day the event belongs to in the demo story (project start, the week of each site log,
/// the Friday each payroll was paid...). Nothing is ever dated after the moment of the seed.
/// </summary>
public sealed partial class DemoDataSeeder
{
    private async Task ApplyTimelineAsync(DemoTimeline t, CancellationToken cancellationToken)
    {
        await BackdateMasterDataAsync(t, cancellationToken);
        await BackdateStoreAsync(t, cancellationToken);
        await BackdateBudgetsAsync(t, cancellationToken);
        await BackdateProjectAsync(t, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The company existed well before the project started.</summary>
    private async Task BackdateMasterDataAsync(DemoTimeline t, CancellationToken ct)
    {
        var founded = t.At(t.ProjectStart.AddDays(-60), 8);
        foreach (var user in await _context.Users.ToListAsync(ct))
            user.CreatedAt = founded;
        foreach (var branch in await _context.Branches.ToListAsync(ct))
            branch.CreatedAt = founded;
        foreach (var customer in await _context.Customers.ToListAsync(ct))
            customer.CreatedAt = founded;
        foreach (var supplier in await _context.Suppliers.ToListAsync(ct))
            supplier.CreatedAt = founded;
        foreach (var worker in await _context.Workers.ToListAsync(ct))
            worker.CreatedAt = founded;
        foreach (var material in await _context.MaterialCatalogs.ToListAsync(ct))
            material.CreatedAt = founded;
        foreach (var product in await _context.Products.ToListAsync(ct))
            product.CreatedAt = founded;
    }

    /// <summary>
    /// Stock arrived at the warehouse three weeks ago and was moved to the store a few days
    /// later. The cash register and its sales stay on the day of the seed: it is today's shift.
    /// </summary>
    private async Task BackdateStoreAsync(DemoTimeline t, CancellationToken ct)
    {
        foreach (var ticket in await _context.ProductIncorporationTickets.ToListAsync(ct))
        {
            ticket.CreatedDate = ticket.CreatedAt = t.At(t.Today.AddDays(-20), 9);
            ticket.ValidatedDate = ticket.UpdatedAt = t.At(t.Today.AddDays(-20), 11);
        }
        foreach (var movement in await _context.InventoryMovements.ToListAsync(ct))
        {
            movement.SentDate = movement.CreatedAt = t.At(t.Today.AddDays(-18), 8);
            movement.ValidatedDate = movement.UpdatedAt = t.At(t.Today.AddDays(-17), 10);
        }
    }

    /// <summary>
    /// Each budget is created on its own day and every status change (history row) follows
    /// two days after the previous one. The active project's budget precedes its offer.
    /// </summary>
    private async Task BackdateBudgetsAsync(DemoTimeline t, CancellationToken ct)
    {
        var activeBudgetId = await _context.Projects.Select(p => p.BudgetId).SingleAsync(ct);
        var budgets = await _context.Budgets.OrderBy(b => b.Id).ToListAsync(ct);

        // Same order as SeedBudgetsInEveryStateAsync: draft, review, correction, sent, cancelled.
        int[] daysBeforeToday = [4, 10, 15, 20, 45];
        var others = budgets.Where(b => b.Id != activeBudgetId).ToList();

        foreach (var budget in budgets)
        {
            var created =
                budget.Id == activeBudgetId
                    ? t.ProjectStart.AddDays(-35)
                    : t.Today.AddDays(-daysBeforeToday[others.IndexOf(budget) % 5]);
            budget.CreatedAt = t.At(created, 9);

            var history = await _context
                .BudgetHistories.Where(h => h.BudgetId == budget.Id)
                .OrderBy(h => h.Id)
                .ToListAsync(ct);
            for (var i = 0; i < history.Count; i++)
                history[i].Timestamp = history[i].CreatedAt = t.At(created.AddDays(2 * i), 10);
            budget.UpdatedAt = history.Count > 0 ? history[^1].Timestamp : budget.CreatedAt;

            foreach (
                var chapter in await _context
                    .BudgetChapters.Where(c => c.BudgetId == budget.Id)
                    .ToListAsync(ct)
            )
                chapter.CreatedAt = budget.CreatedAt;
        }

        foreach (var offer in await _context.Offers.ToListAsync(ct))
        {
            offer.CreatedAt = t.At(offer.IssueDate, 10);
            offer.UpdatedAt = t.At(t.ProjectStart.AddDays(-10), 11); // accepted by the client
            foreach (
                var chapter in await _context
                    .OfferChapters.Where(c => c.OfferId == offer.Id)
                    .ToListAsync(ct)
            )
                chapter.CreatedAt = offer.CreatedAt;
        }
    }

    private async Task BackdateProjectAsync(DemoTimeline t, CancellationToken ct)
    {
        var start = t.ProjectStart;

        var project = await _context.Projects.SingleAsync(ct);
        project.CreatedAt = t.At(start.AddDays(-7), 9);
        foreach (
            var chapter in await _context
                .ProjectChapters.Where(c => c.ProjectId == project.Id)
                .ToListAsync(ct)
        )
            chapter.CreatedAt = project.CreatedAt;

        // The whole crew joins on the first day.
        foreach (var assignment in await _context.ProjectWorkers.ToListAsync(ct))
            assignment.AssignedAt = assignment.CreatedAt = t.At(start, 7);

        // Same order as the purchases in SeedActiveProjectAsync: cement and sand on day one,
        // rebar and block the Friday before the walls start.
        int[] purchaseDays = [0, 0, 11, 11];
        var tickets = await _context.MaterialTickets.OrderBy(m => m.Id).ToListAsync(ct);
        for (var i = 0; i < tickets.Count; i++)
        {
            var day = start.AddDays(purchaseDays[i % purchaseDays.Length]);
            tickets[i].CreatedAt = t.At(day, 9);
            tickets[i].UpdatedAt = t.At(day, 11); // applied to the project inventory
        }
        foreach (var item in await _context.ProjectInventoryItems.ToListAsync(ct))
            item.CreatedAt = t.At(start, 11);

        // A site log is written on Saturday and submitted/approved the following Monday.
        var siteLogs = await _context.SiteLogs.ToListAsync(ct);
        foreach (var siteLog in siteLogs)
        {
            siteLog.CreatedAt = t.At(siteLog.WeekStart.AddDays(5), 16);
            siteLog.UpdatedAt =
                siteLog.Status == SiteLogStatus.Draft
                    ? siteLog.CreatedAt
                    : t.At(siteLog.WeekEnd.AddDays(1), 10);
            foreach (
                var w in await _context
                    .SiteLogWorkers.Where(w => w.SiteLogId == siteLog.Id)
                    .ToListAsync(ct)
            )
                w.CreatedAt = siteLog.CreatedAt;
            foreach (
                var m in await _context
                    .SiteLogMaterials.Where(m => m.SiteLogId == siteLog.Id)
                    .ToListAsync(ct)
            )
                m.CreatedAt = siteLog.CreatedAt;
            foreach (
                var p in await _context
                    .ProjectProgresses.Where(p => p.SiteLogId == siteLog.Id)
                    .ToListAsync(ct)
            )
                p.CalculatedAt = p.CreatedAt = siteLog.UpdatedAt.Value;
        }

        // Payroll prepared on the Monday after the week, paid that Friday.
        foreach (var payroll in await _context.Payrolls.ToListAsync(ct))
        {
            payroll.CreatedAt = t.At(payroll.WeekEnd.AddDays(1), 11);
            if (payroll.Status == PayrollStatus.Paid)
                payroll.PaidAt = t.At(payroll.WeekEnd.AddDays(5), 15);
            payroll.UpdatedAt = payroll.PaidAt ?? payroll.CreatedAt;

            var details = await _context
                .PayrollDetails.Where(d => d.PayrollId == payroll.Id)
                .ToListAsync(ct);
            foreach (var detail in details)
            {
                detail.CreatedAt = payroll.CreatedAt;
                var payments = await _context
                    .PayrollDetailPayments.Where(p => p.PayrollDetailId == detail.Id)
                    .ToListAsync(ct);
                foreach (var payment in payments)
                    payment.CreatedAt = payroll.PaidAt ?? payroll.CreatedAt;
            }
        }

        // Reported the day it happened, approved the next morning.
        foreach (var incident in await _context.Incidents.ToListAsync(ct))
        {
            incident.CreatedAt = t.At(incident.Date, 15);
            incident.UpdatedAt = t.At(incident.Date.AddDays(1), 9);
        }
    }

    /// <summary>The demo calendar, anchored to the Costa Rica date on which the seed runs.</summary>
    private sealed record DemoTimeline(DateTime Today, DateTime ThisMonday, DateTime ProjectStart)
    {
        /// <summary>Day (from the project start) of the approved rain incident: week 3, Thursday.</summary>
        public const int IncidentDay = 17;

        private readonly DateTime _seededAtUtc = DateTime.UtcNow;

        public static DemoTimeline From(DateTime costaRicaToday)
        {
            var thisMonday = costaRicaToday.AddDays(-(((int)costaRicaToday.DayOfWeek + 6) % 7));
            return new DemoTimeline(costaRicaToday, thisMonday, thisMonday.AddDays(-35));
        }

        /// <summary>
        /// UTC instant of the given Costa Rica day and hour, never later than the seed itself
        /// (e.g. the current week's draft site log when the seed runs early on a Monday).
        /// </summary>
        public DateTime At(DateTime costaRicaDay, int hour)
        {
            var instant = CostaRicaTime.StartOfDayUtc(costaRicaDay).AddHours(hour);
            return instant < _seededAtUtc ? instant : _seededAtUtc;
        }
    }
}
