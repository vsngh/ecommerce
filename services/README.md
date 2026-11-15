# Microservice reference

The application has six .NET 8 APIs behind the YARP gateway. The gateway routes
requests by service prefix and serves the API Swagger documents at `/swagger`.
When using the frontend's Nginx proxy, prefix gateway paths with `/api`.

| Service | Gateway path | Main responsibility | Database setting |
| --- | --- | --- | --- |
| Orders | `/orders` | Orders and order items | `ConnectionStrings__OrdersDatabase` |
| Products | `/products` | Product catalogue and pricing | `ConnectionStrings__ProductsDatabase` |
| Users | `/users` | Account creation, login, and user administration | `ConnectionStrings__UsersDatabase` |
| Inventory | `/inventory` | Available and reserved stock | `ConnectionStrings__InventoryDatabase` |
| Cart | `/cart` | Carts, items, and checkout initiation | `ConnectionStrings__CartDatabase` |
| Payments | `/payments` | Payment records and demo refund state | `ConnectionStrings__PaymentsDatabase` |

Each API also requires `ConnectionStrings__Redis`. SQL Server and Redis are
configured by environment variables in Docker Compose and Kubernetes. Keep
production connection strings and signing keys in a managed secret store.

## API routes

The controller route is appended to its gateway prefix. For example, the
customer order-history route is `/orders/api/orders/mine`, or
`/api/orders/api/orders/mine` through the frontend's Nginx proxy.

| Service | Controller routes |
| --- | --- |
| Orders | `GET /api/orders`, `GET /api/orders/mine`, `GET /api/orders/{orderId}`, `POST /api/orders`, `POST /api/orders/bulk-upload`, `PUT /api/orders/{orderId}`, `PATCH /api/orders/{orderId}/status`, `DELETE /api/orders/{orderId}`, and `GET|POST|PUT|DELETE /api/orders/{orderId}/items[/{orderItemId}]` |
| Products | `GET /api/products`, `GET /api/products/{productId}`, `POST /api/products`, `PUT /api/products/{productId}`, `PATCH /api/products/{productId}/stock`, `PATCH /api/products/{productId}/price`, `DELETE /api/products/{productId}` |
| Users | `POST /api/users/login`, `GET /api/users`, `GET /api/users/{userId}`, `POST /api/users`, `PUT /api/users/{userId}`, `PATCH /api/users/{userId}/role`, `PATCH /api/users/{userId}/password`, `DELETE /api/users/{userId}` |
| Inventory | `GET /api/inventory`, `GET /api/inventory/{inventoryItemId}`, `GET /api/inventory/products/{productId}`, `POST /api/inventory`, `PUT /api/inventory/{inventoryItemId}`, `PATCH /api/inventory/{inventoryItemId}/quantity`, `PATCH /api/inventory/{inventoryItemId}/reserve`, `PATCH /api/inventory/{inventoryItemId}/release`, `DELETE /api/inventory/{inventoryItemId}` |
| Cart | `GET /api/carts`, `GET /api/carts/{cartId}`, `GET /api/carts/users/{userId}/active`, `POST /api/carts`, `PATCH /api/carts/{cartId}/status`, `DELETE /api/carts/{cartId}`, `GET|POST /api/carts/{cartId}/items`, `GET|PUT|DELETE /api/carts/{cartId}/items/{cartItemId}`, `POST /api/carts/{cartId}/checkout` |
| Payments | `GET /api/payments`, `GET /api/payments/{paymentId}`, `GET /api/payments/orders/{orderId}`, `POST /api/payments`, `PATCH /api/payments/{paymentId}/status`, `POST /api/payments/{paymentId}/refund`, `POST /api/payments/orders/{orderId}/refund` |

The API controllers are the authoritative source for request and response DTOs,
status codes, and route details. Open Swagger UI through the gateway at
`/swagger`.

## Order status notifications

Authenticated clients can connect to the Orders SignalR hub at
`/orders/api/orders/hubs/notifications` through the gateway, or
`/api/orders/api/orders/hubs/notifications` through the frontend proxy. The
client supplies its JWT using SignalR's `accessTokenFactory`. The hub sends an
`OrderStatusChanged` event only to the order owner's user connection, with
`orderId`, `userId`, `status`, and `occurredAt` fields. The Redis backplane
supports delivery across Orders API replicas.

Status changes and their notification records are saved atomically to the
Orders database. A hosted worker polls the outbox, attempts each send up to ten
times, and marks successful dispatches; exhausted rows remain available with
their last error for inspection. Dispatch is at-least-once: a send that
succeeds just before a database failure can be repeated. The Redis
backplane supports multiple Orders API replicas, but SignalR does not confirm
that a browser received the event; clients should refresh order state from
`GET /api/orders/mine` when reconnecting.

