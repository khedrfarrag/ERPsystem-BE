using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddB2BWholesalePortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_wholesale_available",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "wholesale_price",
                table: "products",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "merchants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trade_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    payment_terms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchants", x => x.id);
                    table.ForeignKey(
                        name: "fk_merchants_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_merchants_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_merchants_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    message = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    notification_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    payload_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_asp_net_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "b2b_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    payment_preference = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Credit"),
                    total_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    remaining_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false, defaultValue: 0m),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_credit_limit_override_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    credit_limit_override_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    credit_limit_override_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_limit_override_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    credit_limit_at_invoice = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    outstanding_balance_at_invoice = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    credit_amount_at_invoice = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    projected_balance_at_invoice = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    credit_limit_exceeded_by = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_b2b_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_b2b_orders_asp_net_users_cancelled_by_user_id",
                        column: x => x.cancelled_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_b2b_orders_asp_net_users_credit_limit_override_by_user_id",
                        column: x => x.credit_limit_override_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_b2b_orders_merchants_merchant_id",
                        column: x => x.merchant_id,
                        principalTable: "merchants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_b2b_orders_sales_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_b2b_orders_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "b2b_order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    b2b_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    approved_quantity = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    unit_wholesale_price = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    requested_subtotal = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    approved_subtotal = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    adjustment_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    adjusted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    adjusted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_b2b_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_b2b_order_items_asp_net_users_adjusted_by_user_id",
                        column: x => x.adjusted_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_b2b_order_items_b2b_orders_b2b_order_id",
                        column: x => x.b2b_order_id,
                        principalTable: "b2b_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_b2b_order_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_b2b_order_items_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_products_store_id_is_wholesale_available",
                table: "products",
                columns: new[] { "store_id", "is_wholesale_available" });

            migrationBuilder.CreateIndex(
                name: "ix_b2b_order_items_adjusted_by_user_id",
                table: "b2b_order_items",
                column: "adjusted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_order_items_b2b_order_id",
                table: "b2b_order_items",
                column: "b2b_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_order_items_product_id",
                table: "b2b_order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_order_items_store_id",
                table: "b2b_order_items",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_orders_cancelled_by_user_id",
                table: "b2b_orders",
                column: "cancelled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_orders_credit_limit_override_by_user_id",
                table: "b2b_orders",
                column: "credit_limit_override_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_orders_merchant_id",
                table: "b2b_orders",
                column: "merchant_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_orders_sales_invoice_id",
                table: "b2b_orders",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_orders_store_id",
                table: "b2b_orders",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_b2b_orders_store_id_order_number",
                table: "b2b_orders",
                columns: new[] { "store_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_b2b_orders_store_id_status",
                table: "b2b_orders",
                columns: new[] { "store_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_merchants_customer_id",
                table: "merchants",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_merchants_store_id",
                table: "merchants",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_merchants_store_id_phone",
                table: "merchants",
                columns: new[] { "store_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "ix_merchants_user_id",
                table: "merchants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_recipient_user_id",
                table: "notifications",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_store_id",
                table: "notifications",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_store_id_is_read",
                table: "notifications",
                columns: new[] { "store_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "b2b_order_items");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "b2b_orders");

            migrationBuilder.DropTable(
                name: "merchants");

            migrationBuilder.DropIndex(
                name: "ix_products_store_id_is_wholesale_available",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_wholesale_available",
                table: "products");

            migrationBuilder.DropColumn(
                name: "wholesale_price",
                table: "products");
        }
    }
}
