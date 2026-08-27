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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityLifecycle
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityLifecycleTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityLifecycle";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityLifecycleTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddClass("SecurityAuthorityLifecycle", @class =>
                {
                    @class.Static();
                    @class.AddMethod("bool", "CanTransitionUser", method =>
                    {
                        method.Static();
                        method.AddParameter("string", "currentStatus");
                        method.AddParameter("string", "targetStatus");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(targetStatus)) return false;");
                        method.AddStatement("if (string.Equals(currentStatus, targetStatus, StringComparison.Ordinal)) return true;");
                        method.AddStatement("if (IsTerminalUserStatus(currentStatus)) return false;");
                        method.AddStatement("if (string.Equals(currentStatus, \"New\", StringComparison.Ordinal)) return string.Equals(targetStatus, \"Active\", StringComparison.Ordinal) || IsTerminalUserStatus(targetStatus);");
                        method.AddStatement("if (string.Equals(currentStatus, \"Active\", StringComparison.Ordinal)) return string.Equals(targetStatus, \"Suspended\", StringComparison.Ordinal) || IsTerminalUserStatus(targetStatus);");
                        method.AddStatement("if (string.Equals(currentStatus, \"Suspended\", StringComparison.Ordinal)) return string.Equals(targetStatus, \"Active\", StringComparison.Ordinal) || IsTerminalUserStatus(targetStatus);");
                        method.AddStatement("return false;");
                    });
                    @class.AddMethod("bool", "IsTerminalUserStatus", method =>
                    {
                        method.Static();
                        method.AddParameter("string", "status");
                        method.AddStatement("return string.Equals(status, \"Archived\", StringComparison.Ordinal) || string.Equals(status, \"Deleted\", StringComparison.Ordinal);");
                    });
                    @class.AddMethod("bool", "CanCreateSsoSession", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityUser?", "user");
                        method.AddStatement("return IsActiveUser(user);");
                    });
                    @class.AddMethod("bool", "CanIssueRefreshToken", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityUser?", "user");
                        method.AddStatement("return IsActiveUser(user);");
                    });
                    @class.AddMethod("bool", "CanUseApiKey", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityApiKey?", "apiKey");
                        method.AddParameter("SecurityAuthorityUser?", "user");
                        method.AddParameter("SecurityAuthorityService?", "service");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("if (apiKey is null || apiKey.IsRevoked || apiKey.ExpiresAt is not null && apiKey.ExpiresAt <= now) return false;");
                        method.AddStatement("return IsLivePrincipal(apiKey.OwnerPrincipalType, apiKey.OwnerId, user, service);");
                    });
                    @class.AddMethod("bool", "CanUseRoleMembership", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityRoleMembership?", "membership");
                        method.AddParameter("SecurityAuthorityUser?", "user");
                        method.AddParameter("SecurityAuthorityService?", "service");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("if (membership is null || membership.IsRevoked || membership.ExpiresAt is not null && membership.ExpiresAt <= now) return false;");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(membership.UserId)) return IsLivePrincipal(\"User\", membership.UserId, user, service);");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(membership.ServiceId)) return IsLivePrincipal(\"Service\", membership.ServiceId, user, service);");
                        method.AddStatement("return false;");
                    });
                    @class.AddMethod("bool", "CanUseGrant", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityGrant?", "grant");
                        method.AddParameter("SecurityAuthorityUser?", "user");
                        method.AddParameter("SecurityAuthorityService?", "service");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("return grant is not null && !grant.IsRevoked && (grant.ExpiresAt is null || grant.ExpiresAt > now) && IsLivePrincipal(grant.PrincipalType, grant.PrincipalId, user, service);");
                    });
                    @class.AddMethod("bool", "IsActiveUser", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityUser?", "user");
                        method.AddStatement("return user is not null && string.Equals(user.Status, \"Active\", StringComparison.Ordinal);");
                    });
                    @class.AddMethod("bool", "IsActiveService", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityService?", "service");
                        method.AddStatement("return service is not null && service.IsActive;");
                    });
                    @class.AddMethod("bool", "IsLivePrincipal", method =>
                    {
                        method.Static();
                        method.AddParameter("string", "principalType");
                        method.AddParameter("string", "principalId");
                        method.AddParameter("SecurityAuthorityUser?", "user");
                        method.AddParameter("SecurityAuthorityService?", "service");
                        method.AddStatement("if (string.Equals(principalType, \"User\", StringComparison.Ordinal)) return IsActiveUser(user) && string.Equals(user!.Id, principalId, StringComparison.Ordinal);");
                        method.AddStatement("if (string.Equals(principalType, \"Service\", StringComparison.Ordinal)) return IsActiveService(service) && string.Equals(service!.Id, principalId, StringComparison.Ordinal);");
                        method.AddStatement("return false;");
                    });
                    @class.AddMethod("ValueTask<bool>", "IsLivePrincipalAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("string", "principalType");
                        method.AddParameter("string", "principalId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(records);");
                        method.AddStatement("if (string.Equals(principalType, \"User\", StringComparison.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    var user = await records.LoadAsync(typeof(SecurityAuthorityUser), principalId, cancellationToken) as SecurityAuthorityUser;");
                        method.AddStatement("    return IsActiveUser(user);");
                        method.AddStatement("}");
                        method.AddStatement("if (string.Equals(principalType, \"Service\", StringComparison.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    var service = await records.LoadAsync(typeof(SecurityAuthorityService), principalId, cancellationToken) as SecurityAuthorityService;");
                        method.AddStatement("    return IsActiveService(service);");
                        method.AddStatement("}");
                        method.AddStatement("return false;");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityUser>", "TransitionUserAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "userId");
                        method.AddParameter("string", "targetStatus");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthoritySsoSession>>>", "findSsoSessionsByUser");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRefreshToken>>>", "findRefreshTokensByUser");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityApiKey>>>", "findApiKeysByOwner");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRoleMembership>>>", "findRoleMembershipsByPrincipal");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityGrant>>>", "findGrantsByPrincipal");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        method.AddStatement("ArgumentException.ThrowIfNullOrWhiteSpace(userId);");
                        method.AddStatement("ArgumentException.ThrowIfNullOrWhiteSpace(targetStatus);");
                        method.AddStatement("var now = utcNow();");
                        method.AddStatement("await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var user = await operation.Records.LoadAsync(typeof(SecurityAuthorityUser), userId, cancellationToken) as SecurityAuthorityUser ?? throw new InvalidOperationException(\"The User could not be loaded.\");");
                        method.AddStatement("    if (!CanTransitionUser(user.Status, targetStatus)) throw new InvalidOperationException($\"User status cannot transition from '{user.Status}' to '{targetStatus}'.\");");
                        method.AddStatement("    if (string.Equals(user.Status, targetStatus, StringComparison.Ordinal))");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        return user;");
                        method.AddStatement("    }");
                        method.AddStatement("    var updated = user with { Status = targetStatus, UpdatedAt = now, ConcurrencyToken = newConcurrencyToken() };");
                        method.AddStatement("    await operation.Records.UpdateAsync(updated, user.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("    if (IsTerminalUserStatus(targetStatus))");
                        method.AddStatement("    {");
                        method.AddStatement("        await RevokeUserDependentsAsync(operation.Records, userId, findSsoSessionsByUser, findRefreshTokensByUser, findApiKeysByOwner, findRoleMembershipsByPrincipal, findGrantsByPrincipal, now, newConcurrencyToken, cancellationToken);");
                        method.AddStatement("    }");
                        method.AddStatement("    await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("    return updated;");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityService>", "CreateServiceAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("SecurityAuthorityService", "service");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string?, string, CancellationToken, ValueTask<SecurityAuthorityService?>>", "findServiceByTenantAndName");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(persistence);
                            ArgumentNullException.ThrowIfNull(service);
                            ArgumentNullException.ThrowIfNull(findServiceByTenantAndName);
                            ArgumentNullException.ThrowIfNull(authorizationInvalidator);
                            EnsureValid(SecurityAuthorityValidation.ValidateService(service, null, service.TenantId));
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            var existing = await findServiceByTenantAndName(operation.Records, service.TenantId, service.Name, cancellationToken);
                            if (existing is not null)
                            {
                            throw new InvalidOperationException($"A Service named '{service.Name}' already exists in Tenant '{service.TenantId ?? "<global>"}'.");
                            }

                            await operation.Records.AddAsync(service, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.Service, AuthorizationTenantId(service.TenantId), service.Id);
                            return service;
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<string>", "ProvisionServiceAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("SecurityAuthorityService", "service");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string?, string, CancellationToken, ValueTask<SecurityAuthorityService?>>", "findServiceByTenantAndName");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(persistence);
                            ArgumentNullException.ThrowIfNull(service);
                            ArgumentNullException.ThrowIfNull(findServiceByTenantAndName);
                            ArgumentNullException.ThrowIfNull(authorizationInvalidator);
                            EnsureValid(SecurityAuthorityValidation.ValidateService(service, null, service.TenantId));
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            var existing = await findServiceByTenantAndName(operation.Records, service.TenantId, service.Name, cancellationToken);
                            if (existing is not null)
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return existing.Id;
                            }

                            await operation.Records.AddAsync(service, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.Service, AuthorizationTenantId(service.TenantId), service.Id);
                            return service.Id;
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityService?>", "ReadServiceAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(records);");
                        method.AddStatement("ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);");
                        method.AddStatement("return await records.LoadAsync(typeof(SecurityAuthorityService), serviceId, cancellationToken) as SecurityAuthorityService;");
                    });
                    @class.AddMethod("ValueTask<IReadOnlyList<SecurityAuthorityService>>", "ListServicesAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("string?", "tenantId");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string?, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityService>>>", "listServices");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(records);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(listServices);");
                        method.AddStatement("return await listServices(records, tenantId, cancellationToken) ?? throw new InvalidOperationException(\"The Service list query returned null.\");");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityService>", "UpdateServiceAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("string", "name");
                        method.AddParameter("string?", "description");
                        method.AddParameter("string?", "tenantId");
                        method.AddParameter("string", "expectedConcurrencyToken");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string?, string, CancellationToken, ValueTask<SecurityAuthorityService?>>", "findServiceByTenantAndName");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(persistence);
                            ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
                            ArgumentException.ThrowIfNullOrWhiteSpace(name);
                            ArgumentException.ThrowIfNullOrWhiteSpace(expectedConcurrencyToken);
                            ArgumentNullException.ThrowIfNull(findServiceByTenantAndName);
                            ArgumentNullException.ThrowIfNull(utcNow);
                            ArgumentNullException.ThrowIfNull(newConcurrencyToken);
                            ArgumentNullException.ThrowIfNull(authorizationInvalidator);
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            var current = await operation.Records.LoadAsync(typeof(SecurityAuthorityService), serviceId, cancellationToken) as SecurityAuthorityService
                            ?? throw new InvalidOperationException("The Service could not be loaded.");
                            if (!string.Equals(current.ConcurrencyToken, expectedConcurrencyToken, StringComparison.Ordinal))
                            {
                            throw new InvalidOperationException("The Service concurrency token is stale.");
                            }

                            var duplicate = await findServiceByTenantAndName(operation.Records, tenantId, name, cancellationToken);
                            if (duplicate is not null && !string.Equals(duplicate.Id, serviceId, StringComparison.Ordinal))
                            {
                            throw new InvalidOperationException($"A Service named '{name}' already exists in Tenant '{tenantId ?? "<global>"}'.");
                            }

                            var candidate = current with { Name = name, Description = description, TenantId = tenantId, UpdatedAt = utcNow() };
                            EnsureValid(SecurityAuthorityValidation.ValidateService(candidate, current, tenantId));
                            var updated = candidate with { ConcurrencyToken = newConcurrencyToken() };
                            await operation.Records.UpdateAsync(updated, expectedConcurrencyToken, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.Service, AuthorizationTenantId(current.TenantId), serviceId);
                            if (!string.Equals(current.TenantId, tenantId, StringComparison.Ordinal))
                            {
                            authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.Service, AuthorizationTenantId(tenantId), serviceId);
                            }
                            return updated;
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityService>", "ActivateServiceAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityApiKey>>>", "findApiKeysByOwner");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRefreshToken>>>", "findRefreshTokensByService");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("return await ChangeServiceStateAsync(persistence, serviceId, true, false, findApiKeysByOwner, findRefreshTokensByService, utcNow, newConcurrencyToken, authorizationInvalidator, cancellationToken);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityRoleMembership>", "AssignServiceRoleAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("string", "roleId");
                        method.AddParameter("DateTimeOffset?", "expiresAt");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<SecurityAuthorityRoleMembership?>>", "findServiceRoleMembership");
                        method.AddParameter("Func<string>", "newId");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(persistence);
                            ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
                            ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
                            ArgumentNullException.ThrowIfNull(findServiceRoleMembership);
                            ArgumentNullException.ThrowIfNull(newId);
                            ArgumentNullException.ThrowIfNull(utcNow);
                            ArgumentNullException.ThrowIfNull(newConcurrencyToken);
                            ArgumentNullException.ThrowIfNull(authorizationInvalidator);
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            var service = await operation.Records.LoadAsync(typeof(SecurityAuthorityService), serviceId, cancellationToken) as SecurityAuthorityService
                            ?? throw new InvalidOperationException("The Service could not be loaded.");
                            if (!IsActiveService(service)) throw new InvalidOperationException("Roles can only be assigned to an active Service.");
                            var role = await operation.Records.LoadAsync(typeof(SecurityAuthorityRole), roleId, cancellationToken) as SecurityAuthorityRole
                            ?? throw new InvalidOperationException("The Role could not be loaded.");
                            if (!role.IsEnabled) throw new InvalidOperationException("The Role is not enabled.");
                            if (role.TenantId is not null && !string.Equals(role.TenantId, service.TenantId, StringComparison.Ordinal))
                            {
                            throw new InvalidOperationException("The Role and Service belong to different Tenants.");
                            }

                            var existing = await findServiceRoleMembership(operation.Records, serviceId, roleId, cancellationToken);
                            if (existing is not null && CanUseRoleMembership(existing, null, service, utcNow()))
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return existing;
                            }

                            var now = utcNow();
                            var membership = new SecurityAuthorityRoleMembership(newId(), roleId, null, serviceId, expiresAt, false, null, null, now, newConcurrencyToken());
                            await operation.Records.AddAsync(membership, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.RoleMembership, AuthorizationTenantId(service.TenantId), membership.Id);
                            return membership;
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask", "RemoveServiceRoleAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("string", "membershipId");
                        method.AddParameter("string?", "reason");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(persistence);
                            ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
                            ArgumentException.ThrowIfNullOrWhiteSpace(membershipId);
                            ArgumentNullException.ThrowIfNull(utcNow);
                            ArgumentNullException.ThrowIfNull(newConcurrencyToken);
                            ArgumentNullException.ThrowIfNull(authorizationInvalidator);
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            var service = await operation.Records.LoadAsync(typeof(SecurityAuthorityService), serviceId, cancellationToken) as SecurityAuthorityService
                            ?? throw new InvalidOperationException("The Service could not be loaded.");
                            var membership = await operation.Records.LoadAsync(typeof(SecurityAuthorityRoleMembership), membershipId, cancellationToken) as SecurityAuthorityRoleMembership
                            ?? throw new InvalidOperationException("The Role Membership could not be loaded.");
                            if (!string.Equals(membership.ServiceId, serviceId, StringComparison.Ordinal) || membership.UserId is not null)
                            {
                            throw new InvalidOperationException("The Role Membership does not belong to the Service.");
                            }
                            if (!membership.IsRevoked)
                            {
                            var now = utcNow();
                            await operation.Records.UpdateAsync(membership with { IsRevoked = true, RevokedAt = now, Reason = reason, ConcurrencyToken = newConcurrencyToken() }, membership.ConcurrencyToken, cancellationToken);
                            }
                            await operation.CommitAsync(cancellationToken);
                            authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.RoleMembership, AuthorizationTenantId(service.TenantId), membership.Id);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityGrant>", "AssignServiceGrantAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("string", "tenantResourceId");
                        method.AddParameter("string", "resourceKind");
                        method.AddParameter("string", "permissionKey");
                        method.AddParameter("string", "effect");
                        method.AddParameter("string", "applicability");
                        method.AddParameter("DateTimeOffset?", "expiresAt");
                        method.AddParameter("string?", "reason");
                        method.AddParameter("Func<string>", "newId");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(persistence);
                            ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
                            ArgumentException.ThrowIfNullOrWhiteSpace(tenantResourceId);
                            ArgumentException.ThrowIfNullOrWhiteSpace(resourceKind);
                            ArgumentException.ThrowIfNullOrWhiteSpace(permissionKey);
                            if (!string.Equals(effect, "Allow", StringComparison.Ordinal) && !string.Equals(effect, "Deny", StringComparison.Ordinal)) throw new ArgumentOutOfRangeException(nameof(effect));
                            if (!string.Equals(applicability, "ThisResourceOnly", StringComparison.Ordinal) && !string.Equals(applicability, "ThisResourceAndDescendants", StringComparison.Ordinal)) throw new ArgumentOutOfRangeException(nameof(applicability));
                            ArgumentNullException.ThrowIfNull(newId);
                            ArgumentNullException.ThrowIfNull(utcNow);
                            ArgumentNullException.ThrowIfNull(newConcurrencyToken);
                            ArgumentNullException.ThrowIfNull(authorizationInvalidator);
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            var service = await operation.Records.LoadAsync(typeof(SecurityAuthorityService), serviceId, cancellationToken) as SecurityAuthorityService
                            ?? throw new InvalidOperationException("The Service could not be loaded.");
                            if (!IsActiveService(service)) throw new InvalidOperationException("Grants can only be assigned to an active Service.");
                            var now = utcNow();
                            var grant = new SecurityAuthorityGrant(newId(), "Service", serviceId, tenantResourceId, resourceKind, permissionKey, effect, applicability, expiresAt, false, null, reason, service.TenantId, now, now, newConcurrencyToken());
                            await operation.Records.AddAsync(grant, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.Grant, AuthorizationTenantId(service.TenantId), grant.Id);
                            return grant;
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask", "RemoveServiceGrantAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("string", "grantId");
                        method.AddParameter("string?", "reason");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(persistence);
                            ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
                            ArgumentException.ThrowIfNullOrWhiteSpace(grantId);
                            ArgumentNullException.ThrowIfNull(utcNow);
                            ArgumentNullException.ThrowIfNull(newConcurrencyToken);
                            ArgumentNullException.ThrowIfNull(authorizationInvalidator);
                            await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            var service = await operation.Records.LoadAsync(typeof(SecurityAuthorityService), serviceId, cancellationToken) as SecurityAuthorityService
                            ?? throw new InvalidOperationException("The Service could not be loaded.");
                            var grant = await operation.Records.LoadAsync(typeof(SecurityAuthorityGrant), grantId, cancellationToken) as SecurityAuthorityGrant
                            ?? throw new InvalidOperationException("The Grant could not be loaded.");
                            if (!string.Equals(grant.PrincipalType, "Service", StringComparison.Ordinal) || !string.Equals(grant.PrincipalId, serviceId, StringComparison.Ordinal))
                            {
                            throw new InvalidOperationException("The Grant does not belong to the Service.");
                            }
                            if (!grant.IsRevoked)
                            {
                            var now = utcNow();
                            await operation.Records.UpdateAsync(grant with { IsRevoked = true, RevokedAt = now, Reason = reason, UpdatedAt = now, ConcurrencyToken = newConcurrencyToken() }, grant.ConcurrencyToken, cancellationToken);
                            }
                            await operation.CommitAsync(cancellationToken);
                            authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.Grant, AuthorizationTenantId(service.TenantId), grant.Id);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityService>", "DeactivateServiceAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityApiKey>>>", "findApiKeysByOwner");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRefreshToken>>>", "findRefreshTokensByService");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("return await ChangeServiceStateAsync(persistence, serviceId, false, false, findApiKeysByOwner, findRefreshTokensByService, utcNow, newConcurrencyToken, authorizationInvalidator, cancellationToken);");
                    });
                    @class.AddMethod("ValueTask", "DeleteServiceAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityApiKey>>>", "findApiKeysByOwner");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRefreshToken>>>", "findRefreshTokensByService");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("await ChangeServiceStateAsync(persistence, serviceId, false, true, findApiKeysByOwner, findRefreshTokensByService, utcNow, newConcurrencyToken, authorizationInvalidator, cancellationToken);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityService>", "ChangeServiceStateAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("bool", "isActive");
                        method.AddParameter("bool", "delete");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityApiKey>>>", "findApiKeysByOwner");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRefreshToken>>>", "findRefreshTokensByService");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        method.AddStatement("ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findApiKeysByOwner);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(findRefreshTokensByService);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(newConcurrencyToken);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(authorizationInvalidator);");
                        method.AddStatement("var now = utcNow();");
                        method.AddStatement("await using var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var service = await operation.Records.LoadAsync(typeof(SecurityAuthorityService), serviceId, cancellationToken) as SecurityAuthorityService ?? throw new InvalidOperationException(\"The Service could not be loaded.\");");
                        method.AddStatement("    var updated = service with { IsActive = isActive, UpdatedAt = now, ConcurrencyToken = newConcurrencyToken() };");
                        method.AddStatement("    if (service.IsActive != isActive) await operation.Records.UpdateAsync(updated, service.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("    if (!isActive) await RevokeServiceDependentsAsync(operation.Records, serviceId, findApiKeysByOwner, findRefreshTokensByService, now, newConcurrencyToken, cancellationToken);");
                        method.AddStatement("    if (delete) await operation.Records.DeleteAsync(typeof(SecurityAuthorityService), service.Id, service.IsActive != isActive ? updated.ConcurrencyToken : service.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("    await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("    authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.Service, AuthorizationTenantId(service.TenantId), service.Id);");
                        method.AddStatement("    return updated;");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask", "RevokeUserDependentsAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("string", "userId");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthoritySsoSession>>>", "findSsoSessionsByUser");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRefreshToken>>>", "findRefreshTokensByUser");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityApiKey>>>", "findApiKeysByOwner");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRoleMembership>>>", "findRoleMembershipsByPrincipal");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityGrant>>>", "findGrantsByPrincipal");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("foreach (var session in await findSsoSessionsByUser(records, userId, cancellationToken))");
                        method.AddStatement("{");
                        method.AddStatement("    if (session.RevokedAt is null) await records.UpdateAsync(session with { RevokedAt = now, ConcurrencyToken = newConcurrencyToken() }, session.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("foreach (var token in await findRefreshTokensByUser(records, userId, cancellationToken))");
                        method.AddStatement("{");
                        method.AddStatement("    if (token.RevokedAt is null) await records.UpdateAsync(token with { RevokedAt = now, ConcurrencyToken = newConcurrencyToken() }, token.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("foreach (var apiKey in await findApiKeysByOwner(records, \"User\", userId, cancellationToken))");
                        method.AddStatement("{");
                        method.AddStatement("    if (!apiKey.IsRevoked) await records.UpdateAsync(apiKey with { IsRevoked = true, RevokedAt = now, UpdatedAt = now, ConcurrencyToken = newConcurrencyToken() }, apiKey.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("foreach (var membership in await findRoleMembershipsByPrincipal(records, \"User\", userId, cancellationToken))");
                        method.AddStatement("{");
                        method.AddStatement("    if (!membership.IsRevoked) await records.UpdateAsync(membership with { IsRevoked = true, RevokedAt = now, Reason = \"User lifecycle cascade\", ConcurrencyToken = newConcurrencyToken() }, membership.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("foreach (var grant in await findGrantsByPrincipal(records, \"User\", userId, cancellationToken))");
                        method.AddStatement("{");
                        method.AddStatement("    if (!grant.IsRevoked) await records.UpdateAsync(grant with { IsRevoked = true, RevokedAt = now, Reason = \"User lifecycle cascade\", UpdatedAt = now, ConcurrencyToken = newConcurrencyToken() }, grant.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask", "RevokeServiceDependentsAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("string", "serviceId");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityApiKey>>>", "findApiKeysByOwner");
                        method.AddParameter("Func<ISecurityAuthorityRecordStore, string, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityRefreshToken>>>", "findRefreshTokensByService");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("Func<string>", "newConcurrencyToken");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("foreach (var apiKey in await findApiKeysByOwner(records, \"Service\", serviceId, cancellationToken))");
                        method.AddStatement("{");
                        method.AddStatement("    if (!apiKey.IsRevoked) await records.UpdateAsync(apiKey with { IsRevoked = true, RevokedAt = now, UpdatedAt = now, ConcurrencyToken = newConcurrencyToken() }, apiKey.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("foreach (var token in await findRefreshTokensByService(records, serviceId, cancellationToken))");
                        method.AddStatement("{");
                        method.AddStatement("    if (token.RevokedAt is null) await records.UpdateAsync(token with { RevokedAt = now, ConcurrencyToken = newConcurrencyToken() }, token.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("void", "EnsureValid", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityValidationResult", "validation");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(validation);");
                        method.AddStatement("if (!validation.IsValid) throw new InvalidOperationException(\"The Service mutation is invalid: \" + string.Join(\"; \", validation.Failures));");
                    });
                    @class.AddMethod("string", "AuthorizationTenantId", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string?", "tenantId");
                        method.AddStatement("return tenantId ?? \"__global__\";");
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
