# Implementation Plan — Security Service Authority

## Tasks

- [x] T0. Module prerequisites and additive foundation
  - [x] T0.1 [module] (satisfies: R1) — Install `Intent.ModuleBuilder`
    - Install module `Intent.ModuleBuilder` version `3.18.10` into `Aryzac.Security.Service` to provide the Module Builder designer and module metadata model.
  - [x] T0.2 [module] (satisfies: R1) — Install `Intent.ModuleBuilder.CSharp`
    - Install module `Intent.ModuleBuilder.CSharp` version `3.7.6` before any C# File Builder template is modelled.
  - [x] T0.3 [module] (satisfies: R1) — Install `Intent.ModuleBuilder.AutoCompile`
    - Install module `Intent.ModuleBuilder.AutoCompile` version `3.4.2` so Software Factory execution compiles the authored module.
  - [x] T0.4 [module] (satisfies: R1) — Install `Intent.VisualStudio.Projects`
    - Install module `Intent.VisualStudio.Projects` version `4.1.8` before modelling the standalone project and output structure.
  - [x] T0.5 [model] (satisfies: R1) — Create the standalone `Aryzac.Security.Service` module package and settings
    - In the Module Builder designer, add one `Intent Module` package named `Aryzac.Security.Service`, included in the module with release notes enabled, an initial prerelease version, and a compatible dependency on `Aryzac.Security` initially aligned to `1.0.2-pre.0`.
    - Add `Authority Features`, `Authority Protocol`, `Authority Tenancy`, `Authority Bootstrap`, `Authority Data Lifecycle`, and `Authority Routes` application-settings configurations with the exact controls, defaults, ranges, options, fixed routes, and help-text constraints in the design.
    - R1 feature controls: independently gate Authorization Code, Client Credentials, Refresh Token, Device Authorization, external provider brokering, management APIs, Integration Events, Lifecycle Notifications, and auditing; validate that Refresh Token requires Authorization Code or Device Authorization.
  - [x] T0.6 [model] (satisfies: R1, R2, R16) — Add additive registration and capability-validation factory extensions
    - In the Module Builder designer, add `SecurityAuthorityModuleDependencyValidationExtension`, `SecurityAuthorityRegistrationExtension`, `SecurityAuthorityMiddlewareExtension`, and `SecurityAuthorityConfigurationValidationExtension` as `Factory Extension` elements.
    - R1: model compatible `Aryzac.Security`, ASP.NET Core host, route, authentication-scheme, configuration-key, persistence-registration, service-registration, and enabled-feature checks without replacement of host behavior.
    - R2: model persistence-guarantee and conditional Tenant Adapter startup validation.
    - R16: model startup checks for atomicity, uniqueness, optimistic concurrency, one-time redemption, settings bounds, HTTPS, and production cryptography.
    - Use actionable `FriendlyException` generation-time diagnostics for missing modules, capabilities, or incompatible application settings; reserve `ElementException` for a specific invalid model element.
  - [x] T0.7 [code] (satisfies: R1.1, R1.2, R1.3, R1.5, R1.6) — Implement additive capability lookup, feature gating, and conflict diagnostics
    - Implement compatible companion/host capability detection and one actionable failure per missing or incompatible capability.
    - Preserve existing routes, actions, schemes, policies, mappings, middleware, and business services.
    - Detect route, scheme, configuration-key, persistence, and service-registration conflicts and name both registrations without replacing either.
    - Gate every generated authority capability from the modelled feature settings, including the Refresh Token dependency rule.
    - Register generated services and settings through established broadcast/handle mechanisms rather than direct host-module coupling.
  - [x] T0.8 [code] (satisfies: R1.4) — Write additive-installation and generation-idempotency tests
    - Cover dedicated-host and existing-host installation fixtures, preservation of pre-existing registrations, conflict diagnostics, missing capability failures, enabled/disabled feature combinations, and invalid Refresh Token combinations.
    - Re-run generation without model or setting changes and assert no duplicate templates, endpoints, registrations, records, or configuration entries are proposed.

