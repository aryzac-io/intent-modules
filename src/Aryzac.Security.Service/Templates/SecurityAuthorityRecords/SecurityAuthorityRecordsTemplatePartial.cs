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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityRecords
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityRecordsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityRecords";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityRecordsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddRecord("SecurityAuthorityUser", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "DisplayName");
                        ctor.AddParameter("string", "NormalizedEmail");
                        ctor.AddParameter("string?", "AvatarUrl");
                        ctor.AddParameter("string", "Status");
                        ctor.AddParameter("DateTimeOffset?", "LastSeenAt");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityExternalIdentity", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "Issuer");
                        ctor.AddParameter("string", "Subject");
                        ctor.AddParameter("string", "UserId");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset?", "LastSeenAt");
                    });
                })
                .AddRecord("SecurityAuthorityService", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "Name");
                        ctor.AddParameter("string?", "Description");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("bool", "IsActive");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityOAuthClient", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "ClientIdentifier");
                        ctor.AddParameter("string", "DisplayName");
                        ctor.AddParameter("string", "ClientType");
                        ctor.AddParameter("string?", "SecretHash");
                        ctor.AddParameter("bool", "IsActive");
                        ctor.AddParameter("IReadOnlyList<string>", "RedirectUris");
                        ctor.AddParameter("IReadOnlyList<string>", "PostLogoutRedirectUris");
                        ctor.AddParameter("IReadOnlyList<string>", "AllowedGrantTypes");
                        ctor.AddParameter("IReadOnlyList<string>", "AllowedScopes");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("string?", "PreferredIdentityProviderId");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityIdentityProvider", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "ProviderIdentifier");
                        ctor.AddParameter("string", "ProviderType");
                        ctor.AddParameter("string", "DisplayName");
                        ctor.AddParameter("string", "AuthorityUrl");
                        ctor.AddParameter("string?", "Issuer");
                        ctor.AddParameter("string", "ClientIdentifier");
                        ctor.AddParameter("string", "EncryptedClientSecret");
                        ctor.AddParameter("string", "RequestedScopes");
                        ctor.AddParameter("bool", "IsActive");
                        ctor.AddParameter("int", "DisplayPriority");
                        ctor.AddParameter("string?", "TenantResourceId");
                        ctor.AddParameter("string", "AccessMode");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityApiKey", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "Name");
                        ctor.AddParameter("string", "OwnerPrincipalType");
                        ctor.AddParameter("string", "OwnerId");
                        ctor.AddParameter("string", "PublicPrefix");
                        ctor.AddParameter("string", "KeyHash");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("DateTimeOffset?", "ExpiresAt");
                        ctor.AddParameter("bool", "IsRevoked");
                        ctor.AddParameter("DateTimeOffset?", "RevokedAt");
                        ctor.AddParameter("DateTimeOffset?", "LastUsedAt");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityTenantResourceRecord", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "TenantResourceId");
                        ctor.AddParameter("string", "ResourceKind");
                        ctor.AddParameter("string?", "ParentTenantResourceId");
                        ctor.AddParameter("string", "TenantId");
                        ctor.AddParameter("bool", "InheritanceProtected");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityRole", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "DefinitionTenantResourceId");
                        ctor.AddParameter("string", "RoleKey");
                        ctor.AddParameter("string", "Name");
                        ctor.AddParameter("string?", "Description");
                        ctor.AddParameter("bool", "IsEnabled");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityRoleMembership", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "RoleId");
                        ctor.AddParameter("string?", "UserId");
                        ctor.AddParameter("string?", "ServiceId");
                        ctor.AddParameter("DateTimeOffset?", "ExpiresAt");
                        ctor.AddParameter("bool", "IsRevoked");
                        ctor.AddParameter("DateTimeOffset?", "RevokedAt");
                        ctor.AddParameter("string?", "Reason");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityGrant", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "PrincipalType");
                        ctor.AddParameter("string", "PrincipalId");
                        ctor.AddParameter("string", "TenantResourceId");
                        ctor.AddParameter("string", "ResourceKind");
                        ctor.AddParameter("string", "PermissionKey");
                        ctor.AddParameter("string", "Effect");
                        ctor.AddParameter("string", "Applicability");
                        ctor.AddParameter("DateTimeOffset?", "ExpiresAt");
                        ctor.AddParameter("bool", "IsRevoked");
                        ctor.AddParameter("DateTimeOffset?", "RevokedAt");
                        ctor.AddParameter("string?", "Reason");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityAuthorizationCode", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "CodeHash");
                        ctor.AddParameter("string", "OAuthClientId");
                        ctor.AddParameter("string", "UserId");
                        ctor.AddParameter("string", "RedirectUri");
                        ctor.AddParameter("IReadOnlyList<string>", "Scopes");
                        ctor.AddParameter("string", "PkceChallenge");
                        ctor.AddParameter("string?", "Nonce");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("DateTimeOffset?", "RedeemedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityDeviceGrant", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "DeviceCodeHash");
                        ctor.AddParameter("string", "UserCode");
                        ctor.AddParameter("string", "OAuthClientId");
                        ctor.AddParameter("IReadOnlyList<string>", "RequestedScopes");
                        ctor.AddParameter("int", "PollingIntervalSeconds");
                        ctor.AddParameter("string", "Status");
                        ctor.AddParameter("string?", "UserId");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("DateTimeOffset?", "ApprovedAt");
                        ctor.AddParameter("DateTimeOffset?", "DeniedAt");
                        ctor.AddParameter("DateTimeOffset?", "RedeemedAt");
                        ctor.AddParameter("DateTimeOffset?", "LastPolledAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityRefreshToken", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "TokenHash");
                        ctor.AddParameter("string", "OAuthClientId");
                        ctor.AddParameter("string", "UserId");
                        ctor.AddParameter("DateTimeOffset", "IssuedAt");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("DateTimeOffset?", "LastUsedAt");
                        ctor.AddParameter("string?", "ReplacedByTokenId");
                        ctor.AddParameter("DateTimeOffset?", "RevokedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityAccessTokenMetadata", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "TokenId");
                        ctor.AddParameter("string", "SigningKeyId");
                        ctor.AddParameter("string", "Issuer");
                        ctor.AddParameter("string", "Audience");
                        ctor.AddParameter("string", "Subject");
                        ctor.AddParameter("string", "PrincipalType");
                        ctor.AddParameter("IReadOnlyList<string>", "Scopes");
                        ctor.AddParameter("DateTimeOffset", "IssuedAt");
                        ctor.AddParameter("DateTimeOffset", "NotBefore");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("IReadOnlyDictionary<string, string>", "ContextualClaims");
                        ctor.AddParameter("DateTimeOffset?", "RevokedAt");
                        ctor.AddParameter("string?", "RevocationReason");
                    });
                })
                .AddRecord("SecurityAuthorityIdTokenMetadata", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "TokenId");
                        ctor.AddParameter("string", "SigningKeyId");
                        ctor.AddParameter("string", "Issuer");
                        ctor.AddParameter("string", "ClientAudience");
                        ctor.AddParameter("string", "UserSubject");
                        ctor.AddParameter("DateTimeOffset", "IssuedAt");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("string?", "NonceHash");
                        ctor.AddParameter("string", "IssuanceStatus");
                    });
                })
                .AddRecord("SecurityAuthoritySsoSession", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("string", "OpaqueCookieIdentifier");
                        ctor.AddParameter("string", "UserId");
                        ctor.AddParameter("DateTimeOffset", "IssuedAt");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("DateTimeOffset?", "RevokedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityBootstrapState", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Id");
                        ctor.AddParameter("bool", "IsClosed");
                        ctor.AddParameter("string?", "AdministratorUserId");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityIdempotencyOutcome", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "IdempotencyKey");
                        ctor.AddParameter("string", "OperationName");
                        ctor.AddParameter("string", "RequestHash");
                        ctor.AddParameter("string", "OutcomeReference");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                    });
                })
                .AddRecord("SecurityAuthorityProcessedIntegrationEvent", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "EventId");
                        ctor.AddParameter("string", "EventType");
                        ctor.AddParameter("int", "Version");
                        ctor.AddParameter("DateTimeOffset", "ProcessedAt");
                        ctor.AddParameter("string", "OutcomeReference");
                    });
                })
                .AddRecord("SecurityAuthorityRevokedCredentialMetadata", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "CredentialId");
                        ctor.AddParameter("string", "CredentialCategory");
                        ctor.AddParameter("string?", "OwnerId");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("DateTimeOffset?", "RevokedAt");
                        ctor.AddParameter("DateTimeOffset?", "RedeemedAt");
                        ctor.AddParameter("DateTimeOffset?", "TerminalAt");
                        ctor.AddParameter("DateTimeOffset", "RetainUntil");
                        ctor.AddParameter("string", "ConcurrencyToken");
                    });
                })
                .AddRecord("SecurityAuthorityCleanupCandidate", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "CredentialId");
                        ctor.AddParameter("string", "CredentialCategory");
                        ctor.AddParameter("string?", "OwnerId");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("DateTimeOffset", "ExpiresAt");
                        ctor.AddParameter("DateTimeOffset?", "RevokedAt");
                        ctor.AddParameter("DateTimeOffset?", "RedeemedAt");
                        ctor.AddParameter("DateTimeOffset?", "TerminalAt");
                        ctor.AddParameter("DateTimeOffset", "RetainUntil");
                        ctor.AddParameter("string", "ConcurrencyToken");
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