using System;
using Intent.Configuration;
using Intent.Engine;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Settings.ModuleSettingsExtensions", Version = "1.0")]

namespace Aryzac.Security.Service.Settings
{
    public static class ModuleSettingsExtensions
    {
        public static AuthorityBootstrap GetAuthorityBootstrap(this IApplicationSettingsProvider settings)
        {
            return new AuthorityBootstrap(settings.GetGroup("23cd9e04-e936-4be3-8034-d360d199e5c0"));
        }

        public static AuthorityCryptography GetAuthorityCryptography(this IApplicationSettingsProvider settings)
        {
            return new AuthorityCryptography(settings.GetGroup("500f483b-e0f6-436a-b160-1251c69f2209"));
        }

        public static AuthorityDataLifecycle GetAuthorityDataLifecycle(this IApplicationSettingsProvider settings)
        {
            return new AuthorityDataLifecycle(settings.GetGroup("0a946a92-387e-4653-973b-efa547d09cb2"));
        }

        public static AuthorityFeatures GetAuthorityFeatures(this IApplicationSettingsProvider settings)
        {
            return new AuthorityFeatures(settings.GetGroup("a29ac6c1-1013-4828-aa6a-fcce0a6f67a2"));
        }

        public static AuthorityProtocol GetAuthorityProtocol(this IApplicationSettingsProvider settings)
        {
            return new AuthorityProtocol(settings.GetGroup("23a988ec-eff9-480d-9c4e-5248d3817298"));
        }

        public static AuthorityRoutes GetAuthorityRoutes(this IApplicationSettingsProvider settings)
        {
            return new AuthorityRoutes(settings.GetGroup("b45afdb3-2a07-47fa-bcc9-222bc3d2716c"));
        }

        public static AuthorityTenancy GetAuthorityTenancy(this IApplicationSettingsProvider settings)
        {
            return new AuthorityTenancy(settings.GetGroup("8f1eb1fd-5516-41a8-b1c8-73809d512350"));
        }
    }

    public class AuthorityBootstrap : IGroupSettings
    {
        private readonly IGroupSettings _groupSettings;

        public AuthorityBootstrap(IGroupSettings groupSettings)
        {
            _groupSettings = groupSettings;
        }

        public string Id => _groupSettings.Id;

        public string Title
        {
            get => _groupSettings.Title;
            set => _groupSettings.Title = value;
        }

        public ISetting GetSetting(string settingId)
        {
            return _groupSettings.GetSetting(settingId);
        }
        public BootstrapStrategyOptions BootstrapStrategy() => new BootstrapStrategyOptions(_groupSettings.GetSetting("2a56fc4e-41c2-4997-89c1-fca5bf6441f4")?.Value);

        public class BootstrapStrategyOptions
        {
            public readonly string Value;

            public BootstrapStrategyOptions(string value)
            {
                Value = value;
            }

            public BootstrapStrategyOptionsEnum AsEnum()
            {
                return Value switch
                {
                    "explicit-identity" => BootstrapStrategyOptionsEnum.ExplicitIdentity,
                    "first-eligible-user" => BootstrapStrategyOptionsEnum.FirstEligibleUser,
                    "custom-seed-function" => BootstrapStrategyOptionsEnum.CustomSeedFunction,
                    _ => throw new ArgumentOutOfRangeException(nameof(Value), $"{Value} is out of range")
                };
            }

            public bool IsExplicitIdentity()
            {
                return Value == "explicit-identity";
            }

            public bool IsFirstEligibleUser()
            {
                return Value == "first-eligible-user";
            }

            public bool IsCustomSeedFunction()
            {
                return Value == "custom-seed-function";
            }
        }

        public enum BootstrapStrategyOptionsEnum
        {
            ExplicitIdentity,
            FirstEligibleUser,
            CustomSeedFunction,
        }

        public string ExplicitIdentityIssuer() => _groupSettings.GetSetting("526c78df-af33-48ba-84c0-c59abf3c0ba8")?.Value;

        public string ExplicitIdentitySubject() => _groupSettings.GetSetting("3bf76ce0-bcdd-49a4-9e77-23c3d7f2fcc8")?.Value;

        public string FirstEligibleUserNormalizedEmail() => _groupSettings.GetSetting("6ca91851-089c-4179-a2b7-c204f1008f08")?.Value;

        public string CustomSeedFunction() => _groupSettings.GetSetting("8f899d0b-78f5-4490-b4c4-6d43d184ea44")?.Value;
    }

    public class AuthorityCryptography : IGroupSettings
    {
        private readonly IGroupSettings _groupSettings;

        public AuthorityCryptography(IGroupSettings groupSettings)
        {
            _groupSettings = groupSettings;
        }

        public string Id => _groupSettings.Id;

        public string Title
        {
            get => _groupSettings.Title;
            set => _groupSettings.Title = value;
        }

