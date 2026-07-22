using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Payroll;
using LvApplication.Services.Budgets;
using LvApplication.Services.Projects;
using LvApplication.Services.SiteLogs;
using LvDomain.Entities.Payroll;
using LvDomain.Enums;

namespace LvApplication.Services.Payroll;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly ISiteLogService _siteLogService;
    private readonly IProjectRepository _projectRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IProjectService _projectService;
    private readonly IProjectChapterService _projectChapterService;
    private readonly IValidator<CreatePayrollDto> _createValidator;
    private readonly IValidator<UpdatePayrollDto> _updateValidator;

    public PayrollService(
        IPayrollRepository payrollRepository,
        ISiteLogService siteLogService,
        IProjectRepository projectRepository,
        IBudgetRepository budgetRepository,
        IProjectService projectService,
        IProjectChapterService projectChapterService,
        IValidator<CreatePayrollDto> createValidator,
        IValidator<UpdatePayrollDto> updateValidator)
    {
        _payrollRepository = payrollRepository;
        _siteLogService = siteLogService;
        _projectRepository = projectRepository;
        _budgetRepository = budgetRepository;
        _projectService = projectService;
        _projectChapterService = projectChapterService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PayrollDto> CreateAsync(CreatePayrollDto request, int createdByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var siteLog = await _siteLogService.GetByIdAsync(request.SiteLogId);

        if (siteLog.Status != SiteLogStatus.Approved)
        {
            throw new ValidationAppException("Solo se puede crear una planilla a partir de una bitácora Aprobada.");
        }

        if (await _payrollRepository.ExistsForSiteLogAsync(request.SiteLogId))
        {
            throw new ConflictException("Ya existe una planilla para esta bitácora.");
        }

        if (request.ChapterId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(siteLog.ProjectId)
                ?? throw new NotFoundException($"Project {siteLog.ProjectId} not found.");
            await ValidateChapterAsync(project.BudgetId, request.ChapterId);
        }

        var payroll = new LvDomain.Entities.Payroll.Payroll
        {
            ProjectId = siteLog.ProjectId,
            SiteLogId = siteLog.Id,
            WeekStart = siteLog.WeekStart,
            WeekEnd = siteLog.WeekEnd,
            Status = PayrollStatus.Pending,
            ChapterId = request.ChapterId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        SyncDetails(payroll, request.Details);
        payroll.TotalPayroll = payroll.Details.Sum(d => d.FinalAmountToPay);

        await _payrollRepository.AddAsync(payroll);

        return MapToDto(payroll);
    }

    public async Task<PayrollDto> UpdateAsync(int id, UpdatePayrollDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var payroll = await _payrollRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Payroll {id} not found.");

        if (payroll.Status != PayrollStatus.Pending)
        {
            throw new ValidationAppException("Solo se puede editar una planilla en estado Pendiente.");
        }

        if (request.ChapterId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(payroll.ProjectId)
                ?? throw new NotFoundException($"Project {payroll.ProjectId} not found.");
            await ValidateChapterAsync(project.BudgetId, request.ChapterId);
        }

        payroll.ChapterId = request.ChapterId;
        payroll.UpdatedAt = DateTime.UtcNow;

        SyncDetails(payroll, request.Details);
        payroll.TotalPayroll = payroll.Details.Sum(d => d.FinalAmountToPay);

        await _payrollRepository.UpdateAsync(payroll);

        return MapToDto(payroll);
    }

    public async Task<PayrollDto> MarkAsPaidAsync(int id)
    {
        var payroll = await _payrollRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Payroll {id} not found.");

        if (payroll.Status != PayrollStatus.Pending)
        {
            throw new ValidationAppException("Solo se puede marcar como pagada una planilla en estado Pendiente.");
        }

        payroll.Status = PayrollStatus.Paid;
        payroll.PaidAt = DateTime.UtcNow;
        payroll.UpdatedAt = DateTime.UtcNow;
        await _payrollRepository.UpdateAsync(payroll);

        var project = await _projectRepository.GetByIdAsync(payroll.ProjectId)
            ?? throw new NotFoundException($"Project {payroll.ProjectId} not found.");
        project.CurrentDirectExpenses += payroll.TotalPayroll;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.UpdateAsync(project);

        await _siteLogService.UpdateTotalPayrollAsync(payroll.SiteLogId, payroll.TotalPayroll);
        await _projectService.IncrementWeekCounterAsync(payroll.ProjectId);

        if (payroll.ChapterId.HasValue)
        {
            await _projectChapterService.RecalculateActualCostAsync(payroll.ProjectId, payroll.ChapterId.Value);
        }

        return MapToDto(payroll);
    }

    public async Task DeleteAsync(int id)
    {
        var payroll = await _payrollRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Payroll {id} not found.");

        if (payroll.Status != PayrollStatus.Pending)
        {
            throw new ValidationAppException("Solo se puede eliminar una planilla en estado Pendiente.");
        }

        await _payrollRepository.DeleteAsync(payroll);
    }

    public async Task<PayrollDto> GetByIdAsync(int id)
    {
        var payroll = await _payrollRepository.GetByIdAsync(id) ?? throw new NotFoundException($"Payroll {id} not found.");
        return MapToDto(payroll);
    }

    public async Task<PagedResult<PayrollDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _payrollRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<PayrollDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<PayrollDto>> GetAllByProjectAsync(int projectId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _payrollRepository.GetPagedByProjectAsync(projectId, pageNumber, pageSize);

        return new PagedResult<PayrollDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static void SyncDetails(LvDomain.Entities.Payroll.Payroll payroll, List<PayrollDetailDto> detailDtos)
    {
        payroll.Details.Clear();

        foreach (var dto in detailDtos)
        {
            var finalAmountToPay = (dto.HoursWorked * dto.HourlyRate) - (dto.AdvanceAmountApplied ?? 0);

            var detail = new PayrollDetail
            {
                WorkerId = dto.WorkerId,
                Date = dto.Date,
                HoursWorked = dto.HoursWorked,
                HourlyRate = dto.HourlyRate,
                PaymentType = dto.PaymentType,
                AdvanceAmountApplied = dto.AdvanceAmountApplied,
                FinalAmountToPay = finalAmountToPay,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var paymentDto in dto.Payments)
            {
                detail.Payments.Add(new PayrollDetailPayment
                {
                    PaymentMethod = paymentDto.PaymentMethod,
                    Amount = paymentDto.Amount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            payroll.Details.Add(detail);
        }
    }

    private static PayrollDto MapToDto(LvDomain.Entities.Payroll.Payroll payroll) => new()
    {
        Id = payroll.Id,
        ProjectId = payroll.ProjectId,
        SiteLogId = payroll.SiteLogId,
        ChapterId = payroll.ChapterId,
        WeekStart = payroll.WeekStart,
        WeekEnd = payroll.WeekEnd,
        TotalPayroll = payroll.TotalPayroll,
        Status = payroll.Status,
        CreatedByUserId = payroll.CreatedByUserId,
        PaidAt = payroll.PaidAt,
        Details = payroll.Details.Select(d => new PayrollDetailResponseDto
        {
            Id = d.Id,
            WorkerId = d.WorkerId,
            Date = d.Date,
            HoursWorked = d.HoursWorked,
            HourlyRate = d.HourlyRate,
            PaymentType = d.PaymentType,
            AdvanceAmountApplied = d.AdvanceAmountApplied,
            FinalAmountToPay = d.FinalAmountToPay,
            Payments = d.Payments.Select(p => new PayrollDetailPaymentResponseDto
            {
                Id = p.Id,
                PaymentMethod = p.PaymentMethod,
                Amount = p.Amount
            }).ToList()
        }).ToList()
    };

    private async Task ValidateChapterAsync(int budgetId, int? chapterId)
    {
        if (!chapterId.HasValue)
        {
            return;
        }

        var budget = await _budgetRepository.GetByIdAsync(budgetId)
            ?? throw new NotFoundException($"Budget {budgetId} not found.");

        if (!budget.Chapters.Any(c => c.Id == chapterId.Value))
        {
            throw new ValidationAppException($"El capítulo {chapterId} no pertenece al presupuesto de este proyecto.");
        }
    }
}
