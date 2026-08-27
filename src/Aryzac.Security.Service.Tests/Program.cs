using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Aryzac.Security.Service.Settings;

var integrationResult = AuthorityIntegrationSelfTests.Run();
var coreContractResult = CoreAuthorityContractSelfTests.Run();
var cryptographicMaterialResult = CryptographicMaterialSelfTests.Run();
var discoveryAndOAuthClientValidationResult = DiscoveryAndOAuthClientValidationSelfTests.Run();
var sessionEndpointsResult = SessionEndpointsSelfTests.Run();
var externalProviderResult = ExternalProviderSelfTests.Run();
var authorizationCodeAndTokenEndpointResult = AuthorizationCodeAndTokenEndpointSelfTests.Run();
var refreshTokenAndDeviceAuthorizationResult = RefreshTokenAndDeviceAuthorizationSelfTests.Run();
var authorizationEngineResult = AuthorizationEngineSelfTests.Run();
var apiKeyManagementResult = ApiKeyManagementSelfTests.Run();
var bootstrapUserAndServiceLifecycleResult = BootstrapUserAndServiceLifecycleSelfTests.Run();
var managementApiContractResult = ManagementApiContractSelfTests.Run();
var integrationDeliveryCleanupResult = IntegrationDeliveryCleanupSelfTests.Run();
var conformanceKitResult = ConformanceKitSelfTests.Run();
return integrationResult == 0 && coreContractResult == 0 && cryptographicMaterialResult == 0 && discoveryAndOAuthClientValidationResult == 0 && sessionEndpointsResult == 0 && externalProviderResult == 0 && authorizationCodeAndTokenEndpointResult == 0 && refreshTokenAndDeviceAuthorizationResult == 0 && authorizationEngineResult == 0 && apiKeyManagementResult == 0 && bootstrapUserAndServiceLifecycleResult == 0 && managementApiContractResult == 0 && integrationDeliveryCleanupResult == 0 && conformanceKitResult == 0 ? 0 : 1;

