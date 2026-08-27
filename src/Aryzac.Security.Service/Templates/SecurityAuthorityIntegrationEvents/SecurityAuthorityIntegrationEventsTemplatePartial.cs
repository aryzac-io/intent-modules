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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityIntegrationEvents
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityIntegrationEventsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityIntegrationEvents";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityIntegrationEventsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Reflection")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityAuthorizationEngine")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityContracts")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityPostCommitDispatch")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityRecords")
                .AddRecord("SecurityAuthorityIntegrationEventEnvelope", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "EventId");
                        ctor.AddParameter("string", "EventType");
                        ctor.AddParameter("int", "SchemaVersion");
                        ctor.AddParameter("DateTimeOffset", "OccurredAt");
                        ctor.AddParameter("string", "CorrelationId");
                        ctor.AddParameter("string", "Source");
                        ctor.AddParameter("object", "Payload");
                        ctor.AddParameter("string?", "ExpectedConcurrencyToken");
                        ctor.AddParameter("string?", "TenantId");
                    });
                })
                .AddRecord("SecurityAuthorityIntegrationEventResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "EventId");
                        ctor.AddParameter("bool", "Replay");
                        ctor.AddParameter("string", "OutcomeReference");
                    });
                })
                .AddClass("SecurityAuthorityIntegrationEventRejectedException", @class =>
                {
                    @class.Sealed();
                    @class.ExtendsClass("Exception");
                    @class.AddProperty("string", "EventId", property => property.WithoutSetter());
                    @class.AddProperty("string", "Reason", property => property.WithoutSetter());
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "eventId");
                        ctor.AddParameter("string", "reason");
                        ctor.CallsBase(call => call.AddArgument("$\"Integration Event '{eventId}' was rejected: {reason}\""));
                        ctor.AddStatement("EventId = eventId;");
                        ctor.AddStatement("Reason = reason;");
                    });
                })
                .AddClass("SecurityAuthorityIntegrationEventHandler", @class =>
                {
                    @class.Sealed();
                    @class.AddMethod("IReadOnlyDictionary<string, Type>", "GetSupportedEventTypes", method =>
                    {
                        method.Private().Static();
                        method.AddStatement("return new Dictionary<string, Type>(StringComparer.Ordinal) { [\"users\"] = typeof(SecurityAuthorityUser), [\"services\"] = typeof(SecurityAuthorityService), [\"oauth-clients\"] = typeof(SecurityAuthorityOAuthClient), [\"identity-providers\"] = typeof(SecurityAuthorityIdentityProvider), [\"tenant-resources\"] = typeof(SecurityAuthorityTenantResourceRecord), [\"roles\"] = typeof(SecurityAuthorityRole), [\"role-memberships\"] = typeof(SecurityAuthorityRoleMembership), [\"grants\"] = typeof(SecurityAuthorityGrant) };");
                    });
                    @class.AddMethod("IReadOnlySet<string>", "GetSupportedTransitions", method =>
                    {
                        method.Private().Static();
                        method.AddStatement("return new HashSet<string>(new[] { \"created\", \"updated\", \"activated\", \"deactivated\", \"suspended\", \"archived\", \"deleted\", \"revoked\", \"approved\", \"denied\" }, StringComparer.Ordinal);");
                    });
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("ISecurityAuthorityPersistence", "persistence", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityAuthorizationInvalidator", "authorizationInvalidator", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("SecurityAuthorityPostCommitDispatch", "postCommitDispatch", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<DateTimeOffset>", "utcNow", param => param.IntroduceReadonlyField());
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(authorizationInvalidator);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(postCommitDispatch);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityIntegrationEventResult>", "HandleAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("SecurityAuthorityIntegrationEventEnvelope", "envelope");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ValidateEnvelope(envelope);
                            var eventShape = ResolveEventShape(envelope);
                            await using var operation = await _persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IntegrationEventDeduplication, true, cancellationToken);
                            try
                            {
                            var processed = (SecurityAuthorityProcessedIntegrationEvent?)await operation.Records.LoadAsync(typeof(SecurityAuthorityProcessedIntegrationEvent), envelope.EventId, cancellationToken);
                            if (processed is not null)
                            {
                            await operation.RollbackAsync(cancellationToken);
                            return new SecurityAuthorityIntegrationEventResult(envelope.EventId, true, processed.OutcomeReference);
                            }

                            await ValidateReferencesAsync(operation.Records, envelope, eventShape.Resource, cancellationToken);
                            var outcomeReference = StableIdentifier(envelope.Payload);
                            if (string.Equals(eventShape.Transition, "created", StringComparison.Ordinal))
                            {
                            if (await operation.Records.LoadAsync(eventShape.RecordType, outcomeReference, cancellationToken) is not null)
                            {
                            Reject(envelope.EventId, "record-already-exists");
                            }
                            await operation.Records.AddAsync(envelope.Payload, cancellationToken);
                            }
                            else
                            {
                            var existing = await operation.Records.LoadAsync(eventShape.RecordType, outcomeReference, cancellationToken);
                            if (existing is null) Reject(envelope.EventId, "unknown-reference");
                            var currentToken = RequiredString(existing!, "ConcurrencyToken", envelope.EventId);
                            if (string.IsNullOrWhiteSpace(envelope.ExpectedConcurrencyToken)) Reject(envelope.EventId, "missing-field:expectedConcurrencyToken");
                            if (!string.Equals(currentToken, envelope.ExpectedConcurrencyToken, StringComparison.Ordinal)) Reject(envelope.EventId, "stale-concurrency-token");
                            await operation.Records.UpdateAsync(envelope.Payload, envelope.ExpectedConcurrencyToken!, cancellationToken);
                            }

                            await operation.Records.AddAsync(new SecurityAuthorityProcessedIntegrationEvent(envelope.EventId, envelope.EventType, envelope.SchemaVersion, _utcNow().ToUniversalTime(), outcomeReference), cancellationToken);
                            var receipt = await operation.CommitAsync(cancellationToken);
                            InvalidateAuthorization(eventShape.Resource, envelope.TenantId, outcomeReference);
                            await _postCommitDispatch.DispatchAsync(
                            receipt,
                            new SecurityAuthorityPrincipalReference("IntegrationEvent", envelope.Source),
                            ToLifecycleTransition(eventShape.Transition),
                            eventShape.Resource,
                            outcomeReference,
                            envelope.TenantId,
                            envelope.CorrelationId,
                            eventShape.Transition,
                            ChangedFields(envelope.Payload),
                            cancellationToken);
                            return new SecurityAuthorityIntegrationEventResult(envelope.EventId, false, outcomeReference);
                            }
                            catch
                            {
                            await operation.RollbackAsync(cancellationToken);
                            throw;
                            }
                            """);
                    });
                    @class.AddMethod("void", "ValidateEnvelope", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("SecurityAuthorityIntegrationEventEnvelope", "envelope");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(envelope);
                            if (!Guid.TryParse(envelope.EventId, out _)) Reject(envelope.EventId, "missing-or-invalid-field:eventId");
                            if (string.IsNullOrWhiteSpace(envelope.EventType)) Reject(envelope.EventId, "missing-field:eventType");
                            if (envelope.SchemaVersion != 1) Reject(envelope.EventId, $"unsupported-schema-version:{envelope.SchemaVersion}");
                            if (envelope.OccurredAt.Offset != TimeSpan.Zero) Reject(envelope.EventId, "occurrence-time-must-be-utc");
                            if (string.IsNullOrWhiteSpace(envelope.CorrelationId)) Reject(envelope.EventId, "missing-field:correlationId");
                            if (string.IsNullOrWhiteSpace(envelope.Source)) Reject(envelope.EventId, "missing-field:source");
                            if (envelope.Payload is null) Reject(envelope.EventId, "missing-field:payload");
                            """);
                    });
                    @class.AddMethod("(string Resource, string Transition, Type RecordType)", "ResolveEventShape", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("SecurityAuthorityIntegrationEventEnvelope", "envelope");
                        method.AddStatement("""
                            var separator = envelope.EventType.LastIndexOf('.', StringComparison.Ordinal);
                            if (separator <= 0 || separator == envelope.EventType.Length - 1) Reject(envelope.EventId, "unsupported-event-type");
                            var resource = envelope.EventType[..separator];
                            var transition = envelope.EventType[(separator + 1)..];
                            if (!GetSupportedEventTypes().TryGetValue(resource, out var recordType) || !GetSupportedTransitions().Contains(transition)) Reject(envelope.EventId, "unsupported-event-type");
                            if (envelope.Payload.GetType() != recordType) Reject(envelope.EventId, "payload-type-mismatch");
                            return (resource, transition, recordType);
                            """);
                    });
                    @class.AddMethod("ValueTask", "ValidateReferencesAsync", method =>
                    {
                        method.Private().Static().Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("SecurityAuthorityIntegrationEventEnvelope", "envelope");
                        method.AddParameter("string", "resource");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ValidateTenant(envelope);
                            var payload = envelope.Payload;
                            if (string.Equals(resource, "oauth-clients", StringComparison.Ordinal))
                            {
                            var providerId = OptionalString(payload, "PreferredIdentityProviderId");
                            if (providerId is not null) await RequireReferenceAsync(records, typeof(SecurityAuthorityIdentityProvider), providerId, envelope.TenantId, envelope.EventId, cancellationToken);
                            }
                            else if (string.Equals(resource, "identity-providers", StringComparison.Ordinal))
                            {
                            var tenantResourceId = OptionalString(payload, "TenantResourceId");
                            if (tenantResourceId is not null) await RequireReferenceAsync(records, typeof(SecurityAuthorityTenantResourceRecord), tenantResourceId, envelope.TenantId, envelope.EventId, cancellationToken);
                            }
                            else if (string.Equals(resource, "tenant-resources", StringComparison.Ordinal))
                            {
                            var parentId = OptionalString(payload, "ParentTenantResourceId");
                            if (parentId is not null) await RequireReferenceAsync(records, typeof(SecurityAuthorityTenantResourceRecord), parentId, RequiredString(payload, "TenantId", envelope.EventId), envelope.EventId, cancellationToken);
                            }
                            else if (string.Equals(resource, "roles", StringComparison.Ordinal))
                            {
                            await RequireReferenceAsync(records, typeof(SecurityAuthorityTenantResourceRecord), RequiredString(payload, "DefinitionTenantResourceId", envelope.EventId), envelope.TenantId, envelope.EventId, cancellationToken);
                            }
                            else if (string.Equals(resource, "role-memberships", StringComparison.Ordinal))
                            {
                            var role = await RequireReferenceAsync(records, typeof(SecurityAuthorityRole), RequiredString(payload, "RoleId", envelope.EventId), envelope.TenantId, envelope.EventId, cancellationToken);
                            var userId = OptionalString(payload, "UserId");
                            var serviceId = OptionalString(payload, "ServiceId");
                            if ((userId is null) == (serviceId is null)) Reject(envelope.EventId, "missing-field:principal");
                            if (userId is not null) await RequireReferenceAsync(records, typeof(SecurityAuthorityUser), userId, TenantOf(role), envelope.EventId, cancellationToken);
                            if (serviceId is not null) await RequireReferenceAsync(records, typeof(SecurityAuthorityService), serviceId, TenantOf(role), envelope.EventId, cancellationToken);
                            }
                            else if (string.Equals(resource, "grants", StringComparison.Ordinal))
                            {
                            await RequireReferenceAsync(records, typeof(SecurityAuthorityTenantResourceRecord), RequiredString(payload, "TenantResourceId", envelope.EventId), envelope.TenantId, envelope.EventId, cancellationToken);
                            var principalType = RequiredString(payload, "PrincipalType", envelope.EventId);
                            var principalRecordType = principalType switch
                            {
                            "User" => typeof(SecurityAuthorityUser),
                            "Service" => typeof(SecurityAuthorityService),
                            "Role" => typeof(SecurityAuthorityRole),
                            "ApiKey" => typeof(SecurityAuthorityApiKey),
                            _ => null
                            };
                            if (principalRecordType is null) Reject(envelope.EventId, "unknown-reference");
                            await RequireReferenceAsync(records, principalRecordType!, RequiredString(payload, "PrincipalId", envelope.EventId), envelope.TenantId, envelope.EventId, cancellationToken);
                            }
                            """);
                    });
                    @class.AddMethod("ValueTask<object>", "RequireReferenceAsync", method =>
                    {
                        method.Private().Static().Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("Type", "recordType");
                        method.AddParameter("string", "recordId");
                        method.AddParameter("string?", "expectedTenantId");
                        method.AddParameter("string", "eventId");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            var record = await records.LoadAsync(recordType, recordId, cancellationToken);
                            if (record is null) Reject(eventId, "unknown-reference");
                            var actualTenantId = TenantOf(record!);
                            if (expectedTenantId is not null && actualTenantId is not null && !string.Equals(expectedTenantId, actualTenantId, StringComparison.Ordinal)) Reject(eventId, "tenant-mismatch");
                            return record!;
                            """);
                    });
                    @class.AddMethod("void", "ValidateTenant", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("SecurityAuthorityIntegrationEventEnvelope", "envelope");
                        method.AddStatement("""
                            var payloadTenantId = TenantOf(envelope.Payload);
                            if (envelope.TenantId is not null && payloadTenantId is not null && !string.Equals(envelope.TenantId, payloadTenantId, StringComparison.Ordinal)) Reject(envelope.EventId, "tenant-mismatch");
                            """);
                    });
                    @class.AddMethod("string", "StableIdentifier", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("object", "payload");
                        method.AddStatement("""
                            foreach (var propertyName in new[] { "Id", "TenantResourceId" })
                            {
                            var value = OptionalString(payload, propertyName);
                            if (value is not null) return value;
                            }
                            throw new InvalidOperationException("An Integration Event payload requires a stable identifier.");
                            """);
                    });
                    @class.AddMethod("string", "RequiredString", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("object", "instance");
                        method.AddParameter("string", "propertyName");
                        method.AddParameter("string", "eventId");
                        method.AddStatement("return OptionalString(instance, propertyName) ?? throw new SecurityAuthorityIntegrationEventRejectedException(eventId, $\"missing-field:{propertyName}\");");
                    });
                    @class.AddMethod("string?", "OptionalString", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("object", "instance");
                        method.AddParameter("string", "propertyName");
                        method.AddStatement("var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);");
                        method.AddStatement("return property?.GetValue(instance) is string value && !string.IsNullOrWhiteSpace(value) ? value : null;");
                    });
                    @class.AddMethod("string?", "TenantOf", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("object", "instance");
                        method.AddStatement("return OptionalString(instance, \"TenantId\");");
                    });
                    @class.AddMethod("void", "InvalidateAuthorization", method =>
                    {
                        method.Private();
                        method.AddParameter("string", "resource");
                        method.AddParameter("string?", "tenantId");
                        method.AddParameter("string", "recordId");
                        method.AddStatement("var change = resource switch { \"users\" => SecurityAuthorityAuthorizationChange.User, \"services\" => SecurityAuthorityAuthorizationChange.Service, \"tenant-resources\" => SecurityAuthorityAuthorizationChange.TenantResourceParent, \"roles\" => SecurityAuthorityAuthorizationChange.Role, \"role-memberships\" => SecurityAuthorityAuthorizationChange.RoleMembership, \"grants\" => SecurityAuthorityAuthorizationChange.Grant, _ => (SecurityAuthorityAuthorizationChange?)null };");
                        method.AddStatement("if (change is not null) _authorizationInvalidator.Invalidate(change.Value, tenantId ?? \"__global__\", recordId);");
                    });
                    @class.AddMethod("SecurityAuthorityLifecycleTransition", "ToLifecycleTransition", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("string", "transition");
                        method.AddStatement("return transition switch { \"created\" => SecurityAuthorityLifecycleTransition.Created, \"updated\" => SecurityAuthorityLifecycleTransition.Updated, \"activated\" => SecurityAuthorityLifecycleTransition.Activated, \"deactivated\" => SecurityAuthorityLifecycleTransition.Deactivated, \"suspended\" => SecurityAuthorityLifecycleTransition.Suspended, \"archived\" => SecurityAuthorityLifecycleTransition.Archived, \"deleted\" => SecurityAuthorityLifecycleTransition.Deleted, \"revoked\" => SecurityAuthorityLifecycleTransition.Revoked, \"approved\" => SecurityAuthorityLifecycleTransition.Approved, \"denied\" => SecurityAuthorityLifecycleTransition.Denied, _ => throw new InvalidOperationException(\"Unsupported lifecycle transition.\") };");
                    });
                    @class.AddMethod("IReadOnlyList<string>", "ChangedFields", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("object", "payload");
                        method.AddStatement("return payload.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name).ToArray();");
                    });
                    @class.AddMethod("void", "Reject", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("string", "eventId");
                        method.AddParameter("string", "reason");
                        method.AddStatement("throw new SecurityAuthorityIntegrationEventRejectedException(eventId, reason);");
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
