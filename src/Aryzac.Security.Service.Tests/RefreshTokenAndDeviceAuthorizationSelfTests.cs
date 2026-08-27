using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class RefreshTokenAndDeviceAuthorizationSelfTests
{
    private static readonly string ServiceProject = FindServiceProject();
    private static readonly string TokenSource = ReadServiceSource("Templates", "SecurityAuthorityTokenEndpoint", "SecurityAuthorityTokenEndpointTemplatePartial.cs");
    private static readonly string DeviceSource = ReadServiceSource("Templates", "SecurityAuthorityDeviceEndpoints", "SecurityAuthorityDeviceEndpointsTemplatePartial.cs");

    public static int Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("Refresh Token rotation updates predecessor and adds one successor atomically", RefreshTokenRotationIsAtomic),
            ("Refresh Token replay revokes the active same-principal successor lineage", RefreshTokenReplayRevokesSuccessorLineage),
            ("concurrent Refresh Token redemption fails closed and revokes the winner lineage", ConcurrentRefreshTokenRedemptionFailsClosed),
            ("Refresh Token redemption rejects inactive mismatched and disallowed principals", RefreshTokenRedemptionRejectsInvalidPrincipals),
            ("Device and User Codes have exact entropy alphabet rendering and lifetimes", DeviceAndUserCodesHaveExactFormats),
            ("User Code normalization accepts canonical hyphenated lowercase input only", UserCodeNormalizationIsStrict),
            ("device review is authenticated and returns the pending grant projection", DeviceReviewIsAuthenticated),
            ("device polling enforces five seconds and exact pending and slow-down errors", DevicePollingEnforcesTimingAndStatuses),
            ("device polling returns exact expiry and denial statuses", DevicePollingReturnsExpiryAndDenialStatuses),
            ("device approval and denial are authenticated validated atomic transitions", DeviceApprovalAndDenialAreAtomic),
            ("approved Device Grants redeem once and concurrent redemption fails closed", DeviceGrantRedemptionIsOneTime),
            ("device flows reject inactive unknown mismatched and disallowed principals before credentials", DeviceFlowsRejectInvalidPrincipalsBeforeCredentials)
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

    private static void RefreshTokenRotationIsAtomic()
    {
        var redeem = RefreshTokenRedemptionBlock();
        Contains(redeem, "BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.RefreshTokenRotation, true, cancellationToken)");
        Contains(redeem, "var successorToken = new SecurityAuthorityRefreshToken");
        Contains(redeem, "ReplacedByTokenId = successorToken.Id, RevokedAt = now");
        Contains(redeem, "new SecurityAuthorityDeferredCredential(operation.OperationId, () => successorClearToken)");
        Contains(redeem, "operation.Records.UpdateAsync(rotatedToken, token.ConcurrencyToken, cancellationToken)");
        Contains(redeem, "operation.Records.AddAsync(successorToken, cancellationToken)");
        Before(redeem, "receipt = await operation.CommitAsync(cancellationToken)", "deferredSuccessor.Reveal(receipt)");
    }

    private static void RefreshTokenReplayRevokesSuccessorLineage()
    {
        var redeem = RefreshTokenRedemptionBlock();
        var lineage = RefreshTokenLineageBlock();
        Contains(redeem, "if (!string.IsNullOrWhiteSpace(token.ReplacedByTokenId))");
        Contains(redeem, "await RevokeActiveSuccessorLineageAsync(operation.Records, token, now, cancellationToken)");
        Contains(redeem, "The rotated Refresh Token was replayed and its active successor lineage was revoked.");
        Contains(lineage, "var visited = new HashSet<string>(StringComparer.Ordinal)");
        Contains(lineage, "while (!string.IsNullOrWhiteSpace(successorId))");
        Contains(lineage, "successor.UserId, replayedToken.UserId, StringComparison.Ordinal");
        Contains(lineage, "successor.OAuthClientId, replayedToken.OAuthClientId, StringComparison.Ordinal");
        Contains(lineage, "successor.RevokedAt is null && successor.ExpiresAt > now");
        Contains(lineage, "records.UpdateAsync(revokedSuccessor, successor.ConcurrencyToken, cancellationToken)");
    }

    private static void ConcurrentRefreshTokenRedemptionFailsClosed()
    {
        var redeem = RefreshTokenRedemptionBlock();
        Contains(redeem, "catch (Exception exception) when (isConcurrencyConflict(exception))");
        Contains(redeem, "await operation.RollbackAsync(cancellationToken)");
        Contains(redeem, "await using var replayOperation = await persistence.BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.RefreshTokenRotation, true, cancellationToken)");
        Contains(redeem, "string.IsNullOrWhiteSpace(replayedToken.ReplacedByTokenId)");
        Contains(redeem, "await RevokeActiveSuccessorLineageAsync(replayOperation.Records, replayedToken, now, cancellationToken)");
        Contains(redeem, "The Refresh Token was concurrently redeemed and its active successor lineage was revoked.");
    }

    private static void RefreshTokenRedemptionRejectsInvalidPrincipals()
    {
        var redeem = RefreshTokenRedemptionBlock();
        Contains(redeem, "!client.IsActive || !client.AllowedGrantTypes.Contains(\\\"refresh_token\\\", StringComparer.Ordinal)");
        Contains(redeem, "scopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))");
        Contains(redeem, "!string.Equals(token.OAuthClientId, client.Id, StringComparison.Ordinal)");
        Contains(redeem, "token.ExpiresAt <= now || token.RevokedAt is not null || token.LastUsedAt is not null");
        Contains(redeem, "user is null || !string.Equals(user.Status, \\\"Active\\\", StringComparison.Ordinal)");
        Contains(redeem, "OAuthError(context, \\\"invalid_grant\\\"");
    }

    private static void DeviceAndUserCodesHaveExactFormats()
    {
        var authorize = DeviceAuthorizationBlock();
        var helpers = DeviceCodeHelpersBlock();
        Contains(helpers, "SecurityAuthorityBase64Url.Encode(RandomNumberGenerator.GetBytes(32))");
        Contains(helpers, "const string alphabet = \\\"3467CDFHJKMNPRTVWXY\\\"");
        Contains(helpers, "Enumerable.Range(0, 8)");
        Contains(helpers, "return $\\\"{canonicalUserCode[..4]}-{canonicalUserCode[4..]}\\\"");
        Contains(DeviceSource, "ExpiresInSeconds = 900;");
        Contains(DeviceSource, "PollingIntervalSeconds != 5");
        Contains(authorize, "[\"device_code\"] = deferredDeviceCode.Reveal(receipt)");
        Contains(authorize, "[\"user_code\"] = renderedUserCode");
        Contains(authorize, "[\"verification_uri_complete\"] = $\"{verificationUri}?userCode={Uri.EscapeDataString(renderedUserCode)}\"");
        Contains(authorize, "[\"expires_in\"] = configuration.ExpiresInSeconds");
        Contains(authorize, "[\"interval\"] = configuration.PollingIntervalSeconds");
    }

    private static void UserCodeNormalizationIsStrict()
    {
        var helpers = DeviceCodeHelpersBlock();
        Contains(helpers, "value.Replace(\\\"-\\\", string.Empty, StringComparison.Ordinal).ToUpperInvariant()");
        Contains(helpers, "canonical.Length == 8");
        Contains(helpers, "canonical.All(character => alphabet.Contains(character)) ? canonical : null");
        DoesNotContain(helpers, "OrdinalIgnoreCase");
    }

    private static void DeviceReviewIsAuthenticated()
    {
        var review = DeviceReviewBlock();
        Contains(review, "resolveActiveUser(context, cancellationToken)");
        Contains(review, "user is null || !string.Equals(user.Status, \"Active\", StringComparison.Ordinal)");
        Contains(review, "return Results.Unauthorized()");
        Contains(review, "NormalizeUserCode(userCode)");
        Contains(review, "invalid_user_code\", \"The User Code is unknown.\", StatusCodes.Status404NotFound");
        Contains(review, "client is null || !client.IsActive");
        Contains(review, "[\"client\"] = client.DisplayName");
        Contains(review, "[\"scopes\"] = grant.RequestedScopes");
        Contains(review, "[\"expires_at\"] = grant.ExpiresAt");
        Contains(review, "[\"status\"] = status");
        Contains(review, "RequireAuthorization()");
    }

    private static void DevicePollingEnforcesTimingAndStatuses()
    {
        var redeem = DeviceRedemptionBlock();
        Contains(DeviceSource, "PollingIntervalSeconds != 5");
        Contains(redeem, "now < grant.LastPolledAt.Value.AddSeconds(grant.PollingIntervalSeconds)");
        Contains(redeem, "grant with { LastPolledAt = now, ConcurrencyToken = NewOpaqueIdentifier() }");
        Contains(redeem, "OAuthError(\"slow_down\", \"Polling occurred before the configured interval elapsed.\")");
        Contains(redeem, "string.Equals(grant.Status, \"Pending\", StringComparison.Ordinal)");
        Contains(redeem, "OAuthError(\"authorization_pending\", \"The Device User has not completed authorization.\")");
        Contains(DeviceSource, "statusCode: StatusCodes.Status400BadRequest");
    }

    private static void DevicePollingReturnsExpiryAndDenialStatuses()
    {
        var redeem = DeviceRedemptionBlock();
        Contains(redeem, "grant.ExpiresAt <= now || string.Equals(grant.Status, \"Expired\", StringComparison.Ordinal)");
        Contains(redeem, "grant with { Status = \"Expired\", ConcurrencyToken = NewOpaqueIdentifier() }");
        Contains(redeem, "OAuthError(\"expired_token\", \"The Device Grant has expired.\")");
        Contains(redeem, "string.Equals(grant.Status, \"Denied\", StringComparison.Ordinal)");
        Contains(redeem, "OAuthErrorAsync(operation, \"access_denied\", \"The Device User denied this request.\"");
    }

    private static void DeviceApprovalAndDenialAreAtomic()
    {
        var approval = DeviceApprovalBlock();
        Contains(approval, "resolveActiveUser(context, cancellationToken)");
        Contains(approval, "decision must be approve or deny");
        Contains(approval, "BeginAtomicOperationAsync(SecurityAuthorityAtomicOperationKind.TokenRedemption, true, cancellationToken)");
        Contains(approval, "activeUser is null || !string.Equals(activeUser.Status, \"Active\", StringComparison.Ordinal)");
        Contains(approval, "client is null || !client.IsActive");
        Contains(approval, "grant.RequestedScopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))");
        Contains(approval, "Status = \"Approved\", UserId = user.Id, ApprovedAt = now");
        Contains(approval, "Status = \"Denied\", UserId = user.Id, DeniedAt = now");
        Contains(approval, "operation.Records.UpdateAsync(updated, grant.ConcurrencyToken, cancellationToken)");
        Contains(approval, "operation.CommitAsync(cancellationToken)");
        Contains(approval, "RequireAuthorization()");
    }

    private static void DeviceGrantRedemptionIsOneTime()
    {
        var redeem = DeviceRedemptionBlock();
        Contains(redeem, "string.Equals(grant.Status, \"Redeemed\", StringComparison.Ordinal)");
        Contains(redeem, "The Device Grant was already redeemed.");
        Contains(redeem, "string.Equals(grant.Status, \"Approved\", StringComparison.Ordinal)");
        Contains(redeem, "idToken = CreateIdToken(configuration, signingKeys, client.ClientIdentifier, user.Id, now)");
        Contains(redeem, "deferredRefreshToken = new SecurityAuthorityDeferredCredential(operation.OperationId, () => clearRefreshToken)");
        DoesNotContain(redeem, "configuration.RefreshTokenEnabled && client.AllowedGrantTypes.Contains(\"refresh_token\", StringComparer.Ordinal)");
        Contains(redeem, "redeemedGrant = grant with { Status = \"Redeemed\", RedeemedAt = now, LastPolledAt = now");
        Contains(redeem, "operation.Records.UpdateAsync(redeemedGrant, grant.ConcurrencyToken, cancellationToken)");
        Before(redeem, "receipt = await operation.CommitAsync(cancellationToken)", "deferredRefreshToken!.Reveal(receipt)");
        Contains(redeem, "catch (Exception exception) when (isConcurrencyConflict(exception))");
        Contains(redeem, "OAuthError(\"invalid_grant\", \"The Device Grant was concurrently redeemed.\")");
    }

    private static void DeviceFlowsRejectInvalidPrincipalsBeforeCredentials()
    {
        var authorize = DeviceAuthorizationBlock();
        var approval = DeviceApprovalBlock();
        var redeem = DeviceRedemptionBlock();
        Contains(authorize, "client is null");
        Contains(authorize, "ValidateDeviceAuthorizationClient(client)");
        Contains(authorize, "scopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))");
        Contains(approval, "The authenticated User is not active.");
        Contains(approval, "The OAuth Client is inactive or unknown.");
        Contains(approval, "A requested Scope is no longer allowed for this client.");
        Contains(redeem, "grant is null || !credentialHasher.VerifyCredential(clearDeviceCode, grant.DeviceCodeHash) || !string.Equals(grant.OAuthClientId, client.Id, StringComparison.Ordinal)");
        Contains(redeem, "grant.RequestedScopes.Any(scope => !client.AllowedScopes.Contains(scope, StringComparer.Ordinal))");
        Contains(redeem, "user is null || !string.Equals(user.Status, \"Active\", StringComparison.Ordinal)");
        Before(redeem, "The Device Grant owner is not active.", "accessToken = CreateAccessToken");
    }

    private static string RefreshTokenRedemptionBlock() =>
        GetBlock(TokenSource, ".AddMethod(\"ValueTask<IResult>\", \"RedeemRefreshTokenAsync\"", ".AddMethod(\"ValueTask\", \"RevokeActiveSuccessorLineageAsync\"");

    private static string RefreshTokenLineageBlock() =>
        GetBlock(TokenSource, ".AddMethod(\"ValueTask\", \"RevokeActiveSuccessorLineageAsync\"", ".AddMethod(\"ValueTask<IResult>\", \"IssueClientCredentialsAsync\"");

    private static string DeviceAuthorizationBlock() =>
        GetBlock(DeviceSource, "endpoints.MapPost(configuration.AuthorizationPath", "endpoints.MapGet(configuration.VerificationPath");

    private static string DeviceReviewBlock() =>
        GetBlock(DeviceSource, "endpoints.MapGet(configuration.VerificationPath", "endpoints.MapPost(configuration.ApprovalPath");

    private static string DeviceApprovalBlock() =>
        GetBlock(DeviceSource, "endpoints.MapPost(configuration.ApprovalPath", "\"\"\");");

    private static string DeviceRedemptionBlock() =>
        GetBlock(DeviceSource, ".AddMethod(\"ValueTask<IResult>\", \"RedeemDeviceCodeAsync\"", ".AddMethod(\"ValueTask<IResult>\", \"RollbackOAuthErrorAsync\"");

    private static string DeviceCodeHelpersBlock() =>
        GetBlock(DeviceSource, ".AddMethod(\"string\", \"NewDeviceCode\"", ".AddMethod(\"string[]?\", \"ParseScopes\"");

    private static string GetBlock(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        True(start >= 0, $"Marker '{startMarker}' was not found.");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        True(end >= 0, $"Marker '{endMarker}' was not found after '{startMarker}'.");
        return source[start..end];
    }

    private static string ReadServiceSource(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { ServiceProject }.Concat(path).ToArray()));

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

    private static void Before(string source, string first, string second)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);
        True(firstIndex >= 0, $"Expected source to contain '{first}'.");
        True(secondIndex >= 0, $"Expected source to contain '{second}'.");
        True(firstIndex < secondIndex, $"Expected '{first}' before '{second}'.");
    }

    private static void Contains(string source, string value) =>
        True(source.Contains(value, StringComparison.Ordinal), $"Expected source to contain '{value}'.");

    private static void DoesNotContain(string source, string value) =>
        True(!source.Contains(value, StringComparison.Ordinal), $"Expected source not to contain '{value}'.");

    private static void True(bool condition, string message = "Expected condition to be true.")
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
