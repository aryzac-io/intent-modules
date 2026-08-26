# Design Document

## Architecture Summary

`Aryzac.Security.Service` will be a standalone companion Intent module. Installing it into a compatible ASP.NET Core Host Application turns that application into a complete Security Authority while preserving existing host behavior. It depends on the `Aryzac.Security` contract module but does not extend that module's current responsibilities.

The module is additive and capability-driven:

- generated single-file C# templates own authority contracts, records, protocol endpoints, management endpoints, validation, cryptography, cleanup, and conformance tests;
- factory extensions integrate with host registration, middleware, persistence, transactions, authentication, authorization, configuration, and failure reporting without replacing existing registrations;
- host-defined persistence and Tenant Adapter contracts remain technology-neutral extension points;
- browser UI, local passwords, tenant hierarchy ownership, and product-specific persistence, messaging, auditing, secrets, or deployment integrations remain out of scope.

## Settled Decisions

- The module remains standalone and requires a compatible `Aryzac.Security` module contract. A future rework of `Aryzac.Security` is outside this spec.
- Post-commit adapter failures use a generated host failure-handler contract. Durable outbox semantics are not required by this MVP.
- Direct API Key permissions are represented canonically as Grants. API contracts may accept Scope keys, but persistence translates them into API Key Grant records.
- MVP token signing is RSA only; JWKS publishes RSA `kid`, `alg`, `kty`, `n`, and `e` values.
- The Tenant Adapter is required only when tenant-scoped records, resource hierarchy, or contextual authorization are enabled.
- OAuth Clients have a separate zero-or-more Post-Logout Redirect URI collection.
- External OIDC callbacks support both GET query and POST `form_post` response modes.
- Device User Codes are stored as canonical uppercase eight-character values and lookup ignores case and the optional hyphen.
- Management routes use `/api/v1/security/{resource}`; standard OIDC/OAuth routes remain unversioned.

## Module and Architecture Prerequisites

### Authoring application prerequisites

Before model work, install the standard C# module-building bundle into `Aryzac.Security.Service`:

| Module | Version | Purpose |
|---|---:|---|
| `Intent.ModuleBuilder` | `3.18.10` | Adds the Module Builder designer and module metadata model. |
| `Intent.ModuleBuilder.CSharp` | `3.7.6` | Adds C# File Builder templates for generated authority code. |
| `Intent.ModuleBuilder.AutoCompile` | `3.4.2` | Compiles the module during Software Factory execution. |
| `Intent.VisualStudio.Projects` | `4.1.8` | Models the standalone module project and output structure. |

Dependencies of these modules are resolved automatically. No other authoring module is required.

### Generated-host prerequisites

- Require a compatible `Aryzac.Security` contract, initially aligned to modeled version `1.0.2-pre.0`.
- Require a compatible ASP.NET Core HTTP host capability.
- Require a host persistence capability that advertises uniqueness, optimistic concurrency, transactions where supported, atomic credential rotation, and one-time redemption.
- Require a Tenant Adapter only when tenant-scoped capabilities are enabled.
- Do not mandate EF Core, a database engine, message bus, audit product, secret store, or deployment platform.

## Model Changes

### `Aryzac.Security.Service` — Module Builder designer

Create one `Intent Module` package named `Aryzac.Security.Service` with `Module Settings`:

- Version: initial prerelease version selected during implementation.
- Include in Module: `true`.
- Include Release Notes: `true`.
- Dependency: compatible `Aryzac.Security` contract.

Create the following model areas.

### Module settings

Add `Module Settings Configuration` elements using `Configuration(Settings Type=Application Settings)`:

1. `Authority Features`
   - Authorization Code, Client Credentials, Refresh Token, Device Authorization, External Identity Provider Brokering, Management APIs, Integration Events, Lifecycle Notifications, Auditing.
   - Each is an independent boolean except validation enforces Refresh Token requires Authorization Code or Device Authorization.
2. `Authority Protocol`
   - Issuer; Access Token minutes default 60 range 1-1440; ID Token minutes default 5 range 1-1440; Refresh Token days default 30 range 1-365; SSO Session lifetime default 8 hours range 5 minutes-30 days; clock skew seconds default/max 60; device polling seconds default 5; API Key prefix; HTTPS required outside Development.
3. `Authority Tenancy`
   - Tenant-scoped capabilities enabled; contextual claim names; inheritance evaluation enabled.
