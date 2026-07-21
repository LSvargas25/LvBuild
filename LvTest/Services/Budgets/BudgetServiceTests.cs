using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvApplication.DTOs.Budgets;
using LvDomain.Entities.Branches;
using LvDomain.Entities.Customers;
using LvDomain.Entities.Materials;
using LvDomain.Enums;
using LvTest.Common;

namespace LvTest.Services.Budgets;

public class BudgetServiceTests
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

    private static async Task<Customer> CreateNonProjectCustomerAsync(LvInfrastructure.Persistence.AppDbContext context)
    {
        var customer = new Customer
        {
            Name = "Commercial Customer",
            CustomerType = CustomerType.Commercial,
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

    private static async Task<MaterialCatalog> CreateMaterialAsync(LvInfrastructure.Persistence.AppDbContext context, string name)
    {
        var material = new MaterialCatalog { Name = name, CreatedAt = DateTime.UtcNow };
        context.MaterialCatalogs.Add(material);
        await context.SaveChangesAsync();
        return material;
    }

    private static CreateBudgetDto BuildCreateDto(int customerId, int branchId, bool withChapter = true) => new()
    {
        CustomerId = customerId,
        BranchId = branchId,
        Name = "New Building Project",
        UtilityPercentage = 10,
        IndirectCostsTotal = 50,
        Chapters = withChapter
            ? new List<BudgetChapterDto>
            {
                new()
                {
                    Name = "Cimentacion",
                    Order = 1,
                    EstimatedWeeks = 2,
                    Activities = new List<BudgetActivityDto>
                    {
                        new()
                        {
                            Description = "Excavacion",
                            MaterialQuantity = 10,
                            MaterialCost = 100,
                            LaborCost = 50,
                            EquipmentCost = 25
                        }
                    }
                }
            }
            : new List<BudgetChapterDto>()
    };

    [Fact]
    public async Task CreateAsync_CreatesInDraftWithOneHistoryEntry()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-budget-create@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-create@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var result = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        result.Status.Should().Be(BudgetStatus.Draft);

        var history = await service.GetHistoryAsync(result.Id);
        history.Should().HaveCount(1);
        history[0].PreviousStatus.Should().BeNull();
        history[0].NewStatus.Should().Be(BudgetStatus.Draft);
    }

    [Fact]
    public async Task CreateAsync_CustomerNotProjectType_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-badcustomer@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-badcustomer@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateNonProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var act = async () => await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("El presupuesto solo puede asociarse a un cliente de tipo Proyecto");
    }

    [Fact]
    public async Task UpdateAsync_InDraft_RecalculatesTotalsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-recalc@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-recalc@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        // Initial: 1 activity (100+50+25=175) => chapter=175; (175+50)*1.10 = 247.5
        created.TotalBudget.Should().Be(247.5m);

        var updateDto = new UpdateBudgetDto
        {
            CustomerId = customer.Id,
            BranchId = branch.Id,
            Name = "New Building Project",
            UtilityPercentage = 20,
            IndirectCostsTotal = 100,
            Chapters = new List<BudgetChapterDto>
            {
                new()
                {
                    Id = created.Chapters[0].Id,
                    Name = "Cimentacion",
                    Order = 1,
                    EstimatedWeeks = 2,
                    Activities = new List<BudgetActivityDto>
                    {
                        new()
                        {
                            Id = created.Chapters[0].Activities[0].Id,
                            Description = "Excavacion",
                            MaterialQuantity = 10,
                            MaterialCost = 200,
                            LaborCost = 100,
                            EquipmentCost = 50
                        }
                    }
                },
                new()
                {
                    Name = "Estructura",
                    Order = 2,
                    EstimatedWeeks = 3,
                    Activities = new List<BudgetActivityDto>
                    {
                        new()
                        {
                            Description = "Columnas",
                            MaterialQuantity = 5,
                            MaterialCost = 80,
                            LaborCost = 20,
                            EquipmentCost = 0
                        }
                    }
                }
            }
        };

        var updated = await service.UpdateAsync(created.Id, updateDto);

        // Chapter1 activity total = 200+100+50 = 350; Chapter2 activity total = 80+20+0 = 100
        // chapters sum = 450; (450+100)*1.20 = 660
        updated.Chapters.Should().HaveCount(2);
        updated.TotalBudget.Should().Be(660m);
    }

    [Theory]
    [InlineData(BudgetStatus.Review)]
    [InlineData(BudgetStatus.Sent)]
    [InlineData(BudgetStatus.ClientApproved)]
    public async Task UpdateAsync_WhenNotDraftOrCorrection_ThrowsForbiddenException(BudgetStatus targetStatus)
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, $"director-forbidden-{targetStatus}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, $"projectadmin-forbidden-{targetStatus}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-forbidden-{targetStatus}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(created.Id, creator.Id);

        if (targetStatus is BudgetStatus.Sent or BudgetStatus.ClientApproved)
        {
            await service.ApproveInternalAsync(created.Id, manager.Id);
        }

        if (targetStatus is BudgetStatus.ClientApproved)
        {
            await service.MarkClientApprovedAsync(created.Id, manager.Id);
        }

        var act = async () => await service.UpdateAsync(created.Id, BuildUpdateDtoFrom(created, customer.Id, branch.Id));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SubmitForReviewAsync_WithoutChapters_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-nosubmit@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-nosubmit@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id, withChapter: false), creator.Id);

        var act = async () => await service.SubmitForReviewAsync(created.Id, creator.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task SubmitForReviewAsync_Valid_TransitionsToReviewAndAddsHistory()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-submitok@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-submitok@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        var result = await service.SubmitForReviewAsync(created.Id, creator.Id);

        result.Status.Should().Be(BudgetStatus.Review);

        var history = await service.GetHistoryAsync(created.Id);
        history.Should().HaveCount(2);
    }

    [Fact]
    public async Task ApproveInternalAsync_FromReview_TransitionsToSentAndAddsHistory()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-approve@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-approve@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-approve@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(created.Id, creator.Id);

        var result = await service.ApproveInternalAsync(created.Id, manager.Id);

        result.Status.Should().Be(BudgetStatus.Sent);

        var history = await service.GetHistoryAsync(created.Id);
        history.Should().HaveCount(3);
    }

    [Fact]
    public async Task RequestCorrectionAsync_WithoutComment_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-nocorrectioncomment@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-nocorrectioncomment@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-nocorrectioncomment@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(created.Id, creator.Id);

        var act = async () => await service.RequestCorrectionAsync(created.Id, new RequestCorrectionDto { Comment = "" }, manager.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task RequestCorrectionAsync_WithComment_TransitionsToCorrectionAndStoresComment()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-correction@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-correction@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-correction@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(created.Id, creator.Id);

        var result = await service.RequestCorrectionAsync(created.Id, new RequestCorrectionDto { Comment = "Ajustar precios de materiales" }, manager.Id);

        result.Status.Should().Be(BudgetStatus.Correction);

        var history = await service.GetHistoryAsync(created.Id);
        history.Should().HaveCount(3);
        history[^1].Comment.Should().Be("Ajustar precios de materiales");
    }

    [Fact]
    public async Task FullCycle_CorrectionToReviewAgain_IsRepeatable()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-cycle@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-cycle@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-cycle@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        for (var i = 0; i < 2; i++)
        {
            var reviewResult = await service.SubmitForReviewAsync(created.Id, creator.Id);
            reviewResult.Status.Should().Be(BudgetStatus.Review);

            var correctionResult = await service.RequestCorrectionAsync(created.Id, new RequestCorrectionDto { Comment = $"Round {i}" }, manager.Id);
            correctionResult.Status.Should().Be(BudgetStatus.Correction);
        }

        var history = await service.GetHistoryAsync(created.Id);
        history.Should().HaveCount(1 + (2 * 2));
    }

    [Fact]
    public async Task WithdrawFromCommercialAsync_FromSent_TransitionsToCorrection()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-withdraw@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-withdraw@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-withdraw@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(created.Id, creator.Id);
        await service.ApproveInternalAsync(created.Id, manager.Id);

        var result = await service.WithdrawFromCommercialAsync(created.Id, new RequestCorrectionDto { Comment = "Retirar de comercial" }, manager.Id);

        result.Status.Should().Be(BudgetStatus.Correction);
    }

    [Fact]
    public async Task MarkClientApprovedAsync_FromSent_TransitionsToClientApproved()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-clientapproved@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-clientapproved@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-clientapproved@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(created.Id, creator.Id);
        await service.ApproveInternalAsync(created.Id, manager.Id);

        var result = await service.MarkClientApprovedAsync(created.Id, manager.Id);

        result.Status.Should().Be(BudgetStatus.ClientApproved);
    }

    [Fact]
    public async Task AfterClientApproved_AnyTransitionOrUpdate_AlwaysThrows()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-locked@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-locked@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-locked@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(created.Id, creator.Id);
        await service.ApproveInternalAsync(created.Id, manager.Id);
        await service.MarkClientApprovedAsync(created.Id, manager.Id);

        var updateAct = async () => await service.UpdateAsync(created.Id, BuildUpdateDtoFrom(created, customer.Id, branch.Id));
        var submitAct = async () => await service.SubmitForReviewAsync(created.Id, creator.Id);
        var approveAct = async () => await service.ApproveInternalAsync(created.Id, manager.Id);
        var correctionAct = async () => await service.RequestCorrectionAsync(created.Id, new RequestCorrectionDto { Comment = "x" }, manager.Id);
        var withdrawAct = async () => await service.WithdrawFromCommercialAsync(created.Id, new RequestCorrectionDto { Comment = "x" }, manager.Id);
        var approveAgainAct = async () => await service.MarkClientApprovedAsync(created.Id, manager.Id);

        await updateAct.Should().ThrowAsync<ForbiddenException>();
        await submitAct.Should().ThrowAsync<ForbiddenException>();
        await approveAct.Should().ThrowAsync<ForbiddenException>();
        await correctionAct.Should().ThrowAsync<ForbiddenException>();
        await withdrawAct.Should().ThrowAsync<ForbiddenException>();
        await approveAgainAct.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateAsync_WithMaterialsEquipmentAndLabor_PersistsSubCollections()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-budget-materials-create@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-budget-materials-create@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var cement = await CreateMaterialAsync(context, "Cemento");
        var service = ServiceFactory.CreateBudgetService(context);

        var createDto = BuildCreateDto(customer.Id, branch.Id);
        createDto.Chapters[0].Activities[0].Materials.Add(new BudgetActivityMaterialDto { MaterialId = cement.Id, UnitPrice = 15 });
        createDto.Chapters[0].Activities[0].Equipment.Add(new BudgetActivityEquipmentDto { EquipmentName = "Excavadora", UnitPrice = 200 });
        createDto.Chapters[0].Activities[0].Labor.Add(new BudgetActivityLaborDto { WorkerType = WorkerType.Laborer, HourlyRate = 5 });

        var result = await service.CreateAsync(createDto, creator.Id);

        var activity = result.Chapters[0].Activities[0];
        activity.Materials.Should().ContainSingle(m => m.MaterialId == cement.Id && m.UnitPrice == 15);
        activity.Equipment.Should().ContainSingle(e => e.EquipmentName == "Excavadora" && e.UnitPrice == 200);
        activity.Labor.Should().ContainSingle(l => l.WorkerType == WorkerType.Laborer && l.HourlyRate == 5);
    }

    [Fact]
    public async Task UpdateAsync_AddsAndRemovesMaterialsEquipmentAndLabor_SyncsCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-budget-materials-update@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-budget-materials-update@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var cement = await CreateMaterialAsync(context, "Cemento");
        var sand = await CreateMaterialAsync(context, "Arena");
        var service = ServiceFactory.CreateBudgetService(context);

        var createDto = BuildCreateDto(customer.Id, branch.Id);
        createDto.Chapters[0].Activities[0].Materials.Add(new BudgetActivityMaterialDto { MaterialId = cement.Id, UnitPrice = 15 });
        createDto.Chapters[0].Activities[0].Equipment.Add(new BudgetActivityEquipmentDto { EquipmentName = "Excavadora", UnitPrice = 200 });
        createDto.Chapters[0].Activities[0].Labor.Add(new BudgetActivityLaborDto { WorkerType = WorkerType.Laborer, HourlyRate = 5 });

        var created = await service.CreateAsync(createDto, creator.Id);
        var createdActivity = created.Chapters[0].Activities[0];

        var updateDto = new UpdateBudgetDto
        {
            CustomerId = customer.Id,
            BranchId = branch.Id,
            Name = created.Name,
            UtilityPercentage = created.UtilityPercentage,
            IndirectCostsTotal = created.IndirectCostsTotal,
            Chapters = new List<BudgetChapterDto>
            {
                new()
                {
                    Id = created.Chapters[0].Id,
                    Name = created.Chapters[0].Name,
                    Order = created.Chapters[0].Order,
                    EstimatedWeeks = created.Chapters[0].EstimatedWeeks,
                    Activities = new List<BudgetActivityDto>
                    {
                        new()
                        {
                            Id = createdActivity.Id,
                            Description = createdActivity.Description,
                            MaterialQuantity = createdActivity.MaterialQuantity,
                            MaterialCost = createdActivity.MaterialCost,
                            LaborCost = createdActivity.LaborCost,
                            EquipmentCost = createdActivity.EquipmentCost,
                            // Omitting the existing "Cemento" line and sending only "Arena" removes the former and adds the latter.
                            Materials = new List<BudgetActivityMaterialDto>
                            {
                                new() { MaterialId = sand.Id, UnitPrice = 8 }
                            },
                            // Sending the existing equipment Id with new values updates it in place.
                            Equipment = new List<BudgetActivityEquipmentDto>
                            {
                                new() { Id = createdActivity.Equipment[0].Id, EquipmentName = "Grua", UnitPrice = 250 }
                            },
                            // Sending an empty Labor list removes the only existing labor line.
                            Labor = new List<BudgetActivityLaborDto>()
                        }
                    }
                }
            }
        };

        var updated = await service.UpdateAsync(created.Id, updateDto);
        var updatedActivity = updated.Chapters[0].Activities[0];

        updatedActivity.Materials.Should().ContainSingle(m => m.MaterialId == sand.Id && m.UnitPrice == 8);
        updatedActivity.Materials.Should().NotContain(m => m.MaterialId == cement.Id);
        updatedActivity.Equipment.Should().ContainSingle(e => e.Id == createdActivity.Equipment[0].Id && e.EquipmentName == "Grua" && e.UnitPrice == 250);
        updatedActivity.Labor.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_MoreRecordsThanPageSize_PaginatesCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-budget-pagination@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-budget-pagination@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        for (var i = 1; i <= 5; i++)
        {
            await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        }

        var firstPage = await service.GetAllAsync(pageNumber: 1, pageSize: 2, status: null);
        var thirdPage = await service.GetAllAsync(pageNumber: 3, pageSize: 2, status: null);

        firstPage.Items.Should().HaveCount(2);
        firstPage.TotalCount.Should().Be(5);
        thirdPage.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_FilteredByStatus_OnlyReturnsMatchingBudgets()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-budget-statusfilter@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-budget-statusfilter@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var draft = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        var submitted = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(submitted.Id, creator.Id);

        var result = await service.GetAllAsync(pageNumber: 1, pageSize: 10, status: BudgetStatus.Review);

        result.TotalCount.Should().Be(1);
        result.Items.Should().OnlyContain(b => b.Id == submitted.Id);
        result.Items.Should().NotContain(b => b.Id == draft.Id);
    }

    [Theory]
    [InlineData(BudgetStatus.Draft)]
    [InlineData(BudgetStatus.Review)]
    [InlineData(BudgetStatus.Correction)]
    [InlineData(BudgetStatus.Sent)]
    public async Task CancelAsync_FromAllowedState_CancelsAndRecordsHistoryWithReason(BudgetStatus fromStatus)
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, $"director-cancel-{fromStatus}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, $"projectadmin-cancel-{fromStatus}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-cancel-{fromStatus}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        if (fromStatus is BudgetStatus.Review or BudgetStatus.Correction or BudgetStatus.Sent)
        {
            await service.SubmitForReviewAsync(created.Id, creator.Id);
        }

        if (fromStatus == BudgetStatus.Correction)
        {
            await service.RequestCorrectionAsync(created.Id, new RequestCorrectionDto { Comment = "Ajustar precios" }, manager.Id);
        }

        if (fromStatus == BudgetStatus.Sent)
        {
            await service.ApproveInternalAsync(created.Id, manager.Id);
        }

        var result = await service.CancelAsync(created.Id, new CancelBudgetDto { Reason = "El cliente desistió del proyecto" }, manager.Id);

        result.Status.Should().Be(BudgetStatus.Cancelled);

        var history = await service.GetHistoryAsync(created.Id);
        history.Last().NewStatus.Should().Be(BudgetStatus.Cancelled);
        history.Last().Reason.Should().Be("El cliente desistió del proyecto");
    }

    [Fact]
    public async Task CancelAsync_FromClientApproved_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-cancel-approved@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-cancel-approved@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-cancel-approved@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.SubmitForReviewAsync(created.Id, creator.Id);
        await service.ApproveInternalAsync(created.Id, manager.Id);
        await service.MarkClientApprovedAsync(created.Id, manager.Id);

        var act = async () => await service.CancelAsync(created.Id, new CancelBudgetDto { Reason = "x" }, manager.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-cancel-twice@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-cancel-twice@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-cancel-twice@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);
        await service.CancelAsync(created.Id, new CancelBudgetDto { Reason = "Primera cancelación" }, manager.Id);

        var act = async () => await service.CancelAsync(created.Id, new CancelBudgetDto { Reason = "Segunda cancelación" }, manager.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task CancelAsync_EmptyReason_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-cancel-noreason@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-cancel-noreason@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, "gm-cancel-noreason@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        var act = async () => await service.CancelAsync(created.Id, new CancelBudgetDto { Reason = "" }, manager.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task DeleteAsync_DraftWithoutHistory_DeletesSuccessfully()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-delete-clean@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-delete-clean@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        await service.DeleteAsync(created.Id);

        var stored = await context.Budgets.FindAsync(created.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DraftWithPriorTransitionHistory_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, "director-delete-hadhistory@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, "projectadmin-delete-hadhistory@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        // Simulate a budget that is currently Draft but previously left Draft at some point
        // (e.g. Draft -> Review -> Correction cycle), which the state machine itself never
        // routes back to literal Draft, but the Delete guard must still catch defensively.
        var budgetEntity = await context.Budgets.FindAsync(created.Id);
        context.BudgetHistories.Add(new LvDomain.Entities.Budgets.BudgetHistory
        {
            BudgetId = created.Id,
            UserId = creator.Id,
            PreviousStatus = BudgetStatus.Review,
            NewStatus = BudgetStatus.Draft,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var act = async () => await service.DeleteAsync(created.Id);

        var exception = await act.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Message.Should().Contain("Cancelar");
    }

    [Theory]
    [InlineData(BudgetStatus.Review)]
    [InlineData(BudgetStatus.Correction)]
    [InlineData(BudgetStatus.Sent)]
    [InlineData(BudgetStatus.ClientApproved)]
    [InlineData(BudgetStatus.Cancelled)]
    public async Task DeleteAsync_NonDraftState_ThrowsValidationException(BudgetStatus targetStatus)
    {
        using var context = TestDbContextFactory.Create();
        var director = await TestUserFactory.CreateAsync(context, $"director-delete-nondraft-{targetStatus}@example.com", roleId: TestUserFactory.OperationsDirectorRoleId);
        var creator = await TestUserFactory.CreateAsync(context, $"projectadmin-delete-nondraft-{targetStatus}@example.com", roleId: TestUserFactory.ProjectAdminRoleId);
        var manager = await TestUserFactory.CreateAsync(context, $"gm-delete-nondraft-{targetStatus}@example.com", roleId: TestUserFactory.GeneralManagerRoleId);
        var customer = await CreateProjectCustomerAsync(context);
        var branch = await CreateBranchAsync(context, director.Id);
        var service = ServiceFactory.CreateBudgetService(context);

        var created = await service.CreateAsync(BuildCreateDto(customer.Id, branch.Id), creator.Id);

        if (targetStatus == BudgetStatus.Cancelled)
        {
            await service.CancelAsync(created.Id, new CancelBudgetDto { Reason = "x" }, manager.Id);
        }
        else
        {
            await service.SubmitForReviewAsync(created.Id, creator.Id);

            if (targetStatus == BudgetStatus.Correction)
            {
                await service.RequestCorrectionAsync(created.Id, new RequestCorrectionDto { Comment = "Ajustar" }, manager.Id);
            }

            if (targetStatus is BudgetStatus.Sent or BudgetStatus.ClientApproved)
            {
                await service.ApproveInternalAsync(created.Id, manager.Id);
            }

            if (targetStatus == BudgetStatus.ClientApproved)
            {
                await service.MarkClientApprovedAsync(created.Id, manager.Id);
            }
        }

        var act = async () => await service.DeleteAsync(created.Id);

        await act.Should().ThrowAsync<ValidationAppException>();
    }

    private static UpdateBudgetDto BuildUpdateDtoFrom(BudgetResponseDto created, int customerId, int branchId) => new()
    {
        CustomerId = customerId,
        BranchId = branchId,
        Name = created.Name,
        UtilityPercentage = created.UtilityPercentage,
        IndirectCostsTotal = created.IndirectCostsTotal,
        Chapters = created.Chapters.Select(c => new BudgetChapterDto
        {
            Id = c.Id,
            Name = c.Name,
            Order = c.Order,
            EstimatedWeeks = c.EstimatedWeeks,
            Activities = c.Activities.Select(a => new BudgetActivityDto
            {
                Id = a.Id,
                Description = a.Description,
                MaterialQuantity = a.MaterialQuantity,
                MaterialCost = a.MaterialCost,
                LaborCost = a.LaborCost,
                EquipmentCost = a.EquipmentCost
            }).ToList()
        }).ToList()
    };
}