- [x] T1. Core host contracts, persistence, records, and validation
  - [x] T1.1 [model] (satisfies: R2, R15, R16) — Add authority host contracts and persistence integration
    - In the Module Builder designer, add the single-file C# File Builder template `SecurityAuthorityContracts` at the `Security` location.
    - Model Tenant Adapter, persistence capability descriptor, unit-of-work/atomic-operation boundary, signing-key provider, secret protector, clock, idempotency store, live-principal validation, Lifecycle Notification Adapter, Audit Adapter, Integration Event handler, and post-commit delivery failure-handler contracts.
    - Add `SecurityAuthorityPersistenceExtension` to integrate authority-owned records with host persistence and transaction boundaries without selecting a database technology.
    - R2: Tenant Adapter data includes Tenant id, Tenant Resource id, Resource Kind, optional parent id, and inheritance protection; it is conditional when tenant-scoped capabilities are disabled.
    - R16: persistence capabilities advertise uniqueness, optimistic concurrency, transactions where supported, atomic credential rotation, and one-time redemption.
  - [x] T1.2 [model] (satisfies: R3, R16) — Add authoritative enums and record contracts
    - Add `SecurityAuthorityEnums` and `SecurityAuthorityRecords` single-file C# File Builder templates at the `Security` location.
    - R3: model User, External Identity, Service, OAuth Client, Redirect URI, Post-Logout Redirect URI, Identity Provider, API Key, Tenant Resource, Role, Role Membership, Grant, Authorization Code, Device Grant, Refresh Token, Access Token Metadata, ID Token Metadata, SSO Session, bootstrap state, idempotency outcome, and processed Integration Event records with every required field, relationship, lifecycle timestamp, uniqueness rule, and bound.
    - R3: keep `PrincipalType` as User/Service/Role and `GrantTargetType` as User/Service/Role/ApiKey; persist direct API Key permissions canonically as Grant records.
    - R3: represent redeemable secrets only by hashes or opaque identifiers after one-time return, and keep Redirect URI and Post-Logout Redirect URI as separate OAuth Client collections.
    - R16: add stable identifiers, UTC timestamps, optimistic concurrency stamps to every mutable record named by R16, and committed expiry/revocation/redemption state needed by cleanup races.
  - [x] T1.3 [model] (satisfies: R3, R16) — Add comprehensive authority validation
    - Add the `SecurityAuthorityValidation` single-file C# File Builder template.
    - R3: model all required/length/range/absolute-URI, exact-comparison, uniqueness, known-reference, lifecycle, and field-addressed validation rules.
    - R2: include Tenant ancestry, cycle, empty Resource Kind, and cross-Tenant-chain validation.
    - R16: include stable-id, UTC, case-sensitivity, feature-dependency, concurrency, atomicity, and secret-exclusion invariants.
  - [x] T1.4 [code] (satisfies: R2.1, R2.3, R2.5, R2.6, R16.1, R16.2) — Implement host persistence and atomic-operation behavior
    - Integrate authority records with the host persistence abstraction while isolating authority-owned storage.
    - Join host transactions where supported and enforce atomic credential rotation, redemption, bootstrap, provisioning, and event deduplication boundaries.
    - Validate advertised persistence guarantees before authority traffic is accepted.
    - Fail startup with the named Tenant Adapter contract only when tenant-scoped capabilities require it.
    - Ensure pre-commit failures leave all affected records unchanged and issue no credentials.
  - [x] T1.5 [code] (satisfies: R2.2, R2.4, R3, R16.3, R16.7) — Implement record invariants and Tenant Adapter validation
    - Implement every record bound, required value, URI, uniqueness, known-reference, lifecycle, and association invariant from R3.
    - Resolve and validate Tenant Resource identity, Resource Kind, parentage, Tenant ownership, inheritance protection, cycles, and cross-Tenant ancestry.
    - Reject invalid tenant-scoped Grant, Role, Role Membership, Service, API Key, and OAuth Client associations without partial persistence.
    - Enforce optimistic concurrency on all mutable records named by R16.
    - Enforce UTC timestamps, stable identifiers, and documented ordinal/case-sensitive comparisons.
  - [x] T1.6 [code] (satisfies: R2, R3, R16.1, R16.2, R16.3, R16.7) — Write core contract, persistence, record, and validation tests
    - Cover all record bounds and associations, uniqueness, field-addressed failures, secret representation, concurrency conflicts, transaction rollback, persistence capability startup failures, Tenant Adapter optionality, ancestry cycles, empty Resource Kind, and cross-Tenant rejection.

