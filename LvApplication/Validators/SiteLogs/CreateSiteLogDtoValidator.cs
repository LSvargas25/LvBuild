using FluentValidation;
using LvApplication.DTOs.SiteLogs;

namespace LvApplication.Validators.SiteLogs;

public class CreateSiteLogDtoValidator : AbstractValidator<CreateSiteLogDto>
{
    public CreateSiteLogDtoValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.TaskDescription).NotEmpty();
        RuleFor(x => x.WeekEnd).GreaterThanOrEqualTo(x => x.WeekStart);

        RuleForEach(x => x.Workers).ChildRules(worker =>
        {
            worker.RuleFor(w => w.WorkerId).GreaterThan(0);
            worker.RuleFor(w => w.HoursWorked).GreaterThan(0);
        });

        RuleForEach(x => x.Materials).ChildRules(material =>
        {
            material.RuleFor(m => m.MaterialId).GreaterThan(0);
            material.RuleFor(m => m.QuantityUsed).GreaterThan(0);
        });
    }
}
