using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Suppliers;
using LvDomain.Entities.Workers;
using Microsoft.EntityFrameworkCore;

namespace LvInfrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Worker> Workers => Set<Worker>();

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BranchIndicator> BranchIndicators => Set<BranchIndicator>();

    public DbSet<MaterialCatalog> MaterialCatalogs => Set<MaterialCatalog>();

    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetChapter> BudgetChapters => Set<BudgetChapter>();
    public DbSet<BudgetActivity> BudgetActivities => Set<BudgetActivity>();
    public DbSet<BudgetActivityMaterial> BudgetActivityMaterials => Set<BudgetActivityMaterial>();
    public DbSet<BudgetActivityEquipment> BudgetActivityEquipment => Set<BudgetActivityEquipment>();
    public DbSet<BudgetActivityLabor> BudgetActivityLabor => Set<BudgetActivityLabor>();
    public DbSet<BudgetHistory> BudgetHistories => Set<BudgetHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
