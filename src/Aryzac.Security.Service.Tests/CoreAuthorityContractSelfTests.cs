using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class CoreAuthorityContractSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string ContractsSource = ReadServiceSource("Templates", "SecurityAuthorityContracts", "SecurityAuthorityContractsTemplatePartial.cs");
    private static readonly string RecordsSource = ReadServiceSource("Templates", "SecurityAuthorityRecords", "SecurityAuthorityRecordsTemplatePartial.cs");
    private static readonly string ValidationSource = ReadServiceSource("Templates", "SecurityAuthorityValidation", "SecurityAuthorityValidationTemplatePartial.cs");
    private static readonly string IntegrationSource = ReadServiceSource("FactoryExtensions", "SecurityAuthorityIntegration.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("all modeled authority records retain their fields", AllModeledRecordsRetainTheirFields),
            ("record bounds and field-addressed failures are emitted", RecordBoundsAndFieldAddressedFailuresAreEmitted),
            ("associations known references and uniqueness are enforced", AssociationsKnownReferencesAndUniquenessAreEnforced),
            ("redeemable secrets are hash or opaque only", RedeemableSecretsAreHashOrOpaqueOnly),
            ("all named mutable records use optimistic concurrency", AllNamedMutableRecordsUseOptimisticConcurrency),
            ("atomic mutations rollback and gate credential reveal", AtomicMutationsRollbackAndGateCredentialReveal),
            ("persistence startup guarantees and host participation are required", PersistenceStartupGuaranteesAndHostParticipationAreRequired),
            ("tenant adapter ancestry and tenant isolation rules are emitted", TenantAdapterAncestryAndTenantIsolationRulesAreEmitted),
            ("utc stable identity and ordinal comparison rules are emitted", UtcStableIdentityAndOrdinalComparisonRulesAreEmitted)
        };
        var failures = 0;

        foreach (var test in tests)
        {
            try
            {
                test.Execute();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {test.Name}: {exception}");
            }
        }

        Console.WriteLine($"{tests.Length - failures}/{tests.Length} core authority contract self-tests passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void AllModeledRecordsRetainTheirFields()
    {
        var expectedRecords = new Dictionary<string, string[]>
        {
            ["SecurityAuthorityUser"] = ["Id", "DisplayName", "NormalizedEmail", "AvatarUrl", "Status", "LastSeenAt", "CreatedAt", "UpdatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityExternalIdentity"] = ["Id", "Issuer", "Subject", "UserId", "CreatedAt", "LastSeenAt"],
            ["SecurityAuthorityService"] = ["Id", "Name", "Description", "TenantId", "IsActive", "CreatedAt", "UpdatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityOAuthClient"] = ["Id", "ClientIdentifier", "DisplayName", "ClientType", "SecretHash", "IsActive", "RedirectUris", "PostLogoutRedirectUris", "AllowedGrantTypes", "AllowedScopes", "TenantId", "PreferredIdentityProviderId", "CreatedAt", "UpdatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityIdentityProvider"] = ["Id", "ProviderIdentifier", "ProviderType", "DisplayName", "AuthorityUrl", "Issuer", "ClientIdentifier", "EncryptedClientSecret", "RequestedScopes", "IsActive", "DisplayPriority", "TenantResourceId", "AccessMode", "CreatedAt", "UpdatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityApiKey"] = ["Id", "Name", "OwnerPrincipalType", "OwnerId", "PublicPrefix", "KeyHash", "TenantId", "ExpiresAt", "IsRevoked", "RevokedAt", "LastUsedAt", "CreatedAt", "UpdatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityTenantResourceRecord"] = ["TenantResourceId", "ResourceKind", "ParentTenantResourceId", "TenantId", "InheritanceProtected", "ConcurrencyToken"],
            ["SecurityAuthorityRole"] = ["Id", "DefinitionTenantResourceId", "RoleKey", "Name", "Description", "IsEnabled", "TenantId", "CreatedAt", "UpdatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityRoleMembership"] = ["Id", "RoleId", "UserId", "ServiceId", "ExpiresAt", "IsRevoked", "RevokedAt", "Reason", "CreatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityGrant"] = ["Id", "PrincipalType", "PrincipalId", "TenantResourceId", "ResourceKind", "PermissionKey", "Effect", "Applicability", "ExpiresAt", "IsRevoked", "RevokedAt", "Reason", "TenantId", "CreatedAt", "UpdatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityAuthorizationCode"] = ["Id", "CodeHash", "OAuthClientId", "UserId", "RedirectUri", "Scopes", "PkceChallenge", "Nonce", "CreatedAt", "ExpiresAt", "RedeemedAt", "ConcurrencyToken"],
            ["SecurityAuthorityDeviceGrant"] = ["Id", "DeviceCodeHash", "UserCode", "OAuthClientId", "RequestedScopes", "PollingIntervalSeconds", "Status", "UserId", "CreatedAt", "ExpiresAt", "ApprovedAt", "DeniedAt", "RedeemedAt", "LastPolledAt", "ConcurrencyToken"],
            ["SecurityAuthorityRefreshToken"] = ["Id", "TokenHash", "OAuthClientId", "UserId", "IssuedAt", "ExpiresAt", "LastUsedAt", "ReplacedByTokenId", "RevokedAt", "ConcurrencyToken"],
            ["SecurityAuthorityAccessTokenMetadata"] = ["TokenId", "SigningKeyId", "Issuer", "Audience", "Subject", "PrincipalType", "Scopes", "IssuedAt", "NotBefore", "ExpiresAt", "ContextualClaims", "RevokedAt", "RevocationReason"],
            ["SecurityAuthorityIdTokenMetadata"] = ["TokenId", "SigningKeyId", "Issuer", "ClientAudience", "UserSubject", "IssuedAt", "ExpiresAt", "NonceHash", "IssuanceStatus"],
            ["SecurityAuthoritySsoSession"] = ["Id", "OpaqueCookieIdentifier", "UserId", "IssuedAt", "ExpiresAt", "RevokedAt", "ConcurrencyToken"],
            ["SecurityAuthorityBootstrapState"] = ["Id", "IsClosed", "AdministratorUserId", "CreatedAt", "UpdatedAt", "ConcurrencyToken"],
            ["SecurityAuthorityIdempotencyOutcome"] = ["IdempotencyKey", "OperationName", "RequestHash", "OutcomeReference", "CreatedAt", "ExpiresAt"],
            ["SecurityAuthorityProcessedIntegrationEvent"] = ["EventId", "EventType", "Version", "ProcessedAt", "OutcomeReference"]
        };

        foreach (var expectedRecord in expectedRecords)
        {
            var block = GetRecordBlock(expectedRecord.Key);
            foreach (var field in expectedRecord.Value)
            {
                Contains(block, $"\"{field}\"");
            }
        }
    }

    private static void RecordBoundsAndFieldAddressedFailuresAreEmitted()
    {
        foreach (var expected in new[]
        {
            "Required(failures, nameof(record.Id), record.Id)",
            "Length(failures, nameof(record.DisplayName), record.DisplayName, 1, 200)",
            "Length(failures, nameof(record.NormalizedEmail), record.NormalizedEmail, 3, 320)",
            "OptionalAbsoluteUri(failures, nameof(record.AvatarUrl), record.AvatarUrl, 2048)",
            "OneOf(failures, nameof(record.Status), record.Status",
            "AbsoluteUri(failures, nameof(record.Issuer), record.Issuer, 2048)",
            "Length(failures, nameof(record.Subject), record.Subject, 1, 255)",
            "Required(failures, nameof(record.UserId), record.UserId)",
            "Length(failures, nameof(record.Name), record.Name, 1, 200)",
            "OptionalLength(failures, nameof(record.Description), record.Description, 2000)",
            "Length(failures, nameof(record.ClientIdentifier), record.ClientIdentifier, 1, 200)",
            "OneOf(failures, nameof(record.ClientType), record.ClientType",
            "Required(failures, nameof(record.SecretHash), record.SecretHash)",
            "Uris(failures, nameof(record.RedirectUris), record.RedirectUris)",
            "Uris(failures, nameof(record.PostLogoutRedirectUris), record.PostLogoutRedirectUris)",
            "RequiredValues(failures, nameof(record.AllowedGrantTypes), record.AllowedGrantTypes)",
            "Values(failures, nameof(record.AllowedScopes), record.AllowedScopes)",
            "Length(failures, nameof(record.ProviderIdentifier), record.ProviderIdentifier, 1, 100)",
            "OneOf(failures, nameof(record.ProviderType), record.ProviderType",
            "AbsoluteUri(failures, nameof(record.AuthorityUrl), record.AuthorityUrl, 2048)",
            "OptionalAbsoluteUri(failures, nameof(record.Issuer), record.Issuer, 2048)",
            "Required(failures, nameof(record.EncryptedClientSecret), record.EncryptedClientSecret)",
            "SpaceDelimitedValues(failures, nameof(record.RequestedScopes), record.RequestedScopes)",
            "OneOf(failures, nameof(record.AccessMode), record.AccessMode",
            "OneOf(failures, nameof(record.OwnerPrincipalType), record.OwnerPrincipalType",
            "Required(failures, nameof(record.OwnerId), record.OwnerId)",
            "Required(failures, nameof(record.PublicPrefix), record.PublicPrefix)",
            "Required(failures, nameof(record.KeyHash), record.KeyHash)",
            "Required(failures, nameof(record.TenantResourceId), record.TenantResourceId)",
            "Required(failures, nameof(record.ResourceKind), record.ResourceKind)",
            "Required(failures, nameof(record.TenantId), record.TenantId)",
            "Required(failures, nameof(record.DefinitionTenantResourceId), record.DefinitionTenantResourceId)",
            "Length(failures, nameof(record.RoleKey), record.RoleKey, 1, 100)",
            "Required(failures, nameof(record.RoleId), record.RoleId)",
            "Exactly one User or Service must be assigned.",
            "Length(failures, nameof(record.PermissionKey), record.PermissionKey, 1, 200)",
            "OneOf(failures, nameof(record.Effect), record.Effect",
            "OneOf(failures, nameof(record.Applicability), record.Applicability",
            "OptionalLength(failures, nameof(record.Reason), record.Reason, 1000)",
            "Required(failures, nameof(authorizationCode.Id), authorizationCode.Id)",
            "Required(failures, nameof(authorizationCode.CodeHash), authorizationCode.CodeHash)",
            "Required(failures, nameof(authorizationCode.OAuthClientId), authorizationCode.OAuthClientId)",
            "Required(failures, nameof(authorizationCode.UserId), authorizationCode.UserId)",
            "AbsoluteUri(failures, nameof(authorizationCode.RedirectUri), authorizationCode.RedirectUri, 2048)",
            "Required(failures, nameof(authorizationCode.PkceChallenge), authorizationCode.PkceChallenge)",
            "Required(failures, nameof(deviceGrant.Id), deviceGrant.Id)",
            "Required(failures, nameof(deviceGrant.DeviceCodeHash), deviceGrant.DeviceCodeHash)",
            "Length(failures, nameof(deviceGrant.UserCode), deviceGrant.UserCode, 8, 8)",
            "Required(failures, nameof(deviceGrant.OAuthClientId), deviceGrant.OAuthClientId)",
            "OneOf(failures, nameof(deviceGrant.Status), deviceGrant.Status",
            "Required(failures, nameof(refreshToken.Id), refreshToken.Id)",
            "Required(failures, nameof(refreshToken.TokenHash), refreshToken.TokenHash)",
            "Required(failures, nameof(refreshToken.OAuthClientId), refreshToken.OAuthClientId)",
            "Required(failures, nameof(refreshToken.UserId), refreshToken.UserId)",
            "Required(failures, nameof(accessToken.TokenId), accessToken.TokenId)",
            "Required(failures, nameof(accessToken.SigningKeyId), accessToken.SigningKeyId)",
            "AbsoluteUri(failures, nameof(accessToken.Issuer), accessToken.Issuer, 2048)",
            "Required(failures, nameof(accessToken.Audience), accessToken.Audience)",
            "Required(failures, nameof(accessToken.Subject), accessToken.Subject)",
            "OneOf(failures, nameof(accessToken.PrincipalType), accessToken.PrincipalType",
            "Required(failures, nameof(idToken.TokenId), idToken.TokenId)",
            "Required(failures, nameof(idToken.SigningKeyId), idToken.SigningKeyId)",
            "AbsoluteUri(failures, nameof(idToken.Issuer), idToken.Issuer, 2048)",
            "Required(failures, nameof(idToken.ClientAudience), idToken.ClientAudience)",
            "Required(failures, nameof(idToken.UserSubject), idToken.UserSubject)",
            "Required(failures, nameof(idToken.IssuanceStatus), idToken.IssuanceStatus)",
            "Required(failures, nameof(ssoSession.Id), ssoSession.Id)",
            "Required(failures, nameof(ssoSession.OpaqueCookieIdentifier), ssoSession.OpaqueCookieIdentifier)",
            "Required(failures, nameof(ssoSession.UserId), ssoSession.UserId)",
            "UtcRange(failures, nameof(authorizationCode.CreatedAt)",
            "UtcRange(failures, nameof(deviceGrant.CreatedAt)",
            "UtcRange(failures, nameof(refreshToken.IssuedAt)",
            "UtcRange(failures, nameof(accessToken.IssuedAt)",
            "UtcRange(failures, nameof(idToken.IssuedAt)",
            "UtcRange(failures, nameof(ssoSession.IssuedAt)",
            "failures.Add(new SecurityAuthorityValidationFailure(field, code, message))"
        })
        {
            Contains(ValidationSource, expected);
        }

        foreach (var helper in new[] { "Required", "Length", "OptionalLength", "AbsoluteUri", "OptionalAbsoluteUri", "Values", "RequiredValues", "SpaceDelimitedValues", "Uris" })
        {
            Contains(ValidationSource, $"AddMethod(\"void\", \"{helper}\"");
        }
    }

    private static void AssociationsKnownReferencesAndUniquenessAreEnforced()
    {
        foreach (var expected in new[]
        {
            "context.ExistsAsync(\\\"User\\\", record.UserId",
            "context.IsUniqueAsync(\\\"ExternalIdentity\\\", \\\"IssuerSubject\\\"",
            "context.IsUniqueAsync(\\\"OAuthClient\\\", nameof(record.ClientIdentifier)",
            "context.ExistsAsync(\\\"IdentityProvider\\\", record.PreferredIdentityProviderId",
            "context.IsUniqueAsync(\\\"IdentityProvider\\\", nameof(record.ProviderIdentifier)",
            "context.ExistsAsync(ownerType, record.OwnerId",
            "context.IsUniqueAsync(\\\"Role\\\", \\\"DefinitionResourceRoleKey\\\"",
            "Exactly one User or Service must be assigned.",
            "context.ExistsAsync(\\\"Role\\\", record.RoleId",
            "context.ExistsAsync(memberType, memberId",
            "context.ExistsAsync(record.PrincipalType, record.PrincipalId",
            "context.ExistsAsync(\\\"OAuthClient\\\", authorizationCode.OAuthClientId",
            "context.ExistsAsync(\\\"User\\\", ssoSession.UserId",
            "unknown_reference",
            "not_unique"
        })
        {
            Contains(ValidationSource, expected);
        }
    }

    private static void RedeemableSecretsAreHashOrOpaqueOnly()
    {
        foreach (var field in new[]
        {
            "SecretHash", "EncryptedClientSecret", "KeyHash", "CodeHash", "DeviceCodeHash", "TokenHash", "NonceHash", "OpaqueCookieIdentifier"
        })
        {
            Contains(RecordsSource, $"\"{field}\"");
        }

        foreach (var forbiddenField in new[]
        {
            "ctor.AddParameter(\\\"string\\\", \\\"ClientSecret\\\")",
            "ctor.AddParameter(\\\"string\\\", \\\"ApiKeySecret\\\")",
            "ctor.AddParameter(\\\"string\\\", \\\"AuthorizationCodeSecret\\\")",
            "ctor.AddParameter(\\\"string\\\", \\\"DeviceCodeSecret\\\")",
            "ctor.AddParameter(\\\"string\\\", \\\"RefreshTokenSecret\\\")",
            "ctor.AddParameter(\\\"string\\\", \\\"CookieSecret\\\")"
        })
        {
            DoesNotContain(RecordsSource, forbiddenField);
        }

        Contains(ValidationSource, "Public clients cannot retain a client secret hash.");
        Contains(ValidationSource, "Required(failures, nameof(record.KeyHash), record.KeyHash)");
        Contains(ValidationSource, "Required(failures, nameof(authorizationCode.CodeHash), authorizationCode.CodeHash)");
        Contains(ValidationSource, "Required(failures, nameof(deviceGrant.DeviceCodeHash), deviceGrant.DeviceCodeHash)");
        Contains(ValidationSource, "Required(failures, nameof(refreshToken.TokenHash), refreshToken.TokenHash)");
    }

    private static void AllNamedMutableRecordsUseOptimisticConcurrency()
    {
        var validators = new Dictionary<string, string>
        {
            ["User"] = "ValidateUser",
            ["Service"] = "ValidateService",
            ["OAuth Client"] = "ValidateOAuthClientAsync",
            ["Identity Provider"] = "ValidateIdentityProviderAsync",
            ["API Key"] = "ValidateApiKeyAsync",
            ["Tenant Resource registration"] = "ValidateTenantResourceRecordAsync",
            ["Role"] = "ValidateRoleAsync",
            ["Role Membership"] = "ValidateRoleMembershipAsync",
            ["Grant"] = "ValidateGrantAsync",
            ["bootstrap"] = "ValidateBootstrapState"
        };

        foreach (var validator in validators)
        {
            var block = GetValidationMethodBlock(validator.Value);
            Contains(block, "ConcurrencyToken");
            True(block.Contains("Concurrency(failures", StringComparison.Ordinal) ||
                block.Contains("MutableLifecycle(failures", StringComparison.Ordinal),
                $"{validator.Key} does not emit optimistic concurrency validation.");
        }

        Contains(ContractsSource, "UpdateAsync");
        Contains(ContractsSource, "expectedConcurrencyToken");
        Contains(ValidationSource, "concurrency_not_advanced");
    }

    private static void AtomicMutationsRollbackAndGateCredentialReveal()
    {
        foreach (var operation in new[]
        {
            "TokenRedemption", "RefreshTokenRotation", "ApiKeyRegeneration", "FirstAdministratorBootstrap", "IdempotentProvisioning", "IntegrationEventDeduplication"
        })
        {
            Contains(ContractsSource, $"@enum.AddLiteral(\"{operation}\")");
        }

        Contains(ValidationSource, "if (!validation.IsValid)");
        Equal(4, Count(ValidationSource, "await operation.RollbackAsync(cancellationToken);"));
        Equal(2, Count(ValidationSource, "await operation.CommitAsync(cancellationToken);"));
        Contains(ValidationSource, "await operation.DisposeAsync();");
        Contains(ContractsSource, "if (receipt.OperationId != OperationId)");
        Contains(ContractsSource, "Interlocked.Exchange(ref _revealed, 1)");
        Contains(IntegrationSource, "defer-credential-reveal-until-commit");
        Contains(IntegrationSource, "rollback-on-pre-commit-failure");
    }

    private static void PersistenceStartupGuaranteesAndHostParticipationAreRequired()
    {
        foreach (var capability in new[]
        {
            "persistence-authority-record-isolation",
            "persistence-uniqueness",
            "persistence-optimistic-concurrency",
            "persistence-atomic-credential-rotation",
            "persistence-one-time-redemption"
        })
        {
            Contains(IntegrationSource, capability);
        }

        Contains(IntegrationSource, "persistence-host-transaction-participation");
        Contains(IntegrationSource, "if (IsTrue(capabilities, \"persistence-transactions\"))");
        Contains(ContractsSource, "AuthorityRecordIsolation && Uniqueness && OptimisticConcurrency && AtomicCredentialRotation && OneTimeRedemption && (!Transactions || HostTransactionParticipation)");
        Contains(ContractsSource, "JoinedHostTransaction");
        Contains(IntegrationSource, "authority-record-scope");
        Contains(IntegrationSource, "SecurityAuthority");
    }

    private static void TenantAdapterAncestryAndTenantIsolationRulesAreEmitted()
    {
        foreach (var expected in new[]
        {
            "HashSet<string>(StringComparer.Ordinal)",
            "cyclic_parentage",
            "unknown_reference",
            "identity_mismatch",
            "tenant_mismatch",
            "Required(failures, \"ResourceKind\", resource.ResourceKind)",
            "ValidateTenantResourceRecordAsync",
            "parent.Ancestry.Resources.Any",
            "resource_kind_mismatch",
            "resource.InheritanceProtected",
            "The API Key and its owner must belong to the same Tenant.",
            "The Role belongs to another Tenant.",
            "The member belongs to another Tenant.",
            "The Grant principal belongs to another Tenant."
        })
        {
            Contains(ValidationSource, expected);
        }

        Contains(IntegrationSource, "TenantScopedCapabilitiesEnabled");
        Contains(IntegrationSource, "ISecurityAuthorityTenantAdapter");
        Contains(IntegrationSource, "if (!application.Settings.GetAuthorityTenancy().TenantScopedCapabilitiesEnabled())");
    }

    private static void UtcStableIdentityAndOrdinalComparisonRulesAreEmitted()
    {
        foreach (var expected in new[]
        {
            "value.Offset != TimeSpan.Zero",
            "Identifiers must remain stable after creation.",
            "Creation timestamps must remain stable after creation.",
            "StringComparison.Ordinal",
            "StringComparer.Ordinal",
            "ordinal case-sensitive comparison",
            "Resource Kind comparison is ordinal and case-sensitive."
        })
        {
            Contains(ValidationSource, expected);
        }

        DoesNotContain(ValidationSource, "StringComparison.OrdinalIgnoreCase");
        DoesNotContain(ValidationSource, "StringComparer.OrdinalIgnoreCase");
    }

    private static string GetRecordBlock(string recordName)
    {
        var marker = $".AddRecord(\"{recordName}\"";
        var start = RecordsSource.IndexOf(marker, StringComparison.Ordinal);
        True(start >= 0, $"Record {recordName} was not found.");
        var end = RecordsSource.IndexOf(".AddRecord(\"", start + marker.Length, StringComparison.Ordinal);
        return RecordsSource[start..(end < 0 ? RecordsSource.Length : end)];
    }

    private static string GetValidationMethodBlock(string methodName)
    {
        var marker = $"AddMethod(\"";
        var nameMarker = $"\", \"{methodName}\"";
        var nameIndex = ValidationSource.IndexOf(nameMarker, StringComparison.Ordinal);
        True(nameIndex >= 0, $"Validation method {methodName} was not found.");
        var start = ValidationSource.LastIndexOf(marker, nameIndex, StringComparison.Ordinal);
        var end = ValidationSource.IndexOf("@class.AddMethod(", nameIndex + nameMarker.Length, StringComparison.Ordinal);
        return ValidationSource[start..(end < 0 ? ValidationSource.Length : end)];
    }

    private static string ReadServiceSource(params string[] path)
    {
        return File.ReadAllText(Path.Combine([ServiceProject, .. path]));
    }

    private static string FindServiceProject()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? directory = new(start);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "Aryzac.Security.Service");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate src/Aryzac.Security.Service.");
    }

    private static int Count(string value, string expected)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(expected, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += expected.Length;
        }

        return count;
    }

    private static void Contains(string value, string expected)
    {
        True(value.Contains(expected, StringComparison.Ordinal), $"Expected to find '{expected}'.");
    }

    private static void DoesNotContain(string value, string unexpected)
    {
        True(!value.Contains(unexpected, StringComparison.Ordinal), $"Did not expect to find '{unexpected}'.");
    }

    private static void Equal<T>(T expected, T actual)
    {
        True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected '{expected}', got '{actual}'.");
    }

    private static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
