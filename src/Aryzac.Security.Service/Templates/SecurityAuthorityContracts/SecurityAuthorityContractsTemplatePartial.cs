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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityContracts
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityContractsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityContracts";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityContractsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityRecords")
                .AddEnum("SecurityAuthorityAtomicOperationKind", @enum =>
                {
                    @enum.AddLiteral("TokenRedemption");
                    @enum.AddLiteral("RefreshTokenRotation");
                    @enum.AddLiteral("ApiKeyRegeneration");
                    @enum.AddLiteral("FirstAdministratorBootstrap");
                    @enum.AddLiteral("IdempotentProvisioning");
                    @enum.AddLiteral("IntegrationEventDeduplication");
                    @enum.AddLiteral("CredentialCleanup");
                })
                .AddEnum("SecurityAuthorityTransactionParticipation", @enum =>
                {
                    @enum.AddLiteral("AuthorityTransaction");
                    @enum.AddLiteral("JoinedHostTransaction");
                    @enum.AddLiteral("TransactionsUnavailable");
                })
                .AddClass("SecurityAuthorityPersistenceCapabilities", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("bool", "AuthorityRecordIsolation", property => property.WithoutSetter());
                    @class.AddProperty("bool", "Uniqueness", property => property.WithoutSetter());
                    @class.AddProperty("bool", "OptimisticConcurrency", property => property.WithoutSetter());
                    @class.AddProperty("bool", "AtomicCredentialRotation", property => property.WithoutSetter());
                    @class.AddProperty("bool", "OneTimeRedemption", property => property.WithoutSetter());
                    @class.AddProperty("bool", "Transactions", property => property.WithoutSetter());
                    @class.AddProperty("bool", "HostTransactionParticipation", property => property.WithoutSetter());
                    @class.AddProperty("bool", "MeetsRequiredGuarantees", property =>
                    {
                        property.Getter.WithExpressionImplementation("AuthorityRecordIsolation && Uniqueness && OptimisticConcurrency && AtomicCredentialRotation && OneTimeRedemption && (!Transactions || HostTransactionParticipation)");
                        property.WithoutSetter();
                    });
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("bool", "authorityRecordIsolation");
                        ctor.AddParameter("bool", "uniqueness");
                        ctor.AddParameter("bool", "optimisticConcurrency");
                        ctor.AddParameter("bool", "atomicCredentialRotation");
                        ctor.AddParameter("bool", "oneTimeRedemption");
                        ctor.AddParameter("bool", "transactions");
                        ctor.AddParameter("bool", "hostTransactionParticipation");
                        ctor.AddStatement("AuthorityRecordIsolation = authorityRecordIsolation;");
                        ctor.AddStatement("Uniqueness = uniqueness;");
                        ctor.AddStatement("OptimisticConcurrency = optimisticConcurrency;");
                        ctor.AddStatement("AtomicCredentialRotation = atomicCredentialRotation;");
                        ctor.AddStatement("OneTimeRedemption = oneTimeRedemption;");
                        ctor.AddStatement("Transactions = transactions;");
                        ctor.AddStatement("HostTransactionParticipation = hostTransactionParticipation;");
                    });
                })
                .AddRecord("SecurityAuthorityTenantResource", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "TenantId");
                        ctor.AddParameter("string", "TenantResourceId");
                        ctor.AddParameter("string", "ResourceKind");
                        ctor.AddParameter("string?", "ParentTenantResourceId");
                        ctor.AddParameter("bool", "InheritanceProtected");
                    });
                })
                .AddInterface("ISecurityAuthorityTenantAdapter", @interface =>
                {
                    @interface.AddMethod("ValueTask<SecurityAuthorityTenantResource?>", "ResolveAsync", method =>
                    {
                        method.AddParameter("string", "tenantId");
                        method.AddParameter("string", "tenantResourceId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddInterface("ISecurityAuthorityRecordStore", @interface =>
                {
                    @interface.AddMethod("ValueTask<object?>", "LoadAsync", method =>
                    {
                        method.AddParameter("Type", "recordType");
                        method.AddParameter("string", "recordId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask", "AddAsync", method =>
                    {
                        method.AddParameter("object", "record");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask", "UpdateAsync", method =>
                    {
                        method.AddParameter("object", "record");
                        method.AddParameter("string", "expectedConcurrencyToken");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask", "DeleteAsync", method =>
                    {
                        method.AddParameter("Type", "recordType");
                        method.AddParameter("string", "recordId");
                        method.AddParameter("string", "expectedConcurrencyToken");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddClass("SecurityAuthorityCommitReceipt", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("Guid", "OperationId", property => property.WithoutSetter());
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("Guid", "operationId");
                        ctor.AddStatement("OperationId = operationId;");
                    });
                })
                .AddEnum("SecurityAuthorityLifecycleTransition", @enum =>
                {
                    @enum.AddLiteral("Created");
                    @enum.AddLiteral("Updated");
                    @enum.AddLiteral("Activated");
                    @enum.AddLiteral("Deactivated");
                    @enum.AddLiteral("Suspended");
                    @enum.AddLiteral("Archived");
                    @enum.AddLiteral("Deleted");
                    @enum.AddLiteral("Revoked");
                    @enum.AddLiteral("Regenerated");
                    @enum.AddLiteral("Approved");
                    @enum.AddLiteral("Denied");
                    @enum.AddLiteral("Redeemed");
                    @enum.AddLiteral("BootstrapCompleted");
                })
                .AddEnum("SecurityAuthorityDeliveryOutcome", @enum =>
                {
                    @enum.AddLiteral("NotConfigured");
                    @enum.AddLiteral("Delivered");
                    @enum.AddLiteral("Failed");
                })
                .AddRecord("SecurityAuthorityPrincipalReference", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "PrincipalType");
                        ctor.AddParameter("string", "PrincipalId");
                    });
                })
                .AddRecord("SecurityAuthorityLifecycleNotification", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityLifecycleTransition", "Transition");
                        ctor.AddParameter("string", "TargetType");
                        ctor.AddParameter("string", "TargetId");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("string", "CorrelationId");
                        ctor.AddParameter("DateTimeOffset", "OccurredAt");
                        ctor.AddParameter("string", "Outcome");
                        ctor.AddParameter("IReadOnlyList<string>", "ChangedFields");
                    });
                })
                .AddRecord("SecurityAuthorityAuditEntry", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityPrincipalReference", "Actor");
                        ctor.AddParameter("string", "Action");
                        ctor.AddParameter("string", "TargetType");
                        ctor.AddParameter("string", "TargetId");
                        ctor.AddParameter("string?", "TenantId");
                        ctor.AddParameter("string", "CorrelationId");
                        ctor.AddParameter("DateTimeOffset", "OccurredAt");
                        ctor.AddParameter("string", "Outcome");
                        ctor.AddParameter("IReadOnlyList<string>", "ChangedFields");
                    });
                })
                .AddRecord("SecurityAuthorityPostCommitDeliveryFailure", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Adapter");
                        ctor.AddParameter("Guid", "OperationId");
                        ctor.AddParameter("string", "Operation");
                        ctor.AddParameter("string", "TargetType");
                        ctor.AddParameter("string", "TargetId");
                        ctor.AddParameter("string", "CorrelationId");
                        ctor.AddParameter("DateTimeOffset", "FailedAt");
                        ctor.AddParameter("Exception", "Exception");
                    });
                })
                .AddInterface("ISecurityAuthorityLifecycleNotificationAdapter", @interface =>
                {
                    @interface.AddMethod("ValueTask", "DeliverAsync", method =>
                    {
                        method.AddParameter("SecurityAuthorityLifecycleNotification", "notification");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddInterface("ISecurityAuthorityAuditAdapter", @interface =>
                {
                    @interface.AddMethod("ValueTask", "SubmitAsync", method =>
                    {
                        method.AddParameter("SecurityAuthorityAuditEntry", "auditEntry");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddInterface("ISecurityAuthorityPostCommitFailureHandler", @interface =>
                {
                    @interface.AddMethod("ValueTask", "HandleAsync", method =>
                    {
                        method.AddParameter("SecurityAuthorityPostCommitDeliveryFailure", "failure");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddClass("SecurityAuthorityDeferredCredential", @class =>
                {
                    @class.Sealed();
                    @class.AddField("Func<string>", "_reveal", field => field.PrivateReadOnly());
                    @class.AddField("int", "_revealed");
                    @class.AddProperty("Guid", "OperationId", property => property.WithoutSetter());
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("Guid", "operationId");
                        ctor.AddParameter("Func<string>", "reveal");
                        ctor.AddStatement("OperationId = operationId;");
                        ctor.AddStatement("_reveal = reveal ?? throw new ArgumentNullException(nameof(reveal));");
                    });
                    @class.AddMethod("string", "Reveal", method =>
                    {
                        method.AddParameter("SecurityAuthorityCommitReceipt", "receipt");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(receipt);");
                        method.AddStatement("if (receipt.OperationId != OperationId) throw new InvalidOperationException(\"A credential can only be revealed after its atomic operation commits.\");");
                        method.AddStatement("if (Interlocked.Exchange(ref _revealed, 1) != 0) throw new InvalidOperationException(\"A committed credential can only be revealed once.\");");
                        method.AddStatement("return _reveal();");
                    });
                })
                .AddInterface("ISecurityAuthorityAtomicOperation", @interface =>
                {
                    @interface.AddProperty("Guid", "OperationId", property => property.WithoutSetter());
                    @interface.AddProperty("ISecurityAuthorityRecordStore", "Records", property => property.WithoutSetter());
                    @interface.AddProperty("SecurityAuthorityTransactionParticipation", "TransactionParticipation", property => property.WithoutSetter());
                    @interface.AddMethod("ValueTask<SecurityAuthorityCommitReceipt>", "CommitAsync", method =>
                    {
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask", "RollbackAsync", method =>
                    {
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                    @interface.AddMethod("ValueTask", "DisposeAsync");
                })
                .AddInterface("ISecurityAuthorityPersistence", @interface =>
                {
                    @interface.AddProperty("SecurityAuthorityPersistenceCapabilities", "Capabilities", property => property.WithoutSetter());
                    @interface.AddMethod("ValueTask<ISecurityAuthorityAtomicOperation>", "BeginAtomicOperationAsync", method =>
                    {
                        method.AddParameter("SecurityAuthorityAtomicOperationKind", "operationKind");
                        method.AddParameter("bool", "joinHostTransactionWhenAvailable");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddInterface("ISecurityAuthorityCleanupCandidateQuery", @interface =>
                {
                    @interface.AddMethod("ValueTask<IReadOnlyList<SecurityAuthorityCleanupCandidate>>", "QueryAsync", method =>
                    {
                        method.AddParameter("string", "credentialCategory");
                        method.AddParameter("DateTimeOffset", "cutoff");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddRecord("SecurityAuthorityCleanupResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("int", "Examined");
                        ctor.AddParameter("int", "Removed");
                        ctor.AddParameter("int", "ConcurrencyLost");
                        ctor.AddParameter("int", "Retained");
                    });
                })
                .AddInterface("ISecurityAuthorityCleanupTrigger", @interface =>
                {
                    @interface.AddMethod("ValueTask<SecurityAuthorityCleanupResult>", "RunAsync", method =>
                    {
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddRecord("SecurityAuthorityIdentityProviderProjection", record =>
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
                        ctor.AddParameter("string", "RequestedScopes");
                        ctor.AddParameter("bool", "IsActive");
                        ctor.AddParameter("int", "DisplayPriority");
                        ctor.AddParameter("string?", "TenantResourceId");
                        ctor.AddParameter("string", "AccessMode");
                        ctor.AddParameter("DateTimeOffset", "CreatedAt");
                        ctor.AddParameter("DateTimeOffset", "UpdatedAt");
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
