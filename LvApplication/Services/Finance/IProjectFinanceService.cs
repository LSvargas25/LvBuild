using LvApplication.DTOs.Finance;
using LvDomain.Enums;

namespace LvApplication.Services.Finance;

public interface IProjectFinanceService
{
    Task<ProjectFinanceDto> GetFinanceAsync(
        int projectId,
        FinancePeriod period,
        DateTime referenceDate
    );
}
