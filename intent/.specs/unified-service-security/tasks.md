# Implementation Plan — Unified Service Security

## Tasks

- [ ] T1. Shared security foundation
  - [x] T1.1 [model] (satisfies: R1) — Preserve `Aryzac.Security` and add `SecurityContractFoundation`
    - Preserve Module Builder package `Aryzac.Security` (`b03410a6-8af0-4dad-a18c-1f6d5490612c`), its 1.x identity, all existing Scope extensions/settings/stereotypes, and `ScopePermissionMap` (`5413af14-47de-4778-8883-bd789689bc6d`).
    - Remove generated dependency `Intent.Security.JWT` 4.3.10; no replacement module is required.
    - Add `SecurityContractFoundation` as `C# Template`, type `Single File`, `C# Template Settings(Templating Method=C# File Builder)`, `Template Settings(Source=Lookup Type, Role=Api, Default Location=Security)`.
    - Generate the assembly marker, contract version, immutable credential/Principal/Scope/rejection contracts, typed outcomes, option roots, ambient accessor, and protected extension interfaces.
  - [x] T1.2 [model] (satisfies: R1) — Add the sole `SecurityRegistration` template
    - Add `SecurityRegistration` with the common API template settings used by `SecurityContractFoundation`.
    - Generate exactly one public entry point, `SecurityContractRegistration.AddAryzacSecurity`, with a configuration overload and independent JWT, API Key, Service Token, Internal Service Key admission, outbound, and diagnostics switches.
  - [x] T1.3 [model] (satisfies: R1) — Add `SecurityInboundCredentials`
    - Add `SecurityInboundCredentials` with the common API template settings.
    - Generate the exact `Authorization` parser/classifier surface, request Principal publication, ambient caller credential scope, and typed resolver hand-off.
  - [x] T1.4 [model] (satisfies: R9) — Add the module self-test project foundation
    - In Codebase Structure under solution `Aryzac`, add `Aryzac.Security.Tests` adjacent to existing project `Aryzac.Security` (`d6257d5c-437c-4b7e-8955-81528181567b`).
    - Use `C# Project (.NET)`, `Microsoft.NET.Sdk`, .NET 8, console output, nullable enabled, repository-default language version, and a `1 → 1` project reference to `Aryzac.Security`.
  - [ ] T1.5 [code] (satisfies: R1) — Implement foundation contracts in `Templates/SecurityContractFoundation/*TemplatePartial.cs`
    - Generate exactly one supported service marker and contract version.
    - Generate immutable `CallerCredential`, `Principal`, `Scope`, `SecurityRejection`, and typed resolution/acquisition outcomes.
    - Include Principal identifier/type, optional account/workspace identifiers, and case-sensitive immutable Scopes.
    - Implement child-flowing, nested-restoring ambient caller credentials with `AsyncLocal`.
    - Generate typed option roots and protected resolver/policy interfaces without a second registration route.
  - [ ] T1.6 [code] (satisfies: R1) — Implement conditional registration and enabled-only validation
    - In `Templates/SecurityRegistration/*TemplatePartial.cs`, register only enabled paths and require only enabled options.
    - Fail startup with the exact invalid option name while excluding configured secret values.
    - Keep `AddAryzacSecurity` as the only public registration path.
  - [ ] T1.7 [code] (satisfies: R1) — Implement inbound parsing and request context
    - In `Templates/SecurityInboundCredentials/*TemplatePartial.cs`, accept exactly one `Authorization` field value and only case-insensitive `Bearer`/`ApiKey` schemes.
    - Return `missing_credential`, `malformed_credential`, or `unsupported_credential_scheme` for their specified shapes and never execute the operation.
    - Require one non-empty credential value and classify API Keys only by the configured non-empty prefix using ordinal comparison.
    - Publish exactly one immutable Principal instance and ambient caller credential for the request lifetime.
  - [ ] T1.8 [code] (satisfies: R1) — Write foundation, registration, and inbound self-tests
    - Cover preserved Scope/module artifacts, one marker, one registration path, independent enablement, enabled-only validation, exact option names, and secret-free failures.
    - Cover absent, empty, multiple, malformed, supported, and unsupported authorization values plus ordinal API Key prefix behavior.
    - Cover Principal shape/same-instance availability, immutable case-sensitive Scopes, protected extension points, and nested ambient restoration.

