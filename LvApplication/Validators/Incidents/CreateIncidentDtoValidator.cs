using FluentValidation;
using LvApplication.DTOs.Incidents;

namespace LvApplication.Validators.Incidents;

public class CreateIncidentDtoValidator : AbstractValidator<CreateIncidentDto>
{
    public CreateIncidentDtoValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty();

        RuleForEach(x => x.Materials).ChildRules(material =>
        {
            material.RuleFor(m => m.MaterialId).GreaterThan(0);
            material.RuleFor(m => m.Quantity).GreaterThan(0);
        });

        RuleForEach(x => x.Workers).ChildRules(worker =>
        {
            worker.RuleFor(w => w.WorkerId).GreaterThan(0);
            worker.RuleFor(w => w.HoursUsed).GreaterThan(0);
        });
    }
}
