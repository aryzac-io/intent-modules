using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aryzac.Security.Service.Settings;
using Intent.Engine;
using Intent.Modules.Common.CSharp.AppStartup;
using Intent.Modules.Common.CSharp.Configuration;
using Intent.Modules.Common.CSharp.DependencyInjection;
using Intent.RoslynWeaver.Attributes;

namespace Aryzac.Security.Service.FactoryExtensions
{
    [IntentManaged(Mode.Ignore)]
    internal static class SecurityAuthorityIntegration
    {
        internal const string CapabilityDiscoveryEvent = "Aryzac.Security.Service.DiscoverHostCapabilities";
        internal const string RegistrationDiscoveryEvent = "Aryzac.Security.Service.DiscoverHostRegistrations";
        internal const string RegistrationRequestEvent = "Aryzac.Security.Service.RegisterAuthority";
        internal const string MiddlewareRequestEvent = "Aryzac.Security.Service.RegisterAuthorityMiddleware";
        internal const string StartupValidationRequestEvent = "Aryzac.Security.Service.RegisterStartupValidation";
        internal const string PersistenceRequestEvent = "Aryzac.Security.Service.RegisterAuthorityPersistence";
        internal const string ConfigurationKey = "SecurityAuthority";
        internal const string AuthenticationScheme = "SecurityAuthority";
        internal const string PersistenceRegistration = "SecurityAuthorityPersistence";
        internal const string ServiceRegistration = "AddSecurityAuthority";
        internal const string MiddlewareRegistration = "UseSecurityAuthority";

        private const string StateKey = "Aryzac.Security.Service.FactoryExtensions.IntegrationState";
        private const string CompanionModuleId = "Aryzac.Security";
        private static readonly Version MinimumCompanionVersion = new(1, 0, 2);

        public static void Initialize(IApplication application)
        {
            var state = GetState(application);
            if (state.IsInitialized)
            {
                return;
            }

            state.IsInitialized = true;
            application.EventDispatcher.Subscribe<ContainerRegistrationRequest>(state.ContainerRegistrations.Add);
            application.EventDispatcher.Subscribe<ServiceConfigurationRequest>(state.ServiceConfigurations.Add);
            application.EventDispatcher.Subscribe<AppSettingRegistrationRequest>(state.AppSettings.Add);
            application.EventDispatcher.Subscribe<ApplicationBuilderRegistrationRequest>(state.ApplicationBuilderRegistrations.Add);
        }

        public static void ValidateCompanionModule(IApplication application)
        {
            var module = application.GetApplicationConfig().Modules
                .FirstOrDefault(x => string.Equals(x.ModuleId, CompanionModuleId, StringComparison.OrdinalIgnoreCase));

            if (module is null)
            {
                throw Friendly(
                    "Aryzac.Security.Service requires the Aryzac.Security companion module. " +
                    "Install Aryzac.Security version 1.0.2-pre.0 or later in the compatible 1.x line, then run the Software Factory again.");
            }

            if (!IsCompatibleCompanionVersion(module.Version))
            {
                throw Friendly(
                    $"Aryzac.Security.Service requires Aryzac.Security version 1.0.2-pre.0 or later in the compatible 1.x line, " +
                    $"but '{module.Version}' is installed. Update Aryzac.Security to a compatible version.");
            }
        }

        public static IReadOnlyDictionary<string, string> DiscoverCapabilities(IApplication application)
        {
            var capabilities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var aspNetCoreModule = application.GetApplicationConfig().Modules
                .FirstOrDefault(x => x.ModuleId.StartsWith("Intent.AspNetCore", StringComparison.OrdinalIgnoreCase));
            var startupTemplate = application.GetApplicationTemplates().OfType<IAppStartupFile>().FirstOrDefault();

            if (startupTemplate is not null || aspNetCoreModule is not null)
            {
                capabilities["aspnet-core"] = aspNetCoreModule?.ModuleId ?? startupTemplate!.GetType().FullName ?? "ASP.NET Core startup template";
            }

            application.EventDispatcher.Publish(CapabilityDiscoveryEvent, capabilities);
            return capabilities;
        }

