using LvApplication.Services.Auth;
using LvApplication.Services.Branches;
using LvApplication.Services.Budgets;
using LvApplication.Services.Customers;
using LvApplication.Services.Finance;
using LvApplication.Services.Inventory;
using LvApplication.Services.Incidents;
using LvApplication.Services.Offers;
using LvApplication.Services.Payroll;
using LvApplication.Services.Progress;
using LvApplication.Services.Projects;
using LvApplication.Services.SiteLogs;
using LvApplication.Services.Storage;
using LvApplication.Services.Suppliers;
using LvApplication.Services.Workers;
using LvApplication.Validators.Auth;
using LvApplication.Validators.Branches;
using LvApplication.Validators.Budgets;
using LvApplication.Validators.Customers;
using LvApplication.Validators.Inventory;
using LvApplication.Validators.Incidents;
using LvApplication.Validators.Offers;
using LvApplication.Validators.Payroll;
using LvApplication.Validators.Projects;
using LvApplication.Validators.SiteLogs;
using LvApplication.Validators.Suppliers;
using LvApplication.Validators.Workers;
using LvInfrastructure.Auth;
using LvInfrastructure.Offers;
using LvInfrastructure.Persistence;
using LvInfrastructure.Storage;
using LvInfrastructure.Repositories.Auth;
using LvInfrastructure.Repositories.Branches;
using LvInfrastructure.Repositories.Budgets;
using LvInfrastructure.Repositories.Customers;
using LvInfrastructure.Repositories.Inventory;
using LvInfrastructure.Repositories.Materials;
using LvInfrastructure.Repositories.Incidents;
using LvInfrastructure.Repositories.Offers;
using LvInfrastructure.Repositories.Payroll;
using LvInfrastructure.Repositories.Progress;
using LvInfrastructure.Repositories.Projects;
using LvInfrastructure.Repositories.SiteLogs;
using LvInfrastructure.Repositories.Suppliers;
using LvInfrastructure.Repositories.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace LvTest.Common;

public static class ServiceFactory
{
    public static AuthService CreateAuthService(
        AppDbContext context,
        IConfiguration? configuration = null,
        IFileStorageService? fileStorageService = null,
        IHostEnvironment? hostEnvironment = null)
    {
        string? storagePath = null;

        if (configuration is null)
        {
            storagePath = Path.Combine(Path.GetTempPath(), "LvTestStorage", Guid.NewGuid().ToString());
            configuration = TestConfigurationFactory.Create(new Dictionary<string, string?>
            {
                ["Storage:WebRootPath"] = storagePath
            });
        }

        return new AuthService(
            new UserRepository(context),
            new RefreshTokenRepository(context),
            new PasswordResetTokenRepository(context),
            new JwtTokenService(configuration),
            fileStorageService ?? new LocalFileStorageService(configuration),
            hostEnvironment ?? new FakeHostEnvironment(),
            configuration,
            NullLogger<AuthService>.Instance,
            new LoginRequestDtoValidator(),
            new RefreshTokenRequestDtoValidator(),
            new ForgotPasswordRequestDtoValidator(),
            new ResetPasswordRequestDtoValidator(),
            new UpdateProfileDtoValidator(),
            new ChangePasswordDtoValidator());
    }

    public static UserService CreateUserService(AppDbContext context) =>
        new(new UserRepository(context), new CreateUserDtoValidator());

    public static CustomerService CreateCustomerService(AppDbContext context) =>
        new(new CustomerRepository(context), new CreateCustomerDtoValidator(), new UpdateCustomerDtoValidator());

    public static SupplierService CreateSupplierService(AppDbContext context) =>
        new(new SupplierRepository(context), new CreateSupplierDtoValidator(), new UpdateSupplierDtoValidator());

    public static WorkerService CreateWorkerService(AppDbContext context) =>
        new(new WorkerRepository(context), new CreateWorkerDtoValidator(), new UpdateWorkerDtoValidator());

