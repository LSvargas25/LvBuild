using FluentValidation;
using LvApplication.DTOs.Branches;
using LvDomain.Enums;

namespace LvApplication.Validators.Branches;

public class UpdateBranchDtoValidator : AbstractValidator<UpdateBranchDto>
{
    public UpdateBranchDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.City)
            .NotEmpty();

        RuleFor(x => x.Province)
            .NotEmpty();

        RuleFor(x => x.BranchType)
            .IsInEnum();

        RuleFor(x => x.BranchAdminId)
            .NotNull()
            .When(x => x.BranchType is BranchType.Commercial or BranchType.Warehouse)
            .WithMessage("El administrador de sucursal es obligatorio para sucursales de tipo Comercio o Bodega");
    }
}
