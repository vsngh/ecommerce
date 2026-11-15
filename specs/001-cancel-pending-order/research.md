# Research: Cancel a Pending Order

- Orders are stored in the Orders API. The current service already owns order
  reads, mutations, and Redis cache invalidation.
- Users tokens carry `ClaimTypes.NameIdentifier` and role permissions. The
  order cancellation route must use that identity and must not trust a
  caller-supplied user ID.
- The payment record stores `OrderId`, `UserId`, `Amount`, and `Status`.
  Existing refund logic only changes the local status; there is no external
  payment provider or funds-transfer integration.
- The Orders and Payments APIs run as internal Docker Compose/AKS services and
  can share the configured JWT signing key. After validating the customer's
  identity and order ownership, Orders mints a short-lived internal JWT with
  the dedicated `payments.order.cancel` permission. Payments does not accept a
  normal customer token for its internal cancellation route.
- Cross-service database transactions are unavailable. The Payments operation
  must be idempotent so a retry can finish cancellation if the payment update
  succeeds but saving the order status fails.
- The order status must be checked before invoking Payments. The generic
  order-status mutation route must require the existing `orders.manage`
  permission to prevent bypassing the cancellation rules.
