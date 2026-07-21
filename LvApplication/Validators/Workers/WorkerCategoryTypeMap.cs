using LvDomain.Enums;

namespace LvApplication.Validators.Workers;

internal static class WorkerCategoryTypeMap
{
    public static readonly Dictionary<WorkerCategory, WorkerType[]> Map = new()
    {
        [WorkerCategory.Office] = new[] { WorkerType.Administration, WorkerType.Engineer, WorkerType.Architect },
        [WorkerCategory.Construction] = new[]
        {
            WorkerType.SiteForeman, WorkerType.Laborer, WorkerType.ConstructionHelper, WorkerType.HeavyEquipmentOperator
        },
        [WorkerCategory.Commercial] = new[] { WorkerType.BusinessManager, WorkerType.Salesperson },
        [WorkerCategory.Storage] = new[] { WorkerType.WarehouseKeeper, WorkerType.Transporter }
    };

    public static bool IsValidCombination(WorkerCategory category, WorkerType type) =>
        Map.TryGetValue(category, out var types) && types.Contains(type);
}
