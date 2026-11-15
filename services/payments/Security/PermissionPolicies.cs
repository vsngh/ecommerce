namespace Payments.Api.Security;

public static class PermissionPolicies
{
    public const string PaymentsRead = "PaymentsRead";
    public const string PaymentsManage = "PaymentsManage";
    public const string PaymentsCreate = "PaymentsCreate";
    public const string PaymentsRefund = "PaymentsRefund";
    public const string OrderCancellation = "OrderCancellation";
}
