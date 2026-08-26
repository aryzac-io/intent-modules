using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aryzac.Security.Api;
using Aryzac.Security.Templates.ScopePermissionMap;
using Aryzac.Security.Templates.SecurityContractFoundation;
using Aryzac.Security.Templates.SecurityRegistration;

return await FoundationRegistrationSelfTests.RunAsync();

internal static class FoundationRegistrationSelfTests
{
    private static readonly string SourceRoot = FindSourceRoot();
    private static readonly string SecurityProject = Path.Combine(SourceRoot, "Aryzac.Security");
    private static readonly string FoundationSource = Read("Templates/SecurityContractFoundation/SecurityContractFoundationTemplatePartial.cs");
    private static readonly string RegistrationSource = Read("Templates/SecurityRegistration/SecurityRegistrationTemplatePartial.cs");
    private static readonly string InboundSource = Read("Templates/SecurityInboundCredentials/SecurityInboundCredentialsTemplatePartial.cs");

    public static async Task<int> RunAsync()
    {
        var tests = new (string Name, Func<Task> Execute)[]
        {
            ("preserved module and Scope artifacts", PreservedArtifacts),
            ("one marker and one registration route", OneMarkerAndRegistrationRoute),
            ("independent capability enablement", IndependentCapabilityEnablement),
            ("enabled-only exact safe validation", EnabledOnlyValidation),
            ("Principal shape and immutable Scopes", PrincipalAndScopes),
            ("protected extension points", ProtectedExtensionPoints),
            ("nested child-flowing ambient restoration", AmbientRestoration),
            ("authorization absence, cardinality, and malformed shapes", AuthorizationRejectionShapes),
            ("supported schemes and ordinal API Key prefix", SupportedSchemesAndApiKeyPrefix),
            ("rejections prevent resolution and operation execution", RejectionPreventsExecution),
            ("single Principal and request ambient credential lifetime", RequestPrincipalAndAmbientLifetime)
        };
        var failures = 0;

        foreach (var test in tests)
        {
            try
            {
                await test.Execute();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
            }
        }

        Console.WriteLine($"{tests.Length - failures}/{tests.Length} foundation/registration self-tests passed.");
        return failures == 0 ? 0 : 1;
    }

    private static Task PreservedArtifacts()
    {
        var assembly = typeof(ScopeModel).Assembly;
        Equal("Aryzac.Security", assembly.GetName().Name);
        Equal("Scope", ScopeModel.SpecializationType);
        Equal("33256cb2-6ac1-48e5-b3dd-75c8e83f156e", ScopeModel.SpecializationTypeId);
        NotNull(assembly.GetType("Aryzac.Security.Api.ScopeConfigurationModel"));
        NotNull(assembly.GetType("Aryzac.Security.Api.ScopeVerbModel"));
        NotNull(assembly.GetType("Aryzac.Security.Api.ScopeModelStereotypeExtensions"));
        Equal("Aryzac.Security.ScopePermissionMap", ScopePermissionMapTemplate.TemplateId);
        Equal(ScopePermissionMapTemplate.TemplateId, new ScopePermissionMapTemplateRegistration().TemplateId);

        var moduleSpec = File.ReadAllText(Path.Combine(SecurityProject, "Aryzac.Security.imodspec"));
        Contains(moduleSpec, "<id>Aryzac.Security</id>");
        Contains(moduleSpec, "<version>1.");
        Contains(moduleSpec, "id=\"Aryzac.Security.ScopePermissionMap\"");
        Contains(moduleSpec, "externalReference=\"5413af14-47de-4778-8883-bd789689bc6d\"");
        return Task.CompletedTask;
    }

