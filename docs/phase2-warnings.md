# Warnings pendientes (Fase 2)

Warnings de los analizadores de .NET (`AnalysisMode=Recommended`) que ya existian en `main` antes de la migracion a PostgreSQL. La rama `feat/postgres` no introduce ninguno nuevo; quedan documentados para corregirlos en la Fase 2.

Lista generada con un build limpio (`dotnet build --no-incremental`) el 2026-10-05: **78 warnings**. Las migraciones de EF Core se excluyen del analisis (`.editorconfig`, `generated_code = true`).

| Regla | Cantidad | Descripcion |
|---|---|---|
| [CA1861](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1861) | 56 | Arrays constantes como argumento (preferir static readonly) |
| [CA1305](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1305) | 7 | Falta IFormatProvider |
| [CA1716](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1716) | 6 | Identificador que coincide con palabra reservada |
| [CA1805](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1805) | 5 | Miembro inicializado explicitamente a su valor por defecto |
| [CA1848](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1848) | 2 | Usar delegados LoggerMessage |
| [CA1860](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1860) | 1 | Preferir Length/Count sobre Any() |
| [CA1873](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873) | 1 | Evaluacion costosa de argumento de log |

## CA1861

- `LvTest/Integration/WarehouseMovementFlowTests.cs` - lineas 115
- `LvTest/Services/Auth/UserServiceTests.cs` - lineas 26, 37, 109, 129, 149, 169, 207
- `LvTest/Services/Branches/BranchServiceTests.cs` - lineas 414, 466, 497, 516, 552, 858, 864
- `LvTest/Services/Commercial/ProductIncorporationTicketServiceTests.cs` - lineas 118, 152, 175, 207, 235, 266, 294, 299, 306, 339, 346, 375, 404
- `LvTest/Services/Commercial/ProductServiceTests.cs` - lineas 49, 72, 89, 108, 126, 129, 169, 176, 201, 208, 227, 231, 249, 253, 276, 283, 305
- `LvTest/Services/Projects/ProjectServiceTests.cs` - lineas 708, 752
- `LvTest/Services/Warehouse/InventoryMovementServiceTests.cs` - lineas 423, 466, 509, 545, 589, 613, 636, 638, 677

## CA1305

- `LvApi/Controllers/ApiControllerBase.cs` - lineas 11
- `LvApi/Controllers/Branches/BranchesController.cs` - lineas 91
- `LvApi/Program.cs` - lineas 19
- `LvApplication/Services/Commercial/InvoiceService.cs` - lineas 226
- `LvInfrastructure/Auth/JwtTokenService.cs` - lineas 34
- `LvInfrastructure/Offers/OfferPdfGenerator.cs` - lineas 109, 113

## CA1716

- `LvApplication/Services/Commercial/ICashRegisterRepository.cs` - lineas 9, 10, 11
- `LvApplication/Services/Finance/IProjectFinanceService.cs` - lineas 8
- `LvApplication/Services/Inventory/IMaterialTicketRepository.cs` - lineas 14
- `LvApplication/Services/SiteLogs/ISiteLogRepository.cs` - lineas 15

## CA1805

- `LvDomain/Entities/Auth/PasswordResetToken.cs` - lineas 12
- `LvDomain/Entities/Auth/RefreshToken.cs` - lineas 12
- `LvDomain/Entities/Auth/User.cs` - lineas 13
- `LvDomain/Entities/Branches/BranchIndicator.cs` - lineas 15, 16

## CA1848

- `LvApi/Middleware/ExceptionMiddleware.cs` - lineas 26
- `LvApplication/Services/Auth/AuthService.cs` - lineas 170

## CA1860

- `LvApplication/Services/Budgets/BudgetService.cs` - lineas 106

## CA1873

- `LvApplication/Services/Auth/AuthService.cs` - lineas 170
