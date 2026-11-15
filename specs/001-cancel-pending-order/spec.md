# Feature Specification: Cancel a Pending Order

**Feature Branch**: `001-cancel-pending-order`
**Created**: 2026-11-05
**Status**: Approved
**Input**: Allow customers to cancel their own Pending orders. Record a full refund for any completed payment before cancelling.

## User Scenarios & Testing

### User Story 1 - Cancel an eligible order (Priority: P1)

As a customer, I want to cancel an order that is still Pending so I can stop
fulfillment before it proceeds.

**Why this priority**: This is the complete, user-visible purpose of the
change.

**Independent Test**: Authenticate as the order owner, cancel a Pending order,
and verify the order status and any associated payment statuses.

**Acceptance Scenarios**:

1. **Given** a Pending order with no payment, **When** its owner cancels it,
   **Then** the order becomes Cancelled.
2. **Given** a Pending order with a Completed payment, **When** its owner
   cancels it, **Then** the payment is marked Refunded before the order becomes
   Cancelled.
3. **Given** the payment service cannot record the refund, **When** the owner
   cancels, **Then** the order remains Pending and the API returns a retryable
   service error.
4. **Given** a non-Pending order, **When** its owner cancels it, **Then** the
   order is unchanged and the API returns a conflict.
5. **Given** another customer's order, **When** a customer attempts to cancel
   it, **Then** no order or payment is changed and the API does not disclose
   the order.
6. **Given** a Pending order, **When** an unauthenticated caller attempts to
   cancel it, **Then** the API returns Unauthorized.
7. **Given** a customer-owned order with a Pending payment, **When** the owner
   cancels it, **Then** the payment record is marked Cancelled and the order is
   marked Cancelled.

## Edge Cases

- Repeating cancellation after the order is Cancelled returns a conflict; no
  second refund is recorded.
- A retry after a payment was marked Refunded but before the order was saved is
  safe: the payment operation is idempotent and the retry can complete the
  order cancellation.
- Payments for a different customer must not be changed, even when their
  order identifier matches.
- This application has no payment-provider integration. `Refunded` and
  `Cancelled` are local record statuses only; this feature does not move funds
  or cancel an external authorization.

## Requirements

### Functional Requirements

- **FR-001**: The API MUST authenticate the caller and allow cancellation only
  for the authenticated order owner. Orders MUST mint a short-lived internal
  JWT after validating the owner; ordinary customer tokens MUST NOT be accepted
  by the Payments cancellation route.
- **FR-002**: The API MUST accept cancellation only while the order status is
  Pending.
- **FR-003**: The payment service MUST mark every Completed payment belonging
  to the order owner as Refunded before the order status is changed.
- **FR-004**: The payment service MUST mark any Pending payment belonging to
  the order owner as Cancelled before the order status is changed.
- **FR-005**: If there is no payment, cancellation may proceed without a
  refund. If the payment service fails or rejects the refund, the order MUST
  remain Pending.
- **FR-006**: The payment operation MUST be idempotent so a retry after a
  successful local refund can complete the order status update.
- **FR-007**: Admin-only generic order status updates MUST not be available to
  customer tokens.
- **FR-008**: Customer UI MUST offer cancellation only for Pending orders and
  explain that a payment status update is not a real funds transfer.

### Key Entities

- **Order**: Existing order record; status transitions from Pending to
  Cancelled. No new columns are required.
- **Payment**: Existing payment record; Completed transitions to Refunded and
  Pending transitions to Cancelled. These values represent local demo state.

## Success Criteria

- **SC-001**: An owner can cancel a Pending order; the order stays unchanged
  when the payment/refund operation fails.
- **SC-002**: A customer cannot cancel another user's order or use the generic
  status endpoint to change arbitrary order statuses.
- **SC-003**: All API tests, frontend build, and gateway build pass.
- **SC-004**: The customer interface clearly distinguishes a recorded demo
  refund from an actual payment-provider refund.

## Assumptions

- Existing JWTs include the user's `ClaimTypes.NameIdentifier`.
- Order and payment records use the same user identifier.
- The Payments API is reachable by the Orders API over the internal service
  network.
- The demo's payment records are simulated; provider refunds are out of scope.