    private static Task OneMarkerAndRegistrationRoute()
    {
        Equal(1, Count(FoundationSource, ".AddClass(\"SecurityContractServiceMarker\""));
        Equal("Aryzac.Security.SecurityContractFoundation", SecurityContractFoundationTemplate.TemplateId);
        Equal("Aryzac.Security.SecurityRegistration", SecurityRegistrationTemplate.TemplateId);
        Equal(SecurityRegistrationTemplate.TemplateId, new SecurityRegistrationTemplateRegistration().TemplateId);
        Null(typeof(SecurityRegistrationTemplate).Assembly.GetType(
            "Aryzac.Security.Templates.SecurityContract.SecurityContractTemplateRegistration"));
        Equal(1, Count(RegistrationSource, ".AddClass(\"SecurityContractRegistration\""));
        Equal(2, Count(RegistrationSource, ".AddMethod(\"IServiceCollection\", \"AddAryzacSecurity\""));
        DoesNotContain(FoundationSource, "AddAryzacSecurity");
        return Task.CompletedTask;
    }

    private static Task IndependentCapabilityEnablement()
    {
        Contains(RegistrationSource, "if (options.Jwt.Enabled) services.AddSingleton(options.Jwt);");
        Contains(RegistrationSource, "if (options.ApiKey.Enabled) services.AddSingleton(options.ApiKey);");
        Contains(RegistrationSource, "if (options.ServiceToken.Enabled) services.AddSingleton(options.ServiceToken);");
        Contains(RegistrationSource, "options.InternalServiceKey.AdmissionEnabled || options.InternalServiceKey.PresentationEnabled");
        Contains(RegistrationSource, "if (options.Outbound.Enabled) services.AddSingleton(options.Outbound);");
        Contains(RegistrationSource, "if (options.Diagnostics.Enabled) services.AddSingleton(options.Diagnostics);");

        Equal(0, RegistrationFixture.Register(new FixtureOptions()).Count);
        foreach (var capability in Enum.GetValues<Capability>())
        {
            var options = FixtureOptions.Valid();
            options.EnableOnly(capability);
            var registrations = RegistrationFixture.Register(options);
            Equal(1, registrations.Count);
            True(registrations.Contains(capability));
        }

        return Task.CompletedTask;
    }

    private static Task EnabledOnlyValidation()
    {
        string[] exactOptionNames =
            [
            "AryzacSecurityOptions.Jwt.PrimaryRsaPublicKeyPem",
            "AryzacSecurityOptions.Jwt.AllowedIssuers",
            "AryzacSecurityOptions.Jwt.AllowedAudiences",
            "AryzacSecurityOptions.Jwt.ClockSkewSeconds",
            "AryzacSecurityOptions.ApiKey.FormatPrefix",
            "AryzacSecurityOptions.ApiKey.ResolverEndpoint",
            "AryzacSecurityOptions.ApiKey.ResolverTimeoutSeconds",
            "AryzacSecurityOptions.ApiKey.ActiveCacheMaximumSeconds",
            "AryzacSecurityOptions.ApiKey.CacheHmacKey",
            "AryzacSecurityOptions.ApiKey.ResolverAuthentication",
            "AryzacSecurityOptions.InternalServiceKey.Value",
            "AryzacSecurityOptions.InternalServiceKey.Principal.Identifier",
            "AryzacSecurityOptions.InternalServiceKey.Principal.Type",
            "AryzacSecurityOptions.Outbound.ServiceCredentialEndpoint",
            "AryzacSecurityOptions.Outbound.ClientIdentity",
            "AryzacSecurityOptions.Outbound.ClientSecret",
            "AryzacSecurityOptions.Outbound.ReservedServiceScope",
            "AryzacSecurityOptions.Outbound.TimeoutSeconds",
            "AryzacSecurityOptions.Outbound.ExpirySafetyWindowSeconds",
            "AryzacSecurityOptions.Diagnostics.HmacKey"
            ];

        Equal(exactOptionNames.Length, Count(RegistrationSource, "throw InvalidOption("));
        foreach (var optionName in exactOptionNames)
        {
            Contains(RegistrationSource, $"InvalidOption(\\\"{optionName}\\\")");
            var message = $"Security option '{optionName}' is missing or invalid.";
            Contains(message, optionName);
            DoesNotContain(message, "SECRET-SENTINEL-91f3");
        }

        var guardedValidationLines = RegistrationSource
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains("throw InvalidOption(", StringComparison.Ordinal))
            .Select(line => line.Trim())
            .ToArray();
        True(guardedValidationLines.All(line =>
            line.StartsWith("method.AddStatement(\"if (", StringComparison.Ordinal) &&
            line.Contains("options.", StringComparison.Ordinal)));
        Contains(RegistrationSource, "Security option '{optionName}' is missing or invalid.");

