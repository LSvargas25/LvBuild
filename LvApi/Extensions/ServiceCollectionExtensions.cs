using FluentValidation;
using LvApplication.Services.Auth;
using LvApplication.Services.Branches;
using LvApplication.Services.Budgets;
using LvApplication.Services.Commercial;
using LvApplication.Services.Customers;
using LvApplication.Services.Finance;
using LvApplication.Services.Incidents;
using LvApplication.Services.Inventory;
using LvApplication.Services.Materials;
using LvApplication.Services.Notifications;
using LvApplication.Services.Offers;
using LvApplication.Services.Payroll;
using LvApplication.Services.Progress;
using LvApplication.Services.Projects;
using LvApplication.Services.SiteLogs;
using LvApplication.Services.Storage;
using LvApplication.Services.Suppliers;
using LvApplication.Services.Warehouse;
using LvApplication.Services.Workers;
using LvInfrastructure.Auth;
using LvInfrastructure.Offers;
using LvInfrastructure.Persistence;
using LvInfrastructure.Storage;
using LvInfrastructure.Repositories.Auth;
using LvInfrastructure.Repositories.Branches;
using LvInfrastructure.Repositories.Budgets;
using LvInfrastructure.Repositories.Commercial;
using LvInfrastructure.Repositories.Customers;
using LvInfrastructure.Repositories.Inventory;
using LvInfrastructure.Repositories.Materials;
using LvInfrastructure.Repositories.Incidents;
using LvInfrastructure.Repositories.Notifications;
using LvInfrastructure.Repositories.Offers;
using LvInfrastructure.Repositories.Payroll;
using LvInfrastructure.Repositories.Progress;
using LvInfrastructure.Repositories.Projects;
using LvInfrastructure.Repositories.SiteLogs;
using LvInfrastructure.Repositories.Suppliers;
using LvInfrastructure.Repositories.Warehouse;
using LvInfrastructure.Repositories.Workers;
using Microsoft.EntityFrameworkCore;

namespace LvApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IOfferPdfGenerator, OfferPdfGenerator>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IMaterialCatalogRepository, MaterialCatalogRepository>();
        services.AddScoped<IMaterialTicketRepository, MaterialTicketRepository>();
        services.AddScoped<IProjectInventoryItemRepository, ProjectInventoryItemRepository>();
        services.AddScoped<ISiteLogRepository, SiteLogRepository>();
        services.AddScoped<IPayrollRepository, PayrollRepository>();
        services.AddScoped<IProjectProgressRepository, ProjectProgressRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IProjectChapterRepository, ProjectChapterRepository>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IBranchInventoryRepository, BranchInventoryRepository>();
        services.AddScoped<IProductIncorporationTicketRepository, ProductIncorporationTicketRepository>();
        services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();

        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(LvApplication.Common.PagedResult<>).Assembly;

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IWorkerService, WorkerService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IMaterialTicketService, MaterialTicketService>();
        services.AddScoped<ISiteLogService, SiteLogService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IProjectProgressService, ProjectProgressService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IProjectChapterService, ProjectChapterService>();
        services.AddScoped<IProjectFinanceService, ProjectFinanceService>();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IBranchInventoryService, BranchInventoryService>();
        services.AddScoped<IProductIncorporationTicketService, ProductIncorporationTicketService>();
        services.AddScoped<ICashRegisterService, CashRegisterService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInventoryMovementService, InventoryMovementService>();

        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