- [x] T2. Cryptography, discovery, and OAuth Client validation
  - [x] T2.1 [model] (satisfies: R4, R7) — Add cryptography options and implementation template
    - Add cryptography fields to `SecurityAuthorityOptions` and add the `SecurityAuthorityCryptography` single-file C# File Builder template.
    - R4: model RSA signing, active/retained verification keys, `kid`/algorithm metadata, development-only ephemeral keys, production key requirements, external-provider secret protection, keyed API Key hashing, constant-time comparison, credential hashing, redaction, and key-rotation ordering.
    - R7: expose signing and hashing services required by token issuance and credential validation.
  - [x] T2.2 [model] (satisfies: R3, R5, R9) — Add discovery endpoints and OAuth Client validation model
    - Add `SecurityAuthorityDiscoveryEndpoints` as a single-file C# File Builder endpoint template.
    - Refine OAuth Client, Redirect URI, and Post-Logout Redirect URI records in `SecurityAuthorityRecords` for Public/Confidential clients, exact URI matching, allowed grants, allowed Scopes, optional preferred provider, active state, and separate logout redirects.
    - Add discovery/JWKS route defaults and collision validation to `Authority Routes` and capability metadata.
  - [x] T2.3 [code] (satisfies: R4.1, R4.2, R4.3, R4.4, R4.5, R4.6, R4.7, R4.8) — Implement cryptographic material and secret safety
    - Sign Access and ID Tokens with RSA and publish only active or still-required public verification keys with `kid`, `alg`, `kty`, `n`, and `e`.
    - Create a non-persisted development key with a restart warning only in Development; fail startup outside Development when required protection material is missing.
    - Encrypt provider secrets at rest and omit them from read/list projections.
    - Hash OAuth Client secrets, API Keys, Authorization Codes, Device Codes, and Refresh Tokens after their one-time clear return.
    - Redact credentials and private material from logs, errors, events, notifications, and audits.
    - Publish a new key before using it and retain previous public keys through the last signed-token expiry.
  - [x] T2.4 [code] (satisfies: R5.1, R5.2, R5.3, R5.4, R5.5, R5.6, R5.7) — Implement discovery, JWKS, and OAuth Client validation
    - Return complete anonymous OIDC metadata based on enabled features, including endpoint, grant, response, Scope, claim, signing-algorithm, and PKCE declarations.
    - Return all active or still-valid RSA verification keys from JWKS and keep discovery issuer identical to token `iss`.
    - Enforce exact ordinal, absolute, fragment-free registered redirect URIs.
    - Enforce Public-client S256 PKCE and no client secret.
    - Enforce Confidential-client Basic or `client_secret_post` authentication, reject dual methods, and return `invalid_client` without issuing credentials.
    - Reject inactive clients at authorization, device authorization, and token issuance.
  - [x] T2.5 [code] (satisfies: R4, R5) — Write cryptography, discovery, and client-validation tests
    - Cover RSA/JWKS fields and rotation order, development and production startup behavior, encryption/hashing/redaction, issuer parity, feature-sensitive discovery, exact redirects, Public/Confidential authentication rules, and inactive-client rejection.

