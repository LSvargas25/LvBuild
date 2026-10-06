using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LvInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncludePendingPayrollsInPendingExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data only: payrolls still Pending now count as the project's pending expenses
            // (PayrollService adds them on create and moves them to direct expenses on payment).
            migrationBuilder.Sql(
                """
                UPDATE projects p
                SET pending_expenses = p.pending_expenses + t.total
                FROM (
                    SELECT project_id, SUM(total_payroll) AS total
                    FROM payrolls
                    WHERE status = 'Pending'
                    GROUP BY project_id
                ) t
                WHERE t.project_id = p.id;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Undo the backfill: take the still-pending payrolls out of the pending expenses.
            migrationBuilder.Sql(
                """
                UPDATE projects p
                SET pending_expenses = p.pending_expenses - t.total
                FROM (
                    SELECT project_id, SUM(total_payroll) AS total
                    FROM payrolls
                    WHERE status = 'Pending'
                    GROUP BY project_id
                ) t
                WHERE t.project_id = p.id;
                """
            );
        }
    }
}
