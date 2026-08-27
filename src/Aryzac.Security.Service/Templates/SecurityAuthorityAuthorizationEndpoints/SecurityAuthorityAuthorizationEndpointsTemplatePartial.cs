using System;
using System.Collections.Generic;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Security.Service.Templates.SecurityAuthorityAuthorizationEndpoints
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityAuthorizationEndpointsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityAuthorizationEndpoints";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityAuthorizationEndpointsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Security.Cryptography")
                .AddUsing("System.Text.Json")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityContracts")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityCryptography")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityDiscoveryEndpoints")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityExternalProviders")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityRecords")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthoritySessionEndpoints")
                .AddUsing("Microsoft.AspNetCore.Builder")
                .AddUsing("Microsoft.AspNetCore.Http")
                .AddUsing("Microsoft.AspNetCore.Routing")
                .AddClass("SecurityAuthorityAuthorizationConfiguration", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("string", "AuthorizationPath");
                    @class.AddProperty("string", "CallbackPath");
                    @class.AddProperty("int", "StateLifetimeMinutes");
                    @class.AddProperty("int", "AuthorizationCodeLifetimeMinutes");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement("AuthorizationPath = \"/connect/authorize\";");
                        ctor.AddStatement("CallbackPath = \"/connect/callback\";");
                        ctor.AddStatement("StateLifetimeMinutes = 10;");
                        ctor.AddStatement("AuthorizationCodeLifetimeMinutes = 5;");
                    });
                })
                .AddRecord("SecurityAuthorityAuthorizationState", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "OAuthClientId");
                        ctor.AddParameter("string", "ClientIdentifier");
                        ctor.AddParameter("string", "RedirectUri");
                        ctor.AddParameter("IReadOnlyList<string>", "Scopes");
                        ctor.AddParameter("string", "PkceChallenge");
                        ctor.AddParameter("string", "PkceChallengeMethod");
                        ctor.AddParameter("string?", "Nonce");
                        ctor.AddParameter("string?", "ReturnState");
                        ctor.AddParameter("string", "IdentityProviderId");
                        ctor.AddParameter("string", "ProviderNonce");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("DateTimeOffset?", "ConsumedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddClass("SecurityAuthorityAuthorizationEndpoints", @class =>
                {
                    @class.Static();
                    @class.AddMethod("void", "Map", method =>
                    {
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("SecurityAuthorityAuthorizationConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySessionConfiguration", "sessionConfiguration");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("SecurityAuthoritySecretProtector", "stateProtector");
                        method.AddParameter("SecurityAuthoritySecretProtector", "cookieProtector");
                        method.AddParameter("SecurityAuthoritySecretProtector", "providerSecretProtector");
                        method.AddParameter("SecurityAuthorityCredentialHasher", "credentialHasher");
                        method.AddParameter("ISecurityAuthorityExternalProviderProtocol", "externalProviderProtocol");
                        method.AddParameter("Func<string, CancellationToken, ValueTask<SecurityAuthorityOAuthClient?>>", "findClientByIdentifier");
                        method.AddParameter("Func<string?, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityIdentityProvider>>>", "findIdentityProviders");
                        method.AddParameter("Func<SecurityAuthorityIdentityProvider, string, string, string, CancellationToken, ValueTask<string>>", "buildProviderRedirect");
                        method.AddParameter("Func<string, string, CancellationToken, ValueTask<SecurityAuthorityExternalIdentity?>>", "findExternalIdentity");
                        method.AddParameter("Func<string, CancellationToken, ValueTask<SecurityAuthorityUser?>>", "findUser");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("bool", "isDevelopment");
                        method.AddStatement("ValidateConfiguration(configuration);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(sessionConfiguration);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(records);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(stateProtector);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(cookieProtector);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(providerSecretProtector);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(credentialHasher);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(externalProviderProtocol);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findClientByIdentifier);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findIdentityProviders);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(buildProviderRedirect);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findExternalIdentity);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findUser);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                        method.AddStatement("""
                            endpoints.MapGet(configuration.AuthorizationPath, async (HttpContext context, string? client_id, string? redirect_uri, string? response_type, string? scope, string? state, string? nonce, string? code_challenge, string? code_challenge_method, CancellationToken cancellationToken) =>
                            {
                            if (string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(redirect_uri) || string.IsNullOrWhiteSpace(response_type))
                            {
                            return OAuthError("invalid_request", "client_id, redirect_uri, and response_type are required.");
                            }

                            var client = await findClientByIdentifier(client_id, cancellationToken);
                            if (client is null)
                            {
                            return OAuthError("unauthorized_client", "The OAuth Client is unknown.");
                            }

                            var clientValidation = SecurityAuthorityOAuthClientValidation.ValidateAuthorizationClient(client, redirect_uri);
                            if (!clientValidation.IsValid)
                            {
                            return OAuthError(clientValidation.Error!, clientValidation.ErrorDescription!);
                            }

                            if (!string.Equals(response_type, "code", StringComparison.Ordinal))
                            {
                            return Results.Redirect(BuildErrorRedirect(redirect_uri, "unsupported_response_type", "Only response_type=code is supported.", state));
                            }

                            if (client.AllowedGrantTypes is null || !client.AllowedGrantTypes.Contains("authorization_code", StringComparer.Ordinal))
                            {
                            return Results.Redirect(BuildErrorRedirect(redirect_uri, "unauthorized_client", "The client is not registered for Authorization Code issuance.", state));
                            }

                            var scopes = ParseScopes(scope);
                            if (scopes is null || scopes.Any(requested => client.AllowedScopes is null || !client.AllowedScopes.Contains(requested, StringComparer.Ordinal)))
                            {
                            return Results.Redirect(BuildErrorRedirect(redirect_uri, "invalid_scope", "A requested Scope is unknown or not allowed for this client.", state));
                            }

                            if (string.IsNullOrWhiteSpace(code_challenge) || !string.Equals(code_challenge_method, "S256", StringComparison.Ordinal))
                            {
                            return Results.Redirect(BuildErrorRedirect(redirect_uri, "invalid_request", "Authorization Code requests must use S256 PKCE.", state));
                            }

                            var providers = await findIdentityProviders(client.TenantId, cancellationToken);
                            var provider = SecurityAuthorityExternalProviders.SelectProvider(providers, client.PreferredIdentityProviderId, client.TenantId);
                            if (provider is null)
                            {
                            return Results.Redirect(BuildErrorRedirect(redirect_uri, "temporarily_unavailable", "No active Identity Provider is eligible for this client.", state));
                            }

                            var now = utcNow();
                            var providerNonce = NewOpaqueIdentifier();
                            var authorizationState = new SecurityAuthorityAuthorizationState(
                            NewOpaqueIdentifier(), client.Id, client.ClientIdentifier, redirect_uri, scopes, code_challenge, "S256", nonce, state,
                            provider.Id, providerNonce, now, now.AddMinutes(configuration.StateLifetimeMinutes), null, NewOpaqueIdentifier());
                            var protectedState = stateProtector.Protect(JsonSerializer.Serialize(authorizationState));
                            var callbackUri = BuildAbsoluteCallbackUri(context.Request, configuration.CallbackPath);
                            var providerRedirect = await buildProviderRedirect(provider, callbackUri, protectedState, providerNonce, cancellationToken);
                            if (!Uri.TryCreate(providerRedirect, UriKind.Absolute, out var providerRedirectUri) || !providerRedirectUri.IsAbsoluteUri)
                            {
                            throw new InvalidOperationException("The external provider authorization redirect must be an absolute URI.");
                            }

                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            await operation.Records.AddAsync(authorizationState, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }

                            return Results.Redirect(providerRedirect);
                            }).AllowAnonymous();
                            """);
                        method.AddStatement("""
                            endpoints.MapMethods(configuration.CallbackPath, new[] { HttpMethods.Get, HttpMethods.Post }, async (HttpContext context, CancellationToken cancellationToken) =>
                            {
                            SecurityAuthorityExternalProviderCallback callback;
                            try
                            {
                            callback = await SecurityAuthorityExternalProviders.ReadCallbackAsync(context.Request, cancellationToken);
                            }
                            catch (InvalidOperationException exception)
                            {
                            return OAuthError("invalid_request", exception.Message);
                            }

                            if (string.IsNullOrWhiteSpace(callback.State))
                            {
                            return OAuthError("invalid_request", "The external provider callback state is required.");
                            }

                            SecurityAuthorityAuthorizationState protectedState;
                            try
                            {
                            protectedState = JsonSerializer.Deserialize<SecurityAuthorityAuthorizationState>(stateProtector.Unprotect(callback.State))
                            ?? throw new CryptographicException("The protected authorization state is empty.");
                            }
                            catch (Exception exception) when (exception is CryptographicException or JsonException)
                            {
                            return OAuthError("invalid_request", "The protected authorization state is invalid.");
                            }

                            var now = utcNow();
                            if (protectedState.ExpiresAt <= now || protectedState.ConsumedAt is not null)
                            {
                            return OAuthError("invalid_request", "The authorization state is expired or already consumed.");
                            }

                            var client = await findClientByIdentifier(protectedState.ClientIdentifier, cancellationToken);
                            if (client is null || !string.Equals(client.Id, protectedState.OAuthClientId, StringComparison.Ordinal))
                            {
                            return OAuthError("unauthorized_client", "The OAuth Client bound to the authorization state is unavailable.");
                            }

                            var clientValidation = SecurityAuthorityOAuthClientValidation.ValidateAuthorizationClient(client, protectedState.RedirectUri);
                            if (!clientValidation.IsValid)
                            {
                            return OAuthError(clientValidation.Error!, clientValidation.ErrorDescription!);
                            }

                            if (protectedState.Scopes.Any(requested => client.AllowedScopes is null || !client.AllowedScopes.Contains(requested, StringComparer.Ordinal)))
                            {
                            return Results.Redirect(BuildErrorRedirect(protectedState.RedirectUri, "invalid_scope", "The authorization state contains a Scope no longer allowed for this client.", protectedState.ReturnState));
                            }

                            var providers = await findIdentityProviders(client.TenantId, cancellationToken);
                            var provider = providers.FirstOrDefault(candidate => string.Equals(candidate.Id, protectedState.IdentityProviderId, StringComparison.Ordinal));
                            if (provider is null)
                            {
                            return Results.Redirect(BuildErrorRedirect(protectedState.RedirectUri, "access_denied", "The selected Identity Provider is unavailable.", protectedState.ReturnState));
                            }

                            var existingSession = await SecurityAuthoritySessionEndpoints.ResolveAsync(context.Request, sessionConfiguration, records, cookieProtector, now, cancellationToken);
                            var callbackResult = await SecurityAuthorityExternalProviders.ProcessCallbackAsync(
                            provider, callback, BuildAbsoluteCallbackUri(context.Request, configuration.CallbackPath), protectedState.ProviderNonce, true,
                            protectedState.ReturnState, existingSession?.User.Id, externalProviderProtocol, providerSecretProtector, persistence,
                            findExternalIdentity, findUser, utcNow, cancellationToken);
                            if (!callbackResult.Succeeded || callbackResult.User is null)
                            {
                            return callbackResult.SafeErrorRedirectUri is not null
                            ? Results.Redirect(callbackResult.SafeErrorRedirectUri)
                            : OAuthError(callbackResult.Error ?? "access_denied", "External Identity Provider authentication failed.");
                            }

                            if (!string.Equals(callbackResult.User.Status, "Active", StringComparison.Ordinal))
                            {
                            return Results.Redirect(BuildErrorRedirect(protectedState.RedirectUri, "access_denied", "The authenticated User is not Active.", protectedState.ReturnState));
                            }

                            now = utcNow();
                            SecurityAuthoritySsoSession session;
                            string code;
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            var persistedState = await operation.Records.LoadAsync(typeof(SecurityAuthorityAuthorizationState), protectedState.Id, cancellationToken) as SecurityAuthorityAuthorizationState;
                            if (persistedState is null || persistedState.ConsumedAt is not null || persistedState.ExpiresAt <= now || !StateMatches(protectedState, persistedState))
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return Results.Redirect(BuildErrorRedirect(protectedState.RedirectUri, "invalid_request", "The authorization state is expired, invalid, or already consumed.", protectedState.ReturnState));
                            }

                            var consumedState = persistedState with { ConsumedAt = now, ConcurrencyToken = NewOpaqueIdentifier() };
                            await operation.Records.UpdateAsync(consumedState, persistedState.ConcurrencyToken, cancellationToken);
                            session = SecurityAuthoritySessionEndpoints.CreateSession(callbackResult.User.Id, sessionConfiguration, now);
                            await operation.Records.AddAsync(session, cancellationToken);
                            var clearCode = NewOpaqueIdentifier();
                            var deferredCode = new SecurityAuthorityDeferredCredential(operation.OperationId, () => clearCode);
                            var authorizationCode = new SecurityAuthorityAuthorizationCode(
                            NewOpaqueIdentifier(), credentialHasher.HashCredential(clearCode), client.Id, callbackResult.User.Id,
                            protectedState.RedirectUri, protectedState.Scopes, protectedState.PkceChallenge, protectedState.Nonce,
                            now, now.AddMinutes(configuration.AuthorizationCodeLifetimeMinutes), null, NewOpaqueIdentifier());
                            await operation.Records.AddAsync(authorizationCode, cancellationToken);
                            var receipt = await operation.CommitAsync(cancellationToken);
                            code = deferredCode.Reveal(receipt);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }

                            SecurityAuthoritySessionEndpoints.IssueCookie(context.Response, sessionConfiguration, cookieProtector, session, now, isDevelopment);
                            return Results.Redirect(BuildSuccessRedirect(protectedState.RedirectUri, code, protectedState.ReturnState));
                            }).AllowAnonymous();
                            """);
                    });
                    @class.AddMethod("string[]?", "ParseScopes", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string?", "scope");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(scope)) return Array.Empty<string>();");
                        method.AddStatement("var values = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);");
                        method.AddStatement("return values.Length == values.Distinct(StringComparer.Ordinal).Count() ? values : null;");
                    });
                    @class.AddMethod("bool", "StateMatches", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityAuthorizationState", "protectedState");
                        method.AddParameter("SecurityAuthorityAuthorizationState", "persistedState");
                        method.AddStatement("""
                            return string.Equals(protectedState.Id, persistedState.Id, StringComparison.Ordinal)
                            && string.Equals(protectedState.OAuthClientId, persistedState.OAuthClientId, StringComparison.Ordinal)
                            && string.Equals(protectedState.ClientIdentifier, persistedState.ClientIdentifier, StringComparison.Ordinal)
                            && string.Equals(protectedState.RedirectUri, persistedState.RedirectUri, StringComparison.Ordinal)
                            && protectedState.Scopes.SequenceEqual(persistedState.Scopes, StringComparer.Ordinal)
                            && string.Equals(protectedState.PkceChallenge, persistedState.PkceChallenge, StringComparison.Ordinal)
                            && string.Equals(protectedState.PkceChallengeMethod, persistedState.PkceChallengeMethod, StringComparison.Ordinal)
                            && string.Equals(protectedState.Nonce, persistedState.Nonce, StringComparison.Ordinal)
                            && string.Equals(protectedState.ReturnState, persistedState.ReturnState, StringComparison.Ordinal)
                            && string.Equals(protectedState.IdentityProviderId, persistedState.IdentityProviderId, StringComparison.Ordinal)
                            && string.Equals(protectedState.ProviderNonce, persistedState.ProviderNonce, StringComparison.Ordinal)
                            && protectedState.CreatedAt == persistedState.CreatedAt
                            && protectedState.ExpiresAt == persistedState.ExpiresAt
                            && string.Equals(protectedState.ConcurrencyToken, persistedState.ConcurrencyToken, StringComparison.Ordinal);
                            """);
                    });
                    @class.AddMethod("string", "BuildAbsoluteCallbackUri", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("HttpRequest", "request");
                        method.AddParameter("string", "callbackPath");
                        method.AddStatement("return $\"{request.Scheme}://{request.Host}{request.PathBase}{callbackPath}\";");
                    });
                    @class.AddMethod("IResult", "OAuthError", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "error");
                        method.AddParameter("string", "description");
                        method.AddStatement("return Results.BadRequest(new { error, error_description = description });");
                    });
                    @class.AddMethod("string", "BuildErrorRedirect", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "redirectUri");
                        method.AddParameter("string", "error");
                        method.AddParameter("string", "description");
                        method.AddParameter("string?", "returnState");
                        method.AddStatement("var separator = redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';");
                        method.AddStatement("var result = $\"{redirectUri}{separator}error={Uri.EscapeDataString(error)}&error_description={Uri.EscapeDataString(description)}\";");
                        method.AddStatement("return string.IsNullOrWhiteSpace(returnState) ? result : $\"{result}&state={Uri.EscapeDataString(returnState)}\";");
                    });
                    @class.AddMethod("string", "BuildSuccessRedirect", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "redirectUri");
                        method.AddParameter("string", "code");
                        method.AddParameter("string?", "returnState");
                        method.AddStatement("var separator = redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';");
                        method.AddStatement("var result = $\"{redirectUri}{separator}code={Uri.EscapeDataString(code)}\";");
                        method.AddStatement("return string.IsNullOrWhiteSpace(returnState) ? result : $\"{result}&state={Uri.EscapeDataString(returnState)}\";");
                    });
                    @class.AddMethod("void", "ValidateConfiguration", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityAuthorizationConfiguration", "configuration");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(configuration);");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(configuration.AuthorizationPath) || string.IsNullOrWhiteSpace(configuration.CallbackPath)) throw new InvalidOperationException(\"Security Authority authorization endpoint paths cannot be empty.\");");
                        method.AddStatement("if (configuration.StateLifetimeMinutes != 10) throw new InvalidOperationException(\"Security Authority authorization state must expire after ten minutes.\");");
                        method.AddStatement("if (configuration.AuthorizationCodeLifetimeMinutes != 5) throw new InvalidOperationException(\"Security Authority Authorization Codes must expire after five minutes.\");");
                    });
                    @class.AddMethod("string", "NewOpaqueIdentifier", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddStatement("return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();");
                    });
                });
        }

        [IntentManaged(Mode.Fully)]
        public CSharpFile CSharpFile { get; }

        [IntentManaged(Mode.Fully)]
        protected override CSharpFileConfig DefineFileConfig()
        {
            return CSharpFile.GetConfig();
        }

        [IntentManaged(Mode.Fully)]
        public override string TransformText()
        {
            return CSharpFile.ToString();
        }
    }
}
