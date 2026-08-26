# Requirements Document

## Introduction

`Aryzac.Security.Service` shall be an installable companion module that turns a backend web application into a Security Authority. It shall support both a dedicated Security Authority application and additive installation into an existing application without removing or replacing existing endpoints, middleware, persistence, or business capabilities.

The Security Authority shall provide the protocol endpoints, authoritative security records, administration APIs, tenant/resource integration contracts, external identity-provider brokering, lifecycle notifications, and runtime validation needed to issue and manage credentials. It shall build on the shared caller-credential, Principal, Scope, and rejection contract supplied by `Aryzac.Security`. Browser-facing login, consent, device-verification, and logout-confirmation pages are outside this feature.

## Vision

A Module Consumer installs one module and receives a complete Security Authority rather than assembling token issuance, OIDC endpoints, credential storage, user and service identities, API keys, roles, grants, sessions, external identity providers, and administration behavior independently. The same module can host a dedicated authority or add authority capabilities to an existing application while preserving that application's existing behavior.

The authority remains reusable across products by requiring the host to supply tenant and resource context instead of hard-coding an Account/Workspace hierarchy. It also permits three explicit administrator-bootstrap strategies and optional eventing and audit integrations.

## Target Users & Jobs To Be Done

- **Module Consumer** — installs and configures a complete Security Authority in a dedicated or existing application.
- **Application Administrator** — registers clients and identity providers, manages Users, Services, API Keys, Roles, and Grants, and controls credential lifecycle.
- **Interactive User** — signs in through an external Identity Provider and receives standards-compatible tokens for an authorized OAuth Client.
- **Device User** — authorizes a CLI or constrained-input device from an authenticated browser session.
- **Service Developer** — registers a confidential OAuth Client or Service and obtains credentials for machine-to-machine access.
- **Platform Integrator** — supplies tenant/resource context and synchronizes security registrations through management APIs or Integration Events.

## Key User Journeys

- **UJ-1. A Module Consumer creates a dedicated Security Authority.**
  - **Persona + context:** A Module Consumer is creating a new backend service that will be the system's credential authority.
  - **Entry state:** The application has persistence and HTTP hosting but no Security Authority.
  - **Path:** The consumer installs `Aryzac.Security.Service`, selects enabled protocol capabilities, configures production cryptographic material and a Tenant Adapter, starts the application, and calls discovery.
  - **Climax:** The discovery and JWKS endpoints return the configured authority metadata and active signing key.
  - **Resolution:** The application can register clients, users, services, providers, resources, roles, and grants and can issue credentials.
  - **Edge case:** Startup stops with one actionable configuration error for each missing mandatory production setting.

- **UJ-2. A Module Consumer adds authority capabilities to an existing application.**
  - **Persona + context:** A Module Consumer has an established backend application with existing controllers, authentication behavior, persistence, and business features.
  - **Entry state:** Existing application routes and services are operational.
  - **Path:** The consumer installs the module, resolves any reported route or authentication-scheme conflict, supplies the Tenant Adapter, and starts the application.
  - **Climax:** Existing application behavior remains available and the Security Authority endpoints are added.
  - **Resolution:** One application hosts both its original features and the Security Authority.
  - **Edge case:** An incompatible route, scheme, or persistence capability produces a specific installation or startup error and does not silently replace existing behavior.

- **UJ-3. An Interactive User completes Authorization Code sign-in.**
  - **Persona + context:** An Interactive User starts sign-in from a registered public or confidential OAuth Client.
  - **Entry state:** The OAuth Client, redirect URI, requested Scopes, and external Identity Provider are active.
  - **Path:** The client starts authorization with PKCE, the authority redirects to the selected external Identity Provider, the provider callback establishes the User and SSO Session, and the client redeems the one-time Authorization Code.
  - **Climax:** The client receives an Access Token, ID Token, and Refresh Token containing only authorized claims and Scopes.
  - **Resolution:** The User can call protected APIs and later refresh or end the session.
  - **Edge case:** A mismatched redirect URI, invalid PKCE verifier, reused code, inactive User, inactive client, or disallowed Scope is rejected before a token is issued.

- **UJ-4. A Device User authorizes a CLI.**
  - **Persona + context:** A Device User signs into a CLI or constrained-input device.
  - **Entry state:** A registered OAuth Client supports Device Authorization.
  - **Path:** The device requests a Device Code and User Code, the User opens the host-supplied verification page, authenticates, reviews the pending request, approves or denies it, and the device polls the token endpoint at the declared interval.
  - **Climax:** Approval returns credentials to the device; denial or expiry returns the corresponding protocol error.
  - **Resolution:** The Device Grant is single-use and records its terminal state.
  - **Edge case:** Polling faster than the declared interval returns `slow_down` without issuing credentials.

- **UJ-5. An Application Administrator manages security access.**
  - **Persona + context:** An Application Administrator manages security for a Tenant Resource supplied by the host application.
  - **Entry state:** The administrator is authenticated and holds the required management Scope.
  - **Path:** The administrator creates or updates Users, Services, OAuth Clients, Identity Providers, API Keys, Roles, Role Memberships, Resources, and Grants; lists current state; and revokes or disables access when needed.
  - **Climax:** Authorization decisions and newly issued credentials reflect the committed change.
  - **Resolution:** Every lifecycle change can emit a typed notification and an audit record when adapters are configured.
  - **Edge case:** Duplicate commands are idempotent where an idempotency key is supplied, and concurrent stale updates return a conflict without losing a committed change.

