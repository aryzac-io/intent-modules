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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityOptions
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityOptionsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityOptions";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityOptionsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddClass("SecurityAuthorityOptions", @class =>
                {
                    @class.Sealed();
                    @class.AddProperty("string?", "Issuer");
                    @class.AddProperty("string?", "SigningPrivateKeyPem");
                    @class.AddProperty("string", "SigningKeyId");
                    @class.AddProperty("string?", "ExternalProviderSecretProtectionKey");
                    @class.AddProperty("string?", "SsoCookieProtectionKey");
                    @class.AddProperty("string?", "ApiKeyHashingKey");
                    @class.AddProperty("int", "MinimumRsaKeySize");
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddStatement("SigningKeyId = \"primary\";");
                        ctor.AddStatement("MinimumRsaKeySize = 2048;");
                    });
                })
                .AddClass("SecurityAuthorityStartupValidator", @class =>
                {
                    @class.Static();
                    @class.AddMethod("void", "Validate", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOptions", "options");
                        method.AddParameter("bool", "isDevelopment");
                        method.AddParameter("Action<string>?", "warning");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(options);");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(options.SigningKeyId)) throw Missing(\"SigningKeyId\");");
                        method.AddStatement("if (options.MinimumRsaKeySize < 2048) throw new InvalidOperationException(\"SecurityAuthority:MinimumRsaKeySize must be at least 2048 bits.\");");
                        method.AddStatement("if (!string.IsNullOrWhiteSpace(options.Issuer) && (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer) || issuer.Scheme != Uri.UriSchemeHttps)) throw new InvalidOperationException(\"SecurityAuthority:Issuer must be an absolute HTTPS URI.\");");
                        method.AddStatement("if (!isDevelopment && string.IsNullOrWhiteSpace(options.Issuer)) throw Missing(\"Issuer\");");
                        method.AddStatement("if (!isDevelopment && string.IsNullOrWhiteSpace(options.SigningPrivateKeyPem)) throw Missing(\"SigningPrivateKeyPem\");");
                        method.AddStatement("if (!isDevelopment && string.IsNullOrWhiteSpace(options.ExternalProviderSecretProtectionKey)) throw Missing(\"ExternalProviderSecretProtectionKey\");");
                        method.AddStatement("if (!isDevelopment && string.IsNullOrWhiteSpace(options.SsoCookieProtectionKey)) throw Missing(\"SsoCookieProtectionKey\");");
                        method.AddStatement("if (!isDevelopment && string.IsNullOrWhiteSpace(options.ApiKeyHashingKey)) throw Missing(\"ApiKeyHashingKey\");");
                        method.AddStatement("if (isDevelopment && string.IsNullOrWhiteSpace(options.SigningPrivateKeyPem)) warning?.Invoke(\"Security Authority is using a non-persisted ephemeral RSA signing key. Restarting the application invalidates credentials signed by this instance.\");");
                    });
                    @class.AddMethod("InvalidOperationException", "Missing", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "name");
                        method.AddStatement("return new InvalidOperationException($\"SecurityAuthority:{name} is required outside Development. Configure protected cryptographic material before the application starts listening.\");");
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