4. `Authority Bootstrap`
   - exactly one Bootstrap Strategy: Explicit Identity, First Eligible User, Custom Seed Function; explicit issuer/subject or normalized email fields where applicable.
5. `Authority Data Lifecycle`
   - revoked metadata retention default 30 days range 1-3650; code/device cleanup delay range 1-7 days and not before 24 hours; SSO/refresh cleanup range 30-90 days.
6. `Authority Routes`
   - fixed defaults for standard routes and `/api/v1/security`; generated validation reports collisions rather than silently changing routes.

Each field uses `Field Configuration` with the appropriate Checkbox, Select, Number, or Text control, default value, allowed range/options, and help text containing the relevant requirement constraint.

### Generated contracts and records

Add `C# Template` elements with required type reference `Single File`, `C# Template Settings(Templating Method=C# File Builder)`, and `Template Settings(Source=Lookup Type, Role=<role>, Default Location=Security)`.

The generated contract templates are:

- `SecurityAuthorityOptions` — strongly typed feature, protocol, tenancy, bootstrap, retention, cleanup, route, and cryptography options with startup validation.
- `SecurityAuthorityContracts` — Tenant Adapter, persistence capability descriptor, unit-of-work/atomic-operation boundary, signing-key provider, secret protector, clock, idempotency store, live-principal validation, Lifecycle Notification Adapter, Audit Adapter, Integration Event handler, and post-commit delivery failure handler contracts.
- `SecurityAuthorityRecords` — User, External Identity, Service, OAuth Client, Redirect URI, Post-Logout Redirect URI, Identity Provider, API Key, Tenant Resource, Role, Role Membership, Grant, Authorization Code, Device Grant, Refresh Token, Access Token Metadata, ID Token Metadata, SSO Session, bootstrap state, idempotency outcome, and processed Integration Event records.
- `SecurityAuthorityEnums` — exact lifecycle, client, provider, grant-target, effect, applicability, credential, bootstrap, device, and protocol enum values.
- `SecurityAuthorityValidation` — bounded fields, URI rules, exact/case-sensitive comparison rules, uniqueness, reference, tenant ancestry, lifecycle, feature-dependency, concurrency, and secret-exclusion validation.

Record precision:

- Apply generated validation metadata for every R3 length/range/required constraint.
- `PrincipalType` remains User, Service, or Role. `GrantTargetType` is User, Service, Role, or ApiKey.
- OAuth Client contains separate Redirect URI and Post-Logout Redirect URI collections.
- API Key direct Scope input is translated to Grant records; no independent persisted Scope collection exists.
- Access Token Metadata records token identifier, signing `kid`, issuer, audience, subject, Principal Type, Scopes, issued-at, not-before, expiry, contextual claim snapshot, and revocation metadata when applicable.
- ID Token Metadata records token identifier, signing `kid`, issuer, client audience, User subject, issued-at, expiry, optional nonce hash, and issuance status.
- Redeemable secrets are represented only by hashes or opaque identifiers after one-time return.

### Protocol templates

Create these single-file C# templates:

- `SecurityAuthorityDiscoveryEndpoints` — anonymous discovery and RSA JWKS endpoints.
- `SecurityAuthorityAuthorizationEndpoints` — `/connect/authorize`, GET and POST `/connect/callback`, provider selection, protected single-use correlation state, exact redirects, PKCE S256, Scope filtering, and Authorization Code issue.
- `SecurityAuthorityTokenEndpoint` — form-urlencoded `/connect/token`, enabled grant dispatch, OAuth error mapping, Access/ID Token creation, Client Credentials, atomic Refresh Token rotation, replay lineage revocation, and Device Code redemption.
- `SecurityAuthorityDeviceEndpoints` — device authorization, authenticated review, approval/denial, canonical User Code normalization, polling interval enforcement, expiry, and one-time redemption.
- `SecurityAuthoritySessionEndpoints` — SSO cookie/session handling, UserInfo, and safe logout redirect behavior.
- `SecurityAuthorityExternalProviders` — Generic OIDC plus Entra External ID, Entra ID, Google, Auth0, and Keycloak presets; no deployment identifiers or secrets are hard-coded.
- `SecurityAuthorityCryptography` — RSA signing, key rotation ordering, retained verification keys, keyed API Key hashing, constant-time comparison, credential hashing, secret redaction, and development-only ephemeral key behavior.

