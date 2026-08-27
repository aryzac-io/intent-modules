using System;
using System.Collections.Generic;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Security.Service.Templates.SecurityAuthorityAuthorizationEngine
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityAuthorizationEngineTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityAuthorizationEngine";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityAuthorizationEngineTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Concurrent")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddEnum("SecurityAuthorityAuthorizationChange", @enum =>
                {
                    @enum.AddLiteral("Grant");
                    @enum.AddLiteral("RoleMembership");
                    @enum.AddLiteral("Role");
                    @enum.AddLiteral("Service");
                    @enum.AddLiteral("User");
                    @enum.AddLiteral("ApiKey");
                    @enum.AddLiteral("TenantResourceParent");
                })
                .AddRecord("SecurityAuthorityAuthorizationRequest", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "PrincipalType");
                        ctor.AddParameter("string", "PrincipalId");
                        ctor.AddParameter("string", "TenantId");
                        ctor.AddParameter("string", "TenantResourceId");
                        ctor.AddParameter("string", "ResourceKind");
                        ctor.AddParameter("string", "PermissionKey");
                    });
                })
                .AddRecord("SecurityAuthorityAuthorizationDecision", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("bool", "IsAllowed");
                        ctor.AddParameter("string", "Reason");
                        ctor.AddParameter("IReadOnlyList<string>", "ContributingGrantIds");
                    });
                })
                .AddRecord("SecurityAuthorityGrantCatalogEntry", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "PermissionKey");
                        ctor.AddParameter("string", "Description");
                    });
                })
                .AddInterface("ISecurityAuthorityAuthorizationDataSource", @interface =>
                {
                    @interface.AddMethod("ValueTask<SecurityAuthorityUser?>", "GetUserAsync", method =>
                    {
                        method.AddParameter("string", "userId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<SecurityAuthorityService?>", "GetServiceAsync", method =>
                    {
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<SecurityAuthorityApiKey?>", "GetApiKeyAsync", method =>
                    {
                        method.AddParameter("string", "apiKeyId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<SecurityAuthorityRole?>", "GetRoleAsync", method =>
                    {
                        method.AddParameter("string", "roleId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<string?>", "GetPrincipalTenantIdAsync", method =>
                    {
                        method.AddParameter("string", "principalType");
                        method.AddParameter("string", "principalId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<IReadOnlyList<SecurityAuthorityRoleMembership>>", "GetRoleMembershipsAsync", method =>
                    {
                        method.AddParameter("string", "principalType");
                        method.AddParameter("string", "principalId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<IReadOnlyList<SecurityAuthorityGrant>>", "GetGrantsAsync", method =>
                    {
                        method.AddParameter("string", "principalType");
                        method.AddParameter("string", "principalId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<IReadOnlyList<SecurityAuthorityGrantCatalogEntry>>", "GetGrantCatalogAsync", method =>
                    {
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddInterface("ISecurityAuthorityAuthorizationInvalidator", @interface =>
                {
                    @interface.AddMethod("void", "Invalidate", method =>
                    {
                        method.AddParameter("SecurityAuthorityAuthorizationChange", "change");
                        method.AddParameter("string", "tenantId");
                        method.AddParameter("string?", "recordId");
                    });
                })
                .AddRecord("SecurityAuthorityAuthorizationCacheEntry", record =>
                {
                    record.Internal();
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityAuthorizationDecision", "Decision");
                        ctor.AddParameter("long", "Revision");
                        ctor.AddParameter("DateTimeOffset?", "ValidUntil");
                    });
                })
                .AddRecord("SecurityAuthorityPrincipalState", record =>
                {
                    record.Internal();
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("bool", "IsActive");
                        ctor.AddParameter("string", "Reason");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("DateTimeOffset?", "ValidUntil");
                    });
                })
                .AddClass("SecurityAuthorityAuthorizationEngine", @class =>
                {
                    @class.Sealed();
                    @class.ImplementsInterface("ISecurityAuthorityAuthorizationInvalidator");
                    @class.AddField("ConcurrentDictionary<string, SecurityAuthorityAuthorizationCacheEntry>", "_cache", field =>
                    {
                        field.PrivateReadOnly();
                        field.WithAssignment(new CSharpStatement("new ConcurrentDictionary<string, SecurityAuthorityAuthorizationCacheEntry>(StringComparer.Ordinal)"));
                    });
                    @class.AddField("long", "_revision");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("ISecurityAuthorityTenantAdapter", "tenantAdapter", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityPersistence", "persistence", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityAuthorizationDataSource", "dataSource", param => param.IntroduceReadonlyField());
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityMutationResult>", "RegisterTenantResourceAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("string", "tenantId");
                        method.AddParameter("string", "tenantResourceId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            var resolved = await SecurityAuthorityValidation.ValidateTenantResourceAsync(
                            _tenantAdapter,
                            tenantId,
                            tenantResourceId,
                            null,
                            cancellationToken);
                            if (!resolved.Validation.IsValid || resolved.Ancestry is null)
                            {
                            return new SecurityAuthorityMutationResult(resolved.Validation, null);
                            }

                            var authoritative = resolved.Ancestry.Resources[0];
                            var operation = await _persistence.BeginAtomicOperationAsync(
                            SecurityAuthorityAtomicOperationKind.IdempotentProvisioning,
                            true,
                            cancellationToken);
                            try
                            {
                            var existing = (SecurityAuthorityTenantResourceRecord?)await operation.Records.LoadAsync(
                            typeof(SecurityAuthorityTenantResourceRecord),
                            authoritative.TenantResourceId,
                            cancellationToken);
                            var record = new SecurityAuthorityTenantResourceRecord(
                            authoritative.TenantResourceId,
                            authoritative.ResourceKind,
                            authoritative.ParentTenantResourceId,
                            authoritative.TenantId,
                            authoritative.InheritanceProtected,
                            Guid.NewGuid().ToString("N"));
                            var validation = await SecurityAuthorityValidation.ValidateTenantResourceRecordAsync(
                            record,
                            existing,
                            _tenantAdapter,
                            cancellationToken);
                            if (!validation.IsValid)
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return new SecurityAuthorityMutationResult(validation, null);
                            }

                            if (existing is not null &&
                            string.Equals(existing.ResourceKind, record.ResourceKind, StringComparison.Ordinal) &&
                            string.Equals(existing.ParentTenantResourceId, record.ParentTenantResourceId, StringComparison.Ordinal) &&
                            string.Equals(existing.TenantId, record.TenantId, StringComparison.Ordinal) &&
                            existing.InheritanceProtected == record.InheritanceProtected)
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return new SecurityAuthorityMutationResult(validation, null);
                            }

                            if (existing is null)
                            {
                            await operation.Records.AddAsync(record, cancellationToken);
                            }
                            else
                            {
                            await operation.Records.UpdateAsync(record, existing.ConcurrencyToken, cancellationToken);
                            }

                            var receipt = await operation.CommitAsync(cancellationToken);
                            Invalidate(SecurityAuthorityAuthorizationChange.TenantResourceParent, tenantId, tenantResourceId);
                            return new SecurityAuthorityMutationResult(validation, receipt);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            finally
                            {
                            await operation.DisposeAsync();
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityAuthorizationDecision>", "AuthorizeAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("SecurityAuthorityAuthorizationRequest", "request");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(request);
                            Require(request.PrincipalType, nameof(request.PrincipalType));
                            Require(request.PrincipalId, nameof(request.PrincipalId));
                            Require(request.TenantId, nameof(request.TenantId));
                            Require(request.TenantResourceId, nameof(request.TenantResourceId));
                            Require(request.ResourceKind, nameof(request.ResourceKind));
                            Require(request.PermissionKey, nameof(request.PermissionKey));

                            if (!new[] { "User", "Service", "ApiKey", "Role" }.Contains(request.PrincipalType, StringComparer.Ordinal))
                            {
                            return Denied("unsupported_principal_type");
                            }

                            var now = DateTimeOffset.UtcNow;
                            var revision = Interlocked.Read(ref _revision);
                            var cacheKey = BuildCacheKey(request);
                            if (_cache.TryGetValue(cacheKey, out var cached) &&
                            cached.Revision == revision &&
                            (cached.ValidUntil is null || cached.ValidUntil > now))
                            {
                            return cached.Decision;
                            }

                            var tenant = await SecurityAuthorityValidation.ValidateTenantResourceAsync(
                            _tenantAdapter,
                            request.TenantId,
                            request.TenantResourceId,
                            request.ResourceKind,
                            cancellationToken);
                            if (!tenant.Validation.IsValid || tenant.Ancestry is null)
                            {
                            return Denied("invalid_tenant_resource:" + string.Join(",", tenant.Validation.Failures.Select(x => x.Code)));
                            }

                            var principal = await GetPrincipalStateAsync(request.PrincipalType, request.PrincipalId, now, cancellationToken);
                            if (!principal.IsActive)
                            {
                            return Denied(principal.Reason);
                            }

                            if (!string.Equals(principal.TenantId, request.TenantId, StringComparison.Ordinal))
                            {
                            return Denied("principal_tenant_mismatch");
                            }

                            var validUntil = principal.ValidUntil;
                            var grantPrincipals = new List<(string Type, string Id)> { (request.PrincipalType, request.PrincipalId) };
                            if (string.Equals(request.PrincipalType, "User", StringComparison.Ordinal) ||
                            string.Equals(request.PrincipalType, "Service", StringComparison.Ordinal))
                            {
                            var memberships = await _dataSource.GetRoleMembershipsAsync(request.PrincipalType, request.PrincipalId, cancellationToken)
                            ?? throw new InvalidOperationException("The authorization data source returned null Role Memberships.");
                            foreach (var membership in memberships)
                            {
                            if (membership.IsRevoked || membership.RevokedAt is not null ||
                            membership.ExpiresAt is not null && membership.ExpiresAt <= now)
                            {
                            continue;
                            }

                            var expectedMemberId = string.Equals(request.PrincipalType, "User", StringComparison.Ordinal)
                            ? membership.UserId
                            : membership.ServiceId;
                            var unexpectedMemberId = string.Equals(request.PrincipalType, "User", StringComparison.Ordinal)
                            ? membership.ServiceId
                            : membership.UserId;
                            if (!string.Equals(expectedMemberId, request.PrincipalId, StringComparison.Ordinal) || unexpectedMemberId is not null)
                            {
                            return Denied("invalid_role_membership");
                            }

                            var role = await _dataSource.GetRoleAsync(membership.RoleId, cancellationToken);
                            if (role is null)
                            {
                            return Denied("unknown_role_reference");
                            }

                            var roleTenant = role.TenantId ?? await _dataSource.GetPrincipalTenantIdAsync("Role", role.Id, cancellationToken);
                            if (!string.Equals(roleTenant, request.TenantId, StringComparison.Ordinal))
                            {
                            return Denied("role_membership_tenant_mismatch");
                            }

                            if (!role.IsEnabled)
                            {
                            continue;
                            }

                            grantPrincipals.Add(("Role", role.Id));
                            validUntil = Earlier(validUntil, membership.ExpiresAt);
                            }
                            }

                            var evaluatedResources = new List<SecurityAuthorityTenantResource>();
                            foreach (var resource in tenant.Ancestry.Resources)
                            {
                            evaluatedResources.Add(resource);
                            if (resource.InheritanceProtected)
                            {
                            break;
                            }
                            }

                            var applicable = new List<SecurityAuthorityGrant>();
                            foreach (var grantPrincipal in grantPrincipals.Distinct())
                            {
                            var grants = await _dataSource.GetGrantsAsync(grantPrincipal.Type, grantPrincipal.Id, cancellationToken)
                            ?? throw new InvalidOperationException("The authorization data source returned null Grants.");
                            foreach (var grant in grants)
                            {
                            if (!string.Equals(grant.PrincipalType, grantPrincipal.Type, StringComparison.Ordinal) ||
                            !string.Equals(grant.PrincipalId, grantPrincipal.Id, StringComparison.Ordinal))
                            {
                            return Denied("invalid_grant_principal");
                            }

                            if (grant.IsRevoked || grant.RevokedAt is not null ||
                            grant.ExpiresAt is not null && grant.ExpiresAt <= now ||
                            !string.Equals(grant.PermissionKey, request.PermissionKey, StringComparison.Ordinal))
                            {
                            continue;
                            }

                            if (!string.Equals(grant.TenantId, request.TenantId, StringComparison.Ordinal))
                            {
                            return Denied("grant_tenant_mismatch");
                            }

                            var resource = evaluatedResources.FirstOrDefault(x =>
                            string.Equals(x.TenantResourceId, grant.TenantResourceId, StringComparison.Ordinal));
                            if (resource is null)
                            {
                            continue;
                            }

                            if (!string.Equals(resource.ResourceKind, grant.ResourceKind, StringComparison.Ordinal))
                            {
                            return Denied("grant_resource_kind_mismatch");
                            }

                            var isRequestedResource = string.Equals(
                            grant.TenantResourceId,
                            request.TenantResourceId,
                            StringComparison.Ordinal);
                            if (!isRequestedResource &&
                            !string.Equals(grant.Applicability, "ThisResourceAndDescendants", StringComparison.Ordinal))
                            {
                            continue;
                            }

                            if (!string.Equals(grant.Applicability, "ThisResourceOnly", StringComparison.Ordinal) &&
                            !string.Equals(grant.Applicability, "ThisResourceAndDescendants", StringComparison.Ordinal))
                            {
                            return Denied("invalid_grant_applicability");
                            }

                            if (!string.Equals(grant.Effect, "Allow", StringComparison.Ordinal) &&
                            !string.Equals(grant.Effect, "Deny", StringComparison.Ordinal))
                            {
                            return Denied("invalid_grant_effect");
                            }

                            applicable.Add(grant);
                            validUntil = Earlier(validUntil, grant.ExpiresAt);
                            }
                            }

                            var denies = applicable
                            .Where(x => string.Equals(x.Effect, "Deny", StringComparison.Ordinal))
                            .Select(x => x.Id)
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(x => x, StringComparer.Ordinal)
                            .ToArray();
                            SecurityAuthorityAuthorizationDecision decision;
                            if (denies.Length != 0)
                            {
                            decision = new SecurityAuthorityAuthorizationDecision(false, "explicit_deny", denies);
                            }
                            else
                            {
                            var allows = applicable
                            .Where(x => string.Equals(x.Effect, "Allow", StringComparison.Ordinal))
                            .Select(x => x.Id)
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(x => x, StringComparer.Ordinal)
                            .ToArray();
                            decision = allows.Length == 0
                            ? Denied("no_applicable_grant")
                            : new SecurityAuthorityAuthorizationDecision(true, "explicit_allow", allows);
                            }

                            _cache[cacheKey] = new SecurityAuthorityAuthorizationCacheEntry(decision, revision, validUntil);
                            return decision;
                            """);
                    });
                    @class.AddMethod("ValueTask<IReadOnlyList<SecurityAuthorityGrantCatalogEntry>>", "GetGrantCatalogAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            var entries = await _dataSource.GetGrantCatalogAsync(cancellationToken)
                            ?? throw new InvalidOperationException("The authorization data source returned a null Grant Catalog.");
                            var result = new List<SecurityAuthorityGrantCatalogEntry>();
                            foreach (var group in entries.GroupBy(x => x.PermissionKey, StringComparer.Ordinal))
                            {
                            if (string.IsNullOrWhiteSpace(group.Key))
                            {
                            throw new InvalidOperationException("Grant Catalog Permission Keys must be non-empty.");
                            }

                            var descriptions = group.Select(x => x.Description).Distinct(StringComparer.Ordinal).ToArray();
                            if (descriptions.Length != 1)
                            {
                            throw new InvalidOperationException($"Grant Catalog Permission Key '{group.Key}' has conflicting descriptions.");
                            }

                            result.Add(new SecurityAuthorityGrantCatalogEntry(group.Key, descriptions[0]));
                            }

                            return result.OrderBy(x => x.PermissionKey, StringComparer.Ordinal).ToArray();
                            """);
                    });
                    @class.AddMethod("void", "Invalidate", method =>
                    {
                        method.AddParameter("SecurityAuthorityAuthorizationChange", "change");
                        method.AddParameter("string", "tenantId");
                        method.AddParameter("string?", "recordId");
                        method.AddStatement("""
                            Require(tenantId, nameof(tenantId));
                            _ = change switch
                            {
                            SecurityAuthorityAuthorizationChange.Grant => recordId,
                            SecurityAuthorityAuthorizationChange.RoleMembership => recordId,
                            SecurityAuthorityAuthorizationChange.Role => recordId,
                            SecurityAuthorityAuthorizationChange.Service => recordId,
                            SecurityAuthorityAuthorizationChange.User => recordId,
                            SecurityAuthorityAuthorizationChange.ApiKey => recordId,
                            SecurityAuthorityAuthorizationChange.TenantResourceParent => recordId,
                            _ => throw new ArgumentOutOfRangeException(nameof(change), change, "Unsupported authorization state change.")
                            };
                            Interlocked.Increment(ref _revision);
                            _cache.Clear();
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityPrincipalState>", "GetPrincipalStateAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("string", "principalType");
                        method.AddParameter("string", "principalId");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            var tenantId = await _dataSource.GetPrincipalTenantIdAsync(principalType, principalId, cancellationToken);
                            if (string.Equals(principalType, "User", StringComparison.Ordinal))
                            {
                            var user = await _dataSource.GetUserAsync(principalId, cancellationToken);
                            return user is null
                            ? new SecurityAuthorityPrincipalState(false, "unknown_user", tenantId, null)
                            : new SecurityAuthorityPrincipalState(
                            string.Equals(user.Status, "Active", StringComparison.Ordinal),
                            string.Equals(user.Status, "Active", StringComparison.Ordinal) ? "active" : "user_not_active",
                            tenantId,
                            null);
                            }

                            if (string.Equals(principalType, "Service", StringComparison.Ordinal))
                            {
                            var service = await _dataSource.GetServiceAsync(principalId, cancellationToken);
                            return service is null
                            ? new SecurityAuthorityPrincipalState(false, "unknown_service", tenantId, null)
                            : new SecurityAuthorityPrincipalState(
                            service.IsActive,
                            service.IsActive ? "active" : "service_not_active",
                            service.TenantId ?? tenantId,
                            null);
                            }

                            if (string.Equals(principalType, "Role", StringComparison.Ordinal))
                            {
                            var role = await _dataSource.GetRoleAsync(principalId, cancellationToken);
                            return role is null
                            ? new SecurityAuthorityPrincipalState(false, "unknown_role", tenantId, null)
                            : new SecurityAuthorityPrincipalState(
                            role.IsEnabled,
                            role.IsEnabled ? "active" : "role_not_enabled",
                            role.TenantId ?? tenantId,
                            null);
                            }

                            var apiKey = await _dataSource.GetApiKeyAsync(principalId, cancellationToken);
                            if (apiKey is null)
                            {
                            return new SecurityAuthorityPrincipalState(false, "unknown_api_key", tenantId, null);
                            }

                            if (apiKey.IsRevoked || apiKey.RevokedAt is not null)
                            {
                            return new SecurityAuthorityPrincipalState(false, "api_key_revoked", apiKey.TenantId ?? tenantId, apiKey.ExpiresAt);
                            }

                            if (apiKey.ExpiresAt is not null && apiKey.ExpiresAt <= now)
                            {
                            return new SecurityAuthorityPrincipalState(false, "api_key_expired", apiKey.TenantId ?? tenantId, apiKey.ExpiresAt);
                            }

                            var owner = await GetPrincipalStateAsync(apiKey.OwnerPrincipalType, apiKey.OwnerId, now, cancellationToken);
                            if (!owner.IsActive)
                            {
                            return new SecurityAuthorityPrincipalState(false, "api_key_owner_not_active", apiKey.TenantId ?? tenantId, apiKey.ExpiresAt);
                            }

                            if (apiKey.TenantId is not null && !string.Equals(apiKey.TenantId, owner.TenantId, StringComparison.Ordinal))
                            {
                            return new SecurityAuthorityPrincipalState(false, "api_key_owner_tenant_mismatch", apiKey.TenantId, apiKey.ExpiresAt);
                            }

                            return new SecurityAuthorityPrincipalState(true, "active", apiKey.TenantId ?? owner.TenantId ?? tenantId, apiKey.ExpiresAt);
                            """);
                    });
                    @class.AddMethod("SecurityAuthorityAuthorizationDecision", "Denied", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "reason");
                        method.AddStatement("return new SecurityAuthorityAuthorizationDecision(false, reason, Array.Empty<string>());");
                    });
                    @class.AddMethod("DateTimeOffset?", "Earlier", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("DateTimeOffset?", "left");
                        method.AddParameter("DateTimeOffset?", "right");
                        method.AddStatement("return left is null ? right : right is null ? left : left <= right ? left : right;");
                    });
                    @class.AddMethod("string", "BuildCacheKey", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityAuthorizationRequest", "request");
                        method.AddStatement("""
                            return string.Concat(
                            Part(request.PrincipalType),
                            Part(request.PrincipalId),
                            Part(request.TenantId),
                            Part(request.TenantResourceId),
                            Part(request.ResourceKind),
                            Part(request.PermissionKey));
                            """);
                    });
                    @class.AddMethod("string", "Part", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "value");
                        method.AddStatement("return value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + \"#\" + value;");
                    });
                    @class.AddMethod("void", "Require", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "value");
                        method.AddParameter("string", "parameterName");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(\"A non-empty ordinal value is required.\", parameterName);");
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

