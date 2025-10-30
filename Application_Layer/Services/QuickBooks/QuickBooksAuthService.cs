using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace Application_Layer.Services
{
    /// <summary>
    /// Handles QuickBooks OAuth 2.0 flow:
    /// - Exchanges authorization code for access & refresh tokens
    /// - Refreshes expired access tokens
    /// - Stores tokens in database via repository
    /// </summary>
    public class QuickBooksAuthService : IQuickBooksAuthService
    {
        private readonly IConfiguration _config;
        private readonly IQuickBooksTokenRepository _repo;
        private readonly ILogger<QuickBooksAuthService> _logger;

        private const string TokenEndpoint = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";

        public QuickBooksAuthService(
            IConfiguration config,
            IQuickBooksTokenRepository repo,
            ILogger<QuickBooksAuthService> logger)
        {
            _config = config;
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// Step 2: Called automatically from controller when QuickBooks redirects back with ?code=...&realmId=...
        /// Exchanges the authorization code for access/refresh tokens.
        /// </summary>
        public async Task HandleAuthCallbackAsync(string code, string realmId)
        {
            var clientId = _config["QuickBooks:ClientId"];
            var clientSecret = _config["QuickBooks:ClientSecret"];
            var redirectUri = _config["QuickBooks:RedirectUri"];

            var client = new RestClient(TokenEndpoint);
            var request = new RestRequest("", Method.Post);

            // Basic auth header (clientId:clientSecret)
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.AddHeader("Authorization", $"Basic {basic}");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            // OAuth2 "authorization_code" exchange
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", code);
            request.AddParameter("redirect_uri", redirectUri);

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"QuickBooks token exchange failed: {(int)response.StatusCode} {response.Content}");

            // ✅ Parse token JSON response
            using var doc = JsonDocument.Parse(response.Content!);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString();
            var refreshToken = root.GetProperty("refresh_token").GetString();
            var expiresIn = root.GetProperty("expires_in").GetInt32();
            var xRefreshTokenExpiresIn = root.GetProperty("x_refresh_token_expires_in").GetInt32();

            var token = new QuickBooksTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60), // expire slightly early
                RefreshTokenExpiresAt = DateTime.UtcNow.AddSeconds(xRefreshTokenExpiresIn - 3600),
                RealmId = realmId,
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.AddOrUpdateAsync(token);
            _logger.LogInformation("✅ QuickBooks tokens stored successfully. RealmId={RealmId}", realmId);
        }

        /// <summary>
        /// Returns a valid QuickBooks access token (refreshing if necessary).
        /// Used by QuickBooksApiManager when calling the API.
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            await EnsureFreshTokenAsync();
            var latest = await _repo.GetLatestAsync();
            if (latest == null || string.IsNullOrWhiteSpace(latest.AccessToken))
                throw new Exception("QuickBooks access token not available. Run OAuth authorization first.");
            return latest.AccessToken;
        }

        /// <summary>
        /// Checks token expiration and refreshes automatically if needed.
        /// </summary>
        public async Task EnsureFreshTokenAsync()
        {
            var token = await _repo.GetLatestAsync();
            if (token == null)
                return;

            var now = DateTime.UtcNow;
            if (now < token.AccessTokenExpiresAt)
                return; // still valid

            if (now >= token.RefreshTokenExpiresAt)
                throw new Exception("QuickBooks refresh token expired. Re-authorize the app.");

            _logger.LogInformation("♻️ Refreshing QuickBooks access token...");

            var clientId = _config["QuickBooks:ClientId"];
            var clientSecret = _config["QuickBooks:ClientSecret"];

            var client = new RestClient(TokenEndpoint);
            var request = new RestRequest("", Method.Post);

            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.AddHeader("Authorization", $"Basic {basic}");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", token.RefreshToken);

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"QuickBooks token refresh failed: {(int)response.StatusCode} {response.Content}");

            // ✅ Parse response again
            using var doc = JsonDocument.Parse(response.Content!);
            var root = doc.RootElement;

            token.AccessToken = root.GetProperty("access_token").GetString();
            token.RefreshToken = root.GetProperty("refresh_token").GetString();
            var expiresIn = root.GetProperty("expires_in").GetInt32();
            var xRefreshTokenExpiresIn = root.GetProperty("x_refresh_token_expires_in").GetInt32();

            token.AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60);
            token.RefreshTokenExpiresAt = DateTime.UtcNow.AddSeconds(xRefreshTokenExpiresIn - 3600);
            token.UpdatedAt = DateTime.UtcNow;

            await _repo.AddOrUpdateAsync(token);
            _logger.LogInformation("✅ QuickBooks access token refreshed successfully.");
        }
    }
}
