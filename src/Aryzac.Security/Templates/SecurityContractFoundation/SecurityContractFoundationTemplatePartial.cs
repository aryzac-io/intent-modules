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

namespace Aryzac.Security.Templates.SecurityContractFoundation
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityContractFoundationTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.SecurityContractFoundation";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityContractFoundationTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Collections.Immutable")
                .AddUsing("System.Net.Http")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddClass("SecurityContractServiceMarker", @class =>
                {
                    @class.Sealed();
                    @class.AddConstructor(ctor => ctor.Private());
                })
                .AddClass("SecurityContractVersion", @class =>
                {
                    @class.Static();
                    @class.AddProperty("string", "Current", property =>
                    {
                        property.Static();
                        property.Getter.WithExpressionImplementation("\"1.0\"");
                        property.WithoutSetter();
                    });
                })
                .AddEnum("CallerCredentialKind", @enum =>
                {
                    @enum.AddLiteral("Jwt");
                    @enum.AddLiteral("ApiKey");
                    @enum.AddLiteral("ServiceToken");
                    @enum.AddLiteral("InternalServiceKey");
                })
                .AddRecord("CallerCredential", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("CallerCredentialKind", "Kind");
                        ctor.AddParameter("string", "Scheme");
                        ctor.AddParameter("ImmutableArray<byte>", "Value");
                    });
                })
                .AddRecord("Scope", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor => ctor.AddParameter("string", "Value"));
                    record.AddMethod("string", "ToString", method =>
                    {
                        method.Override();
                        method.AddStatement("return Value;");
                    });
                })
                .AddRecord("Principal", record =>
                {
                    record.Sealed();
                    record.AddProperty("string", "Identifier", property => property.WithoutSetter());
                    record.AddProperty("string", "Type", property => property.WithoutSetter());
                    record.AddProperty("string?", "AccountIdentifier", property => property.WithoutSetter());
                    record.AddProperty("string?", "WorkspaceIdentifier", property => property.WithoutSetter());
                    record.AddProperty("ImmutableHashSet<Scope>", "Scopes", property => property.WithoutSetter());
                    record.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "identifier");
                        ctor.AddParameter("string", "type");
                        ctor.AddParameter("string?", "accountIdentifier");
                        ctor.AddParameter("string?", "workspaceIdentifier");
                        ctor.AddParameter("IEnumerable<Scope>?", "scopes");
                        ctor.AddStatement("Identifier = identifier;");
                        ctor.AddStatement("Type = type;");
                        ctor.AddStatement("AccountIdentifier = accountIdentifier;");
                        ctor.AddStatement("WorkspaceIdentifier = workspaceIdentifier;");
                        ctor.AddStatement("Scopes = (scopes ?? []).ToImmutableHashSet(EqualityComparer<Scope>.Default);");
                    });
                })
                .AddRecord("SecurityRejection", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Code");
                        ctor.AddParameter("string", "Title");
                        ctor.AddParameter("int", "Status");
                    });
                })
                .AddEnum("CredentialResolutionStatus", @enum =>
                {
                    @enum.AddLiteral("Active");
                    @enum.AddLiteral("InvalidCredential");
                    @enum.AddLiteral("ExpiredCredential");
                    @enum.AddLiteral("RevokedCredential");
                    @enum.AddLiteral("Unavailable");
                })
                .AddRecord("CredentialResolverOutcome", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("CredentialResolutionStatus", "Status");
                        ctor.AddParameter("Principal?", "Principal");
                        ctor.AddParameter("DateTimeOffset?", "ExpiresAt");
                        ctor.AddParameter("SecurityRejection?", "Rejection");
                    });
                })
                .AddEnum("CredentialAcquisitionStatus", @enum =>
                {
                    @enum.AddLiteral("Acquired");
                    @enum.AddLiteral("Unavailable");
                })
                .AddRecord("CredentialAcquisitionResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("CredentialAcquisitionStatus", "Status");
                        ctor.AddParameter("CallerCredential?", "Credential");
                        ctor.AddParameter("DateTimeOffset?", "ExpiresAt");
                        ctor.AddParameter("SecurityRejection?", "Rejection");
                    });
                })
                .AddClass("AryzacSecurityOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("JwtSecurityOptions", "Jwt", property => property.WithoutSetter());
                    @class.AddProperty("ApiKeySecurityOptions", "ApiKey", property => property.WithoutSetter());
                    @class.AddProperty("ServiceTokenSecurityOptions", "ServiceToken", property => property.WithoutSetter());
                    @class.AddProperty("InternalServiceKeySecurityOptions", "InternalServiceKey", property => property.WithoutSetter());
                    @class.AddProperty("OutboundSecurityOptions", "Outbound", property => property.WithoutSetter());
                    @class.AddProperty("SecurityDiagnosticsOptions", "Diagnostics", property => property.WithoutSetter());
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement("Jwt = new JwtSecurityOptions();");
                        ctor.AddStatement("ApiKey = new ApiKeySecurityOptions();");
                        ctor.AddStatement("ServiceToken = new ServiceTokenSecurityOptions();");
                        ctor.AddStatement("InternalServiceKey = new InternalServiceKeySecurityOptions();");
                        ctor.AddStatement("Outbound = new OutboundSecurityOptions();");
                        ctor.AddStatement("Diagnostics = new SecurityDiagnosticsOptions();");
                    });
                })
                .AddClass("JwtSecurityOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("bool", "Enabled");
                    @class.AddProperty("string?", "PrimaryRsaPublicKeyPem");
                    @class.AddProperty("string?", "SecondaryRsaPublicKeyPem");
                    @class.AddProperty("bool", "ValidateIssuer");
                    @class.AddProperty("List<string>", "AllowedIssuers", property => property.WithoutSetter());
                    @class.AddProperty("bool", "ValidateAudience");
                    @class.AddProperty("List<string>", "AllowedAudiences", property => property.WithoutSetter());
                    @class.AddProperty("int", "ClockSkewSeconds");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement("AllowedIssuers = new List<string>();");
                        ctor.AddStatement("AllowedAudiences = new List<string>();");
                        ctor.AddStatement("ClockSkewSeconds = 60;");
                    });
                })
                .AddClass("ApiKeySecurityOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("bool", "Enabled");
                    @class.AddProperty("string?", "FormatPrefix");
                    @class.AddProperty("Uri?", "ResolverEndpoint");
                    @class.AddProperty("ICredentialResolverAuthentication?", "ResolverAuthentication");
                    @class.AddProperty("int", "ResolverTimeoutSeconds");
                    @class.AddProperty("int", "ActiveCacheMaximumSeconds");
                    @class.AddProperty("byte[]?", "CacheHmacKey");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement("ResolverTimeoutSeconds = 5;");
                        ctor.AddStatement("ActiveCacheMaximumSeconds = 60;");
                    });
                })
                .AddClass("ServiceTokenSecurityOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("bool", "Enabled");
                })
                .AddClass("OutboundSecurityOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("bool", "Enabled");
                    @class.AddProperty("Uri?", "ServiceCredentialEndpoint");
                    @class.AddProperty("string?", "ClientIdentity");
                    @class.AddProperty("string?", "ClientSecret");
                    @class.AddProperty("string?", "ReservedServiceScope");
                    @class.AddProperty("int", "TimeoutSeconds");
                    @class.AddProperty("int", "ExpirySafetyWindowSeconds");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement("TimeoutSeconds = 10;");
                        ctor.AddStatement("ExpirySafetyWindowSeconds = 60;");
                    });
                })
                .AddClass("ConfiguredPrincipalOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("string?", "Identifier");
                    @class.AddProperty("string?", "Type");
                    @class.AddProperty("string?", "AccountIdentifier");
                    @class.AddProperty("string?", "WorkspaceIdentifier");
                    @class.AddProperty("List<string>", "Scopes", property => property.WithoutSetter());
                    @class.AddConstructor(ctor => ctor.AddStatement("Scopes = new List<string>();"));
                })
                .AddClass("InternalServiceKeySecurityOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("bool", "AdmissionEnabled");
                    @class.AddProperty("bool", "PresentationEnabled");
                    @class.AddProperty("string?", "Value");
                    @class.AddProperty("ConfiguredPrincipalOptions", "Principal", property => property.WithoutSetter());
                    @class.AddConstructor(ctor => ctor.AddStatement("Principal = new ConfiguredPrincipalOptions();"));
                })
                .AddClass("SecurityDiagnosticsOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("bool", "Enabled");
                    @class.AddProperty("byte[]?", "HmacKey");
                    @class.AddProperty("bool", "IncludeApiKeyFormatPrefix");
                })
                .AddInterface("IAmbientCallerCredentialAccessor", @interface =>
                {
                    @interface.AddProperty("CallerCredential?", "Current", property => property.WithoutSetter());
                    @interface.AddMethod("IDisposable", "Push", method => method.AddParameter("CallerCredential", "credential"));
                })
                .AddClass("AmbientCallerCredentialAccessor", @class =>
                {
                    @class.Sealed();
                    @class.Internal();
                    @class.ImplementsInterface("IAmbientCallerCredentialAccessor");
                    @class.AddField("AsyncLocal<AmbientCallerCredentialScope?>", "_current", field =>
                    {
                        field.PrivateReadOnly();
                        field.WithAssignment(new CSharpStatement("new AsyncLocal<AmbientCallerCredentialScope?>()"));
                    });
                    @class.AddProperty("CallerCredential?", "Current", property =>
                    {
                        property.Getter.WithExpressionImplementation("_current.Value?.Credential");
                        property.WithoutSetter();
                    });
                    @class.AddMethod("IDisposable", "Push", method =>
                    {
                        method.AddParameter("CallerCredential", "credential");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(credential);");
                        method.AddStatement("var previous = _current.Value;");
                        method.AddStatement("AmbientCallerCredentialScope? scope = null;");
                        method.AddStatement("scope = new AmbientCallerCredentialScope(credential, () => ReferenceEquals(_current.Value, scope), () => _current.Value = previous);");
                        method.AddStatement("_current.Value = scope;");
                        method.AddStatement("return scope;");
                    });
                })
                .AddClass("AmbientCallerCredentialScope", @class =>
                {
                    @class.Sealed();
                    @class.Internal();
                    @class.ImplementsInterface("IDisposable");
                    @class.AddField("bool", "_disposed");
                    @class.AddProperty("CallerCredential", "Credential", property =>
                    {
                        property.Getter.WithExpressionImplementation("_credential");
                        property.WithoutSetter();
                    });
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("CallerCredential", "credential", parameter => parameter.IntroduceReadonlyField());
                        ctor.AddParameter("Func<bool>", "isCurrent", parameter => parameter.IntroduceReadonlyField());
                        ctor.AddParameter("Action", "restore", parameter => parameter.IntroduceReadonlyField());
                    });
                    @class.AddMethod("void", "Dispose", method =>
                    {
                        method.AddStatement("if (_disposed) return;");
                        method.AddStatement("if (!_isCurrent()) throw new InvalidOperationException(\"Ambient caller credential scopes must be disposed in reverse order.\");");
                        method.AddStatement("_restore();");
                        method.AddStatement("_disposed = true;");
                    });
                })
                .AddInterface("ICredentialResolverAuthentication", @interface =>
                {
                    @interface.AddMethod("ValueTask", "ApplyAsync", method =>
                    {
                        method.AddParameter("HttpRequestMessage", "request");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddInterface("ICallerCredentialResolver", @interface =>
                {
                    @interface.AddMethod("ValueTask<CredentialResolverOutcome>", "ResolveAsync", method =>
                    {
                        method.AddParameter("CallerCredential", "credential");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddInterface("ICallerCredentialAcquirer", @interface =>
                {
                    @interface.AddMethod("ValueTask<CredentialAcquisitionResult>", "AcquireAsync", method =>
                    {
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddInterface("ISecurityPolicy", @interface =>
                {
                    @interface.AddMethod("bool", "CanBypassScopeComparison", method => method.AddParameter("Principal", "principal"));
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
