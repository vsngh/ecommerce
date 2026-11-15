# Quickstart: Cancel a Pending Order

1. Configure the same test JWT signing key for Users, Orders, and Payments.
2. Configure `Services__Payments__BaseUrl` for Orders to point to the internal
   Payments API address.
3. Create an account and obtain its JWT through the Users API.
4. Create an order for that user's ID and optionally create a payment for the
   same order/user.
5. As the order owner, call `PATCH /api/orders/{orderId}/cancel` with the JWT.
6. Verify the order is Cancelled. If a Completed payment existed, its local
   status is Refunded; if Pending, its local status is Cancelled.
7. Verify an order owned by another user returns 404, a non-Pending order
   returns 409, and unavailable Payments leaves the order Pending.

The recorded payment states are simulated. No actual payment provider is
contacted and no money is moved.
