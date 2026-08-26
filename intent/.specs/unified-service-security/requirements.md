# Requirements Document

## Introduction

`Aryzac.Security` shall provide a reusable, configurable security contract for generated backend services. It serves module consumers that need consistent inbound credential handling, Principal access, Scope enforcement, outbound credential selection, safe diagnostics, and reusable conformance tests without rebuilding those capabilities per service.

This specification covers only capabilities generated or supplied by the universally installed `Aryzac.Security` module in this repository. Security-authority endpoints, authoritative credential stores, product-specific service models and handlers, application adapters, deployment topology, rollout sequencing, and solution-specific conformance orchestration belong to separate specifications in their owning projects. Authority-only OIDC handlers may be delivered later through a separate companion module.

## Target Users & Jobs To Be Done

- **Module Consumer** — configures generated backend services to admit supported Caller Credentials, expose one canonical Principal, enforce declared Scopes, and produce stable rejection responses.
- **Service Developer** — makes authenticated outbound calls while preserving an ambient caller identity when present and acquiring a Service Credential when it is absent.
- **Module Maintainer** — evolves one additive security path and proves its generated behavior without breaking existing Scope extensions or requiring every consuming application to adopt every capability simultaneously.

## Key User Journeys

- **UJ-1. A Module Consumer enables consistent inbound security.**
  - **Persona + context:** A Module Consumer installs or updates `Aryzac.Security` in a generated backend service.
  - **Entry state:** The service already uses the module's existing Scope extensions and may not yet enable the new authentication capabilities.
  - **Path:** The consumer enables selected capabilities, supplies their typed options, starts the service, and sends requests using JWT, API Key, or Service Token credentials.
  - **Climax:** Each admitted request exposes exactly one canonical Principal and each rejected request returns the stable Problem Details contract without executing the operation.
  - **Resolution:** Existing operation security metadata remains intact and disabled capabilities require no configuration.
  - **Edge case:** Startup fails with the exact missing or invalid option when an enabled capability is not safely configured.

- **UJ-2. A Service Developer calls another service securely.**
  - **Persona + context:** A Service Developer uses a generated client during an inbound request or background operation.
  - **Entry state:** An ambient Caller Credential may or may not exist.
  - **Path:** The outbound selector checks the ambient credential, forwards it unchanged when present, otherwise acquires a Service Credential, caches it safely, and attaches it before sending.
  - **Climax:** The downstream request carries the selected credential and concurrent callers share one acquisition attempt.
  - **Resolution:** No downstream request is sent when credential selection or acquisition fails.
  - **Edge case:** Timeout, non-success, or malformed dependency responses produce a typed unavailable failure without exposing the dependency body or a secret.

- **UJ-3. A Module Maintainer proves reusable conformance.**
  - **Persona + context:** A Module Maintainer changes templates, generated components, or shared runtime behavior.
  - **Entry state:** The module has existing Scope designer extensions and generated artifacts that current 1.x consumers rely on.
  - **Path:** The maintainer runs module self-tests, reusable conformance fixtures, non-disclosure checks, concurrency checks, and the warmed overhead benchmark.
  - **Climax:** The test kit reports zero failures only when the generated security contract satisfies every enabled capability.
  - **Resolution:** Consuming solutions can invoke the same fixtures against their discovered services.
  - **Edge case:** A missing marker, duplicate registration, unsupported credential path, leaked sentinel value, or performance regression fails with the violated criterion identified.

## Glossary

- **Caller Credential** — the single credential presented to a backend service for one request; it is exactly one of JWT, API Key, or Service Token after classification.
- **JWT** — a Bearer credential validated with configured public signing keys and optional issuer and audience rules.
- **API Key** — an `ApiKey` credential classified by a required configurable format prefix and resolved through a typed Credential Resolver.
- **Service Token** — a Bearer credential representing a service Principal and carrying the configured Reserved Service Scope.
- **Internal Service Key** — an optional shared-key compatibility credential admitted or presented only when the corresponding capability is explicitly enabled.
- **Principal** — the immutable, canonical identity and context resolved from one admitted Caller Credential; exactly one Principal exists per admitted request.
- **Scope** — a case-sensitive permission value declared by an operation or carried by a Principal.
- **Reserved Service Scope** — the configured Scope that bypasses only per-operation Scope comparison for an authenticated service Principal.
- **Ambient Caller Credential** — the exact inbound or explicitly established Caller Credential available to generated outbound clients within the current asynchronous execution context.
- **Service Credential** — a credential acquired through a configured client-credentials endpoint when no Ambient Caller Credential exists.
- **Credential Resolver** — the typed extension point that returns active, invalid, expired, revoked, or unavailable resolution outcomes without exposing the submitted credential.
- **Security Rejection** — the stable typed failure and RFC 9457 Problem Details response produced before a rejected operation executes.
- **Reusable Conformance Test Kit** — module-owned fixtures and assertions that consuming solutions can run against generated services.

