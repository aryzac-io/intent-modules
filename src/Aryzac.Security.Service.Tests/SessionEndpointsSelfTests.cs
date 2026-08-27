using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class SessionEndpointsSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string SessionSource = ReadServiceSource("Templates", "SecurityAuthoritySessionEndpoints", "SecurityAuthoritySessionEndpointsTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("SSO cookie contains only an opaque protected session handle", SsoCookieContainsOnlyOpaqueProtectedSessionHandle),
            ("SSO cookie settings and lifetime are constrained", SsoCookieSettingsAndLifetimeAreConstrained),
            ("invalid server sessions and inactive users are rejected", InvalidServerSessionsAndInactiveUsersAreRejected),
            ("userinfo returns standard and configured contextual claims", UserInfoReturnsStandardAndConfiguredContextualClaims),
            ("logout revokes only the current SSO Session and clears its cookie", LogoutRevokesOnlyCurrentSessionAndClearsCookie),
            ("logout redirects only to an exact URI for an active client", LogoutRedirectsOnlyToExactUriForActiveClient),
            ("logout does not revoke Access Tokens", LogoutDoesNotRevokeAccessTokens),
            ("session endpoint surface contains no stubs", SessionEndpointSurfaceContainsNoStubs)
        };

        var failures = new List<string>();
        foreach (var test in tests)
        {
            try
            {
                test.Execute();
                Console.WriteLine($"PASS: {test.Name}");
            }
            catch (Exception exception)
            {
                failures.Add($"FAIL: {test.Name}: {exception.Message}");
            }
        }

        foreach (var failure in failures)
        {
            Console.Error.WriteLine(failure);
        }

        return failures.Count == 0 ? 0 : 1;
    }

    private static void SsoCookieContainsOnlyOpaqueProtectedSessionHandle()
    {
        var issueCookie = GetBlock(SessionSource, ".AddMethod(\"void\", \"IssueCookie\"", ".AddMethod(\"ValueTask<SecurityAuthorityResolvedSession?>\", \"ResolveAsync\"");

        Contains(issueCookie, "var payload = $\\\"{EncodeIdentifier(session.Id)}.{EncodeIdentifier(session.OpaqueCookieIdentifier)}\\\";");
        Contains(issueCookie, "cookieProtector.Protect(payload)");
        foreach (var excluded in new[]
        {
            "session.UserId",
            "SecurityAuthorityUser",
            "AccessToken",
            "RefreshToken",
            "Scope",
            "ClientSecret",
            "EncryptedClientSecret",
            "Provider"
        })
        {
            DoesNotContain(issueCookie, excluded);
        }
    }

    private static void SsoCookieSettingsAndLifetimeAreConstrained()
    {
        Contains(SessionSource, "SsoSessionLifetimeMinutes = 480;");
        Contains(SessionSource, "configuration.SsoSessionLifetimeMinutes is < 5 or > 43200");
        Contains(SessionSource, "now.AddMinutes(configuration.SsoSessionLifetimeMinutes)");

        var cookieOptions = GetBlock(SessionSource, ".AddMethod(\"CookieOptions\", \"CreateCookieOptions\"", ".AddMethod(\"void\", \"ValidateConfiguration\"");
        Contains(cookieOptions, "HttpOnly = true");
        Contains(cookieOptions, "Secure = !isDevelopment");
        Contains(cookieOptions, "SameSite = SameSiteMode.Lax");
        Contains(cookieOptions, "Path = \\\"/\\\"");
        Contains(cookieOptions, "Expires = expiresAt");
        DoesNotContain(cookieOptions, "Domain =");
    }

    private static void InvalidServerSessionsAndInactiveUsersAreRejected()
    {
        Contains(SessionSource, "if (!request.Cookies.TryGetValue(configuration.CookieName, out var protectedCookie) || string.IsNullOrWhiteSpace(protectedCookie)) return null;");
        Contains(SessionSource, "session is null || session.RevokedAt is not null || session.ExpiresAt <= now || !FixedTimeEquals(cookieIdentifier, session.OpaqueCookieIdentifier)");
        Contains(SessionSource, "user is not null && string.Equals(user.Status, \\\"Active\\\", StringComparison.Ordinal)");
    }

    private static void UserInfoReturnsStandardAndConfiguredContextualClaims()
    {
        Contains(SessionSource, "endpoints.MapGet(configuration.UserInfoPath");
        Contains(SessionSource, "}).RequireAuthorization();");

        var userInfo = GetBlock(SessionSource, ".AddMethod(\"IReadOnlyDictionary<string, object>\", \"CreateUserInfo\"", ".AddMethod(\"ValueTask<string>\", \"ResolveLogoutRedirectAsync\"");
        foreach (var claim in new[] { "sub", "name", "email", "principal_type" })
        {
            Contains(userInfo, $"[\\\"{claim}\\\"]");
        }

        Contains(userInfo, "[\\\"principal_type\\\"] = \\\"User\\\"");
        Contains(userInfo, "if (!string.IsNullOrWhiteSpace(user.AvatarUrl)) result[\\\"picture\\\"] = user.AvatarUrl;");
        Contains(userInfo, "configuredClaimNames ?? Array.Empty<string>()");
        Contains(userInfo, "contextualClaims.TryGetValue(claimName, out var value)");
        Contains(userInfo, "!reserved.Contains(claimName, StringComparer.Ordinal)");
    }

    private static void LogoutRevokesOnlyCurrentSessionAndClearsCookie()
    {
        var logout = GetBlock(SessionSource, "endpoints.MapGet(configuration.LogoutPath", "}).AllowAnonymous();");
        Contains(logout, "ResolveSessionRecordAsync(context.Request, configuration, records, cookieProtector, now, cancellationToken)");
        Contains(logout, "var revoked = session with { RevokedAt = now, ConcurrencyToken = NewOpaqueIdentifier() };");
        Contains(logout, "await records.UpdateAsync(revoked, session.ConcurrencyToken, cancellationToken);");
        Contains(logout, "ClearCookie(context.Response, configuration, isDevelopment);");
        DoesNotContain(logout, "QueryAsync");
        DoesNotContain(logout, "DeleteAsync");

        var clearCookie = GetBlock(SessionSource, ".AddMethod(\"void\", \"ClearCookie\"", ".AddMethod(\"ValueTask<SecurityAuthoritySsoSession?>\", \"ResolveSessionRecordAsync\"");
        Contains(clearCookie, "response.Cookies.Delete(configuration.CookieName, CreateCookieOptions(DateTimeOffset.UnixEpoch, isDevelopment));");
    }

    private static void LogoutRedirectsOnlyToExactUriForActiveClient()
    {
        var redirect = GetBlock(SessionSource, ".AddMethod(\"ValueTask<string>\", \"ResolveLogoutRedirectAsync\"", ".AddMethod(\"CookieOptions\", \"CreateCookieOptions\"");
        Contains(redirect, "if (string.IsNullOrWhiteSpace(clientIdentifier) || string.IsNullOrWhiteSpace(postLogoutRedirectUri)) return \\\"/\\\";");
        Contains(redirect, "if (client is null || !client.IsActive) return \\\"/\\\";");
        Contains(redirect, "SecurityAuthorityOAuthClientValidation.ValidateRedirectUri(client, postLogoutRedirectUri, true)");
        Contains(redirect, "return validation.IsValid ? postLogoutRedirectUri : \\\"/\\\";");
    }

    private static void LogoutDoesNotRevokeAccessTokens()
    {
        var logout = GetBlock(SessionSource, "endpoints.MapGet(configuration.LogoutPath", "}).AllowAnonymous();");
        DoesNotContain(logout, "SecurityAuthorityAccessToken");
        DoesNotContain(logout, "RevokeAccess");
        DoesNotContain(logout, "typeof(SecurityAuthorityAccessToken)");
    }

    private static void SessionEndpointSurfaceContainsNoStubs()
    {
        DoesNotContain(SessionSource, "NotImplementedException");
        DoesNotContain(SessionSource, "TODO");
        DoesNotContain(SessionSource, "exampleParam");
    }

    private static string GetBlock(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        True(start >= 0, $"Marker '{startMarker}' was not found.");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        True(end >= 0, $"Marker '{endMarker}' was not found after '{startMarker}'.");
        return source[start..end];
    }

    private static string ReadServiceSource(params string[] path)
    {
        return File.ReadAllText(Path.Combine(new[] { ServiceProject }.Concat(path).ToArray()));
    }

    private static string FindServiceProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Aryzac.Security.Service");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(directory.FullName, "src", "Aryzac.Security.Service");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Aryzac.Security.Service.");
    }

    private static void Contains(string source, string value)
    {
        True(source.Contains(value, StringComparison.Ordinal), $"Expected source to contain '{value}'.");
    }

    private static void DoesNotContain(string source, string value)
    {
        True(!source.Contains(value, StringComparison.Ordinal), $"Expected source not to contain '{value}'.");
    }

    private static void True(bool condition, string message = "Expected condition to be true.")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