- **UJ-6. A Platform Integrator synchronizes registrations.**
  - **Persona + context:** A Platform Integrator owns client, resource, user, or service lifecycle in another application.
  - **Entry state:** Optional Integration Event handling is enabled.
  - **Path:** The source publishes a versioned registration or lifecycle event, the Security Authority validates it, applies it idempotently, and emits the resulting lifecycle notification.
  - **Climax:** Replaying the same event produces no duplicate security record or duplicate effective Grant.
  - **Resolution:** The authority remains synchronized without requiring direct database access.
  - **Edge case:** An invalid event is rejected with its event identifier and validation reason and leaves authority state unchanged.

## Glossary

- **Security Authority** — the installed capability that owns security records and exposes OIDC, OAuth 2.0, credential, administration, and synchronization behavior.
- **Module Consumer** — the developer or team installing and configuring `Aryzac.Security.Service`.
- **Host Application** — the dedicated or existing backend application into which the module is installed.
- **Tenant Adapter** — the mandatory host-supplied contract that resolves Tenant, Tenant Resource, parent relationships, and the current request's tenant context for tenant-scoped behavior.
- **Tenant** — the host-defined isolation boundary returned by the Tenant Adapter; one security record may belong to zero or one Tenant as specified by its record contract.
- **Tenant Resource** — a host-defined securable item with a stable identifier, Resource Kind, optional parent Tenant Resource, Tenant identifier, and inheritance-protection state.
- **Resource Kind** — a non-empty, case-sensitive host-defined classification for a Tenant Resource.
- **Principal** — the canonical authenticated identity defined by `Aryzac.Security`; its Principal Type is User, Service, or Role.
- **User** — a human Principal with one status: New, Active, Suspended, Archived, or Deleted.
- **External Identity** — the unique pairing of Identity Provider issuer and subject linked to exactly one User.
- **Service** — a non-human Principal that is either Active or Inactive and belongs to zero or one Tenant.
- **Role** — a named Principal defined at one Tenant Resource and assignable to Users or Services.
- **Role Membership** — a time-bounded or non-expiring assignment of one User or Service to one Role.
- **Grant** — an Allow or Deny permission assigned to a User, Service, Role, or API Key for one Tenant Resource, applying either to that resource only or to that resource and descendants.
- **Scope** — the case-sensitive permission value defined by `Aryzac.Security` and used by OAuth Clients, credentials, management endpoints, and Grants.
- **OAuth Client** — a registered public or confidential application with a unique client identifier, allowed redirect URIs, allowed grant types, allowed Scopes, active state, and optional preferred Identity Provider.
- **Identity Provider** — an external OIDC authority configured as Generic OIDC, Entra External ID, Entra ID, Google, Auth0, or Keycloak.
- **Authorization Code** — a hashed, one-time credential issued to an OAuth Client after interactive authorization and bound to a User, redirect URI, Scopes, PKCE challenge, optional nonce, creation time, expiry time, and redemption time.
- **Device Grant** — a pending, approved, denied, expired, or redeemed authorization containing a hashed Device Code, normalized User Code, OAuth Client, requested Scopes, polling interval, User when approved, and lifecycle timestamps.
- **Refresh Token** — a hashed, client-bound, User-bound rotating credential with issue, expiry, use, replacement, and revocation timestamps.
- **Access Token** — a signed bearer credential for API access containing issuer, audience, subject, Principal Type, Scopes, issue time, not-before time, expiry time, and configured contextual claims.
- **ID Token** — a signed OIDC identity credential containing issuer, audience, User subject, issue time, expiry time, optional nonce, and configured identity claims.
- **SSO Session** — a server-side User session identified to the browser only by an opaque protected cookie and carrying issue, expiry, and optional revocation time.
- **API Key** — a credential authenticating one User or Service; its clear value is returned only when created or regenerated, while the authority retains its public prefix, hash, owner, optional Tenant, optional expiry, revocation state, and last-used time.
- **Bootstrap Strategy** — exactly one configured initial-administrator mode: Explicit Identity, First Eligible User, or Custom Seed Function.
- **Integration Event** — a versioned external registration or lifecycle message processed idempotently by the Security Authority.
- **Lifecycle Notification** — a typed notification emitted after a committed security lifecycle change when an event adapter is configured.
- **Audit Adapter** — an optional host integration that receives actor, action, target, Tenant, correlation identifier, timestamp, and outcome for security lifecycle changes.

## Non-Goals

- Generating browser-facing login, consent, device-verification, logout-confirmation, or administration pages.
- Providing local username/password registration, password storage, password reset, or password authentication.
- Owning the Host Application's Tenant or Tenant Resource model.
- Replacing existing Host Application endpoints, middleware, persistence, authentication schemes, or business logic without an explicit conflict resolution by the Module Consumer.
- Requiring a specific database engine, message bus, audit product, secrets platform, or deployment topology.
- Automatically provisioning production certificates, production signing keys, external Identity Provider applications, DNS, TLS certificates, or secret-store entries.
- Implementing SCIM, SAML, WebAuthn/passkeys, token introspection, dynamic OAuth client registration without administrator authorization, or a user-consent UI in the first release.