- [x] T3. SSO sessions and external identity provider brokering
  - [x] T3.1 [model] (satisfies: R9) — Add SSO Session, UserInfo, and logout endpoint model
    - Add `SecurityAuthoritySessionEndpoints` as a single-file C# File Builder endpoint template.
    - Model opaque cookie settings, server-side SSO Session validation, UserInfo projection, configured contextual claims, logout behavior, and exact Post-Logout Redirect URI validation.
    - Add the default eight-hour SSO lifetime and configurable five-minute-to-thirty-day range to `Authority Protocol`.
  - [x] T3.2 [model] (satisfies: R10) — Add external provider brokering model
    - Add `SecurityAuthorityExternalProviders` as a single-file C# File Builder template.
    - Model Generic OIDC plus Entra External ID, Entra ID, Google, Auth0, and Keycloak presets; provider configuration fields; encrypted-secret usage; priority/eligibility; optional Tenant Resource; Invite Only/Open SSO; and GET query plus POST `form_post` callback support.
    - Ensure presets contain conventions only and no deployment identifiers, secrets, authority URLs, or redirect hosts.
  - [x] T3.3 [code] (satisfies: R9.1, R9.2, R9.3, R9.4, R9.5, R9.6, R9.7) — Implement SSO Session, UserInfo, and logout behavior
    - Issue an opaque cookie with no identity, token, Scope, or provider-secret data and apply HttpOnly, environment-sensitive Secure, SameSite=Lax, host-only, and configured expiry settings.
    - Validate server-side session expiry/revocation and Active User state.
    - Return the required UserInfo claims and configured context claims.
    - Revoke only the current SSO Session on logout, clear the cookie, and redirect only to an exact registered logout URI for an active client; otherwise use `/`.
    - Do not implicitly revoke Access Tokens during logout.
  - [x] T3.4 [code] (satisfies: R10.1, R10.2, R10.3, R10.4, R10.5, R10.6, R10.7) — Implement external provider selection and callback processing
    - Build Generic OIDC and all five provider presets without hard-coded deployment values.
    - Select only active eligible providers using preferred-provider then priority rules.
    - Implement Invite Only and Open SSO linking/creation behavior.
    - Enforce global issuer-subject uniqueness and prevent one callback from merging existing Users.
    - Validate discovery, token exchange, signatures, issuer, nonce, and required claims for GET/query and POST/form_post callbacks.
    - On callback failure, create no SSO Session or Authorization Code and use a safe OIDC error redirect only after prior client redirect validation.
    - Retain existing User/External Identity records when a provider becomes inactive.
  - [x] T3.5 [code] (satisfies: R9, R10) — Write session and external-provider tests
    - Cover cookie contents/settings, server-side invalidation, UserInfo claims, logout redirect safety/non-revocation, every provider preset, Invite/Open SSO, issuer-subject uniqueness, no-user-merge, callback modes, callback failure rollback, and inactive-provider behavior.

