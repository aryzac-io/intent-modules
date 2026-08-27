using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class AuthorizationEngineSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string EngineSource = ReadServiceSource("Templates", "SecurityAuthorityAuthorizationEngine", "SecurityAuthorityAuthorizationEngineTemplatePartial.cs");
    private static readonly string RegistrationSource = ReadServiceSource("Templates", "SecurityAuthorityAuthorizationEngine", "SecurityAuthorityAuthorizationEngineTemplateRegistration.cs");
    private static readonly string ValidationSource = ReadServiceSource("Templates", "SecurityAuthorityValidation", "SecurityAuthorityValidationTemplatePartial.cs");
    private static readonly string IntegrationSource = ReadServiceSource("FactoryExtensions", "SecurityAuthorityIntegration.cs");
    private static readonly string IntegrationEventsSource = ReadServiceSource("Templates", "SecurityAuthorityIntegrationEvents", "SecurityAuthorityIntegrationEventsTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("resource ancestry is adapter resolved and inheritance bounded", ResourceAncestryIsAdapterResolvedAndInheritanceBounded),
            ("Tenant Adapter values override stored parent and Tenant values", TenantAdapterValuesAreAuthoritative),
            ("cycles unknown resources and cross-Tenant ancestry are rejected", InvalidResourceAncestryIsRejected),
            ("Role Membership eligibility and Tenant boundaries are enforced", RoleMembershipEligibilityAndTenantBoundariesAreEnforced),
            ("inactive expired and revoked authorization state is filtered", InactiveExpiredAndRevokedStateIsFiltered),
            ("Deny Grants take precedence over Allow Grants", DenyPrecedesAllow),
            ("Permission Keys use exact ordinal case-sensitive comparison", PermissionKeysAreOrdinalCaseSensitive),
            ("all authorization-affecting changes invalidate before reuse", AuthorizationChangesInvalidateBeforeReuse),
            ("unknown principals and resources are rejected", UnknownPrincipalsAndResourcesAreRejected),
            ("Grant Catalog is descriptive and non-authorizing", GrantCatalogIsNonAuthorizing),
            ("authorization engine integration wiring is complete", AuthorizationEngineIntegrationWiringIsComplete),
            ("authorization engine uses the single-file builder convention", AuthorizationEngineUsesSingleFileBuilderConvention),
            ("authorization engine surface contains no stubs", AuthorizationEngineSurfaceContainsNoStubs)
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

    private static void ResourceAncestryIsAdapterResolvedAndInheritanceBounded()
    {
        var authorize = AuthorizeBlock();
        Contains(authorize, "SecurityAuthorityValidation.ValidateTenantResourceAsync(");
        Contains(ValidationSource, "var resources = new List<SecurityAuthorityTenantResource>()");
        Contains(ValidationSource, "var currentId = tenantResourceId");
        Contains(ValidationSource, "while (currentId is not null)");
        Contains(ValidationSource, "currentId = resource.ParentTenantResourceId");
        Contains(authorize, "foreach (var resource in tenant.Ancestry.Resources)");
        Contains(authorize, "if (resource.InheritanceProtected)");
        Before(authorize, "evaluatedResources.Add(resource)", "if (resource.InheritanceProtected)");
        Contains(authorize, "ThisResourceAndDescendants");
        Contains(authorize, "ThisResourceOnly");
    }

    private static void TenantAdapterValuesAreAuthoritative()
    {
        var register = RegisterTenantResourceBlock();
        Contains(register, "var authoritative = resolved.Ancestry.Resources[0]");
        Contains(register, "authoritative.TenantResourceId");
        Contains(register, "authoritative.ResourceKind");
        Contains(register, "authoritative.ParentTenantResourceId");
        Contains(register, "authoritative.TenantId");
        Contains(register, "authoritative.InheritanceProtected");
    }

    private static void InvalidResourceAncestryIsRejected()
    {
        Contains(ValidationSource, "var visited = new HashSet<string>(StringComparer.Ordinal)");
        Contains(ValidationSource, "if (!visited.Add(currentId))");
        Contains(ValidationSource, "cyclic_parentage");
        Contains(ValidationSource, "The Tenant Adapter returned an unknown Tenant Resource.");
        Contains(ValidationSource, "Every Tenant Resource in one ancestry chain must belong to the same Tenant.");
        Contains(ValidationSource, "!string.Equals(resource.TenantId, tenantId, StringComparison.Ordinal)");
        Contains(AuthorizeBlock(), "return Denied(\"invalid_tenant_resource:\" + string.Join(\",\", tenant.Validation.Failures.Select(x => x.Code)))");
    }

    private static void RoleMembershipEligibilityAndTenantBoundariesAreEnforced()
    {
        var authorize = AuthorizeBlock();
        Contains(authorize, "GetRoleMembershipsAsync(request.PrincipalType, request.PrincipalId, cancellationToken)");
        Contains(authorize, "membership.IsRevoked || membership.RevokedAt is not null");
        Contains(authorize, "membership.ExpiresAt is not null && membership.ExpiresAt <= now");
        Contains(authorize, "return Denied(\"invalid_role_membership\")");
        Contains(authorize, "return Denied(\"unknown_role_reference\")");
        Contains(authorize, "return Denied(\"role_membership_tenant_mismatch\")");
        Contains(authorize, "if (!role.IsEnabled)");
        Contains(authorize, "grantPrincipals.Add((\"Role\", role.Id))");
    }

    private static void InactiveExpiredAndRevokedStateIsFiltered()
    {
        var authorize = AuthorizeBlock();
        var principal = PrincipalStateBlock();
        Contains(authorize, "grant.IsRevoked || grant.RevokedAt is not null");
        Contains(authorize, "grant.ExpiresAt is not null && grant.ExpiresAt <= now");
        Contains(principal, "string.Equals(user.Status, \"Active\", StringComparison.Ordinal)");
        Contains(principal, "service.IsActive");
        Contains(principal, "role.IsEnabled");
        Contains(principal, "apiKey.IsRevoked || apiKey.RevokedAt is not null");
        Contains(principal, "apiKey.ExpiresAt is not null && apiKey.ExpiresAt <= now");
        Contains(principal, "api_key_owner_not_active");
    }

    private static void DenyPrecedesAllow()
    {
        var authorize = AuthorizeBlock();
        Before(authorize, "var denies = applicable", "var allows = applicable");
        Contains(authorize, "if (denies.Length != 0)");
        Contains(authorize, "new SecurityAuthorityAuthorizationDecision(false, \"explicit_deny\", denies)");
        Contains(authorize, "new SecurityAuthorityAuthorizationDecision(true, \"explicit_allow\", allows)");
    }

    private static void PermissionKeysAreOrdinalCaseSensitive()
    {
        var authorize = AuthorizeBlock();
        var catalog = GrantCatalogBlock();
        Contains(authorize, "string.Equals(grant.PermissionKey, request.PermissionKey, StringComparison.Ordinal)");
        Contains(catalog, "GroupBy(x => x.PermissionKey, StringComparer.Ordinal)");
        Contains(catalog, "OrderBy(x => x.PermissionKey, StringComparer.Ordinal)");
        DoesNotContain(authorize + catalog, "OrdinalIgnoreCase");
    }

    private static void AuthorizationChangesInvalidateBeforeReuse()
    {
        var invalidate = InvalidateBlock();
        foreach (var change in new[]
        {
            "Grant",
            "RoleMembership",
            "Role",
            "Service",
            "User",
            "ApiKey",
            "TenantResourceParent"
        })
        {
            Contains(invalidate, $"SecurityAuthorityAuthorizationChange.{change} => recordId");
        }

        Before(invalidate, "Interlocked.Increment(ref _revision)", "_cache.Clear()");
        var authorize = AuthorizeBlock();
        Contains(authorize, "cached.Revision == revision");
        Contains(authorize, "_cache[cacheKey] = new SecurityAuthorityAuthorizationCacheEntry(decision, revision, validUntil)");
        var register = RegisterTenantResourceBlock();
        Before(register, "var receipt = await operation.CommitAsync(cancellationToken)", "Invalidate(SecurityAuthorityAuthorizationChange.TenantResourceParent, tenantId, tenantResourceId)");
        Contains(IntegrationEventsSource, "ctor.AddParameter(\"ISecurityAuthorityAuthorizationInvalidator\", \"authorizationInvalidator\"");
        Before(IntegrationEventsSource, "var receipt = await operation.CommitAsync(cancellationToken)", "InvalidateAuthorization(eventShape.Resource");
        foreach (var change in new[] { "User", "Service", "TenantResourceParent", "Role", "RoleMembership", "Grant" })
        {
            Contains(IntegrationEventsSource, $"SecurityAuthorityAuthorizationChange.{change}");
        }
    }

    private static void UnknownPrincipalsAndResourcesAreRejected()
    {
        var authorize = AuthorizeBlock();
        var principal = PrincipalStateBlock();
        Contains(authorize, "unsupported_principal_type");
        Contains(authorize, "invalid_tenant_resource:");
        Contains(principal, "unknown_user");
        Contains(principal, "unknown_service");
        Contains(principal, "unknown_role");
        Contains(principal, "unknown_api_key");
        Contains(authorize, "unknown_role_reference");
    }

    private static void GrantCatalogIsNonAuthorizing()
    {
        var catalog = GrantCatalogBlock();
        Contains(catalog, "_dataSource.GetGrantCatalogAsync(cancellationToken)");
        Contains(catalog, "new SecurityAuthorityGrantCatalogEntry(group.Key, descriptions[0])");
        DoesNotContain(catalog, "AuthorizeAsync");
        DoesNotContain(catalog, "SecurityAuthorityAuthorizationDecision");
        Contains(IntegrationSource, "request[\"grant-catalog-authorizes\"] = \"false\"");
    }

    private static void AuthorizationEngineIntegrationWiringIsComplete()
    {
        Contains(IntegrationSource, "request[\"authorization-engine\"] = \"SecurityAuthorityAuthorizationEngine\"");
        Contains(IntegrationSource, "request[\"authorization-data-source-contract\"] = \"ISecurityAuthorityAuthorizationDataSource\"");
        Contains(IntegrationSource, "request[\"authorization-invalidator-contract\"] = \"ISecurityAuthorityAuthorizationInvalidator\"");
        Contains(IntegrationSource, "request[\"authorization-engine-lifetime\"] = \"singleton\"");
    }

    private static void AuthorizationEngineUsesSingleFileBuilderConvention()
    {
        Contains(RegistrationSource, "SingleFileTemplateRegistration");
        Contains(RegistrationSource, "SecurityAuthorityAuthorizationEngineTemplate.TemplateId");
        Contains(RegistrationSource, "new SecurityAuthorityAuthorizationEngineTemplate(outputTarget)");
        Contains(EngineSource, ".AddClass(\"SecurityAuthorityAuthorizationEngine\"");
        Contains(EngineSource, "ctor.AddParameter(\"ISecurityAuthorityTenantAdapter\", \"tenantAdapter\"");
        Contains(EngineSource, "ctor.AddParameter(\"ISecurityAuthorityPersistence\", \"persistence\"");
        Contains(EngineSource, "ctor.AddParameter(\"ISecurityAuthorityAuthorizationDataSource\", \"dataSource\"");
    }

    private static void AuthorizationEngineSurfaceContainsNoStubs()
    {
        foreach (var source in new[] { EngineSource, RegistrationSource, IntegrationSource })
        {
            DoesNotContain(source, "NotImplementedException");
            DoesNotContain(source, "TODO");
            DoesNotContain(source, "exampleParam");
        }
    }

    private static string RegisterTenantResourceBlock() =>
        GetBlock(EngineSource, ".AddMethod(\"ValueTask<SecurityAuthorityMutationResult>\", \"RegisterTenantResourceAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityAuthorizationDecision>\", \"AuthorizeAsync\"");

    private static string AuthorizeBlock() =>
        GetBlock(EngineSource, ".AddMethod(\"ValueTask<SecurityAuthorityAuthorizationDecision>\", \"AuthorizeAsync\"", ".AddMethod(\"ValueTask<IReadOnlyList<SecurityAuthorityGrantCatalogEntry>>\", \"GetGrantCatalogAsync\"");

    private static string GrantCatalogBlock() =>
        GetLastBlock(EngineSource, ".AddMethod(\"ValueTask<IReadOnlyList<SecurityAuthorityGrantCatalogEntry>>\", \"GetGrantCatalogAsync\"", ".AddMethod(\"void\", \"Invalidate\"");

    private static string InvalidateBlock() =>
        GetBlock(EngineSource, ".AddMethod(\"void\", \"Invalidate\"", ".AddMethod(\"ValueTask<SecurityAuthorityPrincipalState>\", \"GetPrincipalStateAsync\"");

    private static string PrincipalStateBlock() =>
        GetBlock(EngineSource, ".AddMethod(\"ValueTask<SecurityAuthorityPrincipalState>\", \"GetPrincipalStateAsync\"", ".AddMethod(\"SecurityAuthorityAuthorizationDecision\", \"Denied\"");

    private static string GetBlock(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        True(start >= 0, $"Marker '{startMarker}' was not found.");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        True(end >= 0, $"Marker '{endMarker}' was not found after '{startMarker}'.");
        return source[start..end];
    }

    private static string GetLastBlock(string source, string startMarker, string endMarker)
    {
        var end = source.LastIndexOf(endMarker, StringComparison.Ordinal);
        True(end >= 0, $"Marker '{endMarker}' was not found.");
        var start = source.LastIndexOf(startMarker, end, StringComparison.Ordinal);
        True(start >= 0, $"Marker '{startMarker}' was not found before '{endMarker}'.");
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

            candidate = Path.Combine(directory.FullName, "src", "Aryzac.Security.Service");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Aryzac.Security.Service.");
    }

    private static void Before(string source, string first, string second)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);
        True(firstIndex >= 0, $"Expected source to contain '{first}'.");
        True(secondIndex >= 0, $"Expected source to contain '{second}'.");
        True(firstIndex < secondIndex, $"Expected '{first}' before '{second}'.");
    }

    private static void Contains(string source, string value) =>
        True(source.Contains(value, StringComparison.Ordinal), $"Expected source to contain '{value}'.");

    private static void DoesNotContain(string source, string value) =>
        True(!source.Contains(value, StringComparison.Ordinal), $"Expected source not to contain '{value}'.");

    private static void True(bool condition, string message = "Expected condition to be true.")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
