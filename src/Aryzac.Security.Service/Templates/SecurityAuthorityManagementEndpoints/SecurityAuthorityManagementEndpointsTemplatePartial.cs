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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityManagementEndpoints
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityManagementEndpointsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityManagementEndpoints";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityManagementEndpointsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Security.Claims")
                .AddUsing("System.Security.Cryptography")
                .AddUsing("System.Text.Json")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityPostCommitDispatch")
                .AddUsing("Microsoft.AspNetCore.Builder")
                .AddUsing("Microsoft.AspNetCore.Http")
                .AddUsing("Microsoft.AspNetCore.Routing")
                .AddRecord("SecurityAuthorityApiKeyScope", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "TenantResourceId");
                        ctor.AddParameter("string", "ResourceKind");
                        ctor.AddParameter("string", "PermissionKey");
                        ctor.AddParameter("string", "Applicability");
                    });
                })
                .AddRecord("SecurityAuthorityIssuedApiKey", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityApiKey", "ApiKey");
                        ctor.AddParameter("string", "ClearApiKey");
                    });
                })
                .AddRecord("SecurityAuthorityApiKeyPrincipal", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "PrincipalType");
                        ctor.AddParameter("string", "PrincipalId");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("IReadOnlyList<string>", "Scopes");
                    });
                })
                .AddRecord("SecurityAuthorityManagementOperation", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Resource");
                        ctor.AddParameter("string", "Action");
                        ctor.AddParameter("string?", "Id");
                        ctor.AddParameter("JsonElement?", "Body");
                        ctor.AddParameter("int", "PageNumber");
                        ctor.AddParameter("int", "PageSize");
                    });
                })
                .AddRecord("SecurityAuthorityManagementOperationResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("int", "StatusCode");
                        ctor.AddParameter("object?", "Value");
                        ctor.AddParameter("IReadOnlyDictionary<string, string[]>?", "Errors");
                    });
                })
                .AddRecord("SecurityAuthorityManagementScopeMetadata", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor => ctor.AddParameter("string", "Scope"));
                })
                .AddInterface("ISecurityAuthorityManagementOperations", @interface =>
                {
                    @interface.AddMethod("ValueTask<SecurityAuthorityManagementOperationResult>", "ExecuteAsync", method =>
                    {
                        method.AddParameter("SecurityAuthorityManagementOperation", "operation");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddClass("SecurityAuthorityManagementEndpoints", @class =>
                {
                    @class.Sealed();
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("ISecurityAuthorityPersistence", "persistence", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityAuthorizationDataSource", "dataSource", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityManagementOperations", "managementOperations", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("SecurityAuthorityAuthorizationEngine", "authorizationEngine", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("SecurityAuthorityCredentialHasher", "credentialHasher", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("SecurityAuthorityPostCommitDispatch", "postCommitDispatch", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("string", "configuredPrefix", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<DateTimeOffset>", "utcNow", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<string>", "newId", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<string>", "newConcurrencyToken", param => param.IntroduceReadonlyField());
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(postCommitDispatch);");
                        ctor.AddStatement("if (string.IsNullOrWhiteSpace(configuredPrefix) || configuredPrefix.Contains('.') || configuredPrefix.Any(char.IsWhiteSpace)) throw new ArgumentException(\"The API Key prefix must be non-empty, whitespace-free, and cannot contain periods.\", nameof(configuredPrefix));");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityIssuedApiKey>", "CreateApiKeyAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("string", "name");
                        method.AddParameter("string?", "userId");
                        method.AddParameter("string?", "serviceId");
                        method.AddParameter("string?", "tenantId");
                        method.AddParameter("DateTimeOffset?", "expiresAt");
                        method.AddParameter("IReadOnlyList<SecurityAuthorityApiKeyScope>", "scopes");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentException.ThrowIfNullOrWhiteSpace(name);
                            ArgumentNullException.ThrowIfNull(scopes);
                            if ((userId is null) == (serviceId is null))
                            {
                            throw new ArgumentException("An API Key must have exactly one User or Service owner.");
                            }

                            var now = _utcNow();
                            if (expiresAt is not null && expiresAt <= now)
                            {
                            throw new ArgumentOutOfRangeException(nameof(expiresAt), "An API Key expiry must be in the future.");
                            }

                            var ownerType = userId is not null ? "User" : "Service";
                            var ownerId = userId ?? serviceId!;
                            var ownerTenantId = await GetActiveOwnerTenantIdAsync(ownerType, ownerId, cancellationToken);
                            if (tenantId is not null && ownerTenantId is not null && !string.Equals(tenantId, ownerTenantId, StringComparison.Ordinal))
                            {
                            throw new InvalidOperationException("The API Key and its owner must belong to the same Tenant.");
                            }

                            ValidateScopes(scopes, tenantId ?? ownerTenantId);
                            await using var operation = await _persistence.BeginAtomicOperationAsync(
                            SecurityAuthorityAtomicOperationKind.ApiKeyRegeneration,
                            true,
                            cancellationToken);
                            try
                            {
                            var apiKeyId = _newId();
                            var clearApiKey = CreateClearApiKey(apiKeyId);
                            var apiKey = new SecurityAuthorityApiKey(
                            apiKeyId,
                            name,
                            ownerType,
                            ownerId,
                            _configuredPrefix + "." + apiKeyId,
                            _credentialHasher.HashApiKey(clearApiKey),
                            tenantId ?? ownerTenantId,
                            expiresAt,
                            false,
                            null,
                            null,
                            now,
                            now,
                            _newConcurrencyToken());
                            await operation.Records.AddAsync(apiKey, cancellationToken);
                            foreach (var scope in scopes.Distinct())
                            {
                            await operation.Records.AddAsync(ToGrant(apiKey, scope, now), cancellationToken);
                            }

                            var deferred = new SecurityAuthorityDeferredCredential(operation.OperationId, () => clearApiKey);
                            var receipt = await operation.CommitAsync(cancellationToken);
                            _authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.ApiKey, apiKey.TenantId ?? "__global__", apiKey.Id);
                            await _postCommitDispatch.DispatchAsync(receipt, new SecurityAuthorityPrincipalReference(ownerType, ownerId), SecurityAuthorityLifecycleTransition.Created, "api-key", apiKey.Id, apiKey.TenantId, receipt.OperationId.ToString("N"), "succeeded", new[] { "Name", "OwnerPrincipalType", "OwnerId", "TenantId", "ExpiresAt" }, cancellationToken);
                            return new SecurityAuthorityIssuedApiKey(apiKey, deferred.Reveal(receipt));
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityIssuedApiKey>", "RegenerateApiKeyAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("string", "apiKeyId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyId);
                            await using var operation = await _persistence.BeginAtomicOperationAsync(
                            SecurityAuthorityAtomicOperationKind.ApiKeyRegeneration,
                            true,
                            cancellationToken);
                            try
                            {
                            var apiKey = await operation.Records.LoadAsync(typeof(SecurityAuthorityApiKey), apiKeyId, cancellationToken) as SecurityAuthorityApiKey
                            ?? throw new InvalidOperationException("The API Key does not exist.");
                            await GetActiveOwnerTenantIdAsync(apiKey.OwnerPrincipalType, apiKey.OwnerId, cancellationToken);
                            var now = _utcNow();
                            var clearApiKey = CreateClearApiKey(apiKey.Id);
                            var revokedPrevious = apiKey with
                            {
                            IsRevoked = true,
                            RevokedAt = now,
                            UpdatedAt = now,
                            ConcurrencyToken = _newConcurrencyToken()
                            };
                            var replacement = revokedPrevious with
                            {
                            KeyHash = _credentialHasher.HashApiKey(clearApiKey),
                            IsRevoked = false,
                            RevokedAt = null,
                            LastUsedAt = null,
                            ConcurrencyToken = _newConcurrencyToken()
                            };
                            await operation.Records.UpdateAsync(replacement, apiKey.ConcurrencyToken, cancellationToken);
                            var deferred = new SecurityAuthorityDeferredCredential(operation.OperationId, () => clearApiKey);
                            var receipt = await operation.CommitAsync(cancellationToken);
                            _authorizationInvalidator.Invalidate(SecurityAuthorityAuthorizationChange.ApiKey, replacement.TenantId ?? "__global__", replacement.Id);
                            await _postCommitDispatch.DispatchAsync(receipt, new SecurityAuthorityPrincipalReference(replacement.OwnerPrincipalType, replacement.OwnerId), SecurityAuthorityLifecycleTransition.Regenerated, "api-key", replacement.Id, replacement.TenantId, receipt.OperationId.ToString("N"), "succeeded", new[] { "KeyHash", "IsRevoked", "RevokedAt", "LastUsedAt" }, cancellationToken);
                            return new SecurityAuthorityIssuedApiKey(replacement, deferred.Reveal(receipt));
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityApiKeyPrincipal?>", "AuthenticateApiKeyAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("string", "clearApiKey");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            if (!TryReadApiKeyId(clearApiKey, out var apiKeyId))
                            {
                            return null;
                            }

                            var apiKey = await _dataSource.GetApiKeyAsync(apiKeyId, cancellationToken);
                            if (apiKey is null ||
                            !string.Equals(apiKey.PublicPrefix, _configuredPrefix + "." + apiKey.Id, StringComparison.Ordinal) ||
                            !_credentialHasher.VerifyApiKey(clearApiKey, apiKey.KeyHash))
                            {
                            return null;
                            }

                            var now = _utcNow();
                            var user = string.Equals(apiKey.OwnerPrincipalType, "User", StringComparison.Ordinal)
                            ? await _dataSource.GetUserAsync(apiKey.OwnerId, cancellationToken)
                            : null;
                            var service = string.Equals(apiKey.OwnerPrincipalType, "Service", StringComparison.Ordinal)
                            ? await _dataSource.GetServiceAsync(apiKey.OwnerId, cancellationToken)
                            : null;
                            if (!SecurityAuthorityLifecycle.CanUseApiKey(apiKey, user, service, now))
                            {
                            return null;
                            }

                            var scopes = await GetPrincipalScopesAsync(apiKey, cancellationToken);
                            await using var operation = await _persistence.BeginAtomicOperationAsync(
                            SecurityAuthorityAtomicOperationKind.ApiKeyRegeneration,
                            true,
                            cancellationToken);
                            try
                            {
                            var current = await operation.Records.LoadAsync(typeof(SecurityAuthorityApiKey), apiKey.Id, cancellationToken) as SecurityAuthorityApiKey;
                            if (current is null || !_credentialHasher.VerifyApiKey(clearApiKey, current.KeyHash))
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return null;
                            }

                            var currentUser = string.Equals(current.OwnerPrincipalType, "User", StringComparison.Ordinal)
                            ? await operation.Records.LoadAsync(typeof(SecurityAuthorityUser), current.OwnerId, cancellationToken) as SecurityAuthorityUser
                            : null;
                            var currentService = string.Equals(current.OwnerPrincipalType, "Service", StringComparison.Ordinal)
                            ? await operation.Records.LoadAsync(typeof(SecurityAuthorityService), current.OwnerId, cancellationToken) as SecurityAuthorityService
                            : null;
                            if (!SecurityAuthorityLifecycle.CanUseApiKey(current, currentUser, currentService, now))
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return null;
                            }

                            var updated = current with
                            {
                            LastUsedAt = now,
                            UpdatedAt = now,
                            ConcurrencyToken = _newConcurrencyToken()
                            };
                            await operation.Records.UpdateAsync(updated, current.ConcurrencyToken, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            return new SecurityAuthorityApiKeyPrincipal(current.OwnerPrincipalType, current.OwnerId, current.TenantId, scopes);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("void", "Map", method =>
                    {
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(endpoints);
                            const string root = "/api/v1/security";
                            MapResource(endpoints, root, "users", new[] { "activate", "suspend" });
                            MapResource(endpoints, root, "services", new[] { "activate", "suspend" });
                            MapCollectionAction(endpoints, root, "services", "provision");
                            MapResource(endpoints, root, "api-keys", new[] { "revoke", "regenerate" });
                            MapResource(endpoints, root, "oauth-clients", new[] { "activate", "suspend", "regenerate-secret" });
                            MapResource(endpoints, root, "identity-providers", new[] { "enable", "disable" });
                            MapResource(endpoints, root, "tenant-resources", new[] { "provision" });
                            MapResource(endpoints, root, "roles", new[] { "enable", "disable" });
                            MapResource(endpoints, root, "role-memberships", new[] { "revoke" });
                            MapResource(endpoints, root, "grants", new[] { "revoke" });
                            MapReadOnlyCollection(endpoints, root, "grant-catalog");
                            MapSingleton(endpoints, root, "summary", "read");
                            MapSingleton(endpoints, root, "bootstrap", "read");
                            MapCollectionAction(endpoints, root, "bootstrap", "reset");
                            """);
                    });
                    @class.AddMethod("void", "MapResource", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("string", "root");
                        method.AddParameter("string", "resource");
                        method.AddParameter("IReadOnlyList<string>", "actions");
                        method.AddStatement("""
                            var path = root + "/" + resource;
                            endpoints.MapGet(path, (HttpContext context, SecurityAuthorityManagementEndpoints management, int? pageNumber, int? pageSize, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, "list", null, null, pageNumber ?? 1, pageSize ?? 25, false, cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, "list")));
                            endpoints.MapGet(path + "/{id}", (HttpContext context, SecurityAuthorityManagementEndpoints management, string id, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, "read", id, null, 1, 25, false, cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, "read")));
                            endpoints.MapPost(path, (HttpContext context, SecurityAuthorityManagementEndpoints management, JsonElement body, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, "create", null, body, 1, 25, AllowsOneTimeCredential(resource, "create"), cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, "create")));
                            endpoints.MapPut(path + "/{id}", (HttpContext context, SecurityAuthorityManagementEndpoints management, string id, JsonElement body, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, "update", id, body, 1, 25, false, cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, "update")));
                            endpoints.MapDelete(path + "/{id}", (HttpContext context, SecurityAuthorityManagementEndpoints management, string id, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, "delete", id, null, 1, 25, false, cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, "delete")));
                            foreach (var action in actions)
                            {
                            endpoints.MapPost(path + "/{id}/" + action, (HttpContext context, SecurityAuthorityManagementEndpoints management, string id, JsonElement? body, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, action, id, body, 1, 25, AllowsOneTimeCredential(resource, action), cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, action)));
                            }
                            """);
                    });
                    @class.AddMethod("void", "MapReadOnlyCollection", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("string", "root");
                        method.AddParameter("string", "resource");
                        method.AddStatement("""
                            endpoints.MapGet(root + "/" + resource, (HttpContext context, SecurityAuthorityManagementEndpoints management, int? pageNumber, int? pageSize, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, "list", null, null, pageNumber ?? 1, pageSize ?? 25, false, cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, "list")));
                            """);
                    });
                    @class.AddMethod("void", "MapSingleton", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("string", "root");
                        method.AddParameter("string", "resource");
                        method.AddParameter("string", "action");
                        method.AddStatement("""
                            endpoints.MapGet(root + "/" + resource, (HttpContext context, SecurityAuthorityManagementEndpoints management, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, action, null, null, 1, 25, false, cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, action)));
                            """);
                    });
                    @class.AddMethod("void", "MapCollectionAction", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("string", "root");
                        method.AddParameter("string", "resource");
                        method.AddParameter("string", "action");
                        method.AddStatement("""
                            endpoints.MapPost(root + "/" + resource + "/" + action, (HttpContext context, SecurityAuthorityManagementEndpoints management, JsonElement? body, CancellationToken cancellationToken) =>
                            management.ExecuteAsync(context, resource, action, null, body, 1, 25, AllowsOneTimeCredential(resource, action), cancellationToken))
                            .WithMetadata(new SecurityAuthorityManagementScopeMetadata(Scope(resource, action)));
                            """);
                    });
                    @class.AddMethod("ValueTask<IResult>", "ExecuteAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("HttpContext", "context");
                        method.AddParameter("string", "resource");
                        method.AddParameter("string", "action");
                        method.AddParameter("string?", "id");
                        method.AddParameter("JsonElement?", "body");
                        method.AddParameter("int", "pageNumber");
                        method.AddParameter("int", "pageSize");
                        method.AddParameter("bool", "allowOneTimeCredential");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            var requiredScope = Scope(resource, action);
                            var authorizationFailure = Authorize(context.User, requiredScope);
                            if (authorizationFailure is not null) return authorizationFailure;
                            if (string.Equals(action, "list", StringComparison.Ordinal) && (pageNumber < 1 || pageSize < 1 || pageSize > 100))
                            {
                            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
                            if (pageNumber < 1) errors["pageNumber"] = new[] { "pageNumber must be at least 1." };
                            if (pageSize < 1 || pageSize > 100) errors["pageSize"] = new[] { "pageSize must be between 1 and 100." };
                            return ValidationProblem(errors);
                            }

                            SecurityAuthorityManagementOperationResult result;
                            try
                            {
                            var idempotencyKey = ReadOptionalHeader(context, "Idempotency-Key");
                            if (idempotencyKey is not null && (idempotencyKey.Length < 1 || idempotencyKey.Length > 200))
                            {
                            return ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal) { ["idempotencyKey"] = new[] { "Idempotency-Key must contain between 1 and 200 characters." } });
                            }
                            if (idempotencyKey is not null && !SupportsIdempotency(action))
                            {
                            return ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal) { ["idempotencyKey"] = new[] { "Idempotency-Key is not supported for this operation." } });
                            }

                            var concurrencyToken = RequiresConcurrency(action) ? ReadConcurrencyToken(context, body) : null;
                            if (RequiresConcurrency(action) && string.IsNullOrWhiteSpace(concurrencyToken))
                            {
                            return ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal) { ["concurrencyToken"] = new[] { "A concurrency token is required in If-Match or the request body." } });
                            }
                            var effectiveBody = WithConcurrencyToken(body, concurrencyToken);
                            var operation = new SecurityAuthorityManagementOperation(resource, action, id, effectiveBody, pageNumber, pageSize);
                            var requestHash = idempotencyKey is null ? null : Fingerprint(operation, concurrencyToken);
                            if (idempotencyKey is not null)
                            {
                            var replay = await TryReplayAsync(idempotencyKey, OperationName(operation), requestHash!, cancellationToken);
                            if (replay is not null) result = replay;
                            else
                            {
                            await EnsureCurrentConcurrencyAsync(resource, action, id, concurrencyToken, cancellationToken);
                            result = await ExecuteCoreAsync(operation, cancellationToken);
                            result = NormalizeListResult(operation, result);
                            await StoreOutcomeAsync(idempotencyKey, OperationName(operation), requestHash!, result, cancellationToken);
                            }
                            }
                            else
                            {
                            await EnsureCurrentConcurrencyAsync(resource, action, id, concurrencyToken, cancellationToken);
                            result = await ExecuteCoreAsync(operation, cancellationToken);
                            result = NormalizeListResult(operation, result);
                            }
                            }
                            catch (ArgumentException exception)
                            {
                            return ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
                            {
                            [exception.ParamName ?? "request"] = new[] { exception.Message }
                            });
                            }
                            catch (KeyNotFoundException exception)
                            {
                            return Problem(StatusCodes.Status404NotFound, "Not Found", exception.Message);
                            }
                            catch (UnauthorizedAccessException exception)
                            {
                            return Problem(StatusCodes.Status403Forbidden, "Forbidden", exception.Message);
                            }
                            catch (InvalidOperationException exception)
                            {
                            return Problem(StatusCodes.Status409Conflict, "Conflict", exception.Message);
                            }

                            if (result.Errors is { Count: > 0 })
                            {
                            return result.StatusCode == StatusCodes.Status400BadRequest
                            ? ValidationProblem(result.Errors)
                            : Problem(result.StatusCode, Title(result.StatusCode), "The management operation failed.", result.Errors);
                            }
                            if (result.StatusCode is not (StatusCodes.Status200OK or StatusCodes.Status201Created or StatusCodes.Status204NoContent or StatusCodes.Status400BadRequest or StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden or StatusCodes.Status404NotFound or StatusCodes.Status409Conflict))
                            {
                            throw new InvalidOperationException("A management operation returned an unsupported HTTP status code.");
                            }
                            if (result.StatusCode < StatusCodes.Status400BadRequest)
                            {
                            var expectedStatus = string.Equals(action, "create", StringComparison.Ordinal)
                            ? StatusCodes.Status201Created
                            : string.Equals(action, "delete", StringComparison.Ordinal)
                            ? StatusCodes.Status204NoContent
                            : StatusCodes.Status200OK;
                            if (result.StatusCode != expectedStatus) throw new InvalidOperationException("A management operation returned an incorrect success status code for its action.");
                            }
                            if (result.StatusCode == StatusCodes.Status204NoContent) return Results.NoContent();
                            if (result.StatusCode >= 400) return Problem(result.StatusCode, Title(result.StatusCode), "The management operation failed.");
                            return Results.Json(Sanitize(result.Value, allowOneTimeCredential), statusCode: result.StatusCode);
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityManagementOperationResult>", "ExecuteCoreAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("SecurityAuthorityManagementOperation", "operation");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            if (string.Equals(operation.Resource, "grant-catalog", StringComparison.Ordinal) && string.Equals(operation.Action, "list", StringComparison.Ordinal))
                            {
                            var catalog = await _dataSource.GetGrantCatalogAsync(cancellationToken)
                            ?? throw new InvalidOperationException("The Grant Catalog data source returned null.");
                            var ordered = catalog.OrderBy(x => x.PermissionKey, StringComparer.Ordinal).ToArray();
                            return new SecurityAuthorityManagementOperationResult(StatusCodes.Status200OK, Page(ordered, operation.PageNumber, operation.PageSize), null);
                            }
                            if (string.Equals(operation.Resource, "api-keys", StringComparison.Ordinal) && string.Equals(operation.Action, "create", StringComparison.Ordinal))
                            {
                            return await CreateApiKeyManagementResultAsync(operation.Body, cancellationToken);
                            }
                            if (string.Equals(operation.Resource, "api-keys", StringComparison.Ordinal) && string.Equals(operation.Action, "regenerate", StringComparison.Ordinal))
                            {
                            var issued = await RegenerateApiKeyAsync(operation.Id ?? throw new ArgumentException("An API Key id is required.", "id"), cancellationToken);
                            return new SecurityAuthorityManagementOperationResult(StatusCodes.Status200OK, issued, null);
                            }
                            return await _managementOperations.ExecuteAsync(operation, cancellationToken);
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityManagementOperationResult>", "CreateApiKeyManagementResultAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("JsonElement?", "body");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            if (body is null || body.Value.ValueKind != JsonValueKind.Object)
                            {
                            return new SecurityAuthorityManagementOperationResult(StatusCodes.Status400BadRequest, null, new Dictionary<string, string[]>(StringComparer.Ordinal) { ["body"] = new[] { "A JSON object is required." } });
                            }
                            var value = body.Value;
                            var name = RequiredString(value, "name");
                            var userId = OptionalString(value, "userId");
                            var serviceId = OptionalString(value, "serviceId");
                            var tenantId = OptionalString(value, "tenantId");
                            DateTimeOffset? expiresAt = null;
                            if (value.TryGetProperty("expiresAt", out var expiresElement) && expiresElement.ValueKind != JsonValueKind.Null)
                            {
                            if (expiresElement.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(expiresElement.GetString(), out var parsed)) throw new ArgumentException("expiresAt must be an RFC 3339 timestamp.", "expiresAt");
                            expiresAt = parsed;
                            }
                            var scopes = new List<SecurityAuthorityApiKeyScope>();
                            if (!value.TryGetProperty("scopes", out var scopesElement) || scopesElement.ValueKind != JsonValueKind.Array) throw new ArgumentException("scopes must be an array.", "scopes");
                            foreach (var scope in scopesElement.EnumerateArray())
                            {
                            if (scope.ValueKind != JsonValueKind.Object) throw new ArgumentException("Every scope must be an object.", "scopes");
                            scopes.Add(new SecurityAuthorityApiKeyScope(RequiredString(scope, "tenantResourceId"), RequiredString(scope, "resourceKind"), RequiredString(scope, "permissionKey"), RequiredString(scope, "applicability")));
                            }
                            var issued = await CreateApiKeyAsync(name, userId, serviceId, tenantId, expiresAt, scopes, cancellationToken);
                            return new SecurityAuthorityManagementOperationResult(StatusCodes.Status201Created, issued, null);
                            """);
                    });
                    @class.AddMethod("string?", "ReadOptionalHeader", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("HttpContext", "context");
                        method.AddParameter("string", "headerName");
                        method.AddStatement("if (!context.Request.Headers.TryGetValue(headerName, out var values)) return null;");
                        method.AddStatement("if (values.Count != 1) throw new ArgumentException(headerName + \" must be supplied exactly once.\", headerName);");
                        method.AddStatement("return values[0] ?? string.Empty;");
                    });
                    @class.AddMethod("string?", "ReadConcurrencyToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("HttpContext", "context");
                        method.AddParameter("JsonElement?", "body");
                        method.AddStatement("""
                            var header = ReadOptionalHeader(context, "If-Match");
                            if (header is not null)
                            {
                            var token = header.Trim();
                            if (token.StartsWith("W/", StringComparison.Ordinal)) token = token[2..].Trim();
                            if (token.Length >= 2 && token[0] == '"' && token[^1] == '"') token = token[1..^1];
                            return token;
                            }
                            if (body is { ValueKind: JsonValueKind.Object } && body.Value.TryGetProperty("concurrencyToken", out var property) && property.ValueKind == JsonValueKind.String)
                            {
                            return property.GetString();
                            }
                            return null;
                            """);
                    });
                    @class.AddMethod("JsonElement?", "WithConcurrencyToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("JsonElement?", "body");
                        method.AddParameter("string?", "concurrencyToken");
                        method.AddStatement("""
                            if (string.IsNullOrWhiteSpace(concurrencyToken)) return body;
                            if (body is { ValueKind: JsonValueKind.Object } && body.Value.TryGetProperty("concurrencyToken", out _)) return body;
                            var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                            if (body is { ValueKind: JsonValueKind.Object })
                            {
                            foreach (var property in body.Value.EnumerateObject()) properties[property.Name] = property.Value.Clone();
                            }
                            properties["concurrencyToken"] = JsonSerializer.SerializeToElement(concurrencyToken);
                            return JsonSerializer.SerializeToElement(properties);
                            """);
                    });
                    @class.AddMethod("bool", "SupportsIdempotency", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "action");
                        method.AddStatement("return action is \"create\" or \"update\" or \"activate\" or \"suspend\" or \"enable\" or \"disable\" or \"revoke\" or \"regenerate\" or \"regenerate-secret\" or \"provision\" or \"reset\";");
                    });
                    @class.AddMethod("bool", "RequiresConcurrency", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "action");
                        method.AddStatement("return action is \"update\" or \"activate\" or \"suspend\" or \"enable\" or \"disable\" or \"revoke\" or \"regenerate\" or \"regenerate-secret\" or \"reset\";");
                    });
                    @class.AddMethod("ValueTask", "EnsureCurrentConcurrencyAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("string", "resource");
                        method.AddParameter("string", "action");
                        method.AddParameter("string?", "id");
                        method.AddParameter("string?", "expectedConcurrencyToken");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            if (!RequiresConcurrency(action)) return;
                            var recordType = ConcurrencyRecordType(resource) ?? throw new InvalidOperationException("The management resource does not define a concurrency record.");
                            var recordId = string.Equals(resource, "bootstrap", StringComparison.Ordinal)
                            ? "security-authority-bootstrap"
                            : id ?? throw new ArgumentException("A resource id is required for this concurrency-protected operation.", "id");
                            await using var operation = await _persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            object? current;
                            try
                            {
                            current = await operation.Records.LoadAsync(recordType, recordId, cancellationToken);
                            await operation.RollbackAsync(cancellationToken);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            if (current is null) throw new KeyNotFoundException($"The {resource} record '{recordId}' was not found.");
                            var currentToken = CurrentConcurrencyToken(current);
                            if (!string.Equals(currentToken, expectedConcurrencyToken, StringComparison.Ordinal))
                            {
                            throw new InvalidOperationException("The supplied concurrency token is stale.");
                            }
                            """);
                    });
                    @class.AddMethod("Type?", "ConcurrencyRecordType", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "resource");
                        method.AddStatement("""
                            return resource switch
                            {
                            "users" => typeof(SecurityAuthorityUser),
                            "services" => typeof(SecurityAuthorityService),
                            "api-keys" => typeof(SecurityAuthorityApiKey),
                            "oauth-clients" => typeof(SecurityAuthorityOAuthClient),
                            "identity-providers" => typeof(SecurityAuthorityIdentityProvider),
                            "tenant-resources" => typeof(SecurityAuthorityTenantResourceRecord),
                            "roles" => typeof(SecurityAuthorityRole),
                            "role-memberships" => typeof(SecurityAuthorityRoleMembership),
                            "grants" => typeof(SecurityAuthorityGrant),
                            "bootstrap" => typeof(SecurityAuthorityBootstrapState),
                            _ => null
                            };
                            """);
                    });
                    @class.AddMethod("string", "CurrentConcurrencyToken", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("object", "record");
                        method.AddStatement("""
                            return record switch
                            {
                            SecurityAuthorityUser value => value.ConcurrencyToken,
                            SecurityAuthorityService value => value.ConcurrencyToken,
                            SecurityAuthorityApiKey value => value.ConcurrencyToken,
                            SecurityAuthorityOAuthClient value => value.ConcurrencyToken,
                            SecurityAuthorityIdentityProvider value => value.ConcurrencyToken,
                            SecurityAuthorityTenantResourceRecord value => value.ConcurrencyToken,
                            SecurityAuthorityRole value => value.ConcurrencyToken,
                            SecurityAuthorityRoleMembership value => value.ConcurrencyToken,
                            SecurityAuthorityGrant value => value.ConcurrencyToken,
                            SecurityAuthorityBootstrapState value => value.ConcurrencyToken,
                            _ => throw new InvalidOperationException("The loaded management record does not expose a concurrency token.")
                            };
                            """);
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityManagementOperationResult?>", "TryReplayAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("string", "idempotencyKey");
                        method.AddParameter("string", "operationName");
                        method.AddParameter("string", "requestHash");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            await using var operation = await _persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            SecurityAuthorityIdempotencyOutcome? stored;
                            try
                            {
                            stored = await operation.Records.LoadAsync(typeof(SecurityAuthorityIdempotencyOutcome), idempotencyKey, cancellationToken) as SecurityAuthorityIdempotencyOutcome;
                            await operation.RollbackAsync(cancellationToken);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            if (stored is null || stored.ExpiresAt <= _utcNow()) return null;
                            if (!string.Equals(stored.OperationName, operationName, StringComparison.Ordinal) || !string.Equals(stored.RequestHash, requestHash, StringComparison.Ordinal))
                            {
                            throw new InvalidOperationException("The idempotency key has already been used with a different request.");
                            }
                            return JsonSerializer.Deserialize<SecurityAuthorityManagementOperationResult>(stored.OutcomeReference)
                            ?? throw new InvalidOperationException("The stored idempotency outcome is invalid.");
                            """);
                    });
                    @class.AddMethod("ValueTask", "StoreOutcomeAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("string", "idempotencyKey");
                        method.AddParameter("string", "operationName");
                        method.AddParameter("string", "requestHash");
                        method.AddParameter("SecurityAuthorityManagementOperationResult", "result");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            var now = _utcNow();
                            var stored = new SecurityAuthorityIdempotencyOutcome(idempotencyKey, operationName, requestHash, JsonSerializer.Serialize(result), now, now.AddHours(24));
                            await using var operation = await _persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken);
                            try
                            {
                            await operation.Records.AddAsync(stored, cancellationToken);
                            await operation.CommitAsync(cancellationToken);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("string", "OperationName", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityManagementOperation", "operation");
                        method.AddStatement("return operation.Resource + \"/\" + operation.Action + (operation.Id is null ? string.Empty : \"/\" + operation.Id);");
                    });
                    @class.AddMethod("string", "Fingerprint", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityManagementOperation", "operation");
                        method.AddParameter("string?", "concurrencyToken");
                        method.AddStatement("var canonical = OperationName(operation) + \"\\n\" + (concurrencyToken ?? string.Empty) + \"\\n\" + (operation.Body is null ? \"null\" : CanonicalJson(operation.Body.Value));");
                        method.AddStatement("return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(canonical)));");
                    });
                    @class.AddMethod("string", "CanonicalJson", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("JsonElement", "element");
                        method.AddStatement("""
                            if (element.ValueKind == JsonValueKind.Object)
                            {
                            return "{" + string.Join(",", element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => JsonSerializer.Serialize(x.Name) + ":" + CanonicalJson(x.Value))) + "}";
                            }
                            if (element.ValueKind == JsonValueKind.Array) return "[" + string.Join(",", element.EnumerateArray().Select(CanonicalJson)) + "]";
                            return element.GetRawText();
                            """);
                    });
                    @class.AddMethod("SecurityAuthorityManagementOperationResult", "NormalizeListResult", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityManagementOperation", "operation");
                        method.AddParameter("SecurityAuthorityManagementOperationResult", "result");
                        method.AddStatement("""
                            if (!string.Equals(operation.Action, "list", StringComparison.Ordinal) || result.StatusCode != StatusCodes.Status200OK || result.Value is null) return result;
                            var value = JsonSerializer.SerializeToElement(result.Value);
                            if (value.ValueKind == JsonValueKind.Array)
                            {
                            var allItems = value.EnumerateArray().Select(x => x.Clone()).OrderBy(DeterministicOrderKey, StringComparer.Ordinal).ToArray();
                            return result with { Value = PageElements(allItems, allItems.Length, operation.PageNumber, operation.PageSize) };
                            }
                            if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                            {
                            var ordered = items.EnumerateArray().Select(x => x.Clone()).OrderBy(DeterministicOrderKey, StringComparer.Ordinal).ToArray();
                            var totalCount = value.TryGetProperty("totalCount", out var count) && count.TryGetInt32(out var parsedCount) ? parsedCount : ordered.Length;
                            return result with { Value = PageElements(ordered, totalCount, operation.PageNumber, operation.PageSize, alreadyPaged: true) };
                            }
                            throw new InvalidOperationException("A list management operation must return an array or a page with an items array.");
                            """);
                    });
                    @class.AddMethod("object", "PageElements", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IReadOnlyList<JsonElement>", "items");
                        method.AddParameter("int", "totalCount");
                        method.AddParameter("int", "pageNumber");
                        method.AddParameter("int", "pageSize");
                        method.AddParameter("bool", "alreadyPaged", parameter => parameter.WithDefaultValue("false"));
                        method.AddStatement("var pageItems = alreadyPaged ? items.ToArray() : items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray();");
                        method.AddStatement("return new { totalCount, pageNumber, pageSize, items = pageItems };");
                    });
                    @class.AddMethod("string", "DeterministicOrderKey", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("JsonElement", "item");
                        method.AddStatement("""
                            if (item.ValueKind != JsonValueKind.Object) return CanonicalJson(item);
                            var names = new[] { "id", "tenantResourceId", "clientIdentifier", "providerIdentifier", "roleKey", "permissionKey", "name", "displayName" };
                            var keys = names.Select(name => item.TryGetProperty(name, out var value) ? value.ToString() : string.Empty);
                            return string.Join("\u001f", keys) + "\u001f" + CanonicalJson(item);
                            """);
                    });
                    @class.AddMethod("IResult?", "Authorize", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("ClaimsPrincipal", "principal");
                        method.AddParameter("string", "requiredScope");
                        method.AddStatement("if (principal.Identity?.IsAuthenticated != true) return Problem(StatusCodes.Status401Unauthorized, \"Unauthorized\", \"Authentication is required.\");");
                        method.AddStatement("var scopes = principal.Claims.Where(x => string.Equals(x.Type, \"scope\", StringComparison.Ordinal) || string.Equals(x.Type, \"scp\", StringComparison.Ordinal)).SelectMany(x => x.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));");
                        method.AddStatement("return scopes.Contains(requiredScope, StringComparer.Ordinal) ? null : Problem(StatusCodes.Status403Forbidden, \"Forbidden\", \"The authenticated principal is missing the required management Scope.\");");
                    });
                    @class.AddMethod("IResult", "ValidationProblem", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IReadOnlyDictionary<string, string[]>", "errors");
                        method.AddStatement("return Problem(StatusCodes.Status400BadRequest, \"Bad Request\", \"One or more validation errors occurred.\", errors);");
                    });
                    @class.AddMethod("IResult", "Problem", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("int", "statusCode");
                        method.AddParameter("string", "title");
                        method.AddParameter("string", "detail");
                        method.AddParameter("IReadOnlyDictionary<string, string[]>?", "errors", parameter => parameter.WithDefaultValue("null"));
                        method.AddStatement("""
                            var problem = new Dictionary<string, object?>(StringComparer.Ordinal)
                            {
                            ["type"] = "https://www.rfc-editor.org/rfc/rfc9457#name-problem-details",
                            ["title"] = title,
                            ["status"] = statusCode,
                            ["detail"] = detail
                            };
                            if (errors is not null) problem["errors"] = errors;
                            return Results.Json(problem, statusCode: statusCode, contentType: "application/problem+json");
                            """);
                    });
                    @class.AddMethod("object?", "Sanitize", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("object?", "value");
                        method.AddParameter("bool", "allowOneTimeCredential");
                        method.AddStatement("if (value is null) return null;");
                        method.AddStatement("return SanitizeElement(JsonSerializer.SerializeToElement(value), allowOneTimeCredential);");
                    });
                    @class.AddMethod("object?", "SanitizeElement", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("JsonElement", "element");
                        method.AddParameter("bool", "allowOneTimeCredential");
                        method.AddStatement("""
                            if (element.ValueKind == JsonValueKind.Object)
                            {
                            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                            foreach (var property in element.EnumerateObject())
                            {
                            if (IsSensitive(property.Name, allowOneTimeCredential)) continue;
                            result[property.Name] = SanitizeElement(property.Value, allowOneTimeCredential);
                            }
                            return result;
                            }
                            if (element.ValueKind == JsonValueKind.Array) return element.EnumerateArray().Select(x => SanitizeElement(x, allowOneTimeCredential)).ToArray();
                            if (element.ValueKind == JsonValueKind.String) return element.GetString();
                            if (element.ValueKind == JsonValueKind.Number) return element.TryGetInt64(out var integer) ? integer : element.GetDecimal();
                            if (element.ValueKind == JsonValueKind.True) return true;
                            if (element.ValueKind == JsonValueKind.False) return false;
                            return null;
                            """);
                    });
                    @class.AddMethod("bool", "IsSensitive", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "propertyName");
                        method.AddParameter("bool", "allowOneTimeCredential");
                        method.AddStatement("""
                            var normalized = propertyName.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
                            if (normalized.Contains("privatekey", StringComparison.Ordinal) || normalized.Contains("correlation", StringComparison.Ordinal) || normalized.EndsWith("hash", StringComparison.Ordinal) || normalized.StartsWith("encrypted", StringComparison.Ordinal)) return true;
                            if (allowOneTimeCredential && (normalized is "clearapikey" or "clientsecret" or "clearcredential")) return false;
                            return normalized.Contains("password", StringComparison.Ordinal) || normalized.Contains("credential", StringComparison.Ordinal) || normalized is "secret" or "clientsecret" or "clearapikey";
                            """);
                    });
                    @class.AddMethod("object", "Page", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IReadOnlyList<SecurityAuthorityGrantCatalogEntry>", "items");
                        method.AddParameter("int", "pageNumber");
                        method.AddParameter("int", "pageSize");
                        method.AddStatement("return new { totalCount = items.Count, pageNumber, pageSize, items = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray() };");
                    });
                    @class.AddMethod("string", "RequiredString", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("JsonElement", "element");
                        method.AddParameter("string", "propertyName");
                        method.AddStatement("if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString())) throw new ArgumentException(propertyName + \" is required.\", propertyName);");
                        method.AddStatement("return property.GetString()!;");
                    });
                    @class.AddMethod("string?", "OptionalString", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("JsonElement", "element");
                        method.AddParameter("string", "propertyName");
                        method.AddStatement("if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null) return null;");
                        method.AddStatement("if (property.ValueKind != JsonValueKind.String) throw new ArgumentException(propertyName + \" must be a string.\", propertyName);");
                        method.AddStatement("return string.IsNullOrWhiteSpace(property.GetString()) ? null : property.GetString();");
                    });
                    @class.AddMethod("string", "Scope", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "resource");
                        method.AddParameter("string", "action");
                        method.AddStatement("return \"security.management.\" + resource + \".\" + action;");
                    });
                    @class.AddMethod("bool", "AllowsOneTimeCredential", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "resource");
                        method.AddParameter("string", "action");
                        method.AddStatement("return (string.Equals(resource, \"api-keys\", StringComparison.Ordinal) && (string.Equals(action, \"create\", StringComparison.Ordinal) || string.Equals(action, \"regenerate\", StringComparison.Ordinal))) || (string.Equals(resource, \"oauth-clients\", StringComparison.Ordinal) && (string.Equals(action, \"create\", StringComparison.Ordinal) || string.Equals(action, \"regenerate-secret\", StringComparison.Ordinal)));");
                    });
                    @class.AddMethod("string", "Title", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("int", "statusCode");
                        method.AddStatement("return statusCode switch { StatusCodes.Status400BadRequest => \"Bad Request\", StatusCodes.Status401Unauthorized => \"Unauthorized\", StatusCodes.Status403Forbidden => \"Forbidden\", StatusCodes.Status404NotFound => \"Not Found\", StatusCodes.Status409Conflict => \"Conflict\", _ => \"Error\" };");
                    });
                    @class.AddMethod("ValueTask<IReadOnlyList<string>>", "GetPrincipalScopesAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("SecurityAuthorityApiKey", "apiKey");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            var now = _utcNow();
                            var grants = new List<(string PrincipalType, string PrincipalId, SecurityAuthorityGrant Grant)>();
                            var ownerGrants = await _dataSource.GetGrantsAsync(apiKey.OwnerPrincipalType, apiKey.OwnerId, cancellationToken)
                            ?? throw new InvalidOperationException("The authorization data source returned null owner Grants.");
                            grants.AddRange(ownerGrants.Select(x => (apiKey.OwnerPrincipalType, apiKey.OwnerId, x)));
                            var memberships = await _dataSource.GetRoleMembershipsAsync(apiKey.OwnerPrincipalType, apiKey.OwnerId, cancellationToken)
                            ?? throw new InvalidOperationException("The authorization data source returned null Role Memberships.");
                            foreach (var membership in memberships.Where(x => !x.IsRevoked && x.RevokedAt is null && (x.ExpiresAt is null || x.ExpiresAt > now)))
                            {
                            var role = await _dataSource.GetRoleAsync(membership.RoleId, cancellationToken);
                            if (role is not null && role.IsEnabled)
                            {
                            var roleGrants = await _dataSource.GetGrantsAsync("Role", role.Id, cancellationToken)
                            ?? throw new InvalidOperationException("The authorization data source returned null Role Grants.");
                            grants.AddRange(roleGrants.Select(x => ("Role", role.Id, x)));
                            }
                            }

                            var directApiKeyGrants = await _dataSource.GetGrantsAsync("ApiKey", apiKey.Id, cancellationToken)
                            ?? throw new InvalidOperationException("The authorization data source returned null API Key Grants.");
                            grants.AddRange(directApiKeyGrants.Select(x => ("ApiKey", apiKey.Id, x)));
                            var active = grants.Where(x =>
                            !x.Grant.IsRevoked &&
                            x.Grant.RevokedAt is null &&
                            (x.Grant.ExpiresAt is null || x.Grant.ExpiresAt > now) &&
                            (apiKey.TenantId is null || string.Equals(x.Grant.TenantId, apiKey.TenantId, StringComparison.Ordinal)))
                            .ToArray();
                            var denied = active
                            .Where(x => string.Equals(x.Grant.Effect, "Deny", StringComparison.Ordinal))
                            .Select(x => x.Grant.PermissionKey)
                            .ToHashSet(StringComparer.Ordinal);
                            var allowed = new HashSet<string>(StringComparer.Ordinal);
                            foreach (var candidate in active.Where(x => string.Equals(x.Grant.Effect, "Allow", StringComparison.Ordinal)))
                            {
                            var principalType = string.Equals(candidate.PrincipalType, "ApiKey", StringComparison.Ordinal)
                            ? "ApiKey"
                            : apiKey.OwnerPrincipalType;
                            var principalId = string.Equals(candidate.PrincipalType, "ApiKey", StringComparison.Ordinal)
                            ? apiKey.Id
                            : apiKey.OwnerId;
                            var decision = await _authorizationEngine.AuthorizeAsync(
                            new SecurityAuthorityAuthorizationRequest(
                            principalType,
                            principalId,
                            candidate.Grant.TenantId ?? apiKey.TenantId ?? string.Empty,
                            candidate.Grant.TenantResourceId,
                            candidate.Grant.ResourceKind,
                            candidate.Grant.PermissionKey),
                            cancellationToken);
                            if (decision.IsAllowed)
                            {
                            allowed.Add(candidate.Grant.PermissionKey);
                            }
                            }

                            allowed.ExceptWith(denied);
                            return allowed.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                            """);
                    });
                    @class.AddMethod("ValueTask<string?>", "GetActiveOwnerTenantIdAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("string", "ownerType");
                        method.AddParameter("string", "ownerId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            if (string.Equals(ownerType, "User", StringComparison.Ordinal))
                            {
                            var user = await _dataSource.GetUserAsync(ownerId, cancellationToken);
                            if (!SecurityAuthorityLifecycle.IsActiveUser(user)) throw new InvalidOperationException("An API Key User owner must exist and be active.");
                            return await _dataSource.GetPrincipalTenantIdAsync("User", ownerId, cancellationToken);
                            }

                            if (string.Equals(ownerType, "Service", StringComparison.Ordinal))
                            {
                            var service = await _dataSource.GetServiceAsync(ownerId, cancellationToken);
                            if (!SecurityAuthorityLifecycle.IsActiveService(service)) throw new InvalidOperationException("An API Key Service owner must exist and be active.");
                            return service!.TenantId ?? await _dataSource.GetPrincipalTenantIdAsync("Service", ownerId, cancellationToken);
                            }

                            throw new InvalidOperationException("An API Key owner must be a User or Service.");
                            """);
                    });
                    @class.AddMethod("SecurityAuthorityGrant", "ToGrant", method =>
                    {
                        method.Private();
                        method.AddParameter("SecurityAuthorityApiKey", "apiKey");
                        method.AddParameter("SecurityAuthorityApiKeyScope", "scope");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("return new SecurityAuthorityGrant(_newId(), \"ApiKey\", apiKey.Id, scope.TenantResourceId, scope.ResourceKind, scope.PermissionKey, \"Allow\", scope.Applicability, apiKey.ExpiresAt, false, null, null, apiKey.TenantId, now, now, _newConcurrencyToken());");
                    });
                    @class.AddMethod("string", "CreateClearApiKey", method =>
                    {
                        method.Private();
                        method.AddParameter("string", "apiKeyId");
                        method.AddStatement("var secret = SecurityAuthorityBase64Url.Encode(RandomNumberGenerator.GetBytes(32));");
                        method.AddStatement("return _configuredPrefix + \".\" + apiKeyId + \".\" + secret;");
                    });
                    @class.AddMethod("bool", "TryReadApiKeyId", method =>
                    {
                        method.Private();
                        method.AddParameter("string", "clearApiKey");
                        method.AddParameter("string", "apiKeyId", parameter => parameter.WithOutParameterModifier());
                        method.AddStatement("apiKeyId = string.Empty;");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(clearApiKey) || clearApiKey.Any(char.IsWhiteSpace)) return false;");
                        method.AddStatement("var parts = clearApiKey.Split('.');");
                        method.AddStatement("if (parts.Length != 3 || !string.Equals(parts[0], _configuredPrefix, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2])) return false;");
                        method.AddStatement("apiKeyId = parts[1];");
                        method.AddStatement("return true;");
                    });
                    @class.AddMethod("void", "ValidateScopes", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IReadOnlyList<SecurityAuthorityApiKeyScope>", "scopes");
                        method.AddParameter("string?", "tenantId");
                        method.AddStatement("""
                            foreach (var scope in scopes)
                            {
                            ArgumentNullException.ThrowIfNull(scope);
                            ArgumentException.ThrowIfNullOrWhiteSpace(scope.TenantResourceId);
                            ArgumentException.ThrowIfNullOrWhiteSpace(scope.ResourceKind);
                            ArgumentException.ThrowIfNullOrWhiteSpace(scope.PermissionKey);
                            if (!string.Equals(scope.Applicability, "ThisResourceOnly", StringComparison.Ordinal) &&
                            !string.Equals(scope.Applicability, "ThisResourceAndDescendants", StringComparison.Ordinal))
                            {
                            throw new ArgumentException("An API Key Scope Applicability must be ThisResourceOnly or ThisResourceAndDescendants.", nameof(scopes));
                            }

                            if (tenantId is null)
                            {
                            throw new InvalidOperationException("Direct API Key Scopes require a Tenant context.");
                            }
                            }
                            """);
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