internal static class AuthorityIntegrationSelfTests
{
    private static readonly IntegrationApi Integration = new();
    private static readonly string ServiceProject = FindServiceProject();

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("dedicated-host installation", DedicatedHostInstallation),
            ("existing-host additive preservation", ExistingHostPreservesRegistrations),
            ("conflict diagnostics name both registrations", ConflictDiagnosticsNameBothRegistrations),
            ("companion and host capability diagnostics", CompanionAndHostCapabilityDiagnostics),
            ("persistence guarantees and transaction participation", PersistenceGuaranteesAndTransactionParticipation),
            ("tenant adapter is conditionally required", TenantAdapterIsConditionallyRequired),
            ("atomic persistence request is complete", AtomicPersistenceRequestIsComplete),
            ("credential reveal is commit gated", CredentialRevealIsCommitGated),
            ("all authority feature combinations", AllAuthorityFeatureCombinations),
            ("invalid refresh-token combinations", InvalidRefreshTokenCombinations),
            ("repeat generation is idempotent", RepeatGenerationIsIdempotent),
            ("factory extensions retain intended lifecycle wiring", FactoryExtensionsRetainLifecycleWiring)
        };
        var failures = 0;

        foreach (var test in tests)
        {
            try
            {
                test.Execute();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception exception)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {test.Name}: {exception}");
            }
        }

        Console.WriteLine($"{tests.Length - failures}/{tests.Length} Security Authority integration self-tests passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void DedicatedHostInstallation()
    {
        var fixture = AuthorityHostFixture.Dedicated();

        ValidateInstallation(fixture);
        var proposal = fixture.PublishAuthorityProposal();

        Equal(1, proposal.ConfigurationKeys.Count);
        Equal("SecurityAuthority", proposal.ConfigurationKeys.Single());
        Equal(1, proposal.ServiceRegistrations.Count);
        Equal("AddSecurityAuthority", proposal.ServiceRegistrations.Single());
        Equal(1, proposal.MiddlewareRegistrations.Count);
        Equal("UseSecurityAuthority", proposal.MiddlewareRegistrations.Single());
        Equal(9, proposal.Features.Count);
        True(proposal.Routes.Contains("/.well-known/openid-configuration"));
        True(proposal.Routes.Contains("/.well-known/jwks.json"));
        True(proposal.Routes.Contains("/connect/authorize"));
        True(proposal.Routes.Contains("/connect/token"));
    }

    private static void ExistingHostPreservesRegistrations()
    {
        var fixture = AuthorityHostFixture.Existing();
        var originalRegistrations = fixture.HostRegistrations.ToArray();

        ValidateInstallation(fixture);
        var proposal = fixture.PublishAuthorityProposal();

        SequenceEqual(originalRegistrations, fixture.HostRegistrations);
        ContainsRegistration(fixture, "route:/orders", "existing orders route");
        ContainsRegistration(fixture, "action:CreateOrder", "existing create-order action");
        ContainsRegistration(fixture, "scheme:HostCookie", "existing cookie scheme");
        ContainsRegistration(fixture, "policy:Orders.Read", "existing orders policy");
        ContainsRegistration(fixture, "persistence:OrdersDb", "existing orders persistence mapping");
        ContainsRegistration(fixture, "middleware:UseHostTelemetry", "existing telemetry middleware");
        ContainsRegistration(fixture, "service:AddOrderProcessing", "existing order business service");
        SequenceEqual(["AddSecurityAuthority"], proposal.ServiceRegistrations);
        SequenceEqual(["UseSecurityAuthority"], proposal.MiddlewareRegistrations);
    }

    private static void ConflictDiagnosticsNameBothRegistrations()
    {
        AssertConflict(
            "configuration:SecurityAuthority",
            "host configuration 'SecurityAuthority'",
            "ValidateRegistrationConflicts",
            "Security Authority configuration");
        AssertConflict(
            "scheme:SecurityAuthority",
            "host authentication scheme 'SecurityAuthority'",
            "ValidateRegistrationConflicts",
            "Security Authority authentication scheme 'SecurityAuthority'");
        AssertConflict(
            "persistence:SecurityAuthorityPersistence",
            "host persistence mapping 'SecurityAuthorityPersistence'",
            "ValidateRegistrationConflicts",
            "Security Authority persistence registration 'SecurityAuthorityPersistence'");
        AssertConflict(
            "service:AddSecurityAuthority",
            "host business service 'AddSecurityAuthority'",
            "ValidateRegistrationConflicts",
            "Security Authority service registration 'AddSecurityAuthority'");
        AssertConflict(
            "route:/connect/token",
            "host token route '/connect/token'",
            "ValidateMiddlewareConflicts",
            "Security Authority route '/connect/token'");
    }

    private static void CompanionAndHostCapabilityDiagnostics()
    {
        var missingCompanion = AuthorityHostFixture.Dedicated();
        missingCompanion.Modules.Clear();
        var exception = Throws(() => Integration.Invoke("ValidateCompanionModule", missingCompanion.Application));
        Contains(exception.Message, "requires the Aryzac.Security companion module");
        Contains(exception.Message, "Install Aryzac.Security version 1.0.2-pre.0 or later");

        foreach (var version in new[] { "1.0.1", "1.0.2-alpha.9", "2.0.0" })
        {
            var incompatible = AuthorityHostFixture.Dedicated();
            incompatible.Modules[0] = new HostModule("Aryzac.Security", version);
            exception = Throws(() => Integration.Invoke("ValidateCompanionModule", incompatible.Application));
            Contains(exception.Message, version);
            Contains(exception.Message, "Update Aryzac.Security to a compatible version");
        }

        var missingHttpHost = AuthorityHostFixture.Dedicated();
        missingHttpHost.Capabilities.Remove("aspnet-core");
        exception = Throws(() => Integration.Invoke("ValidateAspNetCoreHost", missingHttpHost.DiscoverCapabilities()));
        Contains(exception.Message, "requires an ASP.NET Core HTTP host capability");
        Contains(exception.Message, "Install or enable the host module");

        var missingPersistence = AuthorityHostFixture.Dedicated();
        missingPersistence.Capabilities.Remove("persistence");
        exception = Throws(() => Integration.Invoke("ValidatePersistenceCapability", missingPersistence.DiscoverCapabilities()));
        Contains(exception.Message, "requires a host persistence capability");
        Contains(exception.Message, "Configure a persistence integration");

        foreach (var capability in new[]
        {
            "persistence-uniqueness",
            "persistence-optimistic-concurrency",
            "persistence-atomic-credential-rotation",
            "persistence-one-time-redemption"
        })
        {
            var incompatiblePersistence = AuthorityHostFixture.Dedicated();
            incompatiblePersistence.Capabilities[capability] = "false";
            exception = Throws(() => Integration.Invoke(
                "ValidatePersistenceGuarantees",
                incompatiblePersistence.DiscoverCapabilities()));
            Contains(exception.Message, "Security Authority persistence registration 'TestPersistence'");
            Contains(exception.Message, "Select or configure a persistence integration");
        }
    }

    private static void PersistenceGuaranteesAndTransactionParticipation()
    {
        foreach (var capability in new[]
        {
            "persistence-authority-record-isolation",
            "persistence-uniqueness",
            "persistence-optimistic-concurrency",
            "persistence-atomic-credential-rotation",
            "persistence-one-time-redemption"
        })
        {
            var fixture = AuthorityHostFixture.Dedicated();
            fixture.Capabilities[capability] = "false";
            var exception = Throws(() => Integration.Invoke(
                "ValidatePersistenceGuarantees",
                fixture.DiscoverCapabilities()));
            Contains(exception.Message, "Security Authority persistence registration 'TestPersistence'");
            Contains(exception.Message, "Select or configure a persistence integration");
        }

        var transactional = AuthorityHostFixture.Dedicated();
        transactional.Capabilities["persistence-transactions"] = "true";
        transactional.Capabilities["persistence-host-transaction-participation"] = "false";
        var transactionException = Throws(() => Integration.Invoke(
            "ValidatePersistenceGuarantees",
            transactional.DiscoverCapabilities()));
        Contains(transactionException.Message, "host transaction participation");

        var nonTransactional = AuthorityHostFixture.Dedicated();
        nonTransactional.Capabilities["persistence-transactions"] = "false";
        nonTransactional.Capabilities.Remove("persistence-host-transaction-participation");
        Integration.Invoke("ValidatePersistenceGuarantees", nonTransactional.DiscoverCapabilities());
    }

    private static void TenantAdapterIsConditionallyRequired()
    {
        var tenantNeutral = AuthorityHostFixture.Dedicated();
        tenantNeutral.Capabilities.Remove("tenant-adapter");
        Integration.Invoke("ValidateTenantAdapter", tenantNeutral.Application, tenantNeutral.DiscoverCapabilities());

        var tenantScoped = AuthorityHostFixture.Dedicated();
        tenantScoped.Settings.SetTenantScopedCapabilitiesEnabled(true);
        tenantScoped.Capabilities.Remove("tenant-adapter");
        var exception = Throws(() => Integration.Invoke(
            "ValidateTenantAdapter",
            tenantScoped.Application,
            tenantScoped.DiscoverCapabilities()));
        Contains(exception.Message, "ISecurityAuthorityTenantAdapter");
    }

    private static void AtomicPersistenceRequestIsComplete()
    {
        var fixture = AuthorityHostFixture.Dedicated();
        var persistence = fixture.PublishNamedRequest(
            "PublishPersistenceRequest",
            "RegisterAuthorityPersistence");
        var startup = fixture.PublishNamedRequest(
            "PublishStartupValidationRequest",
            "RegisterStartupValidation");

        Equal("ISecurityAuthorityPersistence", persistence["persistence-contract"]);
        Equal("ISecurityAuthorityRecordStore", persistence["record-store-contract"]);
        Equal("ISecurityAuthorityAtomicOperation", persistence["atomic-operation-contract"]);
        Equal("SecurityAuthority", persistence["authority-record-scope"]);
        Equal("true", persistence["join-host-transaction-when-supported"]);
        Equal("true", persistence["defer-credential-reveal-until-commit"]);
        Equal("true", persistence["rollback-on-pre-commit-failure"]);

        foreach (var operation in new[]
        {
            "token-redemption",
            "refresh-token-rotation",
            "api-key-regeneration",
            "first-administrator-bootstrap",
            "idempotent-provisioning",
            "integration-event-deduplication"
        })
        {
            Equal("true", persistence[$"atomic-operation:{operation}"]);
            Equal("true", startup[$"atomic-operation:{operation}"]);
        }
    }

    private static void CredentialRevealIsCommitGated()
    {
        var source = File.ReadAllText(Path.Combine(
            ServiceProject,
            "Templates",
            "SecurityAuthorityContracts",
            "SecurityAuthorityContractsTemplatePartial.cs"));

        foreach (var operationKind in new[]
        {
            "TokenRedemption",
            "RefreshTokenRotation",
            "ApiKeyRegeneration",
            "FirstAdministratorBootstrap",
            "IdempotentProvisioning",
            "IntegrationEventDeduplication"
        })
        {
            Equal(1, Count(source, $"@enum.AddLiteral(\"{operationKind}\")"));
        }

        Equal(1, Count(source, "if (receipt.OperationId != OperationId) throw new InvalidOperationException"));
        Equal(1, Count(source, "A credential can only be revealed after its atomic operation commits."));
        Equal(1, Count(source, "Interlocked.Exchange(ref _revealed, 1)"));
        Equal(1, Count(source, "A committed credential can only be revealed once."));
        Equal(1, Count(source, "ValueTask<SecurityAuthorityCommitReceipt>\", \"CommitAsync"));
        Equal(1, Count(source, "ValueTask\", \"RollbackAsync"));
    }

    private static void AllAuthorityFeatureCombinations()
    {
        for (var mask = 0; mask < 1 << AuthoritySettings.FeatureNames.Length; mask++)
        {
            var fixture = AuthorityHostFixture.Dedicated();
            fixture.Settings.SetFeatureMask(mask);
            var proposal = fixture.PublishAuthorityProposal();

            Equal(AuthoritySettings.FeatureNames.Length, proposal.Features.Count);
            for (var index = 0; index < AuthoritySettings.FeatureNames.Length; index++)
            {
                Equal((mask & (1 << index)) != 0, proposal.Features[AuthoritySettings.FeatureNames[index]]);
            }

            True(proposal.Routes.Contains("/.well-known/openid-configuration"));
            True(proposal.Routes.Contains("/.well-known/jwks.json"));
            Equal((mask & AuthoritySettings.AuthorizationCodeMask) != 0, proposal.Routes.Contains("/connect/authorize"));
            Equal((mask & AuthoritySettings.DeviceAuthorizationMask) != 0, proposal.Routes.Contains("/connect/device"));
            Equal((mask & AuthoritySettings.ManagementApisMask) != 0, proposal.Routes.Contains("/authority/manage"));
        }
    }

    private static void InvalidRefreshTokenCombinations()
    {
        var remainingFeatureIndexes = Enumerable.Range(0, AuthoritySettings.FeatureNames.Length)
            .Where(index => index is not AuthoritySettings.AuthorizationCodeIndex
                and not AuthoritySettings.DeviceAuthorizationIndex
                and not AuthoritySettings.RefreshTokenIndex)
            .ToArray();

        for (var mask = 0; mask < 1 << remainingFeatureIndexes.Length; mask++)
        {
            var fixture = AuthorityHostFixture.Dedicated();
            fixture.Settings.SetAllFeatures(false);
            fixture.Settings.SetFeature("refresh-token", true);
            for (var index = 0; index < remainingFeatureIndexes.Length; index++)
            {
                fixture.Settings.SetFeature(
                    AuthoritySettings.FeatureNames[remainingFeatureIndexes[index]],
                    (mask & (1 << index)) != 0);
            }

            var exception = Throws(() => Integration.Invoke("ValidateSettings", fixture.Application));
            Contains(exception.Message, "enables Refresh Token");
            Contains(exception.Message, "both Authorization Code and Device Authorization are disabled");
            Contains(exception.Message, "Enable Authorization Code or Device Authorization, or disable Refresh Token");
        }
    }

    private static void RepeatGenerationIsIdempotent()
    {
        var fixture = AuthorityHostFixture.Existing();
        var originalHostRegistrations = fixture.HostRegistrations.ToArray();
        var first = fixture.PublishAuthorityProposal();
        var second = fixture.PublishAuthorityProposal();

        SequenceEqual(first.Fingerprints, second.Fingerprints);
        SequenceEqual(originalHostRegistrations, fixture.HostRegistrations);
        Equal(first.ConfigurationKeys.Count, second.ConfigurationKeys.Count);
        Equal(first.ServiceRegistrations.Count, second.ServiceRegistrations.Count);
        Equal(first.MiddlewareRegistrations.Count, second.MiddlewareRegistrations.Count);
        Equal(first.Routes.Count, second.Routes.Count);
        Equal(first.Fingerprints.Count, first.Fingerprints.Distinct(StringComparer.Ordinal).Count());
        Equal(first.Routes.Count, first.Routes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Equal(first.Features.Count, first.Features.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Equal(first.ConfigurationKeys.Count, first.ConfigurationKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Equal(first.ServiceRegistrations.Count, first.ServiceRegistrations.Distinct(StringComparer.Ordinal).Count());
        Equal(first.MiddlewareRegistrations.Count, first.MiddlewareRegistrations.Distinct(StringComparer.Ordinal).Count());

        var moduleSpec = XDocument.Load(Path.Combine(ServiceProject, "Aryzac.Security.Service.imodspec"));
        var extensionIds = moduleSpec.Descendants("factoryExtension")
            .Select(element => (string?)element.Attribute("id"))
            .Where(id => id is not null)
            .Cast<string>()
            .ToArray();
        Equal(extensionIds.Length, extensionIds.Distinct(StringComparer.Ordinal).Count());
        Equal(5, extensionIds.Count(id => id.StartsWith("Aryzac.Security.Service.SecurityAuthority", StringComparison.Ordinal)));
        Equal(20, moduleSpec.Descendants("template").Count());
    }


    private static void FactoryExtensionsRetainLifecycleWiring()
    {
        var expectedCalls = new Dictionary<string, string[]>
        {
            ["SecurityAuthorityModuleDependencyValidationExtension.cs"] =
            [
            "ValidateCompanionModule(application)",
            "ValidateAspNetCoreHost(capabilities)",
            "ValidatePersistenceCapability(capabilities)"
            ],
            ["SecurityAuthorityConfigurationValidationExtension.cs"] =
            [
            "ValidateSettings(application)",
            "ValidatePersistenceGuarantees(capabilities)",
            "PublishStartupValidationRequest(application)"
            ],
            ["SecurityAuthorityPersistenceExtension.cs"] =
            [
            "ValidatePersistenceCapability(capabilities)",
            "ValidatePersistenceGuarantees(capabilities)",
            "ValidateTenantAdapter(application, capabilities)",
            "PublishPersistenceRequest(application)"
            ],
            ["SecurityAuthorityRegistrationExtension.cs"] =
            [
            "ValidateRegistrationConflicts(application)",
            "PublishRegistrationRequests(application)"
            ],
            ["SecurityAuthorityMiddlewareExtension.cs"] =
            [
            "ValidateMiddlewareConflicts(application)",
            "PublishMiddlewareRequest(application)"
            ]
        };

        foreach (var pair in expectedCalls)
        {
            var source = File.ReadAllText(Path.Combine(ServiceProject, "FactoryExtensions", pair.Key));
            Equal(1, Count(source, "SecurityAuthorityIntegration.Initialize(application)"));
            foreach (var expectedCall in pair.Value)
            {
                Equal(1, Count(source, $"SecurityAuthorityIntegration.{expectedCall}"));
            }
        }
    }

    private static void ValidateInstallation(AuthorityHostFixture fixture)
    {
        Integration.Invoke("ValidateCompanionModule", fixture.Application);
        var capabilities = fixture.DiscoverCapabilities();
        Integration.Invoke("ValidateAspNetCoreHost", capabilities);
        Integration.Invoke("ValidatePersistenceCapability", capabilities);
        Integration.Invoke("ValidatePersistenceGuarantees", capabilities);
        Integration.Invoke("ValidateSettings", fixture.Application);
        Integration.Invoke("ValidateTenantAdapter", fixture.Application, capabilities);
        Integration.Invoke("ValidateProductionCryptography", capabilities);
        Integration.Invoke("ValidateRegistrationConflicts", fixture.Application);
        Integration.Invoke("ValidateMiddlewareConflicts", fixture.Application);
    }

    private static void AssertConflict(
        string registrationKey,
        string existingRegistration,
        string methodName,
        string proposedRegistration)
    {
        var fixture = AuthorityHostFixture.Dedicated();
        fixture.HostRegistrations.Add(new HostRegistration(registrationKey, existingRegistration));
        var exception = Throws(() => Integration.Invoke(methodName, fixture.Application));

        Contains(exception.Message, existingRegistration);
        Contains(exception.Message, proposedRegistration);
        Contains(exception.Message, "Neither registration was replaced");
        ContainsRegistration(fixture, registrationKey, existingRegistration);
    }

    private static void ContainsRegistration(AuthorityHostFixture fixture, string key, string description)
    {
        True(fixture.HostRegistrations.Contains(new HostRegistration(key, description)));
    }

    private static string FindServiceProject()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? directory = new(start);
            while (directory is not null)
            {
                var direct = Path.Combine(directory.FullName, "Aryzac.Security.Service");
                if (Directory.Exists(direct))
                {
                    return direct;
                }

                var nested = Path.Combine(directory.FullName, "src", "Aryzac.Security.Service");
                if (Directory.Exists(nested))
                {
                    return nested;
                }

                directory = directory.Parent;
            }
        }

        throw new InvalidOperationException("Could not locate Aryzac.Security.Service.");
    }

    private static int Count(string value, string fragment)
    {
        var count = 0;
        for (var index = 0; (index = value.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0; index += fragment.Length)
        {
            count++;
        }

        return count;
    }

    private static Exception Throws(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("Expected an exception.");
    }

    private static void Contains(string value, string expected) =>
        True(value.Contains(expected, StringComparison.Ordinal), $"Expected '{expected}' in '{value}'.");

    private static void Equal<T>(T expected, T actual) =>
        True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected '{expected}', got '{actual}'.");

    private static void Equal<T>(IReadOnlyCollection<T> expected, IReadOnlyCollection<T> actual) =>
        SequenceEqual(expected, actual);

    private static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual) =>
        True(expected.SequenceEqual(actual), "Expected sequences to be equal.");

    private static void True(bool condition, string message = "Expected true.")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

internal sealed class IntegrationApi
{
    private readonly Type _type = typeof(AuthorityFeatures).Assembly.GetType(
        "Aryzac.Security.Service.FactoryExtensions.SecurityAuthorityIntegration",
        throwOnError: true)!;

    public Type ApplicationType => _type.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static)!
        .GetParameters()[0].ParameterType;

    public object? Invoke(string methodName, params object?[] arguments)
    {
        var method = _type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Could not find {methodName}.");
        try
        {
            return method.Invoke(null, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }
}

internal sealed class AuthorityHostFixture
{
    private readonly IntegrationApi _integration = new();
    private readonly EventDispatcherFixture _events;

    private AuthorityHostFixture()
    {
        var applicationType = _integration.ApplicationType;
        Settings = AuthoritySettings.Valid(FindProperty(applicationType, "Settings").PropertyType);
        Modules.Add(new HostModule("Aryzac.Security", "1.0.2-pre.0"));
        Capabilities["aspnet-core"] = "TestHost";
        Capabilities["persistence"] = "TestPersistence";
        Capabilities["persistence-authority-record-isolation"] = "true";
        Capabilities["persistence-uniqueness"] = "true";
        Capabilities["persistence-optimistic-concurrency"] = "true";
        Capabilities["persistence-atomic-credential-rotation"] = "true";
        Capabilities["persistence-one-time-redemption"] = "true";
        Capabilities["persistence-transactions"] = "true";
        Capabilities["persistence-host-transaction-participation"] = "true";
        Capabilities["production-environment"] = "false";
        Capabilities["production-cryptography"] = "false";
        Capabilities["tenant-adapter"] = "TestTenantAdapter";

        _events = new EventDispatcherFixture(
            FindProperty(applicationType, "EventDispatcher").PropertyType,
            OnNamedPublication);
        Application = DynamicProxy.Create(
            _integration.ApplicationType,
            InvokeApplication);
    }

    private static PropertyInfo FindProperty(Type interfaceType, string propertyName)
    {
        return interfaceType.GetInterfaces()
            .Append(interfaceType)
            .Select(type => type.GetProperty(propertyName))
            .FirstOrDefault(property => property is not null)
            ?? throw new InvalidOperationException($"{interfaceType.FullName} has no {propertyName} property.");
    }

    public object Application { get; }
    public AuthoritySettings Settings { get; }
    public List<HostModule> Modules { get; } = [];
    public Dictionary<string, string> Capabilities { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<HostRegistration> HostRegistrations { get; } = [];

    public static AuthorityHostFixture Dedicated() => new();

    public static AuthorityHostFixture Existing()
    {
        var fixture = new AuthorityHostFixture();
        fixture.HostRegistrations.AddRange(
            [
            new HostRegistration("route:/orders", "existing orders route"),
            new HostRegistration("action:CreateOrder", "existing create-order action"),
            new HostRegistration("scheme:HostCookie", "existing cookie scheme"),
            new HostRegistration("policy:Orders.Read", "existing orders policy"),
            new HostRegistration("persistence:OrdersDb", "existing orders persistence mapping"),
            new HostRegistration("middleware:UseHostTelemetry", "existing telemetry middleware"),
            new HostRegistration("service:AddOrderProcessing", "existing order business service")
            ]);
        return fixture;
    }

    public IReadOnlyDictionary<string, string> DiscoverCapabilities()
    {
        return (IReadOnlyDictionary<string, string>)_integration.Invoke("DiscoverCapabilities", Application)!;
    }

    public AuthorityProposal PublishAuthorityProposal()
    {
        _integration.Invoke("Initialize", Application);
        _integration.Invoke("PublishRegistrationRequests", Application);
        _integration.Invoke("PublishMiddlewareRequest", Application);
        _integration.Invoke("PublishStartupValidationRequest", Application);
        return AuthorityProposal.From(_events);
    }

    public IReadOnlyDictionary<string, string> PublishNamedRequest(string methodName, string eventNameSuffix)
    {
        _integration.Invoke(methodName, Application);
        return (IReadOnlyDictionary<string, string>)_events.NamedPublications
            .Last(publication => publication.Name.EndsWith(eventNameSuffix, StringComparison.Ordinal))
            .Payload;
    }

    private object? InvokeApplication(MethodInfo method, object?[]? arguments)
    {
        return method.Name switch
        {
            "get_EventDispatcher" => _events.Proxy,
            "get_Settings" => Settings.Proxy,
            "GetApplicationConfig" => CreateApplicationConfig(method.ReturnType),
            "GetApplicationTemplates" => ReflectionValues.EmptySequence(method.ReturnType),
            "TryResolveInstance" => TryResolveInstance(method, arguments!),
            "RegisterInstance" => RegisterInstance(arguments!),
            _ => ReflectionValues.Default(method.ReturnType)
        };
    }

    private readonly Dictionary<string, object?> _instances = new(StringComparer.Ordinal);

    private object? TryResolveInstance(MethodInfo method, object?[] arguments)
    {
        var found = _instances.TryGetValue((string)arguments[0]!, out var value);
        arguments[1] = value;
        return found;
    }

    private object? RegisterInstance(object?[] arguments)
    {
        _instances[(string)arguments[0]!] = arguments[1];
        return null;
    }

    private object CreateApplicationConfig(Type returnType)
    {
        var modulesProperty = returnType.GetProperty("Modules")
            ?? throw new InvalidOperationException($"{returnType.FullName} has no Modules property.");
        var moduleType = ReflectionValues.SequenceElementType(modulesProperty.PropertyType);
        var modules = Modules
            .Select(module => ReflectionValues.Structured(
                moduleType,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ModuleId"] = module.ModuleId,
                    ["Version"] = module.Version
                }))
            .ToArray();
        var moduleSequence = ReflectionValues.Sequence(modulesProperty.PropertyType, moduleType, modules);
        return ReflectionValues.Structured(
            returnType,
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Modules"] = moduleSequence
            });
    }

    private void OnNamedPublication(string eventName, object payload)
    {
        if (eventName.EndsWith("DiscoverHostCapabilities", StringComparison.Ordinal))
        {
            AddDictionaryEntries(payload, Capabilities);
        }
        else if (eventName.EndsWith("DiscoverHostRegistrations", StringComparison.Ordinal))
        {
            AddDictionaryEntries(payload, HostRegistrations.ToDictionary(
                registration => registration.Key,
                registration => registration.Description,
                StringComparer.OrdinalIgnoreCase));
        }
    }

    private static void AddDictionaryEntries(object target, IReadOnlyDictionary<string, string> values)
    {
        var dictionary = (IDictionary)target;
        foreach (var pair in values)
        {
            dictionary[pair.Key] = pair.Value;
        }
    }
}

internal sealed class EventDispatcherFixture
{
    private readonly Dictionary<Type, List<Delegate>> _subscriptions = [];
    private readonly Action<string, object> _namedPublication;

    public EventDispatcherFixture(Type dispatcherType, Action<string, object> namedPublication)
    {
        _namedPublication = namedPublication;
        Proxy = DynamicProxy.Create(dispatcherType, Invoke);
    }

    public object Proxy { get; }
    public List<object> Publications { get; } = [];
    public List<(string Name, object Payload)> NamedPublications { get; } = [];

    private object? Invoke(MethodInfo method, object?[]? arguments)
    {
        arguments ??= [];
        if (method.Name == "Subscribe" && method.IsGenericMethod)
        {
            var eventType = method.GetGenericArguments()[0];
            if (!_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers = [];
                _subscriptions[eventType] = handlers;
            }

            handlers.Add((Delegate)arguments[0]!);
            return ReflectionValues.Default(method.ReturnType);
        }

        if (method.Name == "Publish" && arguments.Length == 1)
        {
            var publication = arguments[0]!;
            Publications.Add(publication);
            foreach (var handlers in _subscriptions
                .Where(pair => pair.Key.IsInstanceOfType(publication))
                .SelectMany(pair => pair.Value))
            {
                handlers.DynamicInvoke(publication);
            }

            return ReflectionValues.Default(method.ReturnType);
        }

        if (method.Name == "Publish" && arguments.Length >= 2 && arguments[0] is string eventName)
        {
            var payload = arguments[1]!;
            NamedPublications.Add((eventName, payload));
            _namedPublication(eventName, payload);
            return ReflectionValues.Default(method.ReturnType);
        }

        return ReflectionValues.Default(method.ReturnType);
    }
}

internal sealed class AuthoritySettings
{
    internal const int AuthorizationCodeIndex = 0;
    internal const int RefreshTokenIndex = 2;
    internal const int DeviceAuthorizationIndex = 3;
    internal const int AuthorizationCodeMask = 1 << AuthorizationCodeIndex;
    internal const int DeviceAuthorizationMask = 1 << DeviceAuthorizationIndex;
    internal const int ManagementApisMask = 1 << 5;

    internal static readonly string[] FeatureNames =
        [
        "authorization-code",
        "client-credentials",
        "refresh-token",
        "device-authorization",
        "external-identity-provider-brokering",
        "management-apis",
        "integration-events",
        "lifecycle-notifications",
        "auditing"
        ];

    private static readonly string[] FeatureSettingIds =
        [
        "67b1ca34-f4c7-41e0-9129-e7e813c9609b",
        "3e9fb0c7-3cdf-4bfc-981a-81b0f202e836",
        "954589af-ec6f-4cb4-a9bb-dcdb0fe2919c",
        "42d4e6ec-10bd-446f-b255-58787bd66bfe",
        "7bf7e906-e185-446e-b877-46ee73a08edf",
        "39d3f48e-f1d9-4714-af73-20fb56726999",
        "e392ab19-9091-4fdd-aeaf-5e76f3668e2d",
        "3e41cb26-1e6f-4905-87c5-61280794ff70",
        "05a54a85-566d-4352-bea8-2e944a478ec8"
        ];

    private readonly Dictionary<string, Dictionary<string, string>> _groups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _featureIdsByName = FeatureNames
        .Select((name, index) => (name, id: FeatureSettingIds[index]))
        .ToDictionary(pair => pair.name, pair => pair.id, StringComparer.OrdinalIgnoreCase);

    private AuthoritySettings(Type settingsProviderType)
    {
        Proxy = DynamicProxy.Create(
            settingsProviderType,
            InvokeProvider);
    }

    public object Proxy { get; }

    public static AuthoritySettings Valid(Type settingsProviderType)
    {
        var settings = new AuthoritySettings(settingsProviderType);
        settings.SetAllFeatures(false);
        settings.SetFeature("authorization-code", true);
        settings.SetFeature("client-credentials", true);
        settings.SetFeature("refresh-token", true);
        settings.SetFeature("device-authorization", true);
        settings.AddGroup(
            "23a988ec-eff9-480d-9c4e-5248d3817298",
            ("4061fb5f-baa0-4efa-9f8f-751947b35f64", "https://authority.example"),
            ("8eb0c192-d3d2-48a7-8063-7c88abf7cc6b", "15"),
            ("515469e7-39bc-47ee-a9dd-c98e4089830d", "15"),
            ("b9f3da46-9a79-43b9-9315-0341a92e34aa", "30"),
            ("5b954a48-4d56-4f0d-aa76-f5988b8ef9ca", "480"),
            ("4c487158-bf90-485e-8012-294ec3b08195", "30"),
            ("cb665b98-93d7-4d9d-88e4-5eb6dd03eaa3", "5"),
            ("4a4e84b3-044c-4baf-9858-5457ab426ad9", "ak_"),
            ("19b6c6fd-4157-4342-a535-c10f5d60eba0", "true"));
        settings.AddGroup(
            "0a946a92-387e-4653-973b-efa547d09cb2",
            ("decde6e3-4053-431a-b254-5a3e7b2195c5", "365"),
            ("2b23403d-4862-40e2-a5d2-8c8b252400d4", "2"),
            ("8b48bc3f-151e-45ea-af9b-df341334769c", "60"));
        settings.AddGroup(
            "23cd9e04-e936-4be3-8034-d360d199e5c0",
            ("2a56fc4e-41c2-4997-89c1-fca5bf6441f4", "explicit-identity"),
            ("526c78df-af33-48ba-84c0-c59abf3c0ba8", "https://identity.example"),
            ("3bf76ce0-bcdd-49a4-9e77-23c3d7f2fcc8", "bootstrap-admin"),
            ("6ca91851-089c-4179-a2b7-c204f1008f08", "admin@example.com"),
            ("8f899d0b-78f5-4490-b4c4-6d43d184ea44", "SeedAuthority"));
        settings.AddGroup(
            "8f1eb1fd-5516-41a8-b1c8-73809d512350",
            ("be5ebddb-4d73-49b7-a3cf-d2d70fcdda1b", "false"),
            ("b0443e22-c7c1-426a-9c78-d10085b523b1", "account_id,workspace_id"),
            ("4e03965f-74ec-4e97-b8c4-63e84aefd1ff", "true"));
        settings.AddGroup(
            "b45afdb3-2a07-47fa-bcc9-222bc3d2716c",
            ("433af046-20c8-4176-8904-c31600a0dd47", "/.well-known/openid-configuration"),
            ("40459f52-77fb-4b4a-89ac-3966c0ba8cf6", "/.well-known/jwks.json"),
            ("469539a5-9e53-43f6-a0ab-6e94369eb265", "/connect/authorize"),
            ("d83743a3-db55-4c01-aad7-816acce2fafb", "/signin-oidc"),
            ("e6f751e9-a091-4851-bb2f-bffb7c1f2427", "/connect/token"),
            ("fb2ab754-36c3-4b58-882e-a43965b0a60f", "/connect/device"),
            ("99b198d0-ec3e-407d-b08e-a52bcf334a6e", "/device"),
            ("ffdfac1f-8820-4d39-96f9-a015bf981320", "/device/approve"),
            ("c3a72a33-9964-42c7-834c-4082a2ba35d0", "/connect/userinfo"),
            ("c20da934-5784-4b89-a1bf-93556b532f95", "/connect/logout"),
            ("88f097fe-4a84-42c6-9341-0a8af3c7c6f6", "/authority/manage"));
        return settings;
    }

    public void SetFeatureMask(int mask)
    {
        for (var index = 0; index < FeatureNames.Length; index++)
        {
            SetFeature(FeatureNames[index], (mask & (1 << index)) != 0);
        }
    }

    public void SetAllFeatures(bool enabled)
    {
        AddGroup("a29ac6c1-1013-4828-aa6a-fcce0a6f67a2");
        foreach (var featureName in FeatureNames)
        {
            SetFeature(featureName, enabled);
        }
    }

    public void SetTenantScopedCapabilitiesEnabled(bool enabled)
    {
        _groups["8f1eb1fd-5516-41a8-b1c8-73809d512350"]["be5ebddb-4d73-49b7-a3cf-d2d70fcdda1b"] =
            enabled.ToString().ToLowerInvariant();
    }

    public void SetFeature(string featureName, bool enabled)
    {
        _groups["a29ac6c1-1013-4828-aa6a-fcce0a6f67a2"][_featureIdsByName[featureName]] =
            enabled.ToString().ToLowerInvariant();
    }

    private void AddGroup(string groupId, params (string Id, string Value)[] values)
    {
        if (!_groups.TryGetValue(groupId, out var group))
        {
            group = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _groups[groupId] = group;
        }

        foreach (var value in values)
        {
            group[value.Id] = value.Value;
        }
    }

    private object? InvokeProvider(MethodInfo method, object?[]? arguments)
    {
        if (method.Name != "GetGroup")
        {
            return ReflectionValues.Default(method.ReturnType);
        }

        var groupId = (string)arguments![0]!;
        if (!_groups.TryGetValue(groupId, out var group))
        {
            group = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _groups[groupId] = group;
        }

        return DynamicProxy.Create(method.ReturnType, (groupMethod, groupArguments) =>
        {
            return groupMethod.Name switch
            {
                "get_Id" => groupId,
                "get_Title" => groupId,
                "set_Title" => null,
                "GetSetting" => CreateSetting(groupMethod.ReturnType, (string)groupArguments![0]!, group),
                _ => ReflectionValues.Default(groupMethod.ReturnType)
            };
        });
    }

    private static object? CreateSetting(Type settingType, string settingId, IReadOnlyDictionary<string, string> group)
    {
        if (!group.TryGetValue(settingId, out var value))
        {
            return null;
        }

        return DynamicProxy.Create(settingType, (method, _) => method.Name switch
        {
            "get_Id" => settingId,
            "get_Value" => value,
            _ => ReflectionValues.Default(method.ReturnType)
        });
    }
}

internal sealed record HostModule(string ModuleId, string Version);
internal sealed record HostRegistration(string Key, string Description);

internal sealed record AuthorityProposal(
    IReadOnlyList<string> ConfigurationKeys,
    IReadOnlyList<string> ServiceRegistrations,
    IReadOnlyList<string> MiddlewareRegistrations,
    IReadOnlyDictionary<string, bool> Features,
    IReadOnlyList<string> Routes,
    IReadOnlyList<string> Fingerprints)
{
    public static AuthorityProposal From(EventDispatcherFixture events)
    {
        var configurationKeys = events.Publications
            .Where(publication => publication.GetType().Name == "AppSettingRegistrationRequest")
            .Select(publication => ReadString(publication, "Key"))
            .ToArray();
        var serviceRegistrations = events.Publications
            .Where(publication => publication.GetType().Name == "ServiceConfigurationRequest")
            .Select(publication => ReadString(publication, "ExtensionMethodName"))
            .ToArray();
        var middlewareRegistrations = events.Publications
            .Where(publication => publication.GetType().Name == "ApplicationBuilderRegistrationRequest")
            .Select(publication => ReadString(publication, "ExtensionMethodName"))
            .ToArray();
        var featureRequest = events.NamedPublications
            .Single(publication => publication.Name.EndsWith("RegisterAuthority", StringComparison.Ordinal));
        var middlewareRequest = events.NamedPublications
            .Single(publication => publication.Name.EndsWith("RegisterAuthorityMiddleware", StringComparison.Ordinal));
        var features = ReadDictionary(featureRequest.Payload)
            .Where(pair => AuthoritySettings.FeatureNames.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(
                pair => pair.Key,
                pair => bool.Parse(pair.Value),
                StringComparer.OrdinalIgnoreCase);
        var routes = ReadDictionary(middlewareRequest.Payload)
            .Where(pair => pair.Key.StartsWith("route:", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Value)
            .ToArray();
        var fingerprints = configurationKeys.Select(key => $"configuration:{key}")
            .Concat(serviceRegistrations.Select(value => $"service:{value}"))
            .Concat(middlewareRegistrations.Select(value => $"middleware:{value}"))
            .Concat(features.OrderBy(pair => pair.Key).Select(pair => $"feature:{pair.Key}={pair.Value}"))
            .Concat(routes.Select(route => $"route:{route}"))
            .ToArray();
        return new AuthorityProposal(
            configurationKeys,
            serviceRegistrations,
            middlewareRegistrations,
            features,
            routes,
            fingerprints);
    }

    private static string ReadString(object instance, string propertyName)
    {
        return (string)(instance.GetType().GetProperty(propertyName)?.GetValue(instance)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was unavailable."));
    }

    private static Dictionary<string, string> ReadDictionary(object value)
    {
        return ((IEnumerable)value).Cast<object>()
            .Select(item => item.GetType())
            .Select((type, index) =>
            {
                var item = ((IEnumerable)value).Cast<object>().ElementAt(index);
                return new KeyValuePair<string, string>(
                    (string)type.GetProperty("Key")!.GetValue(item)!,
                    (string)type.GetProperty("Value")!.GetValue(item)!);
            })
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }
}

internal class DynamicProxy : DispatchProxy
{
    private Func<MethodInfo, object?[]?, object?> _handler = null!;

    public static object Create(Type interfaceType, Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = (DynamicProxy)DispatchProxy.Create(interfaceType, typeof(DynamicProxy));
        proxy._handler = handler;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? arguments)
    {
        return _handler(targetMethod ?? throw new InvalidOperationException("Proxy method was unavailable."), arguments);
    }
}

internal static class ReflectionValues
{
    public static object? Default(Type type)
    {
        if (type == typeof(void))
        {
            return null;
        }

        if (type == typeof(string))
        {
            return string.Empty;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return EmptySequence(type);
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    public static object EmptySequence(Type sequenceType)
    {
        var elementType = SequenceElementType(sequenceType);
        return Array.CreateInstance(elementType, 0);
    }

    public static Type SequenceElementType(Type sequenceType)
    {
        if (sequenceType.IsArray)
        {
            return sequenceType.GetElementType()!;
        }

        var sequenceInterface = sequenceType.IsGenericType && sequenceType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? sequenceType
            : sequenceType.GetInterfaces()
                .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return sequenceInterface?.GetGenericArguments()[0]
            ?? throw new InvalidOperationException($"Could not determine sequence element type for {sequenceType.FullName}.");
    }

    public static object Sequence(Type sequenceType, Type elementType, object[] values)
    {
        var array = Array.CreateInstance(elementType, values.Length);
        for (var index = 0; index < values.Length; index++)
        {
            array.SetValue(values[index], index);
        }

        if (sequenceType.IsAssignableFrom(array.GetType()))
        {
            return array;
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var value in values)
        {
            list.Add(value);
        }

        return list;
    }

    public static object Structured(Type type, IReadOnlyDictionary<string, object?> values)
    {
        if (type.IsInterface)
        {
            return DynamicProxy.Create(type, (method, _) =>
            {
                if (method.Name.StartsWith("get_", StringComparison.Ordinal) &&
                    values.TryGetValue(method.Name[4..], out var value))
                {
                    return value;
                }

                return Default(method.ReturnType);
            });
        }

        object instance;
        try
        {
            instance = Activator.CreateInstance(type, nonPublic: true)!;
        }
        catch
        {
            instance = RuntimeHelpers.GetUninitializedObject(type);
        }

        foreach (var pair in values)
        {
            var property = type.GetProperty(
                pair.Key,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (property?.SetMethod is not null)
            {
                property.SetValue(instance, pair.Value);
                continue;
            }

            var field = type.GetField(
                $"<{pair.Key}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            field?.SetValue(instance, pair.Value);
        }

        return instance;
    }
}
