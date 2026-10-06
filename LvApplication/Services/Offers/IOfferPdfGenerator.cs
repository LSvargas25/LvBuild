using LvDomain.Entities.Offers;

namespace LvApplication.Services.Offers;

public interface IOfferPdfGenerator
{
    /// <summary>Renders the offer as a PDF document in memory.</summary>
    byte[] Generate(Offer offer);
}
