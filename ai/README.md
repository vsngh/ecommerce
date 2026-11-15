# Responsible AI baseline

**Status: Draft; requires product, security, privacy, and legal approval before
any AI feature is enabled.**

AI features are deferred until an approved Azure AI Foundry model deployment
and access are available. No model endpoint, credentials, or AI-backed runtime
is configured in this repository.

The storefront includes a deterministic customer-service assistant for
documented FAQs, the authenticated customer's order status, and recommendations
ranked from the current product catalog. It does not call an AI model and is
not a substitute for the model-backed agents described below.

## Proposed safeguards

- Use only an approved model deployment and explicitly approved data sources.
- Keep initial retrieval limited to public product catalogue data and
  authenticated users' own orders. Do not retrieve another customer's records.
- Retrieve through existing APIs and enforce their authorization before
  passing content to a model. Never place secrets, credentials, payment
  details, or raw authentication tokens in prompts or retrieved content.
- Treat product descriptions, FAQs, and uploaded documents as untrusted input.
  Ignore instructions embedded in retrieved content that conflict with system
  policy or access controls.
- Clearly identify AI-generated responses, distinguish retrieved facts from
  suggestions, and state when the system cannot answer with confidence.
- Provide a human-support path for unresolved questions, disputes, refunds,
  safety concerns, or requests outside approved scope.
- Keep any assistant read-only for orders, payments, inventory, and user
  accounts. Users must confirm transactions through the existing application
  APIs; a model response is never proof an operation succeeded.
- Minimize prompts and telemetry. Define retention, access, deletion, and
  incident-response controls before production; do not log full prompts or
  personal data by default.
- Evaluate factual grounding, authorization boundaries, prompt-injection
  resistance, harmful content, latency, accessibility, and escalation before
  release and after model or data-source changes.

## Release gates

Before implementing or deploying the AI features in `todo.md`, obtain:

1. An Azure subscription/resource group and permissions to create and deploy an
   Azure AI Foundry project and model deployment.
2. The approved model deployment name and endpoint configuration.
3. Product/security/privacy approval of data sources, prompt/response handling,
   evaluation criteria, telemetry and retention, escalation, and operating
   ownership.
4. A reviewed threat model and passing authorization and retrieval-isolation
   tests.

These safeguards are a starting proposal, not an organization-approved policy
or a claim of regulatory compliance.
