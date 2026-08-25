using FluentValidation;
using LvApplication.DTOs.Incidents;

namespace LvApplication.Validators.Incidents;

public class UpdateIncidentDtoValidator : AbstractValidator<UpdateIncidentDto>
{
    public UpdateIncidentDtoValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.ChapterId).GreaterThan(0).When(x => x.ChapterId.HasValue);

        RuleForEach(x => x.Materials)
            .ChildRules(material =>
            {
                material.RuleFor(m => m.MaterialId).GreaterThan(0);
                material.RuleFor(m => m.Quantity).GreaterThan(0);
            });

        RuleForEach(x => x.Workers)
            .ChildRules(worker =>
            {
                worker.RuleFor(w => w.WorkerId).GreaterThan(0);
                worker.RuleFor(w => w.HoursUsed).GreaterThan(0);
            });
    }
}
