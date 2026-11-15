# Ecommerce Platform Constitution

## Core Principles

### I. Explicit, compatible service contracts
Keep responsibilities in their existing service and architectural layers.
Document and test route, DTO, status-code, and authorization changes. Preserve
existing contracts unless a specification explicitly approves a breaking
change.

### II. Security and privacy by default
Authorize access at the API boundary, validate untrusted input, scope customer
data to the authenticated identity, and keep credentials in approved secret
stores. Do not treat frontend visibility or gateway routing as authorization.

### III. Tested behavior
Add focused tests for changed behavior and important failure cases before
considering an implementation complete. Run the smallest relevant test suite,
then the repository CI checks when changes cross service boundaries.

### IV. Reliable, observable operations
Keep environment-specific configuration outside source. Make failures explicit,
use health probes and bounded resource settings for deployed workloads, and
document database migration and recovery prerequisites.

### V. Simplicity and maintainability
Prefer the smallest change that fully meets the accepted requirements. Reuse
existing patterns, avoid speculative abstractions and dependencies, and update
the relevant service documentation when behavior or operations change.

## Additional Constraints

- Backend APIs and gateway target .NET 8; the frontend uses React and Vite.
- SQL Server is the system of record and Redis is a cache. Cache data must not
  bypass authorization or be treated as authoritative.
- AI features may use only approved models and data. Responses that use
  retrieval must be grounded in authorized sources, disclose uncertainty, and
  provide a human-escalation path. An AI response must not claim that an order,
  payment, refund, or inventory change has occurred unless the corresponding
  API operation succeeded.
- Deployment changes must preserve private-by-default service networking and
  use managed identities, OIDC, or managed secret stores rather than committed
  credentials.

## Development Workflow

- Use Spec Kit for non-trivial behavior changes: establish the specification,
  resolve material ambiguities, plan, derive tasks, implement, and verify
  convergence.
- Keep each change traceable to acceptance criteria and include focused tests
  and relevant documentation.
- Validate API tests, gateway/frontend builds, and deployment configuration
  appropriate to the changed files. State clearly when external infrastructure
  prevents live validation.

## Governance

This constitution guides specifications, plans, implementation, and reviews.
Security, privacy, and service-contract requirements cannot be waived
implicitly. Amendments must be explicit, reviewed, and reflected in affected
guidance and tests.

**Version**: 1.0.0 | **Ratified**: 2026-11-05 | **Last Amended**: 2026-11-05