- [x] T4. Authorization Code, token, and Device Authorization protocols
  - [x] T4.1 [model] (satisfies: R6) — Add Authorization Code and callback endpoint model
    - Add `SecurityAuthorityAuthorizationEndpoints` as a single-file C# File Builder endpoint template.
    - Model `/connect/authorize`, GET and POST `/connect/callback`, provider selection, protected single-use correlation state, exact redirects, PKCE S256, Scope filtering, User/External Identity linking, SSO creation, and Authorization Code issuance.
  - [x] T4.2 [model] (satisfies: R7) — Add token endpoint and token metadata model
    - Add `SecurityAuthorityTokenEndpoint` as a single-file C# File Builder endpoint template.
    - Model form-urlencoded dispatch for enabled Authorization Code, Client Credentials, Refresh Token, and Device Code grants; OAuth error mapping; Access/ID Token metadata; rotating Refresh Tokens; replay lineage; and duration settings.
  - [x] T4.3 [model] (satisfies: R8) — Add Device Authorization endpoint model
    - Add `SecurityAuthorityDeviceEndpoints` as a single-file C# File Builder endpoint template.
    - Model device authorization, authenticated review and approve/deny operations, Device Grant states, canonical User Code normalization, polling interval, expiry, and one-time redemption.
    - Add default 15-minute expiry and five-second polling settings.
  - [x] T4.4 [code] (satisfies: R6.1, R6.2, R6.3, R6.4, R6.5, R6.7, R6.8) — Implement authorization request and callback flow
    - Validate required authorize parameters, `response_type=code`, exact client redirect, requested/allowed Scopes, and S256 requirements before any provider redirect.
    - Select the preferred active provider or highest-priority eligible provider.
    - Protect ten-minute, single-use state binding client, redirect, Scopes, PKCE values, nonce, and return state.
    - Process GET/query and POST/form_post callbacks, link/create identities per access mode, establish SSO, and issue a five-minute one-time Authorization Code.
    - Bind the code to client, redirect, User, granted Scopes, challenge, and nonce.
    - Treat configured client Scopes as pre-authorized and never issue an unknown/disallowed Scope.
  - [x] T4.5 [code] (satisfies: R6.6, R7.1, R7.2, R7.3, R7.4, R7.5, R7.6, R7.10) — Implement token dispatch, claims, and protocol errors
    - Accept only form-urlencoded requests and dispatch exactly the enabled grant types.
    - Redeem valid Authorization Codes atomically and reject unknown, expired, redeemed, client/redirect/PKCE-mismatched codes with `invalid_grant` and no credential.
    - Apply configurable Access, ID, and Refresh Token durations and grant-specific credential sets.
    - Populate Access Token issuer, audience, subject, Principal Type, case-sensitive Scopes, temporal claims, token id, and configured context; populate ID Token client audience, User subject, timing, and nonce.
    - Implement active Confidential-client Client Credentials issuance with allowed Scope validation.
    - Map all specified OAuth errors to the exact HTTP 400/401 status contract.
  - [x] T4.6 [code] (satisfies: R7.7, R7.8, R7.9) — Implement Refresh Token rotation and replay response
    - Revoke each redeemed Refresh Token, issue and link exactly one successor, and commit atomically.
    - Detect replay of a rotated token, revoke the active successor lineage for the User/client, and return `invalid_grant`.
    - Validate active clients, client authentication, allowed Scopes, expiry, ownership, and lifecycle before rotation.
  - [x] T4.7 [code] (satisfies: R8.1, R8.2, R8.3, R8.4, R8.5, R8.6, R8.7, R8.8) — Implement Device Authorization lifecycle
    - Issue a high-entropy Device Code and an unambiguous uppercase eight-character User Code rendered as four-plus-four, with case-insensitive/hyphen-insensitive lookup.
    - Return verification URIs, `expires_in=900`, and `interval=5`.
    - Require an authenticated Active User for review and approval/denial and expose pending client, Scopes, expiry, and status.
    - Return `authorization_pending`, `slow_down`, `access_denied`, `expired_token`, or `invalid_grant` for the specified states.
    - Atomically issue credentials on the first valid approved poll and mark the grant redeemed.
    - Reject inactive users/clients, unknown codes, mismatched clients, and disallowed Scopes without credentials.
  - [x] T4.8 [code] (satisfies: R6, R7.1, R7.2, R7.4, R7.5, R7.6, R7.9, R7.10) — Write Authorization Code and token endpoint tests
    - Cover authorize validation, provider redirect, state expiry/reuse, callback modes, exact redirects, Scope filtering, code binding/redeem failures, token claims/durations, Client Credentials, feature gating, and exact OAuth HTTP errors.
  - [x] T4.9 [code] (satisfies: R7.7, R7.8, R8) — Write Refresh Token and Device Authorization tests
    - Cover atomic rotation, successor lineage replay revocation, concurrent redemption, Device/User Code format and normalization, authenticated review, poll timing/statuses, expiry, approval/denial, one-time redemption, and inactive/mismatched principals.

- [x] T5. Tenant-aware Roles, Grants, and authorization
  - [x] T5.1 [model] (satisfies: R13) — Add authorization administration and engine model
    - Add `SecurityAuthorityAuthorizationEngine` as a single-file C# File Builder template.
    - Refine Tenant Resource, Role, Role Membership, Grant, and Grant Catalog records and management contracts for resource definition, parentage, Tenant boundaries, assignment targets, case-sensitive Permission Keys, Allow/Deny effect, applicability, expiry/revocation, reasons, and descriptions.
    - Model authorization-result invalidation hooks for Grant, membership, Role, User, Service, API Key, and Tenant Resource parent changes.
  - [x] T5.2 [code] (satisfies: R13.1, R13.2, R13.3, R13.4, R13.5, R13.6, R13.7, R13.8, R13.9) — Implement resource-aware authorization evaluation
    - Register Tenant Resources while treating the Tenant Adapter as authoritative for current parentage and Tenant ownership.
    - Validate Role definitions and User/Service memberships within Tenant boundaries.
    - Validate Grant targets and Tenant Resources and retain exact case-sensitive Permission Keys.
    - Walk from the requested resource toward ancestors until root or inheritance protection.
    - Apply enabled/unexpired/unrevoked membership contributions and Deny precedence over Allow.
    - Invalidate affected cached decisions before the next authorization after all named state changes.
    - Reject cycles, cross-Tenant ancestry, invalid memberships, and unknown principals/resources.
    - Expose a non-authorizing Grant Catalog.
  - [x] T5.3 [code] (satisfies: R2.2, R2.4, R13) — Write Tenant Resource, Role, Grant, and authorization tests
    - Cover hierarchy traversal, inheritance boundaries, Tenant Adapter authority, cycles/cross-Tenant rejection, Role Membership eligibility, expiry/revocation, Deny precedence, Permission Key case rules, invalidation timing, unknown references, and Grant Catalog behavior.

