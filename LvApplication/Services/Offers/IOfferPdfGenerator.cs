using LvDomain.Entities.Offers;

namespace LvApplication.Services.Offers;

public interface IOfferPdfGenerator
{
    string Generate(Offer offer);
}