        public ISetting GetSetting(string settingId)
        {
            return _groupSettings.GetSetting(settingId);
        }

        public string ActiveSigningKeyId() => _groupSettings.GetSetting("c1af57eb-af7c-4afc-9ddc-5faca948e7f6")?.Value;

        public string SigningPrivateKey() => _groupSettings.GetSetting("abdf8fc5-e322-4608-bbf7-ec3d19b4e654")?.Value;

        public string ExternalProviderSecretProtectionKey() => _groupSettings.GetSetting("faf42895-dd09-44f7-9a62-b641157a10f0")?.Value;

        public string APIKeyHashingKey() => _groupSettings.GetSetting("1ae9fa59-b680-44cd-91e6-eab45a42dac7")?.Value;

        public bool AllowEphemeralDevelopmentSigningKey() => bool.TryParse(_groupSettings.GetSetting("f7bb987f-e231-429a-9053-d0f2a9d10f4d")?.Value.ToPascalCase(), out var result) && result;
    }

    public class AuthorityDataLifecycle : IGroupSettings
    {
        private readonly IGroupSettings _groupSettings;

        public AuthorityDataLifecycle(IGroupSettings groupSettings)
        {
            _groupSettings = groupSettings;
        }

        public string Id => _groupSettings.Id;

        public string Title
        {
            get => _groupSettings.Title;
            set => _groupSettings.Title = value;
        }

        public ISetting GetSetting(string settingId)
        {
            return _groupSettings.GetSetting(settingId);
        }

        public string RevokedMetadataRetentionDays() => _groupSettings.GetSetting("decde6e3-4053-431a-b254-5a3e7b2195c5")?.Value;

        public string CodeAndDeviceCleanupDelayDays() => _groupSettings.GetSetting("2b23403d-4862-40e2-a5d2-8c8b252400d4")?.Value;

        public string SSOAndRefreshCleanupDays() => _groupSettings.GetSetting("8b48bc3f-151e-45ea-af9b-df341334769c")?.Value;
    }

    public class AuthorityFeatures : IGroupSettings
    {
        private readonly IGroupSettings _groupSettings;

        public AuthorityFeatures(IGroupSettings groupSettings)
        {
            _groupSettings = groupSettings;
        }

        public string Id => _groupSettings.Id;

        public string Title
        {
            get => _groupSettings.Title;
            set => _groupSettings.Title = value;
        }

        public ISetting GetSetting(string settingId)
        {
            return _groupSettings.GetSetting(settingId);
        }

        public bool AuthorizationCode() => bool.TryParse(_groupSettings.GetSetting("67b1ca34-f4c7-41e0-9129-e7e813c9609b")?.Value.ToPascalCase(), out var result) && result;

        public bool ClientCredentials() => bool.TryParse(_groupSettings.GetSetting("3e9fb0c7-3cdf-4bfc-981a-81b0f202e836")?.Value.ToPascalCase(), out var result) && result;

        public bool RefreshToken() => bool.TryParse(_groupSettings.GetSetting("954589af-ec6f-4cb4-a9bb-dcdb0fe2919c")?.Value.ToPascalCase(), out var result) && result;

        public bool DeviceAuthorization() => bool.TryParse(_groupSettings.GetSetting("42d4e6ec-10bd-446f-b255-58787bd66bfe")?.Value.ToPascalCase(), out var result) && result;

        public bool ExternalIdentityProviderBrokering() => bool.TryParse(_groupSettings.GetSetting("7bf7e906-e185-446e-b877-46ee73a08edf")?.Value.ToPascalCase(), out var result) && result;

        public bool ManagementAPIs() => bool.TryParse(_groupSettings.GetSetting("39d3f48e-f1d9-4714-af73-20fb56726999")?.Value.ToPascalCase(), out var result) && result;

        public bool IntegrationEvents() => bool.TryParse(_groupSettings.GetSetting("e392ab19-9091-4fdd-aeaf-5e76f3668e2d")?.Value.ToPascalCase(), out var result) && result;

        public bool LifecycleNotifications() => bool.TryParse(_groupSettings.GetSetting("3e41cb26-1e6f-4905-87c5-61280794ff70")?.Value.ToPascalCase(), out var result) && result;

        public bool Auditing() => bool.TryParse(_groupSettings.GetSetting("05a54a85-566d-4352-bea8-2e944a478ec8")?.Value.ToPascalCase(), out var result) && result;
    }

    public class AuthorityProtocol : IGroupSettings
    {
        private readonly IGroupSettings _groupSettings;

        public AuthorityProtocol(IGroupSettings groupSettings)
        {
            _groupSettings = groupSettings;
        }

        public string Id => _groupSettings.Id;

        public string Title
        {
            get => _groupSettings.Title;
            set => _groupSettings.Title = value;
        }

        public ISetting GetSetting(string settingId)
        {
            return _groupSettings.GetSetting(settingId);
        }