        public static void ValidateAspNetCoreHost(IReadOnlyDictionary<string, string> capabilities)
        {
            if (!capabilities.TryGetValue("aspnet-core", out var registration) || string.IsNullOrWhiteSpace(registration))
            {
                throw Friendly(
                    "Aryzac.Security.Service requires an ASP.NET Core HTTP host capability. " +
                    "Install or enable the host module that provides the application startup pipeline and endpoint routing.");
            }
        }

        public static void ValidatePersistenceCapability(IReadOnlyDictionary<string, string> capabilities)
        {
            if (!capabilities.TryGetValue("persistence", out var registration) || string.IsNullOrWhiteSpace(registration))
            {
                throw Friendly(
                    "Aryzac.Security.Service requires a host persistence capability. " +
                    "Configure a persistence integration that advertises authority record storage and atomic operations, then run the Software Factory again.");
            }
        }

        public static void ValidatePersistenceGuarantees(IReadOnlyDictionary<string, string> capabilities)
        {
            RequireCapability(capabilities, "persistence-authority-record-isolation", "authority record isolation");
            RequireCapability(capabilities, "persistence-uniqueness", "uniqueness enforcement");
            RequireCapability(capabilities, "persistence-optimistic-concurrency", "optimistic concurrency");
            RequireCapability(capabilities, "persistence-atomic-credential-rotation", "atomic credential rotation");
            RequireCapability(capabilities, "persistence-one-time-redemption", "one-time credential redemption");

            if (IsTrue(capabilities, "persistence-transactions"))
            {
                RequireCapability(capabilities, "persistence-host-transaction-participation", "host transaction participation");
            }
        }

        public static void ValidateTenantAdapter(IApplication application, IReadOnlyDictionary<string, string> capabilities)
        {
            if (!application.Settings.GetAuthorityTenancy().TenantScopedCapabilitiesEnabled())
            {
                return;
            }

            var registeredByCapability = capabilities.TryGetValue("tenant-adapter", out var adapter) && !string.IsNullOrWhiteSpace(adapter);
            var registeredByContainer = GetState(application).ContainerRegistrations.Any(x =>
                MatchesContractName(x.InterfaceType, "ISecurityAuthorityTenantAdapter"));

            if (!registeredByCapability && !registeredByContainer)
            {
                throw Friendly(
                    "Tenant-scoped Security Authority capabilities are enabled, but no ISecurityAuthorityTenantAdapter registration was found. " +
                    "Register ISecurityAuthorityTenantAdapter through the host container registration mechanism.");
            }
        }

        public static void ValidateSettings(IApplication application)
        {
            var features = application.Settings.GetAuthorityFeatures();
            if (features.RefreshToken() && !features.AuthorizationCode() && !features.DeviceAuthorization())
            {
                throw Friendly(
                    "Authority Features enables Refresh Token while both Authorization Code and Device Authorization are disabled. " +
                    "Enable Authorization Code or Device Authorization, or disable Refresh Token.");
            }

            var protocol = application.Settings.GetAuthorityProtocol();
            ValidateHttpsIssuer(protocol.Issuer());
            ValidateInteger("Authority Protocol / Access Token Minutes", protocol.AccessTokenMinutes(), 1, 1440);
            ValidateInteger("Authority Protocol / ID Token Minutes", protocol.IDTokenMinutes(), 1, 1440);
            ValidateInteger("Authority Protocol / Refresh Token Days", protocol.RefreshTokenDays(), 1, 365);
            ValidateInteger("Authority Protocol / SSO Session Lifetime Minutes", protocol.SSOSessionLifetimeMinutes(), 5, 43200);
            ValidateInteger("Authority Protocol / Clock Skew Seconds", protocol.ClockSkewSeconds(), 0, 60);
            ValidateInteger("Authority Protocol / Device Polling Seconds", protocol.DevicePollingSeconds(), 1, int.MaxValue);
            RequireText("Authority Protocol / API Key Prefix", protocol.APIKeyPrefix());

            if (!protocol.HTTPSRequiredOutsideDevelopment())
            {
                throw Friendly(
                    "Authority Protocol / HTTPS Required Outside Development must be enabled. " +
                    "The Security Authority cannot issue credentials over non-HTTPS production traffic.");
            }

            var lifecycle = application.Settings.GetAuthorityDataLifecycle();
            ValidateInteger("Authority Data Lifecycle / Revoked Metadata Retention Days", lifecycle.RevokedMetadataRetentionDays(), 1, 3650);
            ValidateInteger("Authority Data Lifecycle / Code and Device Cleanup Delay Days", lifecycle.CodeAndDeviceCleanupDelayDays(), 1, 7);
            ValidateInteger("Authority Data Lifecycle / SSO and Refresh Cleanup Days", lifecycle.SSOAndRefreshCleanupDays(), 30, 90);

            ValidateBootstrap(application.Settings.GetAuthorityBootstrap());
            ValidateRoutes(application);
        }

