# Design Document

## Design Summary

`Aryzac.Security` remains one additive 1.x Intent module and preserves the existing Scope designer extensions and `ScopePermissionMap` output. The incomplete, hard-coded `SecurityContract` implementation is replaced by cohesive Module Builder `C# Template` elements that generate one shared runtime contract behind exactly one public `AddAryzacSecurity` registration entry point.

The generated contract is self-contained: the legacy `Intent.Security.JWT` 4.3.10 dependency is removed because its authority-oriented registration cannot realize the required public-key option contract and risks introducing a second inbound path. Runtime behavior is configured through typed generated options; product-specific values remain consumer configuration.

The reusable conformance kit is generated into output targets with role `Tests`. A dedicated `Aryzac.Security.Tests` project exercises the same generated fixture contract for module self-tests.

## Resolved Architecture Decisions

- Use modular generated templates rather than expanding the existing single 753-line template.
- Retain exactly one public registration entry point: `SecurityContractRegistration.AddAryzacSecurity`.
- Remove the `Intent.Security.JWT` dependency; preserve compatibility at the module identity, Scope designer-extension, Scope stereotype, and `ScopePermissionMap` artifact level.
- Generate reusable conformance fixtures to the `Tests` role and add a module-owned self-test project.
- Do not add authority endpoints, credential stores, product-specific models, deployment configuration, or a companion authority module.

## Module & Architecture Prerequisites

No new Intent Architect module must be installed before implementation. The installed `Intent.ModuleBuilder` 3.18.4, `Intent.ModuleBuilder.CSharp` 3.7.6, Codebase Structure, Visual Studio Projects, and Roslyn Weaver modules provide the required model, template, project, and code-management capabilities.

The existing generated manifest dependency on `Intent.Security.JWT` 4.3.10 must be removed when the module is regenerated. It is not a prerequisite for this design.

## Existing Model Preserved

### Module Builder

- Package `Aryzac.Security` (`b03410a6-8af0-4dad-a18c-1f6d5490612c`) retains `Module Settings`, its 1.x identity, and selected Services designer reference.
- `Security Extensions`, `Scope`, `Scope Configuration`, `Scope Verb`, `Scope Package Extension`, `Scope Element Script`, Command/Query extensions, both `Scope Settings` stereotypes, and all existing context-menu behavior remain unchanged.
- `ScopePermissionMap` (`5413af14-47de-4778-8883-bd789689bc6d`) remains a `C# Template` with `C# Template Settings(Templating Method=C# File Builder)` and `Template Settings(Source=Lookup Type, Role=Application, Default Location=Security)`.

### Codebase Structure

- Existing `Aryzac.Security` project (`d6257d5c-437c-4b7e-8955-81528181567b`) and its current output anchors remain.

## Model Changes

### Module Builder Designer

Replace the unmodeled monolithic `SecurityContract` template registration with the following modeled, package-level elements. Each is a `C# Template` with type reference `Single File` and `C# Template Settings(Templating Method=C# File Builder)`. API-facing runtime templates use `Template Settings(Source=Lookup Type, Role=Api, Default Location=Security)` unless stated otherwise.

1. `SecurityContractFoundation`
   - Generates the single assembly marker, contract version, glossary-named immutable contracts, stable outcome enums, option roots, and protected extension-point interfaces.
   - Generated contracts include `CallerCredential`, `Principal`, `Scope`, `SecurityRejection`, `CredentialResolverOutcome`, `CredentialAcquisitionResult`, and the Ambient Caller Credential accessor.

2. `SecurityRegistration`
   - Generates the only public registration path: `AddAryzacSecurity(Action<AryzacSecurityOptions>)` and a configuration-binding overload that delegates to the same core path.
   - Registers each capability only when enabled and validates only enabled capability options at startup.
   - No other generated template exposes a public registration extension.

