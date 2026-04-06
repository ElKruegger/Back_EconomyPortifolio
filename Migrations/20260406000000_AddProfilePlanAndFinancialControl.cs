using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EconomyBackPortifolio.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePlanAndFinancialControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────────
            // 1. ALTERAR TABELA users — adicionar ProfileType, PlanType, etc.
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "profile_type",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0); // 0 = PessoaFisica

            migrationBuilder.AddColumn<int>(
                name: "plan_type",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0); // 0 = Basic

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "plan_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // ─────────────────────────────────────────────────────────────────
            // 2. CRIAR TABELA categories
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_user_id__users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_user_id",
                table: "categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_type_system",
                table: "categories",
                columns: new[] { "type", "is_system" });

            // ─────────────────────────────────────────────────────────────────
            // 3. CRIAR TABELA clients
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    accountant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    document = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                    table.ForeignKey(
                        name: "fk_clients_accountant_user_id__users",
                        column: x => x.accountant_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clients_accountant_user_id",
                table: "clients",
                column: "accountant_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_clients_accountant_active",
                table: "clients",
                columns: new[] { "accountant_user_id", "is_active" });

            // ─────────────────────────────────────────────────────────────────
            // 4. CRIAR TABELA financial_entries
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "financial_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    entry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recurrence_interval = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_financial_entries_user_id__users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_financial_entries_category_id__categories",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_entries_client_id__clients",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_financial_entries_user_date",
                table: "financial_entries",
                columns: new[] { "user_id", "entry_date" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_entries_user_type",
                table: "financial_entries",
                columns: new[] { "user_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_entries_client_id",
                table: "financial_entries",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_entries_category_id",
                table: "financial_entries",
                column: "category_id");

            // ─────────────────────────────────────────────────────────────────
            // 5. SEED — Categorias padrão do sistema
            // ─────────────────────────────────────────────────────────────────
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "user_id", "name", "type", "icon", "color", "is_system", "is_active", "created_at" },
                values: new object[,]
                {
                    // Receitas (type=0)
                    { Guid.Parse("10000000-0000-0000-0000-000000000001"), null, "Salário",         0, "💰", "#4CAF50", true, true, seedDate },
                    { Guid.Parse("10000000-0000-0000-0000-000000000002"), null, "Freelance",       0, "💻", "#8BC34A", true, true, seedDate },
                    { Guid.Parse("10000000-0000-0000-0000-000000000003"), null, "Investimentos",   0, "📈", "#009688", true, true, seedDate },
                    { Guid.Parse("10000000-0000-0000-0000-000000000004"), null, "Outras Receitas", 0, "➕", "#607D8B", true, true, seedDate },
                    // Despesas (type=1)
                    { Guid.Parse("20000000-0000-0000-0000-000000000001"), null, "Alimentação",     1, "🍽️", "#FF5722", true, true, seedDate },
                    { Guid.Parse("20000000-0000-0000-0000-000000000002"), null, "Moradia",         1, "🏠", "#795548", true, true, seedDate },
                    { Guid.Parse("20000000-0000-0000-0000-000000000003"), null, "Transporte",      1, "🚗", "#FF9800", true, true, seedDate },
                    { Guid.Parse("20000000-0000-0000-0000-000000000004"), null, "Saúde",           1, "🏥", "#E91E63", true, true, seedDate },
                    { Guid.Parse("20000000-0000-0000-0000-000000000005"), null, "Educação",        1, "📚", "#3F51B5", true, true, seedDate },
                    { Guid.Parse("20000000-0000-0000-0000-000000000006"), null, "Lazer",           1, "🎉", "#9C27B0", true, true, seedDate },
                    { Guid.Parse("20000000-0000-0000-0000-000000000007"), null, "Outras Despesas", 1, "➖", "#9E9E9E", true, true, seedDate },
                    // Ambas (type=2)
                    { Guid.Parse("30000000-0000-0000-0000-000000000001"), null, "Transferência",   2, "🔄", "#00BCD4", true, true, seedDate },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove tabelas novas (ordem inversa de dependência)
            migrationBuilder.DropTable(name: "financial_entries");
            migrationBuilder.DropTable(name: "clients");
            migrationBuilder.DropTable(name: "categories");

            // Remove colunas adicionadas em users
            migrationBuilder.DropColumn(name: "profile_type",   table: "users");
            migrationBuilder.DropColumn(name: "plan_type",      table: "users");
            migrationBuilder.DropColumn(name: "company_name",   table: "users");
            migrationBuilder.DropColumn(name: "plan_expires_at",table: "users");
        }
    }
}