        public static void ValidateProductionCryptography(IReadOnlyDictionary<string, string> capabilities)
        {
            if (IsTrue(capabilities, "production-environment") && !IsTrue(capabilities, "production-cryptography"))
            {
                throw Friendly(
                    "The Security Authority is configured for production but no production cryptography capability was reported. " +
                    "Register a persistent RSA signing-key provider and secret protector; development-only ephemeral keys are not permitted in production.");
            }
        }

        public static void ValidateRegistrationConflicts(IApplication application)
        {
            var registrations = DiscoverRegistrations(application);
            ThrowReportedConflicts(registrations);
            ThrowConflict(registrations, "configuration", ConfigurationKey, "Security Authority configuration");
            ThrowConflict(registrations, "scheme", AuthenticationScheme, $"Security Authority authentication scheme '{AuthenticationScheme}'");
            ThrowConflict(registrations, "persistence", PersistenceRegistration, $"Security Authority persistence registration '{PersistenceRegistration}'");
            ThrowConflict(registrations, "service", ServiceRegistration, $"Security Authority service registration '{ServiceRegistration}'");
        }

        public static void ValidateMiddlewareConflicts(IApplication application)
        {
            var registrations = DiscoverRegistrations(application);
            ThrowReportedConflicts(registrations);
            ThrowConflict(registrations, "middleware", MiddlewareRegistration, $"Security Authority middleware registration '{MiddlewareRegistration}'");

            foreach (var route in GetEnabledRoutes(application))
            {
                ThrowConflict(registrations, "route", route, $"Security Authority route '{route}'");
            }
        }

        public static void PublishRegistrationRequests(IApplication application)
        {
            var state = GetState(application);
            if (state.RegistrationRequestsPublished)
            {
                return;
            }

            state.RegistrationRequestsPublished = true;
            application.EventDispatcher.Publish(new AppSettingRegistrationRequest(ConfigurationKey, BuildAuthorityConfiguration(application)));
            application.EventDispatcher.Publish(ServiceConfigurationRequest
                .ToRegister(ServiceRegistration, ServiceConfigurationRequest.ParameterType.Configuration)
                .ForConcern("Application"));

            var request = BuildFeatureRequest(application);
            request["authorization-engine"] = "SecurityAuthorityAuthorizationEngine";
            request["authorization-data-source-contract"] = "ISecurityAuthorityAuthorizationDataSource";
            request["authorization-invalidator-contract"] = "ISecurityAuthorityAuthorizationInvalidator";
            request["authorization-engine-lifetime"] = "singleton";
            request["external-provider-protocol"] = "SecurityAuthorityOidcExternalProviderProtocol";
            request["external-provider-protocol-contract"] = "ISecurityAuthorityExternalProviderProtocol";
            request["post-commit-dispatch"] = "SecurityAuthorityPostCommitDispatch";
            request["cleanup-trigger"] = "SecurityAuthorityCleanup";
            request["cleanup-hosted-service"] = "SecurityAuthorityCleanupHostedService";
            request["grant-catalog-authorizes"] = "false";
            application.EventDispatcher.Publish(RegistrationRequestEvent, request);
        }

        public static void PublishMiddlewareRequest(IApplication application)
        {
            var state = GetState(application);
            if (state.MiddlewareRequestPublished)
            {
                return;
            }

            state.MiddlewareRequestPublished = true;
            var request = BuildFeatureRequest(application);
            var index = 0;
            foreach (var route in GetEnabledRoutes(application))
            {
                request[$"route:{index++}"] = route;
            }

            application.EventDispatcher.Publish(ApplicationBuilderRegistrationRequest
                .ToRegister(MiddlewareRegistration)
                .WithPriority(500));
            application.EventDispatcher.Publish(MiddlewareRequestEvent, request);
        }

