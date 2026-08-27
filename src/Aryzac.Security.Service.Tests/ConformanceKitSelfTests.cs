using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class ConformanceKitSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string Source = File.ReadAllText(Path.Combine(ServiceProject, "Templates", "SecurityAuthorityConformanceTests", "SecurityAuthorityConformanceTestsTemplatePartial.cs"));

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("R4 through R10 criteria are enumerated exactly", ProtocolCriteriaAreEnumerated),
            ("atomicity rollback concurrency retention and secret safety are covered", SafetyCriteriaAreEnumerated),
            ("dedicated and existing host fixtures are mandatory", HostFixturesAreMandatory),
            ("enabled and disabled feature combinations are mandatory", FeatureCombinationsAreMandatory),
            ("additive installation and repeat generation are covered", InstallationAndGenerationAreCovered),
            ("the generated suite executes every case for every fixture", EveryCaseExecutesForEveryFixture),
            ("the conformance template contains no implementation stubs", ContainsNoStubs)
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

        foreach (var failure in failures) Console.Error.WriteLine(failure);
        return failures.Count == 0 ? 0 : 1;
    }

    private static void ProtocolCriteriaAreEnumerated()
    {
        foreach (var requirement in new[] { ("R4", 8), ("R5", 7), ("R6", 8), ("R7", 10), ("R8", 8), ("R9", 7), ("R10", 7) })
        {
            Contains($"AddRequirement(cases, \"{requirement.Item1}\", {requirement.Item2}");
        }
    }

    private static void SafetyCriteriaAreEnumerated()
    {
        foreach (var marker in new[]
        {
            "R12.atomic-api-key-regeneration", "R12.secret-exclusion", "R13.ordinal-permissions",
            "R14.idempotency", "R14.optimistic-concurrency", "R14.secret-exclusion",
            "R15.event-envelope", "R15.event-at-most-once", "R15.event-rollback", "R15.post-commit", "R15.secret-exclusion",
            "R16.atomic-operations", "R16.failure-rollback", "R16.optimistic-concurrency", "R16.retention",
            "R16.cleanup-windows", "R16.cleanup-race", "R16.utc-stable-comparisons"
        })
        {
            Contains(marker);
        }
    }

    private static void HostFixturesAreMandatory()
    {
        Contains("RequireHostFixture(fixtures, \"DedicatedHost\")");
        Contains("RequireHostFixture(fixtures, \"ExistingHost\")");
        Contains("string.Equals(fixture.HostKind, hostKind, StringComparison.Ordinal)");
    }

    private static void FeatureCombinationsAreMandatory()
    {
        foreach (var feature in new[] { "refresh-tokens", "device-authorization", "external-identity-providers", "management-apis", "integration-events", "lifecycle-notifications", "auditing" })
        {
            Contains($"\\\"{feature}\\\"");
        }
        Contains("fixture.IsFeatureEnabled(feature)");
        Contains("!fixture.IsFeatureEnabled(feature)");
    }

    private static void InstallationAndGenerationAreCovered()
    {
        Contains("R16.additive-installation");
        Contains("R16.repeat-generation");
        Contains("preserves host registrations and middleware");
    }

    private static void EveryCaseExecutesForEveryFixture()
    {
        Contains("var cases = BuildCases()");
        Contains("foreach (var fixture in fixtures.OrderBy(x => x.Name, StringComparer.Ordinal))");
        Contains("foreach (var testCase in cases)");
        Contains("await fixture.ExecuteAsync(testCase, cancellationToken)");
        Contains("new SecurityAuthorityConformanceFailure(fixture.Name, testCase.RequirementId, observation.Detail)");
    }

    private static void ContainsNoStubs()
    {
        DoesNotContain("NotImplementedException");
        DoesNotContain("TODO: Implement");
        DoesNotContain("exampleParam");
    }

    private static string FindServiceProject()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Aryzac.Security.Service");
            if (Directory.Exists(candidate)) return candidate;
            candidate = Path.Combine(directory.FullName, "src", "Aryzac.Security.Service");
            if (Directory.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Aryzac.Security.Service project directory.");
    }

    private static void Contains(string value) => True(Source.Contains(value, StringComparison.Ordinal), $"Expected to find '{value}'.");

    private static void DoesNotContain(string value) => True(!Source.Contains(value, StringComparison.Ordinal), $"Did not expect to find '{value}'.");

    private static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
