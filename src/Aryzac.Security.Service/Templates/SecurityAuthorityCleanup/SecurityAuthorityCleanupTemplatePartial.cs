using System;
using System.Collections.Generic;
using System.Globalization;
using Aryzac.Security.Service.Settings;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Security.Service.Templates.SecurityAuthorityCleanup
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityCleanupTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityCleanup";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityCleanupTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            static Exception Friendly(string message)
            {
                var exceptionType = Type.GetType("Intent.Exceptions.FriendlyException, Intent.SoftwareFactory.SDK");
                return exceptionType is not null && Activator.CreateInstance(exceptionType, message) is Exception exception
                    ? exception
                    : new InvalidOperationException(message);
            }

            static int ParseRange(string value, string settingName, int minimum, int maximum)
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result < minimum || result > maximum)
                {
                    throw Friendly($"{settingName} must be configured as a whole number from {minimum} through {maximum}.");
                }

                return result;
            }

            var lifecycle = ExecutionContext.Settings.GetAuthorityDataLifecycle();
            var revokedMetadataRetentionDays = ParseRange(lifecycle.RevokedMetadataRetentionDays(), "Authority Data Lifecycle: Revoked Metadata Retention Days", 1, 3650);
            var codeAndDeviceCleanupDelayDays = ParseRange(lifecycle.CodeAndDeviceCleanupDelayDays(), "Authority Data Lifecycle: Code and Device Cleanup Delay Days", 1, 7);
            var ssoAndRefreshCleanupDays = ParseRange(lifecycle.SSOAndRefreshCleanupDays(), "Authority Data Lifecycle: SSO and Refresh Cleanup Days", 30, 90);

            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityContracts")
                .AddUsing("Aryzac.Security.Service.Templates.SecurityAuthorityRecords")
                .AddUsing("Microsoft.Extensions.Hosting")
                .AddClass("SecurityAuthorityCleanup", @class =>
                {
                    @class.Sealed();
                    @class.ImplementsInterface("ISecurityAuthorityCleanupTrigger");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("ISecurityAuthorityPersistence", "persistence", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("ISecurityAuthorityCleanupCandidateQuery", "candidateQuery", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<Exception, bool>", "isConcurrencyConflict", param => param.IntroduceReadonlyField());
                        ctor.AddParameter("Func<DateTimeOffset>", "utcNow", param => param.IntroduceReadonlyField());
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(persistence);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(candidateQuery);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(isConcurrencyConflict);");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityCleanupResult>", "RunAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("var now = _utcNow().ToUniversalTime();");
                        method.AddStatement($"var revokedMetadataCutoff = now.AddDays(-{revokedMetadataRetentionDays});");
                        method.AddStatement($"var codeAndDeviceCutoff = now.AddDays(-{codeAndDeviceCleanupDelayDays});");
                        method.AddStatement($"var ssoAndRefreshCutoff = now.AddDays(-{ssoAndRefreshCleanupDays});");
                        method.AddStatement("var candidates = new List<SecurityAuthorityCleanupCandidate>();");
                        method.AddStatement("candidates.AddRange(await _candidateQuery.QueryAsync(\"AuthorizationCode\", codeAndDeviceCutoff, cancellationToken));");
                        method.AddStatement("candidates.AddRange(await _candidateQuery.QueryAsync(\"DeviceGrant\", codeAndDeviceCutoff, cancellationToken));");
                        method.AddStatement("candidates.AddRange(await _candidateQuery.QueryAsync(\"SsoSession\", ssoAndRefreshCutoff, cancellationToken));");
                        method.AddStatement("candidates.AddRange(await _candidateQuery.QueryAsync(\"RefreshToken\", ssoAndRefreshCutoff, cancellationToken));");
                        method.AddStatement("candidates.AddRange(await _candidateQuery.QueryAsync(\"RevokedCredentialMetadata\", revokedMetadataCutoff, cancellationToken));");
                        method.AddStatement("var removed = 0;");
                        method.AddStatement("var concurrencyLost = 0;");
                        method.AddStatement("var retained = 0;");
                        method.AddStatement("foreach (var candidate in candidates)");
                        method.AddStatement("{");
                        method.AddStatement("    ValidateCandidate(candidate);");
                        method.AddStatement("    if (!IsEligible(candidate, now, revokedMetadataCutoff, codeAndDeviceCutoff, ssoAndRefreshCutoff))");
                        method.AddStatement("    {");
                        method.AddStatement("        retained++;");
                        method.AddStatement("        continue;");
                        method.AddStatement("    }");
                        method.AddStatement("    await using var operation = await _persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.CredentialCleanup, true, cancellationToken);");
                        method.AddStatement("    try");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.Records.DeleteAsync(GetRecordType(candidate.CredentialCategory), candidate.CredentialId, candidate.ConcurrencyToken, cancellationToken);");
                        method.AddStatement("        await operation.CommitAsync(cancellationToken);");
                        method.AddStatement("        removed++;");
                        method.AddStatement("    }");
                        method.AddStatement("    catch (Exception exception) when (_isConcurrencyConflict(exception))");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        concurrencyLost++;");
                        method.AddStatement("    }");
                        method.AddStatement("    catch");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation.RollbackAsync(cancellationToken);");
                        method.AddStatement("        throw;");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("return new SecurityAuthorityCleanupResult(candidates.Count, removed, concurrencyLost, retained);");
                    });
                    @class.AddMethod("bool", "IsEligible", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityCleanupCandidate", "candidate");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("DateTimeOffset", "revokedMetadataCutoff");
                        method.AddParameter("DateTimeOffset", "codeAndDeviceCutoff");
                        method.AddParameter("DateTimeOffset", "ssoAndRefreshCutoff");
                        method.AddStatement("if (candidate.RetainUntil.ToUniversalTime() > now) return false;");
                        method.AddStatement("var expiresAt = candidate.ExpiresAt.ToUniversalTime();");
                        method.AddStatement("if (string.Equals(candidate.CredentialCategory, \"AuthorizationCode\", StringComparison.Ordinal) || string.Equals(candidate.CredentialCategory, \"DeviceGrant\", StringComparison.Ordinal)) return expiresAt <= codeAndDeviceCutoff;");
                        method.AddStatement("if (string.Equals(candidate.CredentialCategory, \"SsoSession\", StringComparison.Ordinal) || string.Equals(candidate.CredentialCategory, \"RefreshToken\", StringComparison.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    var lifecycleAt = candidate.TerminalAt ?? candidate.RevokedAt ?? candidate.ExpiresAt;");
                        method.AddStatement("    return lifecycleAt.ToUniversalTime() <= ssoAndRefreshCutoff;");
                        method.AddStatement("}");
                        method.AddStatement("if (string.Equals(candidate.CredentialCategory, \"RevokedCredentialMetadata\", StringComparison.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    var lifecycleAt = candidate.TerminalAt ?? candidate.RedeemedAt ?? candidate.RevokedAt ?? candidate.ExpiresAt;");
                        method.AddStatement("    return lifecycleAt.ToUniversalTime() <= revokedMetadataCutoff;");
                        method.AddStatement("}");
                        method.AddStatement("throw new InvalidOperationException(\"Unsupported cleanup credential category.\");");
                    });
                    @class.AddMethod("Type", "GetRecordType", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "credentialCategory");
                        method.AddStatement("if (string.Equals(credentialCategory, \"AuthorizationCode\", StringComparison.Ordinal)) return typeof(SecurityAuthorityAuthorizationCode);");
                        method.AddStatement("if (string.Equals(credentialCategory, \"DeviceGrant\", StringComparison.Ordinal)) return typeof(SecurityAuthorityDeviceGrant);");
                        method.AddStatement("if (string.Equals(credentialCategory, \"SsoSession\", StringComparison.Ordinal)) return typeof(SecurityAuthoritySsoSession);");
                        method.AddStatement("if (string.Equals(credentialCategory, \"RefreshToken\", StringComparison.Ordinal)) return typeof(SecurityAuthorityRefreshToken);");
                        method.AddStatement("if (string.Equals(credentialCategory, \"RevokedCredentialMetadata\", StringComparison.Ordinal)) return typeof(SecurityAuthorityRevokedCredentialMetadata);");
                        method.AddStatement("throw new InvalidOperationException(\"Unsupported cleanup credential category.\");");
                    });
                    @class.AddMethod("void", "ValidateCandidate", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityCleanupCandidate", "candidate");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(candidate);");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(candidate.CredentialId)) throw new InvalidOperationException(\"A cleanup candidate requires a stable credential identifier.\");");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(candidate.CredentialCategory)) throw new InvalidOperationException(\"A cleanup candidate requires a credential category.\");");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(candidate.ConcurrencyToken)) throw new InvalidOperationException(\"A cleanup candidate requires an optimistic concurrency token.\");");
                    });
                })
                .AddClass("SecurityAuthorityCleanupHostedService", @class =>
                {
                    @class.Sealed();
                    @class.ExtendsClass("BackgroundService");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("ISecurityAuthorityCleanupTrigger", "cleanup", param => param.IntroduceReadonlyField());
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(cleanup);");
                    });
                    @class.AddMethod("Task", "ExecuteAsync", method =>
                    {
                        method.Protected().Override().Async();
                        method.AddParameter("CancellationToken", "stoppingToken");
                        method.AddStatement("""
                            while (!stoppingToken.IsCancellationRequested)
                            {
                            await _cleanup.RunAsync(stoppingToken);
                            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
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
