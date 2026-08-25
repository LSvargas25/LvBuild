using FluentValidation;
using LvApplication.DTOs.Workers;
using LvApplication.Services.Branches;
using LvDomain.Enums;

namespace LvApplication.Validators.Workers;

public class CreateWorkerDtoValidator : AbstractValidator<CreateWorkerDto>
{
    public CreateWorkerDtoValidator(IBranchRepository branchRepository)
    {
        RuleFor(x => x.Name).NotEmpty();

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => WorkerCategoryTypeMap.IsValidCombination(x.Category, x.Type))
            .WithMessage("El tipo de trabajador no corresponde a la categoría seleccionada")
            .WithName(nameof(CreateWorkerDto.Type));

        RuleFor(x => x.BranchId)
            .MustAsync(
                async (branchId, _) =>
                    await branchRepository.GetByIdAsync(branchId!.Value) is not null
            )
            .WithMessage("La sucursal indicada no existe")
            .When(x => x.BranchId.HasValue);

        RuleFor(x => x)
            .MustAsync(
                async (dto, _) =>
                {
                    var branch = await branchRepository.GetByIdAsync(dto.BranchId!.Value);
                    return branch is null
                        || branch.BranchType != BranchType.Warehouse
                        || dto.Category == WorkerCategory.Storage;
                }
            )
            .WithMessage(
                "Los trabajadores asignados a una sucursal de tipo Bodega deben tener categoría Almacenamiento"
            )
            .WithName(nameof(CreateWorkerDto.Category))
            .When(x => x.BranchId.HasValue);
    }
}