    public static BranchService CreateBranchService(AppDbContext context) =>
        new(
            new BranchRepository(context),
            new UserRepository(context),
            new CreateBranchDtoValidator(),
            new UpdateBranchDtoValidator());

    public static BudgetService CreateBudgetService(AppDbContext context) =>
        new(
            new BudgetRepository(context),
            new CreateBudgetDtoValidator(new CustomerRepository(context)),
            new UpdateBudgetDtoValidator(new CustomerRepository(context)),
            new RequestCorrectionDtoValidator(),
            new CancelBudgetDtoValidator());

    public static OfferService CreateOfferService(AppDbContext context, string? pdfOutputFolder = null, string? logoPath = null)
    {
        var overrides = new Dictionary<string, string?>
        {
            ["Storage:GeneratedOffersPath"] = pdfOutputFolder
                ?? Path.Combine(Path.GetTempPath(), "LvTestGeneratedOffers", Guid.NewGuid().ToString())
        };

        if (logoPath is not null)
        {
            overrides["Company:LogoPath"] = logoPath;
        }

        var configuration = TestConfigurationFactory.Create(overrides);

        return new OfferService(
            new OfferRepository(context),
            new BudgetRepository(context),
            CreateBudgetService(context),
            new OfferPdfGenerator(configuration),
            new CreateOfferDtoValidator(),
            new UpdateOfferDtoValidator());
    }

    public static ProjectChapterService CreateProjectChapterService(AppDbContext context) =>
        new(
            new ProjectChapterRepository(context),
            new MaterialTicketRepository(context),
            new PayrollRepository(context),
            new IncidentRepository(context));

    public static ProjectService CreateProjectService(AppDbContext context) =>
        new(
            new ProjectRepository(context),
            new OfferRepository(context),
            new BudgetRepository(context),
            new BranchRepository(context),
            new WorkerRepository(context),
            new ProjectChapterRepository(context),
            new CreateProjectDtoValidator(),
            new UpdateEndDateDtoValidator(),
            new AssignWorkerDtoValidator());

    public static MaterialTicketService CreateMaterialTicketService(AppDbContext context) =>
        new(
            new MaterialTicketRepository(context),
            new ProjectInventoryItemRepository(context),
            new ProjectRepository(context),
            new BudgetRepository(context),
            new SupplierRepository(context),
            new MaterialCatalogRepository(context),
            CreateProjectChapterService(context),
            new CreateMaterialTicketDtoValidator(),
            new UpdateMaterialTicketDtoValidator());

    public static ProjectProgressService CreateProjectProgressService(AppDbContext context) =>
        new(
            new ProjectProgressRepository(context),
            new SiteLogRepository(context),
            new ProjectRepository(context),
            new OfferRepository(context));

    public static SiteLogService CreateSiteLogService(AppDbContext context) =>
        new(
            new SiteLogRepository(context),
            new ProjectRepository(context),
            new ProjectInventoryItemRepository(context),
            new BudgetRepository(context),
            CreateProjectProgressService(context),
            new CreateSiteLogDtoValidator(),
            new UpdateSiteLogDtoValidator());

    public static PayrollService CreatePayrollService(AppDbContext context) =>
        new(
            new PayrollRepository(context),
            CreateSiteLogService(context),
            new ProjectRepository(context),
            new BudgetRepository(context),
            CreateProjectService(context),
            CreateProjectChapterService(context),
            new CreatePayrollDtoValidator(),
            new UpdatePayrollDtoValidator());

    public static IncidentService CreateIncidentService(AppDbContext context) =>
        new(
            new IncidentRepository(context),
            new ProjectRepository(context),
            new BudgetRepository(context),
            new ProjectInventoryItemRepository(context),
            new WorkerRepository(context),
            CreateProjectChapterService(context),
            new CreateIncidentDtoValidator(),
            new UpdateIncidentDtoValidator());

    public static ProjectFinanceService CreateProjectFinanceService(AppDbContext context) =>
        new(
            new ProjectRepository(context),
            new MaterialTicketRepository(context),
            new SiteLogRepository(context));
}