## Authentication and authorization

Users issues a two-hour JWT. Configure the same `Jwt__SigningKey`, issuer, and
audience on each token-validating API. The signing key must be at least 32
random characters and must not be committed to source. Orders uses the shared
key to mint a one-minute internal token after it checks the caller's identity
and order ownership. Orders also requires `Services__Payments__BaseUrl` to
reach Payments internally.

| API operation | Enforcement |
| --- | --- |
| User listing, lookup, update, role/password change, deletion | `users.manage` |
| Inventory controller | Valid JWT with `inventory.manage` |
| Product create, update, stock/price changes, deletion | Valid JWT with `products.manage` |
| All-order list and bulk upload | `orders.read` and `orders.bulk.upload`, respectively |
| Generic order-status updates | `orders.manage` |
| Create order | Authenticated user with `orders.create`; customer owner/status fields are set by the API |
| Order update, deletion, and item mutations | `orders.manage` |
| Customer order history and individual order/item reads | Authenticated user; individual reads are limited to the owner or `orders.read` |
| Customer order cancellation | Authenticated owner; only Pending orders; calls the Payments API before changing status |
| Cart access and mutations | Valid JWT with `carts.manage`, plus cart-owner checks; all-cart listing and status changes require `carts.read` |
| Payment listing | `payments.read` |
| Payment-by-ID and by-order reads | Authenticated user; payment records are limited to their owner unless `payments.read` |
| Create payment | `payments.create`; customer owner and initial `Pending` status are set by the API |
| Payment status changes and payment-by-ID refund | `payments.manage` and `payments.refund`, respectively |
| Order payment refund/cancellation | Internal Orders API token with `payments.order.cancel`; ordinary customer tokens are not accepted |

Gateway routing does not itself authorize a request. The APIs enforce the
policies above, and owner checks return Not Found for resources belonging to
another user.

Customer order cancellation marks Completed payment records as Refunded and
Pending payment records as Cancelled, then sets the order to Cancelled. Orders
validates the customer's ownership before minting a short-lived internal token
for Payments. These are local demo record states only: the application has no
payment-provider integration, so no funds are actually moved. If the Payments
API is unavailable or rejects the request, the order remains Pending and the
customer can retry. Orders uses `Services__Payments__BaseUrl` for its internal
Payments API endpoint.

Roles and their issued permissions are defined in
[`users/Security/PermissionCatalog.cs`](./users/Security/PermissionCatalog.cs).
The UI role check is not a security boundary; API authorization is required for
every protected operation.

## Database setup

APIs do not apply EF Core migrations when they start. Provision each database
and apply its migrations before routing application traffic. For example:

```powershell
dotnet ef database update --project services/orders/Orders.Api.csproj
dotnet ef database update --project services/products/Products.Api.csproj
dotnet ef database update --project services/users/Users.Api.csproj
dotnet ef database update --project services/inventory/Inventory.Api.csproj
dotnet ef database update --project services/cart/Cart.Api.csproj
dotnet ef database update --project services/payments/Payments.Api.csproj
```

Set the matching connection string in the environment when running each
migration command. Never place production credentials in a checked-in
`appsettings` file.

## Tests

Each API with a test project uses xUnit and EF Core InMemory for focused
service/controller tests. Current suites cover representative persistence,
business outcomes, missing records, authentication/ownership, policy metadata,
request validation, and controller response semantics; they are not exhaustive
end-to-end tests against SQL Server, Redis, or a live identity provider.
Run a suite with `--collect:"XPlat Code Coverage"` to produce a Cobertura
report under that test project's `TestResults` directory. CI uploads one
14-day coverage artifact per API test project.

```powershell
dotnet test services/cart/tests/Cart.Api.Tests.csproj --configuration Release --collect:"XPlat Code Coverage"
dotnet test services/inventory/tests/Inventory.Api.Tests.csproj --configuration Release
dotnet test services/orders/tests/Orders.Api.Tests.csproj --configuration Release
dotnet test services/payments/tests/Payments.Api.Tests.csproj --configuration Release
dotnet test services/products/tests/Products.Api.Tests.csproj --configuration Release
dotnet test services/users/tests/Users.Api.Tests.csproj --configuration Release
dotnet build services/gateway/Gateway.Api.csproj --configuration Release
```

The gateway currently has no dedicated test project. The CI workflow runs the
API suites, builds the gateway and frontend, and builds container images; see
[`../.github/workflows/ci.yml`](../.github/workflows/ci.yml).
