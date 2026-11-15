namespace Orders.Api.Security;

public static class PermissionPolicies
{
    public const string OrdersCreate = "OrdersCreate";
    public const string OrdersRead = "OrdersRead";
    public const string OrdersManage = "OrdersManage";
    public const string BulkOrdersUpload = "BulkOrdersUpload";
    public const string BulkOrdersApprove = "BulkOrdersApprove";
}
