namespace LvApi;

/// <summary>Text shown at the top of the Swagger UI.</summary>
internal static class ApiDocumentation
{
    public const string Description = """
        REST API of **LvBuild**, an ERP for a Costa Rican construction company: budgets,
        commercial offers (PDF), projects, weekly site logs and payroll, incidents, warehouse
        movements and the stores' point of sale (cash registers, invoices, stock).

        ### Try it with the demo data

        1. `POST /api/auth/login` with a demo account, e.g.
           `{ "email": "gerencia@lvbuild.test", "password": "LvBuild#2026" }`
           (one account per role, all with the same password: `gerencia@`, `operaciones@`,
           `proyectos@`, `sucursal@`, `comercial@` + `lvbuild.test`).
        2. Copy `accessToken` from the response, click **Authorize** and paste it.
        3. Explore, e.g. `GET /api/projects` and then `GET /api/projects/{id}/finance?period=Month&date=...`.

        Roles: GeneralManager and OperationsDirector approve; ProjectAdmin prepares budgets,
        offers, site logs and payrolls; BranchAdmin and BusinessManager run the stores.
        Health: `GET /health` (liveness) and `GET /health/ready` (database).
        """;
}