### Administration and authorization templates

Create these single-file C# templates:

- `SecurityAuthorityManagementEndpoints` — `/api/v1/security` JSON endpoints for all R14 resources, pagination, deterministic ordering, status codes, RFC 9457 field errors, management Scopes, idempotency, and concurrency tokens.
- `SecurityAuthorityBootstrap` — three bootstrap strategies, atomic first-user compare-and-set, closure/reset behavior, and initial administrator Grants.
- `SecurityAuthorityLifecycle` — User and Service transitions plus cascading credential, membership, and Grant revocation.
- `SecurityAuthorityAuthorizationEngine` — Tenant Adapter validation, resource ancestry traversal, inheritance boundary, Role Membership contribution, Deny precedence, effective Scope calculation, and cache invalidation before the next authorization.
- `SecurityAuthorityIntegrationEvents` — versioned event envelope validation, at-most-once processing, exact rejection reasons, and record/lifecycle handlers.
- `SecurityAuthorityPostCommitDispatch` — after-commit Lifecycle Notification and Audit Adapter dispatch with secret-safe payloads and failure-handler reporting without transaction rollback.
- `SecurityAuthorityCleanup` — bounded retention and race-safe cleanup using committed expiry, revocation, redemption, and concurrency state.

### Registration and additive integration

Add `Factory Extension` elements:

- `SecurityAuthorityModuleDependencyValidationExtension` — validates compatible `Aryzac.Security`, ASP.NET Core host capability, and persistence capability; throws actionable `FriendlyException` messages for application-level missing/incompatible capabilities.
- `SecurityAuthorityRegistrationExtension` — registers generated contracts and services through the host's established registration/broadcast mechanisms; detects and reports route, authentication scheme, configuration key, persistence, and service-registration conflicts without replacement.
- `SecurityAuthorityMiddlewareExtension` — adds authority middleware/endpoints without changing existing route templates, middleware order, authentication schemes, policies, persistence mappings, or business services.
- `SecurityAuthorityPersistenceExtension` — integrates authority-owned records with host persistence and transaction boundaries without owning a database technology.
- `SecurityAuthorityConfigurationValidationExtension` — emits startup validation for production cryptography, conditional Tenant Adapter, persistence guarantees, settings ranges, and feature dependencies.

Use `ElementException` only when a generation error is tied to a specific modeled element; use `FriendlyException` for missing modules, host capabilities, or incompatible application settings.

### Package dependencies and registrations

Model `NuGet Packages` only for libraries not already supplied by the compatible host/module ecosystem. Prefer ASP.NET Core framework capabilities and Microsoft identity-model abstractions; avoid introducing a product-specific authorization server dependency. Generated DI and app-settings wiring must use common broadcast/handle mechanisms rather than direct coupling to a specific host module.

### Conformance templates

Create `SecurityAuthorityConformanceTests` as generated test infrastructure covering:

- every acceptance criterion in R4-R10;
- atomicity, idempotency, concurrency, cleanup races, and secret exclusion in R12-R16;
- additive-installation preservation and generation idempotency from R1;
- dedicated-host and existing-host fixture modes;
- enabled/disabled feature combinations, including invalid Refresh Token combinations.

## Journey Coverage

- `UJ-1` flows through settings validation, registration extensions, persistence capability validation, discovery/JWKS, management endpoints, and token issuance.
- `UJ-2` flows through conflict detection and additive registration/middleware/persistence extensions; no existing registration is overwritten.
- `UJ-3` flows through authorization, provider callback, User/External Identity linking, SSO Session creation, Authorization Code redemption, token issuance, UserInfo, and logout.
- `UJ-4` flows through Device Authorization creation, host-provided authenticated approval UI calling modeled endpoints, polling, and terminal redemption/denial/expiry.
- `UJ-5` flows through scoped management endpoints, idempotency/concurrency, lifecycle/authorization invalidation, and optional post-commit notifications/audit.
- `UJ-6` flows through versioned Integration Event validation, deduplication, atomic mutation, notification, and replay-safe success.

## Per-Requirement Realization Plan

### R1 — Complete Additive Module Installation

- `[model]` Module dependency, feature settings, registration/middleware/persistence/configuration factory extensions, all enabled template registrations, and conformance template.
- `[code]` Implement capability lookup, conflict diagnostics naming both registrations, feature gating, and idempotent template/factory-extension behavior in generated module source.
- Mechanism: modeled `Factory Extension`, `Module Settings Configuration`, and `C# Template` elements.
- Depends on: authoring prerequisites and compatible `Aryzac.Security` contract.
- Code placement: after module package/settings/templates are generated.