## MVP Scope

### In Scope

- Additive installation into dedicated and existing backend applications.
- OIDC discovery, JWKS, Authorization Code with PKCE, Client Credentials, Refresh Token, Device Authorization, UserInfo, and RP-initiated logout.
- Generic OIDC federation with presets for Entra External ID, Entra ID, Google, Auth0, and Keycloak.
- Authoritative records and management APIs for Users, External Identities, Services, OAuth Clients, Identity Providers, API Keys, Tenant Resources, Roles, Role Memberships, and Grants.
- Required host-defined Tenant Adapter and host persistence integration.
- Configurable administrator bootstrap using Explicit Identity, First Eligible User, or Custom Seed Function.
- Optional idempotent Integration Event consumers, Lifecycle Notifications, and Audit Adapter calls.

### Out of Scope for MVP

- Any browser UI.
- Local passwords and additional identity protocols.
- A module-owned tenant hierarchy.

## Success Metrics

**Primary**

- **SM-1**: A generated conformance suite passes 100% of the enabled OIDC and OAuth 2.0 happy-path and rejection cases defined by R4-R9. Validates R4, R5, R6, R7, R8, R9.
- **SM-2**: Installing the module into the reference dedicated service and one existing application produces all enabled authority endpoints with zero manual operation-body stubs. Validates R1, R2, R13.
- **SM-3**: Replaying each supported Integration Event ten times produces one resulting security record and one effective lifecycle transition. Validates R14.

**Counter-metrics (do not optimize)**

- **SM-C1**: Installation changes zero pre-existing route templates, controller actions, authentication scheme names, or persistence mappings unless the Module Consumer explicitly resolves a reported conflict. Counterbalances SM-2.
- **SM-C2**: Production logs, audit payloads, lifecycle notifications, and persisted records contain zero clear API Keys, client secrets, Authorization Codes, Device Codes, Refresh Tokens, private signing keys, or external provider secrets. Counterbalances SM-1 and SM-3.

## Requirements

### Requirement 1: Complete Additive Module Installation

**User Story:** As a Module Consumer, I want one module to add a complete Security Authority, so that I do not assemble authority capabilities separately.

**Realizes:** UJ-1, UJ-2

#### Acceptance Criteria

1. WHEN `Aryzac.Security.Service` is installed, THE module SHALL require the compatible `Aryzac.Security` contract and SHALL add the enabled authority endpoints, security records, management contracts, registration behavior, configuration validation, and conformance tests.
2. WHEN installed into an existing Host Application, THE module SHALL preserve every pre-existing route template, controller action, authentication scheme name, authorization policy, persistence mapping, middleware registration, and business service unless the Module Consumer explicitly changes it.
3. IF an authority route, authentication scheme, configuration key, or persistence registration conflicts with an existing registration, THEN THE module SHALL identify both conflicting registrations and SHALL NOT silently replace either registration.
4. WHEN generation is repeated without model or setting changes, THE module SHALL propose zero structural code changes and SHALL create no duplicate endpoint, registration, security record definition, or configuration entry.
5. IF a required companion capability is absent or incompatible, THEN THE module SHALL stop generation with one actionable error naming the missing or incompatible capability and the corrective action.
6. THE module SHALL support enabling or disabling Authorization Code, Client Credentials, Refresh Token, Device Authorization, external Identity Provider brokering, management APIs, Integration Events, Lifecycle Notifications, and auditing independently, except that Refresh Token requires Authorization Code or Device Authorization.

### Requirement 2: Host Persistence and Tenant Integration

**User Story:** As a Platform Integrator, I want the authority to use host persistence and host-defined tenancy, so that it fits dedicated and existing applications.

**Realizes:** UJ-1, UJ-2, UJ-5, UJ-6

#### Acceptance Criteria

1. THE Security Authority SHALL persist its records through the Host Application's configured persistence capability and SHALL NOT require a specific database engine.
2. THE Tenant Adapter SHALL resolve a Tenant identifier, Tenant Resource identifier, Resource Kind, optional parent Tenant Resource identifier, and inheritance-protection state for every tenant-scoped authorization decision.
3. IF tenant-scoped capabilities are enabled and no Tenant Adapter is registered, THEN THE Host Application SHALL fail startup with an error naming the missing Tenant Adapter contract.
4. IF the Tenant Adapter returns an unknown Tenant Resource, a cyclic parent chain, an empty Resource Kind, or a Tenant mismatch within one parent chain, THEN THE Security Authority SHALL reject the operation and SHALL NOT create or update a Grant, Role, Role Membership, Service, API Key, or OAuth Client association.
5. THE Security Authority SHALL keep authority-owned records isolated from Host Application records while permitting both to participate in the Host Application's configured transaction boundary where that persistence capability supports transactions.
6. IF the selected host persistence capability cannot provide required uniqueness, optimistic concurrency, atomic credential rotation, or one-time redemption behavior, THEN THE Host Application SHALL fail startup before accepting authority traffic.

### Requirement 3: Authoritative Security Record Contract

**User Story:** As an Application Administrator, I want complete authority-owned records, so that credential and authorization behavior is deterministic.

**Realizes:** UJ-3, UJ-4, UJ-5, UJ-6

#### Acceptance Criteria