- [x] T6. Bootstrap, lifecycle, Services, API Keys, and management APIs
  - [x] T6.1 [model] (satisfies: R11) — Add bootstrap and User lifecycle model
    - Add `SecurityAuthorityBootstrap` and `SecurityAuthorityLifecycle` single-file C# File Builder templates.
    - Model Explicit Identity, First Eligible User, and Custom Seed Function settings/state; initial administrator Grants; atomic compare-and-set; permanent closure and protected reset.
    - Model allowed User transitions, Active-state authorization checks, and cascading revocation of SSO Sessions, Refresh Tokens, API Keys, Role Memberships, and direct Grants while retaining immutable history.
  - [x] T6.2 [model] (satisfies: R12) — Add Service and API Key lifecycle model
    - Refine Service and API Key records, lifecycle hooks, cryptography usage, management operations, and Grant translation.
    - Model idempotent Tenant/name provisioning, create/read/list/update/activate/deactivate/delete, Role/Grant assignment, API Key issue/regeneration, one-time clear return, expiry/revocation, last-used time, and owner lifecycle integration.
  - [x] T6.3 [model] (satisfies: R14) — Add the versioned management API contract
    - Add `SecurityAuthorityManagementEndpoints` as a single-file C# File Builder endpoint template under fixed `/api/v1/security` routes.
    - Model JSON endpoints for Users, Services, API Keys, OAuth Clients, Identity Providers, Tenant Resources, Roles, Role Memberships, Grants, Grant Catalog, security summary, and bootstrap maintenance.
    - Model action-specific management Scopes, deterministic paging, status codes, RFC 9457 field errors, secret-safe projections, optional 24-hour idempotency keys, concurrency tokens, and parity operations for lifecycle, revocation, regeneration, provisioning, memberships, Grants, and summary.
  - [x] T6.4 [code] (satisfies: R11.1, R11.2, R11.3, R11.4, R11.5) — Implement administrator bootstrap strategies
    - Validate exactly one configured bootstrap strategy.
    - Match Explicit Identity by the configured issuer-subject pair or normalized email.
    - Use atomic compare-and-set for First Eligible User and return conflicts to concurrent later attempts.
    - Invoke and validate the Custom Seed Function, initial User, and initial Grants; fail startup when absent or invalid.
    - Permanently close bootstrap after commit and allow reset only through protected maintenance.
  - [x] T6.5 [code] (satisfies: R11.6, R11.7, R11.8) — Implement User and Service lifecycle cascades
    - Enforce only the allowed User transitions and terminal Archived/Deleted states.
    - Deny every listed credential/session operation for non-Active Users.
    - Revoke User SSO Sessions, Refresh Tokens, API Keys, memberships, and direct Grants on archive/delete without deleting audit history.
    - Revoke Service API Keys and Refresh Tokens on deactivate/delete and make live-principal token validation fail.
  - [x] T6.6 [code] (satisfies: R12.1, R12.2, R12.3) — Implement Service administration and idempotent provisioning
    - Implement Service CRUD, activate/deactivate, Role assignment/removal, and Grant assignment/removal.
    - Enforce unique Service name within Tenant.
    - Return the existing Service identifier for repeated idempotent provisioning with the same Tenant/name.
    - Apply Service-state credential revocation and live-principal behavior.
  - [x] T6.7 [code] (satisfies: R12.4, R12.5, R12.6, R12.7, R12.8, R12.9) — Implement API Key issue, regeneration, and authentication
    - Require exactly one active User or Service owner and translate direct Scope input into API Key Grant records.
    - Generate the configured prefix plus at least 32 random bytes without whitespace and return clear material exactly once.
    - Store a keyed one-way hash and compare in constant time.
    - Atomically revoke and replace the prior value during regeneration.
    - Reject expired, revoked, unknown, owner-inactive, and mismatched keys without touching last-used time.
    - On success, update last-used and produce effective owner Scopes plus direct API Key Grants minus Denies.
  - [x] T6.8 [code] (satisfies: R14.1, R14.2, R14.4, R14.5, R14.8, R14.9) — Implement management resources, authorization, and response contracts
    - Implement all versioned resource endpoints and reference-parity actions under `/api/v1/security`.
    - Require authentication and the documented action-specific management Scope.
    - Return exact 200/201/204 success and 400/401/403/404/409 failure contracts with field-addressed RFC 9457 details.
    - Keep every read/list/action projection free of hashes, encrypted values, clear credentials, private keys, and correlation state.
    - Integrate lifecycle, provisioning, revocation, regeneration, membership, Grant, catalog, summary, and bootstrap operations.
  - [x] T6.9 [code] (satisfies: R14.3, R14.6, R14.7) — Implement management paging, idempotency, and concurrency
    - Validate `pageNumber >= 1` and `pageSize` 1-100 with defaults 1/25 and return total count, page metadata, and deterministic ordering.
    - Store 1-200 character idempotency keys and request fingerprints for 24 hours; replay identical requests with the original outcome and reject different requests with HTTP 409.
    - Require concurrency tokens for update/state-transition operations and reject stale tokens with HTTP 409 without overwriting state.
  - [x] T6.10 [code] (satisfies: R11, R12) — Write bootstrap, lifecycle, Service, and API Key tests
    - Cover each bootstrap strategy, compare-and-set races, closure/reset, User transitions and cascades, non-Active denial, Service CRUD/provisioning uniqueness/idempotency, API Key entropy/one-time return/hash/regeneration/authentication, Grant translation, Deny behavior, and owner-state failures.
  - [x] T6.11 [code] (satisfies: R14) — Write management API contract tests
    - Cover every resource/action, action-specific Scopes, paging bounds/defaults/order, status codes, RFC 9457 errors, idempotency replays/conflicts, stale concurrency, secret-safe responses, reference-parity actions, and fixed route versioning.