        public string Issuer() => _groupSettings.GetSetting("4061fb5f-baa0-4efa-9f8f-751947b35f64")?.Value;

        public string AccessTokenMinutes() => _groupSettings.GetSetting("8eb0c192-d3d2-48a7-8063-7c88abf7cc6b")?.Value;

        public string IDTokenMinutes() => _groupSettings.GetSetting("515469e7-39bc-47ee-a9dd-c98e4089830d")?.Value;

        public string RefreshTokenDays() => _groupSettings.GetSetting("b9f3da46-9a79-43b9-9315-0341a92e34aa")?.Value;

        public string SSOSessionLifetimeMinutes() => _groupSettings.GetSetting("5b954a48-4d56-4f0d-aa76-f5988b8ef9ca")?.Value;

        public string ClockSkewSeconds() => _groupSettings.GetSetting("4c487158-bf90-485e-8012-294ec3b08195")?.Value;

        public string DevicePollingSeconds() => _groupSettings.GetSetting("cb665b98-93d7-4d9d-88e4-5eb6dd03eaa3")?.Value;

        public string APIKeyPrefix() => _groupSettings.GetSetting("4a4e84b3-044c-4baf-9858-5457ab426ad9")?.Value;

        public bool HTTPSRequiredOutsideDevelopment() => bool.TryParse(_groupSettings.GetSetting("19b6c6fd-4157-4342-a535-c10f5d60eba0")?.Value.ToPascalCase(), out var result) && result;

        public string DeviceAuthorizationMinutes() => _groupSettings.GetSetting("b264f859-8461-4790-aefc-39ffb3600775")?.Value;
    }

    public class AuthorityRoutes : IGroupSettings
    {
        private readonly IGroupSettings _groupSettings;

        public AuthorityRoutes(IGroupSettings groupSettings)
        {
            _groupSettings = groupSettings;
        }

        public string Id => _groupSettings.Id;

        public string Title
        {
            get => _groupSettings.Title;
            set => _groupSettings.Title = value;
        }

        public ISetting GetSetting(string settingId)
        {
            return _groupSettings.GetSetting(settingId);
        }

        public string DiscoveryRoute() => _groupSettings.GetSetting("433af046-20c8-4176-8904-c31600a0dd47")?.Value;

        public string JWKSRoute() => _groupSettings.GetSetting("40459f52-77fb-4b4a-89ac-3966c0ba8cf6")?.Value;

        public string AuthorizationRoute() => _groupSettings.GetSetting("469539a5-9e53-43f6-a0ab-6e94369eb265")?.Value;

        public string CallbackRoute() => _groupSettings.GetSetting("d83743a3-db55-4c01-aad7-816acce2fafb")?.Value;

        public string TokenRoute() => _groupSettings.GetSetting("e6f751e9-a091-4851-bb2f-bffb7c1f2427")?.Value;

        public string DeviceAuthorizationRoute() => _groupSettings.GetSetting("fb2ab754-36c3-4b58-882e-a43965b0a60f")?.Value;

        public string DeviceVerificationRoute() => _groupSettings.GetSetting("99b198d0-ec3e-407d-b08e-a52bcf334a6e")?.Value;

        public string DeviceApprovalRoute() => _groupSettings.GetSetting("ffdfac1f-8820-4d39-96f9-a015bf981320")?.Value;

        public string UserInfoRoute() => _groupSettings.GetSetting("c3a72a33-9964-42c7-834c-4082a2ba35d0")?.Value;

        public string LogoutRoute() => _groupSettings.GetSetting("c20da934-5784-4b89-a1bf-93556b532f95")?.Value;

        public string ManagementBaseRoute() => _groupSettings.GetSetting("88f097fe-4a84-42c6-9341-0a8af3c7c6f6")?.Value;
    }

    public class AuthorityTenancy : IGroupSettings
    {
        private readonly IGroupSettings _groupSettings;

        public AuthorityTenancy(IGroupSettings groupSettings)
        {
            _groupSettings = groupSettings;
        }

        public string Id => _groupSettings.Id;

        public string Title
        {
            get => _groupSettings.Title;
            set => _groupSettings.Title = value;
        }

        public ISetting GetSetting(string settingId)
        {
            return _groupSettings.GetSetting(settingId);
        }

        public bool TenantScopedCapabilitiesEnabled() => bool.TryParse(_groupSettings.GetSetting("be5ebddb-4d73-49b7-a3cf-d2d70fcdda1b")?.Value.ToPascalCase(), out var result) && result;

        public string ContextualClaimNames() => _groupSettings.GetSetting("b0443e22-c7c1-426a-9c78-d10085b523b1")?.Value;

        public bool InheritanceEvaluationEnabled() => bool.TryParse(_groupSettings.GetSetting("4e03965f-74ec-4e97-b8c4-63e84aefd1ff")?.Value.ToPascalCase(), out var result) && result;
    }
}