1. THE Security Authority SHALL retain for each User: stable identifier, display name from 1 to 200 characters, normalized email address from 3 to 320 characters, optional avatar URL up to 2048 characters, status, last-seen time, creation time, update time, and zero or more External Identities.
2. THE Security Authority SHALL retain for each External Identity: issuer up to 2048 characters, subject up to 255 characters, linked User identifier, creation time, and last-seen time; the issuer-and-subject pair SHALL be globally unique.
3. THE Security Authority SHALL retain for each Service: stable identifier, name from 1 to 200 characters, optional description up to 2000 characters, optional Tenant identifier, active state, creation time, and update time.
4. THE Security Authority SHALL retain for each OAuth Client: unique client identifier from 1 to 200 characters, display name from 1 to 200 characters, Public or Confidential type, optional hashed secret, active state, zero or more exact redirect URIs, one or more allowed grant types, zero or more allowed Scopes, optional Tenant identifier, optional preferred Identity Provider identifier, creation time, and update time.
5. THE Security Authority SHALL retain for each Identity Provider: unique provider identifier from 1 to 100 characters, provider type, display name from 1 to 200 characters, authority URL, optional issuer, client identifier, encrypted client secret, space-delimited requested Scopes, active state, display priority, optional Tenant Resource identifier, Invite Only or Open SSO access mode, creation time, and update time.
6. THE Security Authority SHALL retain for each API Key: stable identifier, name from 1 to 200 characters, owner Principal Type restricted to User or Service, owner identifier, public key prefix, one-way key hash, optional Tenant identifier, optional expiry time, revocation state and time, last-used time, creation time, update time, and zero or more direct API Key Grants.
7. THE Security Authority SHALL retain for each Tenant Resource registration: stable host identifier, Resource Kind, optional parent identifier, Tenant identifier, inheritance-protection state, and concurrency stamp.
8. THE Security Authority SHALL retain for each Role: stable identifier, definition Tenant Resource identifier, role key from 1 to 100 case-sensitive characters unique within that Tenant Resource, name from 1 to 200 characters, optional description up to 2000 characters, enabled state, optional Tenant identifier, creation time, update time, and concurrency stamp.
9. THE Security Authority SHALL retain for each Role Membership: stable identifier, Role identifier, exactly one User or Service identifier, optional expiry time, revocation state and time, optional reason up to 1000 characters, creation time, and concurrency stamp.
10. THE Security Authority SHALL retain for each Grant: stable identifier, Principal Type, Principal identifier, Tenant Resource identifier, Resource Kind, case-sensitive Permission Key from 1 to 200 characters, Allow or Deny effect, This Resource Only or This Resource And Descendants applicability, optional expiry time, revocation state and time, optional reason up to 1000 characters, optional Tenant identifier, creation time, update time, and concurrency stamp.
11. THE Security Authority SHALL retain Authorization Codes, Device Grants, Refresh Tokens, Access Token metadata, ID Token metadata, and SSO Sessions with every field defined in the Glossary and SHALL store only hashes or opaque identifiers for redeemable bearer secrets.
12. IF a required field is empty, exceeds its stated bound, contains an invalid absolute URI, references an unknown record, or violates a uniqueness rule, THEN THE Security Authority SHALL return a field-addressed validation error and SHALL NOT persist any part of the request.

### Requirement 4: Cryptographic Material and Secret Handling

**User Story:** As a Module Consumer, I want explicit cryptographic behavior, so that credentials are not issued with unknown or leaked key material.

**Realizes:** UJ-1, UJ-2, UJ-3, UJ-4

#### Acceptance Criteria

1. THE Security Authority SHALL sign Access Tokens and ID Tokens with an asymmetric signing key and SHALL publish only active public keys through JWKS.
2. EACH published key SHALL have a non-empty `kid`, supported algorithm, key type, modulus, and exponent, and each issued token SHALL reference the signing key's `kid`.
3. IN Development, IF no signing key is configured, THEN THE Security Authority SHALL create an ephemeral development signing key, SHALL log that tokens will become invalid after restart, and SHALL NOT persist or export the private key.
4. OUTSIDE Development, IF a signing private key, issuer, external provider secret-protection key, or SSO cookie-protection capability is missing, THEN THE Host Application SHALL fail startup before listening for requests.
5. THE Security Authority SHALL encrypt external Identity Provider client secrets at rest and SHALL never return them from read or list APIs.
6. THE Security Authority SHALL store OAuth Client secrets, API Keys, Authorization Codes, Device Codes, and Refresh Tokens only as non-reversible hashes after their one-time clear value has been returned.
7. THE Security Authority SHALL exclude clear credential values and private key material from logs, errors, Audit Adapter payloads, Lifecycle Notifications, and Integration Event rejection messages.
8. WHEN signing keys rotate, THE Security Authority SHALL publish the new key before issuing tokens with it and SHALL retain each previous public key until every token signed by that key has expired.

### Requirement 5: Discovery, JWKS, and OAuth Client Validation

**User Story:** As a Service Developer, I want standards-compatible authority metadata and client validation, so that clients can discover and use the authority.

**Realizes:** UJ-1, UJ-3, UJ-4

#### Acceptance Criteria