## Non-Goals

- Implementing an authoritative API-key store, token issuer, token endpoint, introspection endpoint handler, or other Security-authority OIDC API in `Aryzac.Security`.
- Modelling or implementing product-specific commands, DTOs, handlers, service adapters, Principal policies, or credential records.
- Installing or configuring security capabilities across a consuming solution's applications, deployment topology, secrets platform, or rollout pipeline.
- Hard-coding product-specific credential prefixes, issuer names, endpoint addresses, Reserved Service Scopes, client identities, or Problem Details base URIs.
- Replacing existing operation `Secured`, `Unsecured`, Scope settings, or Scope assignments.
- Introducing a sibling inbound security path that bypasses the module's single registration entry point.

## MVP Scope

### In Scope

- Additive `Aryzac.Security` 1.x capabilities for inbound credential classification, Principal resolution, JWT validation, typed API-key resolution, authorization, stable rejections, outbound selection, safe diagnostics, and reusable conformance tests.
- Typed runtime options with startup validation for each enabled capability.
- Optional Internal Service Key admission and presentation primitives controlled independently by the consuming application.
- Preservation of the existing Scope designer extensions and generated artifacts.

### Out of Scope for MVP

- A companion authority module containing OIDC token or introspection handlers.
- Consuming-project models, implementations, configuration values, deployment sequencing, and solution-wide discovery commands.

## Requirements

### Requirement 1: Shared Security Contract Foundation

**User Story:** As a Module Consumer, I want one generated security registration path and one Principal contract, so that every enabled backend service handles Caller Credentials consistently.

**Realizes:** UJ-1, UJ-3

#### Acceptance Criteria

1. THE `Aryzac.Security` module SHALL preserve its existing module identity, Scope designer extensions, and generated Scope artifacts for compatible 1.x consumers.
2. THE module SHALL generate exactly one service marker and exactly one public registration entry point for the shared security contract.
3. THE registration entry point SHALL permit JWT, API Key, Service Token, Internal Service Key admission, outbound credential selection, and credential-safe diagnostics to be enabled independently.
4. WHEN a capability is disabled, THE registration entry point SHALL NOT require that capability's options and SHALL NOT register its credential path.
5. WHEN an enabled capability has a missing or invalid required option, THE service SHALL fail startup with an error that names the exact option and SHALL NOT include any configured secret value.
6. THE inbound contract SHALL accept exactly one `Authorization` field value and SHALL recognize only the case-insensitive schemes `Bearer` and `ApiKey`.
7. IF the `Authorization` field is absent or empty, THEN THE inbound contract SHALL return `missing_credential` and SHALL NOT execute the operation.
8. IF the `Authorization` field contains multiple values or a scheme without one non-empty credential value, THEN THE inbound contract SHALL return `malformed_credential` and SHALL NOT execute the operation.
9. IF the scheme is neither `Bearer` nor `ApiKey`, THEN THE inbound contract SHALL return `unsupported_credential_scheme` and SHALL NOT execute the operation.
10. THE API Key capability SHALL require a non-empty configurable format prefix and SHALL classify an `ApiKey` value as an API Key only when it starts with that prefix using ordinal comparison.
11. WHEN a Caller Credential is admitted, THE inbound contract SHALL create exactly one immutable Principal and SHALL make that same Principal instance available throughout the request.
12. THE Principal SHALL expose the resolved principal identifier, principal type, zero or one account identifier, zero or one workspace identifier, and a case-sensitive immutable set of Scopes.
13. THE module SHALL provide an asynchronous Ambient Caller Credential accessor whose value flows through child asynchronous operations and is restored when a nested scope ends.
14. THE module SHALL expose protected extension points for typed credential resolution and policy variation without permitting a second registration path.

### Requirement 2: Configurable JWT Validation

**User Story:** As a Module Consumer, I want reusable public-key JWT validation, so that services validate caller and service tokens without private signing material.

**Realizes:** UJ-1, UJ-3

#### Acceptance Criteria

