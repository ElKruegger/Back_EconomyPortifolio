using Microsoft.EntityFrameworkCore;
using EconomyBackPortifolio.Models;

namespace EconomyBackPortifolio.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Assets> Assets { get; set; }
        public DbSet<Wallets> Wallets { get; set; }
        public DbSet<Positions> Positions { get; set; }
        public DbSet<Transactions> Transactions { get; set; }
        public DbSet<VerificationCodes> VerificationCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da tabela Users
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
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // Configuração da tabela VerificationCodes
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

            // Configuração da tabela Assets
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

            // Configuração da tabela Wallets
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

            // Configuração da tabela Positions
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

            // Configuração da tabela Transactions
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
    }
}