### R2 — Host Persistence and Tenant Integration

- `[model]` Persistence capability, transaction boundary, Tenant Adapter contracts, conditional startup validation, Tenant Resource record, and persistence factory extension.
- `[code]` Implement ancestry validation, cycle/cross-Tenant rejection, capability checks, and atomic host transaction integration.
- Mechanism: modeled contract and validation templates plus persistence/configuration factory extensions.
- Depends on: R1 registration foundation.
- Code placement: after contract and extension generation.

### R3 — Authoritative Security Record Contract

- `[model]` `SecurityAuthorityRecords`, enums, and validation templates containing every glossary record, association, bound, uniqueness rule, lifecycle field, and concurrency stamp.
- `[code]` Implement generated validation metadata and invariant checks; no hand-written target-app record scaffolding.
- Mechanism: modeled single-file C# templates.
- Depends on: none beyond R1 module package.
- Code placement: immediately after record template generation; other requirements depend on it.

### R4 — Cryptographic Material and Secret Handling

- `[model]` Cryptography options/contracts/template and secret-safe DTO projections.
- `[code]` Implement RSA signing/JWKS, development ephemeral key warning, production validation, encryption/hashing/redaction, rotation ordering, and retained public keys.
- Mechanism: modeled C# template and configuration-validation factory extension.
- Depends on: R3 credential/token records.
- Code placement: after R3 generation.

### R5 — Discovery, JWKS, and OAuth Client Validation

- `[model]` Discovery endpoint and OAuth Client validation templates, exact Redirect URI and Post-Logout Redirect URI records.
- `[code]` Implement anonymous metadata/JWKS responses, issuer parity, exact ordinal redirect validation, Public/Confidential authentication rules, and inactive-client rejection.
- Mechanism: modeled C# endpoint templates.
- Depends on: R3 and R4.
- Code placement: after record and cryptography generation.

### R6 — Authorization Code with PKCE

- `[model]` Authorization endpoints, correlation state, Authorization Code, External Identity, User, SSO Session, and provider templates.
- `[code]` Implement provider selection, GET/POST callback validation, single-use ten-minute state, five-minute codes, S256, exact redirect, Scope filtering, and Invite/Open SSO linking rules.
- Mechanism: modeled C# endpoint templates.
- Depends on: R3-R5 and R10 provider configuration.
- Code placement: after all dependent modeling and generation.

### R7 — Token Endpoint and Credential Issuance

- `[model]` Token endpoint, token metadata, Refresh Token, duration settings, and protocol error templates.
- `[code]` Implement enabled grant dispatch, claims, durations, atomic rotation/replay handling, Client Credentials, and exact HTTP/OAuth errors.
- Mechanism: modeled C# templates and settings.
- Depends on: R3-R6 and R8 for Device Code grant.
- Code placement: interleaved after each supported grant template exists, finalized after R8.

### R8 — Device Authorization

- `[model]` Device endpoints and Device Grant record/status fields.
- `[code]` Implement high-entropy codes, canonical User Code generation/lookup, authenticated approval/denial, interval enforcement, expiry, and single-use redemption.
- Mechanism: modeled C# endpoint template.
- Depends on: R3, R5, R7 token issuance, and R9 authenticated SSO principal.
- Code placement: after dependency generation.

### R9 — SSO Session, UserInfo, and Logout

- `[model]` Session endpoints, SSO Session record, cookie settings, UserInfo projection, and Post-Logout Redirect URI collection.
- `[code]` Implement opaque protected cookie, server-side checks, configured claims, exact logout redirect, fallback `/`, and non-revoking logout semantics.
- Mechanism: modeled C# endpoint/settings templates.
- Depends on: R3-R5.
- Code placement: before completing R6 and R8 interactive flows.

### R10 — External Identity Provider Brokering

- `[model]` Provider configuration record, presets, encrypted-secret contract, and callback support templates.
- `[code]` Implement Generic OIDC and five presets, Invite/Open SSO behavior, issuer-subject uniqueness, no-user-merge invariant, GET/query and POST/form_post callbacks, and safe error redirects.
- Mechanism: modeled C# provider template.
- Depends on: R3-R5.
- Code placement: before finalizing R6.

