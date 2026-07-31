using FluentValidation;
using LvApplication.DTOs.Workers;
using LvApplication.Services.Branches;
using LvDomain.Enums;

namespace LvApplication.Validators.Workers;

public class UpdateWorkerDtoValidator : AbstractValidator<UpdateWorkerDto>
{
    public UpdateWorkerDtoValidator(IBranchRepository branchRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Category)
            .IsInEnum();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.HourlyRate)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => WorkerCategoryTypeMap.IsValidCombination(x.Category, x.Type))
            .WithMessage("El tipo de trabajador no corresponde a la categoría seleccionada")
            .WithName(nameof(UpdateWorkerDto.Type));

        RuleFor(x => x.BranchId)
            .MustAsync(async (branchId, _) => await branchRepository.GetByIdAsync(branchId!.Value) is not null)
            .WithMessage("La sucursal indicada no existe")
            .When(x => x.BranchId.HasValue);

        RuleFor(x => x)
            .MustAsync(async (dto, _) =>
            {
                var branch = await branchRepository.GetByIdAsync(dto.BranchId!.Value);
                return branch is null || branch.BranchType != BranchType.Warehouse || dto.Category == WorkerCategory.Storage;
            })
            .WithMessage("Los trabajadores asignados a una sucursal de tipo Bodega deben tener categoría Almacenamiento")
            .WithName(nameof(UpdateWorkerDto.Category))
            .When(x => x.BranchId.HasValue);
    }
}
