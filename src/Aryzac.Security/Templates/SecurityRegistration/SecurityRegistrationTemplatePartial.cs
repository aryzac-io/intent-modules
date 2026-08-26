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

namespace Aryzac.Security.Templates.SecurityRegistration
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityRegistrationTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.SecurityRegistration";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityRegistrationTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("Microsoft.Extensions.Configuration")
                .AddUsing("Microsoft.Extensions.DependencyInjection")
                .AddClass("SecurityContractRegistration", @class =>
                {
                    @class.Static();
                    @class.AddMethod("IServiceCollection", "AddAryzacSecurity", method =>
                    {
                        method.Static();
                        method.AddParameter("IServiceCollection", "services", parameter => parameter.WithThisModifier());
                        method.AddParameter("Action<AryzacSecurityOptions>", "configure");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(services);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(configure);");
                        method.AddStatement("var options = new AryzacSecurityOptions();");
                        method.AddStatement("configure(options);");
                        method.AddStatement("ValidateEnabledOptions(options);");
                        method.AddStatement("services.AddSingleton(options);");
                        method.AddStatement("services.AddSingleton<IAmbientCallerCredentialAccessor, AmbientCallerCredentialAccessor>();");
                        method.AddStatement("if (options.Jwt.Enabled) services.AddSingleton(options.Jwt);");
                        method.AddStatement("if (options.ApiKey.Enabled) services.AddSingleton(options.ApiKey);");
                        method.AddStatement("if (options.ServiceToken.Enabled) services.AddSingleton(options.ServiceToken);");
                        method.AddStatement("if (options.InternalServiceKey.AdmissionEnabled || options.InternalServiceKey.PresentationEnabled) services.AddSingleton(options.InternalServiceKey);");
                        method.AddStatement("if (options.Outbound.Enabled) services.AddSingleton(options.Outbound);");
                        method.AddStatement("if (options.Diagnostics.Enabled) services.AddSingleton(options.Diagnostics);");
                        method.AddStatement("return services;");
                    });
                    @class.AddMethod("IServiceCollection", "AddAryzacSecurity", method =>
                    {
                        method.Static();
                        method.AddParameter("IServiceCollection", "services", parameter => parameter.WithThisModifier());
                        method.AddParameter("IConfiguration", "configuration");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(configuration);");
                        method.AddStatement("return services.AddAryzacSecurity(options => configuration.Bind(options));");
                    });
                    @class.AddMethod("void", "ValidateEnabledOptions", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("AryzacSecurityOptions", "options");
                        method.AddStatement("if (options.Jwt.Enabled && string.IsNullOrWhiteSpace(options.Jwt.PrimaryRsaPublicKeyPem)) throw InvalidOption(\"AryzacSecurityOptions.Jwt.PrimaryRsaPublicKeyPem\");");
                        method.AddStatement("if (options.Jwt.Enabled && options.Jwt.ValidateIssuer && (options.Jwt.AllowedIssuers.Count == 0 || options.Jwt.AllowedIssuers.Exists(string.IsNullOrWhiteSpace))) throw InvalidOption(\"AryzacSecurityOptions.Jwt.AllowedIssuers\");");
                        method.AddStatement("if (options.Jwt.Enabled && options.Jwt.ValidateAudience && (options.Jwt.AllowedAudiences.Count == 0 || options.Jwt.AllowedAudiences.Exists(string.IsNullOrWhiteSpace))) throw InvalidOption(\"AryzacSecurityOptions.Jwt.AllowedAudiences\");");
                        method.AddStatement("if (options.Jwt.Enabled && (options.Jwt.ClockSkewSeconds < 0 || options.Jwt.ClockSkewSeconds > 300)) throw InvalidOption(\"AryzacSecurityOptions.Jwt.ClockSkewSeconds\");");
                        method.AddStatement("if (options.ApiKey.Enabled && string.IsNullOrWhiteSpace(options.ApiKey.FormatPrefix)) throw InvalidOption(\"AryzacSecurityOptions.ApiKey.FormatPrefix\");");
                        method.AddStatement("if (options.ApiKey.Enabled && options.ApiKey.ResolverEndpoint != null && !options.ApiKey.ResolverEndpoint.IsAbsoluteUri) throw InvalidOption(\"AryzacSecurityOptions.ApiKey.ResolverEndpoint\");");
                        method.AddStatement("if (options.ApiKey.Enabled && (options.ApiKey.ResolverTimeoutSeconds < 1 || options.ApiKey.ResolverTimeoutSeconds > 60)) throw InvalidOption(\"AryzacSecurityOptions.ApiKey.ResolverTimeoutSeconds\");");
                        method.AddStatement("if (options.ApiKey.Enabled && options.ApiKey.ActiveCacheMaximumSeconds < 0) throw InvalidOption(\"AryzacSecurityOptions.ApiKey.ActiveCacheMaximumSeconds\");");
                        method.AddStatement("if (options.ApiKey.Enabled && options.ApiKey.ActiveCacheMaximumSeconds > 0 && (options.ApiKey.CacheHmacKey == null || options.ApiKey.CacheHmacKey.Length == 0)) throw InvalidOption(\"AryzacSecurityOptions.ApiKey.CacheHmacKey\");");
                        method.AddStatement("if (options.ApiKey.Enabled && options.ApiKey.ResolverEndpoint != null && options.ApiKey.ResolverAuthentication == null) throw InvalidOption(\"AryzacSecurityOptions.ApiKey.ResolverAuthentication\");");
                        method.AddStatement("if ((options.InternalServiceKey.AdmissionEnabled || options.InternalServiceKey.PresentationEnabled) && string.IsNullOrWhiteSpace(options.InternalServiceKey.Value)) throw InvalidOption(\"AryzacSecurityOptions.InternalServiceKey.Value\");");
                        method.AddStatement("if (options.InternalServiceKey.AdmissionEnabled && string.IsNullOrWhiteSpace(options.InternalServiceKey.Principal.Identifier)) throw InvalidOption(\"AryzacSecurityOptions.InternalServiceKey.Principal.Identifier\");");
                        method.AddStatement("if (options.InternalServiceKey.AdmissionEnabled && string.IsNullOrWhiteSpace(options.InternalServiceKey.Principal.Type)) throw InvalidOption(\"AryzacSecurityOptions.InternalServiceKey.Principal.Type\");");
                        method.AddStatement("if (options.Outbound.Enabled && (options.Outbound.ServiceCredentialEndpoint == null || !options.Outbound.ServiceCredentialEndpoint.IsAbsoluteUri)) throw InvalidOption(\"AryzacSecurityOptions.Outbound.ServiceCredentialEndpoint\");");
                        method.AddStatement("if (options.Outbound.Enabled && string.IsNullOrWhiteSpace(options.Outbound.ClientIdentity)) throw InvalidOption(\"AryzacSecurityOptions.Outbound.ClientIdentity\");");
                        method.AddStatement("if (options.Outbound.Enabled && string.IsNullOrWhiteSpace(options.Outbound.ClientSecret)) throw InvalidOption(\"AryzacSecurityOptions.Outbound.ClientSecret\");");
                        method.AddStatement("if (options.Outbound.Enabled && string.IsNullOrWhiteSpace(options.Outbound.ReservedServiceScope)) throw InvalidOption(\"AryzacSecurityOptions.Outbound.ReservedServiceScope\");");
                        method.AddStatement("if (options.Outbound.Enabled && (options.Outbound.TimeoutSeconds < 1 || options.Outbound.TimeoutSeconds > 120)) throw InvalidOption(\"AryzacSecurityOptions.Outbound.TimeoutSeconds\");");
                        method.AddStatement("if (options.Outbound.Enabled && options.Outbound.ExpirySafetyWindowSeconds < 0) throw InvalidOption(\"AryzacSecurityOptions.Outbound.ExpirySafetyWindowSeconds\");");
                        method.AddStatement("if (options.Diagnostics.Enabled && (options.Diagnostics.HmacKey == null || options.Diagnostics.HmacKey.Length == 0)) throw InvalidOption(\"AryzacSecurityOptions.Diagnostics.HmacKey\");");
                    });
                    @class.AddMethod("InvalidOperationException", "InvalidOption", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "optionName");
                        method.AddStatement("return new InvalidOperationException($\"Security option '{optionName}' is missing or invalid.\");");
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
