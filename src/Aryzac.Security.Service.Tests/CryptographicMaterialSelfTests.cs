using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class CryptographicMaterialSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string CryptographySource = ReadServiceSource("Templates", "SecurityAuthorityCryptography", "SecurityAuthorityCryptographyTemplatePartial.cs");
    private static readonly string OptionsSource = ReadServiceSource("Templates", "SecurityAuthorityOptions", "SecurityAuthorityOptionsTemplatePartial.cs");
    private static readonly string ContractsSource = ReadServiceSource("Templates", "SecurityAuthorityContracts", "SecurityAuthorityContractsTemplatePartial.cs");
    private static readonly string RecordsSource = ReadServiceSource("Templates", "SecurityAuthorityRecords", "SecurityAuthorityRecordsTemplatePartial.cs");
    private static readonly string IntegrationSource = ReadServiceSource("FactoryExtensions", "SecurityAuthorityIntegration.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("RSA tokens and public JWKS material", RsaTokensAndPublicJwksMaterial),
            ("development ephemeral keys are constrained and warned", DevelopmentEphemeralKeysAreConstrainedAndWarned),
            ("production cryptographic material is startup required", ProductionCryptographicMaterialIsStartupRequired),
            ("external secrets are encrypted and omitted from projections", ExternalSecretsAreEncryptedAndOmittedFromProjections),
            ("redeemable credentials are one-way hashed", RedeemableCredentialsAreOneWayHashed),
            ("API Keys use keyed constant-time verification", ApiKeysUseKeyedConstantTimeVerification),
            ("secret-bearing values are redacted", SecretBearingValuesAreRedacted),
            ("signing key publication and retention are enforced", SigningKeyPublicationAndRetentionAreEnforced),
            ("Wave 3 surfaces contain no stubs", WaveThreeSurfacesContainNoStubs)
        };

        var failures = new List<string>();
        foreach (var test in tests)
        {
            try
            {
                test.Execute();
                Console.WriteLine($"PASS: {test.Name}");
            }
            catch (Exception exception)
            {
                failures.Add($"FAIL: {test.Name}: {exception.Message}");
            }
        }

        foreach (var failure in failures)
        {
            Console.Error.WriteLine(failure);
        }

        return failures.Count == 0 ? 0 : 1;
    }

    private static void RsaTokensAndPublicJwksMaterial()
    {
        foreach (var expected in new[]
        {
            "HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1",
            "Alg = \\\"RS256\\\"",
            "SecurityAuthorityPublicVerificationKey",
            "\"Kid\"",
            "\"Alg\"",
            "\"Kty\"",
            "\"N\"",
            "\"E\"",
            "ExportParameters(false)",
            "SignToken",
            "JsonDocument.Parse(SecurityAuthorityBase64Url.Decode(encodedHeader))",
            "The token header kid must reference the active signing key.",
            "The token header algorithm must match the active signing key."
        })
        {
            Contains(CryptographySource, expected);
        }

        var publicKey = GetBlock(CryptographySource, ".AddRecord(\"SecurityAuthorityPublicVerificationKey\"", ".AddClass(", startOffset: 1);
        DoesNotContain(publicKey, "PublishedAt");
        DoesNotContain(publicKey, "ActivatesAt");
        DoesNotContain(publicKey, "RetainUntil");
    }

    private static void DevelopmentEphemeralKeysAreConstrainedAndWarned()
    {
        Contains(CryptographySource, "CreateDevelopmentEphemeral");
        Contains(CryptographySource, "Ephemeral RSA signing keys are only permitted in Development.");
        Contains(CryptographySource, "non-persisted ephemeral RSA signing key");
        Contains(CryptographySource, "Restarting the application invalidates credentials signed by this instance.");
        DoesNotContain(CryptographySource, "ExportPkcs8PrivateKey");
        DoesNotContain(CryptographySource, "ExportRSAPrivateKey");
        DoesNotContain(CryptographySource, "ExportEncryptedPkcs8PrivateKey");
        Contains(OptionsSource, "isDevelopment && string.IsNullOrWhiteSpace(options.SigningPrivateKeyPem)");
    }

    private static void ProductionCryptographicMaterialIsStartupRequired()
    {
        foreach (var option in new[]
        {
            "Issuer",
            "SigningPrivateKeyPem",
            "ExternalProviderSecretProtectionKey",
            "SsoCookieProtectionKey",
            "ApiKeyHashingKey"
        })
        {
            Contains(OptionsSource, $"!isDevelopment && string.IsNullOrWhiteSpace(options.{option})");
        }

        foreach (var requirement in new[]
        {
            "require-production-issuer",
            "require-production-rsa-signing-private-key",
            "require-production-external-provider-secret-protection",
            "require-production-sso-cookie-protection",
            "require-keyed-api-key-hashing"
        })
        {
            Contains(IntegrationSource, requirement);
        }
    }

    private static void ExternalSecretsAreEncryptedAndOmittedFromProjections()
    {
        Contains(RecordsSource, "EncryptedClientSecret");
        Contains(CryptographySource, "new AesGcm(_key, tag.Length)");
        Contains(CryptographySource, "aes.Encrypt(nonce, clearBytes, ciphertext, tag)");
        Contains(CryptographySource, "aes.Decrypt(nonce, ciphertext, tag, clearBytes)");

        var projection = GetBlock(ContractsSource, ".AddRecord(\"SecurityAuthorityIdentityProviderProjection\"", ".AddRecord(", startOffset: 1);
        DoesNotContain(projection, "EncryptedClientSecret");
        DoesNotContain(projection, "ClientSecret");
    }

    private static void RedeemableCredentialsAreOneWayHashed()
    {
        foreach (var field in new[] { "SecretHash", "CodeHash", "DeviceCodeHash", "TokenHash" })
        {
            Contains(RecordsSource, $"\"{field}\"");
        }

        Contains(CryptographySource, "Rfc2898DeriveBytes.Pbkdf2");
        Contains(CryptographySource, "pbkdf2-sha256");
        DoesNotContain(RecordsSource, "AuthorizationCodeSecret");
        DoesNotContain(RecordsSource, "DeviceCodeSecret");
        DoesNotContain(RecordsSource, "RefreshTokenSecret");
        Contains(ContractsSource, "Interlocked.Exchange(ref _revealed, 1)");
    }

    private static void ApiKeysUseKeyedConstantTimeVerification()
    {
        Contains(CryptographySource, "HMACSHA256.HashData(_apiKeyHashingKey");
        Contains(CryptographySource, "hmac-sha256");
        True(Count(CryptographySource, "CryptographicOperations.FixedTimeEquals") >= 2);
        Contains(RecordsSource, "KeyHash");
        DoesNotContain(RecordsSource, "ApiKeySecret");
    }

    private static void SecretBearingValuesAreRedacted()
    {
        Contains(CryptographySource, "SecurityAuthoritySecretRedactor");
        Contains(CryptographySource, "[REDACTED]");
        foreach (var marker in new[] { "secret", "password", "private", "credential", "cookie", "authorization", "bearer", "token", "authorizationcode", "devicecode", "refreshtoken", "apikey" })
        {
            Contains(CryptographySource, $"\\\"{marker}\\\"");
        }

        Contains(IntegrationSource, "require-secret-redaction");
    }

    private static void SigningKeyPublicationAndRetentionAreEnforced()
    {
        Contains(CryptographySource, "x.PublishedAt <= now && x.ActivatesAt <= now && (x.DeactivatesAt is null || x.DeactivatesAt > now)");
        Contains(CryptographySource, "Publish a public verification key before activating it for signing.");
        Contains(CryptographySource, "key.RetainThrough(expiresAt)");
        Contains(CryptographySource, "x.RetainUntil is not null && x.RetainUntil >= now");
        Contains(CryptographySource, "RemoveExpiredVerificationKeys");
    }

    private static void WaveThreeSurfacesContainNoStubs()
    {
        foreach (var source in new[] { CryptographySource, OptionsSource, ContractsSource })
        {
            DoesNotContain(source, "NotImplementedException");
            DoesNotContain(source, "TODO");
            DoesNotContain(source, "exampleParam");
        }
    }

    private static string GetBlock(string source, string startMarker, string nextMarker, int startOffset)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        True(start >= 0, $"Marker '{startMarker}' was not found.");
        var end = source.IndexOf(nextMarker, start + startMarker.Length + startOffset, StringComparison.Ordinal);
        return source[start..(end < 0 ? source.Length : end)];
    }

    private static string ReadServiceSource(params string[] path)
    {
        return File.ReadAllText(Path.Combine(new[] { ServiceProject }.Concat(path).ToArray()));
    }

    private static string FindServiceProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Aryzac.Security.Service");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(directory.FullName, "src", "Aryzac.Security.Service");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Aryzac.Security.Service.");
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static void Contains(string source, string value)
    {
        True(source.Contains(value, StringComparison.Ordinal), $"Expected source to contain '{value}'.");
    }

    private static void DoesNotContain(string source, string value)
    {
        True(!source.Contains(value, StringComparison.Ordinal), $"Expected source not to contain '{value}'.");
    }

    private static void True(bool condition, string message = "Expected condition to be true.")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
