using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Offers;
using LvApplication.Services.Budgets;
using LvDomain.Entities.Offers;
using LvDomain.Enums;

namespace LvApplication.Services.Offers;

public class OfferService : IOfferService
{
    private readonly IOfferRepository _offerRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IBudgetService _budgetService;
    private readonly IOfferPdfGenerator _pdfGenerator;
    private readonly IValidator<CreateOfferDto> _createValidator;
    private readonly IValidator<UpdateOfferDto> _updateValidator;

    public OfferService(
        IOfferRepository offerRepository,
        IBudgetRepository budgetRepository,
        IBudgetService budgetService,
        IOfferPdfGenerator pdfGenerator,
        IValidator<CreateOfferDto> createValidator,
        IValidator<UpdateOfferDto> updateValidator
    )
    {
        _offerRepository = offerRepository;
        _budgetRepository = budgetRepository;
        _budgetService = budgetService;
        _pdfGenerator = pdfGenerator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<OfferResponseDto> CreateAsync(CreateOfferDto request, int createdByUserId)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var budget =
            await _budgetRepository.GetByIdAsync(request.BudgetId)
            ?? throw new NotFoundException($"Budget {request.BudgetId} not found.");

        if (budget.Status != BudgetStatus.Sent)
        {
            throw new ValidationAppException(
                "Solo se puede crear una oferta a partir de un presupuesto en estado Enviado."
            );
        }

        var existingOffer = await _offerRepository.GetByBudgetIdAsync(request.BudgetId);
        if (existingOffer is not null)
        {
            throw new ConflictException(
                $"El presupuesto {request.BudgetId} ya tiene una oferta activa (Id {existingOffer.Id}, estado {existingOffer.Status})."
            );
        }

        var offer = new Offer
        {
            BudgetId = budget.Id,
            CustomerId = budget.CustomerId,
            OfferNumber = await GenerateOfferNumberAsync(request.IssueDate.Year),
            OfferType = request.OfferType,
            IssueDate = request.IssueDate,
            ValidityDays = request.ValidityDays,
            WorkLocation = request.WorkLocation,
            WorkScope = request.WorkScope,
            EstimatedStartDate = request.EstimatedStartDate,
            EstimatedDurationWeeks = request.EstimatedDurationWeeks,
            EstimatedDeliveryDate =
                request.EstimatedDeliveryDate
                ?? request.EstimatedStartDate.AddDays(request.EstimatedDurationWeeks * 7),
            PaymentTerms = request.PaymentTerms,
            Warranties = request.Warranties,
            Exclusions = request.Exclusions,
            TotalProjectPrice = request.TotalProjectPrice,
            AgreedPercentage = request.AgreedPercentage,
            PercentageIncludes = request.PercentageIncludes,
            PercentageExcludes = request.PercentageExcludes,
            PercentageCalculationMethod = request.PercentageCalculationMethod,
            PaymentFrequency = request.PaymentFrequency,
            Status = OfferStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var chapter in budget.Chapters)
        {
            offer.Chapters.Add(
                new OfferChapter
                {
                    ChapterName = chapter.Name,
                    EstimatedWeeks = chapter.EstimatedWeeks,
                    ApproxMaterialQuantity = chapter.Activities.Sum(a => a.MaterialQuantity),
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }

        await _offerRepository.AddAsync(offer);

        return MapToDto(offer);
    }

    public async Task<OfferResponseDto> UpdateAsync(int id, UpdateOfferDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var offer =
            await _offerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Offer {id} not found.");

        if (offer.Status != OfferStatus.Draft)
        {
            throw new ForbiddenException("Solo se puede editar una oferta en estado Borrador.");
        }

        offer.OfferType = request.OfferType;
        offer.IssueDate = request.IssueDate;
        offer.ValidityDays = request.ValidityDays;
        offer.WorkLocation = request.WorkLocation;
        offer.WorkScope = request.WorkScope;
        offer.EstimatedStartDate = request.EstimatedStartDate;
        offer.EstimatedDurationWeeks = request.EstimatedDurationWeeks;
        offer.EstimatedDeliveryDate =
            request.EstimatedDeliveryDate
            ?? request.EstimatedStartDate.AddDays(request.EstimatedDurationWeeks * 7);
        offer.PaymentTerms = request.PaymentTerms;
        offer.Warranties = request.Warranties;
        offer.Exclusions = request.Exclusions;
        offer.TotalProjectPrice = request.TotalProjectPrice;
        offer.AgreedPercentage = request.AgreedPercentage;
        offer.PercentageIncludes = request.PercentageIncludes;
        offer.PercentageExcludes = request.PercentageExcludes;
        offer.PercentageCalculationMethod = request.PercentageCalculationMethod;
        offer.PaymentFrequency = request.PaymentFrequency;
        offer.UpdatedAt = DateTime.UtcNow;

        await _offerRepository.UpdateAsync(offer);

        return MapToDto(offer);
    }

    public async Task<OfferResponseDto> SendToClientAsync(int id)
    {
        var offer =
            await _offerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Offer {id} not found.");

        if (offer.Status != OfferStatus.Draft)
        {
            throw new ForbiddenException(
                "Solo se puede enviar al cliente una oferta en estado Borrador."
            );
        }

        offer.GeneratedPdfPath = _pdfGenerator.Generate(offer);
        offer.Status = OfferStatus.SentToClient;
        offer.UpdatedAt = DateTime.UtcNow;

        await _offerRepository.UpdateAsync(offer);

        return MapToDto(offer);
    }

    public async Task<OfferResponseDto> MarkAcceptedAsync(int id, int actingUserId)
    {
        var offer =
            await _offerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Offer {id} not found.");

        if (offer.Status != OfferStatus.SentToClient)
        {
            throw new ForbiddenException(
                "Solo se puede marcar como aceptada una oferta Enviada al cliente."
            );
        }

        offer.Status = OfferStatus.ClientAccepted;
        offer.UpdatedAt = DateTime.UtcNow;

        await _offerRepository.UpdateAsync(offer);

        await _budgetService.MarkClientApprovedAsync(offer.BudgetId, actingUserId);

        return MapToDto(offer);
    }

    public async Task<OfferResponseDto> RevertToDraftAsync(int id)
    {
        var offer =
            await _offerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Offer {id} not found.");

        if (offer.Status != OfferStatus.SentToClient)
        {
            throw new ForbiddenException(
                "Solo se puede regresar a Borrador una oferta Enviada al cliente."
            );
        }

        offer.Status = OfferStatus.Draft;
        offer.UpdatedAt = DateTime.UtcNow;

        await _offerRepository.UpdateAsync(offer);

        return MapToDto(offer);
    }

    public async Task DeleteAsync(int id)
    {
        var offer =
            await _offerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Offer {id} not found.");

        if (offer.Status != OfferStatus.Draft)
        {
            throw new ValidationAppException(
                "Solo se puede eliminar una oferta en estado Borrador; una vez enviada al cliente ya no se puede eliminar."
            );
        }

        await _offerRepository.DeleteAsync(offer);
    }

    public async Task<OfferResponseDto> GetByIdAsync(int id)
    {
        var offer =
            await _offerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Offer {id} not found.");
        return MapToDto(offer);
    }

    public async Task<PagedResult<OfferResponseDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        OfferStatus? status
    )
    {
        var (items, totalCount) = await _offerRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            status
        );

        return new PagedResult<OfferResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task<(string FilePath, string FileName)> GetPdfFileAsync(int id)
    {
        var offer =
            await _offerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Offer {id} not found.");

        if (string.IsNullOrEmpty(offer.GeneratedPdfPath) || !File.Exists(offer.GeneratedPdfPath))
        {
            throw new ValidationAppException(
                "El PDF de esta oferta aún no existe: primero debe enviarse al cliente (send-to-client)."
            );
        }

        return (offer.GeneratedPdfPath, $"{offer.OfferNumber}.pdf");
    }

    private async Task<string> GenerateOfferNumberAsync(int year)
    {
        var prefix = $"OF-{year}-";
        var count = await _offerRepository.CountByOfferNumberPrefixAsync(prefix);
        return $"{prefix}{count + 1:D4}";
    }

    private static OfferResponseDto MapToDto(Offer offer) =>
        new()
        {
            Id = offer.Id,
            BudgetId = offer.BudgetId,
            CustomerId = offer.CustomerId,
            OfferNumber = offer.OfferNumber,
            OfferType = offer.OfferType,
            IssueDate = offer.IssueDate,
            ValidityDays = offer.ValidityDays,
            WorkLocation = offer.WorkLocation,
            WorkScope = offer.WorkScope,
            EstimatedStartDate = offer.EstimatedStartDate,
            EstimatedDurationWeeks = offer.EstimatedDurationWeeks,
            EstimatedDeliveryDate = offer.EstimatedDeliveryDate,
            PaymentTerms = offer.PaymentTerms,
            Warranties = offer.Warranties,
            Exclusions = offer.Exclusions,
            TotalProjectPrice = offer.TotalProjectPrice,
            AgreedPercentage = offer.AgreedPercentage,
            PercentageIncludes = offer.PercentageIncludes,
            PercentageExcludes = offer.PercentageExcludes,
            PercentageCalculationMethod = offer.PercentageCalculationMethod,
            PaymentFrequency = offer.PaymentFrequency,
            Status = offer.Status,
            GeneratedPdfPath = offer.GeneratedPdfPath,
            CreatedByUserId = offer.CreatedByUserId,
            Chapters = offer
                .Chapters.OrderBy(c => c.Id)
                .Select(c => new OfferChapterResponseDto
                {
                    Id = c.Id,
                    ChapterName = c.ChapterName,
                    EstimatedWeeks = c.EstimatedWeeks,
                    ApproxMaterialQuantity = c.ApproxMaterialQuantity,
                })
                .ToList(),
        };
}
