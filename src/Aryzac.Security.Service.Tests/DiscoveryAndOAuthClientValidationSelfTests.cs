using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class DiscoveryAndOAuthClientValidationSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string DiscoverySource = ReadServiceSource("Templates", "SecurityAuthorityDiscoveryEndpoints", "SecurityAuthorityDiscoveryEndpointsTemplatePartial.cs");
    private static readonly string CryptographySource = ReadServiceSource("Templates", "SecurityAuthorityCryptography", "SecurityAuthorityCryptographyTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("discovery and JWKS endpoints are anonymous and complete", DiscoveryAndJwksEndpointsAreAnonymousAndComplete),
            ("discovery metadata follows enabled features", DiscoveryMetadataFollowsEnabledFeatures),
            ("discovery and token issuance share one issuer", DiscoveryAndTokenIssuanceShareOneIssuer),
            ("redirect URIs are exact absolute and fragment-free", RedirectUrisAreExactAbsoluteAndFragmentFree),
            ("public clients use no secret and require S256 PKCE", PublicClientsUseNoSecretAndRequireS256Pkce),
            ("confidential clients use exactly one authentication method", ConfidentialClientsUseExactlyOneAuthenticationMethod),
            ("inactive clients are rejected at every protocol entry point", InactiveClientsAreRejectedAtEveryProtocolEntryPoint),
            ("discovery and client validation surfaces contain no stubs", DiscoveryAndClientValidationSurfacesContainNoStubs)
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

    private static void DiscoveryAndJwksEndpointsAreAnonymousAndComplete()
    {
        Contains(DiscoverySource, "endpoints.MapGet(configuration.DiscoveryPath, () => Results.Json(CreateDiscoveryDocument(options, configuration))).AllowAnonymous()");
        Contains(DiscoverySource, "endpoints.MapGet(configuration.JwksPath, () => Results.Json(CreateJwksDocument(signingKeys, utcNow()))).AllowAnonymous()");

        foreach (var field in new[]
        {
            "issuer",
            "authorization_endpoint",
            "token_endpoint",
            "userinfo_endpoint",
            "jwks_uri",
            "end_session_endpoint",
            "response_types_supported",
            "grant_types_supported",
            "subject_types_supported",
            "id_token_signing_alg_values_supported",
            "token_endpoint_auth_methods_supported",
            "scopes_supported",
            "claims_supported",
            "code_challenge_methods_supported"
        })
        {
            Contains(DiscoverySource, $"[\\\"{field}\\\"]");
        }

        Contains(DiscoverySource, "signingKeys.GetPublishedVerificationKeys(now)");
        foreach (var field in new[] { "kid", "alg", "kty", "use", "n", "e" })
        {
            Contains(DiscoverySource, $"[\\\"{field}\\\"]");
        }

        Contains(DiscoverySource, "[\\\"use\\\"] = \\\"sig\\\"");
    }

    private static void DiscoveryMetadataFollowsEnabledFeatures()
    {
        Contains(DiscoverySource, "if (configuration.AuthorizationCodeEnabled) grantTypes.Add(\\\"authorization_code\\\")");
        Contains(DiscoverySource, "if (configuration.ClientCredentialsEnabled) grantTypes.Add(\\\"client_credentials\\\")");
        Contains(DiscoverySource, "if (configuration.RefreshTokenEnabled) grantTypes.Add(\\\"refresh_token\\\")");
        Contains(DiscoverySource, "if (configuration.DeviceAuthorizationEnabled) grantTypes.Add(\\\"urn:ietf:params:oauth:grant-type:device_code\\\")");
        Contains(DiscoverySource, "configuration.AuthorizationCodeEnabled ? new[] { \\\"code\\\" } : Array.Empty<string>()");
        Contains(DiscoverySource, "if (configuration.DeviceAuthorizationEnabled)");
        Contains(DiscoverySource, "metadata[\\\"device_authorization_endpoint\\\"] = Endpoint(issuer, configuration.DeviceAuthorizationPath)");
        Contains(DiscoverySource, "new[] { \\\"openid\\\", \\\"profile\\\", \\\"email\\\" }.Concat(configuration.SupportedScopes");
        Contains(DiscoverySource, "new[] { \\\"S256\\\" }");
    }

    private static void DiscoveryAndTokenIssuanceShareOneIssuer()
    {
        Contains(DiscoverySource, "var issuer = GetIssuer(options)");
        Contains(DiscoverySource, "[\\\"issuer\\\"] = issuer");
        Contains(DiscoverySource, "SecurityAuthority:Issuer must be configured as an absolute URI before discovery or token issuance.");
        Contains(DiscoverySource, "return issuer.AbsoluteUri.TrimEnd('/')");
        True(Count(DiscoverySource, "GetIssuer(options)") == 1, "Discovery must resolve its issuer through the shared issuer function exactly once.");
    }

    private static void RedirectUrisAreExactAbsoluteAndFragmentFree()
    {
        Contains(DiscoverySource, "Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri)");
        Contains(DiscoverySource, "!uri.IsAbsoluteUri || !string.IsNullOrEmpty(uri.Fragment)");
        Contains(DiscoverySource, "The redirect URI must be absolute and fragment-free.");
        Contains(DiscoverySource, "postLogout ? client.PostLogoutRedirectUris : client.RedirectUris");
        Contains(DiscoverySource, "registeredUris.Contains(redirectUri, StringComparer.Ordinal)");
        Contains(DiscoverySource, "The redirect URI is not registered for this client.");
        DoesNotContain(DiscoverySource, "StringComparer.OrdinalIgnoreCase) ? Valid() : Invalid(\\\"invalid_request\\\"");
    }

    private static void PublicClientsUseNoSecretAndRequireS256Pkce()
    {
        Contains(DiscoverySource, "string.Equals(client.ClientType, \\\"Public\\\", StringComparison.Ordinal)");
        Contains(DiscoverySource, "Public clients cannot have a client secret.");
        Contains(DiscoverySource, "hasBasic || !string.IsNullOrWhiteSpace(postClientSecret) || !string.IsNullOrEmpty(client.SecretHash)");
        Contains(DiscoverySource, "string.Equals(postClientIdentifier, client.ClientIdentifier, StringComparison.Ordinal) ? Valid() : InvalidClient()");
        Contains(DiscoverySource, "Public clients must redeem Authorization Codes using S256 PKCE.");
        Contains(DiscoverySource, "string.Equals(codeChallengeMethod, \\\"S256\\\", StringComparison.Ordinal)");
        Contains(DiscoverySource, "SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier))");
        Contains(DiscoverySource, "CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes)");
    }

    private static void ConfidentialClientsUseExactlyOneAuthenticationMethod()
    {
        Contains(DiscoverySource, "var hasBasic = !string.IsNullOrWhiteSpace(authorizationHeader)");
        Contains(DiscoverySource, "var hasPost = !string.IsNullOrWhiteSpace(postClientIdentifier) || !string.IsNullOrWhiteSpace(postClientSecret)");
        Contains(DiscoverySource, "if (hasBasic && hasPost) return InvalidClient()");
        Contains(DiscoverySource, "hasBasic == hasPost || string.IsNullOrWhiteSpace(client.SecretHash)");
        Contains(DiscoverySource, "authorizationHeader!.StartsWith(\\\"Basic \\\", StringComparison.OrdinalIgnoreCase)");
        Contains(DiscoverySource, "Convert.FromBase64String(authorizationHeader[6..].Trim())");
        Contains(DiscoverySource, "suppliedIdentifier = postClientIdentifier");
        Contains(DiscoverySource, "suppliedSecret = postClientSecret");
        Contains(DiscoverySource, "credentialHasher.VerifyCredential(suppliedSecret, client.SecretHash) ? Valid() : InvalidClient()");
        Contains(DiscoverySource, "return Invalid(\\\"invalid_client\\\", \\\"Client authentication failed.\\\")");
    }

    private static void InactiveClientsAreRejectedAtEveryProtocolEntryPoint()
    {
        Contains(DiscoverySource, "ValidateActive(client, \\\"authorization\\\", \\\"unauthorized_client\\\")");
        Contains(DiscoverySource, "ValidateActive(client, \\\"device authorization\\\", \\\"unauthorized_client\\\")");
        Contains(DiscoverySource, "ValidateActive(client, \\\"token issuance\\\", \\\"invalid_client\\\")");
        Contains(DiscoverySource, "return client.IsActive ? Valid() : Invalid(error, $\\\"Inactive clients cannot perform {operation}.\\\")");
    }

    private static void DiscoveryAndClientValidationSurfacesContainNoStubs()
    {
        DoesNotContain(DiscoverySource, "NotImplementedException");
        DoesNotContain(DiscoverySource, "TODO");
        DoesNotContain(DiscoverySource, "exampleParam");
        DoesNotContain(CryptographySource, "NotImplementedException");
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

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
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
