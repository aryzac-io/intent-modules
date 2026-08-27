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

namespace Aryzac.Security.Service.Templates.SecurityAuthorityConformanceTests
{
    [IntentManaged(Mode.Fully, Body = Mode.Merge)]
    public partial class SecurityAuthorityConformanceTestsTemplate : CSharpTemplateBase<object>, ICSharpFileBuilderTemplate
    {
        public const string TemplateId = "Aryzac.Security.Service.SecurityAuthorityConformanceTests";

        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public SecurityAuthorityConformanceTestsTemplate(IOutputTarget outputTarget, object model = null) : base(TemplateId, outputTarget, model)
        {
            CSharpFile = new CSharpFile(this.GetNamespace(), this.GetFolderPath())
                .AddUsing("System")
                .AddUsing("System.Collections.Generic")
                .AddUsing("System.Linq")
                .AddUsing("System.Threading")
                .AddUsing("System.Threading.Tasks")
                .AddRecord("SecurityAuthorityConformanceCase", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "RequirementId");
                        ctor.AddParameter("string", "Category");
                        ctor.AddParameter("string", "Description");
                    });
                })
                .AddRecord("SecurityAuthorityConformanceObservation", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("bool", "Passed");
                        ctor.AddParameter("string", "Detail");
                    });
                })
                .AddRecord("SecurityAuthorityConformanceFailure", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("string", "Fixture");
                        ctor.AddParameter("string", "RequirementId");
                        ctor.AddParameter("string", "Detail");
                    });
                })
                .AddRecord("SecurityAuthorityConformanceReport", record =>
                {
                    record.Sealed();
                    record.AddPrimaryConstructor(ctor =>
                    {
                        ctor.AddParameter("int", "Executed");
                        ctor.AddParameter("IReadOnlyList<SecurityAuthorityConformanceFailure>", "Failures");
                    });
                })
                .AddInterface("ISecurityAuthorityConformanceFixture", @interface =>
                {
                    @interface.AddProperty("string", "Name", property => property.WithoutSetter());
                    @interface.AddProperty("string", "HostKind", property => property.WithoutSetter());
                    @interface.AddMethod("bool", "IsFeatureEnabled", method => method.AddParameter("string", "feature"));
                    @interface.AddMethod("ValueTask<SecurityAuthorityConformanceObservation>", "ExecuteAsync", method =>
                    {
                        method.AddParameter("SecurityAuthorityConformanceCase", "testCase");
                        method.AddParameter("CancellationToken", "cancellationToken");
                    });
                })
                .AddClass("SecurityAuthorityConformanceTests", @class =>
                {
                    @class.Sealed();
                    @class.AddMethod("IReadOnlyList<string>", "GetOptionalFeatures", method =>
                    {
                        method.Private().Static();
                        method.AddStatement("return new[] { \"refresh-tokens\", \"device-authorization\", \"external-identity-providers\", \"management-apis\", \"integration-events\", \"lifecycle-notifications\", \"auditing\" };");
                    });
                    @class.AddMethod("ValueTask<SecurityAuthorityConformanceReport>", "RunAsync", method =>
                    {
                        method.Static().Async();
                        method.AddParameter("IReadOnlyList<ISecurityAuthorityConformanceFixture>", "fixtures");
                        method.AddParameter("CancellationToken", "cancellationToken");
                        method.AddStatement("""
                            ArgumentNullException.ThrowIfNull(fixtures);
                            RequireHostFixture(fixtures, "DedicatedHost");
                            RequireHostFixture(fixtures, "ExistingHost");
                            RequireFeatureCombinations(fixtures);

                            var failures = new List<SecurityAuthorityConformanceFailure>();
                            var executed = 0;
                            var cases = BuildCases();
                            foreach (var fixture in fixtures.OrderBy(x => x.Name, StringComparer.Ordinal))
                            {
                            foreach (var testCase in cases)
                            {
                            cancellationToken.ThrowIfCancellationRequested();
                            var observation = await fixture.ExecuteAsync(testCase, cancellationToken);
                            executed++;
                            if (!observation.Passed)
                            {
                            failures.Add(new SecurityAuthorityConformanceFailure(fixture.Name, testCase.RequirementId, observation.Detail));
                            }
                            }
                            }

                            return new SecurityAuthorityConformanceReport(executed, failures);
                            """);
                    });
                    @class.AddMethod("IReadOnlyList<SecurityAuthorityConformanceCase>", "BuildCases", method =>
                    {
                        method.Private().Static();
                        method.AddStatement("""
                            var cases = new List<SecurityAuthorityConformanceCase>();
                            AddRequirement(cases, "R4", 8, "discovery", "OIDC discovery and JWKS");
                            AddRequirement(cases, "R5", 7, "client-validation", "OAuth Client validation");
                            AddRequirement(cases, "R6", 8, "authorization-code", "Authorization Code and PKCE");
                            AddRequirement(cases, "R7", 10, "token-endpoint", "token endpoint and claims");
                            AddRequirement(cases, "R8", 8, "refresh-and-device", "Refresh Token and Device Authorization");
                            AddRequirement(cases, "R9", 7, "session", "SSO Session, userinfo, and logout");
                            AddRequirement(cases, "R10", 7, "external-provider", "external provider brokering");
                            cases.AddRange(new[]
                            {
                            new SecurityAuthorityConformanceCase("R12.atomic-api-key-regeneration", "atomicity", "API Key regeneration revokes and replaces atomically without exposing retained clear secrets"),
                            new SecurityAuthorityConformanceCase("R12.secret-exclusion", "secret-exclusion", "stored and projected API Key material excludes clear credentials"),
                            new SecurityAuthorityConformanceCase("R13.ordinal-permissions", "comparison", "Permission Keys and resource identifiers use documented ordinal case rules"),
                            new SecurityAuthorityConformanceCase("R14.idempotency", "idempotency", "management idempotency replays the original outcome and rejects changed requests"),
                            new SecurityAuthorityConformanceCase("R14.optimistic-concurrency", "concurrency", "stale management writes return conflict without mutation"),
                            new SecurityAuthorityConformanceCase("R14.secret-exclusion", "secret-exclusion", "management projections exclude hashes, encrypted values, private keys, and clear credentials"),
                            new SecurityAuthorityConformanceCase("R15.event-envelope", "integration-events", "versioned event envelopes validate identifiers, UTC occurrence, correlation, source, and payload"),
                            new SecurityAuthorityConformanceCase("R15.event-at-most-once", "idempotency", "ten replays yield one record and one effective lifecycle transition"),
                            new SecurityAuthorityConformanceCase("R15.event-rollback", "atomicity", "event rejection preserves state and reports event identifier with exact reason"),
                            new SecurityAuthorityConformanceCase("R15.post-commit", "delivery", "notifications and audits occur after commit and adapter failure cannot replay mutation"),
                            new SecurityAuthorityConformanceCase("R15.secret-exclusion", "secret-exclusion", "audit changed-field names exclude secret-bearing fields"),
                            new SecurityAuthorityConformanceCase("R16.atomic-operations", "atomicity", "redemption, rotation, regeneration, bootstrap, provisioning, and deduplication are atomic"),
                            new SecurityAuthorityConformanceCase("R16.failure-rollback", "atomicity", "pre-commit validation, authorization, persistence, cryptographic, and Tenant failures rollback"),
                            new SecurityAuthorityConformanceCase("R16.optimistic-concurrency", "concurrency", "all mutable authority records reject stale concurrency tokens"),
                            new SecurityAuthorityConformanceCase("R16.retention", "retention", "revoked metadata and terminal timestamps remain without clear redeemable secrets"),
                            new SecurityAuthorityConformanceCase("R16.cleanup-windows", "retention", "cleanup honors 24-hour to seven-day and 30-day to 90-day windows"),
                            new SecurityAuthorityConformanceCase("R16.cleanup-race", "concurrency", "exactly one cleanup or validation/redemption operation wins"),
                            new SecurityAuthorityConformanceCase("R16.utc-stable-comparisons", "determinism", "timestamps are UTC, identifiers stable, and documented comparisons exact"),
                            new SecurityAuthorityConformanceCase("R16.enabled-disabled-features", "configuration", "enabled and disabled feature combinations remain operational"),
                            new SecurityAuthorityConformanceCase("R16.dedicated-host", "installation", "dedicated-host fixture exposes the complete configured authority"),
                            new SecurityAuthorityConformanceCase("R16.existing-host", "installation", "existing-host fixture preserves host registrations and middleware"),
                            new SecurityAuthorityConformanceCase("R16.additive-installation", "installation", "installation remains additive without replacing host services"),
                            new SecurityAuthorityConformanceCase("R16.repeat-generation", "generation", "repeat generation is idempotent and duplicate free")
                            });
                            return cases;
                            """);
                    });
                    @class.AddMethod("void", "AddRequirement", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("ICollection<SecurityAuthorityConformanceCase>", "cases");
                        method.AddParameter("string", "requirement");
                        method.AddParameter("int", "criterionCount");
                        method.AddParameter("string", "category");
                        method.AddParameter("string", "description");
                        method.AddStatement("""
                            for (var criterion = 1; criterion <= criterionCount; criterion++)
                            {
                            cases.Add(new SecurityAuthorityConformanceCase($"{requirement}.{criterion}", category, $"{description} acceptance criterion {criterion}"));
                            }
                            """);
                    });
                    @class.AddMethod("void", "RequireHostFixture", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("IReadOnlyList<ISecurityAuthorityConformanceFixture>", "fixtures");
                        method.AddParameter("string", "hostKind");
                        method.AddStatement("""
                            if (!fixtures.Any(fixture => string.Equals(fixture.HostKind, hostKind, StringComparison.Ordinal)))
                            {
                            throw new InvalidOperationException($"A {hostKind} Security Authority conformance fixture is required.");
                            }
                            """);
                    });
                    @class.AddMethod("void", "RequireFeatureCombinations", method =>
                    {
                        method.Private().Static();
                        method.AddParameter("IReadOnlyList<ISecurityAuthorityConformanceFixture>", "fixtures");
                        method.AddStatement("""
                            foreach (var feature in GetOptionalFeatures())
                            {
                            if (!fixtures.Any(fixture => fixture.IsFeatureEnabled(feature)))
                            {
                            throw new InvalidOperationException($"At least one conformance fixture must enable '{feature}'.");
                            }
                            if (!fixtures.Any(fixture => !fixture.IsFeatureEnabled(feature)))
                            {
                            throw new InvalidOperationException($"At least one conformance fixture must disable '{feature}'.");
                            }
                            }
                            """);
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
