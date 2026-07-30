using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Google.Apis.Auth.OAuth2;

namespace soteltestgcp;

/// <summary>
/// Provides Google ADC access tokens for OTLP exporters.
/// Handles per-scope token caching, automatic refresh, and HttpClient reuse.
/// </summary>
public class GoogleCloudBearerTokenProvider
{
    // Cache scoped credentials per-scope, since CreateScoped returns a new instance.
    private readonly ConcurrentDictionary<string, GoogleCredential> _scopedCredentials = new();
    private readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> _tokenCache = new();
    private readonly SemaphoreSlim _credentialLock = new(1, 1);
    private GoogleCredential? _baseCredential;
    private readonly Lazy<HttpClient> _sharedHttpClient = new(() => new HttpClient());

    public async Task<string?> GetBearerTokenAsync(string scope = "https://www.googleapis.com/auth/cloud-platform")
    {
        if (_tokenCache.TryGetValue(scope, out var cached) && DateTime.UtcNow.AddSeconds(60) < cached.Expiry)
        {
            return cached.Token;
        }

        try
        {
            var scopedCredential = await GetScopedCredentialAsync(scope).ConfigureAwait(false);
            if (scopedCredential is null) return null;

            // Cast to ITokenAccess — GetAccessTokenForRequestAsync is an explicit interface
            // implementation on GoogleCredential, not directly callable on the concrete type.
            var token = await ((ITokenAccess)scopedCredential).GetAccessTokenForRequestAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(token))
            {
                _tokenCache[scope] = (token, DateTime.UtcNow.AddMinutes(55));
            }
            Console.WriteLine($"[GoogleCloudBearerTokenProvider] scope='{scope}' credentialType='{scopedCredential.UnderlyingCredential?.GetType().Name}' tokenPrefix='{token?.Substring(0, Math.Min(12, token?.Length ?? 0))}' tokenLen={token?.Length}");
            return token;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GoogleCloudBearerTokenProvider] Failed to get Google access token for scope '{scope}': {ex}");
            return null;
        }
    }

    private async Task<GoogleCredential?> GetScopedCredentialAsync(string scope)
    {
        if (_scopedCredentials.TryGetValue(scope, out var existing)) return existing;

        var baseCredential = await GetBaseCredentialAsync().ConfigureAwait(false);
        if (baseCredential is null) return null;

        // ServiceAccountCredential defaults UseJwtAccessWithScopes to true, which makes
        // GetAccessTokenForRequestAsync return a locally-signed JWT instead of a real
        // OAuth2 access token. Google's Telemetry API rejects that token type with
        // ACCESS_TOKEN_TYPE_UNSUPPORTED, so disable it explicitly BEFORE calling
        // CreateScoped — doing it after loses the scopes set by CreateScoped.
        if (baseCredential.UnderlyingCredential is ServiceAccountCredential serviceAccountCredential)
        {
            baseCredential = GoogleCredential.FromServiceAccountCredential(
                serviceAccountCredential.WithUseJwtAccessWithScopes(false));
        }

        var scoped = baseCredential.IsCreateScopedRequired
            ? baseCredential.CreateScoped(scope)
            : baseCredential; // ComputeCredential/UserCredential already carry their scopes

        _scopedCredentials[scope] = scoped;
        return scoped;
    }

    private async Task<GoogleCredential?> GetBaseCredentialAsync()
    {
        if (_baseCredential is not null) return _baseCredential;

        await _credentialLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_baseCredential is not null) return _baseCredential;
            _baseCredential = await GoogleCredential.GetApplicationDefaultAsync().ConfigureAwait(false);
            return _baseCredential;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GoogleCloudBearerTokenProvider] Failed to load application default credentials: {ex}");
            return null;
        }
        finally
        {
            _credentialLock.Release();
        }
    }

    public Func<HttpClient> CreateHttpClientFactory(string scope = "https://www.googleapis.com/auth/cloud-platform")
    {
        return () =>
        {
            var handler = new BearerTokenHandler(this, scope) { InnerHandler = new HttpClientHandler() };
            return new HttpClient(handler);
        };
    }

    private sealed class BearerTokenHandler : DelegatingHandler
    {
        private readonly GoogleCloudBearerTokenProvider _provider;
        private readonly string _scope;

        public BearerTokenHandler(GoogleCloudBearerTokenProvider provider, string scope)
        {
            _provider = provider;
            _scope = scope;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _provider.GetBearerTokenAsync(_scope).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

#if NET5_0_OR_GREATER
        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = _provider.GetBearerTokenAsync(_scope).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return base.Send(request, cancellationToken);
            
        }
#endif

    }
}