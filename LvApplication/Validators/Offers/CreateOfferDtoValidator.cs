using FluentValidation;
using LvApplication.DTOs.Offers;
using LvDomain.Enums;

namespace LvApplication.Validators.Offers;

public class CreateOfferDtoValidator : AbstractValidator<CreateOfferDto>
{
    public CreateOfferDtoValidator()
    {
        RuleFor(x => x.WorkLocation).NotEmpty();
        RuleFor(x => x.WorkScope).NotEmpty();
        RuleFor(x => x.PaymentTerms).NotEmpty();
        RuleFor(x => x.Warranties).NotEmpty();
        RuleFor(x => x.Exclusions).NotEmpty();
        RuleFor(x => x.ValidityDays).GreaterThan(0);
        RuleFor(x => x.EstimatedDurationWeeks).GreaterThan(0);

        RuleFor(x => x.TotalProjectPrice)
            .NotNull()
            .WithMessage("TotalProjectPrice es obligatorio para ofertas Llave en Mano (Turnkey).")
            .When(x => x.OfferType == OfferType.Turnkey);
        RuleFor(x => x.TotalProjectPrice)
            .Null()
            .WithMessage("TotalProjectPrice no debe enviarse en ofertas por Porcentaje.")
            .When(x => x.OfferType == OfferType.Percentage);

        RuleFor(x => x.AgreedPercentage)
            .NotNull()
            .WithMessage("AgreedPercentage es obligatorio para ofertas por Porcentaje.")
            .When(x => x.OfferType == OfferType.Percentage);
        RuleFor(x => x.AgreedPercentage)
            .Null()
            .WithMessage("AgreedPercentage no debe enviarse en ofertas Llave en Mano.")
            .When(x => x.OfferType == OfferType.Turnkey);

        RuleFor(x => x.PercentageIncludes)
            .NotEmpty()
            .WithMessage("PercentageIncludes es obligatorio para ofertas por Porcentaje.")
            .When(x => x.OfferType == OfferType.Percentage);
        RuleFor(x => x.PercentageIncludes)
            .Empty()
            .WithMessage("PercentageIncludes no debe enviarse en ofertas Llave en Mano.")
            .When(x => x.OfferType == OfferType.Turnkey);

        RuleFor(x => x.PercentageExcludes)
            .NotEmpty()
            .WithMessage("PercentageExcludes es obligatorio para ofertas por Porcentaje.")
            .When(x => x.OfferType == OfferType.Percentage);
        RuleFor(x => x.PercentageExcludes)
            .Empty()
            .WithMessage("PercentageExcludes no debe enviarse en ofertas Llave en Mano.")
            .When(x => x.OfferType == OfferType.Turnkey);

        RuleFor(x => x.PercentageCalculationMethod)
            .NotEmpty()
            .WithMessage("PercentageCalculationMethod es obligatorio para ofertas por Porcentaje.")
            .When(x => x.OfferType == OfferType.Percentage);
        RuleFor(x => x.PercentageCalculationMethod)
            .Empty()
            .WithMessage("PercentageCalculationMethod no debe enviarse en ofertas Llave en Mano.")
            .When(x => x.OfferType == OfferType.Turnkey);

        RuleFor(x => x.PaymentFrequency)
            .NotNull()
            .WithMessage("PaymentFrequency es obligatorio para ofertas por Porcentaje.")
            .When(x => x.OfferType == OfferType.Percentage);
        RuleFor(x => x.PaymentFrequency)
            .Null()
            .WithMessage("PaymentFrequency no debe enviarse en ofertas Llave en Mano.")
            .When(x => x.OfferType == OfferType.Turnkey);
    }
}
