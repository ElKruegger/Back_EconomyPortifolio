using Microsoft.EntityFrameworkCore;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;

namespace EconomyBackPortifolio.Data
{
    /// <summary>
    /// Contexto principal do Entity Framework Core para o Economy Portfolio.
    ///
    /// Contém todos os DbSets da aplicação e as configurações de mapeamento
    /// objeto-relacional para o PostgreSQL.
    ///
    /// Convenções de nomenclatura adotadas:
    ///   - Tabelas em snake_case (ex: financial_entries)
    ///   - Colunas em snake_case (ex: user_id, created_at)
    ///   - PKs nomeadas pk_<tabela>
    ///   - FKs nomeadas fk_<tabela>_<coluna>__<tabela_ref>
    ///   - Índices únicos nomeados uk_<tabela>_<coluna>
    ///   - Índices de busca nomeados ix_<tabela>_<coluna>
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ─────────────────────────────────────────────────────────────────────
        // DBSETS — Entidades existentes (investimentos)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Usuários cadastrados na plataforma.</summary>
        public DbSet<Users> Users { get; set; }

        /// <summary>Ativos financeiros disponíveis para negociação (ações, ETFs, cripto).</summary>
        public DbSet<Assets> Assets { get; set; }

        /// <summary>Carteiras multi-moeda dos usuários.</summary>
        public DbSet<Wallets> Wallets { get; set; }

        /// <summary>Posições abertas de ativos por carteira.</summary>
        public DbSet<Positions> Positions { get; set; }

        /// <summary>Histórico de transações de investimento (compra, venda, depósito, conversão).</summary>
        public DbSet<Transactions> Transactions { get; set; }

        /// <summary>Códigos de verificação 2FA (registro, login, reset de senha).</summary>
        public DbSet<VerificationCodes> VerificationCodes { get; set; }

        // ─────────────────────────────────────────────────────────────────────
        // DBSETS — Novas entidades (controle financeiro)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Categorias de lançamentos financeiros (sistema e personalizadas pelo usuário).</summary>
        public DbSet<Category> Categories { get; set; }

        /// <summary>Lançamentos financeiros de receitas e despesas.</summary>
        public DbSet<FinancialEntry> FinancialEntries { get; set; }

        /// <summary>Clientes gerenciados por Contadores (multi-tenant).</summary>
        public DbSet<Client> Clients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUsers(modelBuilder);
            ConfigureVerificationCodes(modelBuilder);
            ConfigureAssets(modelBuilder);
            ConfigureWallets(modelBuilder);
            ConfigurePositions(modelBuilder);
            ConfigureTransactions(modelBuilder);
            ConfigureCategories(modelBuilder);
            ConfigureFinancialEntries(modelBuilder);
            ConfigureClients(modelBuilder);

