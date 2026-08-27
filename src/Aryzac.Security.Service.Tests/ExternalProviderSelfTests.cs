using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class ExternalProviderSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string ProviderSource = ReadServiceSource("Templates", "SecurityAuthorityExternalProviders", "SecurityAuthorityExternalProvidersTemplatePartial.cs");
    private static readonly string RecordsSource = ReadServiceSource("Templates", "SecurityAuthorityRecords", "SecurityAuthorityRecordsTemplatePartial.cs");
    private static readonly string ContractsSource = ReadServiceSource("Templates", "SecurityAuthorityContracts", "SecurityAuthorityContractsTemplatePartial.cs");
    private static readonly string ValidationSource = ReadServiceSource("Templates", "SecurityAuthorityValidation", "SecurityAuthorityValidationTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("generic OIDC and provider presets contain conventions only", ProviderPresetsContainConventionsOnly),
            ("provider selection uses eligibility preferred provider and priority", ProviderSelectionUsesEligibilityPreferenceAndPriority),
            ("Invite Only and Open SSO enforce linking and creation rules", AccessModesEnforceLinkingAndCreationRules),
            ("issuer subject is globally unique and existing Users cannot merge", IssuerSubjectIsUniqueAndUsersCannotMerge),
            ("callbacks accept GET query and POST form_post", CallbackModesAreSupported),
            ("generic OIDC protocol performs discovery exchange and validation", GenericOidcProtocolIsConcrete),
            ("OIDC authentication requires every validation stage", AuthenticationRequiresEveryValidationStage),
            ("callback failures are atomic and redirect only when safe", CallbackFailuresAreAtomicAndRedirectOnlyWhenSafe),
            ("inactive providers retain existing identity records", InactiveProvidersRetainExistingRecords),
            ("provider secrets stay encrypted and out of projections", ProviderSecretsStayProtected),
            ("external provider surface contains no stubs", ExternalProviderSurfaceContainsNoStubs)
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

    private static void ProviderPresetsContainConventionsOnly()
    {
        var presets = GetBlock(ProviderSource, ".AddMethod(\"SecurityAuthorityExternalProviderPreset\", \"GetPreset\"", ".AddMethod(\"SecurityAuthorityIdentityProvider?\", \"SelectProvider\"");
        foreach (var providerType in new[] { "GenericOidc", "EntraExternalId", "EntraId", "Google", "Auth0", "Keycloak" })
        {
            Contains(presets, $"\\\"{providerType}\\\" => new(providerType");
        }

        foreach (var convention in new[] { "openid", "profile", "email", "sub", "name", "preferred_username", "picture" })
        {
            Contains(presets, $"\\\"{convention}\\\"");
        }

        foreach (var deploymentValue in new[] { "https://", "http://", "tenantId", "clientId", "clientSecret", "authorityUrl", "redirectUri", "localhost" })
        {
            DoesNotContain(presets, deploymentValue);
        }
    }

    private static void ProviderSelectionUsesEligibilityPreferenceAndPriority()
    {
        var selection = GetBlock(ProviderSource, ".AddMethod(\"SecurityAuthorityIdentityProvider?\", \"SelectProvider\"", ".AddMethod(\"ValueTask<SecurityAuthorityExternalProviderCallback>\", \"ReadCallbackAsync\"");
        Contains(selection, "provider.IsActive");
        Contains(selection, "provider.TenantResourceId is null || string.Equals(provider.TenantResourceId, tenantResourceId, StringComparison.Ordinal)");
        Contains(selection, "eligible.FirstOrDefault(provider => string.Equals(provider.Id, preferredProviderId, StringComparison.Ordinal) || string.Equals(provider.ProviderIdentifier, preferredProviderId, StringComparison.Ordinal))");
        Contains(selection, "eligible.OrderBy(provider => provider.DisplayPriority).ThenBy(provider => provider.ProviderIdentifier, StringComparer.Ordinal).FirstOrDefault()");
        True(selection.IndexOf("preferred is not null", StringComparison.Ordinal) < selection.IndexOf("OrderBy(provider => provider.DisplayPriority)", StringComparison.Ordinal), "Preferred provider selection must precede priority fallback.");
    }

    private static void AccessModesEnforceLinkingAndCreationRules()
    {
        var process = ProcessCallbackBlock();
        Contains(process, "if (identity is not null)");
        Contains(process, "user = await findUser(identity.UserId, cancellationToken)");
        Contains(process, "string.Equals(provider.AccessMode, \\\"InviteOnly\\\", StringComparison.Ordinal)");
        Contains(process, "This Identity Provider permits invited or previously linked Users only.");
        Contains(process, "string.Equals(provider.AccessMode, \\\"OpenSso\\\", StringComparison.Ordinal)");
        Contains(process, "user = string.IsNullOrWhiteSpace(existingUserId) ? null : await findUser(existingUserId, cancellationToken)");
        Contains(process, "user ??= new SecurityAuthorityUser(");
        Contains(process, "if (string.IsNullOrWhiteSpace(existingUserId)) await operation.Records.AddAsync(user, cancellationToken)");
        Contains(process, "identity = new SecurityAuthorityExternalIdentity(");
    }

    private static void IssuerSubjectIsUniqueAndUsersCannotMerge()
    {
        var process = ProcessCallbackBlock();
        Contains(process, "findExternalIdentity(authentication.Issuer, authentication.Subject, cancellationToken)");
        Contains(process, "!string.Equals(existingUserId, user.Id, StringComparison.Ordinal)");
        Contains(process, "The callback cannot merge two existing Users.");
        Contains(ValidationSource, "var identityKey = record.Issuer + \\\"\\\\u001f\\\" + record.Subject;");
        Contains(ValidationSource, "IsUniqueAsync(\\\"ExternalIdentity\\\", \\\"IssuerSubject\\\", identityKey, record.Id, cancellationToken)");
        Contains(ValidationSource, "Issuer and Subject must be globally unique using ordinal case-sensitive comparison.");
    }

    private static void CallbackModesAreSupported()
    {
        var callback = GetBlock(ProviderSource, ".AddMethod(\"ValueTask<SecurityAuthorityExternalProviderCallback>\", \"ReadCallbackAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityExternalProviderCallbackResult>\", \"ProcessCallbackAsync\"");
        Contains(callback, "HttpMethods.IsGet(request.Method)");
        Contains(callback, "request.Query[\\\"code\\\"]");
        Contains(callback, "request.Query[\\\"state\\\"]");
        Contains(callback, "HttpMethods.IsPost(request.Method) && request.HasFormContentType");
        Contains(callback, "await request.ReadFormAsync(cancellationToken)");
        Contains(callback, "form[\\\"code\\\"]");
        Contains(callback, "form[\\\"state\\\"]");
        Contains(callback, "GET query or POST form_post");
    }

    private static void GenericOidcProtocolIsConcrete()
    {
        var protocol = GetBlock(ProviderSource, ".AddClass(\"SecurityAuthorityOidcExternalProviderProtocol\"", ".AddClass(\"SecurityAuthorityExternalProviders\"");
        foreach (var behavior in new[]
        {
            "/.well-known/openid-configuration",
            "token_endpoint",
            "jwks_uri",
            "FormUrlEncodedContent",
            "secretProtector.Unprotect(request.Provider.EncryptedClientSecret)",
            "RSA.Create()",
            "rsa.VerifyData",
            "HashAlgorithmName.SHA256",
            "RSASignaturePadding.Pkcs1",
            "The external provider ID Token issuer is invalid.",
            "ContainsAudience(payload.RootElement, request.Provider.ClientIdentifier)",
            "request.ExpectedNonce",
            "request.Preset.SubjectClaim",
            "request.Preset.DisplayNameClaim",
            "request.Preset.EmailClaim"
        })
        {
            Contains(protocol, behavior);
        }

        Contains(ProviderSource, "@class.ImplementsInterface(\"ISecurityAuthorityExternalProviderProtocol\")");
    }

    private static void AuthenticationRequiresEveryValidationStage()
    {
        foreach (var validation in new[]
        {
            "DiscoveryValidated",
            "TokenExchangeSucceeded",
            "SignatureValidated",
            "IssuerValidated",
            "AudienceValidated",
            "NonceValidated",
            "RequiredClaimsValidated"
        })
        {
            Contains(ProviderSource, $"ctor.AddParameter(\"bool\", \"{validation}\")");
            Contains(ProviderSource, $"!authentication.{validation}");
        }

        foreach (var requiredClaim in new[] { "Issuer", "Subject", "DisplayName", "NormalizedEmail" })
        {
            Contains(ProviderSource, $"string.IsNullOrWhiteSpace(authentication.{requiredClaim})");
        }

        Contains(ProviderSource, "protocol.AuthenticateAsync(request, secretProtector, cancellationToken)");
        Contains(ProviderSource, "exception is HttpRequestException or CryptographicException or InvalidOperationException");
    }

    private static void CallbackFailuresAreAtomicAndRedirectOnlyWhenSafe()
    {
        var process = ProcessCallbackBlock();
        Contains(process, "BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.IdempotentProvisioning, true, cancellationToken)");
        Contains(process, "return await RollbackFailureAsync(operation");
        Contains(process, "await operation.RollbackAsync(cancellationToken)");
        DoesNotContain(process, "SecurityAuthoritySsoSession");
        DoesNotContain(process, "SecurityAuthorityAuthorizationCode");

        var failure = GetBlock(ProviderSource, ".AddMethod(\"SecurityAuthorityExternalProviderCallbackResult\", \"Failure\"", ".AddMethod(\"string\", \"BuildErrorRedirect\"");
        Contains(failure, "redirectValidated ? BuildErrorRedirect(redirectUri, error, description, returnState) : null");

        var redirect = GetBlock(ProviderSource, ".AddMethod(\"string\", \"BuildErrorRedirect\"", ".AddMethod(\"string\", \"NewOpaqueIdentifier\"");
        Contains(redirect, "Uri.EscapeDataString(error)");
        Contains(redirect, "Uri.EscapeDataString(description)");
        Contains(redirect, "Uri.EscapeDataString(returnState)");
    }

    private static void InactiveProvidersRetainExistingRecords()
    {
        var process = ProcessCallbackBlock();
        var inactiveCheck = process.IndexOf("if (!provider.IsActive)", StringComparison.Ordinal);
        var operationStart = process.IndexOf("BeginAtomicOperationAsync", StringComparison.Ordinal);
        True(inactiveCheck >= 0 && operationStart > inactiveCheck, "Inactive providers must be rejected before persistence begins.");
        DoesNotContain(process, "DeleteAsync");
        DoesNotContain(process, "RemoveAsync");
        Contains(process, "identity = identity with { LastSeenAt = now }");
    }

    private static void ProviderSecretsStayProtected()
    {
        var providerRecord = GetBlock(RecordsSource, ".AddRecord(\"SecurityAuthorityIdentityProvider\"", ".AddRecord(\"SecurityAuthorityApiKey\"");
        Contains(providerRecord, "ctor.AddParameter(\"string\", \"EncryptedClientSecret\")");
        DoesNotContain(providerRecord, "ctor.AddParameter(\"string\", \"ClientSecret\")");

        var projection = GetBlock(ContractsSource, ".AddRecord(\"SecurityAuthorityIdentityProviderProjection\"", ");\n        }");
        DoesNotContain(projection, "EncryptedClientSecret");
        DoesNotContain(projection, "ClientSecret");

        Contains(ProviderSource, "SecurityAuthoritySecretProtector");
        Contains(ProviderSource, "protocol.AuthenticateAsync(request, secretProtector, cancellationToken)");
        DoesNotContain(ProviderSource, "Console.WriteLine");
        DoesNotContain(ProviderSource, "logger.Log");
    }

    private static void ExternalProviderSurfaceContainsNoStubs()
    {
        DoesNotContain(ProviderSource, "NotImplementedException");
        DoesNotContain(ProviderSource, "TODO");
        DoesNotContain(ProviderSource, "exampleParam");
    }

    private static string ProcessCallbackBlock()
    {
        return GetBlock(ProviderSource, ".AddMethod(\"ValueTask<SecurityAuthorityExternalProviderCallbackResult>\", \"ProcessCallbackAsync\"", ".AddMethod(\"ValueTask<SecurityAuthorityExternalProviderCallbackResult>\", \"RollbackFailureAsync\"");
    }

    private static string GetBlock(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        True(start >= 0, $"Marker '{startMarker}' was not found.");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        True(end >= 0, $"Marker '{endMarker}' was not found after '{startMarker}'.");
        return source[start..end];
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
