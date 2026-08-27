using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aryzac.Security.Service.Settings;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Security.Service.Templates.SecurityAuthorityTokenEndpoint
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityTokenEndpointTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityTokenEndpoint";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityTokenEndpointTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            static Exception Friendly(string message)
            {
                var exceptionType = Type.GetType("Intent.Exceptions.FriendlyException, Intent.SoftwareFactory.SDK");
                return exceptionType is not null && Activator.CreateInstance(exceptionType, message) is Exception exception
                    ? exception
                    : new InvalidOperationException(message);
            }

            static int ParsePositive(string value, string settingName)
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result <= 0)
                {
                    throw Friendly($"{settingName} must be configured as a positive whole number.");
                }

                return result;
            }

            static string Escape(string value)
            {
                return (value ?? string.Empty)
                    .Replace("\\", "\\\\", StringComparison.Ordinal)
                    .Replace("\"", "\\\"", StringComparison.Ordinal);
            }

            var protocol = ExecutionContext.Settings.GetAuthorityProtocol();
            var features = ExecutionContext.Settings.GetAuthorityFeatures();
            var routes = ExecutionContext.Settings.GetAuthorityRoutes();
            var tenancy = ExecutionContext.Settings.GetAuthorityTenancy();
            var cryptography = ExecutionContext.Settings.GetAuthorityCryptography();
            var accessTokenMinutes = ParsePositive(protocol.AccessTokenMinutes(), "Authority Protocol: Access Token Minutes");
            var idTokenMinutes = ParsePositive(protocol.IDTokenMinutes(), "Authority Protocol: ID Token Minutes");
            var refreshTokenDays = ParsePositive(protocol.RefreshTokenDays(), "Authority Protocol: Refresh Token Days");
            var contextualClaimNames = (tenancy.ContextualClaimNames() ?? string.Empty)
                .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var contextualClaimNamesExpression = contextualClaimNames.Length == 0
                ? "Array.Empty<string>()"
                : $"new[] {{ {string.Join(", ", contextualClaimNames.Select(value => $"\"{Escape(value)}\""))} }}";

            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Security.Cryptography")
                .AddUsing("System.Text")
                .AddUsing("System.Text.Json")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityContracts")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityCryptography")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityDiscoveryEndpoints")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityRecords")
                .AddUsing("Microsoft.AspNetCore.Builder")
                .AddUsing("Microsoft.AspNetCore.Http")
                .AddUsing("Microsoft.AspNetCore.Routing")
                .AddClass("SecurityAuthorityTokenConfiguration", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("string", "TokenPath");
                    @class.AddProperty("string", "Issuer");
                    @class.AddProperty("string", "SigningKeyId");
                    @class.AddProperty("int", "AccessTokenMinutes");
                    @class.AddProperty("int", "IdTokenMinutes");
                    @class.AddProperty("int", "RefreshTokenDays");
                    @class.AddProperty("bool", "AuthorizationCodeEnabled");
                    @class.AddProperty("bool", "ClientCredentialsEnabled");
                    @class.AddProperty("bool", "RefreshTokenEnabled");
                    @class.AddProperty("bool", "DeviceCodeEnabled");
                    @class.AddProperty("IReadOnlyList<string>", "ContextualClaimNames");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement($"TokenPath = \"{Escape(routes.TokenRoute())}\";");
                        ctor.AddStatement($"Issuer = \"{Escape(protocol.Issuer())}\";");
                        ctor.AddStatement($"SigningKeyId = \"{Escape(cryptography.ActiveSigningKeyId())}\";");
                        ctor.AddStatement($"AccessTokenMinutes = {accessTokenMinutes.ToString(CultureInfo.InvariantCulture)};");
                        ctor.AddStatement($"IdTokenMinutes = {idTokenMinutes.ToString(CultureInfo.InvariantCulture)};");
                        ctor.AddStatement($"RefreshTokenDays = {refreshTokenDays.ToString(CultureInfo.InvariantCulture)};");
                        ctor.AddStatement($"AuthorizationCodeEnabled = {features.AuthorizationCode().ToString().ToLowerInvariant()};");
                        ctor.AddStatement($"ClientCredentialsEnabled = {features.ClientCredentials().ToString().ToLowerInvariant()};");
                        ctor.AddStatement($"RefreshTokenEnabled = {features.RefreshToken().ToString().ToLowerInvariant()};");
                        ctor.AddStatement($"DeviceCodeEnabled = {features.DeviceAuthorization().ToString().ToLowerInvariant()};");
                        ctor.AddStatement($"ContextualClaimNames = {contextualClaimNamesExpression};");
                    });
                })
                .AddClass("SecurityAuthorityTokenEndpoint", @class =>
                {
                    @class.Static();
                    @class.AddMethod("void", "Map", method =>
                    {
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("SecurityAuthorityCredentialHasher", "credentialHasher");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("Func<string, CancellationToken, ValueTask<SecurityAuthorityOAuthClient?>>", "findClientByIdentifier");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<SecurityAuthorityAuthorizationCode?>>", "findAuthorizationCode");
                        method.AddParameter("Func<string, string, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyDictionary<string, string>>>", "resolveContextClaims");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<SecurityAuthorityRefreshToken?>>", "findRefreshToken");
                        method.AddParameter("Func<Exception, bool>", "isConcurrencyConflict");
                        method.AddParameter("Func<HttpContext, IFormCollection, SecurityAuthorityOAuthClient, CancellationToken, ValueTask<IResult>>", "redeemDeviceCode");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddStatement("ValidateConfiguration(configuration);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(credentialHasher);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(signingKeys);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findClientByIdentifier);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findAuthorizationCode);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(resolveContextClaims);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findRefreshToken);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(isConcurrencyConflict);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(redeemDeviceCode);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                        method.AddStatement("""
                            endpoints.MapPost(configuration.TokenPath, async (HttpContext context, CancellationToken cancellationToken) =>
                            {
                            var mediaType = context.Request.ContentType?.Split(';', StringSplitOptions.TrimEntries).FirstOrDefault();
                            if (!string.Equals(mediaType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                            {
                            return OAuthError(context, "invalid_request", "The token endpoint accepts only application/x-www-form-urlencoded requests.");
                            }

                            var form = await context.Request.ReadFormAsync(cancellationToken);
                            var grantType = form["grant_type"].ToString();
                            if (string.IsNullOrWhiteSpace(grantType))
                            {
                            return OAuthError(context, "invalid_request", "grant_type is required.");
                            }

                            if (!TryReadClientIdentifier(context.Request, form, out var clientIdentifier))
                            {
                            return OAuthError(context, "invalid_client", "Client authentication failed.", StatusCodes.Status401Unauthorized);
                            }

                            var client = await findClientByIdentifier(clientIdentifier!, cancellationToken);
                            if (client is null)
                            {
                            return OAuthError(context, "invalid_client", "Client authentication failed.", StatusCodes.Status401Unauthorized);
                            }

                            var authentication = SecurityAuthorityOAuthClientValidation.AuthenticateForToken(
                            client, context.Request.Headers.Authorization.ToString(), form["client_id"].ToString(), form["client_secret"].ToString(), credentialHasher);
                            if (!authentication.IsValid)
                            {
                            return OAuthError(context, authentication.Error!, authentication.ErrorDescription!, StatusCodes.Status401Unauthorized);
                            }

                            return grantType switch
                            {
                            "authorization_code" when configuration.AuthorizationCodeEnabled => await RedeemAuthorizationCodeAsync(
                            context, form, configuration, persistence, credentialHasher, signingKeys, client, findAuthorizationCode, resolveContextClaims, utcNow, cancellationToken),
                            "client_credentials" when configuration.ClientCredentialsEnabled => await IssueClientCredentialsAsync(
                            context, form, configuration, signingKeys, client, resolveContextClaims, utcNow, cancellationToken),
                            "refresh_token" when configuration.RefreshTokenEnabled => await RedeemRefreshTokenAsync(
                            context, form, configuration, persistence, credentialHasher, signingKeys, client, findRefreshToken, resolveContextClaims, isConcurrencyConflict, utcNow, cancellationToken),
                            "urn:ietf:params:oauth:grant-type:device_code" when configuration.DeviceCodeEnabled => await redeemDeviceCode(context, form, client, cancellationToken),
                            "authorization_code" or "client_credentials" or "refresh_token" or "urn:ietf:params:oauth:grant-type:device_code" => OAuthError(context, "unsupported_grant_type", "The requested grant type is disabled."),
                            _ => OAuthError(context, "unsupported_grant_type", "The requested grant type is not supported.")
                            };
                            }).AllowAnonymous();
                            """);
                    });
                    @class.AddMethod("ValueTask<IResult>", "RedeemAuthorizationCodeAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("HttpContext", "context");
                        method.AddParameter("IFormCollection", "form");
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("SecurityAuthorityCredentialHasher", "credentialHasher");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<SecurityAuthorityAuthorizationCode?>>", "findAuthorizationCode");
                        method.AddParameter("Func<string, string, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyDictionary<string, string>>>", "resolveContextClaims");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var clearCode = form[\"code\"].ToString();");
                        method.AddStatement("var redirectUri = form[\"redirect_uri\"].ToString();");
                        method.AddStatement("var codeVerifier = form[\"code_verifier\"].ToString();");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(clearCode) || string.IsNullOrWhiteSpace(redirectUri) || string.IsNullOrWhiteSpace(codeVerifier)) return OAuthError(context, \"invalid_request\", \"code, redirect_uri, and code_verifier are required.\");");
                        method.AddStatement("var now = utcNow();");
                        method.AddStatement("await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.TokenRedemption, true, cancellationToken);");
                        method.AddStatement("SecurityAuthorityAuthorizationCode redeemedCode;");
                        method.AddStatement("SecurityAuthorityDeferredCredential? deferredRefreshToken = null;");
                        method.AddStatement("SecurityAuthorityCommitReceipt receipt;");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var code = await findAuthorizationCode(operation.Records, clearCode, cancellationToken);");
                        method.AddStatement("    var invalid = code is null || !credentialHasher.VerifyCredential(clearCode, code.CodeHash) || code.ExpiresAt <= now || code.RedeemedAt is not null || !string.Equals(code.OAuthClientId, client.Id, StringComparison.Ordinal) || !string.Equals(code.RedirectUri, redirectUri, StringComparison.Ordinal);");
                        method.AddStatement("    if (!invalid)");
                        method.AddStatement("    {");
                        method.AddStatement("        var validation = SecurityAuthorityOAuthClientValidation.ValidateAuthorizationCodeRedemption(client, redirectUri, codeVerifier, code!.PkceChallenge, \"S256\");");
                        method.AddStatement("        invalid = !validation.IsValid;");
                        method.AddStatement("    }");
                        method.AddStatement("    if (invalid)");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        return OAuthError(context, \"invalid_grant\", \"The Authorization Code is invalid, expired, or already redeemed.\");");
                        method.AddStatement("    }");
                        method.AddStatement("    redeemedCode = code! with { RedeemedAt = now, ConcurrencyToken = NewOpaqueIdentifier() };");
                        method.AddStatement("    await operation.Records.UpdateAsync(redeemedCode, code.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("    var clearRefreshToken = NewOpaqueIdentifier();");
                        method.AddStatement("    deferredRefreshToken = new SecurityAuthorityDeferredCredential(operation.OperationId, () => clearRefreshToken);");
                        method.AddStatement("    await operation.Records.AddAsync(new SecurityAuthorityRefreshToken(NewOpaqueIdentifier(), credentialHasher.HashCredential(clearRefreshToken), client.Id, code.UserId, now, now.AddDays(configuration.RefreshTokenDays), null, null, null, NewOpaqueIdentifier()), cancellationToken);");
                        method.AddStatement("    receipt = await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                        method.AddStatement("var contextClaims = await ResolveContextClaimsAsync(configuration, resolveContextClaims, \"User\", redeemedCode.UserId, redeemedCode.Scopes, cancellationToken);");
                        method.AddStatement("var accessToken = CreateAccessToken(configuration, signingKeys, redeemedCode.UserId, \"User\", client.ClientIdentifier, redeemedCode.Scopes, contextClaims, now);");
                        method.AddStatement("var idToken = CreateIdToken(configuration, signingKeys, client.ClientIdentifier, redeemedCode.UserId, redeemedCode.Nonce, now);");
                        method.AddStatement("var refreshToken = deferredRefreshToken!.Reveal(receipt);");
                        method.AddStatement("return TokenResponse(accessToken, configuration.AccessTokenMinutes * 60, redeemedCode.Scopes, idToken, refreshToken);");
                    });
                    @class.AddMethod("ValueTask<IResult>", "RedeemRefreshTokenAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("HttpContext", "context");
                        method.AddParameter("IFormCollection", "form");
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("SecurityAuthorityCredentialHasher", "credentialHasher");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<SecurityAuthorityRefreshToken?>>", "findRefreshToken");
                        method.AddParameter("Func<string, string, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyDictionary<string, string>>>", "resolveContextClaims");
                        method.AddParameter("Func<Exception, bool>", "isConcurrencyConflict");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("if (!client.IsActive || !client.AllowedGrantTypes.Contains(\"refresh_token\", StringComparer.Ordinal)) return OAuthError(context, \"unauthorized_client\", \"The client is not permitted to use Refresh Token redemption.\");");
                        method.AddStatement("var clearRefreshToken = form[\"refresh_token\"].ToString();");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(clearRefreshToken)) return OAuthError(context, \"invalid_request\", \"refresh_token is required.\");");
                        method.AddStatement("var scopes = ParseScopes(form[\"scope\"].ToString());");
                        method.AddStatement("if (scopes is null || scopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))) return OAuthError(context, \"invalid_scope\", \"A requested Scope is unknown or not allowed for this client.\");");
                        method.AddStatement("var now = utcNow();");
                        method.AddStatement("await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.RefreshTokenRotation, true, cancellationToken);");
                        method.AddStatement("SecurityAuthorityRefreshToken rotatedToken;");
                        method.AddStatement("SecurityAuthorityDeferredCredential deferredSuccessor;");
                        method.AddStatement("SecurityAuthorityCommitReceipt receipt;");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var token = await findRefreshToken(operation.Records, clearRefreshToken, cancellationToken);");
                        method.AddStatement("    if (token is null || !credentialHasher.VerifyCredential(clearRefreshToken, token.TokenHash) || !string.Equals(token.OAuthClientId, client.Id, StringComparison.Ordinal))");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        return OAuthError(context, \"invalid_grant\", \"The Refresh Token is invalid, expired, revoked, or does not belong to this client.\");");
                        method.AddStatement("    }");
                        method.AddStatement("    if (!string.IsNullOrWhiteSpace(token.ReplacedByTokenId))");
                        method.AddStatement("    {");
                        method.AddStatement("        await RevokeActiveSuccessorLineageAsync(operation.Records, token, now, cancellationToken);");
                        method.AddStatement("        await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("        return OAuthError(context, \"invalid_grant\", \"The rotated Refresh Token was replayed and its active successor lineage was revoked.\");");
                        method.AddStatement("    }");
                        method.AddStatement("    if (token.ExpiresAt <= now || token.RevokedAt is not null || token.LastUsedAt is not null)");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        return OAuthError(context, \"invalid_grant\", \"The Refresh Token is invalid, expired, revoked, or already redeemed.\");");
                        method.AddStatement("    }");
                        method.AddStatement("    var user = await operation.Records.LoadAsync(typeof(SecurityAuthorityUser), token.UserId, cancellationToken) as SecurityAuthorityUser;");
                        method.AddStatement("    if (user is null || !string.Equals(user.Status, \"Active\", StringComparison.Ordinal))");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        return OAuthError(context, \"invalid_grant\", \"The Refresh Token owner is not active.\");");
                        method.AddStatement("    }");
                        method.AddStatement("    var successorClearToken = NewOpaqueIdentifier();");
                        method.AddStatement("    var successorToken = new SecurityAuthorityRefreshToken(NewOpaqueIdentifier(), credentialHasher.HashCredential(successorClearToken), client.Id, token.UserId, now, now.AddDays(configuration.RefreshTokenDays), null, null, null, NewOpaqueIdentifier());");
                        method.AddStatement("    rotatedToken = token with { LastUsedAt = now, ReplacedByTokenId = successorToken.Id, RevokedAt = now, ConcurrencyToken = NewOpaqueIdentifier() };");
                        method.AddStatement("    deferredSuccessor = new SecurityAuthorityDeferredCredential(operation.OperationId, () => successorClearToken);");
                        method.AddStatement("    await operation.Records.UpdateAsync(rotatedToken, token.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("    await operation.Records.AddAsync(successorToken, cancellationToken);");
                        method.AddStatement("    receipt = await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("catch (Exception exception) when (isConcurrencyConflict(exception))");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    await using var replayOperation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.RefreshTokenRotation, true, cancellationToken);");
                        method.AddStatement("    try");
                        method.AddStatement("    {");
                        method.AddStatement("        var replayedToken = await findRefreshToken(replayOperation.Records, clearRefreshToken, cancellationToken);");
                        method.AddStatement("        if (replayedToken is null || !credentialHasher.VerifyCredential(clearRefreshToken, replayedToken.TokenHash) || !string.Equals(replayedToken.OAuthClientId, client.Id, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(replayedToken.ReplacedByTokenId))");
                        method.AddStatement("        {");
                        method.AddStatement("            await replayOperation.RollbackAsync(cancellationToken);");
                        method.AddStatement("            return OAuthError(context, \"invalid_grant\", \"The Refresh Token was concurrently redeemed.\");");
                        method.AddStatement("        }");
                        method.AddStatement("        await RevokeActiveSuccessorLineageAsync(replayOperation.Records, replayedToken, now, cancellationToken);");
                        method.AddStatement("        await replayOperation.CommitAsync(cancellationToken);");
                        method.AddStatement("    }");
                        method.AddStatement("    catch");
                        method.AddStatement("    {");
                        method.AddStatement("        await replayOperation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        throw;");
                        method.AddStatement("    }");
                        method.AddStatement("    return OAuthError(context, \"invalid_grant\", \"The Refresh Token was concurrently redeemed and its active successor lineage was revoked.\");");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                        method.AddStatement("var contextClaims = await ResolveContextClaimsAsync(configuration, resolveContextClaims, \"User\", rotatedToken.UserId, scopes, cancellationToken);");
                        method.AddStatement("var accessToken = CreateAccessToken(configuration, signingKeys, rotatedToken.UserId, \"User\", client.ClientIdentifier, scopes, contextClaims, now);");
                        method.AddStatement("var successorRefreshToken = deferredSuccessor.Reveal(receipt);");
                        method.AddStatement("return TokenResponse(accessToken, configuration.AccessTokenMinutes * 60, scopes, null, successorRefreshToken);");
                    });
                    @class.AddMethod("ValueTask", "RevokeActiveSuccessorLineageAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("SecurityAuthorityRefreshToken", "replayedToken");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var visited = new HashSet<string>(StringComparer.Ordinal);");
                        method.AddStatement("var successorId = replayedToken.ReplacedByTokenId;");
                        method.AddStatement("while (!string.IsNullOrWhiteSpace(successorId))");
                        method.AddStatement("{");
                        method.AddStatement("    if (!visited.Add(successorId)) throw new InvalidOperationException(\"The Refresh Token successor lineage contains a cycle.\");");
                        method.AddStatement("    var successor = await records.LoadAsync(typeof(SecurityAuthorityRefreshToken), successorId, cancellationToken) as SecurityAuthorityRefreshToken ?? throw new InvalidOperationException(\"A linked Refresh Token successor could not be loaded.\");");
                        method.AddStatement("    if (!string.Equals(successor.UserId, replayedToken.UserId, StringComparison.Ordinal) || !string.Equals(successor.OAuthClientId, replayedToken.OAuthClientId, StringComparison.Ordinal)) throw new InvalidOperationException(\"A Refresh Token successor must belong to the same User and OAuth Client.\");");
                        method.AddStatement("    if (successor.RevokedAt is null && successor.ExpiresAt > now)");
                        method.AddStatement("    {");
                        method.AddStatement("        var revokedSuccessor = successor with { RevokedAt = now, ConcurrencyToken = NewOpaqueIdentifier() };");
                        method.AddStatement("        await records.UpdateAsync(revokedSuccessor, successor.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("    }");
                        method.AddStatement("    successorId = successor.ReplacedByTokenId;");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask<IResult>", "IssueClientCredentialsAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("HttpContext", "context");
                        method.AddParameter("IFormCollection", "form");
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("Func<string, string, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyDictionary<string, string>>>", "resolveContextClaims");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("if (!string.Equals(client.ClientType, \"Confidential\", StringComparison.Ordinal) || !client.IsActive) return OAuthError(context, \"invalid_client\", \"Client Credentials requires an active Confidential client.\", StatusCodes.Status401Unauthorized);");
                        method.AddStatement("if (!client.AllowedGrantTypes.Contains(\"client_credentials\", StringComparer.Ordinal)) return OAuthError(context, \"invalid_scope\", \"The client is not configured for Client Credentials.\");");
                        method.AddStatement("var scopes = ParseScopes(form[\"scope\"].ToString());");
                        method.AddStatement("if (scopes is null || scopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))) return OAuthError(context, \"invalid_scope\", \"A requested Scope is unknown or not allowed for this client.\");");
                        method.AddStatement("var now = utcNow();");
                        method.AddStatement("var contextClaims = await ResolveContextClaimsAsync(configuration, resolveContextClaims, \"Service\", client.Id, scopes, cancellationToken);");
                        method.AddStatement("var accessToken = CreateAccessToken(configuration, signingKeys, client.Id, \"Service\", client.ClientIdentifier, scopes, contextClaims, now);");
                        method.AddStatement("return TokenResponse(accessToken, configuration.AccessTokenMinutes * 60, scopes, null, null);");
                    });
                    @class.AddMethod("ValueTask<IReadOnlyDictionary<string, string>>", "ResolveContextClaimsAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddParameter("Func<string, string, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyDictionary<string, string>>>", "resolveContextClaims");
                        method.AddParameter("string", "principalType");
                        method.AddParameter("string", "principalId");
                        method.AddParameter("IReadOnlyList<string>", "scopes");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var resolved = await resolveContextClaims(principalType, principalId, scopes, cancellationToken);");
                        method.AddStatement("return resolved.Where(claim => configuration.ContextualClaimNames.Contains(claim.Key, StringComparer.Ordinal)).ToDictionary(claim => claim.Key, claim => claim.Value, StringComparer.Ordinal);");
                    });
                    @class.AddMethod("string", "CreateAccessToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("string", "subject");
                        method.AddParameter("string", "principalType");
                        method.AddParameter("string", "audience");
                        method.AddParameter("IReadOnlyList<string>", "scopes");
                        method.AddParameter("IReadOnlyDictionary<string, string>", "contextClaims");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("var expiresAt = now.AddMinutes(configuration.AccessTokenMinutes);");
                        method.AddStatement("var claims = new Dictionary<string, object>(StringComparer.Ordinal)");
                        method.AddStatement("{");
                        method.AddStatement("    [\"iss\"] = configuration.Issuer,");
                        method.AddStatement("    [\"aud\"] = audience,");
                        method.AddStatement("    [\"sub\"] = subject,");
                        method.AddStatement("    [\"principal_type\"] = principalType,");
                        method.AddStatement("    [\"scope\"] = string.Join(' ', scopes),");
                        method.AddStatement("    [\"iat\"] = now.ToUnixTimeSeconds(),");
                        method.AddStatement("    [\"nbf\"] = now.ToUnixTimeSeconds(),");
                        method.AddStatement("    [\"exp\"] = expiresAt.ToUnixTimeSeconds(),");
                        method.AddStatement("    [\"jti\"] = NewOpaqueIdentifier()");
                        method.AddStatement("};");
                        method.AddStatement("foreach (var claim in contextClaims)");
                        method.AddStatement("{");
                        method.AddStatement("    if (!claims.ContainsKey(claim.Key)) claims.Add(claim.Key, claim.Value);");
                        method.AddStatement("}");
                        method.AddStatement("return SignToken(configuration, signingKeys, claims, now, expiresAt);");
                    });
                    @class.AddMethod("string", "CreateIdToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("string", "clientAudience");
                        method.AddParameter("string", "userSubject");
                        method.AddParameter("string?", "nonce");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("var expiresAt = now.AddMinutes(configuration.IdTokenMinutes);");
                        method.AddStatement("var claims = new Dictionary<string, object>(StringComparer.Ordinal) { [\"iss\"] = configuration.Issuer, [\"aud\"] = clientAudience, [\"sub\"] = userSubject, [\"iat\"] = now.ToUnixTimeSeconds(), [\"exp\"] = expiresAt.ToUnixTimeSeconds() };");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(nonce)) claims.Add(\"nonce\", nonce);");
                        method.AddStatement("return SignToken(configuration, signingKeys, claims, now, expiresAt);");
                    });
                    @class.AddMethod("string", "SignToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("IReadOnlyDictionary<string, object>", "claims");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("DateTimeOffset", "expiresAt");
                        method.AddStatement("var header = SecurityAuthorityBase64Url.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<string, object>(StringComparer.Ordinal) { [\"alg\"] = \"RS256\", [\"kid\"] = configuration.SigningKeyId, [\"typ\"] = \"JWT\" })));");
                        method.AddStatement("var payload = SecurityAuthorityBase64Url.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(claims)));");
                        method.AddStatement("return signingKeys.SignToken(header, payload, now, expiresAt);");
                    });
                    @class.AddMethod("IResult", "TokenResponse", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "accessToken");
                        method.AddParameter("int", "expiresIn");
                        method.AddParameter("IReadOnlyList<string>", "scopes");
                        method.AddParameter("string?", "idToken");
                        method.AddParameter("string?", "refreshToken");
                        method.AddStatement("var response = new Dictionary<string, object>(StringComparer.Ordinal) { [\"access_token\"] = accessToken, [\"token_type\"] = \"Bearer\", [\"expires_in\"] = expiresIn, [\"scope\"] = string.Join(' ', scopes) };");
                        method.AddStatement("if (idToken is not null) response.Add(\"id_token\", idToken);");
                        method.AddStatement("if (refreshToken is not null) response.Add(\"refresh_token\", refreshToken);");
                        method.AddStatement("return Results.Json(response);");
                    });
                    @class.AddMethod("IResult", "OAuthError", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("HttpContext", "context");
                        method.AddParameter("string", "error");
                        method.AddParameter("string", "description");
                        method.AddParameter("int", "statusCode", parameter => parameter.WithDefaultValue("StatusCodes.Status400BadRequest"));
                        method.AddStatement("if (statusCode == StatusCodes.Status401Unauthorized) context.Response.Headers.WWWAuthenticate = \"Basic realm=\\\"token\\\"\";");
                        method.AddStatement("return Results.Json(new { error, error_description = description }, statusCode: statusCode);");
                    });
                    @class.AddMethod("bool", "TryReadClientIdentifier", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("HttpRequest", "request");
                        method.AddParameter("IFormCollection", "form");
                        method.AddParameter("string?", "clientIdentifier", parameter => parameter.WithOutParameterModifier());
                        method.AddStatement("clientIdentifier = null;");
                        method.AddStatement("var authorization = request.Headers.Authorization.ToString();");
                        method.AddStatement("var postIdentifier = form[\"client_id\"].ToString();");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(authorization) && !string.IsNullOrWhiteSpace(postIdentifier)) return false;");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(authorization)) { clientIdentifier = postIdentifier; return !string.IsNullOrWhiteSpace(clientIdentifier); }");
                        method.AddStatement("if (!authorization.StartsWith(\"Basic \", StringComparison.OrdinalIgnoreCase)) return false;");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorization[6..].Trim()));");
                        method.AddStatement("    var separator = decoded.IndexOf(':');");
                        method.AddStatement("    if (separator <= 0) return false;");
                        method.AddStatement("    clientIdentifier = decoded[..separator];");
                        method.AddStatement("    return !string.IsNullOrWhiteSpace(clientIdentifier);");
                        method.AddStatement("}");
                        method.AddStatement("catch (FormatException)");
                        method.AddStatement("{");
                        method.AddStatement("    return false;");
                        method.AddStatement("}");
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
                    @class.AddMethod("void", "ValidateConfiguration", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityTokenConfiguration", "configuration");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(configuration);");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(configuration.TokenPath)) throw new InvalidOperationException(\"The Security Authority token path cannot be empty.\");");
                        method.AddStatement("if (!Uri.TryCreate(configuration.Issuer, UriKind.Absolute, out var issuer) || !issuer.IsAbsoluteUri) throw new InvalidOperationException(\"The Security Authority issuer must be an absolute URI.\");");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(configuration.SigningKeyId)) throw new InvalidOperationException(\"The Security Authority signing key id cannot be empty.\");");
                        method.AddStatement("if (configuration.AccessTokenMinutes <= 0 || configuration.IdTokenMinutes <= 0 || configuration.RefreshTokenDays <= 0) throw new InvalidOperationException(\"Security Authority token durations must be positive.\");");
                        method.AddStatement("if (configuration.ContextualClaimNames.Any(string.IsNullOrWhiteSpace) || configuration.ContextualClaimNames.Distinct(StringComparer.Ordinal).Count() != configuration.ContextualClaimNames.Count) throw new InvalidOperationException(\"Configured contextual claim names must be non-empty and unique using ordinal comparison.\");");
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