            SeedSystemCategories(modelBuilder);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONFIGURAÇÕES — Entidades existentes
        // ─────────────────────────────────────────────────────────────────────

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id).HasName("pk_users");
                entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uk_users_email");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(150);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(e => e.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
                entity.Property(e => e.ProfileType).HasColumnName("profile_type").HasDefaultValue(ProfileType.PessoaFisica);
                entity.Property(e => e.PlanType).HasColumnName("plan_type").HasDefaultValue(PlanType.Basic);
                entity.Property(e => e.CompanyName).HasColumnName("company_name").HasMaxLength(200);
                entity.Property(e => e.PlanExpiresAt).HasColumnName("plan_expires_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });
        }

        private static void ConfigureVerificationCodes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VerificationCodes>(entity =>
            {
                entity.ToTable("verification_codes");
                entity.HasKey(e => e.Id).HasName("pk_verification_codes");
                entity.HasIndex(e => new { e.UserId, e.Type, e.IsUsed }).HasDatabaseName("ix_verification_codes_user_type_used");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(6);
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .HasConstraintName("fk_verification_codes_user_id__users");
            });
        }

        private static void ConfigureAssets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Assets>(entity =>
            {
                entity.ToTable("assets");
                entity.HasKey(e => e.Id).HasName("pk_assets");
                entity.HasIndex(e => e.Symbol).IsUnique().HasDatabaseName("uk_assets_symbol");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Symbol).HasColumnName("symbol").HasMaxLength(20);
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150);
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20);
                entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
                entity.Property(e => e.CurrentPrice).HasColumnName("current_price").HasPrecision(18, 6);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });
        }

        private static void ConfigureWallets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Wallets>(entity =>
            {
                entity.ToTable("wallets");
                entity.HasKey(e => e.Id).HasName("pk_wallets");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
                entity.Property(e => e.Balance).HasColumnName("balance").HasPrecision(18, 2);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .HasConstraintName("fk_wallets_user_id__users");
            });
        }

        private static void ConfigurePositions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Positions>(entity =>
            {
                entity.ToTable("positions");
                entity.HasKey(e => e.Id).HasName("pk_positions");
                entity.HasIndex(e => new { e.WalletId, e.AssetId }).IsUnique().HasDatabaseName("uk_positions_wallet_asset");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.WalletId).HasColumnName("wallet_id");
                entity.Property(e => e.AssetId).HasColumnName("asset_id");
                entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 6);
                entity.Property(e => e.AveragePrice).HasColumnName("average_price").HasPrecision(18, 6);
                entity.Property(e => e.TotalInvested).HasColumnName("total_invested").HasPrecision(18, 2);
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Wallet)
                    .WithMany()
                    .HasForeignKey(e => e.WalletId)
                    .HasConstraintName("fk_positions_wallet_id__wallets");

                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .HasConstraintName("fk_positions_asset_id__assets");
            });
        }

        private static void ConfigureTransactions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transactions>(entity =>
            {
                entity.ToTable("transactions");
                entity.HasKey(e => e.Id).HasName("pk_transactions");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.WalletId).HasColumnName("wallet_id");
                entity.Property(e => e.AssetId).HasColumnName("asset_id");
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20);
                entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 6);
                entity.Property(e => e.Price).HasColumnName("price").HasPrecision(18, 6);
                entity.Property(e => e.Total).HasColumnName("total").HasPrecision(18, 2);
                entity.Property(e => e.TransactionAt).HasColumnName("transaction_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Wallet)
                    .WithMany()
                    .HasForeignKey(e => e.WalletId)
                    .HasConstraintName("fk_transactions_wallet_id__wallets");

                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .HasConstraintName("fk_transactions_asset_id__assets");
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONFIGURAÇÕES — Novas entidades (controle financeiro)
        // ─────────────────────────────────────────────────────────────────────

        private static void ConfigureCategories(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("categories");
                entity.HasKey(e => e.Id).HasName("pk_categories");
                entity.HasIndex(e => e.UserId).HasDatabaseName("ix_categories_user_id");
                entity.HasIndex(e => new { e.Type, e.IsSystem }).HasDatabaseName("ix_categories_type_system");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(50);
                entity.Property(e => e.Color).HasColumnName("color").HasMaxLength(7);
                entity.Property(e => e.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .IsRequired(false)
                    .HasConstraintName("fk_categories_user_id__users")
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureFinancialEntries(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FinancialEntry>(entity =>
            {
                entity.ToTable("financial_entries");
                entity.HasKey(e => e.Id).HasName("pk_financial_entries");
                entity.HasIndex(e => new { e.UserId, e.EntryDate }).HasDatabaseName("ix_financial_entries_user_date");
                entity.HasIndex(e => new { e.UserId, e.Type }).HasDatabaseName("ix_financial_entries_user_type");
                entity.HasIndex(e => e.ClientId).HasDatabaseName("ix_financial_entries_client_id");
                entity.HasIndex(e => e.CategoryId).HasDatabaseName("ix_financial_entries_category_id");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.ClientId).HasColumnName("client_id");
                entity.Property(e => e.CategoryId).HasColumnName("category_id");
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(e => e.EntryDate).HasColumnName("entry_date");
                entity.Property(e => e.IsRecurring).HasColumnName("is_recurring").HasDefaultValue(false);
                entity.Property(e => e.RecurrenceInterval).HasColumnName("recurrence_interval").HasMaxLength(20);
                entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .HasConstraintName("fk_financial_entries_user_id__users")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.FinancialEntries)
                    .HasForeignKey(e => e.CategoryId)
                    .HasConstraintName("fk_financial_entries_category_id__categories")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Client)
                    .WithMany(c => c.FinancialEntries)
                    .HasForeignKey(e => e.ClientId)
                    .IsRequired(false)
                    .HasConstraintName("fk_financial_entries_client_id__clients")
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureClients(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("clients");
                entity.HasKey(e => e.Id).HasName("pk_clients");
                entity.HasIndex(e => e.AccountantUserId).HasDatabaseName("ix_clients_accountant_user_id");
                entity.HasIndex(e => new { e.AccountantUserId, e.IsActive }).HasDatabaseName("ix_clients_accountant_active");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AccountantUserId).HasColumnName("accountant_user_id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(150);
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
                entity.Property(e => e.Document).HasColumnName("document").HasMaxLength(20);
                entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Accountant)
                    .WithMany()
                    .HasForeignKey(e => e.AccountantUserId)
                    .HasConstraintName("fk_clients_accountant_user_id__users")
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEED — Categorias padrão do sistema
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Preenche as categorias padrão do sistema disponíveis para todos os usuários.
        /// IDs fixos (GUIDs determinísticos) garantem idempotência nas migrations.
        /// </summary>
        private static void SeedSystemCategories(ModelBuilder modelBuilder)
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var categories = new List<Category>
            {
                // ── Receitas ─────────────────────────────────────────────────
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Salário",        Type = CategoryType.Receita, Icon = "💰", Color = "#4CAF50", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Freelance",      Type = CategoryType.Receita, Icon = "💻", Color = "#8BC34A", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Investimentos",  Type = CategoryType.Receita, Icon = "📈", Color = "#009688", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Outras Receitas",Type = CategoryType.Receita, Icon = "➕", Color = "#607D8B", IsSystem = true, IsActive = true, CreatedAt = now },

                // ── Despesas ─────────────────────────────────────────────────
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "Alimentação",    Type = CategoryType.Despesa, Icon = "🍽️", Color = "#FF5722", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Name = "Moradia",        Type = CategoryType.Despesa, Icon = "🏠", Color = "#795548", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Name = "Transporte",     Type = CategoryType.Despesa, Icon = "🚗", Color = "#FF9800", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Name = "Saúde",          Type = CategoryType.Despesa, Icon = "🏥", Color = "#E91E63", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), Name = "Educação",       Type = CategoryType.Despesa, Icon = "📚", Color = "#3F51B5", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000006"), Name = "Lazer",          Type = CategoryType.Despesa, Icon = "🎉", Color = "#9C27B0", IsSystem = true, IsActive = true, CreatedAt = now },
                new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000007"), Name = "Outras Despesas",Type = CategoryType.Despesa, Icon = "➖", Color = "#9E9E9E", IsSystem = true, IsActive = true, CreatedAt = now },

                // ── Ambas ─────────────────────────────────────────────────────
                new() { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), Name = "Transferência",  Type = CategoryType.Ambas,   Icon = "🔄", Color = "#00BCD4", IsSystem = true, IsActive = true, CreatedAt = now },
            };

            modelBuilder.Entity<Category>().HasData(categories);
        }
    }
}