- [ ] T2. Configurable JWT validation
  - [ ] T2.1 [model] (satisfies: R2) — Add `SecurityJwtValidation`
    - Add `SecurityJwtValidation` as `C# Template`, type `Single File`, with `C# File Builder` and `Template Settings(Source=Lookup Type, Role=Api, Default Location=Security)`.
    - Generate public-only RSA options, primary/secondary rotation, independent issuer/audience switches, clock skew, typed outcomes, and service-token classification hooks.
  - [ ] T2.2 [code] (satisfies: R2) — Implement public-key JWT validation
    - In `Templates/SecurityJwtValidation/*TemplatePartial.cs`, import only RSA public keys and require a primary with at most one secondary key.
    - Enforce issuer/audience allow-lists only when independently enabled.
    - Apply clock skew default 60 seconds with accepted range 0–300 seconds.
    - Validate with the primary only, or either configured key during overlap.
  - [ ] T2.3 [code] (satisfies: R2) — Implement JWT outcomes and classification
    - Return `invalid_credential` without cryptographic detail for malformed tokens, invalid signatures, enabled issuer/audience failures, or missing Principal claims.
    - Return `expired_credential` after configured skew.
    - Classify a validated service Principal carrying the Reserved Service Scope as Service Token; otherwise classify it as JWT.
  - [ ] T2.4 [code] (satisfies: R2) — Write JWT self-tests
    - Cover primary/secondary keys, issuer/audience enabled and disabled, skew defaults/boundaries, invalid configuration, signatures, malformed/expired tokens, claims, classification, and non-disclosure.

- [ ] T3. Typed API-key resolution
  - [ ] T3.1 [model] (satisfies: R3) — Add `SecurityApiKeyResolution`
    - Add `SecurityApiKeyResolution` with the common API template settings.
    - Generate local typed resolver contracts, the remote client, timeout options, active-result HMAC cache, and expiry bounding; generate no authority endpoint or credential store.
  - [ ] T3.2 [code] (satisfies: R3) — Implement typed local and remote resolution
    - In `Templates/SecurityApiKeyResolution/*TemplatePartial.cs`, implement `active`, `invalid_credential`, `expired_credential`, `revoked_credential`, and `credential_resolution_unavailable`.
    - Permit a Principal and optional expiry only for `active`.
    - Send the complete API Key only to the configured resolver endpoint with configured authentication.
    - Enforce timeout default 5 seconds/range 1–60 and map timeout, connection, non-success, and malformed bodies to unavailable without dependency-body disclosure.
  - [ ] T3.3 [code] (satisfies: R3) — Implement active-only resolver caching
    - Cache only `active` results under an HMAC-SHA256 digest of the complete credential.
    - Bound duration by configured maximum (default 60 seconds) and remaining credential lifetime.
    - Never cache any failure outcome; resolve again after expiry.
  - [ ] T3.4 [code] (satisfies: R3) — Write resolver transport and cache self-tests
    - Cover every outcome, Principal/expiry invariants, authenticated transport, timeout and malformed/non-success behavior.
    - Cover active reuse, expiry bounds, re-resolution, failure non-caching, HMAC keys, and credential/dependency-body non-disclosure.

- [ ] T4. Outbound credential selection
  - [ ] T4.1 [model] (satisfies: R4) — Add `SecurityOutboundCredentials`
    - Add `SecurityOutboundCredentials` as `C# Template`, type `Single File`, `C# File Builder`, `Template Settings(Source=Lookup Type, Role=Application, Default Location=Security)`.
    - Generate ambient forwarding, client-credentials acquisition, timeout/safety options, cache, per-client/endpoint single-flight, typed failure, and no-send handler surfaces.
  - [ ] T4.2 [model] (satisfies: R4) — Add `SecurityHttpClientFactoryExtension`
    - Add `SecurityHttpClientFactoryExtension` as a Module Builder `Factory Extension` that attaches the outbound handler exactly once to compatible generated HTTP clients.
  - [ ] T4.3 [code] (satisfies: R4) — Implement selection, acquisition, and no-send behavior
    - In `Templates/SecurityOutboundCredentials/*TemplatePartial.cs`, forward ambient scheme and bytes unchanged and skip acquisition.
    - Otherwise acquire with `client_credentials`, configured client identity/secret, and Reserved Service Scope.
    - Enforce timeout default 10 seconds/range 1–120 and return typed failure without sending downstream on timeout, connection, non-success, or malformed response.
  - [ ] T4.4 [code] (satisfies: R4) — Implement cache and single-flight
    - Cache until expiry minus safety window, default 60 seconds; do not cache when remaining lifetime is at or below the window.
    - Share exactly one acquisition success or failure per configured client/endpoint among concurrent callers.
    - Remove completed failures so the next caller can retry.
  - [ ] T4.5 [code] (satisfies: R4) — Implement generated-client integration
    - In `FactoryExtensions/SecurityHttpClientFactoryExtension.cs`, attach the handler once and expose duplicate attachment to conformance failure.
  - [ ] T4.6 [code] (satisfies: R4) — Write outbound selection and concurrency self-tests
    - Cover byte-for-byte forwarding, absence fallback, exact grant/Scope, timeout/failures, no unauthenticated send, expiry safety, shared success/failure, retry, and duplicate attachment.
    - Assert caller/service credentials, client secrets, and dependency bodies are never disclosed.

