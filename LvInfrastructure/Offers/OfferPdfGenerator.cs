using System.Globalization;
using LvApplication.Services.Offers;
using LvDomain.Entities.Offers;
using LvDomain.Enums;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LvInfrastructure.Offers;

public class OfferPdfGenerator : IOfferPdfGenerator
{
    // The PDF is read by Costa Rican customers: format numbers the local way regardless of
    // the server culture (containers usually run with the invariant culture).
    private static readonly CultureInfo CostaRicaCulture = CultureInfo.GetCultureInfo("es-CR");

    private readonly IConfiguration _configuration;

    static OfferPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public OfferPdfGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Rendered in memory on every request: the host disk (Render) is ephemeral, and the
    // offer data is the source of truth, so there is no file to keep in sync.
    public byte[] Generate(Offer offer)
    {
        var companyName = _configuration["Company:Name"] ?? "LV Construcciones";
        var logoPath = _configuration["Company:LogoPath"];

        return Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header()
                        .Column(column =>
                        {
                            column
                                .Item()
                                .Row(row =>
                                {
                                    if (
                                        !string.IsNullOrWhiteSpace(logoPath)
                                        && File.Exists(logoPath)
                                    )
                                    {
                                        row.ConstantItem(70).Image(logoPath);
                                    }

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Text(companyName)
                                        .Bold()
                                        .FontSize(16);
                                });

                            column.Item().PaddingTop(5).LineHorizontal(1);
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            column.Spacing(6);

                            column
                                .Item()
                                .Text($"Oferta N.º {offer.OfferNumber}")
                                .Bold()
                                .FontSize(14);
                            column.Item().Text($"Fecha de emisión: {offer.IssueDate:dd/MM/yyyy}");
                            column.Item().Text($"Vigencia: {offer.ValidityDays} días");
                            column.Item().Text($"Ubicación de la obra: {offer.WorkLocation}");

                            column.Item().PaddingTop(5).Text("Alcance de la obra").Bold();
                            column.Item().Text(offer.WorkScope);

                            column.Item().PaddingTop(10).Text("Capítulos").Bold().FontSize(12);
                            column
                                .Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Capítulo").Bold();
                                        header.Cell().Text("Semanas").Bold();
                                        header.Cell().Text("Cant. aprox. materiales").Bold();
                                    });

                                    foreach (var chapter in offer.Chapters)
                                    {
                                        table.Cell().Text(chapter.ChapterName);
                                        table
                                            .Cell()
                                            .Text(
                                                chapter.EstimatedWeeks.ToString(CostaRicaCulture)
                                            );
                                        table
                                            .Cell()
                                            .Text(
                                                chapter.ApproxMaterialQuantity?.ToString(
                                                    "N2",
                                                    CostaRicaCulture
                                                ) ?? "-"
                                            );
                                    }
                                });

                            column
                                .Item()
                                .PaddingTop(10)
                                .Text(
                                    $"Fecha estimada de inicio: {offer.EstimatedStartDate:dd/MM/yyyy}"
                                );
                            column
                                .Item()
                                .Text($"Duración estimada: {offer.EstimatedDurationWeeks} semanas");
                            column
                                .Item()
                                .Text(
                                    $"Fecha estimada de entrega: {offer.EstimatedDeliveryDate:dd/MM/yyyy}"
                                );

                            column
                                .Item()
                                .PaddingTop(10)
                                .Text("Condiciones comerciales")
                                .Bold()
                                .FontSize(12);
                            column.Item().Text($"Forma de pago: {offer.PaymentTerms}");
                            column.Item().Text($"Garantías: {offer.Warranties}");
                            column.Item().Text($"Exclusiones: {offer.Exclusions}");

                            if (offer.OfferType == OfferType.Turnkey)
                            {
                                column
                                    .Item()
                                    .PaddingTop(10)
                                    .Text($"Precio total del proyecto: {offer.TotalProjectPrice:C}")
                                    .Bold()
                                    .FontSize(13);
                            }
                            else
                            {
                                column
                                    .Item()
                                    .PaddingTop(10)
                                    .Text("Condiciones por porcentaje")
                                    .Bold()
                                    .FontSize(12);
                                column
                                    .Item()
                                    .Text($"Porcentaje acordado: {offer.AgreedPercentage}%");
                                column.Item().Text($"Qué incluye: {offer.PercentageIncludes}");
                                column.Item().Text($"Qué no incluye: {offer.PercentageExcludes}");
                                column
                                    .Item()
                                    .Text($"Forma de cálculo: {offer.PercentageCalculationMethod}");
                                column.Item().Text($"Frecuencia de pago: {offer.PaymentFrequency}");
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            })
            .GeneratePdf();
    }
}