1. WHEN OIDC is enabled, THE Security Authority SHALL expose `GET /.well-known/openid-configuration` anonymously and return issuer, authorization endpoint, token endpoint, UserInfo endpoint, JWKS URI, end-session endpoint, Device Authorization endpoint when enabled, supported response types, supported grant types, supported subject types, supported signing algorithms, supported Scopes, supported claims, and supported PKCE methods.
2. WHEN OIDC is enabled, THE Security Authority SHALL expose `GET /.well-known/jwks.json` anonymously and return every active or still-valid verification key.
3. THE issuer returned by discovery SHALL exactly equal the `iss` claim in every issued Access Token and ID Token.
4. THE Security Authority SHALL compare redirect URIs by exact ordinal match and SHALL reject unregistered, relative, malformed, or fragment-bearing redirect URIs.
5. A Public OAuth Client SHALL NOT possess or require a client secret and SHALL require PKCE with `S256` for Authorization Code redemption.
6. A Confidential OAuth Client SHALL authenticate with HTTP Basic or `client_secret_post`; IF both methods are supplied or the secret is invalid, THEN THE token request SHALL return `invalid_client` and SHALL issue no credential.
7. AN inactive OAuth Client SHALL be rejected at authorization, Device Authorization, and token issuance.

### Requirement 6: Authorization Code with PKCE

**User Story:** As an Interactive User, I want browser-based sign-in, so that a registered client receives credentials without handling my external provider secret.

**Realizes:** UJ-3

#### Acceptance Criteria

1. THE Security Authority SHALL expose `GET /connect/authorize` anonymously with required `client_id`, `redirect_uri`, and `response_type=code`; optional `scope`, `state`, `nonce`, `code_challenge`, and `code_challenge_method`; and SHALL reject any unsupported response type.
2. WHEN the request is valid, THE Security Authority SHALL select the OAuth Client's preferred active Identity Provider or the highest-priority active eligible provider and SHALL redirect the browser to that provider with protected correlation state.
3. THE protected correlation state SHALL expire after 10 minutes, bind client identifier, redirect URI, requested Scopes, PKCE values, nonce, and return state, and SHALL be rejected after successful use.
4. THE Security Authority SHALL expose `GET /connect/callback` anonymously, validate provider state and response, link or create the External Identity and User according to provider access mode, establish an SSO Session, issue a one-time Authorization Code, and redirect to the exact registered redirect URI with `code` and the original `state`.
5. AN Authorization Code SHALL expire after 5 minutes, SHALL be single-use, and SHALL be bound to client identifier, redirect URI, User, granted Scopes, PKCE challenge, and nonce.
6. THE token endpoint SHALL reject an expired, redeemed, unknown, client-mismatched, redirect-mismatched, or PKCE-mismatched Authorization Code with `invalid_grant` and SHALL issue no credential.
7. THE authority SHALL issue only Scopes that are both requested and allowed for the OAuth Client; an unknown or disallowed Scope SHALL return `invalid_scope` before redirecting to an external Identity Provider.
8. BECAUSE the MVP contains no consent UI, THE authority SHALL treat administrator-configured OAuth Client Scopes as pre-authorized and SHALL NOT issue any Scope outside that configured set.

### Requirement 7: Token Endpoint and Credential Issuance

**User Story:** As an OAuth Client, I want one token endpoint for enabled grants, so that I can obtain and renew credentials predictably.

**Realizes:** UJ-3, UJ-4

#### Acceptance Criteria

1. THE Security Authority SHALL expose `POST /connect/token` anonymously, accept `application/x-www-form-urlencoded`, and support exactly the enabled `authorization_code`, `client_credentials`, `refresh_token`, and `urn:ietf:params:oauth:grant-type:device_code` grant types.
2. AN Access Token SHALL expire after 60 minutes by default; an ID Token SHALL expire after 5 minutes by default; both durations SHALL be configurable to whole minutes from 1 through 1440.
3. A Refresh Token SHALL expire after 30 days by default; its duration SHALL be configurable from 1 through 365 days.
4. THE Authorization Code and Device Authorization grants SHALL issue an Access Token, ID Token, and rotating Refresh Token; Client Credentials SHALL issue an Access Token and SHALL NOT issue an ID Token or Refresh Token.
5. EACH Access Token SHALL contain issuer, audience, subject, Principal Type, case-sensitive Scopes, issued-at, not-before, expiry, unique token identifier, and configured tenant/resource context claims.
6. EACH ID Token SHALL contain issuer, audience equal to the OAuth Client identifier, User subject, issued-at, expiry, and nonce when one was supplied.
7. WHEN a Refresh Token is redeemed, THE Security Authority SHALL revoke that token, issue one successor Refresh Token, link the old token to the successor, and atomically commit the rotation.
8. IF a rotated Refresh Token is replayed, THEN THE Security Authority SHALL revoke the active successor lineage for that User and OAuth Client and SHALL return `invalid_grant`.
9. A Client Credentials request SHALL require an active Confidential OAuth Client, a valid client secret, and requested Scopes allowed for that client; otherwise it SHALL return `invalid_client` or `invalid_scope` and issue no token.
10. THE token endpoint SHALL return protocol errors using the OAuth `error` field and optional `error_description`, with HTTP 400 for `invalid_request`, `invalid_grant`, `unsupported_grant_type`, `invalid_scope`, `authorization_pending`, `slow_down`, `access_denied`, and `expired_token`, and HTTP 401 for `invalid_client`.