- [ ] T5. Scope authorization and stable rejections
  - [ ] T5.1 [model] (satisfies: R8) — Add `SecurityAuthorization` over preserved Scope metadata
    - Add `SecurityAuthorization` with the common API template settings.
    - Preserve every existing `Secured`/`Unsecured` value, Scope setting/assignment, and `ScopePermissionMap` input.
    - Generate all-Scope ordinal enforcement, empty-scope behavior, Reserved Service Scope bypass hook, and immutable RFC 9457 rejection mapping.
  - [ ] T5.2 [code] (satisfies: R8) — Implement Scope enforcement
    - In `Templates/SecurityAuthorization/*TemplatePartial.cs`, treat absent Scopes as empty and permit authenticated secured operations with no required Scope.
    - Require every declared Scope ordinally unless the Reserved Service Scope rule applies.
    - Return `insufficient_scope`/403 and prevent operation execution on failure.
  - [ ] T5.3 [code] (satisfies: R8) — Implement stable RFC 9457 rejections
    - Emit `application/problem+json` with `type`, `title`, `status`, and immutable `code`; build type from configured base URI plus `/code`.
    - Support exactly the eight required stable codes and fixed 401/403/503 mappings.
    - Add `WWW-Authenticate: Bearer` to applicable 401 responses without credential/cryptographic detail.
    - Allow configurable titles/base URI but never code/status semantics.
  - [ ] T5.4 [code] (satisfies: R8) — Write authorization and rejection self-tests
    - Cover preserved metadata, empty/no-required/all-required Scopes, ordinal comparison, non-execution, exact code set, media/shape/URI/status, challenge, and configurability boundaries.

- [ ] T6. Credential-safe diagnostics
  - [ ] T6.1 [model] (satisfies: R11) — Add `SecurityDiagnostics`
    - Add `SecurityDiagnostics` with the common API template settings.
    - Generate safe rejection records, request correlation, dedicated diagnostics-key HMAC correlation, permitted API Key prefix metadata, and centralized redaction helpers.
  - [ ] T6.2 [code] (satisfies: R11) — Implement credential correlation
    - In `Templates/SecurityDiagnostics/*TemplatePartial.cs`, require a dedicated non-empty HMAC key only when enabled and name the missing option safely.
    - Compute HMAC-SHA256 over the complete credential and render exactly 64 lowercase hex characters.
    - Never derive the key from credentials, Internal Service Keys, client secrets, or signing keys; record at most the configured API Key prefix.
  - [ ] T6.3 [code] (satisfies: R11) — Integrate safe diagnostics across runtime paths
    - Record request correlation, stable code, classifiable kind, resolved Principal identity/type, and credential correlation.
    - Redact all complete/partial credentials, signing details, secrets, dependency bodies, and forbidden data from logs, traces, metrics, Problem Details, rejection records, and caller-visible exceptions.
  - [ ] T6.4 [code] (satisfies: R11) — Write diagnostics and sentinel self-tests
    - Cover enablement, startup validation, exact HMAC/rendering, and permitted fields.
    - Inject distinct sentinels for every credential/secret/body type and fail on disclosure through any captured surface.

- [ ] T7. Universal Service Token admission
  - [ ] T7.1 [code] (satisfies: R5) — Complete Service Token admission and safe rejection behavior
    - Extend JWT validation to admit arbitrary valid service Principals without a module-level allow-list.
    - Keep signature, expiry, issuer, audience, Principal claim, and classification checks active.
    - Return typed invalid/expired outcomes and identify a resolved service Principal in rejection records without the token.
  - [ ] T7.2 [code] (satisfies: R5) — Complete Reserved Service Scope authorization
    - Extend authorization so the configured Reserved Service Scope bypasses only per-operation Scope comparison and nothing else.
  - [ ] T7.3 [code] (satisfies: R5) — Write Service Token self-tests
    - Cover arbitrary service Principals, configured bypass, absent-Scope behavior, all validation checks, invalid/expired no-bypass, and safe rejection records.

