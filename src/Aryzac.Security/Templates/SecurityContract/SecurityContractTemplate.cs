using System;
using Intent.Engine;
using Intent.Modules.Common;
using Intent.Modules.Common.CSharp.Templates;

namespace Aryzac.Security.Templates.SecurityContract;

public sealed class SecurityContractTemplate : CSharpTemplateBase<object>
{
    public const string TemplateId = "Aryzac.Security.SecurityContract";

    public SecurityContractTemplate(IOutputTarget outputTarget, object model = null)
        : base(TemplateId, outputTarget, model)
    {
    }

    protected override CSharpFileConfig DefineFileConfig()
    {
        return new CSharpFileConfig(
            className: "SecurityContract",
            @namespace: this.GetNamespace(),
            relativeLocation: this.GetFolderPath(),
            fileName: "SecurityContract");
    }

    public override string TransformText()
    {
        var targetNamespace = this.GetNamespace();
        var isSecurityService = string.Equals(
            ExecutionContext.GetApplicationConfig().Name,
            "Verentis.Security",
            StringComparison.Ordinal);
        var role = isSecurityService
            ? "Security"
            : "Consumer";
        var configureSecurityService = isSecurityService
            ? "        ConfigureSecurityServiceDeviations(services);\n"
            : string.Empty;
        var securityServiceExtensionPoint = isSecurityService
            ? """

    static partial void ConfigureSecurityServiceDeviations(IServiceCollection services);
"""
            : string.Empty;

        return $$"""
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

[assembly: global::{{targetNamespace}}.BackendServiceSecurityContractAttribute(
    global::{{targetNamespace}}.SecurityContract.ContractVersion,
    global::{{targetNamespace}}.BackendServiceSecurityRole.{{role}})]

namespace {{targetNamespace}};

public static class SecurityContract
{
    public const string ContractVersion = "1.0";
}

public enum CallerCredentialKind
{
    VerentisJwt,
    VerentisApiKey,
    ServiceToken
}

public enum BackendServiceSecurityRole
{
    Consumer,
    Security
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class BackendServiceSecurityContractAttribute : Attribute
{
    public BackendServiceSecurityContractAttribute(
        string contractVersion,
        BackendServiceSecurityRole serviceRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contractVersion);
        ContractVersion = contractVersion;
        ServiceRole = serviceRole;
    }

    public string ContractVersion { get; }

    public BackendServiceSecurityRole ServiceRole { get; }
}

public sealed class Principal
{
    public Principal(
        Guid principalId,
        string principalType,
        Guid? accountId = null,
        Guid? workspaceId = null,
        IEnumerable<string>? scopes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principalType);

        PrincipalId = principalId;
        PrincipalType = principalType;
        AccountId = accountId;
        WorkspaceId = workspaceId;
        Scopes = (scopes ?? Array.Empty<string>())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .ToFrozenSet(StringComparer.Ordinal);
    }

    public Guid PrincipalId { get; }

    public string PrincipalType { get; }

    public Guid? AccountId { get; }

    public Guid? WorkspaceId { get; }

    public IReadOnlySet<string> Scopes { get; }
}

public static class PrincipalClaimTypes
{
    public const string Subject = "sub";
    public const string PrincipalType = "principal_type";
    public const string AccountId = "account_id";
    public const string WorkspaceId = "workspace_id";
    public const string Scope = "scope";
}

public interface IPrincipalResolver
{
    Principal? Resolve(ClaimsPrincipal claimsPrincipal);
}

internal sealed class PrincipalResolver : IPrincipalResolver
{
    public Principal? Resolve(ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        if (claimsPrincipal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var principalIdValue = FindFirstValue(
            claimsPrincipal,
            PrincipalClaimTypes.Subject,
            ClaimTypes.NameIdentifier);
        var principalType = FindFirstValue(
            claimsPrincipal,
            PrincipalClaimTypes.PrincipalType);

        if (!Guid.TryParse(principalIdValue, out var principalId) ||
            string.IsNullOrWhiteSpace(principalType))
        {
            return null;
        }

        return new Principal(
            principalId,
            principalType,
            ParseOptionalGuid(claimsPrincipal, PrincipalClaimTypes.AccountId),
            ParseOptionalGuid(claimsPrincipal, PrincipalClaimTypes.WorkspaceId),
            claimsPrincipal.FindAll(PrincipalClaimTypes.Scope)
                .SelectMany(claim => claim.Value.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)));
    }

    private static string? FindFirstValue(
        ClaimsPrincipal claimsPrincipal,
        params string[] claimTypes)
    {
        return claimTypes
            .Select(claimsPrincipal.FindFirst)
            .FirstOrDefault(claim => claim is not null)
            ?.Value;
    }

    private static Guid? ParseOptionalGuid(
        ClaimsPrincipal claimsPrincipal,
        string claimType)
    {
        return Guid.TryParse(
            claimsPrincipal.FindFirst(claimType)?.Value,
            out var value)
            ? value
            : null;
    }
}

public sealed class CallerCredential
{
    internal CallerCredential(
        string authorizationHeaderValue,
        CallerCredentialKind? kind,
        bool isInternalServiceKey)
    {
        _authorizationHeaderValue = authorizationHeaderValue;
        Kind = kind;
        IsInternalServiceKey = isInternalServiceKey;
    }

    private readonly string _authorizationHeaderValue;

    public CallerCredentialKind? Kind { get; }

    public bool IsInternalServiceKey { get; }

    public string GetAuthorizationHeaderValue()
    {
        return _authorizationHeaderValue;
    }

    public override string ToString()
    {
        return IsInternalServiceKey
            ? "CallerCredential(ApiKey)"
            : $"CallerCredential({Kind})";
    }
}

public sealed record CredentialChallengeResult(
    int StatusCode,
    string Code,
    string WwwAuthenticate)
{
    public static CredentialChallengeResult MissingCredential { get; } =
        new(StatusCodes.Status401Unauthorized, "missing_credential", "Bearer");

    public static CredentialChallengeResult UnsupportedCredentialScheme { get; } =
        new(StatusCodes.Status401Unauthorized, "unsupported_credential_scheme", "Bearer");

    public static CredentialChallengeResult MalformedCredential { get; } =
        new(StatusCodes.Status401Unauthorized, "malformed_credential", "Bearer");
}

public sealed record CallerCredentialParseResult(
    CallerCredential? Credential,
    CredentialChallengeResult? Challenge)
{
    public bool IsSuccess => Credential is not null;

    public static CallerCredentialParseResult Success(CallerCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        return new CallerCredentialParseResult(credential, null);
    }

    public static CallerCredentialParseResult Rejected(CredentialChallengeResult challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        return new CallerCredentialParseResult(null, challenge);
    }
}

public static class AuthorizationHeaderParser
{
    private const string AuthorizationHeaderName = "Authorization";
    private const string BearerScheme = "Bearer";
    private const string ApiKeyScheme = "ApiKey";
    private const string VerentisApiKeyPrefix = "vrt_";

    public static CallerCredentialParseResult Parse(IHeaderDictionary headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        return headers.TryGetValue(AuthorizationHeaderName, out var values)
            ? Parse(values)
            : CallerCredentialParseResult.Rejected(
                CredentialChallengeResult.MissingCredential);
    }

    public static CallerCredentialParseResult Parse(StringValues values)
    {
        if (values.Count != 1)
        {
            return CallerCredentialParseResult.Rejected(
                CredentialChallengeResult.MalformedCredential);
        }

        var authorizationHeaderValue = values[0];
        if (string.IsNullOrWhiteSpace(authorizationHeaderValue) ||
            authorizationHeaderValue.Contains(',', StringComparison.Ordinal))
        {
            return CallerCredentialParseResult.Rejected(
                CredentialChallengeResult.MalformedCredential);
        }

        var segments = authorizationHeaderValue.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2 || segments[1].Any(char.IsWhiteSpace))
        {
            return CallerCredentialParseResult.Rejected(
                CredentialChallengeResult.MalformedCredential);
        }

        if (string.Equals(segments[0], BearerScheme, StringComparison.OrdinalIgnoreCase))
        {
            var kind = segments[1].StartsWith(
                VerentisApiKeyPrefix,
                StringComparison.Ordinal)
                ? CallerCredentialKind.VerentisApiKey
                : CallerCredentialKind.VerentisJwt;

            return CallerCredentialParseResult.Success(
                new CallerCredential(authorizationHeaderValue, kind, false));
        }

        if (string.Equals(segments[0], ApiKeyScheme, StringComparison.OrdinalIgnoreCase))
        {
            return CallerCredentialParseResult.Success(
                new CallerCredential(authorizationHeaderValue, null, true));
        }

        return CallerCredentialParseResult.Rejected(
            CredentialChallengeResult.UnsupportedCredentialScheme);
    }
}

internal sealed class CallerCredentialAuthorizationFilter :
    IAsyncAuthorizationFilter,
    IOrderedFilter
{
    public int Order => int.MinValue;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Filters.OfType<IAllowAnonymousFilter>().Any() ||
            context.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>()
                .Any())
        {
            return Task.CompletedTask;
        }

        var parseResult = AuthorizationHeaderParser.Parse(
            context.HttpContext.Request.Headers);
        if (parseResult.IsSuccess)
        {
            return Task.CompletedTask;
        }

        var challenge = parseResult.Challenge!;
        context.HttpContext.Response.Headers.WWWAuthenticate =
            challenge.WwwAuthenticate;

        var problemDetails = new ProblemDetails
        {
            Type = $"https://verentis.io/problems/{challenge.Code}",
            Title = "Authentication failed.",
            Status = challenge.StatusCode
        };
        problemDetails.Extensions["code"] = challenge.Code;

        var result = new ObjectResult(problemDetails)
        {
            StatusCode = challenge.StatusCode
        };
        result.ContentTypes.Add("application/problem+json");
        context.Result = result;

        return Task.CompletedTask;
    }
}

public interface IAmbientCallerCredentialAccessor
{
    CallerCredential? Current { get; }

    IDisposable Push(CallerCredential credential);
}

internal sealed class AmbientCallerCredentialAccessor : IAmbientCallerCredentialAccessor
{
    private readonly AsyncLocal<CredentialScope?> _ambient = new();
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AmbientCallerCredentialAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CallerCredential? Current
    {
        get
        {
            var headers = _httpContextAccessor.HttpContext?.Request.Headers;
            if (headers is not null && headers.ContainsKey("Authorization"))
            {
                return AuthorizationHeaderParser.Parse(headers).Credential;
            }

            return _ambient.Value?.Credential;
        }
    }

    public IDisposable Push(CallerCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        var scope = new CredentialScope(this, credential, _ambient.Value);
        _ambient.Value = scope;
        return scope;
    }

    private sealed class CredentialScope : IDisposable
    {
        private readonly AmbientCallerCredentialAccessor _owner;
        private readonly CredentialScope? _previous;
        private bool _disposed;

        public CredentialScope(
            AmbientCallerCredentialAccessor owner,
            CallerCredential credential,
            CredentialScope? previous)
        {
            _owner = owner;
            Credential = credential;
            _previous = previous;
        }

        public CallerCredential Credential { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (!ReferenceEquals(_owner._ambient.Value, this))
            {
                throw new InvalidOperationException(
                    "Ambient caller credential scopes must be disposed in reverse order.");
            }

            _disposed = true;
            _owner._ambient.Value = _previous;
        }
    }
}

internal static class SecurityConfigurationKeys
{
    public const string RsaPublicKey = "Security-Bearer:RsaPublicKey";
    public const string RsaPublicKeySecondary =
        "Security-Bearer:RsaPublicKeySecondary";
    public const string Issuer = "Security-Bearer:Issuer";
}

internal sealed class VerentisJwtValidationOptions
{
    public string PrimaryPublicKey { get; set; } = string.Empty;

    public string? SecondaryPublicKey { get; set; }

    public string? Issuer { get; set; }
}

internal static class RsaPublicKeyLoader
{
    public static SecurityKey Load(string value, string configurationKey)
    {
        try
        {
            return new RsaSecurityKey(Import(value));
        }
        catch (ArgumentException exception)
        {
            throw InvalidConfiguration(configurationKey, exception);
        }
        catch (FormatException exception)
        {
            throw InvalidConfiguration(configurationKey, exception);
        }
        catch (CryptographicException exception)
        {
            throw InvalidConfiguration(configurationKey, exception);
        }
    }

    public static bool IsValid(string value)
    {
        try
        {
            using var rsa = Import(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static RSA Import(string value)
    {
        if (value.Contains("PRIVATE KEY", StringComparison.Ordinal))
        {
            throw new CryptographicException(
                "Private signing material is not accepted.");
        }

        var rsa = RSA.Create();
        try
        {
            if (value.Contains("BEGIN", StringComparison.Ordinal))
            {
                rsa.ImportFromPem(value);
                return rsa;
            }

            var keyBytes = Convert.FromBase64String(value);
            try
            {
                rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
                return rsa;
            }
            catch (CryptographicException)
            {
                rsa.Dispose();
                rsa = RSA.Create();
                rsa.ImportRSAPublicKey(keyBytes, out _);
                return rsa;
            }
        }
        catch (ArgumentException)
        {
            rsa.Dispose();
            throw;
        }
        catch (FormatException)
        {
            rsa.Dispose();
            throw;
        }
        catch (CryptographicException)
        {
            rsa.Dispose();
            throw;
        }
    }

    private static InvalidOperationException InvalidConfiguration(
        string configurationKey,
        Exception innerException)
    {
        return new InvalidOperationException(
            $"Configuration key '{configurationKey}' must contain an RSA public key.",
            innerException);
    }
}

internal sealed class VerentisJwtBearerPostConfigureOptions :
    IPostConfigureOptions<JwtBearerOptions>
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(60);
    private readonly IOptions<VerentisJwtValidationOptions> _validationOptions;

    public VerentisJwtBearerPostConfigureOptions(
        IOptions<VerentisJwtValidationOptions> validationOptions)
    {
        _validationOptions = validationOptions;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(
                name,
                JwtBearerDefaults.AuthenticationScheme,
                StringComparison.Ordinal))
        {
            return;
        }

        var validationOptions = _validationOptions.Value;
        var signingKeys = new List<SecurityKey>
        {
            RsaPublicKeyLoader.Load(
                validationOptions.PrimaryPublicKey,
                SecurityConfigurationKeys.RsaPublicKey)
        };

        if (!string.IsNullOrWhiteSpace(validationOptions.SecondaryPublicKey))
        {
            signingKeys.Add(RsaPublicKeyLoader.Load(
                validationOptions.SecondaryPublicKey,
                SecurityConfigurationKeys.RsaPublicKeySecondary));
        }

        options.Authority = null;
        options.Audience = null;
        options.MetadataAddress = null;
        options.Configuration = null;
        options.ConfigurationManager = null;
        options.IncludeErrorDetails = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = ClockSkew,
            IssuerSigningKeys = signingKeys,
            NameClaimType = PrincipalClaimTypes.Subject,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            RoleClaimType = "role",
            ValidateAudience = false,
            ValidateIssuer = !string.IsNullOrWhiteSpace(
                validationOptions.Issuer),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = validationOptions.Issuer
        };

        options.Events ??= new JwtBearerEvents();
        options.Events.OnChallenge = WriteChallengeAsync;
    }

    private static async Task WriteChallengeAsync(
        JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        const int statusCode = StatusCodes.Status401Unauthorized;
        var code = IsExpired(context.AuthenticateFailure)
            ? "expired_credential"
            : "invalid_credential";

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.WWWAuthenticate = "Bearer";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new
            {
                type = $"https://verentis.io/problems/{code}",
                title = "Authentication failed.",
                status = statusCode,
                code
            },
            cancellationToken: context.HttpContext.RequestAborted);
    }

    private static bool IsExpired(Exception? exception)
    {
        if (exception is SecurityTokenExpiredException)
        {
            return true;
        }

        if (exception is AggregateException aggregateException)
        {
            return aggregateException.InnerExceptions.Any(IsExpired);
        }

        return exception?.InnerException is not null &&
               IsExpired(exception.InnerException);
    }
}

public static partial class SecurityContractRegistration
{
    public static IServiceCollection AddAryzacSecurity(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.TryAddSingleton<
            IAmbientCallerCredentialAccessor,
            AmbientCallerCredentialAccessor>();
        services.TryAddSingleton<IPrincipalResolver, PrincipalResolver>();
        services.AddOptions<VerentisJwtValidationOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.PrimaryPublicKey =
                    configuration[SecurityConfigurationKeys.RsaPublicKey] ??
                    string.Empty;
                options.SecondaryPublicKey =
                    configuration[
                        SecurityConfigurationKeys.RsaPublicKeySecondary];
                options.Issuer =
                    configuration[SecurityConfigurationKeys.Issuer];
            })
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.PrimaryPublicKey),
                $"Missing required configuration key " +
                $"'{SecurityConfigurationKeys.RsaPublicKey}'.")
            .Validate(
                options => string.IsNullOrWhiteSpace(
                               options.PrimaryPublicKey) ||
                           RsaPublicKeyLoader.IsValid(
                               options.PrimaryPublicKey),
                $"Configuration key " +
                $"'{SecurityConfigurationKeys.RsaPublicKey}' must contain " +
                "an RSA public key.")
            .Validate(
                options => string.IsNullOrWhiteSpace(
                               options.SecondaryPublicKey) ||
                           RsaPublicKeyLoader.IsValid(
                               options.SecondaryPublicKey),
                $"Configuration key " +
                $"'{SecurityConfigurationKeys.RsaPublicKeySecondary}' must " +
                "contain an RSA public key.")
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IPostConfigureOptions<JwtBearerOptions>,
            VerentisJwtBearerPostConfigureOptions>());
        services.Configure<MvcOptions>(options =>
            options.Filters.Insert(
                0,
                new CallerCredentialAuthorizationFilter()));
{{configureSecurityService}}        return services;
    }{{securityServiceExtensionPoint}}
}
""";
    }
}
