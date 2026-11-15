# Implementation Plan: Cancel a Pending Order

**Branch**: `001-cancel-pending-order` | **Date**: 2026-11-05 | **Spec**: [spec.md](./spec.md)

## Summary

Add an authenticated owner cancellation route to Orders. Before changing the
order from Pending to Cancelled, call an internal, owner-scoped Payments route
that idempotently records Completed payments as Refunded and Pending payments
as Cancelled. Orders mints a short-lived internal JWT with a dedicated
permission after checking order ownership. If the Payments call fails, do not
update the order.

## Technical Context

**Language/Version**: C# / .NET 8; React / Vite
**Primary Dependencies**: ASP.NET Core, EF Core, HttpClient, JWT, xUnit
**Storage**: SQL Server per service; Redis cache
**Testing**: xUnit and EF Core InMemory; frontend production build
**Target Platform**: Docker Compose and AKS
**Project Type**: Web application / microservices
**Performance Goals**: One internal Payments request per cancellation
**Constraints**: No shared transaction across service databases; no payment provider integration
**Scale/Scope**: One Orders API workflow, one Payments API operation, and one customer UI action

## Constitution Check

- API behavior is authenticated and ownership-scoped.
- Service contracts remain explicit and tests cover success and failure.
- The payment operation is idempotent to support recovery from cross-service
  partial completion.
- No schema change or external credential is needed.
- UI and documentation disclose that no real funds are moved.

## Project Structure

```text
services/orders/
├── Controllers/OrdersController.cs
├── Services/OrdersService.cs
├── Services/IOrdersService.cs
├── Payments/
│   ├── IPaymentsClient.cs
│   └── PaymentsClient.cs
└── tests/OrdersServiceAndControllerTests.cs

services/payments/
├── Controllers/PaymentsController.cs
├── Services/PaymentsService.cs
├── Services/IPaymentsService.cs
└── tests/PaymentsServiceAndControllerTests.cs

services/frontend/src/main.jsx
docker-compose.yml
deploy/kubernetes/aks/workloads.yaml
services/README.md
```

## Risks

The Payments and Orders databases cannot commit atomically. Idempotent payment
status changes allow a later cancellation retry to complete after a partial
success. A real payment provider will require a durable refund workflow and
provider reconciliation before this prototype can be used for actual payments.