1. THE JWT capability SHALL require at least one RSA public signing key and SHALL accept one optional secondary RSA public signing key for rotation overlap.
2. THE JWT capability SHALL NOT accept, request, or expose private signing key material.
3. THE JWT capability SHALL allow issuer validation and audience validation to be enabled independently, each with one or more configured allowed values.
4. WHEN issuer validation is disabled, THE validator SHALL NOT reject a token because of its issuer claim.
5. WHEN audience validation is disabled, THE validator SHALL NOT reject a token because of its audience claim.
6. THE JWT capability SHALL use a configurable clock skew whose default is exactly 60 seconds and whose accepted range is 0 through 300 seconds inclusive.
7. WHEN one public key is configured, THE validator SHALL validate signatures only against that key.
8. WHEN primary and secondary public keys are configured, THE validator SHALL admit a token signed by either key when all other enabled checks pass.
9. IF a Bearer value is malformed, has an invalid signature, fails an enabled issuer rule, fails an enabled audience rule, or lacks required Principal claims, THEN THE validator SHALL return `invalid_credential` without cryptographic detail.
10. IF a token is expired after applying the configured clock skew, THEN THE validator SHALL return `expired_credential`.
11. WHEN a validated Bearer token identifies a service Principal and contains the configured Reserved Service Scope, THE contract SHALL classify it as a Service Token; otherwise it SHALL classify it as a JWT.
12. THE module self-tests SHALL cover primary-key operation, dual-key overlap, issuer enabled and disabled, audience enabled and disabled, clock-skew boundaries, invalid signatures, malformed tokens, expired tokens, and missing startup options.

### Requirement 3: Typed API-Key Resolution Capability

**User Story:** As a Module Consumer, I want a typed API-key resolver contract and reusable remote client, so that authority-specific storage remains outside the universal module.

**Realizes:** UJ-1, UJ-2, UJ-3

#### Acceptance Criteria

1. THE module SHALL define Credential Resolver outcomes `active`, `invalid_credential`, `expired_credential`, `revoked_credential`, and `credential_resolution_unavailable`.
2. AN `active` outcome SHALL contain one Principal and MAY contain credential expiry; every non-active outcome SHALL NOT contain a Principal.
3. THE module SHALL provide a local Credential Resolver extension point without supplying an authoritative credential store or authority endpoint handler.
4. THE module SHALL provide a remote Credential Resolver client that sends the complete API Key only to the configured resolution endpoint using the configured authentication strategy.
5. THE remote client SHALL use a configurable completion timeout whose default is exactly 5 seconds and whose accepted range is 1 through 60 seconds inclusive.
6. IF the remote call times out, cannot connect, returns a non-success status, or returns a malformed body, THEN THE client SHALL return `credential_resolution_unavailable` and SHALL NOT include the dependency response body.
7. THE remote client SHALL cache only `active` outcomes.
8. THE active-result cache key SHALL be an HMAC-SHA256 digest of the complete credential and SHALL NOT contain the credential or its suffix.
9. THE active-result cache duration SHALL be the lesser of the configured maximum, whose default is 60 seconds, and the remaining credential lifetime.
10. THE remote client SHALL NOT cache invalid, expired, revoked, unavailable, timeout, or malformed-response outcomes.
11. WHEN a cached active result expires, THE next resolution SHALL call the configured resolver again.
12. THE module self-tests SHALL cover every typed outcome, the timeout, malformed and non-success responses, successful cache reuse, credential-expiry bounding, failure non-caching, and credential non-disclosure.

### Requirement 4: Outbound Credential Selection

**User Story:** As a Service Developer, I want generated clients to forward caller credentials or acquire service credentials, so that downstream calls preserve caller context whenever possible.

**Realizes:** UJ-2, UJ-3

#### Acceptance Criteria

1. WHEN an Ambient Caller Credential exists, THE outbound selector SHALL attach the same scheme and credential bytes to the downstream request and SHALL NOT acquire a Service Credential.
2. WHEN no Ambient Caller Credential exists, THE outbound selector SHALL request a Service Credential using grant type `client_credentials`, the configured client identity and client secret, and the configured Reserved Service Scope.
3. THE Service Credential acquisition timeout SHALL be configurable with a default of exactly 10 seconds and an accepted range of 1 through 120 seconds inclusive.
4. IF acquisition times out, cannot connect, returns a non-success status, or returns a malformed response, THEN THE selector SHALL return a typed acquisition failure and SHALL NOT send the downstream request.
5. THE selector SHALL cache a successful Service Credential until its expiry minus a configurable safety window whose default is exactly 60 seconds.
6. IF the acquired Service Credential's remaining lifetime is less than or equal to the safety window, THEN THE selector SHALL NOT cache it.
7. WHEN concurrent callers require a Service Credential for the same configured client and endpoint, THE selector SHALL execute exactly one acquisition attempt and SHALL share its success or failure with those callers.
8. AFTER a shared acquisition failure completes, THE next caller SHALL be permitted to start a new acquisition attempt.
9. THE module SHALL NOT log, trace, meter, return, or place in an exception the Ambient Caller Credential, Service Credential, client secret, or dependency response body.
10. THE module self-tests SHALL cover byte-for-byte forwarding, absence fallback, exact grant and Scope values, expiry safety, concurrent success, concurrent failure, timeout, non-success, malformed responses, retry after failure, and prevention of unauthenticated sends.

