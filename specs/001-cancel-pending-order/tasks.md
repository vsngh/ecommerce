# Tasks: Cancel a Pending Order

**Input**: Design documents from `specs/001-cancel-pending-order/`

## Phase 1: Payments API

- [x] T001 Add an authenticated, owner-scoped order-payment state operation in `services/payments/Controllers/PaymentsController.cs` and `services/payments/Services/PaymentsService.cs`.
- [x] T002 Make payment state updates idempotent for Completed, Pending, and already Refunded/Cancelled payments.
- [x] T003 Add JWT validation and the `payments.refund` policy for administrative refunds in `services/payments/Program.cs`.
- [x] T004 Add service and controller tests for ownership, completed refund, pending cancellation, and retries in `services/payments/tests/PaymentsServiceAndControllerTests.cs`.

## Phase 2: Orders API

- [x] T005 Add an internal typed Payments client with a short-lived delegated JWT and configurable base URL.
- [x] T006 Add owner- and status-checked order cancellation; do not change the order when Payments fails.
- [x] T007 Protect generic order status updates with `orders.manage`.
- [x] T008 Add controller/service tests for owner, missing identity, invalid status, payment failure, and success.
- [x] T009 Configure the internal Payments URL and JWT signing key in Compose and AKS.

## Phase 3: Customer UI and verification

- [x] T010 Add a confirmation-backed Cancel Order action for Pending orders and display the demo-refund limitation.
- [x] T011 Update service documentation for route, status behavior, configuration, and provider limitation.
- [x] T012 Run all API test projects, the gateway build, frontend build, and deployment configuration checks.