        var disabled = new FixtureOptions { Secret = "SECRET-SENTINEL-91f3" };
        Equal(0, RegistrationFixture.Register(disabled).Count);

        foreach (var capability in new[] { Capability.Jwt, Capability.ApiKey, Capability.InternalServiceKey, Capability.Outbound, Capability.Diagnostics })
        {
            var options = FixtureOptions.Valid();
            options.Secret = "SECRET-SENTINEL-91f3";
            options.EnableOnly(capability);
            options.Invalidate(capability);
            var exception = Throws<InvalidOperationException>(() => RegistrationFixture.Register(options));
            DoesNotContain(exception.ToString(), options.Secret);
        }

        return Task.CompletedTask;
    }

    private static Task PrincipalAndScopes()
    {
        foreach (var property in new[] { "Identifier", "Type", "AccountIdentifier", "WorkspaceIdentifier", "Scopes" })
        {
            Contains(FoundationSource, $"\"{property}\"");
        }
        Contains(FoundationSource, "ToImmutableHashSet(EqualityComparer<Scope>.Default)");

        var source = new List<TestScope> { new("orders.read"), new("Orders.Read") };
        var principal = new TestPrincipal("principal-1", "user", "account-1", null, source);
        source.Add(new TestScope("orders.write"));
        var context = new PrincipalContext(principal);

        Equal(2, principal.Scopes.Count);
        False(principal.Scopes.Contains(new TestScope("ORDERS.READ")));
        False(principal.Scopes.Contains(new TestScope("orders.write")));
        Same(principal, context.Principal);
        Same(principal, context.Resolve());
        return Task.CompletedTask;
    }

    private static Task ProtectedExtensionPoints()
    {
        foreach (var extensionPoint in new[]
        {
            "ICredentialResolverAuthentication",
            "ICallerCredentialResolver",
            "ICallerCredentialAcquirer",
            "ISecurityPolicy"
        })
        {
            Equal(1, Count(FoundationSource, $".AddInterface(\"{extensionPoint}\""));
        }
        DoesNotContain(FoundationSource, "SecurityContractRegistration");
        return Task.CompletedTask;
    }

    private static async Task AmbientRestoration()
    {
        Contains(FoundationSource, "AsyncLocal<AmbientCallerCredentialScope?>");
        Contains(FoundationSource, "var previous = _current.Value;");
        Contains(FoundationSource, "() => _current.Value = previous");
        Contains(FoundationSource, "must be disposed in reverse order");

        var accessor = new AmbientFixture();
        var outer = new TestCredential("outer");
        var inner = new TestCredential("inner");
        Null(accessor.Current);

        using (accessor.Push(outer))
        {
            Same(outer, accessor.Current);
            Same(outer, await Task.Run(() => accessor.Current));
            using (accessor.Push(inner))
            {
                Same(inner, accessor.Current);
                await Task.Yield();
                Same(inner, accessor.Current);
            }
            Same(outer, accessor.Current);
        }

        Null(accessor.Current);
        var first = accessor.Push(outer);
        var second = accessor.Push(inner);
        Throws<InvalidOperationException>(first.Dispose);
        second.Dispose();
        first.Dispose();
        Null(accessor.Current);
    }

    private static Task AuthorizationRejectionShapes()
    {
        Contains(InboundSource, "if (!headers.TryGetValue(\\\"Authorization\\\", out var values))");
        Contains(InboundSource, "if (values.Count == 0)");
        Contains(InboundSource, "if (values.Count != 1)");
        Contains(InboundSource, "if (string.IsNullOrWhiteSpace(authorizationValue))");
        Contains(InboundSource, "authorizationValue.Contains(',', StringComparison.Ordinal)");
        Contains(InboundSource, "if (separatorIndex <= 0)");
        Contains(InboundSource, "credentialValue.Length == 0");
        Contains(InboundSource, "credentialValue.IndexOf(' ') >= 0");

        AssertRejected(InboundParserFixture.Parse(null), "missing_credential");
        AssertRejected(InboundParserFixture.Parse([]), "missing_credential");
        AssertRejected(InboundParserFixture.Parse([""]), "missing_credential");
        AssertRejected(InboundParserFixture.Parse(["   "]), "missing_credential");
        AssertRejected(InboundParserFixture.Parse(["Bearer one", "Bearer two"]), "malformed_credential");

        foreach (var malformed in new[]
        {
            "Bearer",
            " Bearer",
            "Bearer ",
            "Bearer one two",
            "Bearer\tone",
            "Bearer one,two",
            "ApiKey key\rvalue",
            "ApiKey key\nvalue"
        })
        {
            AssertRejected(InboundParserFixture.Parse([malformed]), "malformed_credential");
        }

        return Task.CompletedTask;
    }

    private static Task SupportedSchemesAndApiKeyPrefix()
    {
        Contains(InboundSource, "string.Equals(scheme, \\\"Bearer\\\", StringComparison.OrdinalIgnoreCase)");
        Contains(InboundSource, "string.Equals(scheme, \\\"ApiKey\\\", StringComparison.OrdinalIgnoreCase)");
        Contains(InboundSource, "credentialValue.StartsWith(apiKeyOptions.FormatPrefix!, StringComparison.Ordinal)");

        Equal(InboundCredentialKind.Jwt, RequireCredential(InboundParserFixture.Parse(["Bearer token"])).Kind);
        Equal(InboundCredentialKind.Jwt, RequireCredential(InboundParserFixture.Parse(["bEaReR token"])).Kind);
        Equal(InboundCredentialKind.ApiKey, RequireCredential(InboundParserFixture.Parse(["ApiKey ak_value"], true, "ak_")).Kind);
        Equal(InboundCredentialKind.ApiKey, RequireCredential(InboundParserFixture.Parse(["aPiKeY ak_value"], true, "ak_")).Kind);
        Equal(InboundCredentialKind.InternalServiceKey, RequireCredential(InboundParserFixture.Parse(["ApiKey AK_value"], true, "ak_")).Kind);
        Equal(InboundCredentialKind.InternalServiceKey, RequireCredential(InboundParserFixture.Parse(["ApiKey ak_value"], true, "AK_")).Kind);
        AssertRejected(InboundParserFixture.Parse(["Basic value"]), "unsupported_credential_scheme");
        Throws<InvalidOperationException>(() => InboundParserFixture.Parse(["ApiKey value"], true, ""));
        return Task.CompletedTask;
    }

    private static async Task RejectionPreventsExecution()
    {
        Contains(InboundSource, "if (!parseResult.IsSuccess)");
        Contains(InboundSource, "return parseResult.Rejection;");
        Contains(InboundSource, "var resolution = await _credentialResolver.ResolveAsync(credential, cancellationToken);");

        var fixture = new InboundExecutionFixture(new TestPrincipal("principal-1", "user", null, null, []), new AmbientFixture());
        foreach (var values in new string[]?[]
        {
            null,
            [],
            [""],
            ["Bearer one", "Bearer two"],
            ["Bearer"],
            ["Basic value"]
        })
        {
            var outcome = await fixture.ExecuteAsync(values, (_, _) =>
            {
                fixture.OperationExecutions++;
                return ValueTask.CompletedTask;
            });
            NotNull(outcome.RejectionCode);
        }

        Equal(0, fixture.ResolverExecutions);
        Equal(0, fixture.OperationExecutions);
    }

    private static async Task RequestPrincipalAndAmbientLifetime()
    {
        Contains(InboundSource, "var principal = resolution.Principal;");
        Contains(InboundSource, "var principalKey = typeof(SecurityInboundCredentials);");
        Contains(InboundSource, "httpContext.Items[principalKey] = principal;");
        Contains(InboundSource, "using (_ambientCallerCredentialAccessor.Push(credential))");
        Contains(InboundSource, "await operation(principal, cancellationToken);");

        var scopeSource = new List<TestScope> { new("orders.read") };
        var principal = new TestPrincipal("principal-1", "user", "account-1", "workspace-1", scopeSource);
        var accessor = new AmbientFixture();
        var fixture = new InboundExecutionFixture(principal, accessor);
        var outer = new TestCredential("outer");
        var observedPrincipals = new List<TestPrincipal>();

        using (accessor.Push(outer))
        {
            var outcome = await fixture.ExecuteAsync(["Bearer request-token"], async (operationPrincipal, _) =>
            {
                observedPrincipals.Add(operationPrincipal);
                observedPrincipals.Add(fixture.GetPrincipal()!);
                Same(principal, operationPrincipal);
                Same(principal, fixture.GetPrincipal());
                Equal("request-token", accessor.Current?.Value);
                await Task.Yield();
                Same(principal, fixture.GetPrincipal());
                Equal("request-token", accessor.Current?.Value);
            });

            Null(outcome.RejectionCode);
            Same(principal, fixture.ResolvedPrincipal);
            Same(principal, fixture.GetPrincipal());
            Same(outer, accessor.Current);
        }

        scopeSource.Add(new TestScope("orders.write"));
        Equal(1, principal.Scopes.Count);
        False(principal.Scopes.Contains(new TestScope("ORDERS.READ")));
        Equal(1, observedPrincipals.Distinct(ReferenceEqualityComparer.Instance).Count());
        Equal(1, fixture.ResolverExecutions);
        Equal(1, fixture.OperationExecutions);
        Null(accessor.Current);
    }

    private static void AssertRejected(InboundParseResult result, string expectedCode)
    {
        Equal(expectedCode, result.RejectionCode);
        Null(result.Credential);
    }

    private static InboundCredential RequireCredential(InboundParseResult result)
    {
        Null(result.RejectionCode);
        return result.Credential ?? throw new InvalidOperationException("Expected a parsed credential.");
    }

    private static string Read(string relativePath)
    {
        return File.ReadAllText(Path.Combine(SecurityProject, relativePath));
    }

    private static string FindSourceRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? directory = new(start);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Aryzac.Security"))) return directory.FullName;
                if (Directory.Exists(Path.Combine(directory.FullName, "src", "Aryzac.Security"))) return Path.Combine(directory.FullName, "src");
                directory = directory.Parent;
            }
        }
        throw new InvalidOperationException("Could not locate the repository source root.");
    }

    private static int Count(string value, string fragment)
    {
        var count = 0;
        for (var index = 0; (index = value.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0; index += fragment.Length) count++;
        return count;
    }

    private static TException Throws<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException exception) { return exception; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static void Contains(string value, string expected) => True(value.Contains(expected, StringComparison.Ordinal), $"Expected '{expected}'.");
    private static void DoesNotContain(string value, string unexpected) => True(!value.Contains(unexpected, StringComparison.Ordinal), $"Unexpected '{unexpected}'.");
    private static void Equal<T>(T expected, T actual) => True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected '{expected}', got '{actual}'.");
    private static void Same(object? expected, object? actual) => True(ReferenceEquals(expected, actual), "Expected the same instance.");
    private static void Null(object? value) => True(value is null, "Expected null.");
    private static void NotNull(object? value) => True(value is not null, "Expected non-null.");
    private static void False(bool condition) => True(!condition, "Expected false.");
    private static void True(bool condition, string message = "Expected true.")
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

internal enum Capability { Jwt, ApiKey, ServiceToken, InternalServiceKey, Outbound, Diagnostics }

internal sealed class FixtureOptions
{
    public Capability? Enabled { get; private set; }
    public string? JwtKey { get; set; }
    public string? ApiKeyPrefix { get; set; }
    public string? InternalKey { get; set; }
    public string? InternalPrincipalIdentifier { get; set; }
    public string? InternalPrincipalType { get; set; }
    public Uri? OutboundEndpoint { get; set; }
    public string? OutboundClientIdentity { get; set; }
    public string? OutboundClientSecret { get; set; }
    public string? OutboundScope { get; set; }
    public byte[]? DiagnosticsKey { get; set; }
    public string Secret { get; set; } = string.Empty;

    public static FixtureOptions Valid() => new()
    {
        JwtKey = "PUBLIC-KEY",
        ApiKeyPrefix = "ak_",
        InternalKey = "INTERNAL-KEY",
        InternalPrincipalIdentifier = "service-1",
        InternalPrincipalType = "service",
        OutboundEndpoint = new Uri("https://security.example/token"),
        OutboundClientIdentity = "client-1",
        OutboundClientSecret = "CLIENT-SECRET",
        OutboundScope = "service",
        DiagnosticsKey = [1]
    };

    public void EnableOnly(Capability capability) => Enabled = capability;

    public void Invalidate(Capability capability)
    {
        switch (capability)
        {
            case Capability.Jwt: JwtKey = null; break;
            case Capability.ApiKey: ApiKeyPrefix = null; break;
            case Capability.InternalServiceKey: InternalKey = null; break;
            case Capability.Outbound: OutboundClientSecret = null; break;
            case Capability.Diagnostics: DiagnosticsKey = null; break;
        }
    }
}

internal static class RegistrationFixture
{
    public static HashSet<Capability> Register(FixtureOptions options)
    {
        if (options.Enabled == Capability.Jwt && string.IsNullOrWhiteSpace(options.JwtKey)) throw Invalid("AryzacSecurityOptions.Jwt.PrimaryRsaPublicKeyPem");
        if (options.Enabled == Capability.ApiKey && string.IsNullOrWhiteSpace(options.ApiKeyPrefix)) throw Invalid("AryzacSecurityOptions.ApiKey.FormatPrefix");
        if (options.Enabled == Capability.InternalServiceKey && string.IsNullOrWhiteSpace(options.InternalKey)) throw Invalid("AryzacSecurityOptions.InternalServiceKey.Value");
        if (options.Enabled == Capability.Outbound && string.IsNullOrWhiteSpace(options.OutboundClientSecret)) throw Invalid("AryzacSecurityOptions.Outbound.ClientSecret");
        if (options.Enabled == Capability.Diagnostics && (options.DiagnosticsKey is null || options.DiagnosticsKey.Length == 0)) throw Invalid("AryzacSecurityOptions.Diagnostics.HmacKey");
        return options.Enabled is null ? [] : [options.Enabled.Value];
    }

    private static InvalidOperationException Invalid(string optionName) => new($"Security option '{optionName}' is missing or invalid.");
}

internal enum InboundCredentialKind { Jwt, ApiKey, InternalServiceKey }
internal sealed record InboundCredential(InboundCredentialKind Kind, string Scheme, ImmutableArray<byte> Value)
{
    public string Text => Encoding.UTF8.GetString(Value.AsSpan());
}
internal sealed record InboundParseResult(InboundCredential? Credential, string? RejectionCode);

internal static class InboundParserFixture
{
    public static InboundParseResult Parse(string[]? values, bool apiKeyEnabled = false, string? apiKeyPrefix = null)
    {
        if (values is null || values.Length == 0 || (values.Length == 1 && string.IsNullOrWhiteSpace(values[0])))
        {
            return Reject("missing_credential");
        }
        if (values.Length != 1)
        {
            return Reject("malformed_credential");
        }

        var authorizationValue = values[0];
        if (authorizationValue.Contains(',', StringComparison.Ordinal))
        {
            return Reject("malformed_credential");
        }
        var separatorIndex = authorizationValue.IndexOf(' ');
        if (separatorIndex <= 0)
        {
            return Reject("malformed_credential");
        }

        var scheme = authorizationValue[..separatorIndex];
        var credentialValue = authorizationValue[(separatorIndex + 1)..].Trim();
        if (credentialValue.Length == 0 || credentialValue.IndexOfAny([' ', '\t', '\r', '\n']) >= 0)
        {
            return Reject("malformed_credential");
        }

        InboundCredentialKind kind;
        if (string.Equals(scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            kind = InboundCredentialKind.Jwt;
        }
        else if (string.Equals(scheme, "ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            if (apiKeyEnabled && string.IsNullOrWhiteSpace(apiKeyPrefix))
            {
                throw new InvalidOperationException("Security option 'AryzacSecurityOptions.ApiKey.FormatPrefix' is missing or invalid.");
            }
            kind = apiKeyEnabled && credentialValue.StartsWith(apiKeyPrefix!, StringComparison.Ordinal)
                ? InboundCredentialKind.ApiKey
                : InboundCredentialKind.InternalServiceKey;
        }
        else
        {
            return Reject("unsupported_credential_scheme");
        }

        return new InboundParseResult(new InboundCredential(kind, scheme, Encoding.UTF8.GetBytes(credentialValue).ToImmutableArray()), null);
    }

    private static InboundParseResult Reject(string code) => new(null, code);
}

internal sealed class InboundExecutionFixture(TestPrincipal resolvedPrincipal, AmbientFixture ambient)
{
    private readonly Dictionary<object, object> _requestItems = [];
    public int ResolverExecutions { get; private set; }
    public int OperationExecutions { get; set; }
    public TestPrincipal? ResolvedPrincipal { get; private set; }

    public TestPrincipal? GetPrincipal() => _requestItems.TryGetValue(typeof(InboundExecutionFixture), out var value) ? value as TestPrincipal : null;

    public async ValueTask<InboundExecutionResult> ExecuteAsync(
        string[]? authorizationValues,
        Func<TestPrincipal, CancellationToken, ValueTask> operation)
    {
        var parseResult = InboundParserFixture.Parse(authorizationValues, true, "ak_");
        if (parseResult.Credential is null)
        {
            return new InboundExecutionResult(parseResult.RejectionCode);
        }

        ResolverExecutions++;
        ResolvedPrincipal = resolvedPrincipal;
        _requestItems[typeof(InboundExecutionFixture)] = resolvedPrincipal;
        using (ambient.Push(new TestCredential(parseResult.Credential.Text)))
        {
            OperationExecutions++;
            await operation(resolvedPrincipal, CancellationToken.None);
        }
        return new InboundExecutionResult(null);
    }
}

internal sealed record InboundExecutionResult(string? RejectionCode);
internal sealed record TestScope(string Value);
internal sealed record TestPrincipal(string Identifier, string Type, string? AccountIdentifier, string? WorkspaceIdentifier, IEnumerable<TestScope> Source)
{
    public ImmutableHashSet<TestScope> Scopes { get; } = Source.ToImmutableHashSet(EqualityComparer<TestScope>.Default);
}
internal sealed class PrincipalContext(TestPrincipal principal)
{
    public TestPrincipal Principal { get; } = principal;
    public TestPrincipal Resolve() => Principal;
}
internal sealed record TestCredential(string Value);

internal sealed class AmbientFixture
{
    private static readonly AsyncLocal<AmbientScope?> CurrentScope = new();
    public TestCredential? Current => CurrentScope.Value?.Credential;

    public IDisposable Push(TestCredential credential)
    {
        var previous = CurrentScope.Value;
        AmbientScope? scope = null;
        scope = new AmbientScope(credential, () => ReferenceEquals(CurrentScope.Value, scope), () => CurrentScope.Value = previous);
        CurrentScope.Value = scope;
        return scope;
    }

    private sealed class AmbientScope(TestCredential credential, Func<bool> isCurrent, Action restore) : IDisposable
    {
        private bool _disposed;
        public TestCredential Credential { get; } = credential;
        public void Dispose()
        {
            if (_disposed) return;
            if (!isCurrent()) throw new InvalidOperationException("Ambient caller credential scopes must be disposed in reverse order.");
            restore();
            _disposed = true;
        }
    }
}
