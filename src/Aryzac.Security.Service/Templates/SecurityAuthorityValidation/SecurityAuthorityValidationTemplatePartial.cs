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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityValidation
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityValidationTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityValidation";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityValidationTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddRecord("SecurityAuthorityValidationFailure", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Field");
                        ctor.AddParameter("string", "Code");
                        ctor.AddParameter("string", "Message");
                    });
                })
                .AddClass("SecurityAuthorityValidationResult", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("IReadOnlyList<SecurityAuthorityValidationFailure>", "Failures", property => property.WithoutSetter());
                    @class.AddProperty("bool", "IsValid", property =>
                    {
                        property.Getter.WithExpressionImplementation("Failures.Count == 0");
                        property.WithoutSetter();
                    });
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("IReadOnlyList<SecurityAuthorityValidationFailure>", "failures");
                        ctor.AddStatement("Failures = failures ?? throw new ArgumentNullException(nameof(failures));");
                    });
                })
                .AddRecord("SecurityAuthorityTenantAncestry", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("IReadOnlyList<SecurityAuthorityTenantResource>", "Resources");
                        ctor.AddParameter("string?", "InheritanceBoundaryTenantResourceId");
                    });
                })
                .AddRecord("SecurityAuthorityTenantValidationResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityValidationResult", "Validation");
                        ctor.AddParameter("SecurityAuthorityTenantAncestry?", "Ancestry");
                    });
                })
                .AddRecord("SecurityAuthorityMutationResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityValidationResult", "Validation");
                        ctor.AddParameter("SecurityAuthorityCommitReceipt?", "CommitReceipt");
                    });
                })
                .AddInterface("ISecurityAuthorityValidationContext", @interface =>
                {
                    @interface.AddMethod("ValueTask<bool>", "ExistsAsync", method =>
                    {
                        method.AddParameter("string", "recordType");
                        method.AddParameter("string", "recordId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<bool>", "IsUniqueAsync", method =>
                    {
                        method.AddParameter("string", "recordType");
                        method.AddParameter("string", "field");
                        method.AddParameter("string", "value");
                        method.AddParameter("string?", "excludingRecordId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask<string?>", "GetTenantIdAsync", method =>
                    {
                        method.AddParameter("string", "recordType");
                        method.AddParameter("string", "recordId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddClass("SecurityAuthorityValidation", @class =>
                {
                    @class.Static();
                    @class.AddMethod("SecurityAuthorityValidationResult", "ValidateUser", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityUser", "record");
                        method.AddParameter("SecurityAuthorityUser?", "existing");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(record);");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("Length(failures, nameof(record.DisplayName), record.DisplayName, 1, 200);");
                        method.AddStatement("Length(failures, nameof(record.NormalizedEmail), record.NormalizedEmail, 3, 320);");
                        method.AddStatement("OptionalAbsoluteUri(failures, nameof(record.AvatarUrl), record.AvatarUrl, 2048);");
                        method.AddStatement("OneOf(failures, nameof(record.Status), record.Status, \"New\", \"Active\", \"Suspended\", \"Archived\", \"Deleted\");");
                        method.AddStatement("Utc(failures, nameof(record.CreatedAt), record.CreatedAt);");
                        method.AddStatement("Utc(failures, nameof(record.UpdatedAt), record.UpdatedAt);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.LastSeenAt), record.LastSeenAt);");
                        method.AddStatement("Chronology(failures, nameof(record.UpdatedAt), record.CreatedAt, record.UpdatedAt);");
                        method.AddStatement("OptionalChronology(failures, nameof(record.LastSeenAt), record.CreatedAt, record.LastSeenAt);");
                        method.AddStatement("Stable(failures, record.Id, record.CreatedAt, existing?.Id, existing?.CreatedAt);");
                        method.AddStatement("Concurrency(failures, nameof(record.ConcurrencyToken), record.ConcurrencyToken, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateExternalIdentityAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityExternalIdentity", "record");
                        method.AddParameter("ISecurityAuthorityValidationContext", "context");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(record);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(context);");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("AbsoluteUri(failures, nameof(record.Issuer), record.Issuer, 2048);");
                        method.AddStatement("Length(failures, nameof(record.Subject), record.Subject, 1, 255);");
                        method.AddStatement("Required(failures, nameof(record.UserId), record.UserId);");
                        method.AddStatement("Utc(failures, nameof(record.CreatedAt), record.CreatedAt);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.LastSeenAt), record.LastSeenAt);");
                        method.AddStatement("OptionalChronology(failures, nameof(record.LastSeenAt), record.CreatedAt, record.LastSeenAt);");
                        method.AddStatement("if (!await context.ExistsAsync(\"User\", record.UserId, cancellationToken)) Failure(failures, nameof(record.UserId), \"unknown_reference\", \"The referenced User does not exist.\");");
                        method.AddStatement("var identityKey = record.Issuer + \"\\u001f\" + record.Subject;");
                        method.AddStatement("if (!await context.IsUniqueAsync(\"ExternalIdentity\", \"IssuerSubject\", identityKey, record.Id, cancellationToken)) Failure(failures, nameof(record.Subject), \"not_unique\", \"Issuer and Subject must be globally unique using ordinal case-sensitive comparison.\");");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("SecurityAuthorityValidationResult", "ValidateService", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityService", "record");
                        method.AddParameter("SecurityAuthorityService?", "existing");
                        method.AddParameter("string?", "currentTenantId");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("Length(failures, nameof(record.Name), record.Name, 1, 200);");
                        method.AddStatement("OptionalLength(failures, nameof(record.Description), record.Description, 2000);");
                        method.AddStatement("TenantOwnership(failures, nameof(record.TenantId), record.TenantId, currentTenantId);");
                        method.AddStatement("MutableLifecycle(failures, record.Id, record.CreatedAt, record.UpdatedAt, record.ConcurrencyToken, existing?.Id, existing?.CreatedAt, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateOAuthClientAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityOAuthClient", "record");
                        method.AddParameter("SecurityAuthorityOAuthClient?", "existing");
                        method.AddParameter("string?", "currentTenantId");
                        method.AddParameter("ISecurityAuthorityValidationContext", "context");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("Length(failures, nameof(record.ClientIdentifier), record.ClientIdentifier, 1, 200);");
                        method.AddStatement("Length(failures, nameof(record.DisplayName), record.DisplayName, 1, 200);");
                        method.AddStatement("OneOf(failures, nameof(record.ClientType), record.ClientType, \"Public\", \"Confidential\");");
                        method.AddStatement("if (string.Equals(record.ClientType, \"Public\", StringComparison.Ordinal) && !string.IsNullOrEmpty(record.SecretHash)) Failure(failures, nameof(record.SecretHash), \"forbidden\", \"Public clients cannot retain a client secret hash.\");");
                        method.AddStatement("if (string.Equals(record.ClientType, \"Confidential\", StringComparison.Ordinal)) Required(failures, nameof(record.SecretHash), record.SecretHash);");
                        method.AddStatement("Uris(failures, nameof(record.RedirectUris), record.RedirectUris);");
                        method.AddStatement("Uris(failures, nameof(record.PostLogoutRedirectUris), record.PostLogoutRedirectUris);");
                        method.AddStatement("RequiredValues(failures, nameof(record.AllowedGrantTypes), record.AllowedGrantTypes);");
                        method.AddStatement("Values(failures, nameof(record.AllowedScopes), record.AllowedScopes);");
                        method.AddStatement("TenantOwnership(failures, nameof(record.TenantId), record.TenantId, currentTenantId);");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(record.PreferredIdentityProviderId))");
                        method.AddStatement("{");
                        method.AddStatement("    if (!await context.ExistsAsync(\"IdentityProvider\", record.PreferredIdentityProviderId, cancellationToken)) Failure(failures, nameof(record.PreferredIdentityProviderId), \"unknown_reference\", \"The preferred Identity Provider does not exist.\");");
                        method.AddStatement("    else");
                        method.AddStatement("    {");
                        method.AddStatement("        var providerTenant = await context.GetTenantIdAsync(\"IdentityProvider\", record.PreferredIdentityProviderId, cancellationToken);");
                        method.AddStatement("        if (providerTenant is not null && !string.Equals(providerTenant, record.TenantId, StringComparison.Ordinal)) Failure(failures, nameof(record.PreferredIdentityProviderId), \"tenant_mismatch\", \"The preferred Identity Provider belongs to another Tenant.\");");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("if (!await context.IsUniqueAsync(\"OAuthClient\", nameof(record.ClientIdentifier), record.ClientIdentifier, record.Id, cancellationToken)) Failure(failures, nameof(record.ClientIdentifier), \"not_unique\", \"Client Identifier must be globally unique using ordinal case-sensitive comparison.\");");
                        method.AddStatement("MutableLifecycle(failures, record.Id, record.CreatedAt, record.UpdatedAt, record.ConcurrencyToken, existing?.Id, existing?.CreatedAt, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateIdentityProviderAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityIdentityProvider", "record");
                        method.AddParameter("SecurityAuthorityIdentityProvider?", "existing");
                        method.AddParameter("string?", "currentTenantId");
                        method.AddParameter("ISecurityAuthorityTenantAdapter", "tenantAdapter");
                        method.AddParameter("ISecurityAuthorityValidationContext", "context");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("Length(failures, nameof(record.ProviderIdentifier), record.ProviderIdentifier, 1, 100);");
                        method.AddStatement("OneOf(failures, nameof(record.ProviderType), record.ProviderType, \"GenericOidc\", \"EntraExternalId\", \"EntraId\", \"Google\", \"Auth0\", \"Keycloak\");");
                        method.AddStatement("Length(failures, nameof(record.DisplayName), record.DisplayName, 1, 200);");
                        method.AddStatement("AbsoluteUri(failures, nameof(record.AuthorityUrl), record.AuthorityUrl, 2048);");
                        method.AddStatement("OptionalAbsoluteUri(failures, nameof(record.Issuer), record.Issuer, 2048);");
                        method.AddStatement("Length(failures, nameof(record.ClientIdentifier), record.ClientIdentifier, 1, 200);");
                        method.AddStatement("Required(failures, nameof(record.EncryptedClientSecret), record.EncryptedClientSecret);");
                        method.AddStatement("SpaceDelimitedValues(failures, nameof(record.RequestedScopes), record.RequestedScopes);");
                        method.AddStatement("if (record.DisplayPriority < 0) Failure(failures, nameof(record.DisplayPriority), \"out_of_range\", \"Display Priority cannot be negative.\");");
                        method.AddStatement("OneOf(failures, nameof(record.AccessMode), record.AccessMode, \"InviteOnly\", \"OpenSso\");");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(record.TenantResourceId))");
                        method.AddStatement("{");
                        method.AddStatement("    if (string.IsNullOrWhiteSpace(currentTenantId)) Failure(failures, nameof(record.TenantResourceId), \"tenant_context_required\", \"A Tenant context is required for a tenant-scoped Identity Provider.\");");
                        method.AddStatement("    if (!string.IsNullOrWhiteSpace(currentTenantId))");
                        method.AddStatement("    {");
                        method.AddStatement("        var tenant = await ValidateTenantResourceAsync(tenantAdapter, currentTenantId, record.TenantResourceId, null, cancellationToken);");
                        method.AddStatement("        AddFailures(failures, tenant.Validation, nameof(record.TenantResourceId), nameof(record.TenantResourceId), nameof(record.TenantResourceId));");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("if (!await context.IsUniqueAsync(\"IdentityProvider\", nameof(record.ProviderIdentifier), record.ProviderIdentifier, record.Id, cancellationToken)) Failure(failures, nameof(record.ProviderIdentifier), \"not_unique\", \"Provider Identifier must be globally unique using ordinal case-sensitive comparison.\");");
                        method.AddStatement("MutableLifecycle(failures, record.Id, record.CreatedAt, record.UpdatedAt, record.ConcurrencyToken, existing?.Id, existing?.CreatedAt, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateApiKeyAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityApiKey", "record");
                        method.AddParameter("SecurityAuthorityApiKey?", "existing");
                        method.AddParameter("string?", "currentTenantId");
                        method.AddParameter("ISecurityAuthorityValidationContext", "context");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("Length(failures, nameof(record.Name), record.Name, 1, 200);");
                        method.AddStatement("OneOf(failures, nameof(record.OwnerPrincipalType), record.OwnerPrincipalType, \"User\", \"Service\");");
                        method.AddStatement("Required(failures, nameof(record.OwnerId), record.OwnerId);");
                        method.AddStatement("Required(failures, nameof(record.PublicPrefix), record.PublicPrefix);");
                        method.AddStatement("Required(failures, nameof(record.KeyHash), record.KeyHash);");
                        method.AddStatement("TenantOwnership(failures, nameof(record.TenantId), record.TenantId, currentTenantId);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.ExpiresAt), record.ExpiresAt);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.RevokedAt), record.RevokedAt);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.LastUsedAt), record.LastUsedAt);");
                        method.AddStatement("Revocation(failures, record.IsRevoked, record.RevokedAt);");
                        method.AddStatement("OptionalChronology(failures, nameof(record.ExpiresAt), record.CreatedAt, record.ExpiresAt);");
                        method.AddStatement("if (string.Equals(record.OwnerPrincipalType, \"User\", StringComparison.Ordinal) || string.Equals(record.OwnerPrincipalType, \"Service\", StringComparison.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    var ownerType = record.OwnerPrincipalType;");
                        method.AddStatement("    if (!await context.ExistsAsync(ownerType, record.OwnerId, cancellationToken)) Failure(failures, nameof(record.OwnerId), \"unknown_reference\", \"The API Key owner does not exist.\");");
                        method.AddStatement("    else if (record.TenantId is not null)");
                        method.AddStatement("    {");
                        method.AddStatement("        var ownerTenant = await context.GetTenantIdAsync(ownerType, record.OwnerId, cancellationToken);");
                        method.AddStatement("        if (ownerTenant is not null && !string.Equals(ownerTenant, record.TenantId, StringComparison.Ordinal)) Failure(failures, nameof(record.OwnerId), \"tenant_mismatch\", \"The API Key and its owner must belong to the same Tenant.\");");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("MutableLifecycle(failures, record.Id, record.CreatedAt, record.UpdatedAt, record.ConcurrencyToken, existing?.Id, existing?.CreatedAt, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityTenantValidationResult>", "ValidateTenantResourceAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityTenantAdapter", "tenantAdapter");
                        method.AddParameter("string", "tenantId");
                        method.AddParameter("string", "tenantResourceId");
                        method.AddParameter("string?", "expectedResourceKind");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(tenantAdapter);");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(tenantId), tenantId);");
                        method.AddStatement("Required(failures, nameof(tenantResourceId), tenantResourceId);");
                        method.AddStatement("if (failures.Count != 0) return new SecurityAuthorityTenantValidationResult(Result(failures), null);");
                        method.AddStatement("var resources = new List<SecurityAuthorityTenantResource>();");
                        method.AddStatement("var visited = new HashSet<string>(StringComparer.Ordinal);");
                        method.AddStatement("var currentId = tenantResourceId;");
                        method.AddStatement("string? inheritanceBoundary = null;");
                        method.AddStatement("while (currentId is not null)");
                        method.AddStatement("{");
                        method.AddStatement("    if (!visited.Add(currentId))");
                        method.AddStatement("    {");
                        method.AddStatement("        Failure(failures, \"ParentTenantResourceId\", \"cyclic_parentage\", \"The Tenant Resource parent chain contains a cycle.\");");
                        method.AddStatement("        break;");
                        method.AddStatement("    }");
                        method.AddStatement("    var resource = await tenantAdapter.ResolveAsync(tenantId, currentId, cancellationToken);");
                        method.AddStatement("    if (resource is null)");
                        method.AddStatement("    {");
                        method.AddStatement("        Failure(failures, \"TenantResourceId\", \"unknown_reference\", \"The Tenant Adapter returned an unknown Tenant Resource.\");");
                        method.AddStatement("        break;");
                        method.AddStatement("    }");
                        method.AddStatement("    if (!string.Equals(resource.TenantResourceId, currentId, StringComparison.Ordinal))");
                        method.AddStatement("    {");
                        method.AddStatement("        Failure(failures, \"TenantResourceId\", \"identity_mismatch\", \"The Tenant Adapter returned a different Tenant Resource identifier.\");");
                        method.AddStatement("        break;");
                        method.AddStatement("    }");
                        method.AddStatement("    if (!string.Equals(resource.TenantId, tenantId, StringComparison.Ordinal))");
                        method.AddStatement("    {");
                        method.AddStatement("        Failure(failures, \"TenantId\", \"tenant_mismatch\", \"Every Tenant Resource in one ancestry chain must belong to the same Tenant.\");");
                        method.AddStatement("        break;");
                        method.AddStatement("    }");
                        method.AddStatement("""    Required(failures, "ResourceKind", resource.ResourceKind);""");
                        method.AddStatement("    if (resources.Count == 0 && expectedResourceKind is not null && !string.Equals(resource.ResourceKind, expectedResourceKind, StringComparison.Ordinal)) Failure(failures, \"ResourceKind\", \"resource_kind_mismatch\", \"Resource Kind comparison is ordinal and case-sensitive.\");");
                        method.AddStatement("    resources.Add(resource);");
                        method.AddStatement("    if (resource.InheritanceProtected && inheritanceBoundary is null) inheritanceBoundary = resource.TenantResourceId;");
                        method.AddStatement("    currentId = resource.ParentTenantResourceId;");
                        method.AddStatement("}");
                        method.AddStatement("var ancestry = failures.Count == 0 ? new SecurityAuthorityTenantAncestry(resources, inheritanceBoundary) : null;");
                        method.AddStatement("return new SecurityAuthorityTenantValidationResult(Result(failures), ancestry);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateTenantResourceRecordAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityTenantResourceRecord", "record");
                        method.AddParameter("SecurityAuthorityTenantResourceRecord?", "existing");
                        method.AddParameter("ISecurityAuthorityTenantAdapter", "tenantAdapter");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.TenantResourceId), record.TenantResourceId);");
                        method.AddStatement("Required(failures, nameof(record.ResourceKind), record.ResourceKind);");
                        method.AddStatement("Required(failures, nameof(record.TenantId), record.TenantId);");
                        method.AddStatement("if (existing is not null && !string.Equals(record.TenantResourceId, existing.TenantResourceId, StringComparison.Ordinal)) Failure(failures, nameof(record.TenantResourceId), \"immutable\", \"Tenant Resource identifiers must remain stable after creation.\");");
                        method.AddStatement("if (existing is not null && !string.Equals(record.TenantId, existing.TenantId, StringComparison.Ordinal)) Failure(failures, nameof(record.TenantId), \"immutable\", \"Tenant ownership must remain stable after creation.\");");
                        method.AddStatement("if (record.ParentTenantResourceId is not null)");
                        method.AddStatement("{");
                        method.AddStatement("    if (string.Equals(record.ParentTenantResourceId, record.TenantResourceId, StringComparison.Ordinal)) Failure(failures, nameof(record.ParentTenantResourceId), \"cyclic_parentage\", \"A Tenant Resource cannot be its own parent.\");");
                        method.AddStatement("    else if (!string.IsNullOrWhiteSpace(record.TenantId))");
                        method.AddStatement("    {");
                        method.AddStatement("        var parent = await ValidateTenantResourceAsync(tenantAdapter, record.TenantId, record.ParentTenantResourceId, null, cancellationToken);");
                        method.AddStatement("        AddFailures(failures, parent.Validation, nameof(record.ParentTenantResourceId), nameof(record.TenantId), nameof(record.ResourceKind));");
                        method.AddStatement("        if (parent.Ancestry is not null && parent.Ancestry.Resources.Any(x => string.Equals(x.TenantResourceId, record.TenantResourceId, StringComparison.Ordinal))) Failure(failures, nameof(record.ParentTenantResourceId), \"cyclic_parentage\", \"The Tenant Resource parent chain contains this Tenant Resource.\");");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("Concurrency(failures, nameof(record.ConcurrencyToken), record.ConcurrencyToken, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("SecurityAuthorityValidationResult", "ValidateBootstrapState", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityBootstrapState", "record");
                        method.AddParameter("SecurityAuthorityBootstrapState?", "existing");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("if (record.IsClosed && string.IsNullOrWhiteSpace(record.AdministratorUserId)) Failure(failures, nameof(record.AdministratorUserId), \"required\", \"Closed bootstrap state requires the administrator User identifier.\");");
                        method.AddStatement("if (existing?.IsClosed == true && !record.IsClosed) Failure(failures, nameof(record.IsClosed), \"invalid_lifecycle\", \"Closed bootstrap state cannot be reopened.\");");
                        method.AddStatement("MutableLifecycle(failures, record.Id, record.CreatedAt, record.UpdatedAt, record.ConcurrencyToken, existing?.Id, existing?.CreatedAt, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateRoleAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityRole", "record");
                        method.AddParameter("SecurityAuthorityRole?", "existing");
                        method.AddParameter("string", "currentTenantId");
                        method.AddParameter("ISecurityAuthorityTenantAdapter", "tenantAdapter");
                        method.AddParameter("ISecurityAuthorityValidationContext", "context");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("Required(failures, nameof(record.DefinitionTenantResourceId), record.DefinitionTenantResourceId);");
                        method.AddStatement("Length(failures, nameof(record.RoleKey), record.RoleKey, 1, 100);");
                        method.AddStatement("Length(failures, nameof(record.Name), record.Name, 1, 200);");
                        method.AddStatement("OptionalLength(failures, nameof(record.Description), record.Description, 2000);");
                        method.AddStatement("TenantOwnership(failures, nameof(record.TenantId), record.TenantId, currentTenantId);");
                        method.AddStatement("var tenant = await ValidateTenantResourceAsync(tenantAdapter, currentTenantId, record.DefinitionTenantResourceId, null, cancellationToken);");
                        method.AddStatement("AddFailures(failures, tenant.Validation, nameof(record.DefinitionTenantResourceId), nameof(record.TenantId), nameof(record.DefinitionTenantResourceId));");
                        method.AddStatement("var roleKey = record.DefinitionTenantResourceId + \"\\u001f\" + record.RoleKey;");
                        method.AddStatement("if (!await context.IsUniqueAsync(\"Role\", \"DefinitionResourceRoleKey\", roleKey, record.Id, cancellationToken)) Failure(failures, nameof(record.RoleKey), \"not_unique\", \"Role Key must be unique within its Tenant Resource using ordinal case-sensitive comparison.\");");
                        method.AddStatement("MutableLifecycle(failures, record.Id, record.CreatedAt, record.UpdatedAt, record.ConcurrencyToken, existing?.Id, existing?.CreatedAt, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateRoleMembershipAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityRoleMembership", "record");
                        method.AddParameter("SecurityAuthorityRoleMembership?", "existing");
                        method.AddParameter("string", "currentTenantId");
                        method.AddParameter("ISecurityAuthorityValidationContext", "context");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("Required(failures, nameof(record.RoleId), record.RoleId);");
                        method.AddStatement("var memberCount = (string.IsNullOrWhiteSpace(record.UserId) ? 0 : 1) + (string.IsNullOrWhiteSpace(record.ServiceId) ? 0 : 1);");
                        method.AddStatement("if (memberCount != 1) Failure(failures, nameof(record.UserId), \"exclusive_association\", \"Exactly one User or Service must be assigned.\");");
                        method.AddStatement("OptionalLength(failures, nameof(record.Reason), record.Reason, 1000);");
                        method.AddStatement("Utc(failures, nameof(record.CreatedAt), record.CreatedAt);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.ExpiresAt), record.ExpiresAt);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.RevokedAt), record.RevokedAt);");
                        method.AddStatement("Revocation(failures, record.IsRevoked, record.RevokedAt);");
                        method.AddStatement("OptionalChronology(failures, nameof(record.ExpiresAt), record.CreatedAt, record.ExpiresAt);");
                        method.AddStatement("var roleExists = await context.ExistsAsync(\"Role\", record.RoleId, cancellationToken);");
                        method.AddStatement("if (!roleExists) Failure(failures, nameof(record.RoleId), \"unknown_reference\", \"The Role does not exist.\");");
                        method.AddStatement("var memberType = record.UserId is not null ? \"User\" : \"Service\";");
                        method.AddStatement("var memberId = record.UserId ?? record.ServiceId ?? string.Empty;");
                        method.AddStatement("var memberField = record.UserId is not null ? nameof(record.UserId) : nameof(record.ServiceId);");
                        method.AddStatement("var memberExists = memberId.Length != 0 && await context.ExistsAsync(memberType, memberId, cancellationToken);");
                        method.AddStatement("if (memberId.Length != 0 && !memberExists) Failure(failures, memberField, \"unknown_reference\", \"The assigned User or Service does not exist.\");");
                        method.AddStatement("var roleTenant = roleExists ? await context.GetTenantIdAsync(\"Role\", record.RoleId, cancellationToken) : null;");
                        method.AddStatement("var memberTenant = memberExists ? await context.GetTenantIdAsync(memberType, memberId, cancellationToken) : null;");
                        method.AddStatement("if (roleTenant is not null && !string.Equals(roleTenant, currentTenantId, StringComparison.Ordinal)) Failure(failures, nameof(record.RoleId), \"tenant_mismatch\", \"The Role belongs to another Tenant.\");");
                        method.AddStatement("if (memberTenant is not null && !string.Equals(memberTenant, currentTenantId, StringComparison.Ordinal)) Failure(failures, memberField, \"tenant_mismatch\", \"The member belongs to another Tenant.\");");
                        method.AddStatement("Stable(failures, record.Id, record.CreatedAt, existing?.Id, existing?.CreatedAt);");
                        method.AddStatement("Concurrency(failures, nameof(record.ConcurrencyToken), record.ConcurrencyToken, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateGrantAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityGrant", "record");
                        method.AddParameter("SecurityAuthorityGrant?", "existing");
                        method.AddParameter("string", "currentTenantId");
                        method.AddParameter("ISecurityAuthorityTenantAdapter", "tenantAdapter");
                        method.AddParameter("ISecurityAuthorityValidationContext", "context");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("Required(failures, nameof(record.Id), record.Id);");
                        method.AddStatement("OneOf(failures, nameof(record.PrincipalType), record.PrincipalType, \"User\", \"Service\", \"Role\", \"ApiKey\");");
                        method.AddStatement("Required(failures, nameof(record.PrincipalId), record.PrincipalId);");
                        method.AddStatement("Required(failures, nameof(record.TenantResourceId), record.TenantResourceId);");
                        method.AddStatement("Required(failures, nameof(record.ResourceKind), record.ResourceKind);");
                        method.AddStatement("Length(failures, nameof(record.PermissionKey), record.PermissionKey, 1, 200);");
                        method.AddStatement("OneOf(failures, nameof(record.Effect), record.Effect, \"Allow\", \"Deny\");");
                        method.AddStatement("OneOf(failures, nameof(record.Applicability), record.Applicability, \"ThisResourceOnly\", \"ThisResourceAndDescendants\");");
                        method.AddStatement("OptionalLength(failures, nameof(record.Reason), record.Reason, 1000);");
                        method.AddStatement("TenantOwnership(failures, nameof(record.TenantId), record.TenantId, currentTenantId);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.ExpiresAt), record.ExpiresAt);");
                        method.AddStatement("OptionalUtc(failures, nameof(record.RevokedAt), record.RevokedAt);");
                        method.AddStatement("Revocation(failures, record.IsRevoked, record.RevokedAt);");
                        method.AddStatement("OptionalChronology(failures, nameof(record.ExpiresAt), record.CreatedAt, record.ExpiresAt);");
                        method.AddStatement("OptionalChronology(failures, nameof(record.RevokedAt), record.CreatedAt, record.RevokedAt);");
                        method.AddStatement("var tenant = await ValidateTenantResourceAsync(tenantAdapter, currentTenantId, record.TenantResourceId, record.ResourceKind, cancellationToken);");
                        method.AddStatement("AddFailures(failures, tenant.Validation, nameof(record.TenantResourceId), nameof(record.TenantId), nameof(record.ResourceKind));");
                        method.AddStatement("if (new[] { \"User\", \"Service\", \"Role\", \"ApiKey\" }.Contains(record.PrincipalType, StringComparer.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    var principalExists = await context.ExistsAsync(record.PrincipalType, record.PrincipalId, cancellationToken);");
                        method.AddStatement("    if (!principalExists) Failure(failures, nameof(record.PrincipalId), \"unknown_reference\", \"The Grant principal does not exist.\");");
                        method.AddStatement("    else");
                        method.AddStatement("    {");
                        method.AddStatement("        var principalTenant = await context.GetTenantIdAsync(record.PrincipalType, record.PrincipalId, cancellationToken);");
                        method.AddStatement("        if (principalTenant is not null && !string.Equals(principalTenant, currentTenantId, StringComparison.Ordinal)) Failure(failures, nameof(record.PrincipalId), \"tenant_mismatch\", \"The Grant principal belongs to another Tenant.\");");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("MutableLifecycle(failures, record.Id, record.CreatedAt, record.UpdatedAt, record.ConcurrencyToken, existing?.Id, existing?.CreatedAt, existing?.ConcurrencyToken);");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityValidationResult>", "ValidateCredentialRecordsAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddParameter("SecurityAuthorityAuthorizationCode?", "authorizationCode");
                        method.AddParameter("SecurityAuthorityDeviceGrant?", "deviceGrant");
                        method.AddParameter("SecurityAuthorityRefreshToken?", "refreshToken");
                        method.AddParameter("SecurityAuthorityAccessTokenMetadata?", "accessToken");
                        method.AddParameter("SecurityAuthorityIdTokenMetadata?", "idToken");
                        method.AddParameter("SecurityAuthoritySsoSession?", "ssoSession");
                        method.AddParameter("ISecurityAuthorityValidationContext", "context");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(context);");
                        method.AddStatement("var failures = new List<SecurityAuthorityValidationFailure>();");
                        method.AddStatement("if (authorizationCode is not null)");
                        method.AddStatement("{");
                        method.AddStatement("    Required(failures, nameof(authorizationCode.Id), authorizationCode.Id);");
                        method.AddStatement("    Required(failures, nameof(authorizationCode.CodeHash), authorizationCode.CodeHash);");
                        method.AddStatement("    Required(failures, nameof(authorizationCode.OAuthClientId), authorizationCode.OAuthClientId);");
                        method.AddStatement("    Required(failures, nameof(authorizationCode.UserId), authorizationCode.UserId);");
                        method.AddStatement("    AbsoluteUri(failures, nameof(authorizationCode.RedirectUri), authorizationCode.RedirectUri, 2048);");
                        method.AddStatement("    Values(failures, nameof(authorizationCode.Scopes), authorizationCode.Scopes);");
                        method.AddStatement("    Required(failures, nameof(authorizationCode.PkceChallenge), authorizationCode.PkceChallenge);");
                        method.AddStatement("    UtcRange(failures, nameof(authorizationCode.CreatedAt), authorizationCode.CreatedAt, nameof(authorizationCode.ExpiresAt), authorizationCode.ExpiresAt);");
                        method.AddStatement("    OptionalUtc(failures, nameof(authorizationCode.RedeemedAt), authorizationCode.RedeemedAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(authorizationCode.RedeemedAt), authorizationCode.CreatedAt, authorizationCode.RedeemedAt);");
                        method.AddStatement("    if (!await context.ExistsAsync(\"OAuthClient\", authorizationCode.OAuthClientId, cancellationToken)) Failure(failures, nameof(authorizationCode.OAuthClientId), \"unknown_reference\", \"The OAuth Client does not exist.\");");
                        method.AddStatement("    if (!await context.ExistsAsync(\"User\", authorizationCode.UserId, cancellationToken)) Failure(failures, nameof(authorizationCode.UserId), \"unknown_reference\", \"The User does not exist.\");");
                        method.AddStatement("    Concurrency(failures, nameof(authorizationCode.ConcurrencyToken), authorizationCode.ConcurrencyToken, null);");
                        method.AddStatement("}");
                        method.AddStatement("if (deviceGrant is not null)");
                        method.AddStatement("{");
                        method.AddStatement("    Required(failures, nameof(deviceGrant.Id), deviceGrant.Id);");
                        method.AddStatement("    Required(failures, nameof(deviceGrant.DeviceCodeHash), deviceGrant.DeviceCodeHash);");
                        method.AddStatement("    Length(failures, nameof(deviceGrant.UserCode), deviceGrant.UserCode, 8, 8);");
                        method.AddStatement("    Required(failures, nameof(deviceGrant.OAuthClientId), deviceGrant.OAuthClientId);");
                        method.AddStatement("    Values(failures, nameof(deviceGrant.RequestedScopes), deviceGrant.RequestedScopes);");
                        method.AddStatement("    if (deviceGrant.PollingIntervalSeconds < 1) Failure(failures, nameof(deviceGrant.PollingIntervalSeconds), \"out_of_range\", \"Polling interval must be positive.\");");
                        method.AddStatement("    OneOf(failures, nameof(deviceGrant.Status), deviceGrant.Status, \"Pending\", \"Approved\", \"Denied\", \"Expired\", \"Redeemed\");");
                        method.AddStatement("    UtcRange(failures, nameof(deviceGrant.CreatedAt), deviceGrant.CreatedAt, nameof(deviceGrant.ExpiresAt), deviceGrant.ExpiresAt);");
                        method.AddStatement("    OptionalUtc(failures, nameof(deviceGrant.ApprovedAt), deviceGrant.ApprovedAt);");
                        method.AddStatement("    OptionalUtc(failures, nameof(deviceGrant.DeniedAt), deviceGrant.DeniedAt);");
                        method.AddStatement("    OptionalUtc(failures, nameof(deviceGrant.RedeemedAt), deviceGrant.RedeemedAt);");
                        method.AddStatement("    OptionalUtc(failures, nameof(deviceGrant.LastPolledAt), deviceGrant.LastPolledAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(deviceGrant.ApprovedAt), deviceGrant.CreatedAt, deviceGrant.ApprovedAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(deviceGrant.DeniedAt), deviceGrant.CreatedAt, deviceGrant.DeniedAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(deviceGrant.RedeemedAt), deviceGrant.CreatedAt, deviceGrant.RedeemedAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(deviceGrant.LastPolledAt), deviceGrant.CreatedAt, deviceGrant.LastPolledAt);");
                        method.AddStatement("    if (string.Equals(deviceGrant.Status, \"Approved\", StringComparison.Ordinal) && (string.IsNullOrWhiteSpace(deviceGrant.UserId) || deviceGrant.ApprovedAt is null)) Failure(failures, nameof(deviceGrant.Status), \"invalid_lifecycle\", \"Approved Device Grants require a User and approval timestamp.\");");
                        method.AddStatement("    if (string.Equals(deviceGrant.Status, \"Denied\", StringComparison.Ordinal) && deviceGrant.DeniedAt is null) Failure(failures, nameof(deviceGrant.Status), \"invalid_lifecycle\", \"Denied Device Grants require a denial timestamp.\");");
                        method.AddStatement("    if (string.Equals(deviceGrant.Status, \"Redeemed\", StringComparison.Ordinal) && deviceGrant.RedeemedAt is null) Failure(failures, nameof(deviceGrant.Status), \"invalid_lifecycle\", \"Redeemed Device Grants require a redemption timestamp.\");");
                        method.AddStatement("    if (!await context.ExistsAsync(\"OAuthClient\", deviceGrant.OAuthClientId, cancellationToken)) Failure(failures, nameof(deviceGrant.OAuthClientId), \"unknown_reference\", \"The OAuth Client does not exist.\");");
                        method.AddStatement("    if (!string.IsNullOrWhiteSpace(deviceGrant.UserId) && !await context.ExistsAsync(\"User\", deviceGrant.UserId, cancellationToken)) Failure(failures, nameof(deviceGrant.UserId), \"unknown_reference\", \"The User does not exist.\");");
                        method.AddStatement("    Concurrency(failures, nameof(deviceGrant.ConcurrencyToken), deviceGrant.ConcurrencyToken, null);");
                        method.AddStatement("}");
                        method.AddStatement("if (refreshToken is not null)");
                        method.AddStatement("{");
                        method.AddStatement("    Required(failures, nameof(refreshToken.Id), refreshToken.Id);");
                        method.AddStatement("    Required(failures, nameof(refreshToken.TokenHash), refreshToken.TokenHash);");
                        method.AddStatement("    Required(failures, nameof(refreshToken.OAuthClientId), refreshToken.OAuthClientId);");
                        method.AddStatement("    Required(failures, nameof(refreshToken.UserId), refreshToken.UserId);");
                        method.AddStatement("    UtcRange(failures, nameof(refreshToken.IssuedAt), refreshToken.IssuedAt, nameof(refreshToken.ExpiresAt), refreshToken.ExpiresAt);");
                        method.AddStatement("    OptionalUtc(failures, nameof(refreshToken.LastUsedAt), refreshToken.LastUsedAt);");
                        method.AddStatement("    OptionalUtc(failures, nameof(refreshToken.RevokedAt), refreshToken.RevokedAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(refreshToken.LastUsedAt), refreshToken.IssuedAt, refreshToken.LastUsedAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(refreshToken.RevokedAt), refreshToken.IssuedAt, refreshToken.RevokedAt);");
                        method.AddStatement("    if (!await context.ExistsAsync(\"OAuthClient\", refreshToken.OAuthClientId, cancellationToken)) Failure(failures, nameof(refreshToken.OAuthClientId), \"unknown_reference\", \"The OAuth Client does not exist.\");");
                        method.AddStatement("    if (!await context.ExistsAsync(\"User\", refreshToken.UserId, cancellationToken)) Failure(failures, nameof(refreshToken.UserId), \"unknown_reference\", \"The User does not exist.\");");
                        method.AddStatement("    if (!string.IsNullOrWhiteSpace(refreshToken.ReplacedByTokenId) && !await context.ExistsAsync(\"RefreshToken\", refreshToken.ReplacedByTokenId, cancellationToken)) Failure(failures, nameof(refreshToken.ReplacedByTokenId), \"unknown_reference\", \"The replacement Refresh Token does not exist.\");");
                        method.AddStatement("    Concurrency(failures, nameof(refreshToken.ConcurrencyToken), refreshToken.ConcurrencyToken, null);");
                        method.AddStatement("}");
                        method.AddStatement("if (accessToken is not null)");
                        method.AddStatement("{");
                        method.AddStatement("    Required(failures, nameof(accessToken.TokenId), accessToken.TokenId);");
                        method.AddStatement("    Required(failures, nameof(accessToken.SigningKeyId), accessToken.SigningKeyId);");
                        method.AddStatement("    AbsoluteUri(failures, nameof(accessToken.Issuer), accessToken.Issuer, 2048);");
                        method.AddStatement("    Required(failures, nameof(accessToken.Audience), accessToken.Audience);");
                        method.AddStatement("    Required(failures, nameof(accessToken.Subject), accessToken.Subject);");
                        method.AddStatement("    OneOf(failures, nameof(accessToken.PrincipalType), accessToken.PrincipalType, \"User\", \"Service\", \"Role\");");
                        method.AddStatement("    Values(failures, nameof(accessToken.Scopes), accessToken.Scopes);");
                        method.AddStatement("    UtcRange(failures, nameof(accessToken.IssuedAt), accessToken.IssuedAt, nameof(accessToken.ExpiresAt), accessToken.ExpiresAt);");
                        method.AddStatement("    Utc(failures, nameof(accessToken.NotBefore), accessToken.NotBefore);");
                        method.AddStatement("    Chronology(failures, nameof(accessToken.NotBefore), accessToken.IssuedAt, accessToken.NotBefore);");
                        method.AddStatement("    OptionalUtc(failures, nameof(accessToken.RevokedAt), accessToken.RevokedAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(accessToken.RevokedAt), accessToken.IssuedAt, accessToken.RevokedAt);");
                        method.AddStatement("    if (new[] { \"User\", \"Service\", \"Role\" }.Contains(accessToken.PrincipalType, StringComparer.Ordinal) && !await context.ExistsAsync(accessToken.PrincipalType, accessToken.Subject, cancellationToken)) Failure(failures, nameof(accessToken.Subject), \"unknown_reference\", \"The token Principal does not exist.\");");
                        method.AddStatement("}");
                        method.AddStatement("if (idToken is not null)");
                        method.AddStatement("{");
                        method.AddStatement("    Required(failures, nameof(idToken.TokenId), idToken.TokenId);");
                        method.AddStatement("    Required(failures, nameof(idToken.SigningKeyId), idToken.SigningKeyId);");
                        method.AddStatement("    AbsoluteUri(failures, nameof(idToken.Issuer), idToken.Issuer, 2048);");
                        method.AddStatement("    Required(failures, nameof(idToken.ClientAudience), idToken.ClientAudience);");
                        method.AddStatement("    Required(failures, nameof(idToken.UserSubject), idToken.UserSubject);");
                        method.AddStatement("    Required(failures, nameof(idToken.IssuanceStatus), idToken.IssuanceStatus);");
                        method.AddStatement("    UtcRange(failures, nameof(idToken.IssuedAt), idToken.IssuedAt, nameof(idToken.ExpiresAt), idToken.ExpiresAt);");
                        method.AddStatement("    if (!await context.ExistsAsync(\"User\", idToken.UserSubject, cancellationToken)) Failure(failures, nameof(idToken.UserSubject), \"unknown_reference\", \"The User does not exist.\");");
                        method.AddStatement("}");
                        method.AddStatement("if (ssoSession is not null)");
                        method.AddStatement("{");
                        method.AddStatement("    Required(failures, nameof(ssoSession.Id), ssoSession.Id);");
                        method.AddStatement("    Required(failures, nameof(ssoSession.OpaqueCookieIdentifier), ssoSession.OpaqueCookieIdentifier);");
                        method.AddStatement("    Required(failures, nameof(ssoSession.UserId), ssoSession.UserId);");
                        method.AddStatement("    UtcRange(failures, nameof(ssoSession.IssuedAt), ssoSession.IssuedAt, nameof(ssoSession.ExpiresAt), ssoSession.ExpiresAt);");
                        method.AddStatement("    OptionalUtc(failures, nameof(ssoSession.RevokedAt), ssoSession.RevokedAt);");
                        method.AddStatement("    OptionalChronology(failures, nameof(ssoSession.RevokedAt), ssoSession.IssuedAt, ssoSession.RevokedAt);");
                        method.AddStatement("    if (!await context.ExistsAsync(\"User\", ssoSession.UserId, cancellationToken)) Failure(failures, nameof(ssoSession.UserId), \"unknown_reference\", \"The User does not exist.\");");
                        method.AddStatement("    Concurrency(failures, nameof(ssoSession.ConcurrencyToken), ssoSession.ConcurrencyToken, null);");
                        method.AddStatement("}");
                        method.AddStatement("return Result(failures);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityMutationResult>", "ValidateAndAddAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddGenericParameter("TRecord");
                        method.AddParameter("TRecord", "record");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("Func<TRecord, ISecurityAuthorityRecordStore, CancellationToken, ValueTask<SecurityAuthorityValidationResult>>", "validate");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(record);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(validate);");
                        method.AddStatement("var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var validation = await validate(record, operation.Records, cancellationToken);");
                        method.AddStatement("    if (!validation.IsValid)");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        return new SecurityAuthorityMutationResult(validation, null);");
                        method.AddStatement("    }");
                        method.AddStatement("    await operation.Records.AddAsync(record!, cancellationToken);");
                        method.AddStatement("    var receipt = await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("    return new SecurityAuthorityMutationResult(validation, receipt);");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                        method.AddStatement("finally");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.DisposeAsync();");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityMutationResult>", "ValidateAndUpdateAsync", method =>
                    {
                        method.Static();
                        method.Async();
                        method.AddGenericParameter("TRecord");
                        method.AddParameter("TRecord", "record");
                        method.AddParameter("string", "expectedConcurrencyToken");
                        method.AddParameter("ISecurityAuthorityPersistence", "persistence");
                        method.AddParameter("Func<TRecord, ISecurityAuthorityRecordStore, CancellationToken, ValueTask<SecurityAuthorityValidationResult>>", "validate");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(expectedConcurrencyToken)) throw new ArgumentException(\"An expected concurrency token is required.\", nameof(expectedConcurrencyToken));");
                        method.AddStatement("var operation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var validation = await validate(record, operation.Records, cancellationToken);");
                        method.AddStatement("    if (!validation.IsValid)");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        return new SecurityAuthorityMutationResult(validation, null);");
                        method.AddStatement("    }");
                        method.AddStatement("    await operation.Records.UpdateAsync(record!, expectedConcurrencyToken, cancellationToken);");
                        method.AddStatement("    var receipt = await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("    return new SecurityAuthorityMutationResult(validation, receipt);");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                        method.AddStatement("finally");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.DisposeAsync();");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("void", "AddFailures", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("SecurityAuthorityValidationResult", "validation");
                        method.AddParameter("string", "resourceField");
                        method.AddParameter("string", "tenantField");
                        method.AddParameter("string", "resourceKindField");
                        method.AddStatement("foreach (var failure in validation.Failures)");
                        method.AddStatement("{");
                        method.AddStatement("    var field = failure.Field switch");
                        method.AddStatement("    {");
                        method.AddStatement("        \"TenantId\" => tenantField,");
                        method.AddStatement("        \"ResourceKind\" => resourceKindField,");
                        method.AddStatement("        \"ParentTenantResourceId\" => resourceField,");
                        method.AddStatement("        _ => resourceField");
                        method.AddStatement("    };");
                        method.AddStatement("    failures.Add(new SecurityAuthorityValidationFailure(field, failure.Code, failure.Message));");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("void", "MutableLifecycle", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "id");
                        method.AddParameter("DateTimeOffset", "createdAt");
                        method.AddParameter("DateTimeOffset", "updatedAt");
                        method.AddParameter("string", "concurrencyToken");
                        method.AddParameter("string?", "existingId");
                        method.AddParameter("DateTimeOffset?", "existingCreatedAt");
                        method.AddParameter("string?", "existingConcurrencyToken");
                        method.AddStatement("Utc(failures, nameof(createdAt), createdAt);");
                        method.AddStatement("Utc(failures, nameof(updatedAt), updatedAt);");
                        method.AddStatement("Chronology(failures, nameof(updatedAt), createdAt, updatedAt);");
                        method.AddStatement("Stable(failures, id, createdAt, existingId, existingCreatedAt);");
                        method.AddStatement("Concurrency(failures, nameof(concurrencyToken), concurrencyToken, existingConcurrencyToken);");
                    });
                    @class.AddMethod("void", "Stable", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "id");
                        method.AddParameter("DateTimeOffset", "createdAt");
                        method.AddParameter("string?", "existingId");
                        method.AddParameter("DateTimeOffset?", "existingCreatedAt");
                        method.AddStatement("if (existingId is not null && !string.Equals(id, existingId, StringComparison.Ordinal)) Failure(failures, \"Id\", \"immutable\", \"Identifiers must remain stable after creation.\");");
                        method.AddStatement("if (existingCreatedAt is not null && createdAt != existingCreatedAt.Value) Failure(failures, \"CreatedAt\", \"immutable\", \"Creation timestamps must remain stable after creation.\");");
                    });
                    @class.AddMethod("void", "Concurrency", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string", "value");
                        method.AddParameter("string?", "existingValue");
                        method.AddStatement("Required(failures, field, value);");
                        method.AddStatement("if (existingValue is not null && string.Equals(value, existingValue, StringComparison.Ordinal)) Failure(failures, field, \"concurrency_not_advanced\", \"A mutable record update must advance its concurrency token.\");");
                    });
                    @class.AddMethod("void", "TenantOwnership", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string?", "recordTenantId");
                        method.AddParameter("string?", "currentTenantId");
                        method.AddStatement("if (recordTenantId is not null && currentTenantId is not null && !string.Equals(recordTenantId, currentTenantId, StringComparison.Ordinal)) Failure(failures, field, \"tenant_mismatch\", \"The record belongs to another Tenant.\");");
                    });
                    @class.AddMethod("void", "Revocation", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("bool", "isRevoked");
                        method.AddParameter("DateTimeOffset?", "revokedAt");
                        method.AddStatement("if (isRevoked != revokedAt.HasValue) Failure(failures, \"RevokedAt\", \"invalid_lifecycle\", \"Revocation state and timestamp must change together.\");");
                    });
                    @class.AddMethod("void", "Uris", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("IReadOnlyList<string>", "values");
                        method.AddStatement("if (values is null) { Failure(failures, field, \"required\", \"The collection is required.\"); return; }");
                        method.AddStatement("var seen = new HashSet<string>(StringComparer.Ordinal);");
                        method.AddStatement("for (var index = 0; index < values.Count; index++)");
                        method.AddStatement("{");
                        method.AddStatement("    AbsoluteUri(failures, $\"{field}[{index}]\", values[index], 2048);");
                        method.AddStatement("    if (Uri.TryCreate(values[index], UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Fragment)) Failure(failures, $\"{field}[{index}]\", \"invalid_uri\", \"Redirect URI values cannot contain a fragment.\");");
                        method.AddStatement("    if (!seen.Add(values[index])) Failure(failures, $\"{field}[{index}]\", \"not_unique\", \"URI values must be unique using ordinal case-sensitive comparison.\");");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("void", "Values", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("IReadOnlyList<string>", "values");
                        method.AddStatement("if (values is null) { Failure(failures, field, \"required\", \"The collection is required.\"); return; }");
                        method.AddStatement("var seen = new HashSet<string>(StringComparer.Ordinal);");
                        method.AddStatement("for (var index = 0; index < values.Count; index++)");
                        method.AddStatement("{");
                        method.AddStatement("    Required(failures, $\"{field}[{index}]\", values[index]);");
                        method.AddStatement("    if (!seen.Add(values[index])) Failure(failures, $\"{field}[{index}]\", \"not_unique\", \"Values must be unique using ordinal case-sensitive comparison.\");");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("void", "RequiredValues", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("IReadOnlyList<string>", "values");
                        method.AddStatement("if (values is null || values.Count == 0) { Failure(failures, field, \"required\", \"At least one value is required.\"); return; }");
                        method.AddStatement("var seen = new HashSet<string>(StringComparer.Ordinal);");
                        method.AddStatement("for (var index = 0; index < values.Count; index++)");
                        method.AddStatement("{");
                        method.AddStatement("    Required(failures, $\"{field}[{index}]\", values[index]);");
                        method.AddStatement("    if (!seen.Add(values[index])) Failure(failures, $\"{field}[{index}]\", \"not_unique\", \"Values must be unique using ordinal case-sensitive comparison.\");");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("void", "SpaceDelimitedValues", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string", "value");
                        method.AddStatement("Required(failures, field, value);");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(value)) return;");
                        method.AddStatement("var values = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);");
                        method.AddStatement("if (!string.Equals(value, string.Join(\" \", values), StringComparison.Ordinal)) Failure(failures, field, \"invalid_format\", \"Scopes must be separated by single spaces.\");");
                        method.AddStatement("var seen = new HashSet<string>(StringComparer.Ordinal);");
                        method.AddStatement("for (var index = 0; index < values.Length; index++)");
                        method.AddStatement("{");
                        method.AddStatement("    if (!seen.Add(values[index])) Failure(failures, field, \"not_unique\", \"Scopes must be unique using ordinal case-sensitive comparison.\");");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("void", "AbsoluteUri", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string", "value");
                        method.AddParameter("int", "maximumLength");
                        method.AddStatement("Length(failures, field, value, 1, maximumLength);");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(value) && (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri)) Failure(failures, field, \"invalid_uri\", \"The value must be an absolute URI.\");");
                    });
                    @class.AddMethod("void", "OptionalAbsoluteUri", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string?", "value");
                        method.AddParameter("int", "maximumLength");
                        method.AddStatement("if (value is not null) AbsoluteUri(failures, field, value, maximumLength);");
                    });
                    @class.AddMethod("void", "Length", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string?", "value");
                        method.AddParameter("int", "minimum");
                        method.AddParameter("int", "maximum");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(value)) Failure(failures, field, \"required\", \"A non-empty value is required.\");");
                        method.AddStatement("else if (value.Length < minimum || value.Length > maximum) Failure(failures, field, \"length\", $\"Length must be from {minimum} through {maximum} characters.\");");
                    });
                    @class.AddMethod("void", "OptionalLength", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string?", "value");
                        method.AddParameter("int", "maximum");
                        method.AddStatement("if (value is not null && value.Length > maximum) Failure(failures, field, \"length\", $\"Length cannot exceed {maximum} characters.\");");
                    });
                    @class.AddMethod("void", "Required", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string?", "value");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(value)) Failure(failures, field, \"required\", \"A non-empty value is required.\");");
                    });
                    @class.AddMethod("void", "OneOf", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string", "value");
                        method.AddParameter("params string[]", "allowed");
                        method.AddStatement("if (!allowed.Contains(value, StringComparer.Ordinal)) Failure(failures, field, \"invalid_value\", \"The value is not one of the allowed case-sensitive values.\");");
                    });
                    @class.AddMethod("void", "UtcRange", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "startField");
                        method.AddParameter("DateTimeOffset", "start");
                        method.AddParameter("string", "endField");
                        method.AddParameter("DateTimeOffset", "end");
                        method.AddStatement("Utc(failures, startField, start);");
                        method.AddStatement("Utc(failures, endField, end);");
                        method.AddStatement("Chronology(failures, endField, start, end);");
                    });
                    @class.AddMethod("void", "Utc", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("DateTimeOffset", "value");
                        method.AddStatement("if (value.Offset != TimeSpan.Zero) Failure(failures, field, \"not_utc\", \"Timestamps must be UTC instants.\");");
                    });
                    @class.AddMethod("void", "OptionalUtc", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("DateTimeOffset?", "value");
                        method.AddStatement("if (value is not null) Utc(failures, field, value.Value);");
                    });
                    @class.AddMethod("void", "Chronology", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("DateTimeOffset", "start");
                        method.AddParameter("DateTimeOffset", "end");
                        method.AddStatement("if (end < start) Failure(failures, field, \"invalid_lifecycle\", \"The timestamp cannot precede the record start time.\");");
                    });
                    @class.AddMethod("void", "OptionalChronology", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("DateTimeOffset", "start");
                        method.AddParameter("DateTimeOffset?", "end");
                        method.AddStatement("if (end is not null) Chronology(failures, field, start, end.Value);");
                    });
                    @class.AddMethod("SecurityAuthorityValidationResult", "Result", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddStatement("return new SecurityAuthorityValidationResult(failures);");
                    });
                    @class.AddMethod("void", "Failure", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("List<SecurityAuthorityValidationFailure>", "failures");
                        method.AddParameter("string", "field");
                        method.AddParameter("string", "code");
                        method.AddParameter("string", "message");
                        method.AddStatement("failures.Add(new SecurityAuthorityValidationFailure(field, code, message));");
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
