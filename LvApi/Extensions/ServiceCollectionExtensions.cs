using FluentValidation;
using LvApplication.Services.Auth;
using LvApplication.Services.Branches;
using LvApplication.Services.Budgets;
using LvApplication.Services.Customers;
using LvApplication.Services.Suppliers;
using LvApplication.Services.Workers;
using LvInfrastructure.Auth;
using LvInfrastructure.Persistence;
using LvInfrastructure.Repositories.Auth;
using LvInfrastructure.Repositories.Branches;
using LvInfrastructure.Repositories.Budgets;
using LvInfrastructure.Repositories.Customers;
using LvInfrastructure.Repositories.Suppliers;
using LvInfrastructure.Repositories.Workers;
using Microsoft.EntityFrameworkCore;

namespace LvApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(LvApplication.Common.Result<>).Assembly;

        services.AddAutoMapper(cfg => { }, applicationAssembly);
        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IWorkerService, WorkerService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IBudgetService, BudgetService>();

        return services;
    }
}
