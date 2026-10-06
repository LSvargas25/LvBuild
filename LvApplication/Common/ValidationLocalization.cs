using System.Globalization;
using System.Reflection;
using FluentValidation;

namespace LvApplication.Common;

/// <summary>
/// FluentValidation's built-in messages ("must not be empty"...) follow the thread culture,
/// which is invariant (English) in the container. The API answers in Spanish, so the messages
/// are pinned to Spanish and the property names shown in them are translated too.
/// </summary>
public static class ValidationLocalization
{
    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.Ordinal)
    {
        ["Activities"] = "Actividades",
        ["AgreedPercentage"] = "Porcentaje acordado",
        ["Amount"] = "Monto",
        ["AssignedSoldTotal"] = "Total vendido asignado",
        ["BranchAdminId"] = "Administrador de sucursal",
        ["BranchId"] = "Sucursal",
        ["BranchType"] = "Tipo de sucursal",
        ["CashRegisterId"] = "Caja",
        ["Category"] = "Categoría",
        ["ChapterId"] = "Capítulo",
        ["Chapters"] = "Capítulos",
        ["City"] = "Ciudad",
        ["ClosingBalance"] = "Saldo de cierre",
        ["Comment"] = "Comentario",
        ["CurrentPassword"] = "Contraseña actual",
        ["CustomerId"] = "Cliente",
        ["CustomerType"] = "Tipo de cliente",
        ["Description"] = "Descripción",
        ["DestinationBranchId"] = "Sucursal destino",
        ["DestinationProjectId"] = "Proyecto destino",
        ["Details"] = "Detalle",
        ["Discount"] = "Descuento",
        ["Email"] = "Correo",
        ["EstimatedDurationWeeks"] = "Duración estimada (semanas)",
        ["Exclusions"] = "Exclusiones",
        ["HourlyRate"] = "Tarifa por hora",
        ["HoursUsed"] = "Horas usadas",
        ["HoursWorked"] = "Horas trabajadas",
        ["MaterialId"] = "Material",
        ["Materials"] = "Materiales",
        ["Name"] = "Nombre",
        ["NewEndDate"] = "Nueva fecha de entrega",
        ["NewPassword"] = "Nueva contraseña",
        ["OfferId"] = "Oferta",
        ["OpeningBalance"] = "Saldo de apertura",
        ["OperationsDirectorId"] = "Director de operaciones",
        ["OriginBranchId"] = "Sucursal origen",
        ["Password"] = "Contraseña",
        ["PaymentFrequency"] = "Frecuencia de pago",
        ["PaymentTerms"] = "Condiciones de pago",
        ["Payments"] = "Pagos",
        ["PercentageCalculationMethod"] = "Método de cálculo del porcentaje",
        ["PercentageExcludes"] = "Exclusiones del porcentaje",
        ["PercentageIncludes"] = "Inclusiones del porcentaje",
        ["ProductId"] = "Producto",
        ["ProjectId"] = "Proyecto",
        ["Province"] = "Provincia",
        ["Quantity"] = "Cantidad",
        ["QuantityUsed"] = "Cantidad usada",
        ["Reason"] = "Motivo",
        ["RefreshToken"] = "Token de sesión",
        ["RoleIds"] = "Roles",
        ["SiteLogId"] = "Bitácora",
        ["Sku"] = "SKU",
        ["StartDate"] = "Fecha de inicio",
        ["Status"] = "Estado",
        ["SupplierId"] = "Proveedor",
        ["TaskDescription"] = "Descripción de tareas",
        ["Token"] = "Token",
        ["TotalProjectPrice"] = "Precio total del proyecto",
        ["Type"] = "Tipo",
        ["UnitCost"] = "Costo unitario",
        ["UnitOfMeasure"] = "Unidad de medida",
        ["UnitPrice"] = "Precio unitario",
        ["ValidityDays"] = "Días de validez",
        ["Warranties"] = "Garantías",
        ["WeekEnd"] = "Fin de semana laboral",
        ["WeekStart"] = "Inicio de semana laboral",
        ["WorkLocation"] = "Ubicación de la obra",
        ["WorkScope"] = "Alcance de la obra",
        ["WorkerId"] = "Trabajador",
        ["Workers"] = "Trabajadores",
    };

    public static void UseSpanish()
    {
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("es");
        ValidatorOptions.Global.DisplayNameResolver = ResolveDisplayName;
    }

    /// <summary>Spanish label for a property.</summary>
    public static string? ResolveDisplayName(
        Type type,
        MemberInfo? member,
        System.Linq.Expressions.LambdaExpression? expression
    ) => member is not null && DisplayNames.TryGetValue(member.Name, out var label) ? label : null; // null = FluentValidation's default (the property name, split by words)
}