### Requirement 5: Universal Service-Token Admission

**User Story:** As a Module Consumer, I want authenticated service Principals handled consistently, so that service calls do not require callee-specific allow-lists in the shared module.

**Realizes:** UJ-1, UJ-3

#### Acceptance Criteria

1. WHEN a valid Bearer token resolves to a service Principal, THE contract SHALL admit it without a module-level service allow-list.
2. WHEN an admitted service Principal contains the configured Reserved Service Scope, THE authorization contract SHALL bypass only the per-operation Scope comparison.
3. THE Reserved Service Scope SHALL NOT bypass signature, expiry, enabled issuer, enabled audience, Principal claim, or credential classification checks.
4. IF a service token is invalid, THEN THE contract SHALL return `invalid_credential`; IF it is expired after clock skew, THEN THE contract SHALL return `expired_credential`.
5. THE Security Rejection record SHALL identify the service Principal when it was resolved and SHALL NOT contain the Service Token.
6. THE module self-tests SHALL cover arbitrary service Principals, configured Reserved Service Scope bypass, absence of that Scope, all other checks remaining active, and invalid and expired tokens receiving no bypass.

### Requirement 6: Optional Internal Service Key Primitive

**User Story:** As a Module Consumer, I want an explicitly enabled shared-key compatibility primitive, so that legacy service calls can migrate without making the behavior universal.

**Realizes:** UJ-1, UJ-2, UJ-3

#### Acceptance Criteria

1. THE module SHALL expose Internal Service Key admission and Internal Service Key presentation as two independently enabled capabilities.
2. WHEN admission is enabled, THE capability SHALL require a non-empty configured Internal Service Key and configured Principal values.
3. THE admission capability SHALL compare the complete presented value to the configured value using a fixed-time comparison.
4. WHEN the values match, THE capability SHALL resolve the configured service Principal with the configured Reserved Service Scope.
5. IF admission is enabled and the configured key is missing, THEN startup SHALL fail naming the missing option.
6. IF the presented value does not match, THEN THE capability SHALL return `invalid_credential` and SHALL NOT invoke the API Key Credential Resolver as a fallback.
7. WHEN presentation is enabled and no Ambient Caller Credential exists, THE outbound selector SHALL send `Authorization: ApiKey <configured-value>` and SHALL NOT acquire a Service Credential.
8. WHEN presentation is enabled and an Ambient Caller Credential exists, THE outbound selector SHALL forward the Ambient Caller Credential unchanged.
9. WHEN either capability is disabled, THE module SHALL NOT admit or present an Internal Service Key through that capability.
10. THE module SHALL NOT expose the Internal Service Key in diagnostics, Problem Details, metric labels, traces, or caller-visible exceptions.
11. THE module self-tests SHALL cover independent enablement, fixed-time match, missing configuration, mismatch, no resolver fallback, ambient forwarding precedence, presentation fallback, and secret non-disclosure.

### Requirement 8: Scope Enforcement and Stable Rejections

**User Story:** As a Module Consumer, I want consistent authorization and rejection responses, so that clients and operations observe one contract across generated services.

**Realizes:** UJ-1, UJ-3

#### Acceptance Criteria

