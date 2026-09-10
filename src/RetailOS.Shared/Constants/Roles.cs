namespace RetailOS.Shared.Constants;

public static class Roles
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string InventoryClerk = "InventoryClerk";
    public const string Merchant = "Merchant";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Owner,
        Manager,
        Cashier,
        InventoryClerk,
        Merchant
    };
}