### Requirement 8: Device Authorization

**User Story:** As a Device User, I want to approve a device from another browser, so that a CLI can obtain credentials without collecting my password.

**Realizes:** UJ-4

#### Acceptance Criteria

1. WHEN Device Authorization is enabled, THE Security Authority SHALL expose `POST /connect/device/authorize` anonymously with required `client_id` and optional `scope`.
2. A successful response SHALL contain a high-entropy Device Code, an eight-character User Code rendered as two groups of four characters, verification URI, verification URI containing the User Code, `expires_in=900`, and `interval=5`.
3. THE authority SHALL use an unambiguous uppercase User Code alphabet that excludes vowels and visually ambiguous characters.
4. THE Security Authority SHALL expose authenticated `GET /connect/device?userCode={value}` to return the pending OAuth Client name, requested Scopes, expiry, and current status, and SHALL expose authenticated `POST /connect/device/approve` to approve or deny the pending Device Grant.
5. BEFORE approval, token polling SHALL return `authorization_pending`; polling less than 5 seconds after the prior poll SHALL return `slow_down`.
6. AFTER denial, polling SHALL return `access_denied`; after 15 minutes, polling SHALL return `expired_token`.
7. AFTER approval, the first valid poll SHALL issue credentials and mark the Device Grant redeemed; every later poll SHALL return `invalid_grant`.
8. AN inactive User, inactive OAuth Client, unknown User Code, unknown Device Code, client mismatch, or disallowed Scope SHALL issue no credential and SHALL return the matching validation or protocol error.

### Requirement 9: SSO Session, UserInfo, and Logout

**User Story:** As an Interactive User, I want session reuse, identity claims, and logout, so that browser sign-in has a complete lifecycle.

**Realizes:** UJ-3

#### Acceptance Criteria

1. THE Security Authority SHALL represent a browser SSO Session with an opaque cookie containing no User data, token, Scope, or provider secret.
2. THE SSO cookie SHALL be HttpOnly, Secure outside Development, SameSite=Lax, restricted to the authority host, and expire after 8 hours by default; the lifetime SHALL be configurable from 5 minutes through 30 days.
3. THE server-side SSO Session SHALL record User, issue time, expiry time, and revocation time and SHALL be rejected when expired, revoked, or linked to a User that is not Active.
4. THE Security Authority SHALL expose authenticated `GET /connect/userinfo` and return `sub`, `name`, `email`, optional `picture`, Principal Type, and configured contextual claims for the authenticated User.
5. THE Security Authority SHALL expose anonymous `GET /connect/logout` with optional `client_id` and `post_logout_redirect_uri`, revoke the current SSO Session, remove its cookie, and redirect only to an exact registered post-logout URI for the supplied active OAuth Client.
6. IF the post-logout URI is absent or invalid, THEN THE authority SHALL redirect to `/` and SHALL NOT redirect to an unregistered external location.
7. LOGOUT SHALL NOT implicitly revoke Access Tokens unless token revocation is explicitly requested through an authorized management operation.

### Requirement 10: External Identity Provider Brokering

**User Story:** As an Application Administrator, I want reusable external provider configuration, so that Users can authenticate through product-selected providers.

**Realizes:** UJ-3, UJ-5

#### Acceptance Criteria

1. THE Security Authority SHALL support Generic OIDC plus configuration presets for Entra External ID, Entra ID, Google, Auth0, and Keycloak.
2. A preset SHALL provide default discovery and claim-mapping conventions but SHALL NOT hard-code tenant identifiers, client identifiers, client secrets, authority URLs, or redirect hosts.
3. THE administrator SHALL configure provider identifier, provider type, display name, authority URL, optional issuer, client identifier, client secret, requested Scopes, active state, priority, optional Tenant Resource, and Invite Only or Open SSO access mode.
4. INVITE ONLY SHALL admit only an External Identity already linked to an invited or existing User; Open SSO SHALL permit first successful sign-in to create a New User linked to that External Identity.
5. THE issuer-and-subject pair SHALL link to at most one User, and one callback SHALL NOT merge two existing Users.
6. IF provider discovery, token exchange, signature validation, issuer validation, nonce validation, or required claim extraction fails, THEN THE authority SHALL reject the callback, create no SSO Session or Authorization Code, and redirect with an OIDC error to the registered client redirect URI when that URI was previously validated.
7. IF an Identity Provider is inactive, THEN new authorization requests SHALL NOT select it, while existing User and External Identity records SHALL remain retained.

### Requirement 11: Administrator Bootstrap and User Lifecycle

**User Story:** As a Module Consumer, I want an explicit bootstrap strategy, so that the first administrator is established according to the host's governance model.

**Realizes:** UJ-1, UJ-3, UJ-5

#### Acceptance Criteria