1. THE module SHALL preserve every existing operation `Secured`, `Unsecured`, Scope setting, and Scope assignment.
2. THE authorization contract SHALL treat absent Principal Scopes as an empty set.
3. WHEN a secured operation declares no required Scope, THE authorization contract SHALL permit any authenticated Principal that passes all other checks.
4. WHEN a secured operation declares required Scopes, THE authorization contract SHALL require every declared Scope unless Requirement 5.2 applies.
5. IF the Principal lacks any required Scope, THEN THE contract SHALL return `insufficient_scope` with HTTP 403 and SHALL NOT execute the operation.
6. EVERY HTTP Security Rejection SHALL use `application/problem+json` and SHALL contain `type`, `title`, `status`, and `code`.
7. THE `type` SHALL equal the configured Problem Details base URI followed by `/` and the exact `code`.
8. THE module SHALL use exactly these stable codes: `missing_credential`, `unsupported_credential_scheme`, `malformed_credential`, `invalid_credential`, `expired_credential`, `revoked_credential`, `insufficient_scope`, and `credential_resolution_unavailable`.
9. `missing_credential`, `unsupported_credential_scheme`, `malformed_credential`, `invalid_credential`, `expired_credential`, and `revoked_credential` SHALL use HTTP 401.
10. `insufficient_scope` SHALL use HTTP 403 and `credential_resolution_unavailable` SHALL use HTTP 503.
11. HTTP 401 responses for Bearer-capable secured operations SHALL include `WWW-Authenticate: Bearer` and SHALL NOT include credential or cryptographic detail.
12. THE module SHALL allow Problem Details titles and the base URI to be configured but SHALL NOT allow the stable codes or their status meanings to be changed.
13. THE module self-tests SHALL cover the exact code set, response media type and shape, status mapping, challenge header, empty Scopes, operations with no Scope, declared Scope enforcement, Reserved Service Scope behavior, and non-execution after rejection.

### Requirement 9: Reusable Conformance Test Kit

**User Story:** As a Module Maintainer, I want reusable conformance fixtures, so that module and consuming-solution tests prove the same generated security contract.

**Realizes:** UJ-3

#### Acceptance Criteria

1. THE module SHALL ship self-tests and a Reusable Conformance Test Kit that consuming test projects can invoke without copying fixture implementations.
2. THE test kit SHALL verify that each supplied service has exactly one supported marker and exactly one shared security registration.
3. THE test kit SHALL expose fixtures for credential classification, JWT validation, API-key outcomes, Internal Service Key outcomes, Principal availability, Scope enforcement, Problem Details, outbound forwarding, Service Credential acquisition, caching, single-flight behavior, and timeout behavior.
4. THE test kit SHALL expose positive and negative assertions for every stable code applicable to the enabled capabilities.
5. THE test kit SHALL accept discovered service instances or factories from the consuming solution and SHALL NOT require a product-specific fixed service count.
6. WHEN a conformance assertion fails, THE result SHALL identify the service or generated component, the security concern, and the violated requirement criterion.
7. THE module SHALL provide a warmed benchmark fixture that compares the shared inbound contract with a baseline handler under the same process and workload.
8. THE warmed median overhead of the shared inbound contract SHALL be no more than 5 milliseconds above the baseline median.
9. THE benchmark SHALL perform at least 100 warm-up executions and at least 1,000 measured executions per path before calculating the median.
10. THE test kit SHALL report success only when every invoked conformance and performance assertion passes.

### Requirement 11: Credential-Safe Diagnostics

**User Story:** As a Module Consumer, I want correlatable security diagnostics without credential disclosure, so that failures can be investigated safely.

**Realizes:** UJ-1, UJ-2, UJ-3

#### Acceptance Criteria

1. WHEN credential-safe diagnostics are enabled, THE module SHALL require a dedicated non-empty HMAC key and SHALL fail startup naming the missing option when it is absent.
2. THE module SHALL compute credential correlation as HMAC-SHA256 over the complete credential using the dedicated diagnostics key.
3. THE module SHALL render the correlation value as lowercase hexadecimal containing exactly 64 characters.
4. THE module SHALL NOT derive the diagnostics key from a Caller Credential, Service Credential, Internal Service Key, client secret, or signing key.
5. THE module MAY record only the configured API Key format prefix beside the correlation value and SHALL NOT record any characters following that prefix.
6. A Security Rejection record SHALL contain request correlation, the stable Problem Details code, credential kind when classifiable, Principal identifier and type when resolved, and the credential correlation when a credential was present.
7. THE module SHALL NOT place complete or partial credential values, signing details, configured secrets, client secrets, or dependency response bodies in logs, traces, metric labels, Problem Details, rejection records, or caller-visible exceptions.
8. THE Reusable Conformance Test Kit SHALL inject distinct sentinel values as each credential and secret type and SHALL fail if a sentinel or dependency body appears in any captured response, log, trace, metric label, rejection record, or exception.
9. THE non-disclosure fixtures SHALL include positive assertions that permitted request, code, kind, Principal, and correlation fields remain available.

## Assumptions

- Scheme names `Bearer` and `ApiKey` remain stable interoperability terms even though product-specific prefixes, endpoints, issuers, audiences, scopes, identities, and Problem Details base URIs are configurable.
- Service Token acquisition uses an OAuth-compatible client-credentials response containing an access token and expiry.
- The separate companion authority module is not created by this specification.
