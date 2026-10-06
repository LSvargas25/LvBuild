using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Finance;
using LvApplication.Services.Incidents;
using LvApplication.Services.Inventory;
using LvApplication.Services.Payroll;
using LvApplication.Services.Projects;
using LvApplication.Services.SiteLogs;
using LvDomain.Enums;

namespace LvApplication.Services.Finance;

public class ProjectFinanceService : IProjectFinanceService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMaterialTicketRepository _ticketRepository;
    private readonly ISiteLogRepository _siteLogRepository;
    private readonly IPayrollRepository _payrollRepository;
    private readonly IIncidentRepository _incidentRepository;

    public ProjectFinanceService(
        IProjectRepository projectRepository,
        IMaterialTicketRepository ticketRepository,
        ISiteLogRepository siteLogRepository,
        IPayrollRepository payrollRepository,
        IIncidentRepository incidentRepository
    )
    {
        _projectRepository = projectRepository;
        _ticketRepository = ticketRepository;
        _siteLogRepository = siteLogRepository;
        _payrollRepository = payrollRepository;
        _incidentRepository = incidentRepository;
    }

    public async Task<ProjectFinanceDto> GetFinanceAsync(
        int projectId,
        FinancePeriod period,
        DateTime referenceDate
    )
    {
        var project =
            await _projectRepository.GetByIdAsync(projectId)
            ?? throw new NotFoundException($"No se encontró el proyecto {projectId}.");

        var (periodStart, periodEnd) = ComputePeriodRange(period, referenceDate);

        var tickets = await _ticketRepository.GetAppliedInRangeAsync(
            projectId,
            periodStart,
            periodEnd
        );
        var siteLogs = await _siteLogRepository.GetInRangeAsync(projectId, periodStart, periodEnd);

        // Period totals, not the project's running totals (Project.CurrentDirectExpenses /
        // PendingExpenses), so different periods show different figures.
        var payrolls = await _payrollRepository.SumPaidInRangeAsync(
            projectId,
            periodStart,
            periodEnd
        );
        var materials = tickets.Sum(t => t.Total);
        var incidents = await _incidentRepository.SumInRangeAsync(
            projectId,
            IncidentStatus.Approved,
            periodStart,
            periodEnd
        );
        var pendingTickets = await _ticketRepository.SumInRangeAsync(
            projectId,
            MaterialTicketStatus.Review,
            periodStart,
            periodEnd
        );
        var pendingIncidents = await _incidentRepository.SumInRangeAsync(
            projectId,
            IncidentStatus.Draft,
            periodStart,
            periodEnd
        );

        return new ProjectFinanceDto
        {
            ProjectId = project.Id,
            Period = period,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CurrentDirectExpenses = payrolls + materials + incidents,
            PayrollExpenses = payrolls,
            MaterialExpenses = materials,
            IncidentExpenses = incidents,
            PendingExpenses = pendingTickets + pendingIncidents,
            TotalHoursWorked = siteLogs.SelectMany(s => s.Workers).Sum(w => w.HoursWorked),
            Materials = tickets
                .Select(t => new ProjectFinanceMaterialDto
                {
                    MaterialName = t.MaterialName,
                    SupplierName = t.Supplier.Name,
                    Quantity = t.Quantity,
                    Total = t.Total,
                    Date = t.CreatedAt,
                })
                .ToList(),
        };
    }

    // SUPUESTO (sección 15, el documento no define límites exactos de periodo):
    // - week: ventana de 7 días iniciando en `date` (mismo criterio WeekStart→WeekEnd usado en
    //   SiteLog/Payroll).
    // - month: del primer al último día del mes calendario que contiene `date`.
    // - year: del 1 de enero al 31 de diciembre del año que contiene `date`.
    private static (DateTime Start, DateTime End) ComputePeriodRange(
        FinancePeriod period,
        DateTime date
    ) =>
        period switch
        {
            FinancePeriod.Week => (date, date.AddDays(6)),
            FinancePeriod.Month => (
                new DateTime(date.Year, date.Month, 1),
                new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1)
            ),
            FinancePeriod.Year => (new DateTime(date.Year, 1, 1), new DateTime(date.Year, 12, 31)),
            _ => throw new ArgumentOutOfRangeException(nameof(period)),
        };
}
