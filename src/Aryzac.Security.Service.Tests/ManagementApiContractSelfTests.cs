using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class ManagementApiContractSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string Source = ReadServiceSource(
        "Templates",
        "SecurityAuthorityManagementEndpoints",
        "SecurityAuthorityManagementEndpointsTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("management routes are fixed at v1 and cover every resource", VersionedRoutesCoverEveryResource),
            ("management routes expose every parity lifecycle action", ParityActionsAreMapped),
            ("every endpoint requires authentication and an action Scope", AuthenticationAndActionScopesAreRequired),
            ("success and failure status contracts are exact", StatusContractsAreExact),
            ("validation errors are field-addressed RFC 9457 problems", ValidationProblemsAreFieldAddressedRfc9457),
            ("paging defaults bounds metadata and deterministic ordering are enforced", PagingContractIsComplete),
            ("idempotency replays outcomes and rejects different requests for 24 hours", IdempotencyContractIsComplete),
            ("concurrency is required and stale writes cannot reach execution", ConcurrencyContractIsComplete),
            ("read list and action projections do not expose secrets", ProjectionsAreSecretSafe),
            ("catalog summary bootstrap and API Key integration are concrete", SpecializedManagementIntegrationIsConcrete),
            ("Role membership and Grant assignment and removal use CRUD parity", AssignmentAndRemovalParityUsesCrud)
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

    private static void VersionedRoutesCoverEveryResource()
    {
        var map = Block(Source, ".AddMethod(\"void\", \"Map\"", ".AddMethod(\"void\", \"MapResource\"");
        Contains(map, "const string root = \"/api/v1/security\";");
        DoesNotContain(map, "/api/v2/");
        foreach (var resource in new[]
        {
            "users", "services", "api-keys", "oauth-clients", "identity-providers",
            "tenant-resources", "roles", "role-memberships", "grants"
        })
        {
            Contains(map, $"MapResource(endpoints, root, \"{resource}\"");
        }
        Contains(map, "MapReadOnlyCollection(endpoints, root, \"grant-catalog\")");
        Contains(map, "MapSingleton(endpoints, root, \"summary\", \"read\")");
        Contains(map, "MapSingleton(endpoints, root, \"bootstrap\", \"read\")");
        Contains(map, "MapCollectionAction(endpoints, root, \"bootstrap\", \"reset\")");
    }

    private static void ParityActionsAreMapped()
    {
        var map = Block(Source, ".AddMethod(\"void\", \"Map\"", ".AddMethod(\"void\", \"MapResource\"");
        Contains(map, "\"users\", new[] { \"activate\", \"suspend\" }");
        Contains(map, "\"services\", new[] { \"activate\", \"suspend\" }");
        Contains(map, "MapCollectionAction(endpoints, root, \"services\", \"provision\")");
        Contains(map, "\"api-keys\", new[] { \"revoke\", \"regenerate\" }");
        Contains(map, "\"oauth-clients\", new[] { \"activate\", \"suspend\", \"regenerate-secret\" }");
        Contains(map, "\"identity-providers\", new[] { \"enable\", \"disable\" }");
        Contains(map, "\"tenant-resources\", new[] { \"provision\" }");
        Contains(map, "\"roles\", new[] { \"enable\", \"disable\" }");
        Contains(map, "\"role-memberships\", new[] { \"revoke\" }");
        Contains(map, "\"grants\", new[] { \"revoke\" }");
    }

    private static void AuthenticationAndActionScopesAreRequired()
    {
        var resource = Block(Source, ".AddMethod(\"void\", \"MapResource\"", ".AddMethod(\"void\", \"MapReadOnlyCollection\"");
        foreach (var action in new[] { "list", "read", "create", "update", "delete" })
        {
            Contains(resource, $"Scope(resource, \"{action}\")");
        }
        Contains(resource, "Scope(resource, action)");
        Contains(Block(Source, ".AddMethod(\"void\", \"MapReadOnlyCollection\"", ".AddMethod(\"void\", \"MapSingleton\""), "Scope(resource, \"list\")");
        Contains(Block(Source, ".AddMethod(\"void\", \"MapSingleton\"", ".AddMethod(\"void\", \"MapCollectionAction\""), "Scope(resource, action)");
        Contains(Block(Source, ".AddMethod(\"void\", \"MapCollectionAction\"", ".AddMethod(\"ValueTask<IResult>\", \"ExecuteAsync\""), "Scope(resource, action)");

        var execute = Block(Source, ".AddMethod(\"ValueTask<IResult>\", \"ExecuteAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult>\", \"ExecuteCoreAsync\"");
        Before(execute, "Authorize(context.User, requiredScope)", "ExecuteCoreAsync(operation, cancellationToken)");
        var authorize = Block(Source, ".AddMethod(\"IResult?\", \"Authorize\"", ".AddMethod(\"IResult\", \"ValidationProblem\"");
        Contains(authorize, "principal.Identity?.IsAuthenticated != true");
        Contains(authorize, "StatusCodes.Status401Unauthorized");
        Contains(authorize, "x.Type, \\\"scope\\\"");
        Contains(authorize, "x.Type, \\\"scp\\\"");
        Contains(authorize, "StatusCodes.Status403Forbidden");
        Contains(authorize, "scopes.Contains(requiredScope, StringComparer.Ordinal)");
        Contains(Source, "return \\\"security.management.\\\" + resource + \\\".\\\" + action;");
    }

    private static void StatusContractsAreExact()
    {
        var execute = Block(Source, ".AddMethod(\"ValueTask<IResult>\", \"ExecuteAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult>\", \"ExecuteCoreAsync\"");
        foreach (var status in new[]
        {
            "Status200OK", "Status201Created", "Status204NoContent", "Status400BadRequest",
            "Status401Unauthorized", "Status403Forbidden", "Status404NotFound", "Status409Conflict"
        })
        {
            Contains(execute, status);
        }
        Contains(execute, "? StatusCodes.Status201Created");
        Contains(execute, "? StatusCodes.Status204NoContent");
        Contains(execute, ": StatusCodes.Status200OK");
        Contains(execute, "Results.NoContent()");
        Contains(execute, "catch (ArgumentException exception)");
        Contains(execute, "catch (KeyNotFoundException exception)");
        Contains(execute, "catch (UnauthorizedAccessException exception)");
        Contains(execute, "catch (InvalidOperationException exception)");
    }

    private static void ValidationProblemsAreFieldAddressedRfc9457()
    {
        var problem = Block(Source, ".AddMethod(\"IResult\", \"Problem\"", ".AddMethod(\"object?\", \"Sanitize\"");
        Contains(problem, "https://www.rfc-editor.org/rfc/rfc9457#name-problem-details");
        foreach (var field in new[] { "type", "title", "status", "detail" })
        {
            Contains(problem, $"[\"{field}\"]");
        }
        Contains(problem, "problem[\"errors\"] = errors");
        Contains(problem, "contentType: \"application/problem+json\"");

        var execute = Block(Source, ".AddMethod(\"ValueTask<IResult>\", \"ExecuteAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult>\", \"ExecuteCoreAsync\"");
        foreach (var field in new[] { "pageNumber", "pageSize", "idempotencyKey", "concurrencyToken" })
        {
            Contains(execute, $"[\"{field}\"]");
        }
        Contains(execute, "[exception.ParamName ?? \"request\"]");
        Contains(Source, "[\"body\"] = new[] { \"A JSON object is required.\" }");
    }

    private static void PagingContractIsComplete()
    {
        Contains(Block(Source, ".AddMethod(\"void\", \"MapResource\"", ".AddMethod(\"void\", \"MapReadOnlyCollection\""), "pageNumber ?? 1, pageSize ?? 25");
        Contains(Block(Source, ".AddMethod(\"void\", \"MapReadOnlyCollection\"", ".AddMethod(\"void\", \"MapSingleton\""), "pageNumber ?? 1, pageSize ?? 25");
        var execute = Block(Source, ".AddMethod(\"ValueTask<IResult>\", \"ExecuteAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult>\", \"ExecuteCoreAsync\"");
        Contains(execute, "pageNumber < 1");
        Contains(execute, "pageSize < 1 || pageSize > 100");
        var normalize = Block(Source, ".AddMethod(\"SecurityAuthorityManagementOperationResult\", \"NormalizeListResult\"", ".AddMethod(\"object\", \"PageElements\"");
        Contains(normalize, "OrderBy(DeterministicOrderKey, StringComparer.Ordinal)");
        Contains(normalize, "totalCount");
        var page = Block(Source, ".AddMethod(\"object\", \"PageElements\"", ".AddMethod(\"string\", \"DeterministicOrderKey\"");
        Contains(page, "items.Skip((pageNumber - 1) * pageSize).Take(pageSize)");
        Contains(page, "new { totalCount, pageNumber, pageSize, items = pageItems }");
        var order = Block(Source, ".AddMethod(\"string\", \"DeterministicOrderKey\"", ".AddMethod(\"IResult?\", \"Authorize\"");
        Contains(order, "\"id\", \"tenantResourceId\", \"clientIdentifier\", \"providerIdentifier\", \"roleKey\", \"permissionKey\", \"name\", \"displayName\"");
        Contains(order, "CanonicalJson(item)");
    }

    private static void IdempotencyContractIsComplete()
    {
        var execute = Block(Source, ".AddMethod(\"ValueTask<IResult>\", \"ExecuteAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult>\", \"ExecuteCoreAsync\"");
        Contains(execute, "ReadOptionalHeader(context, \"Idempotency-Key\")");
        Contains(execute, "idempotencyKey.Length < 1 || idempotencyKey.Length > 200");
        Before(execute, "TryReplayAsync", "ExecuteCoreAsync(operation, cancellationToken)");
        Contains(execute, "StoreOutcomeAsync");
        var supports = Block(Source, ".AddMethod(\"bool\", \"SupportsIdempotency\"", ".AddMethod(\"bool\", \"RequiresConcurrency\"");
        foreach (var action in new[] { "create", "update", "activate", "suspend", "enable", "disable", "revoke", "regenerate", "regenerate-secret", "provision", "reset" })
        {
            Contains(supports, $"\\\"{action}\\\"");
        }
        var replay = Block(Source, ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult?>\", \"TryReplayAsync\"", ".AddMethod(\"ValueTask\", \"StoreOutcomeAsync\"");
        Contains(replay, "stored.ExpiresAt <= _utcNow()");
        Contains(replay, "stored.OperationName");
        Contains(replay, "stored.RequestHash");
        Contains(replay, "different request");
        Contains(replay, "JsonSerializer.Deserialize<SecurityAuthorityManagementOperationResult>");
        var store = Block(Source, ".AddMethod(\"ValueTask\", \"StoreOutcomeAsync\"", ".AddMethod(\"string\", \"OperationName\"");
        Contains(store, "now.AddHours(24)");
        Contains(store, "JsonSerializer.Serialize(result)");
        var fingerprint = Block(Source, ".AddMethod(\"string\", \"Fingerprint\"", ".AddMethod(\"string\", \"CanonicalJson\"");
        Contains(fingerprint, "OperationName(operation)");
        Contains(fingerprint, "concurrencyToken");
        Contains(fingerprint, "CanonicalJson(operation.Body.Value)");
        Contains(fingerprint, "SHA256.HashData");
    }

    private static void ConcurrencyContractIsComplete()
    {
        var requires = Block(Source, ".AddMethod(\"bool\", \"RequiresConcurrency\"", ".AddMethod(\"ValueTask\", \"EnsureCurrentConcurrencyAsync\"");
        foreach (var action in new[] { "update", "activate", "suspend", "enable", "disable", "revoke", "regenerate", "regenerate-secret", "reset" })
        {
            Contains(requires, $"\\\"{action}\\\"");
        }
        var execute = Block(Source, ".AddMethod(\"ValueTask<IResult>\", \"ExecuteAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult>\", \"ExecuteCoreAsync\"");
        Contains(execute, "A concurrency token is required in If-Match or the request body.");
        Before(execute, "EnsureCurrentConcurrencyAsync", "ExecuteCoreAsync(operation, cancellationToken)");
        var concurrency = Block(Source, ".AddMethod(\"ValueTask\", \"EnsureCurrentConcurrencyAsync\"", ".AddMethod(\"Type?\", \"ConcurrencyRecordType\"");
        Contains(concurrency, "if (current is null) throw new KeyNotFoundException");
        Contains(concurrency, "The supplied concurrency token is stale.");
        DoesNotContain(concurrency, "UpdateAsync");
        DoesNotContain(concurrency, "CommitAsync");
    }

    private static void ProjectionsAreSecretSafe()
    {
        Contains(Source, "Sanitize(result.Value, allowOneTimeCredential)");
        var sensitive = Block(Source, ".AddMethod(\"bool\", \"IsSensitive\"", ".AddMethod(\"object\", \"Page\"");
        foreach (var marker in new[] { "privatekey", "correlation", "hash", "encrypted", "password", "credential", "secret", "clientsecret", "clearapikey" })
        {
            Contains(sensitive, marker);
        }
        var allowed = Block(Source, ".AddMethod(\"bool\", \"AllowsOneTimeCredential\"", ".AddMethod(\"string\", \"Title\"");
        Contains(allowed, "\\\"api-keys\\\"");
        Contains(allowed, "\\\"create\\\"");
        Contains(allowed, "\\\"regenerate\\\"");
        Contains(allowed, "\\\"oauth-clients\\\"");
        Contains(allowed, "\\\"regenerate-secret\\\"");
        DoesNotContain(allowed, "\\\"read\\\"");
        DoesNotContain(allowed, "\\\"list\\\"");
    }

    private static void SpecializedManagementIntegrationIsConcrete()
    {
        var core = Block(Source, ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult>\", \"ExecuteCoreAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityManagementOperationResult>\", \"CreateApiKeyManagementResultAsync\"");
        Contains(core, "operation.Resource, \"grant-catalog\"");
        Contains(core, "_dataSource.GetGrantCatalogAsync");
        Contains(core, "OrderBy(x => x.PermissionKey, StringComparer.Ordinal)");
        Contains(core, "operation.Resource, \"api-keys\"");
        Contains(core, "CreateApiKeyManagementResultAsync");
        Contains(core, "RegenerateApiKeyAsync");
        Contains(core, "_managementOperations.ExecuteAsync(operation, cancellationToken)");
        var map = Block(Source, ".AddMethod(\"void\", \"Map\"", ".AddMethod(\"void\", \"MapResource\"");
        Contains(map, "MapSingleton(endpoints, root, \"summary\", \"read\")");
        Contains(map, "MapSingleton(endpoints, root, \"bootstrap\", \"read\")");
        Contains(map, "MapCollectionAction(endpoints, root, \"bootstrap\", \"reset\")");
    }

    private static void AssignmentAndRemovalParityUsesCrud()
    {
        var map = Block(Source, ".AddMethod(\"void\", \"Map\"", ".AddMethod(\"void\", \"MapResource\"");
        Contains(map, "MapResource(endpoints, root, \"role-memberships\"");
        Contains(map, "MapResource(endpoints, root, \"grants\"");
        Contains(map, "\"role-memberships\", new[] { \"revoke\" }");
        Contains(map, "\"grants\", new[] { \"revoke\" }");
        var resource = Block(Source, ".AddMethod(\"void\", \"MapResource\"", ".AddMethod(\"void\", \"MapReadOnlyCollection\"");
        Contains(resource, "endpoints.MapPost(path");
        Contains(resource, "resource, \"create\"");
        Contains(resource, "endpoints.MapDelete(path + \"/{id}\"");
        Contains(resource, "resource, \"delete\"");
    }

    private static string Block(string source, string startMarker, string endMarker)
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
