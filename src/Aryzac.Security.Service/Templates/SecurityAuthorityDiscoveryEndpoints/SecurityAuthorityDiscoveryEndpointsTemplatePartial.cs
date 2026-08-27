using System;
using System.Collections.Generic;
using Aryzac.Security.Service.Templates.SecurityAuthorityCryptography;
using Aryzac.Security.Service.Templates.SecurityAuthorityOptions;
using Aryzac.Security.Service.Templates.SecurityAuthorityRecords;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Builder;
using Intent.Modules.Common.CSharp.Templates;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;
using Intent.Templates;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.CSharp.Templates.CSharpTemplatePartial", Version = "1.0")]

namespace Aryzac.Security.Service.Templates.SecurityAuthorityDiscoveryEndpoints
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityDiscoveryEndpointsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityDiscoveryEndpoints";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityDiscoveryEndpointsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            AddTypeSource(SecurityAuthorityCryptographyTemplate.TemplateId);
            AddTypeSource(SecurityAuthorityOptionsTemplate.TemplateId);
            AddTypeSource(SecurityAuthorityRecordsTemplate.TemplateId);

            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Security.Cryptography")
                .AddUsing("System.Text")
                .AddUsing("Microsoft.AspNetCore.Builder")
                .AddUsing("Microsoft.AspNetCore.Http")
                .AddUsing("Microsoft.AspNetCore.Routing")
                .AddRecord("SecurityAuthorityDiscoveryConfiguration", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "DiscoveryPath");
                        ctor.AddParameter("string", "JwksPath");
                        ctor.AddParameter("string", "AuthorizationPath");
                        ctor.AddParameter("string", "TokenPath");
                        ctor.AddParameter("string", "UserInfoPath");
                        ctor.AddParameter("string", "EndSessionPath");
                        ctor.AddParameter("string?", "DeviceAuthorizationPath");
                        ctor.AddParameter("bool", "AuthorizationCodeEnabled");
                        ctor.AddParameter("bool", "ClientCredentialsEnabled");
                        ctor.AddParameter("bool", "RefreshTokenEnabled");
                        ctor.AddParameter("bool", "DeviceAuthorizationEnabled");
                        ctor.AddParameter("IReadOnlyList<string>", "SupportedScopes");
                        ctor.AddParameter("IReadOnlyList<string>", "SupportedClaims");
                    });
                })
                .AddRecord("SecurityAuthorityOAuthValidationResult", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("bool", "IsValid");
                        ctor.AddParameter("string?", "Error");
                        ctor.AddParameter("string?", "ErrorDescription");
                    });
                })
                .AddClass("SecurityAuthorityDiscoveryEndpoints", @class =>
                {
                    @class.Static();
                    @class.AddMethod("void", "Map", method =>
                    {
                        method.Static();
                        method.AddParameter("IEndpointRouteBuilder", "endpoints");
                        method.AddParameter("SecurityAuthorityOptions", "options");
                        method.AddParameter("SecurityAuthorityDiscoveryConfiguration", "configuration");
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("Func<DateTimeOffset>", "utcNow");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(endpoints);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(options);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(configuration);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(signingKeys);");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(utcNow);");
                        method.AddStatement("endpoints.MapGet(configuration.DiscoveryPath, () => Results.Json(CreateDiscoveryDocument(options, configuration))).AllowAnonymous();");
                        method.AddStatement("endpoints.MapGet(configuration.JwksPath, () => Results.Json(CreateJwksDocument(signingKeys, utcNow()))).AllowAnonymous();");
                    });
                    @class.AddMethod("IReadOnlyDictionary<string, object>", "CreateDiscoveryDocument", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOptions", "options");
                        method.AddParameter("SecurityAuthorityDiscoveryConfiguration", "configuration");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(configuration);");
                        method.AddStatement("var issuer = GetIssuer(options);");
                        method.AddStatement("var grantTypes = new List<string>();");
                        method.AddStatement("if (configuration.AuthorizationCodeEnabled) grantTypes.Add(\"authorization_code\");");
                        method.AddStatement("if (configuration.ClientCredentialsEnabled) grantTypes.Add(\"client_credentials\");");
                        method.AddStatement("if (configuration.RefreshTokenEnabled) grantTypes.Add(\"refresh_token\");");
                        method.AddStatement("if (configuration.DeviceAuthorizationEnabled) grantTypes.Add(\"urn:ietf:params:oauth:grant-type:device_code\");");
                        method.AddStatement("var metadata = new Dictionary<string, object>(StringComparer.Ordinal)");
                        method.AddStatement("{");
                        method.AddStatement("    [\"issuer\"] = issuer,");
                        method.AddStatement("    [\"authorization_endpoint\"] = Endpoint(issuer, configuration.AuthorizationPath),");
                        method.AddStatement("    [\"token_endpoint\"] = Endpoint(issuer, configuration.TokenPath),");
                        method.AddStatement("    [\"userinfo_endpoint\"] = Endpoint(issuer, configuration.UserInfoPath),");
                        method.AddStatement("    [\"jwks_uri\"] = Endpoint(issuer, configuration.JwksPath),");
                        method.AddStatement("    [\"end_session_endpoint\"] = Endpoint(issuer, configuration.EndSessionPath),");
                        method.AddStatement("    [\"response_types_supported\"] = configuration.AuthorizationCodeEnabled ? new[] { \"code\" } : Array.Empty<string>(),");
                        method.AddStatement("    [\"grant_types_supported\"] = grantTypes,");
                        method.AddStatement("    [\"subject_types_supported\"] = new[] { \"public\" },");
                        method.AddStatement("    [\"id_token_signing_alg_values_supported\"] = new[] { \"RS256\" },");
                        method.AddStatement("    [\"token_endpoint_auth_methods_supported\"] = new[] { \"client_secret_basic\", \"client_secret_post\", \"none\" },");
                        method.AddStatement("    [\"scopes_supported\"] = Normalize(new[] { \"openid\", \"profile\", \"email\" }.Concat(configuration.SupportedScopes ?? Array.Empty<string>()).ToArray()),");
                        method.AddStatement("    [\"claims_supported\"] = Normalize(new[] { \"sub\", \"name\", \"preferred_username\", \"email\", \"email_verified\" }.Concat(configuration.SupportedClaims ?? Array.Empty<string>()).ToArray()),");
                        method.AddStatement("    [\"code_challenge_methods_supported\"] = new[] { \"S256\" }");
                        method.AddStatement("};");
                        method.AddStatement("if (configuration.DeviceAuthorizationEnabled)");
                        method.AddStatement("{");
                        method.AddStatement("    if (string.IsNullOrWhiteSpace(configuration.DeviceAuthorizationPath)) throw new InvalidOperationException(\"A device authorization endpoint path is required when Device Authorization is enabled.\");");
                        method.AddStatement("    metadata[\"device_authorization_endpoint\"] = Endpoint(issuer, configuration.DeviceAuthorizationPath);");
                        method.AddStatement("}");
                        method.AddStatement("return metadata;");
                    });
                    @class.AddMethod("IReadOnlyDictionary<string, object>", "CreateJwksDocument", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthoritySigningKeyRing", "signingKeys");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(signingKeys);");
                        method.AddStatement("var keys = signingKeys.GetPublishedVerificationKeys(now).Select(key => (object)new Dictionary<string, object>(StringComparer.Ordinal)");
                        method.AddStatement("{");
                        method.AddStatement("    [\"kid\"] = key.Kid,");
                        method.AddStatement("    [\"alg\"] = key.Alg,");
                        method.AddStatement("    [\"kty\"] = key.Kty,");
                        method.AddStatement("    [\"use\"] = \"sig\",");
                        method.AddStatement("    [\"n\"] = key.N,");
                        method.AddStatement("    [\"e\"] = key.E");
                        method.AddStatement("}).ToArray();");
                        method.AddStatement("return new Dictionary<string, object>(StringComparer.Ordinal) { [\"keys\"] = keys };");
                    });
                    @class.AddMethod("string", "GetIssuer", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOptions", "options");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(options);");
                        method.AddStatement("if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer) || string.IsNullOrWhiteSpace(issuer.Scheme)) throw new InvalidOperationException(\"SecurityAuthority:Issuer must be configured as an absolute URI before discovery or token issuance.\");");
                        method.AddStatement("return issuer.AbsoluteUri.TrimEnd('/');");
                    });
                    @class.AddMethod("string", "Endpoint", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "issuer");
                        method.AddParameter("string", "path");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException(\"Security Authority endpoint paths cannot be empty.\");");
                        method.AddStatement("return new Uri(new Uri(issuer + \"/\", UriKind.Absolute), path.TrimStart('/')).AbsoluteUri;");
                    });
                    @class.AddMethod("string[]", "Normalize", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("IReadOnlyList<string>", "values");
                        method.AddStatement("return (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();");
                    });
                })
                .AddClass("SecurityAuthorityOAuthClientValidation", @class =>
                {
                    @class.Static();
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "ValidateRedirectUri", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("string", "redirectUri");
                        method.AddParameter("bool", "postLogout");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(client);");
                        method.AddStatement("if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri || !string.IsNullOrEmpty(uri.Fragment)) return Invalid(\"invalid_request\", \"The redirect URI must be absolute and fragment-free.\");");
                        method.AddStatement("var registeredUris = postLogout ? client.PostLogoutRedirectUris : client.RedirectUris;");
                        method.AddStatement("return registeredUris is not null && registeredUris.Contains(redirectUri, StringComparer.Ordinal) ? Valid() : Invalid(\"invalid_request\", \"The redirect URI is not registered for this client.\");");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "ValidateAuthorizationClient", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("string", "redirectUri");
                        method.AddStatement("var active = ValidateActive(client, \"authorization\", \"unauthorized_client\");");
                        method.AddStatement("return active.IsValid ? ValidateRedirectUri(client, redirectUri, false) : active;");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "ValidateDeviceAuthorizationClient", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddStatement("var active = ValidateActive(client, \"device authorization\", \"unauthorized_client\");");
                        method.AddStatement("return !active.IsValid || AllowsGrant(client, \"urn:ietf:params:oauth:grant-type:device_code\") ? active : Invalid(\"unauthorized_client\", \"The client is not registered for Device Authorization.\");");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "ValidateTokenClient", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddStatement("return ValidateActive(client, \"token issuance\", \"invalid_client\");");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "ValidateAuthorizationCodeRedemption", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("string", "redirectUri");
                        method.AddParameter("string?", "codeVerifier");
                        method.AddParameter("string?", "codeChallenge");
                        method.AddParameter("string?", "codeChallengeMethod");
                        method.AddStatement("var active = ValidateTokenClient(client);");
                        method.AddStatement("if (!active.IsValid) return active;");
                        method.AddStatement("if (!AllowsGrant(client, \"authorization_code\")) return Invalid(\"unauthorized_client\", \"The client is not registered for Authorization Code redemption.\");");
                        method.AddStatement("var redirect = ValidateRedirectUri(client, redirectUri, false);");
                        method.AddStatement("if (!redirect.IsValid) return redirect;");
                        method.AddStatement("var isPublic = string.Equals(client.ClientType, \"Public\", StringComparison.Ordinal);");
                        method.AddStatement("if (isPublic && !string.IsNullOrEmpty(client.SecretHash)) return Invalid(\"invalid_client\", \"Public clients cannot have a client secret.\");");
                        method.AddStatement("if (isPublic && (!string.Equals(codeChallengeMethod, \"S256\", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(codeVerifier) || string.IsNullOrWhiteSpace(codeChallenge))) return Invalid(\"invalid_grant\", \"Public clients must redeem Authorization Codes using S256 PKCE.\");");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(codeChallenge)) return isPublic ? Invalid(\"invalid_grant\", \"Public clients must provide an S256 PKCE challenge.\") : Valid();");
                        method.AddStatement("if (!string.Equals(codeChallengeMethod, \"S256\", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(codeVerifier)) return Invalid(\"invalid_grant\", \"Only S256 PKCE is supported.\");");
                        method.AddStatement("var actualChallenge = SecurityAuthorityBase64Url.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));");
                        method.AddStatement("var actualBytes = Encoding.ASCII.GetBytes(actualChallenge);");
                        method.AddStatement("var expectedBytes = Encoding.ASCII.GetBytes(codeChallenge);");
                        method.AddStatement("return actualBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes) ? Valid() : Invalid(\"invalid_grant\", \"The PKCE verifier is invalid.\");");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "AuthenticateForToken", method =>
                    {
                        method.Static();
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("string?", "authorizationHeader");
                        method.AddParameter("string?", "postClientIdentifier");
                        method.AddParameter("string?", "postClientSecret");
                        method.AddParameter("SecurityAuthorityCredentialHasher", "credentialHasher");
                        method.AddStatement("var active = ValidateTokenClient(client);");
                        method.AddStatement("if (!active.IsValid) return active;");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(credentialHasher);");
                        method.AddStatement("var hasBasic = !string.IsNullOrWhiteSpace(authorizationHeader);");
                        method.AddStatement("var hasPost = !string.IsNullOrWhiteSpace(postClientIdentifier) || !string.IsNullOrWhiteSpace(postClientSecret);");
                        method.AddStatement("if (hasBasic && hasPost) return InvalidClient();");
                        method.AddStatement("if (string.Equals(client.ClientType, \"Public\", StringComparison.Ordinal))");
                        method.AddStatement("{");
                        method.AddStatement("    if (hasBasic || !string.IsNullOrWhiteSpace(postClientSecret) || !string.IsNullOrEmpty(client.SecretHash)) return InvalidClient();");
                        method.AddStatement("    return string.Equals(postClientIdentifier, client.ClientIdentifier, StringComparison.Ordinal) ? Valid() : InvalidClient();");
                        method.AddStatement("}");
                        method.AddStatement("if (!string.Equals(client.ClientType, \"Confidential\", StringComparison.Ordinal) || hasBasic == hasPost || string.IsNullOrWhiteSpace(client.SecretHash)) return InvalidClient();");
                        method.AddStatement("string? suppliedIdentifier = postClientIdentifier;");
                        method.AddStatement("string? suppliedSecret = postClientSecret;");
                        method.AddStatement("if (hasBasic)");
                        method.AddStatement("{");
                        method.AddStatement("    if (!authorizationHeader!.StartsWith(\"Basic \", StringComparison.OrdinalIgnoreCase)) return InvalidClient();");
                        method.AddStatement("    try");
                        method.AddStatement("    {");
                        method.AddStatement("        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader[6..].Trim()));");
                        method.AddStatement("        var separator = decoded.IndexOf(':');");
                        method.AddStatement("        if (separator <= 0) return InvalidClient();");
                        method.AddStatement("        suppliedIdentifier = decoded[..separator];");
                        method.AddStatement("        suppliedSecret = decoded[(separator + 1)..];");
                        method.AddStatement("    }");
                        method.AddStatement("    catch (FormatException)");
                        method.AddStatement("    {");
                        method.AddStatement("        return InvalidClient();");
                        method.AddStatement("    }");
                        method.AddStatement("}");
                        method.AddStatement("if (!string.Equals(suppliedIdentifier, client.ClientIdentifier, StringComparison.Ordinal) || string.IsNullOrEmpty(suppliedSecret)) return InvalidClient();");
                        method.AddStatement("return credentialHasher.VerifyCredential(suppliedSecret, client.SecretHash) ? Valid() : InvalidClient();");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "ValidateActive", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("string", "operation");
                        method.AddParameter("string", "error");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(client);");
                        method.AddStatement("return client.IsActive ? Valid() : Invalid(error, $\"Inactive clients cannot perform {operation}.\");");
                    });
                    @class.AddMethod("bool", "AllowsGrant", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("SecurityAuthorityOAuthClient", "client");
                        method.AddParameter("string", "grantType");
                        method.AddStatement("return client.AllowedGrantTypes is not null && client.AllowedGrantTypes.Contains(grantType, StringComparer.Ordinal);");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "Valid", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddStatement("return new SecurityAuthorityOAuthValidationResult(true, null, null);");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "Invalid", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "error");
                        method.AddParameter("string", "description");
                        method.AddStatement("return new SecurityAuthorityOAuthValidationResult(false, error, description);");
                    });
                    @class.AddMethod("SecurityAuthorityOAuthValidationResult", "InvalidClient", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddStatement("return Invalid(\"invalid_client\", \"Client authentication failed.\");");
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

