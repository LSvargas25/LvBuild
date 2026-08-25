using FluentValidation;
using LvApplication.Common;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Workers;
using LvDomain.Entities.Workers;
using LvDomain.Enums;

namespace LvApplication.Services.Workers;

public class WorkerService : IWorkerService
{
    private readonly IWorkerRepository _workerRepository;
    private readonly IValidator<CreateWorkerDto> _createValidator;
    private readonly IValidator<UpdateWorkerDto> _updateValidator;

    public WorkerService(
        IWorkerRepository workerRepository,
        IValidator<CreateWorkerDto> createValidator,
        IValidator<UpdateWorkerDto> updateValidator
    )
    {
        _workerRepository = workerRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<WorkerResponseDto> CreateAsync(CreateWorkerDto request)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request);

        var worker = new Worker
        {
            Name = request.Name,
            PersonalId = request.PersonalId,
            PhoneNumber = request.PhoneNumber,
            Birthday = request.Birthday,
            Status = ActiveStatus.Active,
            Category = request.Category,
            Type = request.Type,
            HourlyRate = request.HourlyRate,
            BranchId = request.BranchId,
            CreatedAt = DateTime.UtcNow,
        };

        await _workerRepository.AddAsync(worker);

        return MapToDto(worker);
    }

    public async Task<WorkerResponseDto> UpdateAsync(int id, UpdateWorkerDto request)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request);

        var worker =
            await _workerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Worker {id} not found.");

        worker.Name = request.Name;
        worker.PersonalId = request.PersonalId;
        worker.PhoneNumber = request.PhoneNumber;
        worker.Birthday = request.Birthday;
        worker.Status = request.Status;
        worker.Category = request.Category;
        worker.Type = request.Type;
        worker.HourlyRate = request.HourlyRate;
        worker.BranchId = request.BranchId;
        worker.UpdatedAt = DateTime.UtcNow;

        await _workerRepository.UpdateAsync(worker);

        return MapToDto(worker);
    }

    public async Task<WorkerResponseDto> GetByIdAsync(int id)
    {
        var worker =
            await _workerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Worker {id} not found.");
        return MapToDto(worker);
    }

    public async Task<PagedResult<WorkerResponseDto>> GetAllAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _workerRepository.GetPagedAsync(pageNumber, pageSize);

        return new PagedResult<WorkerResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var worker =
            await _workerRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Worker {id} not found.");
        await _workerRepository.DeleteAsync(worker);
    }

    private static WorkerResponseDto MapToDto(Worker worker) =>
        new()
        {
            Id = worker.Id,
            Name = worker.Name,
            PersonalId = worker.PersonalId,
            PhoneNumber = worker.PhoneNumber,
            Birthday = worker.Birthday,
            Status = worker.Status,
            Category = worker.Category,
            Type = worker.Type,
            HourlyRate = worker.HourlyRate,
            BranchId = worker.BranchId,
        };
}