- [x] T7. Integration events, post-commit delivery, cleanup, and conformance
  - [x] T7.1 [model] (satisfies: R15) — Add Integration Event and post-commit adapter model
    - Add `SecurityAuthorityIntegrationEvents` and `SecurityAuthorityPostCommitDispatch` single-file C# File Builder templates.
    - Model versioned event envelopes, processed-event records, handlers for Users, Services, OAuth Clients, Identity Providers, Tenant Resources, Roles, Role Memberships, and Grants, exact rejection outcomes, and at-most-once processing.
    - Model optional Lifecycle Notification and Audit Adapter dispatch after commit with non-secret payloads and failure-handler reporting that never rolls back or falsely repeats a committed mutation.
  - [x] T7.2 [model] (satisfies: R16) — Add cleanup and conformance templates
    - Add `SecurityAuthorityCleanup` and `SecurityAuthorityConformanceTests` single-file C# File Builder templates.
    - Model revoked-metadata retention default 30 days/range 1-3650, code/device cleanup 1-7 days but never before 24 hours, and SSO/Refresh cleanup 30-90 days.
    - Model race-safe cleanup over committed expiry, revocation, redemption, and concurrency state.
    - Register dedicated-host and existing-host conformance fixtures and enabled/disabled feature combinations.
  - [x] T7.3 [code] (satisfies: R15.1, R15.2, R15.3, R15.4) — Implement Integration Event validation and at-most-once mutation
    - Accept versioned registration/lifecycle events for every named record type.
    - Validate global event id, type, schema version, occurrence time, correlation id, source, and resource payload.
    - Deduplicate atomically so replay returns/records success without duplicate records, memberships, Grants, or transitions.
    - Reject unsupported versions, missing fields, unknown references, stale tokens, and Tenant mismatches with event id and exact reason while leaving state unchanged.
  - [x] T7.4 [code] (satisfies: R15.5, R15.6, R15.7, R15.8) — Implement post-commit notifications, auditing, and delivery failure handling
    - Emit one post-commit Lifecycle Notification for each listed lifecycle/credential/bootstrap transition when configured.
    - Submit actor, action, target, optional Tenant, correlation, timestamp, outcome, and non-secret changed-field names to the Audit Adapter for every required mutation.
    - Keep the authority operational when optional adapters are absent without claiming delivery.
    - Report post-commit adapter failures through the host failure-handler contract without transaction rollback or false mutation replay.
  - [x] T7.5 [code] (satisfies: R16.4, R16.5, R16.6) — Implement retention and race-safe cleanup
    - Retain revoked metadata and terminal timestamps for the configured period without retaining clear redeemable secrets.
    - Remove Authorization Codes and Device Grants only within the configured 24-hour-to-seven-day post-expiry window.
    - Remove expired/revoked SSO Sessions and Refresh Tokens only within the configured 30-to-90-day window.
    - Use committed expiry, revocation, redemption, and optimistic-concurrency state so exactly one cleanup or validation/redemption operation succeeds.
  - [x] T7.6 [code] (satisfies: R15, R16.1, R16.2, R16.3, R16.4, R16.5, R16.6, R16.7) — Write integration, delivery, and cleanup tests
    - Cover event versions/envelopes, exact rejection reasons, atomic deduplication/replay, all lifecycle notifications, audit payloads and redaction, absent adapters, post-commit failure reporting, retention windows, cleanup races, atomic operations, UTC/stable-id/case rules, and optimistic concurrency.
  - [x] T7.7 [code] (satisfies: R4, R5, R6, R7, R8, R9, R10, R12, R13, R14, R15, R16.8) — Complete protocol and safety conformance test implementations
    - Implement generated conformance coverage for every acceptance criterion in R4-R10 across dedicated-host and existing-host fixtures.
    - Implement generated conformance coverage for atomicity, idempotency, concurrency, cleanup races, and secret exclusion in R12-R16.
    - Cover enabled/disabled feature combinations and confirm additive installation and repeat generation remain idempotent.

