using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LvInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    customer_type = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    city = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    phone_number = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    personal_id = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    email = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: true
                    ),
                    branch_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "material_catalogs",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    unit_of_measure = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_catalogs", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:IdentitySequenceOptions",
                            "'6', '1', '', '', 'False', '1'"
                        )
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "stored_files",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    file_name = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    content_type = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stored_files", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    city = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    phone_number = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    personal_id = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    email = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:IdentitySequenceOptions",
                            "'2', '1', '', '', 'False', '1'"
                        )
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                    email = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    last_login_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    blocked_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    profile_photo_file_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_stored_files_profile_photo_file_id",
                        column: x => x.profile_photo_file_id,
                        principalTable: "stored_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                    phone_number = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    email = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: true
                    ),
                    city = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    province = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    branch_type = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    operations_director_id = table.Column<int>(type: "integer", nullable: false),
                    branch_admin_id = table.Column<int>(type: "integer", nullable: true),
                    business_manager_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_branches_users_branch_admin_id",
                        column: x => x.branch_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_branches_users_business_manager_id",
                        column: x => x.business_manager_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_branches_users_operations_director_id",
                        column: x => x.operations_director_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    message = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    expires_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    used = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    description = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    sku = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    unit_of_measure = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    category = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    active_status = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    validated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    validated_date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_products_users_validated_by_user_id",
                        column: x => x.validated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(
                        type: "character varying(400)",
                        maxLength: 400,
                        nullable: false
                    ),
                    expires_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    revoked = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_ip = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "branch_indicators",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    profit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    losses = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    direct_expenses = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    indirect_expenses = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    total_workers = table.Column<int>(type: "integer", nullable: false),
                    total_materials = table.Column<int>(type: "integer", nullable: false),
                    last_updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_indicators", x => x.id);
                    table.ForeignKey(
                        name: "fk_branch_indicators_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "budgets",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    utility_percentage = table.Column<decimal>(
                        type: "numeric(5,2)",
                        nullable: false
                    ),
                    indirect_costs_total = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    total_budget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budgets", x => x.id);
                    table.ForeignKey(
                        name: "fk_budgets_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_budgets_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_budgets_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "cash_registers",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    opened_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    opening_date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    opening_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    closed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    closing_date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    closing_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    expected_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    difference = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_registers", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_registers_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_cash_registers_users_closed_by_user_id",
                        column: x => x.closed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_cash_registers_users_opened_by_user_id",
                        column: x => x.opened_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "workers",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    personal_id = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    phone_number = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    birthday = table.Column<DateTime>(type: "date", nullable: true),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    category = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    type = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    hourly_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    branch_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workers", x => x.id);
                    table.ForeignKey(
                        name: "fk_workers_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "branch_inventories",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    minimum_stock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_inventories", x => x.id);
                    table.ForeignKey(
                        name: "fk_branch_inventories_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_branch_inventories_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "product_incorporation_tickets",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    validated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    validated_date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_incorporation_tickets", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_incorporation_tickets_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_product_incorporation_tickets_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_product_incorporation_tickets_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_product_incorporation_tickets_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_product_incorporation_tickets_users_validated_by_user_id",
                        column: x => x.validated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "budget_chapters",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    budget_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                    order = table.Column<int>(type: "integer", nullable: false),
                    total_chapter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    estimated_weeks = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_chapters", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_chapters_budgets_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "budget_histories",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    budget_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    previous_status = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    new_status = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    comment = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    reason = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    timestamp = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_histories_budgets_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_budget_histories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "offers",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    budget_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    offer_number = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    offer_type = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    issue_date = table.Column<DateTime>(type: "date", nullable: false),
                    validity_days = table.Column<int>(type: "integer", nullable: false),
                    work_location = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    work_scope = table.Column<string>(type: "text", nullable: false),
                    estimated_start_date = table.Column<DateTime>(type: "date", nullable: false),
                    estimated_duration_weeks = table.Column<int>(type: "integer", nullable: false),
                    estimated_delivery_date = table.Column<DateTime>(type: "date", nullable: false),
                    payment_terms = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    warranties = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    exclusions = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    total_project_price = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: true
                    ),
                    agreed_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    percentage_includes = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    percentage_excludes = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    percentage_calculation_method = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    payment_frequency = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    status = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_offers_budgets_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_offers_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_offers_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    cash_register_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    invoice_number = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    payment_type = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoices_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_invoices_cash_registers_cash_register_id",
                        column: x => x.cash_register_id,
                        principalTable: "cash_registers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_invoices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_invoices_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "budget_activities",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    chapter_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    material_quantity = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    material_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    labor_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    equipment_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_activity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_activities_budget_chapters_chapter_id",
                        column: x => x.chapter_id,
                        principalTable: "budget_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "offer_chapters",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    offer_id = table.Column<int>(type: "integer", nullable: false),
                    chapter_name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                    estimated_weeks = table.Column<int>(type: "integer", nullable: false),
                    approx_material_quantity = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_chapters", x => x.id);
                    table.ForeignKey(
                        name: "fk_offer_chapters_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    offer_id = table.Column<int>(type: "integer", nullable: false),
                    budget_id = table.Column<int>(type: "integer", nullable: false),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    project_type = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: false),
                    weeks_counter = table.Column<int>(type: "integer", nullable: false),
                    total_worked_hours = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    workers_used_count = table.Column<int>(type: "integer", nullable: false),
                    materials_used_count = table.Column<int>(type: "integer", nullable: false),
                    current_direct_expenses = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    pending_expenses = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    current_profit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_projects_budgets_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_projects_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_projects_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_projects_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "invoice_details",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    invoice_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_details_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_invoice_details_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "invoice_payments",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    invoice_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_method = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    received_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_invoice_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_invoice_payments_users_received_by_user_id",
                        column: x => x.received_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "budget_activity_equipment",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    activity_id = table.Column<int>(type: "integer", nullable: false),
                    equipment_name = table.Column<string>(
                        type: "character varying(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_activity_equipment", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_activity_equipment_budget_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "budget_activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "budget_activity_labor",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    activity_id = table.Column<int>(type: "integer", nullable: false),
                    worker_type = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    hourly_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_activity_labor", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_activity_labor_budget_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "budget_activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "budget_activity_materials",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    activity_id = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budget_activity_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_budget_activity_materials_budget_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "budget_activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_budget_activity_materials_material_catalogs_material_id",
                        column: x => x.material_id,
                        principalTable: "material_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "date", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    approved_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    chapter_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incidents", x => x.id);
                    table.ForeignKey(
                        name: "fk_incidents_budget_chapters_chapter_id",
                        column: x => x.chapter_id,
                        principalTable: "budget_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_incidents_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_incidents_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_incidents_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    origin_branch_id = table.Column<int>(type: "integer", nullable: false),
                    destination_branch_id = table.Column<int>(type: "integer", nullable: true),
                    destination_project_id = table.Column<int>(type: "integer", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    sent_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    sent_date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    validated_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    validated_date = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.CheckConstraint(
                        "ck_inventory_movements_exactly_one_destination",
                        "(destination_branch_id IS NOT NULL AND destination_project_id IS NULL) OR (destination_branch_id IS NULL AND destination_project_id IS NOT NULL)"
                    );
                    table.ForeignKey(
                        name: "fk_inventory_movements_branches_destination_branch_id",
                        column: x => x.destination_branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_inventory_movements_branches_origin_branch_id",
                        column: x => x.origin_branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_inventory_movements_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_inventory_movements_projects_destination_project_id",
                        column: x => x.destination_project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_inventory_movements_users_sent_by_user_id",
                        column: x => x.sent_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_inventory_movements_users_validated_by_user_id",
                        column: x => x.validated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "material_tickets",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    invoice_photo_path = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: true
                    ),
                    material_name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    chapter_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_tickets", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_tickets_budget_chapters_chapter_id",
                        column: x => x.chapter_id,
                        principalTable: "budget_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_material_tickets_material_catalogs_material_id",
                        column: x => x.material_id,
                        principalTable: "material_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_material_tickets_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_material_tickets_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_material_tickets_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "project_chapters",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    chapter_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_sold_total = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    actual_cost_total = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    chapter_profit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    incident_count = table.Column<int>(type: "integer", nullable: false),
                    incident_percentage = table.Column<decimal>(
                        type: "numeric(5,2)",
                        nullable: true
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_chapters", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_chapters_budget_chapters_chapter_id",
                        column: x => x.chapter_id,
                        principalTable: "budget_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_project_chapters_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "project_end_date_histories",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    previous_date = table.Column<DateTime>(type: "date", nullable: false),
                    new_date = table.Column<DateTime>(type: "date", nullable: false),
                    reason = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_end_date_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_end_date_histories_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_project_end_date_histories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "project_inventory_items",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<int>(type: "integer", nullable: true),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    current_quantity = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    reference_unit_cost = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_inventory_items", x => x.id);
                    table.CheckConstraint(
                        "ck_project_inventory_items_exactly_one_catalog_reference",
                        "(material_id IS NOT NULL AND product_id IS NULL) OR (material_id IS NULL AND product_id IS NOT NULL)"
                    );
                    table.ForeignKey(
                        name: "fk_project_inventory_items_material_catalogs_material_id",
                        column: x => x.material_id,
                        principalTable: "material_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_project_inventory_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_project_inventory_items_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "project_workers",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    assigned_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_workers", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_workers_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_project_workers_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_project_workers_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "site_logs",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    week_start = table.Column<DateTime>(type: "date", nullable: false),
                    week_end = table.Column<DateTime>(type: "date", nullable: false),
                    task_description = table.Column<string>(type: "text", nullable: false),
                    pending_tasks = table.Column<string>(type: "text", nullable: true),
                    total_payroll = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_materials = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    progress_percentage = table.Column<decimal>(
                        type: "numeric(5,2)",
                        nullable: true
                    ),
                    status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    approved_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    chapter_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_logs_budget_chapters_chapter_id",
                        column: x => x.chapter_id,
                        principalTable: "budget_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_site_logs_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_site_logs_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_site_logs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "incident_materials",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    incident_id = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incident_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_incident_materials_incidents_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_incident_materials_material_catalogs_material_id",
                        column: x => x.material_id,
                        principalTable: "material_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "incident_workers",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    incident_id = table.Column<int>(type: "integer", nullable: false),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    hours_used = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incident_workers", x => x.id);
                    table.ForeignKey(
                        name: "fk_incident_workers_incidents_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_incident_workers_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "payrolls",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    site_log_id = table.Column<int>(type: "integer", nullable: false),
                    week_start = table.Column<DateTime>(type: "date", nullable: false),
                    week_end = table.Column<DateTime>(type: "date", nullable: false),
                    total_payroll = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    paid_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    chapter_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payrolls", x => x.id);
                    table.ForeignKey(
                        name: "fk_payrolls_budget_chapters_chapter_id",
                        column: x => x.chapter_id,
                        principalTable: "budget_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_payrolls_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_payrolls_site_logs_site_log_id",
                        column: x => x.site_log_id,
                        principalTable: "site_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_payrolls_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "project_progresses",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    site_log_id = table.Column<int>(type: "integer", nullable: false),
                    progress_percentage = table.Column<decimal>(
                        type: "numeric(5,2)",
                        nullable: false
                    ),
                    calculated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_progresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_progresses_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_project_progresses_site_logs_site_log_id",
                        column: x => x.site_log_id,
                        principalTable: "site_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "site_log_equipment",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    site_log_id = table.Column<int>(type: "integer", nullable: false),
                    equipment_type = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    description = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_log_equipment", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_log_equipment_site_logs_site_log_id",
                        column: x => x.site_log_id,
                        principalTable: "site_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "site_log_materials",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    site_log_id = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<int>(type: "integer", nullable: false),
                    quantity_used = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_log_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_log_materials_material_catalogs_material_id",
                        column: x => x.material_id,
                        principalTable: "material_catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_site_log_materials_site_logs_site_log_id",
                        column: x => x.site_log_id,
                        principalTable: "site_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "site_log_workers",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    site_log_id = table.Column<int>(type: "integer", nullable: false),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    hours_worked = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_log_workers", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_log_workers_site_logs_site_log_id",
                        column: x => x.site_log_id,
                        principalTable: "site_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_site_log_workers_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "payroll_details",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    payroll_id = table.Column<int>(type: "integer", nullable: false),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "date", nullable: false),
                    hours_worked = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    hourly_rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_type = table.Column<string>(
                        type: "character varying(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    advance_amount_applied = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: true
                    ),
                    final_amount_to_pay = table.Column<decimal>(
                        type: "numeric(18,2)",
                        nullable: false
                    ),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_payroll_details_payrolls_payroll_id",
                        column: x => x.payroll_id,
                        principalTable: "payrolls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_payroll_details_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "payroll_detail_payments",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    payroll_detail_id = table.Column<int>(type: "integer", nullable: false),
                    payment_method = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_detail_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payroll_detail_payments_payroll_details_payroll_detail_id",
                        column: x => x.payroll_detail_id,
                        principalTable: "payroll_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "name", "updated_at" },
                values: new object[,]
                {
                    {
                        1,
                        new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc),
                        "GeneralManager",
                        null,
                    },
                    {
                        2,
                        new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc),
                        "OperationsDirector",
                        null,
                    },
                    {
                        3,
                        new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc),
                        "ProjectAdmin",
                        null,
                    },
                    {
                        4,
                        new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc),
                        "BranchAdmin",
                        null,
                    },
                    {
                        5,
                        new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc),
                        "BusinessManager",
                        null,
                    },
                }
            );

            migrationBuilder.InsertData(
                table: "users",
                columns: new[]
                {
                    "id",
                    "blocked_at",
                    "created_at",
                    "email",
                    "failed_login_attempts",
                    "last_login_at",
                    "name",
                    "password_hash",
                    "profile_photo_file_id",
                    "status",
                    "updated_at",
                },
                values: new object[]
                {
                    1,
                    null,
                    new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc),
                    "admin@lvconstrucciones.com",
                    0,
                    null,
                    "Administrador",
                    "$2a$11$Rj4e9thsbsIFJ6zUWhMyMOCaOkGouPh8JtTuqCv7t1dPj/ilsk4LW",
                    null,
                    "Active",
                    null,
                }
            );

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "role_id", "user_id" },
                values: new object[] { 1, 1 }
            );

            migrationBuilder.CreateIndex(
                name: "ix_branch_indicators_branch_id",
                table: "branch_indicators",
                column: "branch_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_branch_inventories_branch_id_product_id",
                table: "branch_inventories",
                columns: new[] { "branch_id", "product_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_branch_inventories_product_id",
                table: "branch_inventories",
                column: "product_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_branches_branch_admin_id",
                table: "branches",
                column: "branch_admin_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_branches_business_manager_id",
                table: "branches",
                column: "business_manager_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_branches_operations_director_id",
                table: "branches",
                column: "operations_director_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budget_activities_chapter_id",
                table: "budget_activities",
                column: "chapter_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budget_activity_equipment_activity_id",
                table: "budget_activity_equipment",
                column: "activity_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budget_activity_labor_activity_id",
                table: "budget_activity_labor",
                column: "activity_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budget_activity_materials_activity_id",
                table: "budget_activity_materials",
                column: "activity_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budget_activity_materials_material_id",
                table: "budget_activity_materials",
                column: "material_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budget_chapters_budget_id",
                table: "budget_chapters",
                column: "budget_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budget_histories_budget_id",
                table: "budget_histories",
                column: "budget_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budget_histories_user_id",
                table: "budget_histories",
                column: "user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budgets_branch_id",
                table: "budgets",
                column: "branch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budgets_created_by_user_id",
                table: "budgets",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_budgets_customer_id",
                table: "budgets",
                column: "customer_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_cash_registers_branch_id",
                table: "cash_registers",
                column: "branch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_cash_registers_closed_by_user_id",
                table: "cash_registers",
                column: "closed_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_cash_registers_opened_by_user_id",
                table: "cash_registers",
                column: "opened_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_customers_email",
                table: "customers",
                column: "email"
            );

            migrationBuilder.CreateIndex(
                name: "ix_customers_personal_id",
                table: "customers",
                column: "personal_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_incident_materials_incident_id",
                table: "incident_materials",
                column: "incident_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_incident_materials_material_id",
                table: "incident_materials",
                column: "material_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_incident_workers_incident_id",
                table: "incident_workers",
                column: "incident_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_incident_workers_worker_id",
                table: "incident_workers",
                column: "worker_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_incidents_approved_by_user_id",
                table: "incidents",
                column: "approved_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_incidents_chapter_id",
                table: "incidents",
                column: "chapter_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_incidents_created_by_user_id",
                table: "incidents",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_incidents_project_id",
                table: "incidents",
                column: "project_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_destination_branch_id",
                table: "inventory_movements",
                column: "destination_branch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_destination_project_id",
                table: "inventory_movements",
                column: "destination_project_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_origin_branch_id",
                table: "inventory_movements",
                column: "origin_branch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id",
                table: "inventory_movements",
                column: "product_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_sent_by_user_id",
                table: "inventory_movements",
                column: "sent_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_validated_by_user_id",
                table: "inventory_movements",
                column: "validated_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoice_details_invoice_id",
                table: "invoice_details",
                column: "invoice_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoice_details_product_id",
                table: "invoice_details",
                column: "product_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoice_payments_invoice_id",
                table: "invoice_payments",
                column: "invoice_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoice_payments_received_by_user_id",
                table: "invoice_payments",
                column: "received_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoices_branch_id_invoice_number",
                table: "invoices",
                columns: new[] { "branch_id", "invoice_number" },
                unique: true,
                filter: "invoice_number IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoices_cash_register_id",
                table: "invoices",
                column: "cash_register_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoices_created_by_user_id",
                table: "invoices",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_invoices_customer_id",
                table: "invoices",
                column: "customer_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_material_catalogs_name",
                table: "material_catalogs",
                column: "name"
            );

            migrationBuilder.CreateIndex(
                name: "ix_material_tickets_chapter_id",
                table: "material_tickets",
                column: "chapter_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_material_tickets_created_by_user_id",
                table: "material_tickets",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_material_tickets_material_id",
                table: "material_tickets",
                column: "material_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_material_tickets_project_id",
                table: "material_tickets",
                column: "project_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_material_tickets_supplier_id",
                table: "material_tickets",
                column: "supplier_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_is_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" }
            );

            migrationBuilder.CreateIndex(
                name: "ix_offer_chapters_offer_id",
                table: "offer_chapters",
                column: "offer_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_offers_budget_id",
                table: "offers",
                column: "budget_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_offers_created_by_user_id",
                table: "offers",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_offers_customer_id",
                table: "offers",
                column: "customer_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_offers_offer_number",
                table: "offers",
                column: "offer_number",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_token",
                table: "password_reset_tokens",
                column: "token",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_payroll_detail_payments_payroll_detail_id",
                table: "payroll_detail_payments",
                column: "payroll_detail_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_payroll_details_payroll_id",
                table: "payroll_details",
                column: "payroll_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_payroll_details_worker_id",
                table: "payroll_details",
                column: "worker_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_payrolls_chapter_id",
                table: "payrolls",
                column: "chapter_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_payrolls_created_by_user_id",
                table: "payrolls",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_payrolls_project_id",
                table: "payrolls",
                column: "project_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_payrolls_site_log_id",
                table: "payrolls",
                column: "site_log_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_product_incorporation_tickets_branch_id",
                table: "product_incorporation_tickets",
                column: "branch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_product_incorporation_tickets_created_by_user_id",
                table: "product_incorporation_tickets",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_product_incorporation_tickets_product_id",
                table: "product_incorporation_tickets",
                column: "product_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_product_incorporation_tickets_supplier_id",
                table: "product_incorporation_tickets",
                column: "supplier_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_product_incorporation_tickets_validated_by_user_id",
                table: "product_incorporation_tickets",
                column: "validated_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_products_created_by_user_id",
                table: "products",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_products_sku",
                table: "products",
                column: "sku",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_products_validated_by_user_id",
                table: "products",
                column: "validated_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_chapters_chapter_id",
                table: "project_chapters",
                column: "chapter_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_chapters_project_id_chapter_id",
                table: "project_chapters",
                columns: new[] { "project_id", "chapter_id" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_end_date_histories_project_id",
                table: "project_end_date_histories",
                column: "project_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_end_date_histories_user_id",
                table: "project_end_date_histories",
                column: "user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_inventory_items_material_id",
                table: "project_inventory_items",
                column: "material_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_inventory_items_product_id",
                table: "project_inventory_items",
                column: "product_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_inventory_items_project_id_material_id",
                table: "project_inventory_items",
                columns: new[] { "project_id", "material_id" },
                unique: true,
                filter: "material_id IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_inventory_items_project_id_product_id",
                table: "project_inventory_items",
                columns: new[] { "project_id", "product_id" },
                unique: true,
                filter: "product_id IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_progresses_project_id",
                table: "project_progresses",
                column: "project_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_progresses_site_log_id",
                table: "project_progresses",
                column: "site_log_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_workers_assigned_by_user_id",
                table: "project_workers",
                column: "assigned_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_workers_project_id",
                table: "project_workers",
                column: "project_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_project_workers_worker_id",
                table: "project_workers",
                column: "worker_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_projects_branch_id",
                table: "projects",
                column: "branch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_projects_budget_id",
                table: "projects",
                column: "budget_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_projects_created_by_user_id",
                table: "projects",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_projects_customer_id",
                table: "projects",
                column: "customer_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_projects_offer_id",
                table: "projects",
                column: "offer_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_log_equipment_site_log_id",
                table: "site_log_equipment",
                column: "site_log_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_log_materials_material_id",
                table: "site_log_materials",
                column: "material_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_log_materials_site_log_id",
                table: "site_log_materials",
                column: "site_log_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_log_workers_site_log_id",
                table: "site_log_workers",
                column: "site_log_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_log_workers_worker_id",
                table: "site_log_workers",
                column: "worker_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_logs_approved_by_user_id",
                table: "site_logs",
                column: "approved_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_logs_chapter_id",
                table: "site_logs",
                column: "chapter_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_logs_created_by_user_id",
                table: "site_logs",
                column: "created_by_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_site_logs_project_id_week_start",
                table: "site_logs",
                columns: new[] { "project_id", "week_start" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_email",
                table: "suppliers",
                column: "email"
            );

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_personal_id",
                table: "suppliers",
                column: "personal_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "ix_users_profile_photo_file_id",
                table: "users",
                column: "profile_photo_file_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_workers_branch_id",
                table: "workers",
                column: "branch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_workers_personal_id",
                table: "workers",
                column: "personal_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "branch_indicators");

            migrationBuilder.DropTable(name: "branch_inventories");

            migrationBuilder.DropTable(name: "budget_activity_equipment");

            migrationBuilder.DropTable(name: "budget_activity_labor");

            migrationBuilder.DropTable(name: "budget_activity_materials");

            migrationBuilder.DropTable(name: "budget_histories");

            migrationBuilder.DropTable(name: "incident_materials");

            migrationBuilder.DropTable(name: "incident_workers");

            migrationBuilder.DropTable(name: "inventory_movements");

            migrationBuilder.DropTable(name: "invoice_details");

            migrationBuilder.DropTable(name: "invoice_payments");

            migrationBuilder.DropTable(name: "material_tickets");

            migrationBuilder.DropTable(name: "notifications");

            migrationBuilder.DropTable(name: "offer_chapters");

            migrationBuilder.DropTable(name: "password_reset_tokens");

            migrationBuilder.DropTable(name: "payroll_detail_payments");

            migrationBuilder.DropTable(name: "product_incorporation_tickets");

            migrationBuilder.DropTable(name: "project_chapters");

            migrationBuilder.DropTable(name: "project_end_date_histories");

            migrationBuilder.DropTable(name: "project_inventory_items");

            migrationBuilder.DropTable(name: "project_progresses");

            migrationBuilder.DropTable(name: "project_workers");

            migrationBuilder.DropTable(name: "refresh_tokens");

            migrationBuilder.DropTable(name: "site_log_equipment");

            migrationBuilder.DropTable(name: "site_log_materials");

            migrationBuilder.DropTable(name: "site_log_workers");

            migrationBuilder.DropTable(name: "user_roles");

            migrationBuilder.DropTable(name: "budget_activities");

            migrationBuilder.DropTable(name: "incidents");

            migrationBuilder.DropTable(name: "invoices");

            migrationBuilder.DropTable(name: "payroll_details");

            migrationBuilder.DropTable(name: "suppliers");

            migrationBuilder.DropTable(name: "products");

            migrationBuilder.DropTable(name: "material_catalogs");

            migrationBuilder.DropTable(name: "roles");

            migrationBuilder.DropTable(name: "cash_registers");

            migrationBuilder.DropTable(name: "payrolls");

            migrationBuilder.DropTable(name: "workers");

            migrationBuilder.DropTable(name: "site_logs");

            migrationBuilder.DropTable(name: "budget_chapters");

            migrationBuilder.DropTable(name: "projects");

            migrationBuilder.DropTable(name: "offers");

            migrationBuilder.DropTable(name: "budgets");

            migrationBuilder.DropTable(name: "branches");

            migrationBuilder.DropTable(name: "customers");

            migrationBuilder.DropTable(name: "users");

            migrationBuilder.DropTable(name: "stored_files");
        }
    }
}
