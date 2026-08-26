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

namespace Aryzac.Security.Templates.SecurityInboundCredentials
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityInboundCredentialsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.SecurityInboundCredentials";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityInboundCredentialsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Immutable")
                .AddUsing("System.Text")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddUsing("Microsoft.AspNetCore.Http")
                .AddRecord("AuthorizationCredentialParseResult", record =>
                {
                    record.Sealed();
                    record.Internal();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("CallerCredential?", "Credential");
                        ctor.AddParameter("SecurityRejection?", "Rejection");
                    });
                    record.AddProperty("bool", "IsSuccess", property =>
                    {
                        property.Getter.WithExpressionImplementation("Credential is not null");
                        property.WithoutSetter();
                    });
                })
                .AddClass("AuthorizationCredentialParser", @class =>
                {
                    @class.Static();
                    @class.Internal();
                    @class.AddMethod("AuthorizationCredentialParseResult", "Parse", method =>
                    {
                        method.Static();
                        method.AddParameter("IHeaderDictionary", "headers");
                        method.AddParameter("ApiKeySecurityOptions", "apiKeyOptions");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(headers);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(apiKeyOptions);");
                        method.AddStatement("if (!headers.TryGetValue(\"Authorization\", out var values))");
                        method.AddStatement("{");
                        method.AddStatement("    return Reject(\"missing_credential\", \"Caller credential is required.\");");
                        method.AddStatement("}");
                        method.AddStatement("if (values.Count == 0)");
                        method.AddStatement("{");
                        method.AddStatement("    return Reject(\"missing_credential\", \"Caller credential is required.\");");
                        method.AddStatement("}");
                        method.AddStatement("if (values.Count != 1)");
                        method.AddStatement("{");
                        method.AddStatement("    return Reject(\"malformed_credential\", \"Caller credential is malformed.\");");
                        method.AddStatement("}");
                        method.AddStatement("var authorizationValue = values[0];");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(authorizationValue))");
                        method.AddStatement("{");
                        method.AddStatement("    return Reject(\"missing_credential\", \"Caller credential is required.\");");
                        method.AddStatement("}");
                        method.AddStatement("if (authorizationValue.Contains(',', StringComparison.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    return Reject(\"malformed_credential\", \"Caller credential is malformed.\");");
                        method.AddStatement("}");
                        method.AddStatement("var separatorIndex = authorizationValue.IndexOf(' ');");
                        method.AddStatement("if (separatorIndex <= 0)");
                        method.AddStatement("{");
                        method.AddStatement("    return Reject(\"malformed_credential\", \"Caller credential is malformed.\");");
                        method.AddStatement("}");
                        method.AddStatement("var scheme = authorizationValue[..separatorIndex];");
                        method.AddStatement("var credentialValue = authorizationValue[(separatorIndex + 1)..].Trim();");
                        method.AddStatement("if (credentialValue.Length == 0 || credentialValue.IndexOf(' ') >= 0 || credentialValue.IndexOf('\\t') >= 0 || credentialValue.IndexOf('\\r') >= 0 || credentialValue.IndexOf('\\n') >= 0)");
                        method.AddStatement("{");
                        method.AddStatement("    return Reject(\"malformed_credential\", \"Caller credential is malformed.\");");
                        method.AddStatement("}");
                        method.AddStatement("CallerCredentialKind kind;");
                        method.AddStatement("if (string.Equals(scheme, \"Bearer\", StringComparison.OrdinalIgnoreCase))");
                        method.AddStatement("{");
                        method.AddStatement("    kind = CallerCredentialKind.Jwt;");
                        method.AddStatement("}");
                        method.AddStatement("else if (string.Equals(scheme, \"ApiKey\", StringComparison.OrdinalIgnoreCase))");
                        method.AddStatement("{");
                        method.AddStatement("    if (apiKeyOptions.Enabled && string.IsNullOrWhiteSpace(apiKeyOptions.FormatPrefix))");
                        method.AddStatement("    {");
                        method.AddStatement("        throw new InvalidOperationException(\"Security option 'AryzacSecurityOptions.ApiKey.FormatPrefix' is missing or invalid.\");");
                        method.AddStatement("    }");
                        method.AddStatement("    kind = apiKeyOptions.Enabled && credentialValue.StartsWith(apiKeyOptions.FormatPrefix!, StringComparison.Ordinal)");
                        method.AddStatement("        ? CallerCredentialKind.ApiKey");
                        method.AddStatement("        : CallerCredentialKind.InternalServiceKey;");
                        method.AddStatement("}");
                        method.AddStatement("else");
                        method.AddStatement("{");
                        method.AddStatement("    return Reject(\"unsupported_credential_scheme\", \"Caller credential scheme is unsupported.\");");
                        method.AddStatement("}");
                        method.AddStatement("var credentialBytes = Encoding.UTF8.GetBytes(credentialValue).ToImmutableArray();");
                        method.AddStatement("return new AuthorizationCredentialParseResult(new CallerCredential(kind, scheme, credentialBytes), null);");
                    });
                    @class.AddMethod("AuthorizationCredentialParseResult", "Reject", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "code");
                        method.AddParameter("string", "title");
                        method.AddStatement("return new AuthorizationCredentialParseResult(null, new SecurityRejection(code, title, StatusCodes.Status401Unauthorized));");
                    });
                })
                .AddClass("SecurityInboundCredentials", @class =>
                {
                    @class.Sealed();
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("ApiKeySecurityOptions", "apiKeyOptions", parameter => parameter.IntroduceReadonlyField());
                        ctor.AddParameter("ICallerCredentialResolver", "credentialResolver", parameter => parameter.IntroduceReadonlyField());
                        ctor.AddParameter("IAmbientCallerCredentialAccessor", "ambientCallerCredentialAccessor", parameter => parameter.IntroduceReadonlyField());
                    });
                    @class.AddMethod("Principal?", "GetPrincipal", method =>
                    {
                        method.Static();
                        method.AddParameter("HttpContext", "httpContext");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(httpContext);");
                        method.AddStatement("return httpContext.Items.TryGetValue(typeof(SecurityInboundCredentials), out var value) ? value as Principal : null;");
                    });
                    @class.AddMethod("ValueTask<SecurityRejection?>", "ExecuteAsync", method =>
                    {
                        method.Async();
                        method.AddParameter("HttpContext", "httpContext");
                        method.AddParameter("Func<Principal, CancellationToken, ValueTask>", "operation");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(httpContext);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(operation);");
                        method.AddStatement("var parseResult = AuthorizationCredentialParser.Parse(httpContext.Request.Headers, _apiKeyOptions);");
                        method.AddStatement("if (!parseResult.IsSuccess)");
                        method.AddStatement("{");
                        method.AddStatement("    return parseResult.Rejection;");
                        method.AddStatement("}");
                        method.AddStatement("var credential = parseResult.Credential!;");
                        method.AddStatement("var resolution = await _credentialResolver.ResolveAsync(credential, cancellationToken);");
                        method.AddStatement("if (resolution.Status != CredentialResolutionStatus.Active || resolution.Principal is null)");
                        method.AddStatement("{");
                        method.AddStatement("    return resolution.Rejection ?? new SecurityRejection(\"invalid_credential\", \"Caller credential is invalid.\", StatusCodes.Status401Unauthorized);");
                        method.AddStatement("}");
                        method.AddStatement("var principal = resolution.Principal;");
                        method.AddStatement("var principalKey = typeof(SecurityInboundCredentials);");
                        method.AddStatement("var hadPreviousPrincipal = httpContext.Items.TryGetValue(principalKey, out var previousPrincipal);");
                        method.AddStatement("httpContext.Items[principalKey] = principal;");
                        method.AddStatement("try");
                        method.AddStatement("{");
                        method.AddStatement("    using (_ambientCallerCredentialAccessor.Push(credential))");
                        method.AddStatement("    {");
                        method.AddStatement("        await operation(principal, cancellationToken);");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("finally");
                        method.AddStatement("{");
                        method.AddStatement("    if (hadPreviousPrincipal)");
                        method.AddStatement("    {");
                        method.AddStatement("        httpContext.Items[principalKey] = previousPrincipal;");
                        method.AddStatement("    }");
                        method.AddStatement("    else");
                        method.AddStatement("    {");
                        method.AddStatement("        httpContext.Items.Remove(principalKey);");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("return null;");
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