1. THE Module Consumer SHALL configure exactly one Bootstrap Strategy: Explicit Identity, First Eligible User, or Custom Seed Function.
2. FOR Explicit Identity, THE Module Consumer SHALL configure one allowed issuer-and-subject pair or one normalized email address; only the matching authenticated User SHALL become the initial administrator.
3. FOR First Eligible User, THE first Active User to complete bootstrap SHALL become the initial administrator through an atomic compare-and-set; simultaneous later attempts SHALL receive a conflict and SHALL NOT receive administrator access.
4. FOR Custom Seed Function, THE Host Application SHALL supply a function that returns the initial administrator User and initial Grants; startup SHALL fail if the function is absent or returns an unknown, inactive, suspended, archived, or deleted User.
5. AFTER initial bootstrap commits, THE public bootstrap operation SHALL be permanently closed unless an Application Administrator explicitly resets bootstrap state through a protected maintenance operation.
6. THE User lifecycle SHALL permit New to Active, Active to Suspended, Suspended to Active, and New, Active, or Suspended to Archived or Deleted; Archived and Deleted SHALL be terminal in the MVP.
7. A User that is not Active SHALL be denied new SSO Sessions, Authorization Codes, Device Grant approvals, Access Tokens, ID Tokens, Refresh Token exchanges, and API Key authentication.
8. DELETING or archiving a User SHALL revoke active SSO Sessions, Refresh Tokens, API Keys, Role Memberships, and direct Grants for that User without deleting immutable audit history.

### Requirement 12: Services and API Keys

**User Story:** As an Application Administrator, I want Service and API Key lifecycle management, so that non-human callers can authenticate and be revoked.

**Realizes:** UJ-4, UJ-5, UJ-6

#### Acceptance Criteria

1. AN administrator SHALL be able to create, read, list, update, activate, deactivate, and delete Services and to assign or remove their Roles and Grants.
2. A Service name SHALL be unique within its Tenant, and an idempotent provisioning request with the same Tenant and name SHALL return the existing Service identifier.
3. DEACTIVATING or deleting a Service SHALL revoke its active API Keys and Refresh Tokens and SHALL cause subsequent Access Token validation requiring live-principal validation to fail.
4. AN administrator SHALL be able to create an API Key for exactly one active User or Service, with a name, optional Tenant, optional expiry, and zero or more direct Scopes.
5. A generated API Key SHALL contain a configurable prefix followed by at least 32 bytes of cryptographic randomness encoded without whitespace; the clear API Key SHALL be returned exactly once at creation or regeneration.
6. THE stored API Key hash SHALL use a keyed one-way hash and constant-time comparison; the clear API Key SHALL never be recoverable from authority state.
7. REGENERATING an API Key SHALL revoke the previous value and return one new clear value atomically.
8. AN expired, revoked, unknown, owner-inactive, or hash-mismatched API Key SHALL be rejected as a Caller Credential and SHALL NOT update last-used time.
9. A successful API Key authentication SHALL update last-used time and produce a Principal containing only the owner's effective allowed Scopes plus direct API Key Grants, minus effective Deny Grants.

### Requirement 13: Resource, Role, Grant, and Authorization Administration

**User Story:** As an Application Administrator, I want resource-aware Roles and Grants, so that access can follow the Host Application's hierarchy.

**Realizes:** UJ-5, UJ-6

#### Acceptance Criteria

1. THE Security Authority SHALL register Tenant Resources from management APIs or Integration Events and SHALL treat the Tenant Adapter as authoritative for current parentage and Tenant ownership.
2. A Role SHALL be defined at exactly one Tenant Resource and SHALL be assignable to Users and Services through Role Memberships.
3. A Grant SHALL target exactly one User, Service, Role, or API Key and one Tenant Resource and SHALL carry one Permission Key, effect, applicability, optional expiry, and optional reason.
4. FOR a requested Tenant Resource, THE authorization decision SHALL evaluate direct and inherited Grants from that resource toward its ancestors until the root or an inheritance-protected resource is reached.
5. AN unexpired, unrevoked Deny Grant SHALL override an Allow Grant for the same Permission Key and evaluated Tenant Resource.
6. A Role Membership SHALL contribute that Role's effective Grants only while the Role is enabled and the membership is unexpired and unrevoked.
7. A Grant, Role Membership, Role state, Service state, User state, API Key state, or Tenant Resource parent change SHALL invalidate affected authorization results before the next request is authorized.
8. THE authority SHALL reject parent cycles, cross-Tenant ancestry, Role Memberships outside the Role's Tenant boundary, and Grants whose Principal or Tenant Resource is unknown.
9. THE authority SHALL expose a Grant Catalog listing known Permission Keys and their descriptions without granting access by itself.

### Requirement 14: Management API Contract

**User Story:** As an Application Administrator, I want protected management endpoints, so that I can operate the authority without direct persistence access.

**Realizes:** UJ-5

#### Acceptance Criteria