        public static void PublishStartupValidationRequest(IApplication application)
        {
            var state = GetState(application);
            if (state.StartupValidationRequestPublished)
            {
                return;
            }

            state.StartupValidationRequestPublished = true;
            var request = BuildFeatureRequest(application);
            request["require-https-outside-development"] = "true";
            request["require-production-issuer"] = "true";
            request["require-external-provider-protocol"] = "true";
            request["require-external-provider-discovery"] = "true";
            request["require-external-provider-signature-validation"] = "true";
            request["require-production-rsa-signing-private-key"] = "true";
            request["require-production-external-provider-secret-protection"] = "true";
            request["require-production-sso-cookie-protection"] = "true";
            request["require-keyed-api-key-hashing"] = "true";
            request["require-secret-redaction"] = "true";
            request["require-public-provider-projections"] = "true";
            request["require-persistence-authority-record-isolation"] = "true";
            request["require-persistence-uniqueness"] = "true";
            request["require-persistence-optimistic-concurrency"] = "true";
            request["require-persistence-atomic-credential-rotation"] = "true";
            request["require-persistence-one-time-redemption"] = "true";
            request["join-host-transaction-when-supported"] = "true";
            request["defer-credential-reveal-until-commit"] = "true";
            request["rollback-on-pre-commit-failure"] = "true";
            AddAtomicOperationRequirements(request);
            request["require-tenant-adapter"] = application.Settings.GetAuthorityTenancy().TenantScopedCapabilitiesEnabled().ToString(CultureInfo.InvariantCulture);
            application.EventDispatcher.Publish(StartupValidationRequestEvent, request);
        }

        public static void PublishPersistenceRequest(IApplication application)
        {
            var request = BuildFeatureRequest(application);
            request["persistence-contract"] = "ISecurityAuthorityPersistence";
            request["record-store-contract"] = "ISecurityAuthorityRecordStore";
            request["atomic-operation-contract"] = "ISecurityAuthorityAtomicOperation";
            request["commit-receipt-contract"] = "SecurityAuthorityCommitReceipt";
            request["deferred-credential-contract"] = "SecurityAuthorityDeferredCredential";
            request["tenant-adapter-contract"] = "ISecurityAuthorityTenantAdapter";
            request["authority-record-scope"] = "SecurityAuthority";
            request["require-authority-record-isolation"] = "true";
            request["require-uniqueness"] = "true";
            request["require-optimistic-concurrency"] = "true";
            request["require-atomic-credential-rotation"] = "true";
            request["require-one-time-redemption"] = "true";
            request["join-host-transaction-when-supported"] = "true";
            request["defer-credential-reveal-until-commit"] = "true";
            request["rollback-on-pre-commit-failure"] = "true";
            AddAtomicOperationRequirements(request);
            request["require-tenant-adapter"] = application.Settings.GetAuthorityTenancy().TenantScopedCapabilitiesEnabled().ToString(CultureInfo.InvariantCulture);
            application.EventDispatcher.Publish(PersistenceRequestEvent, request);
        }

        private static void AddAtomicOperationRequirements(IDictionary<string, string> request)
        {
            request["atomic-operation:token-redemption"] = "true";
            request["atomic-operation:refresh-token-rotation"] = "true";
            request["atomic-operation:api-key-regeneration"] = "true";
            request["atomic-operation:first-administrator-bootstrap"] = "true";
            request["atomic-operation:idempotent-provisioning"] = "true";
            request["atomic-operation:integration-event-deduplication"] = "true";
        }

        private static SecurityAuthorityIntegrationState GetState(IApplication application)
        {
            if (application.TryResolveInstance<SecurityAuthorityIntegrationState>(StateKey, out var state))
            {
                return state;
            }

            state = new SecurityAuthorityIntegrationState();
            application.RegisterInstance(StateKey, state);
            return state;
        }

        private static Dictionary<string, string> DiscoverRegistrations(IApplication application)
        {
            var registrations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var state = GetState(application);

            foreach (var setting in state.AppSettings)
            {
                registrations.TryAdd($"configuration:{setting.Key}", $"configuration key '{setting.Key}'");
            }

            foreach (var service in state.ServiceConfigurations)
            {
                registrations.TryAdd($"service:{service.ExtensionMethodName}", $"service registration '{service.ExtensionMethodName}'");
            }

            foreach (var service in state.ContainerRegistrations)
            {
                var name = service.InterfaceType ?? service.ConcreteType;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    registrations.TryAdd($"service:{name}", $"container registration '{name}'");
                }
            }

