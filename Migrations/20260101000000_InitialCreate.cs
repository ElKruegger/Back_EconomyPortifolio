using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EconomyBackPortifolio.Migrations
{
    /// <summary>
    /// Initial migration — creates all base tables for the Economy Portfolio schema.
    /// Must run before any other migration.
    ///
    /// Written as idempotent raw SQL (CREATE TABLE IF NOT EXISTS / CREATE INDEX IF NOT EXISTS)
    /// so it never fails on re-runs or when deploying against a database that already has
    /// these tables (e.g. after a Railway volume reset or a partial previous deploy).
    ///
    /// Tables created:
    ///   - users        : registered user accounts
    ///   - assets       : global tradeable asset catalog (stocks, crypto, ETFs)
    ///   - wallets      : per-user currency balances (BRL, USD, BTC, etc.)
    ///   - positions    : open investment holdings per user per asset
    ///   - transactions : immutable record of all financial operations
    ///
    /// Note: email_verified and verification_codes are handled by the next migration.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── users ────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS users (
                    id            uuid                        NOT NULL,
                    name          character varying(100)      NOT NULL,
                    email         character varying(150)      NOT NULL,
                    password_hash character varying(255)      NOT NULL,
                    created_at    timestamp with time zone    NOT NULL,
                    CONSTRAINT pk_users PRIMARY KEY (id)
                );
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS uk_users_email ON users (email);
            ");

            // ─── assets ───────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS assets (
                    id            uuid                        NOT NULL,
                    symbol        character varying(20)       NOT NULL,
                    name          character varying(150)      NOT NULL,
                    type          character varying(20)       NOT NULL,
                    currency      character varying(10)       NOT NULL,
                    current_price numeric(18,6)               NOT NULL,
                    created_at    timestamp with time zone    NOT NULL,
                    CONSTRAINT pk_assets PRIMARY KEY (id)
                );
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS uk_assets_symbol ON assets (symbol);
            ");

            // ─── wallets ──────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS wallets (
                    id         uuid                        NOT NULL,
                    user_id    uuid                        NOT NULL,
                    currency   character varying(10)       NOT NULL,
                    balance    numeric(18,2)               NOT NULL,
                    created_at timestamp with time zone    NOT NULL,
                    CONSTRAINT pk_wallets    PRIMARY KEY (id),
                    CONSTRAINT fk_wallets_user_id__users
                        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_wallets_user_id ON wallets (user_id);
            ");

            // ─── positions ────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS positions (
                    id             uuid                        NOT NULL,
                    wallet_id      uuid                        NOT NULL,
                    asset_id       uuid                        NOT NULL,
                    quantity       numeric(18,6)               NOT NULL,
                    average_price  numeric(18,6)               NOT NULL,
                    total_invested numeric(18,2)               NOT NULL,
                    updated_at     timestamp with time zone    NOT NULL,
                    CONSTRAINT pk_positions PRIMARY KEY (id),
                    CONSTRAINT fk_positions_wallet_id__wallets
                        FOREIGN KEY (wallet_id) REFERENCES wallets (id) ON DELETE CASCADE,
                    CONSTRAINT fk_positions_asset_id__assets
                        FOREIGN KEY (asset_id) REFERENCES assets (id) ON DELETE RESTRICT
                );
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS uk_positions_wallet_asset ON positions (wallet_id, asset_id);
            ");

            // ─── transactions ─────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS transactions (
                    id             uuid                        NOT NULL,
                    wallet_id      uuid                        NOT NULL,
                    asset_id       uuid,
                    type           character varying(20)       NOT NULL,
                    quantity       numeric(18,6),
                    price          numeric(18,6),
                    total          numeric(18,2)               NOT NULL,
                    transaction_at timestamp with time zone    NOT NULL,
                    created_at     timestamp with time zone    NOT NULL,
                    CONSTRAINT pk_transactions PRIMARY KEY (id),
                    CONSTRAINT fk_transactions_wallet_id__wallets
                        FOREIGN KEY (wallet_id) REFERENCES wallets (id) ON DELETE CASCADE,
                    CONSTRAINT fk_transactions_asset_id__assets
                        FOREIGN KEY (asset_id) REFERENCES assets (id) ON DELETE SET NULL
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_transactions_wallet_id ON transactions (wallet_id);
                CREATE INDEX IF NOT EXISTS ix_transactions_asset_id  ON transactions (asset_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS transactions;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS positions;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS wallets;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS assets;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS users;");
        }
    }
}
