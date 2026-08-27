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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityCryptography
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityCryptographyTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityCryptography";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityCryptographyTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Security.Cryptography")
                .AddUsing("System.Text")
                .AddUsing("System.Text.Json")
                .AddRecord("SecurityAuthorityPublicVerificationKey", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Kid");
                        ctor.AddParameter("string", "Alg");
                        ctor.AddParameter("string", "Kty");
                        ctor.AddParameter("string", "N");
                        ctor.AddParameter("string", "E");
                    });
                })
                .AddClass("SecurityAuthoritySigningKey", @class =>
                {
                    @class.Sealed();
                    @class.ImplementsInterface("IDisposable");
                    @class.AddField("RSA", "_rsa", field => field.PrivateReadOnly());
                    @class.AddField("DateTimeOffset?", "_deactivatesAt");
                    @class.AddField("DateTimeOffset?", "_retainUntil");
                    @class.AddProperty("string", "Kid", property => property.WithoutSetter());
                    @class.AddProperty("string", "Alg", property => property.WithoutSetter());
                    @class.AddProperty("DateTimeOffset", "PublishedAt", property => property.WithoutSetter());
                    @class.AddProperty("DateTimeOffset", "ActivatesAt", property => property.WithoutSetter());
                    @class.AddProperty("DateTimeOffset?", "DeactivatesAt", property =>
                    {
                        property.Getter.WithExpressionImplementation("_deactivatesAt");
                        property.WithoutSetter();
                    });
                    @class.AddProperty("DateTimeOffset?", "RetainUntil", property =>
                    {
                        property.Getter.WithExpressionImplementation("_retainUntil");
                        property.WithoutSetter();
                    });
                    @class.AddConstructor(ctor =>
                    {
                        ctor.Private();
                        ctor.AddParameter("RSA", "rsa");
                        ctor.AddParameter("string", "kid");
                        ctor.AddParameter("DateTimeOffset", "publishedAt");
                        ctor.AddParameter("DateTimeOffset", "activatesAt");
                        ctor.AddStatement("_rsa = rsa;");
                        ctor.AddStatement("Kid = kid;");
                        ctor.AddStatement("Alg = \"RS256\";");
                        ctor.AddStatement("PublishedAt = publishedAt;");
                        ctor.AddStatement("ActivatesAt = activatesAt;");
                    });
                    @class.AddMethod("SecurityAuthoritySigningKey", "FromPem", method =>
                    {
                        method.Static();
                        method.AddParameter("string", "privateKeyPem");
                        method.AddParameter("string", "kid");
                        method.AddParameter("DateTimeOffset", "publishedAt");
                        method.AddParameter("DateTimeOffset", "activatesAt");
                        method.AddParameter("int", "minimumKeySize");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(privateKeyPem)) throw new ArgumentException(\"A private RSA key is required.\", nameof(privateKeyPem));");
                        method.AddStatement("var rsa = RSA.Create();");
                        method.AddStatement("rsa.ImportFromPem(privateKeyPem);");
                        method.AddStatement("if (rsa.KeySize < minimumKeySize) { rsa.Dispose(); throw new InvalidOperationException($\"RSA signing keys must be at least {minimumKeySize} bits.\"); }");
                        method.AddStatement("return new SecurityAuthoritySigningKey(rsa, RequireKid(kid), publishedAt, activatesAt);");
                    });
                    @class.AddMethod("SecurityAuthoritySigningKey", "CreateDevelopmentEphemeral", method =>
                    {
                        method.Static();
                        method.AddParameter("bool", "isDevelopment");
                        method.AddParameter("string", "kid");
                        method.AddParameter("DateTimeOffset", "publishedAt");
                        method.AddParameter("DateTimeOffset", "activatesAt");
                        method.AddParameter("int", "keySize");
                        method.AddParameter("Action<string>?", "warning");
                        method.AddStatement("if (!isDevelopment) throw new InvalidOperationException(\"Ephemeral RSA signing keys are only permitted in Development.\");");
                        method.AddStatement("if (keySize < 2048) throw new InvalidOperationException(\"Ephemeral RSA signing keys must be at least 2048 bits.\");");
                        method.AddStatement("warning?.Invoke(\"Security Authority generated a non-persisted ephemeral RSA signing key. Restarting the application invalidates credentials signed by this instance.\");");
                        method.AddStatement("return new SecurityAuthoritySigningKey(RSA.Create(keySize), RequireKid(kid), publishedAt, activatesAt);");
                    });
                    @class.AddMethod("byte[]", "Sign", method =>
                    {
                        method.AddParameter("byte[]", "data");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(data);");
                        method.AddStatement("return _rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);");
                    });
                    @class.AddMethod("SecurityAuthorityPublicVerificationKey", "ToPublicVerificationKey", method =>
                    {
                        method.AddStatement("var parameters = _rsa.ExportParameters(false);");
                        method.AddStatement("return new SecurityAuthorityPublicVerificationKey(Kid, Alg, \"RSA\", SecurityAuthorityBase64Url.Encode(parameters.Modulus!), SecurityAuthorityBase64Url.Encode(parameters.Exponent!));");
                    });
                    @class.AddMethod("void", "DeactivateAt", method =>
                    {
                        method.AddParameter("DateTimeOffset", "deactivatesAt");
                        method.AddStatement("if (deactivatesAt <= ActivatesAt) throw new ArgumentOutOfRangeException(nameof(deactivatesAt), \"A signing key must remain active after its activation time.\");");
                        method.AddStatement("_deactivatesAt = deactivatesAt;");
                    });
                    @class.AddMethod("void", "RetainThrough", method =>
                    {
                        method.AddParameter("DateTimeOffset", "lastSignedTokenExpiresAt");
                        method.AddStatement("if (_retainUntil is null || lastSignedTokenExpiresAt > _retainUntil.Value) _retainUntil = lastSignedTokenExpiresAt;");
                    });
                    @class.AddMethod("void", "Dispose", method => method.AddStatement("_rsa.Dispose();"));
                    @class.AddMethod("string", "RequireKid", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "kid");
                        method.AddStatement("return !string.IsNullOrWhiteSpace(kid) ? kid : throw new ArgumentException(\"A signing key identifier is required.\", nameof(kid));");
                    });
                })
                .AddClass("SecurityAuthoritySigningKeyRing", @class =>
                {
                    @class.Sealed();
                    @class.AddField("List<SecurityAuthoritySigningKey>", "_keys", field =>
                    {
                        field.PrivateReadOnly();
                        field.WithAssignment(new CSharpStatement("new List<SecurityAuthoritySigningKey>()"));
                    });
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("IEnumerable<SecurityAuthoritySigningKey>", "keys");
                        ctor.AddStatement("ArgumentNullException.ThrowIfNull(keys);");
                        ctor.AddStatement("_keys.AddRange(keys);");
                        ctor.AddStatement("if (_keys.Select(x => x.Kid).Distinct(StringComparer.Ordinal).Count() != _keys.Count) throw new InvalidOperationException(\"Signing key identifiers must be unique.\");");
                    });
                    @class.AddMethod("void", "Publish", method =>
                    {
                        method.AddParameter("SecurityAuthoritySigningKey", "key");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(key);");
                        method.AddStatement("if (_keys.Any(x => string.Equals(x.Kid, key.Kid, StringComparison.Ordinal))) throw new InvalidOperationException($\"Signing key '{key.Kid}' is already published.\");");
                        method.AddStatement("_keys.Add(key);");
                    });
                    @class.AddMethod("string", "SignToken", method =>
                    {
                        method.AddParameter("string", "encodedHeader");
                        method.AddParameter("string", "encodedPayload");
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddParameter("DateTimeOffset", "expiresAt");
                        method.AddStatement("if (expiresAt <= now) throw new ArgumentOutOfRangeException(nameof(expiresAt), \"A signed token must expire after it is issued.\");");
                        method.AddStatement("var key = _keys.Where(x => x.PublishedAt <= now && x.ActivatesAt <= now && (x.DeactivatesAt is null || x.DeactivatesAt > now)).OrderByDescending(x => x.ActivatesAt).FirstOrDefault() ?? throw new InvalidOperationException(\"No published RSA signing key is active. Publish a public verification key before activating it for signing.\");");
                        method.AddStatement("using var header = JsonDocument.Parse(SecurityAuthorityBase64Url.Decode(encodedHeader));");
                        method.AddStatement("if (!header.RootElement.TryGetProperty(\"kid\", out var kid) || !string.Equals(kid.GetString(), key.Kid, StringComparison.Ordinal)) throw new InvalidOperationException(\"The token header kid must reference the active signing key.\");");
                        method.AddStatement("if (!header.RootElement.TryGetProperty(\"alg\", out var alg) || !string.Equals(alg.GetString(), key.Alg, StringComparison.Ordinal)) throw new InvalidOperationException(\"The token header algorithm must match the active signing key.\");");
                        method.AddStatement("var signingInput = $\"{encodedHeader}.{encodedPayload}\";");
                        method.AddStatement("var signature = key.Sign(Encoding.ASCII.GetBytes(signingInput));");
                        method.AddStatement("key.RetainThrough(expiresAt);");
                        method.AddStatement("return $\"{signingInput}.{SecurityAuthorityBase64Url.Encode(signature)}\";");
                    });
                    @class.AddMethod("IReadOnlyList<SecurityAuthorityPublicVerificationKey>", "GetPublishedVerificationKeys", method =>
                    {
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("return _keys.Where(x => x.PublishedAt <= now && ((x.DeactivatesAt is null || x.DeactivatesAt > now) || (x.RetainUntil is not null && x.RetainUntil >= now))).OrderBy(x => x.ActivatesAt).Select(x => x.ToPublicVerificationKey()).ToArray();");
                    });
                    @class.AddMethod("void", "RemoveExpiredVerificationKeys", method =>
                    {
                        method.AddParameter("DateTimeOffset", "now");
                        method.AddStatement("_keys.RemoveAll(x => x.DeactivatesAt is not null && x.DeactivatesAt <= now && (x.RetainUntil is null || x.RetainUntil < now));");
                    });
                })
                .AddClass("SecurityAuthoritySecretProtector", @class =>
                {
                    @class.Sealed();
                    @class.AddField("byte[]", "_key", field => field.PrivateReadOnly());
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "base64RootKey");
                        ctor.AddParameter("string", "purpose");
                        ctor.AddStatement("var rootKey = DecodeRootKey(base64RootKey);");
                        ctor.AddStatement("if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException(\"A secret-protection purpose is required.\", nameof(purpose));");
                        ctor.AddStatement("_key = HMACSHA256.HashData(rootKey, Encoding.UTF8.GetBytes(purpose));");
                        ctor.AddStatement("CryptographicOperations.ZeroMemory(rootKey);");
                    });
                    @class.AddMethod("string", "Protect", method =>
                    {
                        method.AddParameter("string", "clearValue");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(clearValue);");
                        method.AddStatement("var nonce = RandomNumberGenerator.GetBytes(12);");
                        method.AddStatement("var clearBytes = Encoding.UTF8.GetBytes(clearValue);");
                        method.AddStatement("var ciphertext = new byte[clearBytes.Length];");
                        method.AddStatement("var tag = new byte[16];");
                        method.AddStatement("using var aes = new AesGcm(_key, tag.Length);");
                        method.AddStatement("aes.Encrypt(nonce, clearBytes, ciphertext, tag);");
                        method.AddStatement("CryptographicOperations.ZeroMemory(clearBytes);");
                        method.AddStatement("return $\"v1.{SecurityAuthorityBase64Url.Encode(nonce)}.{SecurityAuthorityBase64Url.Encode(tag)}.{SecurityAuthorityBase64Url.Encode(ciphertext)}\";");
                    });
                    @class.AddMethod("string", "Unprotect", method =>
                    {
                        method.AddParameter("string", "protectedValue");
                        method.AddStatement("var parts = protectedValue?.Split('.') ?? Array.Empty<string>();");
                        method.AddStatement("if (parts.Length != 4 || parts[0] != \"v1\") throw new CryptographicException(\"The protected secret format is invalid.\");");
                        method.AddStatement("var nonce = SecurityAuthorityBase64Url.Decode(parts[1]);");
                        method.AddStatement("var tag = SecurityAuthorityBase64Url.Decode(parts[2]);");
                        method.AddStatement("var ciphertext = SecurityAuthorityBase64Url.Decode(parts[3]);");
                        method.AddStatement("var clearBytes = new byte[ciphertext.Length];");
                        method.AddStatement("using var aes = new AesGcm(_key, tag.Length);");
                        method.AddStatement("aes.Decrypt(nonce, ciphertext, tag, clearBytes);");
                        method.AddStatement("try { return Encoding.UTF8.GetString(clearBytes); } finally { CryptographicOperations.ZeroMemory(clearBytes); }");
                    });
                    @class.AddMethod("byte[]", "DecodeRootKey", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "base64RootKey");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(base64RootKey)) throw new ArgumentException(\"A secret-protection root key is required.\", nameof(base64RootKey));");
                        method.AddStatement("var key = Convert.FromBase64String(base64RootKey);");
                        method.AddStatement("if (key.Length < 32) { CryptographicOperations.ZeroMemory(key); throw new CryptographicException(\"Secret-protection root keys must contain at least 256 bits.\"); }");
                        method.AddStatement("return key;");
                    });
                })
                .AddClass("SecurityAuthorityCredentialHasher", @class =>
                {
                    @class.Sealed();
                    @class.AddField("byte[]", "_apiKeyHashingKey", field => field.PrivateReadOnly());
                    @class.AddConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "base64ApiKeyHashingKey");
                        ctor.AddStatement("_apiKeyHashingKey = Convert.FromBase64String(base64ApiKeyHashingKey ?? throw new ArgumentNullException(nameof(base64ApiKeyHashingKey)));");
                        ctor.AddStatement("if (_apiKeyHashingKey.Length < 32) throw new CryptographicException(\"API Key hashing keys must contain at least 256 bits.\");");
                    });
                    @class.AddMethod("string", "HashCredential", method =>
                    {
                        method.AddParameter("string", "clearCredential");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(clearCredential);");
                        method.AddStatement("var salt = RandomNumberGenerator.GetBytes(16);");
                        method.AddStatement("const int iterations = 210000;");
                        method.AddStatement("var hash = Rfc2898DeriveBytes.Pbkdf2(clearCredential, salt, iterations, HashAlgorithmName.SHA256, 32);");
                        method.AddStatement("return $\"pbkdf2-sha256.{iterations}.{SecurityAuthorityBase64Url.Encode(salt)}.{SecurityAuthorityBase64Url.Encode(hash)}\";");
                    });
                    @class.AddMethod("bool", "VerifyCredential", method =>
                    {
                        method.AddParameter("string", "clearCredential");
                        method.AddParameter("string", "storedHash");
                        method.AddStatement("var parts = storedHash?.Split('.') ?? Array.Empty<string>();");
                        method.AddStatement("if (parts.Length != 4 || parts[0] != \"pbkdf2-sha256\" || !int.TryParse(parts[1], out var iterations)) return false;");
                        method.AddStatement("var salt = SecurityAuthorityBase64Url.Decode(parts[2]);");
                        method.AddStatement("var expected = SecurityAuthorityBase64Url.Decode(parts[3]);");
                        method.AddStatement("var actual = Rfc2898DeriveBytes.Pbkdf2(clearCredential, salt, iterations, HashAlgorithmName.SHA256, expected.Length);");
                        method.AddStatement("return CryptographicOperations.FixedTimeEquals(actual, expected);");
                    });
                    @class.AddMethod("string", "HashApiKey", method =>
                    {
                        method.AddParameter("string", "clearApiKey");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(clearApiKey);");
                        method.AddStatement("var hash = HMACSHA256.HashData(_apiKeyHashingKey, Encoding.UTF8.GetBytes(clearApiKey));");
                        method.AddStatement("return $\"hmac-sha256.{SecurityAuthorityBase64Url.Encode(hash)}\";");
                    });
                    @class.AddMethod("bool", "VerifyApiKey", method =>
                    {
                        method.AddParameter("string", "clearApiKey");
                        method.AddParameter("string", "storedHash");
                        method.AddStatement("var parts = storedHash?.Split('.') ?? Array.Empty<string>();");
                        method.AddStatement("if (parts.Length != 2 || parts[0] != \"hmac-sha256\") return false;");
                        method.AddStatement("var expected = SecurityAuthorityBase64Url.Decode(parts[1]);");
                        method.AddStatement("var actual = HMACSHA256.HashData(_apiKeyHashingKey, Encoding.UTF8.GetBytes(clearApiKey));");
                        method.AddStatement("return CryptographicOperations.FixedTimeEquals(actual, expected);");
                    });
                })
                .AddClass("SecurityAuthoritySecretRedactor", @class =>
                {
                    @class.Static();
                    @class.AddMethod("string?", "Redact", method =>
                    {
                        method.Static();
                        method.AddParameter("string", "name");
                        method.AddParameter("string?", "value");
                        method.AddStatement("return IsSensitiveName(name) && value is not null ? \"[REDACTED]\" : value;");
                    });
                    @class.AddMethod("IReadOnlyDictionary<string, string?>", "Redact", method =>
                    {
                        method.Static();
                        method.AddParameter("IReadOnlyDictionary<string, string?>", "values");
                        method.AddStatement("ArgumentNullException.ThrowIfNull(values);");
                        method.AddStatement("return values.ToDictionary(x => x.Key, x => Redact(x.Key, x.Value), StringComparer.Ordinal);");
                    });
                    @class.AddMethod("bool", "IsSensitiveName", method =>
                    {
                        method.Private();
                        method.Static();
                        method.AddParameter("string", "name");
                        method.AddStatement("if (string.IsNullOrWhiteSpace(name)) return false;");
                        method.AddStatement("return new[] { \"secret\", \"password\", \"private\", \"credential\", \"cookie\", \"authorization\", \"bearer\", \"token\", \"authorizationcode\", \"devicecode\", \"refreshtoken\", \"apikey\" }.Any(marker => name.Replace(\"_\", string.Empty, StringComparison.Ordinal).Replace(\"-\", string.Empty, StringComparison.Ordinal).Contains(marker, StringComparison.OrdinalIgnoreCase));");
                    });
                })
                .AddClass("SecurityAuthorityBase64Url", @class =>
                {
                    @class.Static();
                    @class.AddMethod("string", "Encode", method =>
                    {
                        method.Static();
                        method.AddParameter("byte[]", "value");
                        method.AddStatement("return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');");
                    });
                    @class.AddMethod("byte[]", "Decode", method =>
                    {
                        method.Static();
                        method.AddParameter("string", "value");
                        method.AddStatement("var base64 = value.Replace('-', '+').Replace('_', '/');");
                        method.AddStatement("base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');");
                        method.AddStatement("return Convert.FromBase64String(base64);");
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