## Task Dependency Graph

```json
{
  "waves": [
    {
      "id": 1,
      "tasks": ["T0.1", "T0.2", "T0.3", "T0.4", "T0.5", "T0.6", "T0.7", "T0.8"],
      "label": "Module prerequisites and additive foundation"
    },
    {
      "id": 2,
      "tasks": ["T1.1", "T1.2", "T1.3", "T1.4", "T1.5", "T1.6"],
      "label": "Core contracts, persistence, records, and validation"
    },
    {
      "id": 3,
      "tasks": ["T2.1", "T2.2", "T2.3", "T2.4", "T2.5"],
      "label": "Cryptography, discovery, and OAuth Clients"
    },
    {
      "id": 4,
      "tasks": ["T3.1", "T3.2", "T3.3", "T3.4", "T3.5"],
      "label": "SSO sessions and external providers"
    },
    {
      "id": 5,
      "tasks": ["T4.1", "T4.2", "T4.3", "T4.4", "T4.5", "T4.6", "T4.7", "T4.8", "T4.9"],
      "label": "Authorization Code, token, and device protocols"
    },
    {
      "id": 6,
      "tasks": ["T5.1", "T5.2", "T5.3"],
      "label": "Tenant-aware authorization administration"
    },
    {
      "id": 7,
      "tasks": ["T6.1", "T6.2", "T6.3", "T6.4", "T6.5", "T6.6", "T6.7", "T6.8", "T6.9", "T6.10", "T6.11"],
      "label": "Bootstrap, Services, API Keys, and management"
    },
    {
      "id": 8,
      "tasks": ["T7.1", "T7.2", "T7.3", "T7.4", "T7.5", "T7.6", "T7.7"],
      "label": "Integration, cleanup, and conformance"
    }
  ]
}
```