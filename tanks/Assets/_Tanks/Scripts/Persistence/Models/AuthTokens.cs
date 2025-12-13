using System;
using System.Globalization;
using Tanks.ApiClient.Model;
using UnityEngine;

namespace Tanks.Complete.Persistence.Models
{
    [Serializable]
    public class AuthTokens
    {
        private const int DefaultExpiryMarginMinutes = 5;

        public string AccessToken;
        public string RefreshToken;
        public long AccessTokenExpiresAtUnixSeconds;
        public long RefreshTokenExpiresAtUnixSeconds;

        public DateTimeOffset AccessTokenExpiresAt => DateTimeOffset.FromUnixTimeSeconds(AccessTokenExpiresAtUnixSeconds);

        public DateTimeOffset RefreshTokenExpiresAt => DateTimeOffset.FromUnixTimeSeconds(RefreshTokenExpiresAtUnixSeconds);

        public bool IsAccessTokenValid(TimeSpan? margin = null)
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                return false;
            }

            var buffer = margin ?? TimeSpan.FromMinutes(DefaultExpiryMarginMinutes);
            return DateTimeOffset.UtcNow + buffer < AccessTokenExpiresAt;
        }

        public bool IsRefreshTokenValid(TimeSpan? margin = null)
        {
            if (string.IsNullOrEmpty(RefreshToken))
            {
                return false;
            }

            var buffer = margin ?? TimeSpan.FromMinutes(DefaultExpiryMarginMinutes);
            return DateTimeOffset.UtcNow + buffer < RefreshTokenExpiresAt;
        }

        public static AuthTokens FromAuthResponse(AuthResponse response)
        {
            if (response == null)
            {
                return null;
            }

            return new AuthTokens
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                AccessTokenExpiresAtUnixSeconds = ParseIso8601ToUnixSeconds(response.AccessTokenExpiresAt),
                RefreshTokenExpiresAtUnixSeconds = ParseIso8601ToUnixSeconds(response.RefreshTokenExpiresAt)
            };
        }

        public static AuthTokens FromTokenResponse(TokenResponse response, string currentRefreshToken, DateTimeOffset currentRefreshExpiry)
        {
            if (response == null)
            {
                return null;
            }

            var refreshToken = string.IsNullOrEmpty(response.RefreshToken) ? currentRefreshToken : response.RefreshToken;
            var refreshExpiry = string.IsNullOrEmpty(response.RefreshTokenExpiresAt)
                ? currentRefreshExpiry
                : DateTimeOffset.FromUnixTimeSeconds(ParseIso8601ToUnixSeconds(response.RefreshTokenExpiresAt));

            if (refreshExpiry == default)
            {
                refreshExpiry = DateTimeOffset.UtcNow;
            }

            return new AuthTokens
            {
                AccessToken = response.AccessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAtUnixSeconds = ParseIso8601ToUnixSeconds(response.AccessTokenExpiresAt),
                RefreshTokenExpiresAtUnixSeconds = refreshExpiry.ToUnixTimeSeconds()
            };
        }

        public string ToJson() => JsonUtility.ToJson(this);

        public static AuthTokens FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<AuthTokens>(json);
        }

        private static long ParseIso8601ToUnixSeconds(string iso8601)
        {
            if (DateTimeOffset.TryParse(iso8601, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
            {
                return result.ToUnixTimeSeconds();
            }

            Debug.LogWarning($"Could not parse date string '{iso8601}', falling back to current time.");
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
