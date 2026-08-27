using System;
using System.Collections.Generic;
using Aryzac.Security.Service.Templates.SecurityAuthorityContracts;
using Aryzac.Security.Service.Templates.SecurityAuthorityCryptography;
using Aryzac.Security.Service.Templates.SecurityAuthorityRecords;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Security.Service.Templates.SecurityAuthorityExternalProviders
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityExternalProvidersTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityExternalProviders";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityExternalProvidersTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            AddTypeSource(SecurityAuthorityContractsTemplate.TemplateId);
            AddTypeSource(SecurityAuthorityCryptographyTemplate.TemplateId);
            AddTypeSource(SecurityAuthorityRecordsTemplate.TemplateId);

            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Net.Http")
                .AddUsing("System.Security.Cryptography")
                .AddUsing("System.Text")
                .AddUsing("System.Text.Json")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Microsoft.AspNetCore.Http")
                .AddRecord("SecurityAuthorityExternalProviderPreset", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "ProviderType");
                        ctor.AddParameter("IReadOnlyList<string>", "DefaultScopes");
                        ctor.AddParameter("string", "SubjectClaim");
                        ctor.AddParameter("string", "DisplayNameClaim");
                        ctor.AddParameter("string", "EmailClaim");
                        ctor.AddParameter("string", "AvatarClaim");
                    });
                })
                .AddRecord("SecurityAuthorityExternalProviderCallback", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string?", "Code");
                        ctor.AddParameter("string?", "State");
                        ctor.AddParameter("string?", "Error");
                        ctor.AddParameter("string?", "ErrorDescription");
                    });
                })
                .AddRecord("SecurityAuthorityExternalProviderAuthenticationRequest", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityIdentityProvider", "Provider");
                        ctor.AddParameter("SecurityAuthorityExternalProviderPreset", "Preset");
                        ctor.AddParameter("SecurityAuthorityExternalProviderCallback", "Callback");
                        ctor.AddParameter("string", "RedirectUri");
                        ctor.AddParameter("string", "ExpectedNonce");
                    });
                })
                .AddRecord("SecurityAuthorityExternalProviderAuthenticationResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("bool", "DiscoveryValidated");
                        ctor.AddParameter("bool", "TokenExchangeSucceeded");
                        ctor.AddParameter("bool", "SignatureValidated");
                        ctor.AddParameter("bool", "IssuerValidated");
                        ctor.AddParameter("bool", "AudienceValidated");
                        ctor.AddParameter("bool", "NonceValidated");
                        ctor.AddParameter("bool", "RequiredClaimsValidated");
                        ctor.AddParameter("string?", "Issuer");
                        ctor.AddParameter("string?", "Subject");
                        ctor.AddParameter("string?", "DisplayName");
                        ctor.AddParameter("string?", "NormalizedEmail");
                        ctor.AddParameter("string?", "AvatarUrl");
                        ctor.AddParameter("string?", "Error");
                    });
                })
                .AddRecord("SecurityAuthorityExternalProviderCallbackResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("bool", "Succeeded");
                        ctor.AddParameter("SecurityAuthorityUser?", "User");
                        ctor.AddParameter("SecurityAuthorityExternalIdentity?", "ExternalIdentity");
                        ctor.AddParameter("string?", "Error");
                        ctor.AddParameter("string?", "SafeErrorRedirectUri");
                    });
                })
                .AddInterface("ISecurityAuthorityExternalProviderProtocol", @interface =>
                {
                    @interface.AddMethod("ValueTask<SecurityAuthorityExternalProviderAuthenticationResult>", "AuthenticateAsync", method =>
                    {
                        method.AddParameter("SecurityAuthorityExternalProviderAuthenticationRequest", "request");
                        method.AddParameter("SecurityAuthoritySecretProtector", "secretProtector");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddClass("SecurityAuthorityOidcExternalProviderProtocol", @class =>
                {
                    @class.Sealed();
                    @class.ImplementsInterface("ISecurityAuthorityExternalProviderProtocol");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("HttpClient", "httpClient", param => param.IntroduceReadonlyField());
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(httpClient);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityExternalProviderAuthenticationResult>", "AuthenticateAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("SecurityAuthorityExternalProviderAuthenticationRequest", "request");
                        method.AddParameter("SecurityAuthoritySecretProtector", "secretProtector");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(request);
                            ArgumentNullException.ThrowIfNull(secretProtector);
                            if (string.IsNullOrWhiteSpace(request.Callback.Code)) throw new InvalidOperationException("An authorization code is required.");
                            if (!Uri.TryCreate(request.Provider.AuthorityUrl, UriKind.Absolute, out var authority) || authority.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("The external provider authority must be an absolute HTTPS URI.");
                            if (!Uri.TryCreate(request.RedirectUri, UriKind.Absolute, out _)) throw new InvalidOperationException("The external provider redirect URI must be absolute.");

                            var discoveryUri = new Uri(authority.AbsoluteUri.TrimEnd('/') + "/.well-known/openid-configuration", UriKind.Absolute);
                            using var discoveryResponse = await _httpClient.GetAsync(discoveryUri, cancellationToken);
                            discoveryResponse.EnsureSuccessStatusCode();
                            using var discovery = JsonDocument.Parse(await discoveryResponse.Content.ReadAsStreamAsync(cancellationToken));
                            var issuer = RequiredString(discovery.RootElement, "issuer");
                            var tokenEndpoint = RequiredHttpsUri(discovery.RootElement, "token_endpoint");
                            var jwksUri = RequiredHttpsUri(discovery.RootElement, "jwks_uri");
                            if (!string.IsNullOrWhiteSpace(request.Provider.Issuer) && !string.Equals(request.Provider.Issuer, issuer, StringComparison.Ordinal)) throw new CryptographicException("The discovered issuer does not match the configured issuer.");

                            var clientSecret = secretProtector.Unprotect(request.Provider.EncryptedClientSecret);
                            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                            {
                            Content = new FormUrlEncodedContent(new Dictionary<string, string>
                            {
                            ["grant_type"] = "authorization_code",
                            ["code"] = request.Callback.Code,
                            ["redirect_uri"] = request.RedirectUri,
                            ["client_id"] = request.Provider.ClientIdentifier,
                            ["client_secret"] = clientSecret
                            })
                            };
                            using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
                            tokenResponse.EnsureSuccessStatusCode();
                            using var tokens = JsonDocument.Parse(await tokenResponse.Content.ReadAsStreamAsync(cancellationToken));
                            var idToken = RequiredString(tokens.RootElement, "id_token");
                            var parts = idToken.Split('.');
                            if (parts.Length != 3) throw new CryptographicException("The external provider ID Token format is invalid.");
                            using var header = JsonDocument.Parse(SecurityAuthorityBase64Url.Decode(parts[0]));
                            using var payload = JsonDocument.Parse(SecurityAuthorityBase64Url.Decode(parts[1]));
                            if (!string.Equals(RequiredString(header.RootElement, "alg"), "RS256", StringComparison.Ordinal)) throw new CryptographicException("The external provider ID Token must use RS256.");
                            var keyId = RequiredString(header.RootElement, "kid");

                            using var jwksResponse = await _httpClient.GetAsync(jwksUri, cancellationToken);
                            jwksResponse.EnsureSuccessStatusCode();
                            using var jwks = JsonDocument.Parse(await jwksResponse.Content.ReadAsStreamAsync(cancellationToken));
                            var key = jwks.RootElement.GetProperty("keys").EnumerateArray().FirstOrDefault(candidate => string.Equals(OptionalString(candidate, "kid"), keyId, StringComparison.Ordinal));
                            if (key.ValueKind != JsonValueKind.Object || !string.Equals(OptionalString(key, "kty"), "RSA", StringComparison.Ordinal)) throw new CryptographicException("The external provider signing key is unavailable.");
                            using var rsa = RSA.Create();
                            rsa.ImportParameters(new RSAParameters { Modulus = SecurityAuthorityBase64Url.Decode(RequiredString(key, "n")), Exponent = SecurityAuthorityBase64Url.Decode(RequiredString(key, "e")) });
                            var signedBytes = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
                            if (!rsa.VerifyData(signedBytes, SecurityAuthorityBase64Url.Decode(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) throw new CryptographicException("The external provider ID Token signature is invalid.");

                            var tokenIssuer = RequiredString(payload.RootElement, "iss");
                            if (!string.Equals(tokenIssuer, issuer, StringComparison.Ordinal)) throw new CryptographicException("The external provider ID Token issuer is invalid.");
                            if (!ContainsAudience(payload.RootElement, request.Provider.ClientIdentifier)) throw new CryptographicException("The external provider ID Token audience is invalid.");
                            if (!string.Equals(RequiredString(payload.RootElement, "nonce"), request.ExpectedNonce, StringComparison.Ordinal)) throw new CryptographicException("The external provider ID Token nonce is invalid.");
                            var subject = RequiredString(payload.RootElement, request.Preset.SubjectClaim);
                            var displayName = RequiredString(payload.RootElement, request.Preset.DisplayNameClaim);
                            var email = RequiredString(payload.RootElement, request.Preset.EmailClaim).Trim().ToUpperInvariant();
                            var avatar = OptionalString(payload.RootElement, request.Preset.AvatarClaim);
                            return new SecurityAuthorityExternalProviderAuthenticationResult(true, true, true, true, true, true, true, issuer, subject, displayName, email, avatar, null);
                            """);
                    });
                    @class.AddMethod("Uri", "RequiredHttpsUri", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("JsonElement", "element");
                        method.AddParameter("string", "propertyName");
                        method.AddStatement("var value = RequiredString(element, propertyName);");
                        method.AddStatement("return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : throw new InvalidOperationException($\"External provider discovery property '{propertyName}' must be an absolute HTTPS URI.\");");
                    });
                    @class.AddMethod("string", "RequiredString", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("JsonElement", "element");
                        method.AddParameter("string", "propertyName");
                        method.AddStatement("return OptionalString(element, propertyName) ?? throw new InvalidOperationException($\"External provider data is missing '{propertyName}'.\");");
                    });
                    @class.AddMethod("string?", "OptionalString", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("JsonElement", "element");
                        method.AddParameter("string", "propertyName");
                        method.AddStatement("return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;");
                    });
                    @class.AddMethod("bool", "ContainsAudience", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("JsonElement", "payload");
                        method.AddParameter("string", "clientIdentifier");
                        method.AddStatement("if (!payload.TryGetProperty(\"aud\", out var audience)) return false;");
                        method.AddStatement("if (audience.ValueKind == JsonValueKind.String) return string.Equals(audience.GetString(), clientIdentifier, StringComparison.Ordinal);");
                        method.AddStatement("return audience.ValueKind == JsonValueKind.Array && audience.EnumerateArray().Any(value => value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), clientIdentifier, StringComparison.Ordinal));");
                    });
                })
                .AddClass("SecurityAuthorityExternalProviders", @class =>
                {
                    @class.Static();
                    @class.AddMethod("SecurityAuthorityExternalProviderPreset", "GetPreset", method =>
                    {
                        method.Static();
                        method.AddParameter("string", "providerType");
                        method.AddStatement("return providerType switch");
                        method.AddStatement("{");
                        method.AddStatement("    \"GenericOidc\" => new(providerType, new[] { \"openid\", \"profile\", \"email\" }, \"sub\", \"name\", \"email\", \"picture\"),");
                        method.AddStatement("    \"EntraExternalId\" => new(providerType, new[] { \"openid\", \"profile\", \"email\" }, \"sub\", \"name\", \"email\", \"picture\"),");
                        method.AddStatement("    \"EntraId\" => new(providerType, new[] { \"openid\", \"profile\", \"email\" }, \"sub\", \"name\", \"preferred_username\", \"picture\"),");
                        method.AddStatement("    \"Google\" => new(providerType, new[] { \"openid\", \"profile\", \"email\" }, \"sub\", \"name\", \"email\", \"picture\"),");
                        method.AddStatement("    \"Auth0\" => new(providerType, new[] { \"openid\", \"profile\", \"email\" }, \"sub\", \"name\", \"email\", \"picture\"),");
                        method.AddStatement("    \"Keycloak\" => new(providerType, new[] { \"openid\", \"profile\", \"email\" }, \"sub\", \"name\", \"email\", \"picture\"),");
                        method.AddStatement("    _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, \"Unsupported external Identity Provider type.\")");
                        method.AddStatement("};");
                    });
                    @class.AddMethod("SecurityAuthorityIdentityProvider?", "SelectProvider", method =>
                    {
                        method.Static();
                        method.AddParameter("IEnumerable<SecurityAuthorityIdentityProvider>", "providers");
                        method.AddParameter("string?", "preferredProviderId");
                        method.AddParameter("string?", "tenantResourceId");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(providers);");
                        method.AddStatement("var eligible = providers.Where(provider => provider.IsActive && (provider.TenantResourceId is null || string.Equals(provider.TenantResourceId, tenantResourceId, StringComparison.Ordinal))).ToArray();");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(preferredProviderId))");
                        method.AddStatement("{");
                        method.AddStatement("    var preferred = eligible.FirstOrDefault(provider => string.Equals(provider.Id, preferredProviderId, StringComparison.Ordinal) || string.Equals(provider.ProviderIdentifier, preferredProviderId, StringComparison.Ordinal));");
                        method.AddStatement("    if (preferred is not null) return preferred;");
                        method.AddStatement("}");
                        method.AddStatement("return eligible.OrderBy(provider => provider.DisplayPriority).ThenBy(provider => provider.ProviderIdentifier, StringComparer.Ordinal).FirstOrDefault();");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityExternalProviderCallback>", "ReadCallbackAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("HttpRequest", "request");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(request);");
                        method.AddStatement("if (HttpMethods.IsGet(request.Method)) return new SecurityAuthorityExternalProviderCallback(request.Query[\"code\"].FirstOrDefault(), request.Query[\"state\"].FirstOrDefault(), request.Query[\"error\"].FirstOrDefault(), request.Query[\"error_description\"].FirstOrDefault());");
                        method.AddStatement("if (HttpMethods.IsPost(request.Method) && request.HasFormContentType)");
                        method.AddStatement("{");
                        method.AddStatement("    var form = await request.ReadFormAsync(cancellationToken);");
                        method.AddStatement("    return new SecurityAuthorityExternalProviderCallback(form[\"code\"].FirstOrDefault(), form[\"state\"].FirstOrDefault(), form[\"error\"].FirstOrDefault(), form[\"error_description\"].FirstOrDefault());");
                        method.AddStatement("}");
                        method.AddStatement("throw new InvalidOperationException(\"External OIDC callbacks must use GET query or POST form_post.\");");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityExternalProviderCallbackResult>", "ProcessCallbackAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityIdentityProvider", "provider");
                        method.AddParameter("SecurityAuthorityExternalProviderCallback", "callback");
                        method.AddParameter("string", "redirectUri");
                        method.AddParameter("string", "expectedNonce");
                        method.AddParameter("bool", "clientRedirectUriPreviouslyValidated");
                        method.AddParameter("string?", "returnState");
                        method.AddParameter("string?", "existingUserId");
                        method.AddParameter("ISecurityAuthorityExternalProviderProtocol", "protocol");
                        method.AddParameter("SecurityAuthoritySecretProtector", "secretProtector");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("Func<string, string, CancellationToken, ValueTask<SecurityAuthorityExternalIdentity?>>", "findExternalIdentity");
                        method.AddParameter("Func<string, CancellationToken, ValueTask<SecurityAuthorityUser?>>", "findUser");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(provider);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(callback);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(protocol);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(secretProtector);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findExternalIdentity);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findUser);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                        method.AddStatement("if (!provider.IsActive) return Failure(\"access_denied\", \"The selected Identity Provider is inactive.\", clientRedirectUriPreviouslyValidated, redirectUri, returnState);");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(callback.Error)) return Failure(callback.Error, callback.ErrorDescription, clientRedirectUriPreviouslyValidated, redirectUri, returnState);");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(callback.Code) || string.IsNullOrWhiteSpace(callback.State)) return Failure(\"invalid_request\", \"The external provider callback is incomplete.\", clientRedirectUriPreviouslyValidated, redirectUri, returnState);");
                        method.AddStatement("SecurityAuthorityExternalProviderAuthenticationResult authentication;");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var request = new SecurityAuthorityExternalProviderAuthenticationRequest(provider, GetPreset(provider.ProviderType), callback, redirectUri, expectedNonce);");
                        method.AddStatement("    authentication = await protocol.AuthenticateAsync(request, secretProtector, cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("catch (Exception exception) when (exception is HttpRequestException or CryptographicException or InvalidOperationException)");
                        method.AddStatement("{");
                        method.AddStatement("    return Failure(\"invalid_request\", \"External provider authentication failed.\", clientRedirectUriPreviouslyValidated, redirectUri, returnState);");
                        method.AddStatement("}");
                        method.AddStatement("if (!authentication.DiscoveryValidated || !authentication.TokenExchangeSucceeded || !authentication.SignatureValidated || !authentication.IssuerValidated || !authentication.AudienceValidated || !authentication.NonceValidated || !authentication.RequiredClaimsValidated || string.IsNullOrWhiteSpace(authentication.Issuer) || string.IsNullOrWhiteSpace(authentication.Subject) || string.IsNullOrWhiteSpace(authentication.DisplayName) || string.IsNullOrWhiteSpace(authentication.NormalizedEmail))");
                        method.AddStatement("{");
                        method.AddStatement("    return Failure(\"invalid_request\", authentication.Error ?? \"External provider validation failed.\", clientRedirectUriPreviouslyValidated, redirectUri, returnState);");
                        method.AddStatement("}");
                        method.AddStatement("await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var now = utcNow();");
                        method.AddStatement("    var identity = await findExternalIdentity(authentication.Issuer, authentication.Subject, cancellationToken);");
                        method.AddStatement("    SecurityAuthorityUser? user;");
                        method.AddStatement("    if (identity is not null)");
                        method.AddStatement("    {");
                        method.AddStatement("        user = await findUser(identity.UserId, cancellationToken);");
                        method.AddStatement("        if (user is null) throw new InvalidOperationException(\"The External Identity references an unknown User.\");");
                        method.AddStatement("        if (!string.IsNullOrWhiteSpace(existingUserId) && !string.Equals(existingUserId, user.Id, StringComparison.Ordinal)) return await RollbackFailureAsync(operation, \"account_selection_required\", \"The callback cannot merge two existing Users.\", clientRedirectUriPreviouslyValidated, redirectUri, returnState, cancellationToken);");
                        method.AddStatement("        identity = identity with { LastSeenAt = now };");
                        method.AddStatement("    }");
                        method.AddStatement("    else");
                        method.AddStatement("    {");
                        method.AddStatement("        if (string.Equals(provider.AccessMode, \"InviteOnly\", StringComparison.Ordinal)) return await RollbackFailureAsync(operation, \"access_denied\", \"This Identity Provider permits invited or previously linked Users only.\", clientRedirectUriPreviouslyValidated, redirectUri, returnState, cancellationToken);");
                        method.AddStatement("        if (!string.Equals(provider.AccessMode, \"OpenSso\", StringComparison.Ordinal)) return await RollbackFailureAsync(operation, \"invalid_request\", \"The Identity Provider access mode is invalid.\", clientRedirectUriPreviouslyValidated, redirectUri, returnState, cancellationToken);");
                        method.AddStatement("        user = string.IsNullOrWhiteSpace(existingUserId) ? null : await findUser(existingUserId, cancellationToken);");
                        method.AddStatement("        if (!string.IsNullOrWhiteSpace(existingUserId) && user is null) return await RollbackFailureAsync(operation, \"invalid_request\", \"The existing User could not be resolved.\", clientRedirectUriPreviouslyValidated, redirectUri, returnState, cancellationToken);");
                        method.AddStatement("        user ??= new SecurityAuthorityUser(NewOpaqueIdentifier(), authentication.DisplayName, authentication.NormalizedEmail, authentication.AvatarUrl, \"New\", now, now, now, NewOpaqueIdentifier());");
                        method.AddStatement("        if (string.IsNullOrWhiteSpace(existingUserId)) await operation.Records.AddAsync(user, cancellationToken);");
                        method.AddStatement("        identity = new SecurityAuthorityExternalIdentity(NewOpaqueIdentifier(), authentication.Issuer, authentication.Subject, user.Id, now, now);");
                        method.AddStatement("        await operation.Records.AddAsync(identity, cancellationToken);");
                        method.AddStatement("    }");
                        method.AddStatement("    await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("    return new SecurityAuthorityExternalProviderCallbackResult(true, user, identity, null, null);");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityExternalProviderCallbackResult>", "RollbackFailureAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityAtomicOperation", "operation");
                        method.AddParameter("string", "error");
                        method.AddParameter("string?", "description");
                        method.AddParameter("bool", "redirectValidated");
                        method.AddParameter("string", "redirectUri");
                        method.AddParameter("string?", "returnState");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("return Failure(error, description, redirectValidated, redirectUri, returnState);");
                    });
                    @class.AddMethod("SecurityAuthorityExternalProviderCallbackResult", "Failure", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "error");
                        method.AddParameter("string?", "description");
                        method.AddParameter("bool", "redirectValidated");
                        method.AddParameter("string", "redirectUri");
                        method.AddParameter("string?", "returnState");
                        method.AddStatement("return new SecurityAuthorityExternalProviderCallbackResult(false, null, null, error, redirectValidated ? BuildErrorRedirect(redirectUri, error, description, returnState) : null);");
                    });
                    @class.AddMethod("string", "BuildErrorRedirect", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "redirectUri");
                        method.AddParameter("string", "error");
                        method.AddParameter("string?", "description");
                        method.AddParameter("string?", "returnState");
                        method.AddStatement("var separator = redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';");
                        method.AddStatement("var result = $\"{redirectUri}{separator}error={Uri.EscapeDataString(error)}\";");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(description)) result += $\"&error_description={Uri.EscapeDataString(description)}\";");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(returnState)) result += $\"&state={Uri.EscapeDataString(returnState)}\";");
                        method.AddStatement("return result;");
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
