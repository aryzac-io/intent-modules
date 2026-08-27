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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityPostCommitDispatch
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityPostCommitDispatchTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityPostCommitDispatch";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityPostCommitDispatchTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityContracts")
                .AddRecord("SecurityAuthorityPostCommitDispatchResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("Guid", "OperationId");
                        ctor.AddParameter("SecurityAuthorityDeliveryOutcome", "LifecycleNotification");
                        ctor.AddParameter("SecurityAuthorityDeliveryOutcome", "Audit");
                    });
                })
                .AddClass("SecurityAuthorityPostCommitDispatch", @class =>
                {
                    @class.Sealed();
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("ISecurityAuthorityLifecycleNotificationAdapter?", "lifecycleNotificationAdapter", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityAuditAdapter?", "auditAdapter", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityPostCommitFailureHandler", "failureHandler", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<DateTimeOffset>", "utcNow", param => param.IntroduceReadonlyField());
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(failureHandler);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityPostCommitDispatchResult>", "DispatchAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("SecurityAuthorityCommitReceipt", "commitReceipt");
                        method.AddParameter("SecurityAuthorityPrincipalReference", "actor");
                        method.AddParameter("SecurityAuthorityLifecycleTransition", "transition");
                        method.AddParameter("string", "action");
                        method.AddParameter("string", "targetType");
                        method.AddParameter("string", "targetId");
                        method.AddParameter("string?", "tenantId");
                        method.AddParameter("string", "correlationId");
                        method.AddParameter("string", "outcome");
                        method.AddParameter("IReadOnlyList<string>", "changedFields");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(commitReceipt);
                            ArgumentNullException.ThrowIfNull(actor);
                            if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("An audit action is required.", nameof(action));
                            if (string.IsNullOrWhiteSpace(targetType)) throw new ArgumentException("A target type is required.", nameof(targetType));
                            if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("A stable target identifier is required.", nameof(targetId));
                            if (string.IsNullOrWhiteSpace(correlationId)) throw new ArgumentException("A correlation identifier is required.", nameof(correlationId));
                            if (string.IsNullOrWhiteSpace(outcome)) throw new ArgumentException("An outcome is required.", nameof(outcome));

                            var occurredAt = _utcNow().ToUniversalTime();
                            var safeChangedFields = SanitizeChangedFields(changedFields);
                            var lifecycleOutcome = SecurityAuthorityDeliveryOutcome.NotConfigured;
                            var auditOutcome = SecurityAuthorityDeliveryOutcome.NotConfigured;

                            if (_lifecycleNotificationAdapter is not null)
                            {
                            var notification = new SecurityAuthorityLifecycleNotification(transition, targetType, targetId, tenantId, correlationId, occurredAt, outcome, safeChangedFields);
                            try
                            {
                            await _lifecycleNotificationAdapter.DeliverAsync(notification, cancellationToken);
                            lifecycleOutcome = SecurityAuthorityDeliveryOutcome.Delivered;
                            }
                            catch (Exception exception)
                            {
                            lifecycleOutcome = SecurityAuthorityDeliveryOutcome.Failed;
                            await ReportFailureAsync("LifecycleNotification", commitReceipt.OperationId, transition.ToString(), targetType, targetId, correlationId, occurredAt, exception, cancellationToken);
                            }
                            }

                            if (_auditAdapter is not null)
                            {
                            var auditEntry = new SecurityAuthorityAuditEntry(actor, action, targetType, targetId, tenantId, correlationId, occurredAt, outcome, safeChangedFields);
                            try
                            {
                            await _auditAdapter.SubmitAsync(auditEntry, cancellationToken);
                            auditOutcome = SecurityAuthorityDeliveryOutcome.Delivered;
                            }
                            catch (Exception exception)
                            {
                            auditOutcome = SecurityAuthorityDeliveryOutcome.Failed;
                            await ReportFailureAsync("Audit", commitReceipt.OperationId, action, targetType, targetId, correlationId, occurredAt, exception, cancellationToken);
                            }
                            }

                            return new SecurityAuthorityPostCommitDispatchResult(commitReceipt.OperationId, lifecycleOutcome, auditOutcome);
                            """);
                    });
                    @class.AddMethod("ValueTask", "ReportFailureAsync", method =>
                    {
                        method.Private().Async();
                        method.AddParameter("string", "adapter");
                        method.AddParameter("Guid", "operationId");
                        method.AddParameter("string", "operation");
                        method.AddParameter("string", "targetType");
                        method.AddParameter("string", "targetId");
                        method.AddParameter("string", "correlationId");
                        method.AddParameter("DateTimeOffset", "failedAt");
                        method.AddParameter("Exception", "exception");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("await _failureHandler.HandleAsync(new SecurityAuthorityPostCommitDeliveryFailure(adapter, operationId, operation, targetType, targetId, correlationId, failedAt, exception), cancellationToken);");
                    });
                    @class.AddMethod("IReadOnlyList<string>", "SanitizeChangedFields", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("IReadOnlyList<string>", "changedFields");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(changedFields);
                            return changedFields
                            .Where(field => !string.IsNullOrWhiteSpace(field))
                            .Where(field => !IsSecretField(field))
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(field => field, StringComparer.Ordinal)
                            .ToArray();
                            """);
                    });
                    @class.AddMethod("bool", "IsSecretField", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("string", "fieldName");
                        method.AddStatement("""
                            var normalized = fieldName.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
                            return normalized.Contains("secret", StringComparison.Ordinal)
                            || normalized.Contains("password", StringComparison.Ordinal)
                            || normalized.Contains("hash", StringComparison.Ordinal)
                            || normalized.Contains("privatekey", StringComparison.Ordinal)
                            || normalized.Contains("credential", StringComparison.Ordinal)
                            || normalized.Contains("correlationstate", StringComparison.Ordinal)
                            || normalized.Contains("clearapikey", StringComparison.Ordinal);
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
