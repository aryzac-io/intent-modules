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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityBootstrap
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityBootstrapTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityBootstrap";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityBootstrapTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityPostCommitDispatch")
                .AddEnum("SecurityAuthorityBootstrapOutcome", @enum =>
                {
                    @enum.AddLiteral("Succeeded");
                    @enum.AddLiteral("NotEligible");
                    @enum.AddLiteral("Conflict");
                    @enum.AddLiteral("Closed");
                    @enum.AddLiteral("NotApplicable");
                })
                .AddRecord("SecurityAuthorityExplicitIdentityBootstrap", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string?", "Issuer");
                        ctor.AddParameter("string?", "Subject");
                        ctor.AddParameter("string?", "NormalizedEmail");
                    });
                })
                .AddRecord("SecurityAuthorityBootstrapIdentity", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "UserId");
                        ctor.AddParameter("string?", "Issuer");
                        ctor.AddParameter("string?", "Subject");
                        ctor.AddParameter("string", "NormalizedEmail");
                    });
                })
                .AddRecord("SecurityAuthorityBootstrapSeed", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityUser", "User");
                        ctor.AddParameter("IReadOnlyList<SecurityAuthorityGrant>", "Grants");
                    });
                })
                .AddRecord("SecurityAuthorityBootstrapResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityBootstrapOutcome", "Outcome");
                        ctor.AddParameter("string?", "AdministratorUserId");
                    });
                })
                .AddRecord("SecurityAuthorityBootstrapResetRequest", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "ExpectedConcurrencyToken");
                    });
                })
                .AddClass("SecurityAuthorityBootstrapOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("SecurityAuthorityExplicitIdentityBootstrap?", "ExplicitIdentity", property => property.WithoutSetter());
                    @class.AddProperty("bool", "FirstEligibleUser", property => property.WithoutSetter());
                    @class.AddProperty("Func<CancellationToken, ValueTask<SecurityAuthorityBootstrapSeed?>>?", "CustomSeedFunction", property => property.WithoutSetter());
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityExplicitIdentityBootstrap?", "explicitIdentity");
                        ctor.AddParameter("bool", "firstEligibleUser");
                        ctor.AddParameter("Func<CancellationToken, ValueTask<SecurityAuthorityBootstrapSeed?>>?", "customSeedFunction");
                        ctor.AddStatement("ExplicitIdentity = explicitIdentity;");
                        ctor.AddStatement("FirstEligibleUser = firstEligibleUser;");
                        ctor.AddStatement("CustomSeedFunction = customSeedFunction;");
                        ctor.AddStatement("Validate();");
                    });
                    @class.AddMethod("void", "Validate", method =>
                    {
                        method.Private();
                        method.AddStatement("var configuredStrategies = (ExplicitIdentity is null ? 0 : 1) + (FirstEligibleUser ? 1 : 0) + (CustomSeedFunction is null ? 0 : 1);");
                        method.AddStatement("if (configuredStrategies != 1) throw new InvalidOperationException(\"Exactly one Security Authority bootstrap strategy must be configured.\");");
                        method.AddStatement("if (ExplicitIdentity is null) return;");
                        method.AddStatement("var hasIssuer = !string.IsNullOrWhiteSpace(ExplicitIdentity.Issuer);");
                        method.AddStatement("var hasSubject = !string.IsNullOrWhiteSpace(ExplicitIdentity.Subject);");
                        method.AddStatement("var hasEmail = !string.IsNullOrWhiteSpace(ExplicitIdentity.NormalizedEmail);");
                        method.AddStatement("if (hasIssuer != hasSubject) throw new InvalidOperationException(\"Explicit Identity bootstrap requires both issuer and subject when either value is configured.\");");
                        method.AddStatement("if ((hasIssuer ? 1 : 0) + (hasEmail ? 1 : 0) != 1) throw new InvalidOperationException(\"Explicit Identity bootstrap requires exactly one issuer-subject pair or one normalized email address.\");");
                    });
                })
                .AddClass("SecurityAuthorityBootstrap", @class =>
                {
                    @class.Sealed();
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("SecurityAuthorityBootstrapOptions", "options", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityPersistence", "persistence", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityTenantAdapter", "tenantAdapter", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityValidationContext", "validationContext", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<SecurityAuthorityUser, CancellationToken, ValueTask<IReadOnlyList<SecurityAuthorityGrant>>>", "administratorGrantFactory", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("SecurityAuthorityPostCommitDispatch", "postCommitDispatch", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<Exception, bool>", "isConcurrencyConflict", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<CancellationToken, ValueTask<bool>>", "authorizeBootstrapReset", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<DateTimeOffset>", "utcNow", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<string>", "newConcurrencyToken", param => param.IntroduceReadonlyField());
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(options);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(tenantAdapter);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(validationContext);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(administratorGrantFactory);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(postCommitDispatch);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(isConcurrencyConflict);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(authorizeBootstrapReset);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(newConcurrencyToken);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityBootstrapResult>", "InitializeAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("if (_options.CustomSeedFunction is null) return new SecurityAuthorityBootstrapResult(SecurityAuthorityBootstrapOutcome.NotApplicable, null);");
                        method.AddStatement("var seed = await _options.CustomSeedFunction(cancellationToken) ?? throw new InvalidOperationException(\"The Security Authority custom seed function returned no bootstrap seed.\");");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(seed.User);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(seed.Grants);");
                        method.AddStatement("return await CommitAsync(seed.User.Id, null, seed, cancellationToken);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityBootstrapResult>", "TryBootstrapAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("SecurityAuthorityBootstrapIdentity", "identity");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(identity);");
                        method.AddStatement("if (_options.CustomSeedFunction is not null) return new SecurityAuthorityBootstrapResult(SecurityAuthorityBootstrapOutcome.NotApplicable, null);");
                        method.AddStatement("if (_options.ExplicitIdentity is not null && !Matches(_options.ExplicitIdentity, identity)) return new SecurityAuthorityBootstrapResult(SecurityAuthorityBootstrapOutcome.NotEligible, null);");
                        method.AddStatement("return await CommitAsync(identity.UserId, identity, null, cancellationToken);");
                    });
                    @class.AddMethod("ValueTask", "ResetAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("SecurityAuthorityBootstrapResetRequest", "request");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(request);");
                        method.AddStatement("if (!await _authorizeBootstrapReset(cancellationToken)) throw new UnauthorizedAccessException(\"Only an Application Administrator can reset Security Authority bootstrap state.\");");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(request.ExpectedConcurrencyToken)) throw new ArgumentException(\"A bootstrap state concurrency token is required.\", nameof(request));");
                        method.AddStatement("await using var operation = await _persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.FirstAdministratorBootstrap, true, cancellationToken);");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var state = await LoadStateAsync(operation.Records, cancellationToken) ?? throw new InvalidOperationException(\"Security Authority bootstrap state does not exist.\");");
                        method.AddStatement("    if (!state.IsClosed) throw new InvalidOperationException(\"Security Authority bootstrap state is already open.\");");
                        method.AddStatement("    if (!string.Equals(state.ConcurrencyToken, request.ExpectedConcurrencyToken, StringComparison.Ordinal)) throw new InvalidOperationException(\"The Security Authority bootstrap state concurrency token is stale.\");");
                        method.AddStatement("    await operation.Records.DeleteAsync(typeof(SecurityAuthorityBootstrapState), \"security-authority-bootstrap\", request.ExpectedConcurrencyToken, cancellationToken);");
                        method.AddStatement("    await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityBootstrapResult>", "CommitAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("string", "userId");
                        method.AddParameter("SecurityAuthorityBootstrapIdentity?", "identity");
                        method.AddParameter("SecurityAuthorityBootstrapSeed?", "seed");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(userId)) throw new InvalidOperationException(\"Bootstrap requires a User identifier.\");");
                        method.AddStatement("await using var operation = await _persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.FirstAdministratorBootstrap, true, cancellationToken);");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    var existingState = await LoadStateAsync(operation.Records, cancellationToken);");
                        method.AddStatement("    if (existingState?.IsClosed == true)");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        return new SecurityAuthorityBootstrapResult(SecurityAuthorityBootstrapOutcome.Closed, existingState.AdministratorUserId);");
                        method.AddStatement("    }");
                        method.AddStatement("    var storedUser = await operation.Records.LoadAsync(typeof(SecurityAuthorityUser), userId, cancellationToken) as SecurityAuthorityUser;");
                        method.AddStatement("    if (storedUser is null) throw new InvalidOperationException($\"Bootstrap User '{userId}' is unknown.\");");
                        method.AddStatement("    if (!string.Equals(storedUser.Status, \"Active\", StringComparison.Ordinal)) throw new InvalidOperationException($\"Bootstrap User '{userId}' must be Active.\");");
                        method.AddStatement("    if (_options.ExplicitIdentity?.NormalizedEmail is not null && !string.Equals(_options.ExplicitIdentity.NormalizedEmail, storedUser.NormalizedEmail, StringComparison.Ordinal)) throw new InvalidOperationException(\"The configured bootstrap email does not match the stored User.\");");
                        method.AddStatement("    if (seed is null)");
                        method.AddStatement("    {");
                        method.AddStatement("        var administratorGrants = await _administratorGrantFactory(storedUser, cancellationToken) ?? throw new InvalidOperationException(\"The administrator grant factory returned no Grants.\");");
                        method.AddStatement("        seed = new SecurityAuthorityBootstrapSeed(storedUser, administratorGrants);");
                        method.AddStatement("    }");
                        method.AddStatement("    await AddAndValidateSeedGrantsAsync(operation.Records, seed, storedUser, cancellationToken);");
                        method.AddStatement("    var now = _utcNow();");
                        method.AddStatement("    var closedState = new SecurityAuthorityBootstrapState(\"security-authority-bootstrap\", true, storedUser.Id, existingState?.CreatedAt ?? now, now, _newConcurrencyToken());");
                        method.AddStatement("    if (existingState is null) await operation.Records.AddAsync(closedState, cancellationToken);");
                        method.AddStatement("    else await operation.Records.UpdateAsync(closedState, existingState.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("    var receipt = await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("    await _postCommitDispatch.DispatchAsync(receipt, new SecurityAuthorityPrincipalReference(\"User\", storedUser.Id), SecurityAuthorityLifecycleTransition.BootstrapCompleted, \"bootstrap\", closedState.Id, null, receipt.OperationId.ToString(\"N\"), \"succeeded\", new[] { \"AdministratorUserId\", \"IsClosed\" }, cancellationToken);");
                        method.AddStatement("    return new SecurityAuthorityBootstrapResult(SecurityAuthorityBootstrapOutcome.Succeeded, storedUser.Id);");
                        method.AddStatement("}");
                        method.AddStatement("catch (Exception exception) when (_isConcurrencyConflict(exception))");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    return new SecurityAuthorityBootstrapResult(SecurityAuthorityBootstrapOutcome.Conflict, null);");
                        method.AddStatement("}");
                        method.AddStatement("catch");
                        method.AddStatement("{");
                        method.AddStatement("    await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("    throw;");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask", "AddAndValidateSeedGrantsAsync", method =>
                    {
                        method.Private();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("SecurityAuthorityBootstrapSeed", "seed");
                        method.AddParameter("SecurityAuthorityUser", "storedUser");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("if (!string.Equals(seed.User.Id, storedUser.Id, StringComparison.Ordinal)) throw new InvalidOperationException(\"The custom seed User does not match the stored User.\");");
                        method.AddStatement("if (!string.Equals(seed.User.Status, \"Active\", StringComparison.Ordinal)) throw new InvalidOperationException(\"The custom seed User must be Active.\");");
                        method.AddStatement("if (seed.Grants.Count == 0) throw new InvalidOperationException(\"The custom seed function must return at least one initial Grant.\");");
                        method.AddStatement("var duplicateGrantId = seed.Grants.GroupBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault(x => x.Count() > 1)?.Key;");
                        method.AddStatement("if (duplicateGrantId is not null) throw new InvalidOperationException($\"The custom seed function returned duplicate Grant identifier '{duplicateGrantId}'.\");");
                        method.AddStatement("foreach (var grant in seed.Grants)");
                        method.AddStatement("{");
                        method.AddStatement("    if (!string.Equals(grant.PrincipalType, \"User\", StringComparison.Ordinal) || !string.Equals(grant.PrincipalId, storedUser.Id, StringComparison.Ordinal)) throw new InvalidOperationException(\"Every custom seed Grant must target the initial administrator User.\");");
                        method.AddStatement("    if (grant.IsRevoked) throw new InvalidOperationException($\"Custom seed Grant '{grant.Id}' cannot be revoked.\");");
                        method.AddStatement("    var validation = await SecurityAuthorityValidation.ValidateGrantAsync(grant, null, grant.TenantId ?? string.Empty, _tenantAdapter, _validationContext, cancellationToken);");
                        method.AddStatement("    if (!validation.IsValid) throw new InvalidOperationException($\"Custom seed Grant '{grant.Id}' is invalid: {string.Join(\"; \", validation.Failures.Select(x => $\"{x.Field}: {x.Message}\"))}\");");
                        method.AddStatement("    await records.AddAsync(grant, cancellationToken);");
                        method.AddStatement("}");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityBootstrapState?>", "LoadStateAsync", method =>
                    {
                        method.Private();
                        method.Static();
                        method.Async();
                        method.AddParameter("ISecurityAuthorityRecordStore", "records");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("return await records.LoadAsync(typeof(SecurityAuthorityBootstrapState), \"security-authority-bootstrap\", cancellationToken) as SecurityAuthorityBootstrapState;");
                    });
                    @class.AddMethod("bool", "Matches", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityExplicitIdentityBootstrap", "configured");
                        method.AddParameter("SecurityAuthorityBootstrapIdentity", "identity");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(configured.NormalizedEmail)) return string.Equals(configured.NormalizedEmail, identity.NormalizedEmail, StringComparison.Ordinal);");
                        method.AddStatement("return string.Equals(configured.Issuer, identity.Issuer, StringComparison.Ordinal) && string.Equals(configured.Subject, identity.Subject, StringComparison.Ordinal);");
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