3. `SecurityInboundCredentials`
   - Generates the authorization-field classifier and request-scoped Principal publication.
   - Recognizes only case-insensitive `Bearer` and `ApiKey`; enforces one field value and one non-empty credential value.
   - Uses ordinal API Key prefix matching and routes an Internal Service Key mismatch directly to `invalid_credential` without resolver fallback.

4. `SecurityJwtValidation`
   - Generates RSA public-key loading and JWT validation with one required primary key, one optional secondary key, independent issuer/audience switches and allowed-value collections, and clock skew default/range validation.
   - Classifies a validated service Principal carrying the configured Reserved Service Scope as a Service Token.

5. `SecurityApiKeyResolution`
   - Generates the local `Credential Resolver` extension point, remote resolver client, typed outcomes, active-only HMAC-keyed cache, expiry bounding, timeout handling, and dependency-body suppression.
   - The remote authentication strategy is a typed protected interface registered through the single registration path; authority-specific storage and endpoint handlers are not generated.

6. `SecurityOutboundCredentials`
   - Uses `Template Settings(Source=Lookup Type, Role=Application, Default Location=Security)`.
   - Generates ambient forwarding, Internal Service Key presentation fallback, OAuth-compatible client-credentials acquisition, expiry-safety caching, per-client/endpoint single-flight coordination, and a delegating handler that refuses to send when selection fails.

7. `SecurityAuthorization`
   - Generates Scope enforcement over the existing operation `Secured`, `Unsecured`, Scope settings, and Scope assignments.
   - Applies all declared Scopes with ordinal comparison, treats absent Principal Scopes as empty, and permits the Reserved Service Scope to bypass only Scope comparison.
   - Generates RFC 9457 Problem Details mapping and exact stable status/code semantics.

8. `SecurityDiagnostics`
   - Generates credential-safe rejection records, request correlation integration, and HMAC-SHA256 credential correlation using a dedicated diagnostics key.
   - Correlation output is exactly 64 lowercase hexadecimal characters; only the configured API Key format prefix may be recorded.

9. `SecurityConformanceTestKit`
   - Uses `Template Settings(Source=Lookup Type, Role=Tests, Default Location=Security/Conformance)`.
   - Generates framework-neutral reusable fixtures/assertions for service discovery/factories, marker and registration uniqueness, all enabled credential paths, exact stable rejection contracts, non-execution, cache/single-flight behavior, sentinel non-disclosure, and warmed median benchmarking.
   - Failure objects identify the service/generated component, security concern, and requirement criterion.

10. `SecurityHttpClientFactoryExtension` (`Factory Extension`)
    - Wires the generated outbound delegating handler into supported generated HTTP clients without creating another registration entry point.
    - Its implementation discovers compatible client templates and applies the handler once; duplicate attachment is a conformance failure.

