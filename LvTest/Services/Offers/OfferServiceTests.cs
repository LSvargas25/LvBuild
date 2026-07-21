using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Offers;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Enums;
using LvTest.Common;

namespace LvTest.Services.Offers;

public class OfferServiceTests
{
    private static async Task<Customer> CreateProjectCustomerAsync(LvInfrastructure.Persistence.AppDbContext context)
    {
        var customer = new Customer
        {
            Name = "Project Customer",
            CustomerType = CustomerType.Project,
            Status = ActiveStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task<Branch> CreateBranchAsync(LvInfrastructure.Persistence.AppDbContext context, int operationsDirectorId)
    {
        var branch = new Branch
        {
            Name = "Test Office",
            City = "San Jose",
            Province = "San Jose",
            Status = BranchStatus.Active,
            BranchType = BranchType.Office,
            OperationsDirectorId = operationsDirectorId,
            CreatedAt = DateTime.UtcNow
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private static async Task<Budget> CreateSentBudgetAsync(LvInfrastructure.Persistence.AppDbContext context, int customerId, int branchId, int createdByUserId)
    {
        var budget = new Budget
        {
            CustomerId = customerId,
            BranchId = branchId,
            Name = "Edificio Test",
            Status = BudgetStatus.Sent,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 1000,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            Chapters = new List<BudgetChapter>
            {
                new()
                {
                    Name = "Cimentacion",
                    Order = 1,
                    EstimatedWeeks = 3,
                    TotalChapter = 500,
                    CreatedAt = DateTime.UtcNow,
                    Activities = new List<BudgetActivity>
                    {
                        new()
                        {
                            Description = "Excavacion",
                            MaterialQuantity = 10,
                            MaterialCost = 200,
                            LaborCost = 100,
                            EquipmentCost = 50,
                            TotalActivity = 350,
                            CreatedAt = DateTime.UtcNow
                        },
                        new()
                        {
                            Description = "Relleno",
                            MaterialQuantity = 5,
                            MaterialCost = 100,
                            LaborCost = 40,
                            EquipmentCost = 10,
                            TotalActivity = 150,
                            CreatedAt = DateTime.UtcNow
                        }
                    }
                }
            }
        };

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    private static async Task<Budget> CreateDraftBudgetAsync(LvInfrastructure.Persistence.AppDbContext context, int customerId, int branchId, int createdByUserId)
    {
        var budget = new Budget
        {
            CustomerId = customerId,
            BranchId = branchId,
            Name = "Edificio Draft",
            Status = BudgetStatus.Draft,
            UtilityPercentage = 10,
            IndirectCostsTotal = 50,
            TotalBudget = 500,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    private static CreateOfferDto BuildTurnkeyCreateDto(int budgetId) => new()
    {
        BudgetId = budgetId,
        OfferType = OfferType.Turnkey,
        IssueDate = new DateTime(2026, 1, 10),
        ValidityDays = 30,
        WorkLocation = "San Jose Centro",
        WorkScope = "Construccion de edificio de 3 niveles",
        EstimatedStartDate = new DateTime(2026, 2, 1),
        EstimatedDurationWeeks = 10,
        PaymentTerms = "50% inicio, 50% entrega",
        Warranties = "1 año estructural",
        Exclusions = "No incluye mobiliario",
        TotalProjectPrice = 100000m
    };

    private static CreateOfferDto BuildPercentageCreateDto(int budgetId) => new()
    {
        BudgetId = budgetId,
        OfferType = OfferType.Percentage,
        IssueDate = new DateTime(2026, 1, 10),
        ValidityDays = 30,
        WorkLocation = "San Jose Centro",
        WorkScope = "Construccion de edificio de 3 niveles",
        EstimatedStartDate = new DateTime(2026, 2, 1),
        EstimatedDurationWeeks = 10,
        PaymentTerms = "Contra avance",
        Warranties = "1 año estructural",
        Exclusions = "No incluye mobiliario",
        AgreedPercentage = 15m,
        PercentageIncludes = "Mano de obra y administracion",
        PercentageExcludes = "Materiales",
        PercentageCalculationMethod = "Sobre costo directo",
        PaymentFrequency = PaymentFrequency.Monthly
    };

    private static string CreateTempLogoFile()
    {
        // Minimal valid 1x1 transparent PNG, just enough for QuestPDF to load as an image.
        const string base64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
        var path = Path.Combine(Path.GetTempPath(), "LvTestLogos", $"{Guid.NewGuid()}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Convert.FromBase64String(base64Png));
        return path;
    }

    private static UpdateOfferDto BuildUpdateDtoFrom(OfferResponseDto offer) => new()
    {
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
        PaymentFrequency = offer.PaymentFrequency
    };

    [Fact]
    public async Task CreateAsync_BudgetNotSent_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-notsent@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-notsent@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateDraftBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var act = async () => await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_Valid_GeneratesOfferNumberCopiesChaptersAndCalculatesDeliveryDate()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-create@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-create@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var result = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        result.OfferNumber.Should().Be("OF-2026-0001");
        result.CustomerId.Should().Be(customer.Id);
        result.Status.Should().Be(OfferStatus.Draft);
        result.Chapters.Should().HaveCount(1);
        result.Chapters[0].ChapterName.Should().Be("Cimentacion");
        result.Chapters[0].EstimatedWeeks.Should().Be(3);
        result.Chapters[0].ApproxMaterialQuantity.Should().Be(15m);
        result.EstimatedDeliveryDate.Should().Be(new DateTime(2026, 2, 1).AddDays(10 * 7));
    }

    [Fact]
    public async Task CreateAsync_SecondActiveOfferForSameBudget_ThrowsConflictException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-dup@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-dup@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        var act = async () => await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_TurnkeyWithPercentageFields_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-crosstk@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-crosstk@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var dto = BuildTurnkeyCreateDto(budget.Id);
        dto.AgreedPercentage = 10m;

        var act = async () => await service.CreateAsync(dto, creator.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CreateAsync_PercentageWithTotalProjectPrice_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-crosspct@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-crosspct@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var dto = BuildPercentageCreateDto(budget.Id);
        dto.TotalProjectPrice = 5000m;

        var act = async () => await service.CreateAsync(dto, creator.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task UpdateAsync_InDraft_ModifiesFields()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-updateok@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-updateok@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        var updateDto = BuildUpdateDtoFrom(created);
        updateDto.WorkLocation = "Cartago Centro";
        updateDto.WorkScope = "Construccion de edificio de 5 niveles";
        updateDto.ValidityDays = 45;
        updateDto.EstimatedDurationWeeks = 12;
        updateDto.EstimatedDeliveryDate = null;
        updateDto.TotalProjectPrice = 150000m;

        var updated = await service.UpdateAsync(created.Id, updateDto);

        updated.Status.Should().Be(OfferStatus.Draft);
        updated.WorkLocation.Should().Be("Cartago Centro");
        updated.WorkScope.Should().Be("Construccion de edificio de 5 niveles");
        updated.ValidityDays.Should().Be(45);
        updated.EstimatedDurationWeeks.Should().Be(12);
        updated.TotalProjectPrice.Should().Be(150000m);
        // EstimatedDeliveryDate was sent as null, so it must be recalculated from the new start date/duration.
        updated.EstimatedDeliveryDate.Should().Be(updated.EstimatedStartDate.AddDays(12 * 7));
    }

    [Fact]
    public async Task UpdateAsync_NotDraft_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-update@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-update@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);
        await service.SendToClientAsync(created.Id);

        var act = async () => await service.UpdateAsync(created.Id, BuildUpdateDtoFrom(created));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SendToClientAsync_FromDraft_GeneratesPdfFileOnDisk()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-send@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-send@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var pdfFolder = Path.Combine(Path.GetTempPath(), "LvTestOffers", Guid.NewGuid().ToString());
        var service = ServiceFactory.CreateOfferService(context, pdfFolder);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        var result = await service.SendToClientAsync(created.Id);

        result.Status.Should().Be(OfferStatus.SentToClient);
        result.GeneratedPdfPath.Should().NotBeNullOrWhiteSpace();
        File.Exists(result.GeneratedPdfPath!).Should().BeTrue();
    }

    [Fact]
    public async Task GetPdfFileAsync_StillDraft_ThrowsValidationExceptionWithClearMessage()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-pdfdraft@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-pdfdraft@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        var act = async () => await service.GetPdfFileAsync(created.Id);

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("enviarse al cliente");
    }

    [Fact]
    public async Task GetPdfFileAsync_AfterSendToClient_ReturnsExistingFilePathAndFileName()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-pdfsent@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-pdfsent@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var pdfFolder = Path.Combine(Path.GetTempPath(), "LvTestOffers", Guid.NewGuid().ToString());
        var service = ServiceFactory.CreateOfferService(context, pdfFolder);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);
        var sent = await service.SendToClientAsync(created.Id);

        var (filePath, fileName) = await service.GetPdfFileAsync(created.Id);

        filePath.Should().Be(sent.GeneratedPdfPath);
        fileName.Should().Be($"{sent.OfferNumber}.pdf");
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task SendToClientAsync_PercentageOffer_GeneratesPdfFileOnDisk()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-sendpct@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-sendpct@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var pdfFolder = Path.Combine(Path.GetTempPath(), "LvTestOffers", Guid.NewGuid().ToString());
        var service = ServiceFactory.CreateOfferService(context, pdfFolder);

        var created = await service.CreateAsync(BuildPercentageCreateDto(budget.Id), creator.Id);

        var result = await service.SendToClientAsync(created.Id);

        result.Status.Should().Be(OfferStatus.SentToClient);
        result.GeneratedPdfPath.Should().NotBeNullOrWhiteSpace();
        File.Exists(result.GeneratedPdfPath!).Should().BeTrue();
    }

    [Fact]
    public async Task SendToClientAsync_WithConfiguredLogoPath_GeneratesPdfIncludingLogo()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-logo@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-logo@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var pdfFolder = Path.Combine(Path.GetTempPath(), "LvTestOffers", Guid.NewGuid().ToString());
        var logoPath = CreateTempLogoFile();
        var service = ServiceFactory.CreateOfferService(context, pdfFolder, logoPath);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        var result = await service.SendToClientAsync(created.Id);

        result.GeneratedPdfPath.Should().NotBeNullOrWhiteSpace();
        File.Exists(result.GeneratedPdfPath!).Should().BeTrue();
    }

    [Fact]
    public async Task SendToClientAsync_NotDraft_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-sendinvalid@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-sendinvalid@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);
        await service.SendToClientAsync(created.Id);

        var act = async () => await service.SendToClientAsync(created.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task MarkAcceptedAsync_NotSentToClient_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-acceptinvalid@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-acceptinvalid@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-offer-acceptinvalid@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        var act = async () => await service.MarkAcceptedAsync(created.Id, manager.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RevertToDraftAsync_NotSentToClient_ThrowsForbiddenException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-revertinvalid@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-revertinvalid@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        var act = async () => await service.RevertToDraftAsync(created.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task MarkAcceptedAsync_FromSentToClient_TransitionsOfferAndApprovesLinkedBudget()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-accept@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-accept@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-offer-accept@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);
        await service.SendToClientAsync(created.Id);

        var result = await service.MarkAcceptedAsync(created.Id, manager.Id);

        result.Status.Should().Be(OfferStatus.ClientAccepted);

        var updatedBudget = await context.Budgets.FindAsync(budget.Id);
        updatedBudget!.Status.Should().Be(BudgetStatus.ClientApproved);
    }

    [Fact]
    public async Task RevertToDraftAsync_FromSentToClient_TransitionsToDraft()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-revert@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-revert@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);
        await service.SendToClientAsync(created.Id);

        var result = await service.RevertToDraftAsync(created.Id);

        result.Status.Should().Be(OfferStatus.Draft);
    }

