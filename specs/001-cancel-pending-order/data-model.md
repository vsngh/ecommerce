# Data Model: Cancel a Pending Order

No schema migration is required.

## Order

| Field | Change |
| --- | --- |
| `Id` | Unchanged; identifies the order |
| `UserId` | Unchanged; compared with authenticated identity |
| `Status` | `Pending` may transition to `Cancelled` through the owner cancellation workflow |

## Payment

| Field | Change |
| --- | --- |
| `OrderId` | Unchanged; selects payments for the order |
| `UserId` | Unchanged; payment service checks owner before mutation |
| `Status` | `Completed` becomes `Refunded`; `Pending` becomes `Cancelled`; other states are unchanged |

`Refunded` and `Cancelled` describe local demo records only. No funds are
transferred by this model.
