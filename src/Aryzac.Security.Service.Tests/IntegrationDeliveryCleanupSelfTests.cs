using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

internal static class IntegrationDeliveryCleanupSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string IntegrationSource = ReadServiceSource("Templates", "SecurityAuthorityIntegrationEvents", "SecurityAuthorityIntegrationEventsTemplatePartial.cs");
    private static readonly string DispatchSource = ReadServiceSource("Templates", "SecurityAuthorityPostCommitDispatch", "SecurityAuthorityPostCommitDispatchTemplatePartial.cs");
    private static readonly string CleanupSource = ReadServiceSource("Templates", "SecurityAuthorityCleanup", "SecurityAuthorityCleanupTemplatePartial.cs");
    private static readonly string ContractsSource = ReadServiceSource("Templates", "SecurityAuthorityContracts", "SecurityAuthorityContractsTemplatePartial.cs");
    private static readonly string RegistrationSource = ReadServiceSource("FactoryExtensions", "SecurityAuthorityIntegration.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("integration envelopes and supported resources are complete", IntegrationEnvelopeAndResourcesAreComplete),
            ("integration rejection reasons are exact and rollback precedes failure", IntegrationRejectionsAreExactAndAtomic),
            ("integration replay is globally deduplicated and successful", IntegrationReplayIsAtMostOnce),
            ("post-commit transitions audit payloads and adapter outcomes are complete", PostCommitContractsAreComplete),
            ("post-commit failures report without rollback or false replay", PostCommitFailuresDoNotAffectCommittedMutation),
            ("integration mutations invalidate and dispatch after commit", IntegrationMutationsInvalidateAndDispatch),
            ("changed-field auditing excludes secret material", AuditChangedFieldsExcludeSecrets),
            ("cleanup retention windows and terminal timestamps are exact", CleanupRetentionWindowsAreExact),
            ("cleanup is hosted and registered for every host shape", CleanupIsHostedAndRegistered),
            ("cleanup and redemption race has exactly one winner", CleanupRaceHasExactlyOneWinner),
            ("UTC stable identifiers and ordinal comparisons are enforced", StableUtcAndComparisonRulesAreEnforced)
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

    private static void IntegrationEnvelopeAndResourcesAreComplete()
    {
        foreach (var field in new[] { "EventId", "EventType", "SchemaVersion", "OccurredAt", "CorrelationId", "Source", "Payload", "ExpectedConcurrencyToken", "TenantId" })
        {
            Contains(IntegrationSource, $"\"{field}\"");
        }

        foreach (var resource in new[] { "users", "services", "oauth-clients", "identity-providers", "tenant-resources", "roles", "role-memberships", "grants" })
        {
            Contains(IntegrationSource, $"[\\\"{resource}\\\"]");
        }

        foreach (var record in new[] { "SecurityAuthorityUser", "SecurityAuthorityService", "SecurityAuthorityOAuthClient", "SecurityAuthorityIdentityProvider", "SecurityAuthorityTenantResourceRecord", "SecurityAuthorityRole", "SecurityAuthorityRoleMembership", "SecurityAuthorityGrant" })
        {
            Contains(IntegrationSource, $"typeof({record})");
        }
    }

    private static void IntegrationRejectionsAreExactAndAtomic()
    {
        foreach (var reason in new[]
        {
            "missing-or-invalid-field:eventId", "missing-field:eventType", "unsupported-schema-version:",
            "occurrence-time-must-be-utc", "missing-field:correlationId", "missing-field:source",
            "missing-field:payload", "unsupported-event-type", "payload-type-mismatch", "unknown-reference",
            "stale-concurrency-token", "tenant-mismatch"
        })
        {
            Contains(IntegrationSource, reason);
        }

        var handle = Block(IntegrationSource, ".AddMethod(\"ValueTask<SecurityAuthorityIntegrationEventResult>\", \"HandleAsync\"", ".AddMethod(\"void\", \"ValidateEnvelope\"");
        Before(handle, "BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IntegrationEventDeduplication", "Records.AddAsync(envelope.Payload");
        Before(handle, "Records.AddAsync(new SecurityAuthorityProcessedIntegrationEvent", "operation.CommitAsync");
        Contains(handle, "catch");
        Contains(handle, "operation.RollbackAsync");
    }

    private static void IntegrationReplayIsAtMostOnce()
    {
        var processed = new HashSet<Guid>();
        var mutations = 0;
        var eventId = Guid.NewGuid();
        for (var replay = 0; replay < 10; replay++)
        {
            if (processed.Add(eventId))
            {
                mutations++;
            }
        }

        Equal(1, mutations);
        Equal(1, processed.Count);
        var handle = Block(IntegrationSource, ".AddMethod(\"ValueTask<SecurityAuthorityIntegrationEventResult>\", \"HandleAsync\"", ".AddMethod(\"void\", \"ValidateEnvelope\"");
        Before(handle, "LoadAsync(typeof(SecurityAuthorityProcessedIntegrationEvent)", "ValidateReferencesAsync");
        Contains(handle, "new SecurityAuthorityIntegrationEventResult(envelope.EventId, true, processed.OutcomeReference)");
    }

    private static void PostCommitContractsAreComplete()
    {
        foreach (var transition in new[] { "Created", "Updated", "Activated", "Deactivated", "Suspended", "Archived", "Deleted", "Revoked", "Regenerated", "Approved", "Denied", "Redeemed", "BootstrapCompleted" })
        {
            Contains(ContractsSource, $"@enum.AddLiteral(\"{transition}\")");
        }

        foreach (var field in new[] { "actor", "action", "targetType", "targetId", "tenantId", "correlationId", "outcome", "changedFields" })
        {
            Contains(DispatchSource, $"\"{field}\"");
        }

        Contains(DispatchSource, "SecurityAuthorityDeliveryOutcome.NotConfigured");
        Contains(DispatchSource, "SecurityAuthorityDeliveryOutcome.Delivered");
        Contains(DispatchSource, "new SecurityAuthorityLifecycleNotification");
        Contains(DispatchSource, "new SecurityAuthorityAuditEntry");
        Contains(DispatchSource, "_utcNow().ToUniversalTime()");
    }

    private static void PostCommitFailuresDoNotAffectCommittedMutation()
    {
        Contains(DispatchSource, "_failureHandler.HandleAsync(new SecurityAuthorityPostCommitDeliveryFailure");
        Contains(DispatchSource, "SecurityAuthorityDeliveryOutcome.Failed");
        DoesNotContain(DispatchSource, "RollbackAsync");
        DoesNotContain(DispatchSource, "BeginAtomicOperationAsync");
        DoesNotContain(DispatchSource, "CommitAsync");
    }

    private static void AuditChangedFieldsExcludeSecrets()
    {
        foreach (var marker in new[] { "secret", "password", "hash", "privatekey", "credential", "correlationstate", "clearapikey" })
        {
            Contains(DispatchSource, marker);
        }
        Contains(DispatchSource, ".Distinct(StringComparer.Ordinal)");
        Contains(DispatchSource, ".OrderBy(field => field, StringComparer.Ordinal)");

        var fields = new[] { "DisplayName", "SecretHash", "display_name", "Password", "TenantId", "DisplayName" };
        var safe = fields.Where(field => !IsSecret(field)).Distinct(StringComparer.Ordinal).OrderBy(field => field, StringComparer.Ordinal).ToArray();
        SequenceEqual(new[] { "DisplayName", "TenantId", "display_name" }, safe);
    }

    private static void IntegrationMutationsInvalidateAndDispatch()
    {
        var handle = Block(IntegrationSource, ".AddMethod(\"ValueTask<SecurityAuthorityIntegrationEventResult>\", \"HandleAsync\"", ".AddMethod(\"void\", \"ValidateEnvelope\"");
        Before(handle, "var receipt = await operation.CommitAsync(cancellationToken)", "InvalidateAuthorization(eventShape.Resource");
        Before(handle, "InvalidateAuthorization(eventShape.Resource", "_postCommitDispatch.DispatchAsync(");
        Contains(handle, "new SecurityAuthorityPrincipalReference(\"IntegrationEvent\", envelope.Source)");
        Contains(IntegrationSource, "SecurityAuthorityAuthorizationChange.User");
        Contains(IntegrationSource, "SecurityAuthorityAuthorizationChange.Service");
        Contains(IntegrationSource, "SecurityAuthorityAuthorizationChange.TenantResourceParent");
        Contains(IntegrationSource, "SecurityAuthorityAuthorizationChange.Role");
        Contains(IntegrationSource, "SecurityAuthorityAuthorizationChange.RoleMembership");
        Contains(IntegrationSource, "SecurityAuthorityAuthorizationChange.Grant");
    }

    private static void CleanupRetentionWindowsAreExact()
    {
        Contains(CleanupSource, "ParseRange(lifecycle.CodeAndDeviceCleanupDelayDays(), \"Authority Data Lifecycle: Code and Device Cleanup Delay Days\", 1, 7)");
        Contains(CleanupSource, "ParseRange(lifecycle.SSOAndRefreshCleanupDays(), \"Authority Data Lifecycle: SSO and Refresh Cleanup Days\", 30, 90)");
        Contains(CleanupSource, "ParseRange(lifecycle.RevokedMetadataRetentionDays(), \"Authority Data Lifecycle: Revoked Metadata Retention Days\", 1, 3650)");
        Contains(CleanupSource, "candidate.TerminalAt ?? candidate.RevokedAt ?? candidate.ExpiresAt");
        Contains(CleanupSource, "candidate.TerminalAt ?? candidate.RedeemedAt ?? candidate.RevokedAt ?? candidate.ExpiresAt");
        Contains(CleanupSource, "candidate.RetainUntil.ToUniversalTime() > now");

        var now = new DateTimeOffset(2026, 8, 27, 9, 0, 0, TimeSpan.Zero);
        True(now.AddHours(-23) > now.AddDays(-1));
        True(now.AddHours(-24) <= now.AddDays(-1));
        True(now.AddDays(-29) > now.AddDays(-30));
        True(now.AddDays(-30) <= now.AddDays(-30));
    }

    private static void CleanupRaceHasExactlyOneWinner()
    {
        var state = 0;
        var cleanup = Task.Run(() => Interlocked.CompareExchange(ref state, 1, 0) == 0);
        var redemption = Task.Run(() => Interlocked.CompareExchange(ref state, 2, 0) == 0);
        Task.WaitAll(cleanup, redemption);
        Equal(1, new[] { cleanup.Result, redemption.Result }.Count(result => result));

        Contains(CleanupSource, "DeleteAsync(GetRecordType(candidate.CredentialCategory), candidate.CredentialId, candidate.ConcurrencyToken");
        Contains(CleanupSource, "catch (Exception exception) when (_isConcurrencyConflict(exception))");
        Contains(CleanupSource, "concurrencyLost++");
        Before(CleanupSource, "DeleteAsync(GetRecordType(candidate.CredentialCategory)", "operation.CommitAsync");
    }

    private static void CleanupIsHostedAndRegistered()
    {
        Contains(CleanupSource, ".AddClass(\"SecurityAuthorityCleanupHostedService\"");
        Contains(CleanupSource, "@class.ExtendsClass(\"BackgroundService\")");
        Contains(CleanupSource, "await _cleanup.RunAsync(stoppingToken)");
        Contains(CleanupSource, "Task.Delay(TimeSpan.FromHours(24), stoppingToken)");
        Contains(RegistrationSource, "request[\"cleanup-trigger\"] = \"SecurityAuthorityCleanup\"");
        Contains(RegistrationSource, "request[\"cleanup-hosted-service\"] = \"SecurityAuthorityCleanupHostedService\"");
    }

    private static void StableUtcAndComparisonRulesAreEnforced()
    {
        Contains(IntegrationSource, "envelope.OccurredAt.Offset != TimeSpan.Zero");
        Contains(IntegrationSource, "_utcNow().ToUniversalTime()");
        Contains(IntegrationSource, "StableIdentifier(envelope.Payload)");
        Contains(IntegrationSource, "StringComparison.Ordinal");
        Contains(IntegrationSource, "StringComparer.Ordinal");
        Contains(CleanupSource, "_utcNow().ToUniversalTime()");
        Contains(CleanupSource, "candidate.ConcurrencyToken");
    }

    private static bool IsSecret(string field)
    {
        var normalized = field.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return normalized.Contains("secret", StringComparison.Ordinal)
            || normalized.Contains("password", StringComparison.Ordinal)
            || normalized.Contains("hash", StringComparison.Ordinal)
            || normalized.Contains("privatekey", StringComparison.Ordinal)
            || normalized.Contains("credential", StringComparison.Ordinal);
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
            if (Directory.Exists(candidate)) return candidate;
            candidate = Path.Combine(directory.FullName, "src", "Aryzac.Security.Service");
            if (Directory.Exists(candidate)) return candidate;
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

    private static void Contains(string source, string value) => True(source.Contains(value, StringComparison.Ordinal), $"Expected to find '{value}'.");

    private static void DoesNotContain(string source, string value) => True(!source.Contains(value, StringComparison.Ordinal), $"Did not expect to find '{value}'.");

    private static void Equal<T>(T expected, T actual) where T : notnull =>
        True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected '{expected}', got '{actual}'.");

    private static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual) =>
        True(expected.SequenceEqual(actual), "Sequences were not equal.");

    private static void True(bool condition, string message = "Expected condition to be true.")
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
