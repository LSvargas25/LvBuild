using LvApplication.Services.Auth;
using LvApplication.Services.Branches;
using LvApplication.Services.Budgets;
using LvApplication.Services.Customers;
using LvApplication.Services.Suppliers;
using LvApplication.Services.Workers;
using LvApplication.Validators.Auth;
using LvApplication.Validators.Branches;
using LvApplication.Validators.Budgets;
using LvApplication.Validators.Customers;
using LvApplication.Validators.Suppliers;
using LvApplication.Validators.Workers;
using LvInfrastructure.Auth;
using LvInfrastructure.Persistence;
using LvInfrastructure.Repositories.Auth;
using LvInfrastructure.Repositories.Branches;
using LvInfrastructure.Repositories.Budgets;
using LvInfrastructure.Repositories.Customers;
using LvInfrastructure.Repositories.Suppliers;
using LvInfrastructure.Repositories.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LvTest.Common;

public static class ServiceFactory
{
    public static AuthService CreateAuthService(AppDbContext context, IConfiguration? configuration = null)
    {
        configuration ??= TestConfigurationFactory.Create();

        return new AuthService(
            new UserRepository(context),
            new RefreshTokenRepository(context),
            new PasswordResetTokenRepository(context),
            new JwtTokenService(configuration),
            configuration,
            NullLogger<AuthService>.Instance,
            new LoginRequestDtoValidator(),
            new RefreshTokenRequestDtoValidator(),
            new ForgotPasswordRequestDtoValidator(),
            new ResetPasswordRequestDtoValidator());
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
            new RequestCorrectionDtoValidator());
}
