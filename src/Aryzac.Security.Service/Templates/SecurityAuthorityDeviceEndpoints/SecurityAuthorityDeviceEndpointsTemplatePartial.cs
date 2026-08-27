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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityDeviceEndpoints
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityDeviceEndpointsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityDeviceEndpoints";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityDeviceEndpointsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
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
            var cryptography = ExecutionContext.Settings.GetAuthorityCryptography();
            const int pollingIntervalSeconds = 5;
            var accessTokenMinutes = ParsePositive(protocol.AccessTokenMinutes(), "Authority Protocol: Access Token Minutes");
            var idTokenMinutes = ParsePositive(protocol.IDTokenMinutes(), "Authority Protocol: ID Token Minutes");
            var refreshTokenDays = ParsePositive(protocol.RefreshTokenDays(), "Authority Protocol: Refresh Token Days");

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
                .AddClass("SecurityAuthorityDeviceConfiguration", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("string", "AuthorizationPath");
                    @class.AddProperty("string", "VerificationPath");
                    @class.AddProperty("string", "ApprovalPath");
                    @class.AddProperty("string", "Issuer");
                    @class.AddProperty("string", "SigningKeyId");
                    @class.AddProperty("int", "ExpiresInSeconds");
                    @class.AddProperty("int", "PollingIntervalSeconds");
                    @class.AddProperty("int", "AccessTokenMinutes");
                    @class.AddProperty("int", "IdTokenMinutes");
                    @class.AddProperty("int", "RefreshTokenDays");
                    @class.AddProperty("bool", "RefreshTokenEnabled");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement($"AuthorizationPath = \"{Escape(routes.DeviceAuthorizationRoute())}\";");
                        ctor.AddStatement($"VerificationPath = \"{Escape(routes.DeviceVerificationRoute())}\";");
                        ctor.AddStatement($"ApprovalPath = \"{Escape(routes.DeviceApprovalRoute())}\";");
                        ctor.AddStatement($"Issuer = \"{Escape(protocol.Issuer())}\";");
                        ctor.AddStatement($"SigningKeyId = \"{Escape(cryptography.ActiveSigningKeyId())}\";");
                        ctor.AddStatement("ExpiresInSeconds = 900;");
                        ctor.AddStatement($"PollingIntervalSeconds = {pollingIntervalSeconds.ToString(CultureInfo.InvariantCulture)};");
                        ctor.AddStatement($"AccessTokenMinutes = {accessTokenMinutes.ToString(CultureInfo.InvariantCulture)};");
                        ctor.AddStatement($"IdTokenMinutes = {idTokenMinutes.ToString(CultureInfo.InvariantCulture)};");
                        ctor.AddStatement($"RefreshTokenDays = {refreshTokenDays.ToString(CultureInfo.InvariantCulture)};");
                        ctor.AddStatement($"RefreshTokenEnabled = {features.RefreshToken().ToString().ToLowerInvariant()};");
                    });
                })
                .AddClass("SecurityAuthorityDeviceEndpoints", @class =>
                {
                    @class.Static();
                    @class.AddMethod("void", "Map", method =>
                    {
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("SecurityAuthorityDeviceConfiguration", "configuration");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("SecurityAuthorityCredentialHasher", "credentialHasher");
                        method.AddParameter("Func<string, CancellationToken, ValueTask<SecurityAuthorityOAuthClient?>>", "findClientByIdentifier");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<SecurityAuthorityDeviceGrant?>>", "findDeviceGrantByUserCode");
                        method.AddParameter("Func<HttpContext, CancellationToken, ValueTask<SecurityAuthorityUser?>>", "resolveActiveUser");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(endpoints);");
                        method.AddStatement("ValidateConfiguration(configuration);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(records);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(credentialHasher);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findClientByIdentifier);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findDeviceGrantByUserCode);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(resolveActiveUser);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                        method.AddStatement("""
                            endpoints.MapPost(configuration.AuthorizationPath, async (HttpContext context, CancellationToken cancellationToken) =>
                            {
                            if (!IsFormUrlEncoded(context.Request)) return OAuthError("invalid_request", "The device authorization endpoint accepts only application/x-www-form-urlencoded requests.");
                            var form = await context.Request.ReadFormAsync(cancellationToken);
                            var clientIdentifier = form["client_id"].ToString();
                            if (string.IsNullOrWhiteSpace(clientIdentifier)) return OAuthError("invalid_request", "client_id is required.");
                            var client = await findClientByIdentifier(clientIdentifier, cancellationToken);
                            if (client is null) return OAuthError("invalid_client", "The OAuth Client is unknown.");
                            var clientValidation = SecurityAuthorityOAuthClientValidation.ValidateDeviceAuthorizationClient(client);
                            if (!clientValidation.IsValid) return OAuthError(clientValidation.Error!, clientValidation.ErrorDescription!);
                            var scopes = ParseScopes(form["scope"].ToString());
                            if (scopes is null || scopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))) return OAuthError("invalid_scope", "A requested Scope is unknown or not allowed for this client.");
                            var now = utcNow();
                            var clearDeviceCode = NewDeviceCode();
                            var canonicalUserCode = NewUserCode();
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.TokenRedemption, true, cancellationToken);
                            SecurityAuthorityCommitReceipt receipt;
                            var deferredDeviceCode = new SecurityAuthorityDeferredCredential(operation.OperationId, () => clearDeviceCode);
                            try
                            {
                            var grant = new SecurityAuthorityDeviceGrant(NewOpaqueIdentifier(), credentialHasher.HashCredential(clearDeviceCode), canonicalUserCode, client.Id, scopes, configuration.PollingIntervalSeconds, "Pending", null, now, now.AddSeconds(configuration.ExpiresInSeconds), null, null, null, null, NewOpaqueIdentifier());
                            await operation.Records.AddAsync(grant, cancellationToken);
                            receipt = await operation.CommitAsync(cancellationToken);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            var verificationUri = Endpoint(configuration.Issuer, configuration.VerificationPath);
                            var renderedUserCode = RenderUserCode(canonicalUserCode);
                            return Results.Json(new Dictionary<string, object>(StringComparer.Ordinal)
                            {
                            ["device_code"] = deferredDeviceCode.Reveal(receipt),
                            ["user_code"] = renderedUserCode,
                            ["verification_uri"] = verificationUri,
                            ["verification_uri_complete"] = $"{verificationUri}?userCode={Uri.EscapeDataString(renderedUserCode)}",
                            ["expires_in"] = configuration.ExpiresInSeconds,
                            ["interval"] = configuration.PollingIntervalSeconds
                            });
                            }).AllowAnonymous();

                            endpoints.MapGet(configuration.VerificationPath, async (HttpContext context, string? userCode, CancellationToken cancellationToken) =>
                            {
                            var user = await resolveActiveUser(context, cancellationToken);
                            if (user is null || !string.Equals(user.Status, "Active", StringComparison.Ordinal)) return Results.Unauthorized();
                            var canonicalUserCode = NormalizeUserCode(userCode);
                            if (canonicalUserCode is null) return DeviceValidationError("invalid_user_code", "The User Code is invalid.", StatusCodes.Status404NotFound);
                            var grant = await findDeviceGrantByUserCode(records, canonicalUserCode, cancellationToken);
                            if (grant is null) return DeviceValidationError("invalid_user_code", "The User Code is unknown.", StatusCodes.Status404NotFound);
                            var client = await records.LoadAsync(typeof(SecurityAuthorityOAuthClient), grant.OAuthClientId, cancellationToken) as SecurityAuthorityOAuthClient;
                            if (client is null || !client.IsActive) return DeviceValidationError("invalid_client", "The OAuth Client is inactive or unknown.");
                            var status = grant.ExpiresAt <= utcNow() && !string.Equals(grant.Status, "Redeemed", StringComparison.Ordinal) ? "Expired" : grant.Status;
                            return Results.Json(new Dictionary<string, object>(StringComparer.Ordinal)
                            {
                            ["client"] = client.DisplayName,
                            ["scopes"] = grant.RequestedScopes,
                            ["expires_at"] = grant.ExpiresAt,
                            ["status"] = status
                            });
                            }).RequireAuthorization();

                            endpoints.MapPost(configuration.ApprovalPath, async (HttpContext context, CancellationToken cancellationToken) =>
                            {
                            var user = await resolveActiveUser(context, cancellationToken);
                            if (user is null || !string.Equals(user.Status, "Active", StringComparison.Ordinal)) return Results.Unauthorized();
                            if (!IsFormUrlEncoded(context.Request)) return DeviceValidationError("invalid_request", "The device approval endpoint accepts only application/x-www-form-urlencoded requests.");
                            var form = await context.Request.ReadFormAsync(cancellationToken);
                            var canonicalUserCode = NormalizeUserCode(form["user_code"].ToString());
                            var decision = form["decision"].ToString();
                            if (canonicalUserCode is null) return DeviceValidationError("invalid_user_code", "The User Code is invalid.", StatusCodes.Status404NotFound);
                            if (!string.Equals(decision, "approve", StringComparison.OrdinalIgnoreCase) && !string.Equals(decision, "deny", StringComparison.OrdinalIgnoreCase)) return DeviceValidationError("invalid_request", "decision must be approve or deny.");
                            var now = utcNow();
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.TokenRedemption, true, cancellationToken);
                            try
                            {
                            var grant = await findDeviceGrantByUserCode(operation.Records, canonicalUserCode, cancellationToken);
                            if (grant is null) return await RollbackDeviceErrorAsync(operation, "invalid_user_code", "The User Code is unknown.", StatusCodes.Status404NotFound, cancellationToken);
                            var activeUser = await operation.Records.LoadAsync(typeof(SecurityAuthorityUser), user.Id, cancellationToken) as SecurityAuthorityUser;
                            if (activeUser is null || !string.Equals(activeUser.Status, "Active", StringComparison.Ordinal)) return await RollbackDeviceErrorAsync(operation, "inactive_user", "The authenticated User is not active.", StatusCodes.Status401Unauthorized, cancellationToken);
                            var client = await operation.Records.LoadAsync(typeof(SecurityAuthorityOAuthClient), grant.OAuthClientId, cancellationToken) as SecurityAuthorityOAuthClient;
                            if (client is null || !client.IsActive) return await RollbackDeviceErrorAsync(operation, "invalid_client", "The OAuth Client is inactive or unknown.", StatusCodes.Status400BadRequest, cancellationToken);
                            if (grant.RequestedScopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))) return await RollbackDeviceErrorAsync(operation, "invalid_scope", "A requested Scope is no longer allowed for this client.", StatusCodes.Status400BadRequest, cancellationToken);
                            if (grant.ExpiresAt <= now || string.Equals(grant.Status, "Expired", StringComparison.Ordinal)) return await RollbackDeviceErrorAsync(operation, "expired_token", "The Device Grant has expired.", StatusCodes.Status400BadRequest, cancellationToken);
                            if (!string.Equals(grant.Status, "Pending", StringComparison.Ordinal)) return await RollbackDeviceErrorAsync(operation, "invalid_grant", "The Device Grant is no longer pending.", StatusCodes.Status409Conflict, cancellationToken);
                            var updated = string.Equals(decision, "approve", StringComparison.OrdinalIgnoreCase)
                            ? grant with { Status = "Approved", UserId = user.Id, ApprovedAt = now, ConcurrencyToken = NewOpaqueIdentifier() }
                            : grant with { Status = "Denied", UserId = user.Id, DeniedAt = now, ConcurrencyToken = NewOpaqueIdentifier() };
                            await operation.Records.UpdateAsync(updated, grant.ConcurrencyToken, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            return Results.Ok(new { status = updated.Status });
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            }).RequireAuthorization();
                            """);
                    });
                    @class.AddMethod("ValueTask<IResult>", "RedeemDeviceCodeAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("HttpContext", "context");
                        method.AddParameter("IFormCollection", "form");
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("SecurityAuthorityDeviceConfiguration", "configuration");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("SecurityAuthorityCredentialHasher", "credentialHasher");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<SecurityAuthorityDeviceGrant?>>", "findDeviceGrantByDeviceCode");
                        method.AddParameter("Func<string, string, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyDictionary<string, string>>>", "resolveContextClaims");
                        method.AddParameter("Func<Exception, bool>", "isConcurrencyConflict");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ValidateConfiguration(configuration);
                            ArgumentNullException.ThrowIfNull(persistence);
                            ArgumentNullException.ThrowIfNull(credentialHasher);
                            ArgumentNullException.ThrowIfNull(signingKeys);
                            ArgumentNullException.ThrowIfNull(findDeviceGrantByDeviceCode);
                            ArgumentNullException.ThrowIfNull(resolveContextClaims);
                            ArgumentNullException.ThrowIfNull(isConcurrencyConflict);
                            ArgumentNullException.ThrowIfNull(utcNow);
                            var clientValidation = SecurityAuthorityOAuthClientValidation.ValidateDeviceAuthorizationClient(client);
                            if (!clientValidation.IsValid) return OAuthError(clientValidation.Error!, clientValidation.ErrorDescription!);
                            var clearDeviceCode = form["device_code"].ToString();
                            if (string.IsNullOrWhiteSpace(clearDeviceCode)) return OAuthError("invalid_request", "device_code is required.");
                            var now = utcNow();
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.TokenRedemption, true, cancellationToken);
                            SecurityAuthorityDeviceGrant redeemedGrant;
                            SecurityAuthorityDeferredCredential deferredRefreshToken;
                            SecurityAuthorityCommitReceipt receipt;
                            string accessToken;
                            string idToken;
                            try
                            {
                            var grant = await findDeviceGrantByDeviceCode(operation.Records, clearDeviceCode, cancellationToken);
                            if (grant is null || !credentialHasher.VerifyCredential(clearDeviceCode, grant.DeviceCodeHash) || !string.Equals(grant.OAuthClientId, client.Id, StringComparison.Ordinal)) return await RollbackOAuthErrorAsync(operation, "invalid_grant", "The Device Code is invalid or does not belong to this client.", cancellationToken);
                            if (grant.RequestedScopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))) return await RollbackOAuthErrorAsync(operation, "invalid_grant", "The Device Grant contains a Scope no longer allowed for this client.", cancellationToken);
                            if (string.Equals(grant.Status, "Redeemed", StringComparison.Ordinal)) return await RollbackOAuthErrorAsync(operation, "invalid_grant", "The Device Grant was already redeemed.", cancellationToken);
                            if (grant.ExpiresAt <= now || string.Equals(grant.Status, "Expired", StringComparison.Ordinal))
                            {
                            if (!string.Equals(grant.Status, "Expired", StringComparison.Ordinal))
                            {
                            await operation.Records.UpdateAsync(grant with { Status = "Expired", ConcurrencyToken = NewOpaqueIdentifier() }, grant.ConcurrencyToken, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            }
                            else
                            {
                            await operation.RollbackAsync(cancellationToken);
                            }
                            return OAuthError("expired_token", "The Device Grant has expired.");
                            }
                            if (string.Equals(grant.Status, "Denied", StringComparison.Ordinal)) return await RollbackOAuthErrorAsync(operation, "access_denied", "The Device User denied this request.", cancellationToken);
                            if (grant.LastPolledAt is not null && now < grant.LastPolledAt.Value.AddSeconds(grant.PollingIntervalSeconds))
                            {
                            await operation.Records.UpdateAsync(grant with { LastPolledAt = now, ConcurrencyToken = NewOpaqueIdentifier() }, grant.ConcurrencyToken, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            return OAuthError("slow_down", "Polling occurred before the configured interval elapsed.");
                            }
                            if (string.Equals(grant.Status, "Pending", StringComparison.Ordinal))
                            {
                            await operation.Records.UpdateAsync(grant with { LastPolledAt = now, ConcurrencyToken = NewOpaqueIdentifier() }, grant.ConcurrencyToken, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            return OAuthError("authorization_pending", "The Device User has not completed authorization.");
                            }
                            if (!string.Equals(grant.Status, "Approved", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(grant.UserId)) return await RollbackOAuthErrorAsync(operation, "invalid_grant", "The Device Grant has an invalid state.", cancellationToken);
                            var user = await operation.Records.LoadAsync(typeof(SecurityAuthorityUser), grant.UserId, cancellationToken) as SecurityAuthorityUser;
                            if (user is null || !string.Equals(user.Status, "Active", StringComparison.Ordinal)) return await RollbackOAuthErrorAsync(operation, "invalid_grant", "The Device Grant owner is not active.", cancellationToken);
                            var contextClaims = await resolveContextClaims("User", user.Id, grant.RequestedScopes, cancellationToken);
                            accessToken = CreateAccessToken(configuration, signingKeys, user.Id, client.ClientIdentifier, grant.RequestedScopes, contextClaims, now);
                            idToken = CreateIdToken(configuration, signingKeys, client.ClientIdentifier, user.Id, now);
                            var clearRefreshToken = NewOpaqueIdentifier();
                            deferredRefreshToken = new SecurityAuthorityDeferredCredential(operation.OperationId, () => clearRefreshToken);
                            await operation.Records.AddAsync(new SecurityAuthorityRefreshToken(NewOpaqueIdentifier(), credentialHasher.HashCredential(clearRefreshToken), client.Id, user.Id, now, now.AddDays(configuration.RefreshTokenDays), null, null, null, NewOpaqueIdentifier()), cancellationToken);
                            redeemedGrant = grant with { Status = "Redeemed", RedeemedAt = now, LastPolledAt = now, ConcurrencyToken = NewOpaqueIdentifier() };
                            await operation.Records.UpdateAsync(redeemedGrant, grant.ConcurrencyToken, cancellationToken);
                            receipt = await operation.CommitAsync(cancellationToken);
                            }
                            catch (Exception exception) when (isConcurrencyConflict(exception))
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return OAuthError("invalid_grant", "The Device Grant was concurrently redeemed.");
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            return TokenResponse(accessToken, configuration.AccessTokenMinutes * 60, redeemedGrant.RequestedScopes, idToken, deferredRefreshToken.Reveal(receipt));
                            """);
                    });
                    @class.AddMethod("ValueTask<IResult>", "RollbackOAuthErrorAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityAtomicOperation", "operation");
                        method.AddParameter("string", "error");
                        method.AddParameter("string", "description");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("return OAuthError(error, description);");
                    });
                    @class.AddMethod("ValueTask<IResult>", "RollbackDeviceErrorAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityAtomicOperation", "operation");
                        method.AddParameter("string", "error");
                        method.AddParameter("string", "description");
                        method.AddParameter("int", "statusCode");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("return DeviceValidationError(error, description, statusCode);");
                    });
                    @class.AddMethod("string", "NewDeviceCode", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddStatement("return SecurityAuthorityBase64Url.Encode(RandomNumberGenerator.GetBytes(32));");
                    });
                    @class.AddMethod("string", "NewUserCode", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddStatement("const string alphabet = \"3467CDFHJKMNPRTVWXY\";");
                        method.AddStatement("return new string(Enumerable.Range(0, 8).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());");
                    });
                    @class.AddMethod("string?", "NormalizeUserCode", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string?", "value");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(value)) return null;");
                        method.AddStatement("var canonical = value.Replace(\"-\", string.Empty, StringComparison.Ordinal).ToUpperInvariant();");
                        method.AddStatement("const string alphabet = \"3467CDFHJKMNPRTVWXY\";");
                        method.AddStatement("return canonical.Length == 8 && canonical.All(character => alphabet.Contains(character)) ? canonical : null;");
                    });
                    @class.AddMethod("string", "RenderUserCode", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "canonicalUserCode");
                        method.AddStatement("return $\"{canonicalUserCode[..4]}-{canonicalUserCode[4..]}\";");
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
                    @class.AddMethod("bool", "IsFormUrlEncoded", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("HttpRequest", "request");
                        method.AddStatement("var mediaType = request.ContentType?.Split(';', StringSplitOptions.TrimEntries).FirstOrDefault();");
                        method.AddStatement("return string.Equals(mediaType, \"application/x-www-form-urlencoded\", StringComparison.OrdinalIgnoreCase);");
                    });
                    @class.AddMethod("string", "Endpoint", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "issuer");
                        method.AddParameter("string", "path");
                        method.AddStatement("return new Uri(new Uri(issuer.TrimEnd('/') + \"/\", UriKind.Absolute), path.TrimStart('/')).AbsoluteUri;");
                    });
                    @class.AddMethod("string", "CreateAccessToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityDeviceConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("string", "subject");
                        method.AddParameter("string", "audience");
                        method.AddParameter("IReadOnlyList<string>", "scopes");
                        method.AddParameter("IReadOnlyDictionary<string, string>", "contextClaims");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("var expiresAt = now.AddMinutes(configuration.AccessTokenMinutes);");
                        method.AddStatement("var claims = new Dictionary<string, object>(StringComparer.Ordinal) { [\"iss\"] = configuration.Issuer, [\"aud\"] = audience, [\"sub\"] = subject, [\"principal_type\"] = \"User\", [\"scope\"] = string.Join(' ', scopes), [\"iat\"] = now.ToUnixTimeSeconds(), [\"nbf\"] = now.ToUnixTimeSeconds(), [\"exp\"] = expiresAt.ToUnixTimeSeconds(), [\"jti\"] = NewOpaqueIdentifier() };");
                        method.AddStatement("foreach (var claim in contextClaims) if (!claims.ContainsKey(claim.Key)) claims.Add(claim.Key, claim.Value);");
                        method.AddStatement("return SignToken(configuration, signingKeys, claims, now, expiresAt);");
                    });
                    @class.AddMethod("string", "CreateIdToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityDeviceConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("string", "clientAudience");
                        method.AddParameter("string", "userSubject");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("var expiresAt = now.AddMinutes(configuration.IdTokenMinutes);");
                        method.AddStatement("var claims = new Dictionary<string, object>(StringComparer.Ordinal) { [\"iss\"] = configuration.Issuer, [\"aud\"] = clientAudience, [\"sub\"] = userSubject, [\"iat\"] = now.ToUnixTimeSeconds(), [\"exp\"] = expiresAt.ToUnixTimeSeconds() };");
                        method.AddStatement("return SignToken(configuration, signingKeys, claims, now, expiresAt);");
                    });
                    @class.AddMethod("string", "SignToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityDeviceConfiguration", "configuration");
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
                        method.AddParameter("string", "error");
                        method.AddParameter("string", "description");
                        method.AddStatement("return Results.Json(new { error, error_description = description }, statusCode: StatusCodes.Status400BadRequest);");
                    });
                    @class.AddMethod("IResult", "DeviceValidationError", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "error");
                        method.AddParameter("string", "description");
                        method.AddParameter("int", "statusCode", parameter => parameter.WithDefaultValue("StatusCodes.Status400BadRequest"));
                        method.AddStatement("return Results.Json(new { error, error_description = description }, statusCode: statusCode);");
                    });
                    @class.AddMethod("void", "ValidateConfiguration", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityDeviceConfiguration", "configuration");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(configuration);");
                        method.AddStatement("if (new[] { configuration.AuthorizationPath, configuration.VerificationPath, configuration.ApprovalPath }.Any(string.IsNullOrWhiteSpace)) throw new InvalidOperationException(\"Security Authority device endpoint paths cannot be empty.\");");
                        method.AddStatement("if (!Uri.TryCreate(configuration.Issuer, UriKind.Absolute, out var issuer) || !issuer.IsAbsoluteUri) throw new InvalidOperationException(\"The Security Authority issuer must be an absolute URI.\");");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(configuration.SigningKeyId)) throw new InvalidOperationException(\"The Security Authority signing key id cannot be empty.\");");
                        method.AddStatement("if (configuration.ExpiresInSeconds != 900) throw new InvalidOperationException(\"Device Grants must expire after 900 seconds.\");");
                        method.AddStatement("if (configuration.PollingIntervalSeconds != 5) throw new InvalidOperationException(\"Device Grants must use a five-second polling interval.\");");
                        method.AddStatement("if (configuration.AccessTokenMinutes <= 0 || configuration.IdTokenMinutes <= 0 || configuration.RefreshTokenDays <= 0) throw new InvalidOperationException(\"Security Authority token durations must be positive.\");");
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
