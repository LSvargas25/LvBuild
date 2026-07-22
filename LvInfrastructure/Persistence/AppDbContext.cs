using LvDomain.Entities.Auth;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Budgets;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Inventory;
using LvDomain.Entities.Materials;
using LvDomain.Entities.Incidents;
using LvDomain.Entities.Offers;
using LvDomain.Entities.Payroll;
using LvDomain.Entities.Progress;
using LvDomain.Entities.Projects;
using LvDomain.Entities.SiteLogs;
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

    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferChapter> OfferChapters => Set<OfferChapter>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectEndDateHistory> ProjectEndDateHistories => Set<ProjectEndDateHistory>();
    public DbSet<ProjectWorker> ProjectWorkers => Set<ProjectWorker>();

    public DbSet<MaterialTicket> MaterialTickets => Set<MaterialTicket>();
    public DbSet<ProjectInventoryItem> ProjectInventoryItems => Set<ProjectInventoryItem>();

    public DbSet<SiteLog> SiteLogs => Set<SiteLog>();
    public DbSet<SiteLogWorker> SiteLogWorkers => Set<SiteLogWorker>();
    public DbSet<SiteLogMaterial> SiteLogMaterials => Set<SiteLogMaterial>();
    public DbSet<SiteLogEquipment> SiteLogEquipment => Set<SiteLogEquipment>();

    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<PayrollDetailPayment> PayrollDetailPayments => Set<PayrollDetailPayment>();

    public DbSet<ProjectProgress> ProjectProgresses => Set<ProjectProgress>();

    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentMaterial> IncidentMaterials => Set<IncidentMaterial>();
    public DbSet<IncidentWorker> IncidentWorkers => Set<IncidentWorker>();

    public DbSet<ProjectChapter> ProjectChapters => Set<ProjectChapter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