1. WHEN management APIs are enabled, THE Security Authority SHALL expose version `v1` JSON endpoints for Users, Services, API Keys, OAuth Clients, Identity Providers, Tenant Resources, Roles, Role Memberships, Grants, Grant Catalog, security summary, and bootstrap maintenance.
2. EVERY management endpoint SHALL require an authenticated Principal and one documented management Scope specific to its resource and action.
3. LIST endpoints SHALL accept `pageNumber` from 1 upward and `pageSize` from 1 through 100, default `pageNumber=1` and `pageSize=25`, and return total count, page number, page size, and items in a deterministic order.
4. CREATE operations SHALL return HTTP 201 with the created representation or identifier; successful reads and updates SHALL return HTTP 200; successful deletion with no body SHALL return HTTP 204.
5. A missing record SHALL return HTTP 404; invalid input SHALL return HTTP 400 with field-addressed RFC 9457 Problem Details; missing authentication SHALL return HTTP 401; insufficient Scope SHALL return HTTP 403; uniqueness or stale-concurrency conflicts SHALL return HTTP 409.
6. CREATE, update, state-transition, regeneration, and provisioning operations SHALL accept an optional idempotency key from 1 to 200 characters; repeating the same key with the same request within 24 hours SHALL return the original outcome, while repeating it with a different request SHALL return HTTP 409.
7. UPDATE and state-transition operations SHALL require a concurrency token; a stale token SHALL return HTTP 409 and SHALL NOT overwrite committed state.
8. READ and list responses SHALL never contain secret hashes, encrypted secret values, clear credentials, private keys, or protected correlation state.
9. THE management surface SHALL include the reference parity operations for activation, suspension, enablement, disablement, revocation, regeneration, provisioning, Role membership, Grant assignment, Grant removal, and security summary.

### Requirement 15: Integration Events, Lifecycle Notifications, and Auditing

**User Story:** As a Platform Integrator, I want optional synchronization and observability contracts, so that the authority participates in a distributed platform without requiring a specific product.

**Realizes:** UJ-5, UJ-6

#### Acceptance Criteria

1. WHEN Integration Events are enabled, THE Security Authority SHALL accept versioned registration and lifecycle events for Users, Services, OAuth Clients, Identity Providers, Tenant Resources, Roles, Role Memberships, and Grants.
2. EACH Integration Event SHALL contain a globally unique event identifier, event type, schema version, occurrence time, correlation identifier, source, and resource payload.
3. THE authority SHALL process each event identifier at most once; replaying the same event SHALL return or record success without duplicating records, memberships, Grants, or lifecycle transitions.
4. IF an event has an unsupported version, missing required field, unknown reference, stale concurrency token, or tenant mismatch, THEN THE authority SHALL reject it with the event identifier and exact reason and SHALL leave authority state unchanged.
5. WHEN a Lifecycle Notification adapter is configured, THE authority SHALL emit one notification after commit for each created, updated, activated, deactivated, suspended, archived, deleted, revoked, regenerated, approved, denied, redeemed, or bootstrap-completed transition.
6. WHEN an Audit Adapter is configured, THE authority SHALL submit actor Principal, action, target type and identifier, Tenant when present, correlation identifier, timestamp, outcome, and non-secret changed-field names for every management, bootstrap, credential, provider, and authorization-administration mutation.
7. IF no event or Audit Adapter is configured, THEN the authority SHALL remain operational and SHALL NOT report the optional integration as successfully delivered.
8. IF an optional adapter fails after the authority transaction commits, THEN THE authority SHALL surface the delivery failure through the Host Application's configured failure mechanism and SHALL NOT roll back or falsely repeat the committed security mutation.

### Requirement 16: Failure Safety, Concurrency, and Data Lifecycle

**User Story:** As a Module Consumer, I want deterministic failure and lifecycle behavior, so that security state cannot be partially applied or silently lost.

**Realizes:** UJ-1, UJ-2, UJ-3, UJ-4, UJ-5, UJ-6

#### Acceptance Criteria

1. TOKEN redemption, Refresh Token rotation, API Key regeneration, first-administrator bootstrap, idempotent provisioning, and event deduplication SHALL be atomic operations.
2. IF any validation, authorization, persistence, cryptographic, or Tenant Adapter step fails before commit, THEN THE Security Authority SHALL leave all affected security records unchanged and SHALL issue no credential.
3. THE authority SHALL use optimistic concurrency for mutable User, Service, OAuth Client, Identity Provider, API Key, Tenant Resource registration, Role, Role Membership, Grant, and bootstrap records.
4. THE authority SHALL retain revoked credential metadata and terminal lifecycle timestamps for at least 30 days by default, configurable from 1 through 3650 days, while never retaining clear redeemable secrets.
5. THE authority SHALL remove expired Authorization Codes and Device Grants no earlier than 24 hours and no later than 7 days after expiry, and expired SSO Sessions and Refresh Tokens no earlier than 30 days and no later than 90 days after expiry or revocation.
6. IF a cleanup operation runs concurrently with credential validation or redemption, THEN exactly one operation SHALL succeed according to committed expiry, revocation, redemption, and concurrency state.
7. ALL timestamps SHALL be UTC instants, all identifiers SHALL remain stable after creation, and all comparisons of client identifiers, issuer values, subjects, Scopes, Permission Keys, and redirect URIs SHALL use the case rules stated in this document.
8. THE generated conformance test kit SHALL cover every acceptance criterion in R4-R10 and the atomicity, idempotency, concurrency, and secret-exclusion rules in R12-R16.

## Assumptions

- The first release uses API version `v1` for management APIs and unversioned standard `/connect/*` and `/.well-known/*` protocol routes.
- Default credential lifetimes are Access Token 60 minutes, ID Token 5 minutes, Authorization Code 5 minutes, Device Grant 15 minutes, Refresh Token 30 days, and SSO Session 8 hours.
- Device polling defaults to 5 seconds, and token validation permits at most 60 seconds of clock skew.
- HTTPS is mandatory outside Development.
- Administrator-configured OAuth Client Scopes are pre-authorized because a consent UI is outside MVP scope.
