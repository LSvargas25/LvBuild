using LvApplication.Common;
using LvApplication.DTOs.Offers;
using LvDomain.Enums;

namespace LvApplication.Services.Offers;

public interface IOfferService
{
    Task<OfferResponseDto> CreateAsync(CreateOfferDto request, int createdByUserId);
    Task<OfferResponseDto> UpdateAsync(int id, UpdateOfferDto request);
    Task<OfferResponseDto> SendToClientAsync(int id);
    Task<OfferResponseDto> MarkAcceptedAsync(int id, int actingUserId);
    Task<OfferResponseDto> RevertToDraftAsync(int id);
    Task DeleteAsync(int id);
    Task<OfferResponseDto> GetByIdAsync(int id);
    Task<PagedResult<OfferResponseDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        OfferStatus? status
    );
    Task<(byte[] Content, string FileName)> GetPdfAsync(int id);
}