### R11 — Administrator Bootstrap and User Lifecycle

- `[model]` Bootstrap settings/state/template and lifecycle template.
- `[code]` Implement all three strategies, atomic compare-and-set, permanent closure/reset, allowed User transitions, non-Active denial, and cascading revocation.
- Mechanism: modeled settings and C# templates.
- Depends on: R3 records, R12 API Keys, and R13 Grants/memberships for complete cascading revocation.
- Code placement: bootstrap core after R3; lifecycle cascade finalized after R12-R13.

### R12 — Services and API Keys

- `[model]` Service/API Key records, management operations, Grant translation, hashing options, and lifecycle integration.
- `[code]` Implement idempotent Service provisioning, active-state enforcement, cryptographic key issue/regeneration, one-time clear return, constant-time authentication, last-used update, and effective Grants-minus-Denies.
- Mechanism: modeled records, management, cryptography, and lifecycle templates.
- Depends on: R3, R4, R13.
- Code placement: after authorization engine generation.

### R13 — Resource, Role, Grant, and Authorization Administration

- `[model]` Tenant Resource, Role, Role Membership, Grant, Grant Catalog records and authorization engine template.
- `[code]` Implement ancestry walk, inheritance protection, Tenant boundaries, Deny precedence, enabled/expiry/revocation filtering, invalidation, and catalog behavior.
- Mechanism: modeled records and C# authorization template.
- Depends on: R2 Tenant Adapter and R3 records.
- Code placement: after R2-R3 generation; before R12 completion.

### R14 — Management API Contract

- `[model]` Management endpoint template, management Scope catalog, pagination/idempotency/concurrency contracts, and secret-safe projections.
- `[code]` Implement `/api/v1/security` endpoints, action-specific Scopes, status contract, RFC 9457 field errors, deterministic paging, 24-hour idempotency, and stale-token conflicts.
- Mechanism: modeled C# endpoint template using `Aryzac.Security` Scope contracts.
- Depends on: R3 and each managed capability R10-R13.
- Code placement: interleaved by resource slice, finalized after all records/operations exist.

### R15 — Integration Events, Lifecycle Notifications, and Auditing

- `[model]` feature settings, event envelope/processed-event records, event handling template, notification/audit contracts, post-commit dispatch template, and failure-handler contract.
- `[code]` Implement version validation, atomic deduplication, exact rejection reasons, non-secret post-commit payloads, optional adapters, and failure surfacing without rollback or false replay.
- Mechanism: modeled settings/contracts/templates; host failure hook selected instead of a required outbox module.
- Depends on: R2 transaction boundary and all mutable records from R3/R10-R13.
- Code placement: after lifecycle operations are modeled so all transitions can publish consistently.

### R16 — Failure Safety, Concurrency, and Data Lifecycle

- `[model]` optimistic concurrency fields, atomic operation contract, retention/cleanup settings, cleanup template, and conformance tests.
- `[code]` Implement transaction scopes, compare-and-set/update semantics, cleanup race handling, UTC/stable-ID/case rules, and complete conformance coverage.
- Mechanism: modeled records/settings/templates and persistence capability validation.
- Depends on: all record and operation requirements R2-R15.
- Code placement: concurrency primitives early; cleanup and full conformance suite after all features.

## Cross-Requirement Build Order

1. Module prerequisites and standalone package foundation: R1.
2. Core host contracts and authoritative records: R2-R3.
3. Cryptography and client validation: R4-R5.
4. SSO and provider brokering: R9-R10.
5. Interactive and device protocols: R6-R8.
6. Tenant-aware authorization administration: R13.
7. Bootstrap, Services, API Keys, and management API: R11-R12-R14.
8. Integration events, notifications, auditing, lifecycle cleanup, and conformance: R15-R16.

All work occurs in the Module Builder and Visual Studio designers for `Aryzac.Security.Service`; there is no Domain → Services → UI designer chain. Within the generated authority architecture, records/contracts precede endpoints and endpoints precede host wiring/tests.

## Out-of-Scope Enforcement

The model will not add browser pages, password storage/authentication, a tenant hierarchy, dynamic client registration, consent UI, SCIM, SAML, WebAuthn/passkeys, token introspection, production certificate provisioning, DNS/TLS provisioning, or product-specific persistence/message-bus/audit/secrets integrations.
