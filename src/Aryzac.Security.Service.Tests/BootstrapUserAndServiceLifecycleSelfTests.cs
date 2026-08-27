using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class BootstrapUserAndServiceLifecycleSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string BootstrapSource = ReadServiceSource("Templates", "SecurityAuthorityBootstrap", "SecurityAuthorityBootstrapTemplatePartial.cs");
    private static readonly string LifecycleSource = ReadServiceSource("Templates", "SecurityAuthorityLifecycle", "SecurityAuthorityLifecycleTemplatePartial.cs");
    private static readonly string AuthorizationSource = ReadServiceSource("Templates", "SecurityAuthorityAuthorizationEndpoints", "SecurityAuthorityAuthorizationEndpointsTemplatePartial.cs");
    private static readonly string DeviceSource = ReadServiceSource("Templates", "SecurityAuthorityDeviceEndpoints", "SecurityAuthorityDeviceEndpointsTemplatePartial.cs");
    private static readonly string SessionSource = ReadServiceSource("Templates", "SecurityAuthoritySessionEndpoints", "SecurityAuthoritySessionEndpointsTemplatePartial.cs");
    private static readonly string TokenSource = ReadServiceSource("Templates", "SecurityAuthorityTokenEndpoint", "SecurityAuthorityTokenEndpointTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("exactly one of three bootstrap strategies is required", ExactlyOneBootstrapStrategyIsRequired),
            ("explicit identity matches exact issuer-subject or normalized email", ExplicitIdentityMatchingIsExact),
            ("first eligible bootstrap is atomic and conflicts lose", FirstEligibleBootstrapIsAtomic),
            ("custom seed function and returned authority state are validated", CustomSeedIsValidated),
            ("bootstrap closes permanently and reset is protected", BootstrapClosureAndResetAreProtected),
            ("User transition matrix permits only R11 transitions", UserTransitionMatrixIsExact),
            ("non-Active Users are denied every credential issuance path", NonActiveUsersAreDeniedCredentials),
            ("User terminal transitions revoke dependents without deleting history", UserCascadeRetainsHistory),
            ("Service CRUD activation and authorization assignment surfaces exist", ServiceCrudAndAssignmentsExist),
            ("Service Tenant-name uniqueness and provisioning are idempotent", ServiceUniquenessAndProvisioningAreCorrect),
            ("Service deactivation and deletion revoke credentials atomically", ServiceCascadeRetainsHistory)
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

    private static void ExactlyOneBootstrapStrategyIsRequired()
    {
        Contains(BootstrapSource, "SecurityAuthorityExplicitIdentityBootstrap?");
        Contains(BootstrapSource, "FirstEligibleUser");
        Contains(BootstrapSource, "CustomSeedFunction");
        Contains(BootstrapSource, "var configuredStrategies = (ExplicitIdentity is null ? 0 : 1) + (FirstEligibleUser ? 1 : 0) + (CustomSeedFunction is null ? 0 : 1)");
        Contains(BootstrapSource, "if (configuredStrategies != 1) throw new InvalidOperationException");
    }

    private static void ExplicitIdentityMatchingIsExact()
    {
        var validate = Block(BootstrapSource, ".AddMethod(\"void\", \"Validate\"", ".AddClass(\"SecurityAuthorityBootstrap\"");
        Contains(validate, "hasIssuer != hasSubject");
        Contains(validate, "(hasIssuer ? 1 : 0) + (hasEmail ? 1 : 0) != 1");

        var match = Block(BootstrapSource, ".AddMethod(\"bool\", \"Matches\"", "                });\n        }");
        Contains(match, "StringComparison.Ordinal");
        Contains(match, "configured.NormalizedEmail, identity.NormalizedEmail");
        Contains(match, "configured.Issuer, identity.Issuer");
        Contains(match, "configured.Subject, identity.Subject");
    }

    private static void FirstEligibleBootstrapIsAtomic()
    {
        var commit = Block(BootstrapSource, ".AddMethod(\"ValueTask<SecurityAuthorityBootstrapResult>\", \"CommitAsync\"", ".AddMethod(\"ValueTask\", \"AddAndValidateSeedGrantsAsync\"");
        Contains(commit, "BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.FirstAdministratorBootstrap, true");
        Contains(commit, "existingState?.IsClosed == true");
        Contains(commit, "SecurityAuthorityBootstrapOutcome.Closed");
        Contains(commit, "_administratorGrantFactory(storedUser, cancellationToken)");
        Contains(commit, "await AddAndValidateSeedGrantsAsync(operation.Records, seed, storedUser, cancellationToken)");
        Contains(commit, "operation.Records.AddAsync(closedState");
        Contains(commit, "operation.Records.UpdateAsync(closedState, existingState.ConcurrencyToken");
        Contains(commit, "catch (Exception exception) when (_isConcurrencyConflict(exception))");
        Contains(commit, "SecurityAuthorityBootstrapOutcome.Conflict");
        Before(commit, "AddAndValidateSeedGrantsAsync", "operation.Records.AddAsync(closedState");
        Before(commit, "operation.Records.AddAsync(closedState", "operation.CommitAsync(cancellationToken)");
    }

    private static void CustomSeedIsValidated()
    {
        var initialize = Block(BootstrapSource, ".AddMethod(\"ValueTask<SecurityAuthorityBootstrapResult>\", \"InitializeAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityBootstrapResult>\", \"TryBootstrapAsync\"");
        Contains(initialize, "CustomSeedFunction is null");
        Contains(initialize, "custom seed function returned no bootstrap seed");

        var commit = Block(BootstrapSource, ".AddMethod(\"ValueTask<SecurityAuthorityBootstrapResult>\", \"CommitAsync\"", ".AddMethod(\"ValueTask\", \"AddAndValidateSeedGrantsAsync\"");
        Contains(commit, "Bootstrap User '{userId}' is unknown");
        Contains(commit, "Bootstrap User '{userId}' must be Active");

        var grants = Block(BootstrapSource, ".AddMethod(\"ValueTask\", \"AddAndValidateSeedGrantsAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityBootstrapState?>\", \"LoadStateAsync\"");
        Contains(grants, "custom seed User does not match the stored User");
        Contains(grants, "custom seed User must be Active");
        Contains(grants, "must return at least one initial Grant");
        Contains(grants, "duplicate Grant identifier");
        Contains(grants, "Every custom seed Grant must target the initial administrator User");
        Contains(grants, "cannot be revoked");
        Contains(grants, "SecurityAuthorityValidation.ValidateGrantAsync");
    }

    private static void BootstrapClosureAndResetAreProtected()
    {
        var commit = Block(BootstrapSource, ".AddMethod(\"ValueTask<SecurityAuthorityBootstrapResult>\", \"CommitAsync\"", ".AddMethod(\"ValueTask\", \"AddAndValidateSeedGrantsAsync\"");
        Contains(commit, "existingState?.IsClosed == true");
        Contains(commit, "SecurityAuthorityBootstrapOutcome.Closed");

        var reset = Block(BootstrapSource, ".AddMethod(\"ValueTask\", \"ResetAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityBootstrapResult>\", \"CommitAsync\"");
        Contains(reset, "_authorizeBootstrapReset");
        Contains(reset, "Only an Application Administrator can reset");
        Contains(reset, "ExpectedConcurrencyToken");
        Contains(reset, "concurrency token is stale");
        Contains(reset, "DeleteAsync(typeof(SecurityAuthorityBootstrapState)");
        Before(reset, "DeleteAsync(typeof(SecurityAuthorityBootstrapState)", "operation.CommitAsync(cancellationToken)");
    }

    private static void UserTransitionMatrixIsExact()
    {
        var transition = Block(LifecycleSource, ".AddMethod(\"bool\", \"CanTransitionUser\"", ".AddMethod(\"bool\", \"IsTerminalUserStatus\"");
        Contains(transition, "string.Equals(currentStatus, targetStatus, StringComparison.Ordinal)");
        Contains(transition, "if (IsTerminalUserStatus(currentStatus)) return false");
        Contains(transition, "currentStatus, \\\"New\\\"");
        Contains(transition, "targetStatus, \\\"Active\\\"");
        Contains(transition, "currentStatus, \\\"Active\\\"");
        Contains(transition, "targetStatus, \\\"Suspended\\\"");
        Contains(transition, "currentStatus, \\\"Suspended\\\"");
        Contains(transition, "IsTerminalUserStatus(targetStatus)");
        DoesNotContain(transition, "currentStatus, \\\"New\\\", StringComparison.Ordinal)) return string.Equals(targetStatus, \\\"Active\\\", StringComparison.Ordinal) || string.Equals(targetStatus, \\\"Suspended\\\"");
    }

    private static void NonActiveUsersAreDeniedCredentials()
    {
        Contains(SessionSource, "user is not null && string.Equals(user.Status, \\\"Active\\\", StringComparison.Ordinal)");
        Contains(TokenSource, "user is null || !string.Equals(user.Status, \\\"Active\\\", StringComparison.Ordinal)");
        Contains(DeviceSource, "user is null || !string.Equals(user.Status, \"Active\", StringComparison.Ordinal)");
        Contains(DeviceSource, "activeUser is null || !string.Equals(activeUser.Status, \"Active\", StringComparison.Ordinal)");

        var callback = Block(AuthorizationSource, "var callbackResult = await SecurityAuthorityExternalProviders.ProcessCallbackAsync", "SecurityAuthoritySessionEndpoints.IssueCookie");
        Contains(callback, "callbackResult.User.Status");
        Contains(callback, "\"Active\"");
        Before(callback, "callbackResult.User.Status", "SecurityAuthoritySessionEndpoints.CreateSession");
        Before(callback, "callbackResult.User.Status", "new SecurityAuthorityAuthorizationCode(");
    }

    private static void UserCascadeRetainsHistory()
    {
        var transition = Block(LifecycleSource, ".AddMethod(\"ValueTask<SecurityAuthorityUser>\", \"TransitionUserAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityService>\", \"CreateServiceAsync\"");
        Contains(transition, "if (IsTerminalUserStatus(targetStatus))");
        Contains(transition, "RevokeUserDependentsAsync");

        var cascade = Block(LifecycleSource, ".AddMethod(\"ValueTask\", \"RevokeUserDependentsAsync\"", ".AddMethod(\"ValueTask\", \"RevokeServiceDependentsAsync\"");
        Contains(cascade, "findSsoSessionsByUser");
        Contains(cascade, "findRefreshTokensByUser");
        Contains(cascade, "findApiKeysByOwner(records, \\\"User\\\"");
        Contains(cascade, "findRoleMembershipsByPrincipal(records, \\\"User\\\"");
        Contains(cascade, "findGrantsByPrincipal(records, \\\"User\\\"");
        Contains(cascade, "RevokedAt = now");
        Contains(cascade, "IsRevoked = true");
        DoesNotContain(cascade, "DeleteAsync");
    }

    private static void ServiceCrudAndAssignmentsExist()
    {
        foreach (var method in new[]
        {
            "CreateServiceAsync", "ReadServiceAsync", "ListServicesAsync", "UpdateServiceAsync",
            "ActivateServiceAsync", "DeactivateServiceAsync", "DeleteServiceAsync",
            "AssignServiceRoleAsync", "RemoveServiceRoleAsync", "AssignServiceGrantAsync", "RemoveServiceGrantAsync"
        })
        {
            Contains(LifecycleSource, $"\"{method}\"");
        }

        Contains(LifecycleSource, "Roles can only be assigned to an active Service");
        Contains(LifecycleSource, "Grants can only be assigned to an active Service");
        Contains(LifecycleSource, "SecurityAuthorityAuthorizationChange.RoleMembership");
        Contains(LifecycleSource, "SecurityAuthorityAuthorizationChange.Grant");
    }

    private static void ServiceUniquenessAndProvisioningAreCorrect()
    {
        var create = Block(LifecycleSource, ".AddMethod(\"ValueTask<SecurityAuthorityService>\", \"CreateServiceAsync\"", ".AddMethod(\"ValueTask<string>\", \"ProvisionServiceAsync\"");
        Contains(create, "findServiceByTenantAndName(operation.Records, service.TenantId, service.Name");
        Contains(create, "already exists in Tenant");

        var provision = Block(LifecycleSource, ".AddMethod(\"ValueTask<string>\", \"ProvisionServiceAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityService?>\", \"ReadServiceAsync\"");
        Contains(provision, "findServiceByTenantAndName(operation.Records, service.TenantId, service.Name");
        Contains(provision, "return existing.Id");
        Contains(provision, "return service.Id");

        var update = Block(LifecycleSource, ".AddMethod(\"ValueTask<SecurityAuthorityService>\", \"UpdateServiceAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityService>\", \"ActivateServiceAsync\"");
        Contains(update, "findServiceByTenantAndName(operation.Records, tenantId, name");
        Contains(update, "duplicate is not null && !string.Equals(duplicate.Id, serviceId");
    }

    private static void ServiceCascadeRetainsHistory()
    {
        var stateChange = Block(LifecycleSource, ".AddMethod(\"ValueTask<SecurityAuthorityService>\", \"ChangeServiceStateAsync\"", ".AddMethod(\"ValueTask\", \"RevokeUserDependentsAsync\"");
        Contains(stateChange, "if (!isActive) await RevokeServiceDependentsAsync");
        Before(stateChange, "RevokeServiceDependentsAsync", "operation.CommitAsync(cancellationToken)");

        var cascade = Block(LifecycleSource, ".AddMethod(\"ValueTask\", \"RevokeServiceDependentsAsync\"", ".AddMethod(\"void\", \"EnsureValid\"");
        Contains(cascade, "findApiKeysByOwner(records, \\\"Service\\\"");
        Contains(cascade, "findRefreshTokensByService");
        Contains(cascade, "IsRevoked = true");
        Contains(cascade, "RevokedAt = now");
        DoesNotContain(cascade, "DeleteAsync");
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