- [ ] T8. Optional Internal Service Key
  - [ ] T8.1 [code] (satisfies: R6) — Add independently enabled registration and options
    - Extend the sole registration path with separate admission/presentation switches and enabled-only non-empty key/Principal validation.
    - Name missing options without secret disclosure and register no disabled path.
  - [ ] T8.2 [code] (satisfies: R6) — Implement Internal Service Key admission
    - In the inbound template, compare complete values in fixed time, resolve the configured service Principal with Reserved Service Scope on match, and return `invalid_credential` without API Key resolver fallback on mismatch.
  - [ ] T8.3 [code] (satisfies: R6) — Implement outbound Internal Service Key presentation
    - In the outbound template, preserve ambient forwarding precedence; otherwise send `Authorization: ApiKey <configured-value>` without Service Credential acquisition when enabled.
  - [ ] T8.4 [code] (satisfies: R6) — Integrate Internal Service Key redaction
    - Exclude configured/presented values from all diagnostic, rejection, metric, trace, response, and exception surfaces.
  - [ ] T8.5 [code] (satisfies: R6) — Write Internal Service Key self-tests
    - Cover independent enablement, fixed-time match, configuration failures, mismatch/no fallback, ambient precedence, presentation fallback, disabled behavior, and non-disclosure.

- [ ] T9. Reusable conformance test kit
  - [ ] T9.1 [model] (satisfies: R9) — Add `SecurityConformanceTestKit`
    - Add `SecurityConformanceTestKit` as `C# Template`, type `Single File`, `C# File Builder`, `Template Settings(Source=Lookup Type, Role=Tests, Default Location=Security/Conformance)`.
    - Generate reusable service/factory discovery, contract, rejection, concurrency, disclosure, and benchmark fixture surfaces.
  - [ ] T9.2 [model] (satisfies: R9) — Add dedicated `SecurityConformanceRegistration`
    - Add the selected distinct `Template Registration` element and register test-kit output only when a compatible `Tests` target exists.
  - [ ] T9.3 [model] (satisfies: R9) — Add conformance output anchors
    - In `Aryzac.Security.Tests`, add only anchors/folders needed for `Conformance`, `Fixtures`, and `Benchmarks`; consume, do not copy, the generated fixture contract.
  - [ ] T9.4 [code] (satisfies: R9) — Implement discovery and criterion-addressed contract assertions
    - In `Templates/SecurityConformanceTestKit/*TemplatePartial.cs`, accept discovered service instances/factories without a fixed count.
    - Verify one marker and one shared registration per service.
    - Make every failure identify service/component, security concern, and violated requirement acceptance criterion.
  - [ ] T9.5 [code] (satisfies: R9) — Implement inbound, JWT, resolver, and Internal Service Key fixtures
    - Cover positive/negative classification, Principal availability, JWT validation, every resolver outcome/cache case, Internal Service Key behavior, and applicable stable codes.
  - [ ] T9.6 [code] (satisfies: R9) — Implement authorization, rejection, outbound, and concurrency fixtures
    - Cover Scope/Reserved Scope, RFC 9457 contracts, ambient/acquired credentials, caching, timeout/no-send, single-flight/retry, and duplicate handler attachment.
  - [ ] T9.7 [code] (satisfies: R9, R11) — Implement reusable non-disclosure fixtures
    - Inject distinct credential/secret/body sentinels, inspect every captured surface, fail on forbidden disclosure, and positively assert permitted diagnostic fields.
  - [ ] T9.8 [code] (satisfies: R9) — Implement warmed benchmark fixtures
    - Compare shared inbound handling to a same-process baseline using at least 100 warm-ups and 1,000 measured executions per path.
    - Calculate medians and fail when overhead exceeds 5 milliseconds.
  - [ ] T9.9 [code] (satisfies: R9) — Implement the `Aryzac.Security.Tests` conformance host
    - Run the generated fixture contract against module-owned generated services and report success only when every conformance and performance assertion passes.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 1, "label": "Shared security foundation", "tasks": ["T1.1", "T1.2", "T1.3", "T1.4", "T1.5", "T1.6", "T1.7", "T1.8"] },
    { "id": 2, "label": "Core credential, authorization, and diagnostics capabilities", "tasks": ["T2.1", "T2.2", "T2.3", "T2.4", "T3.1", "T3.2", "T3.3", "T3.4", "T4.1", "T4.2", "T4.3", "T4.4", "T4.5", "T4.6", "T5.1", "T5.2", "T5.3", "T5.4", "T6.1", "T6.2", "T6.3", "T6.4"] },
    { "id": 3, "label": "Service and internal credential extensions", "tasks": ["T7.1", "T7.2", "T7.3", "T8.1", "T8.2", "T8.3", "T8.4", "T8.5"] },
    { "id": 4, "label": "Reusable conformance kit", "tasks": ["T9.1", "T9.2", "T9.3", "T9.4", "T9.5", "T9.6", "T9.7", "T9.8", "T9.9"] }
  ]
}
```
