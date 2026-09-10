using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogAndInventoryLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_units_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    selling_price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    purchase_cost = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    min_stock_level = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    cost_per_unit = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_transactions_asp_net_users_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_transactions_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_transactions_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_store_id",
                table: "categories",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_created_by",
                table: "inventory_transactions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_product_id",
                table: "inventory_transactions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_store_id_created_at",
                table: "inventory_transactions",
                columns: new[] { "store_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_store_id_product_id",
                table: "inventory_transactions",
                columns: new[] { "store_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_store_id",
                table: "products",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_store_id_category_id",
                table: "products",
                columns: new[] { "store_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_products_store_id_is_active",
                table: "products",
                columns: new[] { "store_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_products_unit_id",
                table: "products",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_units_store_id",
                table: "units",
                column: "store_id");

            // Functional unique index: Case-insensitive Category name per store
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_categories_store_name
                ON categories (store_id, lower(name))
                WHERE deleted_at IS NULL;");

            // Functional unique index: Case-insensitive Unit name and symbol per store
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_units_store_name
                ON units (store_id, lower(name))
                WHERE deleted_at IS NULL;");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_units_store_symbol
                ON units (store_id, lower(symbol))
                WHERE deleted_at IS NULL;");

            // Functional unique index: Case-insensitive Product name per store
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_products_store_name
                ON products (store_id, lower(name))
                WHERE deleted_at IS NULL;");

            // Partial unique index: Nullable Product barcode per store
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_products_store_barcode
                ON products (store_id, barcode)
                WHERE barcode IS NOT NULL AND deleted_at IS NULL;");

            // Partial unique index: Opening stock recorded once per product per store
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_inv_tx_opening_balance
                ON inventory_transactions (store_id, product_id)
                WHERE reason = 'OpeningBalance';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_categories_store_name;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_units_store_name;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_units_store_symbol;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_store_name;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_store_barcode;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_inv_tx_opening_balance;");

            migrationBuilder.DropTable(
                name: "inventory_transactions");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "units");
        }
    }
}