    [Fact]
    public async Task DeleteAsync_ClientAccepted_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-delacc@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-delacc@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-offer-delacc@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);
        await service.SendToClientAsync(created.Id);
        await service.MarkAcceptedAsync(created.Id, manager.Id);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_SentToClient_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-delsent@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-delsent@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);
        await service.SendToClientAsync(created.Id);

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsOffer()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-getbyid@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-getbyid@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var created = await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);

        var result = await service.GetByIdAsync(created.Id);

        result.Id.Should().Be(created.Id);
        result.OfferNumber.Should().Be(created.OfferNumber);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = ServiceFactory.CreateOfferService(context);

        var act = async () => await service.GetByIdAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_MoreRecordsThanPageSize_PaginatesCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-pagination@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-pagination@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateOfferService(context);

        for (var i = 1; i <= 5; i++)
        {
            var budget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
            await service.CreateAsync(BuildTurnkeyCreateDto(budget.Id), creator.Id);
        }

        var firstPage = await service.GetAllAsync(pageNumber: 1, pageSize: 2, status: null);
        var thirdPage = await service.GetAllAsync(pageNumber: 3, pageSize: 2, status: null);

        firstPage.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(5);
        thirdPage.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_FilteredByStatus_OnlyReturnsMatchingOffers()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-statusfilter@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-statusfilter@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var draftBudget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var draftOffer = await service.CreateAsync(BuildTurnkeyCreateDto(draftBudget.Id), creator.Id);

        var sentBudget = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var sentOffer = await service.CreateAsync(BuildTurnkeyCreateDto(sentBudget.Id), creator.Id);
        await service.SendToClientAsync(sentOffer.Id);

        var result = await service.GetAllAsync(pageNumber: 1, pageSize: 10, status: OfferStatus.SentToClient);

        result.TotalCount.Should().Be(1);
        result.Items.Should().OnlyContain(o => o.Id == sentOffer.Id);
        result.Items.Should().NotContain(o => o.Id == draftOffer.Id);
    }

    [Fact]
    public async Task DeleteAsync_Draft_DeletesSuccessfully()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-offer-del@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "pa-offer-del@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var budgetDraftDelete = await CreateSentBudgetAsync(context, customer.Id, branch.Id, creator.Id);
        var service = ServiceFactory.CreateOfferService(context);

        var draftOffer = await service.CreateAsync(BuildTurnkeyCreateDto(budgetDraftDelete.Id), creator.Id);
        await service.DeleteAsync(draftOffer.Id);

        var getDraftAct = async () => await service.GetByIdAsync(draftOffer.Id);
        await getDraftAct.Should().ThrowAsync<NotFoundException>();
    }
}
