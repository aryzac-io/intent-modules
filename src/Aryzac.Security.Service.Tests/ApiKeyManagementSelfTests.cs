using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class ApiKeyManagementSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string ManagementSource = ReadServiceSource("Templates", "SecurityAuthorityManagementEndpoints", "SecurityAuthorityManagementEndpointsTemplatePartial.cs");
    private static readonly string LifecycleSource = ReadServiceSource("Templates", "SecurityAuthorityLifecycle", "SecurityAuthorityLifecycleTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("API Keys require exactly one active owner", ApiKeysRequireExactlyOneActiveOwner),
            ("direct Scopes become API Key Allow Grants", DirectScopesBecomeApiKeyAllowGrants),
            ("clear API Keys are prefixed random and one-time", ClearApiKeysArePrefixedRandomAndOneTime),
            ("API Keys use keyed constant-time verification", ApiKeysUseKeyedConstantTimeVerification),
            ("regeneration atomically revokes and replaces", RegenerationAtomicallyRevokesAndReplaces),
            ("failed authentication never updates last-used", FailedAuthenticationNeverUpdatesLastUsed),
            ("successful authentication updates last-used", SuccessfulAuthenticationUpdatesLastUsed),
            ("Principal Scopes combine Allows and subtract Denies", PrincipalScopesCombineAllowsAndSubtractDenies),
            ("API Key management contains no stubs", ApiKeyManagementContainsNoStubs)
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

    private static void ApiKeysRequireExactlyOneActiveOwner()
    {
        var create = CreateBlock();
        Contains(create, "if ((userId is null) == (serviceId is null))");
        Contains(create, "An API Key must have exactly one User or Service owner.");
        Contains(ManagementSource, "SecurityAuthorityLifecycle.IsActiveUser(user)");
        Contains(ManagementSource, "SecurityAuthorityLifecycle.IsActiveService(service)");
        Contains(create, "GetActiveOwnerTenantIdAsync(ownerType, ownerId, cancellationToken)");
    }

    private static void DirectScopesBecomeApiKeyAllowGrants()
    {
        Contains(ManagementSource, ".AddRecord(\"SecurityAuthorityApiKeyScope\"");
        var create = CreateBlock();
        Contains(create, "foreach (var scope in scopes.Distinct())");
        Contains(create, "ToGrant(apiKey, scope, now)");
        var grant = GetBlock(ManagementSource, ".AddMethod(\"SecurityAuthorityGrant\", \"ToGrant\"", ".AddMethod(\"string\", \"CreateClearApiKey\"");
        Contains(grant, "new SecurityAuthorityGrant(_newId(), \\\"ApiKey\\\", apiKey.Id");
        Contains(grant, "scope.PermissionKey, \\\"Allow\\\", scope.Applicability");
    }

    private static void ClearApiKeysArePrefixedRandomAndOneTime()
    {
        var createClear = GetBlock(ManagementSource, ".AddMethod(\"string\", \"CreateClearApiKey\"", ".AddMethod(\"bool\", \"TryReadApiKeyId\"");
        Contains(createClear, "RandomNumberGenerator.GetBytes(32)");
        Contains(createClear, "SecurityAuthorityBase64Url.Encode");
        Contains(createClear, "_configuredPrefix + \\\".\\\" + apiKeyId + \\\".\\\" + secret");
        Contains(ManagementSource, "string.IsNullOrWhiteSpace(configuredPrefix)");
        Contains(ManagementSource, "configuredPrefix.Any(char.IsWhiteSpace)");
        True(Count(ManagementSource, "deferred.Reveal(receipt)") == 2);
        Contains(ManagementSource, "SecurityAuthorityDeferredCredential");
        Before(CreateBlock(), "operation.CommitAsync(cancellationToken)", "deferred.Reveal(receipt)");
        Before(RegenerateBlock(), "operation.CommitAsync(cancellationToken)", "deferred.Reveal(receipt)");
    }

    private static void ApiKeysUseKeyedConstantTimeVerification()
    {
        Contains(ManagementSource, "_credentialHasher.HashApiKey(clearApiKey)");
        Contains(ManagementSource, "_credentialHasher.VerifyApiKey(clearApiKey, apiKey.KeyHash)");
        Contains(ManagementSource, "_credentialHasher.VerifyApiKey(clearApiKey, current.KeyHash)");
        Contains(ManagementSource, "SecurityAuthorityCredentialHasher");
        Contains(ManagementSource, "KeyHash");
        DoesNotContain(ManagementSource, "ApiKeySecret");
        DoesNotContain(ManagementSource, "ClearApiKey { get;");
    }

    private static void RegenerationAtomicallyRevokesAndReplaces()
    {
        var regenerate = RegenerateBlock();
        Contains(regenerate, "SecurityAuthorityAtomicOperationKind.ApiKeyRegeneration");
        Contains(regenerate, "var revokedPrevious = apiKey with");
        Contains(regenerate, "IsRevoked = true");
        Contains(regenerate, "var replacement = revokedPrevious with");
        Contains(regenerate, "KeyHash = _credentialHasher.HashApiKey(clearApiKey)");
        Before(regenerate, "operation.Records.UpdateAsync(replacement", "operation.CommitAsync(cancellationToken)");
        Before(regenerate, "operation.CommitAsync(cancellationToken)", "deferred.Reveal(receipt)");
    }

    private static void FailedAuthenticationNeverUpdatesLastUsed()
    {
        var authenticate = AuthenticateBlock();
        Contains(authenticate, "if (!TryReadApiKeyId(clearApiKey, out var apiKeyId))");
        Contains(authenticate, "apiKey is null ||");
        Contains(authenticate, "!_credentialHasher.VerifyApiKey(clearApiKey, apiKey.KeyHash)");
        Contains(authenticate, "SecurityAuthorityLifecycle.CanUseApiKey(apiKey, user, service, now)");
        Contains(authenticate, "SecurityAuthorityLifecycle.CanUseApiKey(current, currentUser, currentService, now)");
        Before(authenticate, "if (!TryReadApiKeyId", "LastUsedAt = now");
        Before(authenticate, "apiKey is null", "LastUsedAt = now");
        Before(authenticate, "VerifyApiKey(clearApiKey, apiKey.KeyHash)", "LastUsedAt = now");
        Before(authenticate, "SecurityAuthorityLifecycle.CanUseApiKey(apiKey, user, service, now)", "LastUsedAt = now");
        Before(authenticate, "SecurityAuthorityLifecycle.CanUseApiKey(current, currentUser, currentService, now)", "LastUsedAt = now");
        True(Count(authenticate, "return null;") >= 5);

        Contains(LifecycleSource, "apiKey.IsRevoked || apiKey.ExpiresAt is not null && apiKey.ExpiresAt <= now");
        Contains(LifecycleSource, "return IsLivePrincipal(apiKey.OwnerPrincipalType, apiKey.OwnerId, user, service)");
    }

    private static void SuccessfulAuthenticationUpdatesLastUsed()
    {
        var authenticate = AuthenticateBlock();
        Contains(authenticate, "LastUsedAt = now");
        Contains(authenticate, "operation.Records.UpdateAsync(updated, current.ConcurrencyToken, cancellationToken)");
        Before(authenticate, "operation.Records.UpdateAsync(updated", "operation.CommitAsync(cancellationToken)");
        Before(authenticate, "operation.CommitAsync(cancellationToken)", "new SecurityAuthorityApiKeyPrincipal");
    }

    private static void PrincipalScopesCombineAllowsAndSubtractDenies()
    {
        var scopes = GetBlock(ManagementSource, ".AddMethod(\"ValueTask<IReadOnlyList<string>>\", \"GetPrincipalScopesAsync\"", ".AddMethod(\"ValueTask<string?>\", \"GetActiveOwnerTenantIdAsync\"");
        Contains(scopes, "GetGrantsAsync(apiKey.OwnerPrincipalType, apiKey.OwnerId");
        Contains(scopes, "GetRoleMembershipsAsync(apiKey.OwnerPrincipalType, apiKey.OwnerId");
        Contains(scopes, "GetGrantsAsync(\"Role\", role.Id");
        Contains(scopes, "GetGrantsAsync(\"ApiKey\", apiKey.Id");
        Contains(scopes, "_authorizationEngine.AuthorizeAsync(");
        Contains(scopes, "string.Equals(x.Grant.Effect, \"Allow\"");
        Contains(scopes, "string.Equals(x.Grant.Effect, \"Deny\"");
        Before(scopes, "var denied = active", "allowed.ExceptWith(denied)");
        Contains(scopes, "return allowed.OrderBy(x => x, StringComparer.Ordinal).ToArray()");
    }

    private static void ApiKeyManagementContainsNoStubs()
    {
        DoesNotContain(ManagementSource, "NotImplementedException");
        DoesNotContain(ManagementSource, "TODO");
        DoesNotContain(ManagementSource, "exampleParam");
    }

    private static string CreateBlock() =>
        GetBlock(ManagementSource, ".AddMethod(\"ValueTask<SecurityAuthorityIssuedApiKey>\", \"CreateApiKeyAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityIssuedApiKey>\", \"RegenerateApiKeyAsync\"");

    private static string RegenerateBlock() =>
        GetBlock(ManagementSource, ".AddMethod(\"ValueTask<SecurityAuthorityIssuedApiKey>\", \"RegenerateApiKeyAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityApiKeyPrincipal?>\", \"AuthenticateApiKeyAsync\"");

    private static string AuthenticateBlock() =>
        GetBlock(ManagementSource, ".AddMethod(\"ValueTask<SecurityAuthorityApiKeyPrincipal?>\", \"AuthenticateApiKeyAsync\"", ".AddMethod(\"ValueTask<IReadOnlyList<string>>\", \"GetPrincipalScopesAsync\"");

    private static string GetBlock(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        True(start >= 0, $"Marker '{startMarker}' was not found.");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        True(end >= 0, $"Marker '{endMarker}' was not found after '{startMarker}'.");
        return source[start..end];
    }

    private static string ReadServiceSource(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { ServiceProject }.Concat(path).ToArray()));

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

            var sourceCandidate = Path.Combine(directory.FullName, "src", "Aryzac.Security.Service");
            if (Directory.Exists(sourceCandidate))
            {
                return sourceCandidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Aryzac.Security.Service project directory.");
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

    private static void Before(string source, string first, string second)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);
        True(firstIndex >= 0, $"Expected to find '{first}'.");
        True(secondIndex >= 0, $"Expected to find '{second}'.");
        True(firstIndex < secondIndex, $"Expected '{first}' before '{second}'.");
    }

    private static void Contains(string source, string value) =>
        True(source.Contains(value, StringComparison.Ordinal), $"Expected to find '{value}'.");

    private static void DoesNotContain(string source, string value) =>
        True(!source.Contains(value, StringComparison.Ordinal), $"Did not expect to find '{value}'.");

    private static void True(bool condition, string message = "Expected condition to be true.")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
