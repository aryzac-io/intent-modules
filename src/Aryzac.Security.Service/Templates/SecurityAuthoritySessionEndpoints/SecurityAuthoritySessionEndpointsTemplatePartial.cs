using System;
using System.Collections.Generic;
using Aryzac.Security.Service.Templates.SecurityAuthorityContracts;
using Aryzac.Security.Service.Templates.SecurityAuthorityCryptography;
using Aryzac.Security.Service.Templates.SecurityAuthorityDiscoveryEndpoints;
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

namespace Aryzac.Security.Service.Templates.SecurityAuthoritySessionEndpoints
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthoritySessionEndpointsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthoritySessionEndpoints";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthoritySessionEndpointsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            AddTypeSource(SecurityAuthorityContractsTemplate.TemplateId);
            AddTypeSource(SecurityAuthorityCryptographyTemplate.TemplateId);
            AddTypeSource(SecurityAuthorityDiscoveryEndpointsTemplate.TemplateId);
            AddTypeSource(SecurityAuthorityRecordsTemplate.TemplateId);

            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Security.Cryptography")
                .AddUsing("System.Text")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Microsoft.AspNetCore.Builder")
                .AddUsing("Microsoft.AspNetCore.Http")
                .AddUsing("Microsoft.AspNetCore.Routing")
                .AddClass("SecurityAuthoritySessionConfiguration", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("string", "CookieName");
                    @class.AddProperty("string", "UserInfoPath");
                    @class.AddProperty("string", "LogoutPath");
                    @class.AddProperty("int", "SsoSessionLifetimeMinutes");
                    @class.AddProperty("IReadOnlyList<string>", "ContextualClaimNames");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement("CookieName = \"Aryzac.Security.Sso\";");
                        ctor.AddStatement("UserInfoPath = \"/connect/userinfo\";");
                        ctor.AddStatement("LogoutPath = \"/connect/logout\";");
                        ctor.AddStatement("SsoSessionLifetimeMinutes = 480;");
                        ctor.AddStatement("ContextualClaimNames = Array.Empty<string>();");
                    });
                })
                .AddRecord("SecurityAuthorityResolvedSession", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthoritySsoSession", "Session");
                        ctor.AddParameter("SecurityAuthorityUser", "User");
                    });
                })
                .AddClass("SecurityAuthoritySessionEndpoints", @class =>
                {
                    @class.Static();
                    @class.AddMethod("void", "Map", method =>
                    {
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("SecurityAuthoritySessionConfiguration", "configuration");
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("SecurityAuthoritySecretProtector", "cookieProtector");
                        method.AddParameter("Func<string, CancellationToken, ValueTask<SecurityAuthorityOAuthClient?>>", "findClientByIdentifier");
                        method.AddParameter("Func<SecurityAuthorityUser, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyDictionary<string, string>>>", "resolveContextualClaims");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("bool", "isDevelopment");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(endpoints);");
                        method.AddStatement("ValidateConfiguration(configuration);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(records);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(cookieProtector);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findClientByIdentifier);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(resolveContextualClaims);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                        method.AddStatement("endpoints.MapGet(configuration.UserInfoPath, async (HttpContext context, CancellationToken cancellationToken) =>");
                        method.AddStatement("{");
                        method.AddStatement("    var resolved = await ResolveAsync(context.Request, configuration, records, cookieProtector, utcNow(), cancellationToken);");
                        method.AddStatement("    if (resolved is null) return Results.Unauthorized();");
                        method.AddStatement("    var contextualClaims = await resolveContextualClaims(resolved.User, configuration.ContextualClaimNames, cancellationToken);");
                        method.AddStatement("    return Results.Json(CreateUserInfo(resolved.User, configuration.ContextualClaimNames, contextualClaims));");
                        method.AddStatement("}).RequireAuthorization();");
                        method.AddStatement("endpoints.MapGet(configuration.LogoutPath, async (HttpContext context, string? client_id, string? post_logout_redirect_uri, CancellationToken cancellationToken) =>");
                        method.AddStatement("{");
                        method.AddStatement("    var now = utcNow();");
                        method.AddStatement("    var session = await ResolveSessionRecordAsync(context.Request, configuration, records, cookieProtector, now, cancellationToken);");
                        method.AddStatement("    if (session is not null)");
                        method.AddStatement("    {");
                        method.AddStatement("        var revoked = session with { RevokedAt = now, ConcurrencyToken = NewOpaqueIdentifier() };");
                        method.AddStatement("        await records.UpdateAsync(revoked, session.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("    }");
                        method.AddStatement("    ClearCookie(context.Response, configuration, isDevelopment);");
                        method.AddStatement("    var redirectUri = await ResolveLogoutRedirectAsync(client_id, post_logout_redirect_uri, findClientByIdentifier, cancellationToken);");
                        method.AddStatement("    return Results.Redirect(redirectUri);");
                        method.AddStatement("}).AllowAnonymous();");
                    });
                    @class.AddMethod("SecurityAuthoritySsoSession", "CreateSession", method =>
                    {
                        method.Static();
                        method.AddParameter("string", "userId");
                        method.AddParameter("SecurityAuthoritySessionConfiguration", "configuration");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("ArgumentException.ThrowIfNullOrWhiteSpace(userId);");
                        method.AddStatement("ValidateConfiguration(configuration);");
                        method.AddStatement("return new SecurityAuthoritySsoSession(NewOpaqueIdentifier(), NewOpaqueIdentifier(), userId, now, now.AddMinutes(configuration.SsoSessionLifetimeMinutes), null, NewOpaqueIdentifier());");
                    });
                    @class.AddMethod("void", "IssueCookie", method =>
                    {
                        method.Static();
                        method.AddParameter("HttpResponse", "response");
                        method.AddParameter("SecurityAuthoritySessionConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySecretProtector", "cookieProtector");
                        method.AddParameter("SecurityAuthoritySsoSession", "session");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("bool", "isDevelopment");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(response);");
                        method.AddStatement("ValidateConfiguration(configuration);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(cookieProtector);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(session);");
                        method.AddStatement("if (session.RevokedAt is not null || session.ExpiresAt <= now) throw new InvalidOperationException(\"Only an active SSO Session can be issued to the browser.\");");
                        method.AddStatement("var payload = $\"{EncodeIdentifier(session.Id)}.{EncodeIdentifier(session.OpaqueCookieIdentifier)}\";");
                        method.AddStatement("var configuredExpiry = now.AddMinutes(configuration.SsoSessionLifetimeMinutes);");
                        method.AddStatement("var expiresAt = session.ExpiresAt < configuredExpiry ? session.ExpiresAt : configuredExpiry;");
                        method.AddStatement("response.Cookies.Append(configuration.CookieName, cookieProtector.Protect(payload), CreateCookieOptions(expiresAt, isDevelopment));");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityResolvedSession?>", "ResolveAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("HttpRequest", "request");
                        method.AddParameter("SecurityAuthoritySessionConfiguration", "configuration");
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("SecurityAuthoritySecretProtector", "cookieProtector");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var session = await ResolveSessionRecordAsync(request, configuration, records, cookieProtector, now, cancellationToken);");
                        method.AddStatement("if (session is null) return null;");
                        method.AddStatement("var user = await records.LoadAsync(typeof(SecurityAuthorityUser), session.UserId, cancellationToken) as SecurityAuthorityUser;");
                        method.AddStatement("return user is not null && string.Equals(user.Status, \"Active\", StringComparison.Ordinal) ? new SecurityAuthorityResolvedSession(session, user) : null;");
                    });
                    @class.AddMethod("void", "ClearCookie", method =>
                    {
                        method.Static();
                        method.AddParameter("HttpResponse", "response");
                        method.AddParameter("SecurityAuthoritySessionConfiguration", "configuration");
                        method.AddParameter("bool", "isDevelopment");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(response);");
                        method.AddStatement("ValidateConfiguration(configuration);");
                        method.AddStatement("response.Cookies.Delete(configuration.CookieName, CreateCookieOptions(DateTimeOffset.UnixEpoch, isDevelopment));");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthoritySsoSession?>", "ResolveSessionRecordAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("HttpRequest", "request");
                        method.AddParameter("SecurityAuthoritySessionConfiguration", "configuration");
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("SecurityAuthoritySecretProtector", "cookieProtector");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(request);");
                        method.AddStatement("ValidateConfiguration(configuration);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(records);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(cookieProtector);");
                        method.AddStatement("if (!request.Cookies.TryGetValue(configuration.CookieName, out var protectedCookie) || string.IsNullOrWhiteSpace(protectedCookie)) return null;");
                        method.AddStatement("string payload;");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    payload = cookieProtector.Unprotect(protectedCookie);");
                        method.AddStatement("}");
                        method.AddStatement("catch (CryptographicException)");
                        method.AddStatement("{");
                        method.AddStatement("    return null;");
                        method.AddStatement("}");
                        method.AddStatement("var parts = payload.Split('.');");
                        method.AddStatement("if (parts.Length != 2) return null;");
                        method.AddStatement("string sessionId;");
                        method.AddStatement("string cookieIdentifier;");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    sessionId = DecodeIdentifier(parts[0]);");
                        method.AddStatement("    cookieIdentifier = DecodeIdentifier(parts[1]);");
                        method.AddStatement("}");
                        method.AddStatement("catch (FormatException)");
                        method.AddStatement("{");
                        method.AddStatement("    return null;");
                        method.AddStatement("}");
                        method.AddStatement("var session = await records.LoadAsync(typeof(SecurityAuthoritySsoSession), sessionId, cancellationToken) as SecurityAuthoritySsoSession;");
                        method.AddStatement("if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now || !FixedTimeEquals(cookieIdentifier, session.OpaqueCookieIdentifier)) return null;");
                        method.AddStatement("return session;");
                    });
                    @class.AddMethod("IReadOnlyDictionary<string, object>", "CreateUserInfo", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityUser", "user");
                        method.AddParameter("IReadOnlyList<string>", "configuredClaimNames");
                        method.AddParameter("IReadOnlyDictionary<string, string>", "contextualClaims");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(user);");
                        method.AddStatement("var result = new Dictionary<string, object>(StringComparer.Ordinal)");
                        method.AddStatement("{");
                        method.AddStatement("    [\"sub\"] = user.Id,");
                        method.AddStatement("    [\"name\"] = user.DisplayName,");
                        method.AddStatement("    [\"email\"] = user.NormalizedEmail,");
                        method.AddStatement("    [\"principal_type\"] = \"User\"");
                        method.AddStatement("};");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(user.AvatarUrl)) result[\"picture\"] = user.AvatarUrl;");
                        method.AddStatement("var reserved = new[] { \"sub\", \"name\", \"email\", \"picture\", \"principal_type\" };");
                        method.AddStatement("foreach (var claimName in (configuredClaimNames ?? Array.Empty<string>()).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    if (!reserved.Contains(claimName, StringComparer.Ordinal) && contextualClaims is not null && contextualClaims.TryGetValue(claimName, out var value)) result[claimName] = value;");
                        method.AddStatement("}");
                        method.AddStatement("return result;");
                    });
                    @class.AddMethod("ValueTask<string>", "ResolveLogoutRedirectAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("string?", "clientIdentifier");
                        method.AddParameter("string?", "postLogoutRedirectUri");
                        method.AddParameter("Func<string, CancellationToken, ValueTask<SecurityAuthorityOAuthClient?>>", "findClientByIdentifier");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(clientIdentifier) || string.IsNullOrWhiteSpace(postLogoutRedirectUri)) return \"/\";");
                        method.AddStatement("var client = await findClientByIdentifier(clientIdentifier, cancellationToken);");
                        method.AddStatement("if (client is null || !client.IsActive) return \"/\";");
                        method.AddStatement("var validation = SecurityAuthorityOAuthClientValidation.ValidateRedirectUri(client, postLogoutRedirectUri, true);");
                        method.AddStatement("return validation.IsValid ? postLogoutRedirectUri : \"/\";");
                    });
                    @class.AddMethod("CookieOptions", "CreateCookieOptions", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("DateTimeOffset", "expiresAt");
                        method.AddParameter("bool", "isDevelopment");
                        method.AddStatement("return new CookieOptions");
                        method.AddStatement("{");
                        method.AddStatement("    HttpOnly = true,");
                        method.AddStatement("    Secure = !isDevelopment,");
                        method.AddStatement("    SameSite = SameSiteMode.Lax,");
                        method.AddStatement("    Path = \"/\",");
                        method.AddStatement("    Expires = expiresAt");
                        method.AddStatement("};");
                    });
                    @class.AddMethod("void", "ValidateConfiguration", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthoritySessionConfiguration", "configuration");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(configuration);");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(configuration.CookieName)) throw new InvalidOperationException(\"Security Authority SSO cookie name cannot be empty.\");");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(configuration.UserInfoPath) || string.IsNullOrWhiteSpace(configuration.LogoutPath)) throw new InvalidOperationException(\"Security Authority session endpoint paths cannot be empty.\");");
                        method.AddStatement("if (configuration.SsoSessionLifetimeMinutes is < 5 or > 43200) throw new InvalidOperationException(\"Security Authority SSO Session lifetime must be between 5 and 43200 minutes.\");");
                    });
                    @class.AddMethod("string", "NewOpaqueIdentifier", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddStatement("return SecurityAuthorityBase64Url.Encode(RandomNumberGenerator.GetBytes(32));");
                    });
                    @class.AddMethod("string", "EncodeIdentifier", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "value");
                        method.AddStatement("return SecurityAuthorityBase64Url.Encode(Encoding.UTF8.GetBytes(value));");
                    });
                    @class.AddMethod("string", "DecodeIdentifier", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "value");
                        method.AddStatement("return Encoding.UTF8.GetString(SecurityAuthorityBase64Url.Decode(value));");
                    });
                    @class.AddMethod("bool", "FixedTimeEquals", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "left");
                        method.AddParameter("string", "right");
                        method.AddStatement("var leftBytes = Encoding.UTF8.GetBytes(left);");
                        method.AddStatement("var rightBytes = Encoding.UTF8.GetBytes(right);");
                        method.AddStatement("return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);");
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