            foreach (var middleware in state.ApplicationBuilderRegistrations)
            {
                registrations.TryAdd($"middleware:{middleware.ExtensionMethodName}", $"middleware registration '{middleware.ExtensionMethodName}'");
            }

            application.EventDispatcher.Publish(RegistrationDiscoveryEvent, registrations);
            return registrations;
        }

        private static Dictionary<string, string> BuildFeatureRequest(IApplication application)
        {
            var features = application.Settings.GetAuthorityFeatures();
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["authorization-code"] = features.AuthorizationCode().ToString(CultureInfo.InvariantCulture),
                ["client-credentials"] = features.ClientCredentials().ToString(CultureInfo.InvariantCulture),
                ["refresh-token"] = features.RefreshToken().ToString(CultureInfo.InvariantCulture),
                ["device-authorization"] = features.DeviceAuthorization().ToString(CultureInfo.InvariantCulture),
                ["external-identity-provider-brokering"] = features.ExternalIdentityProviderBrokering().ToString(CultureInfo.InvariantCulture),
                ["management-apis"] = features.ManagementAPIs().ToString(CultureInfo.InvariantCulture),
                ["integration-events"] = features.IntegrationEvents().ToString(CultureInfo.InvariantCulture),
                ["lifecycle-notifications"] = features.LifecycleNotifications().ToString(CultureInfo.InvariantCulture),
                ["auditing"] = features.Auditing().ToString(CultureInfo.InvariantCulture)
            };
        }

        private static object BuildAuthorityConfiguration(IApplication application)
        {
            var features = application.Settings.GetAuthorityFeatures();
            var protocol = application.Settings.GetAuthorityProtocol();
            var tenancy = application.Settings.GetAuthorityTenancy();
            var bootstrap = application.Settings.GetAuthorityBootstrap();
            var lifecycle = application.Settings.GetAuthorityDataLifecycle();
            var routes = application.Settings.GetAuthorityRoutes();

            return new
            {
                Features = new
                {
                    AuthorizationCode = features.AuthorizationCode(),
                    ClientCredentials = features.ClientCredentials(),
                    RefreshToken = features.RefreshToken(),
                    DeviceAuthorization = features.DeviceAuthorization(),
                    ExternalIdentityProviderBrokering = features.ExternalIdentityProviderBrokering(),
                    ManagementAPIs = features.ManagementAPIs(),
                    IntegrationEvents = features.IntegrationEvents(),
                    LifecycleNotifications = features.LifecycleNotifications(),
                    Auditing = features.Auditing()
                },
                Protocol = new
                {
                    Issuer = protocol.Issuer(),
                    AccessTokenMinutes = ParseInteger(protocol.AccessTokenMinutes()),
                    IDTokenMinutes = ParseInteger(protocol.IDTokenMinutes()),
                    RefreshTokenDays = ParseInteger(protocol.RefreshTokenDays()),
                    SSOSessionLifetimeMinutes = ParseInteger(protocol.SSOSessionLifetimeMinutes()),
                    ClockSkewSeconds = ParseInteger(protocol.ClockSkewSeconds()),
                    DevicePollingSeconds = ParseInteger(protocol.DevicePollingSeconds()),
                    APIKeyPrefix = protocol.APIKeyPrefix(),
                    HTTPSRequiredOutsideDevelopment = protocol.HTTPSRequiredOutsideDevelopment()
                },
                Tenancy = new
                {
                    TenantScopedCapabilitiesEnabled = tenancy.TenantScopedCapabilitiesEnabled(),
                    ContextualClaimNames = tenancy.ContextualClaimNames(),
                    InheritanceEvaluationEnabled = tenancy.InheritanceEvaluationEnabled()
                },
                Bootstrap = new
                {
                    Strategy = bootstrap.BootstrapStrategy().Value,
                    ExplicitIdentityIssuer = bootstrap.ExplicitIdentityIssuer(),
                    ExplicitIdentitySubject = bootstrap.ExplicitIdentitySubject(),
                    FirstEligibleUserNormalizedEmail = bootstrap.FirstEligibleUserNormalizedEmail(),
                    CustomSeedFunction = bootstrap.CustomSeedFunction()
                },
                DataLifecycle = new
                {
                    RevokedMetadataRetentionDays = ParseInteger(lifecycle.RevokedMetadataRetentionDays()),
                    CodeAndDeviceCleanupDelayDays = ParseInteger(lifecycle.CodeAndDeviceCleanupDelayDays()),
                    SSOAndRefreshCleanupDays = ParseInteger(lifecycle.SSOAndRefreshCleanupDays())
                },
                Routes = new
                {
                    Discovery = routes.DiscoveryRoute(),
                    JWKS = routes.JWKSRoute(),
                    Authorization = routes.AuthorizationRoute(),
                    Callback = routes.CallbackRoute(),
                    Token = routes.TokenRoute(),
                    DeviceAuthorization = routes.DeviceAuthorizationRoute(),
                    DeviceVerification = routes.DeviceVerificationRoute(),
                    DeviceApproval = routes.DeviceApprovalRoute(),
                    UserInfo = routes.UserInfoRoute(),
                    Logout = routes.LogoutRoute(),
                    ManagementBase = routes.ManagementBaseRoute()
                }
            };
        }

        private static IEnumerable<string> GetEnabledRoutes(IApplication application)
        {
            var features = application.Settings.GetAuthorityFeatures();
            var routes = application.Settings.GetAuthorityRoutes();

            yield return routes.DiscoveryRoute();
            yield return routes.JWKSRoute();

            if (features.AuthorizationCode())
            {
                yield return routes.AuthorizationRoute();
                yield return routes.CallbackRoute();
                yield return routes.UserInfoRoute();
                yield return routes.LogoutRoute();
            }

            if (features.AuthorizationCode() || features.ClientCredentials() || features.RefreshToken() || features.DeviceAuthorization())
            {
                yield return routes.TokenRoute();
            }

            if (features.DeviceAuthorization())
            {
                yield return routes.DeviceAuthorizationRoute();
                yield return routes.DeviceVerificationRoute();
                yield return routes.DeviceApprovalRoute();
            }

            if (features.ExternalIdentityProviderBrokering() && !features.AuthorizationCode())
            {
                yield return routes.CallbackRoute();
            }

            if (features.ManagementAPIs())
            {
                yield return routes.ManagementBaseRoute();
            }
        }

        private static void ValidateRoutes(IApplication application)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var route in GetEnabledRoutes(application))
            {
                if (string.IsNullOrWhiteSpace(route) || !route.StartsWith("/", StringComparison.Ordinal))
                {
                    throw Friendly(
                        $"Security Authority route '{route}' is invalid. Configure every enabled authority route as a non-empty absolute path beginning with '/'.");
                }

                if (!seen.Add(route))
                {
                    throw Friendly(
                        $"Security Authority route '{route}' is assigned to more than one enabled authority capability. Configure unique routes before running the Software Factory again.");
                }
            }
        }

        private static void ValidateBootstrap(AuthorityBootstrap bootstrap)
        {
            if (bootstrap.BootstrapStrategy().IsExplicitIdentity())
            {
                RequireText("Authority Bootstrap / Explicit Identity Issuer", bootstrap.ExplicitIdentityIssuer());
                RequireText("Authority Bootstrap / Explicit Identity Subject", bootstrap.ExplicitIdentitySubject());
            }
            else if (bootstrap.BootstrapStrategy().IsFirstEligibleUser())
            {
                RequireText("Authority Bootstrap / First Eligible User Normalized Email", bootstrap.FirstEligibleUserNormalizedEmail());
            }
            else if (bootstrap.BootstrapStrategy().IsCustomSeedFunction())
            {
                RequireText("Authority Bootstrap / Custom Seed Function", bootstrap.CustomSeedFunction());
            }
        }

        private static void ValidateHttpsIssuer(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var issuer) ||
                !string.Equals(issuer.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw Friendly(
                    "Authority Protocol / Issuer must be an absolute HTTPS URI. Configure the canonical public issuer used by discovery and issued tokens.");
            }
        }

        private static void ValidateInteger(string settingName, string value, int minimum, int maximum)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < minimum || parsed > maximum)
            {
                throw Friendly(
                    $"{settingName} must be a whole number from {minimum} through {maximum}. Update the module setting and run the Software Factory again.");
            }
        }

        private static int ParseInteger(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static void RequireText(string settingName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw Friendly($"{settingName} is required. Supply a non-empty value and run the Software Factory again.");
            }
        }

        private static void RequireCapability(IReadOnlyDictionary<string, string> capabilities, string key, string description)
        {
            if (!IsTrue(capabilities, key))
            {
                var provider = capabilities.TryGetValue("persistence", out var registration) ? registration : "the configured persistence capability";
                throw Friendly(
                    $"Security Authority persistence registration '{provider}' does not advertise required {description}. " +
                    "Select or configure a persistence integration that provides this guarantee.");
            }
        }

        private static bool IsTrue(IReadOnlyDictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) && bool.TryParse(value, out var result) && result;
        }

        private static bool MatchesContractName(string value, string contractName)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                (string.Equals(value, contractName, StringComparison.Ordinal) ||
                value.EndsWith("." + contractName, StringComparison.Ordinal));
        }

        private static void ThrowReportedConflicts(IReadOnlyDictionary<string, string> registrations)
        {
            var conflict = registrations.FirstOrDefault(x => x.Key.StartsWith("conflict:", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(conflict.Key))
            {
                return;
            }

            var authorityRegistration = conflict.Key.Split(new[] { ':' }, 3).Last();
            throw Friendly(
                $"Security Authority registration conflict: existing registration '{conflict.Value}' conflicts with proposed registration '{authorityRegistration}'. " +
                "Neither registration was replaced. Rename or remove one registration explicitly, then run the Software Factory again.");
        }

        private static void ThrowConflict(
            IReadOnlyDictionary<string, string> registrations,
            string kind,
            string name,
            string authorityRegistration)
        {
            if (!registrations.TryGetValue($"{kind}:{name}", out var existingRegistration))
            {
                return;
            }

            throw Friendly(
                $"Security Authority {kind} conflict: existing registration '{existingRegistration}' conflicts with proposed registration '{authorityRegistration}'. " +
                "Neither registration was replaced. Rename or remove one registration explicitly, then run the Software Factory again.");
        }

        private static bool IsCompatibleCompanionVersion(string value)
        {
            var versionParts = value?.Split('+', 2)[0].Split('-', 2);
            if (versionParts is null ||
                !Version.TryParse(versionParts[0], out var release) ||
                release.Major != MinimumCompanionVersion.Major ||
                release < MinimumCompanionVersion)
            {
                return false;
            }

            if (release > MinimumCompanionVersion || versionParts.Length == 1)
            {
                return true;
            }

            return ComparePreRelease(versionParts[1], "pre.0") >= 0;
        }

        private static int ComparePreRelease(string left, string right)
        {
            var leftParts = left.Split('.');
            var rightParts = right.Split('.');
            for (var i = 0; i < Math.Max(leftParts.Length, rightParts.Length); i++)
            {
                if (i >= leftParts.Length)
                {
                    return -1;
                }

                if (i >= rightParts.Length)
                {
                    return 1;
                }

                var leftNumeric = int.TryParse(leftParts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var leftNumber);
                var rightNumeric = int.TryParse(rightParts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var rightNumber);
                var comparison = leftNumeric && rightNumeric
                    ? leftNumber.CompareTo(rightNumber)
                    : leftNumeric
                    ? -1
                    : rightNumeric
                    ? 1
                    : string.Compare(leftParts[i], rightParts[i], StringComparison.Ordinal);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        private static Exception Friendly(string message)
        {
            var exceptionType = Type.GetType("Intent.Exceptions.FriendlyException, Intent.SoftwareFactory.SDK");
            if (exceptionType is not null &&
                Activator.CreateInstance(exceptionType, message) is Exception exception)
            {
                return exception;
            }

            return new InvalidOperationException(message);
        }

        private sealed class SecurityAuthorityIntegrationState
        {
            public bool IsInitialized { get; set; }
            public bool RegistrationRequestsPublished { get; set; }
            public bool MiddlewareRequestPublished { get; set; }
            public bool StartupValidationRequestPublished { get; set; }
            public List<ContainerRegistrationRequest> ContainerRegistrations { get; } = new();
            public List<ServiceConfigurationRequest> ServiceConfigurations { get; } = new();
            public List<AppSettingRegistrationRequest> AppSettings { get; } = new();
            public List<ApplicationBuilderRegistrationRequest> ApplicationBuilderRegistrations { get; } = new();
        }
    }
}