11. `SecurityConformanceRegistration` (`Template Registration` or the registration generated with the `SecurityConformanceTestKit` template, following the existing Module Builder C# pattern)
    - Registers the test-kit template only for a `Tests` output target.

The previous hard-coded names and values (`Verentis*`, `vrt_`, `https://verentis.io/problems`, fixed issuer configuration, and application-name deviations) are removed. All corresponding values become typed consumer options.

### Codebase Structure Designer

Under solution `Aryzac`, add `Aryzac.Security.Tests` as `C# Project (.NET)` adjacent to `Aryzac.Security`:

- `.NET Settings`: `SDK=Microsoft.NET.Sdk`, target the same .NET 8 framework as `Aryzac.Security`, `Output Type=Console Application`.
- `C# Project Options`: nullable enabled and the repository-default language version.
- Add a `Project Reference` association to existing `Aryzac.Security`, with source multiplicity `1` and target multiplicity `1`.
- Add folders/output anchors for `Conformance`, `Fixtures`, and `Benchmarks` only where required by generated or protected self-test code.
- The project invokes the same fixture contract generated by `SecurityConformanceTestKit`; it does not maintain a second fixture implementation.

## Generated Option Contract

`AryzacSecurityOptions` contains independent enablement/settings sections. Validation occurs through generated `IValidateOptions<T>` implementations registered by `SecurityRegistration`; validation messages name exact option paths and never values.

- `Jwt`: primary RSA public key required when enabled; optional secondary key; independent issuer/audience validation and non-empty allowed-value collections when their switches are enabled; `ClockSkewSeconds=60`, range `0..300`.
- `ApiKey`: non-empty `FormatPrefix`; local or remote Credential Resolver; remote completion timeout `5` seconds, range `1..60`; active-cache maximum `60` seconds; dedicated HMAC cache key.
- `ServiceCredential`: endpoint, client identity, client secret, Reserved Service Scope; timeout `10` seconds, range `1..120`; cache safety window `60` seconds.
- `InternalServiceKeyAdmission`: independently enabled; complete key plus configured Principal values required.
- `InternalServiceKeyPresentation`: independently enabled; complete presentation key required.
- `Authorization`: configurable Problem Details base URI and titles; stable codes/statuses are constants and cannot be overridden; configured Reserved Service Scope.
- `Diagnostics`: independently enabled; dedicated non-empty HMAC key required.

Secrets are held only in option objects and private runtime state; generated `ToString`, exceptions, diagnostics, metric labels, and Problem Details never render them.

## Code-Management Boundary

### Model-owned and Software-Factory-generated

- All `C# Template`, `Factory Extension`, template registration, output anchor, project, and project-reference scaffolding.
- Generated consumer runtime contracts, option types, DI wiring, authentication/authorization components, outbound handlers, and conformance fixture source.
- Existing Scope model extensions and `ScopePermissionMap` output.

### Protected hand-written code `[code]`

The Module Builder generates template/factory-extension scaffolding, but the following bespoke bodies must be implemented in merge/ignore-managed regions so regeneration cannot reclaim them:

- `Templates/SecurityContractFoundation/*TemplatePartial.cs`: immutable contract and option-generation logic.
- `Templates/SecurityRegistration/*TemplatePartial.cs`: conditional DI graph and startup validation generation.
- `Templates/SecurityInboundCredentials/*TemplatePartial.cs`: parser/classifier and request Principal flow generation.
- `Templates/SecurityJwtValidation/*TemplatePartial.cs`: RSA/JWT validation generation.
- `Templates/SecurityApiKeyResolution/*TemplatePartial.cs`: remote resolver/cache generation.
- `Templates/SecurityOutboundCredentials/*TemplatePartial.cs`: acquisition/cache/single-flight/handler generation.
- `Templates/SecurityAuthorization/*TemplatePartial.cs`: Scope and Problem Details generation.
- `Templates/SecurityDiagnostics/*TemplatePartial.cs`: safe diagnostics generation.
- `Templates/SecurityConformanceTestKit/*TemplatePartial.cs`: reusable fixture and benchmark generation.
- `FactoryExtensions/SecurityHttpClientFactoryExtension.cs`: generated-client integration logic.
- `Aryzac.Security.Tests/Conformance`, `Fixtures`, and `Benchmarks`: module self-test host logic and assertions. Project/solution scaffolding remains model-owned.

No generated consumer file is hand-patched. If a runtime shape is wrong, its owning Module Builder template/model is corrected and the Software Factory is rerun.

## Journey Realization

### UJ-1: Enable consistent inbound security

`SecurityRegistration` binds and validates selected options, `SecurityInboundCredentials` classifies the request, the enabled resolver validates it, `SecurityContractFoundation` exposes one immutable Principal instance, `SecurityAuthorization` applies existing operation metadata, and `SecurityDiagnostics` emits a safe `SecurityRejection`. Disabled capabilities add neither validation nor handlers.

### UJ-2: Call another service securely

`SecurityOutboundCredentials` first reads the Ambient Caller Credential, forwards its exact scheme/value when present, otherwise prefers enabled Internal Service Key presentation, otherwise performs single-flight Service Credential acquisition. Selection failure stops the delegating handler before sending.

### UJ-3: Prove reusable conformance

`SecurityConformanceTestKit` discovers supplied services/factories and executes contract, rejection, non-disclosure, concurrency, and benchmark fixtures. `Aryzac.Security.Tests` invokes the same fixture contract against module-owned generated test services.

## Per-Requirement Realization Plan

### R1 — Shared Security Contract Foundation

- `[model]` Add `SecurityContractFoundation`, `SecurityRegistration`, and `SecurityInboundCredentials` C# Templates; preserve all existing Scope elements and package identity.
- `[code]` Implement generated immutable Principal/Caller Credential contracts, AsyncLocal nested ambient scope restoration, exact header parsing, conditional registration, exact-path option validation, and protected resolver/policy interfaces in template partials.
- Mechanism: Module Builder `C# Template` modeled elements plus protected template bodies.
- Dependencies: none.
- Modelling order: Module Builder only.
- Code placement: after the three templates are modeled/generated, then interleave their bodies as one foundation slice.

### R2 — Configurable JWT Validation

- `[model]` Add `SecurityJwtValidation` C# Template.
- `[code]` Generate RSA public-only key import, primary/secondary overlap, issuer and audience switches, clock skew default/range, Principal claim validation, typed invalid/expired outcomes, and tests. No private-key option exists.
- Mechanism: modeled `C# Template` plus protected template body; legacy `Intent.Security.JWT` dependency removed.
- Dependencies: R1.
- Modelling order: Module Builder only.
- Code placement: after R1 generation; implementation and its focused conformance fixtures may be interleaved.

### R3 — Typed API-Key Resolution Capability

- `[model]` Add `SecurityApiKeyResolution` C# Template.
- `[code]` Generate typed local/remote resolver contracts, complete-key transport, configurable authentication strategy, timeout, active-only HMAC cache, lifetime bounding, failure non-caching, and safe unavailable failures.
- Mechanism: modeled `C# Template` plus protected template body.
- Dependencies: R1.
- Modelling order: Module Builder only.
- Code placement: after template generation; resolver and cache fixtures interleaved.

### R4 — Outbound Credential Selection

- `[model]` Add `SecurityOutboundCredentials` C# Template and `SecurityHttpClientFactoryExtension`.
- `[code]` Generate exact ambient forwarding, client-credentials acquisition, safety-window cache, per-client/endpoint single-flight, retry after shared failure, and no-send-on-failure behavior; implement factory-extension client wiring once.
- Mechanism: modeled `C# Template` and `Factory Extension` plus protected bodies.
- Dependencies: R1.
- Modelling order: Module Builder template before factory extension wiring.
- Code placement: after both elements generate; selector behavior before client-wiring code.

### R5 — Universal Service-Token Admission

- `[model]` No separate template; extend the modeled `SecurityJwtValidation` and `SecurityAuthorization` templates.
- `[code]` Classify validated service Principals, admit them without allow-lists, bypass only Scope comparison for the Reserved Service Scope, and retain all authentication checks and safe rejection identity fields.
- Mechanism: existing modeled template elements `SecurityJwtValidation` and `SecurityAuthorization`.
- Dependencies: R2 and R8.
- Modelling order: JWT behavior before authorization bypass integration.
- Code placement: after both owning templates are generated.

### R6 — Optional Internal Service Key Primitive

- `[model]` Extend `SecurityInboundCredentials`, `SecurityOutboundCredentials`, and `SecurityRegistration`; no sibling registration template.
- `[code]` Generate independent admission/presentation enablement, fixed-time complete-key comparison, configured service Principal creation, mismatch-without-resolver-fallback, ambient precedence, and secret non-disclosure.
- Mechanism: existing modeled C# Templates.
- Dependencies: R3 and R4.
- Modelling order: inbound classification before outbound presentation integration.
- Code placement: interleaved with R3/R4 owning template bodies after their model elements exist.

### R8 — Scope Enforcement and Stable Rejections

- `[model]` Add `SecurityAuthorization`; preserve existing Scope stereotypes, settings, assignments, and `ScopePermissionMap`.
- `[code]` Generate all-Scope ordinal enforcement, empty-scope handling, no-required-Scope behavior, Reserved Service Scope hook, immutable stable code/status map, RFC 9457 body, Bearer challenge, and non-execution.
- Mechanism: modeled `C# Template`, consuming the installed module's existing Scope capability/stereotypes.
- Dependencies: R1.
- Modelling order: existing Scope model remains first; authorization template consumes it.
- Code placement: after template generation, before R5 integration.

### R9 — Reusable Conformance Test Kit

- `[model]` Add `SecurityConformanceTestKit` with Role `Tests`; add `Aryzac.Security.Tests` project, project reference, and required output anchors.
- `[code]` Generate reusable positive/negative fixtures, service/factory discovery, criterion-addressed failures, sentinel capture assertions, single-flight checks, and warmed benchmark with at least 100 warm-ups and 1,000 measured executions per path. Implement the module self-test host against the same fixture contract.
- Mechanism: modeled `C# Template`, modeled Codebase Structure project/reference, and protected fixture/host bodies.
- Dependencies: R1, R2, R3, R4, R5, R6, R8, and R11.
- Modelling order: Module Builder test-kit template, then Codebase Structure test project/reference.
- Code placement: after all runtime slices are generated and behaviorally complete.

### R11 — Credential-Safe Diagnostics

- `[model]` Add `SecurityDiagnostics`; extend `SecurityRegistration` options/registration.
- `[code]` Generate dedicated-key HMAC-SHA256 correlation, exact lowercase 64-character rendering, permitted-field-only rejection records, and centralized redaction/non-disclosure guards used by inbound, resolver, outbound, and authorization paths.
- Mechanism: modeled `C# Template` plus protected body.
- Dependencies: R1.
- Modelling order: diagnostics contract before integrations into other runtime templates.
- Code placement: implement core diagnostics after generation, then interleave integrations with R2/R3/R4/R5/R6/R8.

## Cross-Requirement Dependency Graph

- R1 is foundational and independent.
- R2 depends on R1.
- R3 depends on R1.
- R4 depends on R1.
- R8 depends on R1 and the preserved Scope model.
- R11 depends on R1.
- R5 depends on R2 and R8.
- R6 depends on R3 and R4.
- R9 depends on every runtime requirement: R1, R2, R3, R4, R5, R6, R8, R11.

A valid implementation wave order is: R1; then R2/R3/R4/R8/R11 in parallel-capable slices; then R5/R6; then R9.

## Traceability Targets

Trace requirements to the narrowest modeled owner rather than the package as a whole:

- R1 → `SecurityContractFoundation`, `SecurityRegistration`, `SecurityInboundCredentials`.
- R2 → `SecurityJwtValidation`.
- R3 → `SecurityApiKeyResolution`.
- R4 → `SecurityOutboundCredentials`, `SecurityHttpClientFactoryExtension`.
- R5 → service-token members in `SecurityJwtValidation` and Reserved Service Scope members in `SecurityAuthorization`.
- R6 → Internal Service Key members in `SecurityInboundCredentials`, `SecurityOutboundCredentials`, and `SecurityRegistration`.
- R8 → existing Scope stereotypes/assignments, `ScopePermissionMap`, and `SecurityAuthorization`.
- R9 → `SecurityConformanceTestKit`, `Aryzac.Security.Tests`, and its project reference.
- R11 → `SecurityDiagnostics` and diagnostics option members in `SecurityRegistration`.

## Assumptions

- The module remains on its current 1.x release line; this feature does not prescribe a release-version bump.
- `Bearer` and `ApiKey` remain the only inbound schemes.
- The generated Service Credential client consumes an OAuth-compatible client-credentials response with access token and expiry.
- Consumer-specific values and authority APIs remain outside this module.
- A consuming solution that wants generated conformance fixtures provides a `Tests` output target; no product-specific test project or fixed service count is generated.