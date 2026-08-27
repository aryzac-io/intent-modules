using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class AuthorizationCodeAndTokenEndpointSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string AuthorizationSource = ReadServiceSource("Templates", "SecurityAuthorityAuthorizationEndpoints", "SecurityAuthorityAuthorizationEndpointsTemplatePartial.cs");
    private static readonly string TokenSource = ReadServiceSource("Templates", "SecurityAuthorityTokenEndpoint", "SecurityAuthorityTokenEndpointTemplatePartial.cs");
    private static readonly string ProviderSource = ReadServiceSource("Templates", "SecurityAuthorityExternalProviders", "SecurityAuthorityExternalProvidersTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("authorize validates required client response Scope and S256 inputs", AuthorizeValidatesProtocolInputs),
            ("authorize selects an eligible provider and requires an absolute redirect", AuthorizeSelectsProviderAndValidatesRedirect),
            ("authorization state expires once and is persistently consumed", AuthorizationStateExpiresAndCannotBeReused),
            ("authorization callbacks support GET query and POST form_post", AuthorizationCallbacksSupportQueryAndFormPost),
            ("authorization redirects preserve exact registered URI and escaped state", AuthorizationRedirectsAreExact),
            ("Scope checks and contextual claim pre-authorization are case-sensitive", ScopeAndClaimFilteringAreCaseSensitive),
            ("Authorization Codes are bound and fail closed during redemption", AuthorizationCodesAreBoundAndFailClosed),
            ("Authorization Code requests and redemption enforce S256", AuthorizationCodeFlowEnforcesS256),
            ("token claims and lifetimes come from configured contracts", TokenClaimsAndDurationsUseConfiguration),
            ("Client Credentials is confidential scoped and service-principal only", ClientCredentialsIsConstrained),
            ("grant dispatch is feature gated and form-urlencoded only", GrantDispatchAndMediaTypeAreGated),
            ("OAuth errors map exactly to HTTP 400 and 401", OAuthErrorsMapToExactStatusCodes),
            ("authorization and token surfaces contain no stubs", ProtocolSurfacesContainNoStubs)
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

    private static void AuthorizeValidatesProtocolInputs()
    {
        var authorize = AuthorizeBlock();
        Contains(authorize, "string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(redirect_uri) || string.IsNullOrWhiteSpace(response_type)");
        Contains(authorize, "OAuthError(\"invalid_request\", \"client_id, redirect_uri, and response_type are required.\")");
        Contains(authorize, "ValidateAuthorizationClient(client, redirect_uri)");
        Contains(authorize, "string.Equals(response_type, \"code\", StringComparison.Ordinal)");
        Contains(authorize, "client.AllowedGrantTypes.Contains(\"authorization_code\", StringComparer.Ordinal)");
        Contains(authorize, "scopes.Any(requested => client.AllowedScopes is null || !client.AllowedScopes.Contains(requested, StringComparer.Ordinal))");
        Contains(authorize, "string.IsNullOrWhiteSpace(code_challenge) || !string.Equals(code_challenge_method, \"S256\", StringComparison.Ordinal)");
    }

    private static void AuthorizeSelectsProviderAndValidatesRedirect()
    {
        var authorize = AuthorizeBlock();
        Contains(authorize, "findIdentityProviders(client.TenantId, cancellationToken)");
        Contains(authorize, "SecurityAuthorityExternalProviders.SelectProvider(providers, client.PreferredIdentityProviderId, client.TenantId)");
        Contains(authorize, "No active Identity Provider is eligible for this client.");
        Contains(authorize, "buildProviderRedirect(provider, callbackUri, protectedState, providerNonce, cancellationToken)");
        Contains(authorize, "Uri.TryCreate(providerRedirect, UriKind.Absolute, out var providerRedirectUri) || !providerRedirectUri.IsAbsoluteUri");
        Contains(authorize, "return Results.Redirect(providerRedirect)");
    }

    private static void AuthorizationStateExpiresAndCannotBeReused()
    {
        var callback = CallbackBlock();
        Contains(AuthorizationSource, "StateLifetimeMinutes = 10;");
        Contains(AuthorizationSource, "now.AddMinutes(configuration.StateLifetimeMinutes)");
        Contains(callback, "protectedState.ExpiresAt <= now || protectedState.ConsumedAt is not null");
        Contains(callback, "persistedState is null || persistedState.ConsumedAt is not null || persistedState.ExpiresAt <= now || !StateMatches(protectedState, persistedState)");
        Contains(callback, "persistedState with { ConsumedAt = now, ConcurrencyToken = NewOpaqueIdentifier() }");
        Contains(callback, "operation.Records.UpdateAsync(consumedState, persistedState.ConcurrencyToken, cancellationToken)");
        Contains(AuthorizationSource, "configuration.StateLifetimeMinutes != 10");
    }

    private static void AuthorizationCallbacksSupportQueryAndFormPost()
    {
        Contains(AuthorizationSource, "endpoints.MapMethods(configuration.CallbackPath, new[] { HttpMethods.Get, HttpMethods.Post }");
        Contains(AuthorizationSource, "SecurityAuthorityExternalProviders.ReadCallbackAsync(context.Request, cancellationToken)");
        Contains(ProviderSource, "HttpMethods.IsGet(request.Method)");
        Contains(ProviderSource, "request.Query[\\\"code\\\"]");
        Contains(ProviderSource, "request.Query[\\\"state\\\"]");
        Contains(ProviderSource, "HttpMethods.IsPost(request.Method) && request.HasFormContentType");
        Contains(ProviderSource, "await request.ReadFormAsync(cancellationToken)");
        Contains(ProviderSource, "form[\\\"code\\\"]");
        Contains(ProviderSource, "form[\\\"state\\\"]");
        Contains(ProviderSource, "GET query or POST form_post");
    }

    private static void AuthorizationRedirectsAreExact()
    {
        var authorize = AuthorizeBlock();
        var callback = CallbackBlock();
        var errorRedirect = GetBlock(AuthorizationSource, ".AddMethod(\"string\", \"BuildErrorRedirect\"", ".AddMethod(\"string\", \"BuildSuccessRedirect\"");
        var successRedirect = GetBlock(AuthorizationSource, ".AddMethod(\"string\", \"BuildSuccessRedirect\"", ".AddMethod(\"void\", \"ValidateConfiguration\"");
        Contains(authorize, "ValidateAuthorizationClient(client, redirect_uri)");
        Contains(callback, "ValidateAuthorizationClient(client, protectedState.RedirectUri)");
        Contains(errorRedirect, "redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?'");
        Contains(errorRedirect, "Uri.EscapeDataString(error)");
        Contains(errorRedirect, "Uri.EscapeDataString(description)");
        Contains(errorRedirect, "Uri.EscapeDataString(returnState)");
        Contains(successRedirect, "redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?'");
        Contains(successRedirect, "Uri.EscapeDataString(code)");
        Contains(successRedirect, "Uri.EscapeDataString(returnState)");
        DoesNotContain(errorRedirect + successRedirect, "OrdinalIgnoreCase");
    }

    private static void ScopeAndClaimFilteringAreCaseSensitive()
    {
        Contains(AuthorizationSource, "client.AllowedScopes.Contains(requested, StringComparer.Ordinal)");
        Contains(AuthorizationSource, "values.Distinct(StringComparer.Ordinal)");
        Contains(AuthorizationSource, "protectedState.Scopes.SequenceEqual(persistedState.Scopes, StringComparer.Ordinal)");
        Contains(TokenSource, "client.AllowedScopes.Contains(scope, StringComparer.Ordinal)");
        Contains(TokenSource, "configuration.ContextualClaimNames.Contains(claim.Key, StringComparer.Ordinal)");
        Contains(TokenSource, "ToDictionary(claim => claim.Key, claim => claim.Value, StringComparer.Ordinal)");
        Contains(TokenSource, "if (!claims.ContainsKey(claim.Key)) claims.Add(claim.Key, claim.Value)");
    }

    private static void AuthorizationCodesAreBoundAndFailClosed()
    {
        var redeem = GetBlock(TokenSource, ".AddMethod(\"ValueTask<IResult>\", \"RedeemAuthorizationCodeAsync\"", ".AddMethod(\"ValueTask<IResult>\", \"RedeemRefreshTokenAsync\"");
        Contains(redeem, "credentialHasher.VerifyCredential(clearCode, code.CodeHash)");
        Contains(redeem, "code.ExpiresAt <= now");
        Contains(redeem, "code.RedeemedAt is not null");
        Contains(redeem, "string.Equals(code.OAuthClientId, client.Id, StringComparison.Ordinal)");
        Contains(redeem, "string.Equals(code.RedirectUri, redirectUri, StringComparison.Ordinal)");
        Contains(redeem, "return OAuthError(context, \\\"invalid_grant\\\", \\\"The Authorization Code is invalid, expired, or already redeemed.\\\")");
        Contains(redeem, "code! with { RedeemedAt = now, ConcurrencyToken = NewOpaqueIdentifier() }");
        Contains(redeem, "BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.TokenRedemption, true, cancellationToken)");
        Contains(redeem, "await operation.RollbackAsync(cancellationToken)");
        Contains(redeem, "receipt = await operation.CommitAsync(cancellationToken)");
    }

    private static void AuthorizationCodeFlowEnforcesS256()
    {
        Contains(AuthorizationSource, "!string.Equals(code_challenge_method, \"S256\", StringComparison.Ordinal)");
        Contains(AuthorizationSource, "code_challenge, \"S256\", nonce, state");
        Contains(TokenSource, "ValidateAuthorizationCodeRedemption(client, redirectUri, codeVerifier, code!.PkceChallenge, \\\"S256\\\")");
        Contains(TokenSource, "code, redirect_uri, and code_verifier are required.");
    }

    private static void TokenClaimsAndDurationsUseConfiguration()
    {
        Contains(TokenSource, "AccessTokenMinutes = {accessTokenMinutes.ToString(CultureInfo.InvariantCulture)};");
        Contains(TokenSource, "IdTokenMinutes = {idTokenMinutes.ToString(CultureInfo.InvariantCulture)};");
        Contains(TokenSource, "RefreshTokenDays = {refreshTokenDays.ToString(CultureInfo.InvariantCulture)};");
        Contains(TokenSource, "now.AddMinutes(configuration.AccessTokenMinutes)");
        Contains(TokenSource, "now.AddMinutes(configuration.IdTokenMinutes)");
        Contains(TokenSource, "now.AddDays(configuration.RefreshTokenDays)");
        foreach (var claim in new[] { "iss", "aud", "sub", "principal_type", "scope", "iat", "nbf", "exp", "jti" })
        {
            Contains(TokenSource, $"[\\\"{claim}\\\"]");
        }

        Contains(TokenSource, "claims.Add(\\\"nonce\\\", nonce)");
        Contains(TokenSource, "[\\\"alg\\\"] = \\\"RS256\\\"");
        Contains(TokenSource, "[\\\"kid\\\"] = configuration.SigningKeyId");
        Contains(TokenSource, "[\\\"typ\\\"] = \\\"JWT\\\"");
        Contains(TokenSource, "configuration.AccessTokenMinutes * 60");
        Contains(TokenSource, "var idToken = CreateIdToken(configuration, signingKeys, client.ClientIdentifier, redeemedCode.UserId, redeemedCode.Nonce, now)");
        Contains(TokenSource, "var refreshToken = deferredRefreshToken!.Reveal(receipt)");
        DoesNotContain(TokenSource, "redeemedCode.Scopes.Contains(\\\"openid\\\", StringComparer.Ordinal) ? CreateIdToken");
        DoesNotContain(TokenSource, "configuration.RefreshTokenEnabled && client.AllowedGrantTypes.Contains(\\\"refresh_token\\\", StringComparer.Ordinal)");
    }

    private static void ClientCredentialsIsConstrained()
    {
        var clientCredentials = GetBlock(TokenSource, ".AddMethod(\"ValueTask<IResult>\", \"IssueClientCredentialsAsync\"", ".AddMethod(\"ValueTask<IReadOnlyDictionary<string, string>>\", \"ResolveContextClaimsAsync\"");
        Contains(clientCredentials, "string.Equals(client.ClientType, \\\"Confidential\\\", StringComparison.Ordinal)");
        Contains(clientCredentials, "client.IsActive");
        Contains(clientCredentials, "client.AllowedGrantTypes.Contains(\\\"client_credentials\\\", StringComparer.Ordinal)");
        Contains(clientCredentials, "client.AllowedScopes.Contains(scope, StringComparer.Ordinal)");
        Contains(clientCredentials, "OAuthError(context, \\\"invalid_client\\\", \\\"Client Credentials requires an active Confidential client.\\\", StatusCodes.Status401Unauthorized)");
        Contains(clientCredentials, "OAuthError(context, \\\"invalid_scope\\\", \\\"The client is not configured for Client Credentials.\\\")");
        DoesNotContain(clientCredentials, "unauthorized_client");
        Contains(clientCredentials, "ResolveContextClaimsAsync(configuration, resolveContextClaims, \\\"Service\\\", client.Id, scopes, cancellationToken)");
        Contains(clientCredentials, "CreateAccessToken(configuration, signingKeys, client.Id, \\\"Service\\\", client.ClientIdentifier, scopes, contextClaims, now)");
        Contains(clientCredentials, "TokenResponse(accessToken, configuration.AccessTokenMinutes * 60, scopes, null, null)");
    }

    private static void GrantDispatchAndMediaTypeAreGated()
    {
        var map = GetBlock(TokenSource, ".AddMethod(\"void\", \"Map\"", ".AddMethod(\"ValueTask<IResult>\", \"RedeemAuthorizationCodeAsync\"");
        Contains(map, "context.Request.ContentType?.Split(';', StringSplitOptions.TrimEntries).FirstOrDefault()");
        Contains(map, "string.Equals(mediaType, \"application/x-www-form-urlencoded\", StringComparison.OrdinalIgnoreCase)");
        Contains(map, "The token endpoint accepts only application/x-www-form-urlencoded requests.");
        Contains(map, "\"authorization_code\" when configuration.AuthorizationCodeEnabled");
        Contains(map, "\"client_credentials\" when configuration.ClientCredentialsEnabled");
        Contains(map, "\"refresh_token\" when configuration.RefreshTokenEnabled");
        Contains(map, "\"urn:ietf:params:oauth:grant-type:device_code\" when configuration.DeviceCodeEnabled");
        Contains(map, "The requested grant type is disabled.");
        Contains(map, "The requested grant type is not supported.");
    }

    private static void OAuthErrorsMapToExactStatusCodes()
    {
        var authorizationError = GetBlock(AuthorizationSource, ".AddMethod(\"IResult\", \"OAuthError\"", ".AddMethod(\"string\", \"BuildErrorRedirect\"");
        var tokenError = GetBlock(TokenSource, ".AddMethod(\"IResult\", \"OAuthError\"", ".AddMethod(\"bool\", \"TryReadClientIdentifier\"");
        Contains(authorizationError, "Results.BadRequest(new { error, error_description = description })");
        Contains(tokenError, "WithDefaultValue(\"StatusCodes.Status400BadRequest\")");
        Contains(tokenError, "statusCode == StatusCodes.Status401Unauthorized");
        Contains(tokenError, "context.Response.Headers.WWWAuthenticate = \\\"Basic realm=\\\\\\\"token\\\\\\\"\\\"");
        Contains(tokenError, "Results.Json(new { error, error_description = description }, statusCode: statusCode)");
        Contains(TokenSource, "OAuthError(context, \"invalid_client\", \"Client authentication failed.\", StatusCodes.Status401Unauthorized)");
    }

    private static void ProtocolSurfacesContainNoStubs()
    {
        foreach (var source in new[] { AuthorizationSource, TokenSource })
        {
            DoesNotContain(source, "NotImplementedException");
            DoesNotContain(source, "TODO");
            DoesNotContain(source, "exampleParam");
        }
    }

    private static string AuthorizeBlock()
    {
        return GetBlock(AuthorizationSource, "endpoints.MapGet(configuration.AuthorizationPath", "endpoints.MapMethods(configuration.CallbackPath");
    }

    private static string CallbackBlock()
    {
        return GetBlock(AuthorizationSource, "endpoints.MapMethods(configuration.CallbackPath", ").AllowAnonymous();\n                            \"\"\"");
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
