namespace Users.Api.Security;

public static class PermissionCatalog
{
    public const string OrdersCreate = "orders.create";
    public const string OrdersRead = "orders.read";
    public const string OrdersManage = "orders.manage";
    public const string BulkOrdersUpload = "orders.bulk.upload";
    public const string BulkOrdersApprove = "orders.bulk.approve";
    public const string ProductsRead = "products.read";
    public const string ProductsManage = "products.manage";
    public const string UsersManage = "users.manage";
    public const string InventoryManage = "inventory.manage";
    public const string CartsManage = "carts.manage";
    public const string PaymentsCreate = "payments.create";
    public const string PaymentsRefund = "payments.refund";

    public static IReadOnlyList<string> ForRole(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "admin" => [
                OrdersCreate,
                OrdersRead,
                OrdersManage,
                BulkOrdersUpload,
                BulkOrdersApprove,
                ProductsRead,
                ProductsManage,
                UsersManage,
                InventoryManage,
                CartsManage,
                PaymentsCreate,
                PaymentsRefund
            ],
            "manager" => [
                OrdersRead,
                BulkOrdersUpload,
                ProductsRead,
                ProductsManage,
                InventoryManage
            ],
            _ => [
                OrdersCreate,
                ProductsRead,
                CartsManage,
                PaymentsCreate
            ]
        };
    }
}
