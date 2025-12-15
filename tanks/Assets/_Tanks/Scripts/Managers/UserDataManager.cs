using System;
using System.Threading;
using System.Threading.Tasks;
using Tanks.ApiClient.Api;
using Tanks.ApiClient.Client;
using Tanks.ApiClient.Model;
using Tanks.Complete.Persistence;
using Tanks.Complete.Persistence.Models;
using Tanks.Complete.Utils;
using UnityEngine;

namespace Tanks.Complete
{
    public class UserDataManager : MonoBehaviour
    {
        private const string InvalidRefreshTokenMessage = "Stored refresh token is invalid.";

        private string _basePath;

        private Configuration _configuration;
        public static UserDataManager Instance { get; private set; }

        public UserData CurrentUserData { get; private set; }
        public AuthTokens CurrentTokens { get; private set; }

        public bool HasSession => CurrentUserData != null && CurrentUserData.IsValid && CurrentTokens != null && CurrentTokens.IsRefreshTokenValid();
        public bool HasValidAccessToken => CurrentTokens != null && CurrentTokens.IsAccessTokenValid();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _basePath = ApiConfig.ResolveBasePath();
            RestoreSessionFromStorage();
        }

        public event Action<UserData> OnUserDataChanged;
        public event Action OnSessionCleared;

        public async Task<AuthResponse> CreateAndLoginUserAsync(string displayName, CancellationToken cancellationToken = default)
        {
            var api = new UsersApi(ApiConfig.CreateConfiguration(null, _basePath));
            var response = await api.UsersPostAsync(new CreateUserRequest(displayName), cancellationToken);
            ApplyAuthResponse(response);
            return response;
        }

        public async Task<AuthResponse> LoginWithStoredRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!HasSession)
            {
                throw new InvalidOperationException("No persisted session available.");
            }

            Guid refreshGuid;
            try
            {
                refreshGuid = GetValidatedRefreshGuid();
            }
            catch (InvalidRefreshTokenException)
            {
                ClearSession();
                throw;
            }

            var api = new UsersApi(ApiConfig.CreateConfiguration(null, _basePath));
            var loginRequest = new LoginRequest(CurrentUserData.UserId, refreshGuid);
            var response = await api.UsersLoginPostAsync(loginRequest, cancellationToken);
            ApplyAuthResponse(response);
            return response;
        }

        public async Task<TokenResponse> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentTokens == null)
            {
                throw new InvalidOperationException("No tokens available to refresh.");
            }

            Guid refreshGuid;
            try
            {
                refreshGuid = GetValidatedRefreshGuid();
            }
            catch (InvalidRefreshTokenException)
            {
                ClearSession();
                throw;
            }

            var api = new AuthApi(ApiConfig.CreateConfiguration(null, _basePath));
            var response = await api.AuthRefreshPostAsync(new RefreshTokenRequest(refreshGuid), cancellationToken);

            CurrentTokens = AuthTokens.FromTokenResponse(response, CurrentTokens.RefreshToken, CurrentTokens.RefreshTokenExpiresAt);
            EnsureConfiguration().AccessToken = CurrentTokens.AccessToken;
            PersistSession();
            return response;
        }

        public async Task<UserData> UpdateUserNameAsync(string newUserName, CancellationToken cancellationToken = default)
        {
            if (!HasSession)
            {
                throw new InvalidOperationException("No active session available.");
            }

            if (!UsernameValidator.TryValidate(newUserName, out var error))
            {
                throw new ArgumentException(error, nameof(newUserName));
            }

            var normalized = UsernameValidator.Normalize(newUserName);
            await EnsureAccessTokenAsync(cancellationToken);

            var api = new UsersApi(EnsureConfiguration());
            var response = await api.UsersIdNamePatchAsync(CurrentUserData.UserId.ToString(),
                new UpdateNameRequest(normalized), cancellationToken);

            var updated = UserData.FromAuthResponseUser(response?.User);
            if (updated == null)
            {
                throw new InvalidOperationException("Failed to parse updated user response.");
            }

            CurrentUserData = updated;
            PersistSession();
            OnUserDataChanged?.Invoke(CurrentUserData);
            return CurrentUserData;
        }

        public bool RestoreSessionFromStorage()
        {
            var userJson = EncryptedPrefs.GetString(StorageKeys.UserData, string.Empty);
            var tokenJson = EncryptedPrefs.GetString(StorageKeys.AuthTokens, string.Empty);

            var user = UserData.FromJson(userJson);
            var tokens = AuthTokens.FromJson(tokenJson);

            if (user == null || tokens == null || !user.IsValid || !tokens.IsRefreshTokenValid())
            {
                ClearSession(false);
                return false;
            }

            CurrentUserData = user;
            CurrentTokens = tokens;
            EnsureConfiguration().AccessToken = tokens.AccessToken;
            OnUserDataChanged?.Invoke(CurrentUserData);
            return true;
        }

        public async Task EnsureAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentTokens == null)
            {
                throw new InvalidOperationException("No auth tokens available.");
            }

            if (CurrentTokens.IsAccessTokenValid())
            {
                EnsureConfiguration().AccessToken = CurrentTokens.AccessToken;
                return;
            }

            await RefreshAccessTokenAsync(cancellationToken);
        }

        public void ClearSession(bool deletePersisted = true)
        {
            CurrentUserData = null;
            CurrentTokens = null;
            _configuration = null;

            if (deletePersisted)
            {
                EncryptedPrefs.DeleteKey(StorageKeys.UserData);
                EncryptedPrefs.DeleteKey(StorageKeys.AuthTokens);
                EncryptedPrefs.Save();
            }

            OnSessionCleared?.Invoke();
        }

        private void ApplyAuthResponse(AuthResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            try
            {
                CurrentUserData = UserData.FromAuthResponse(response) ??
                                  throw new InvalidOperationException("Failed to parse authentication response.");
                CurrentTokens = AuthTokens.FromAuthResponse(response) ??
                                throw new InvalidOperationException("Failed to parse authentication response.");
            }
            catch (InvalidOperationException)
            {
                ClearSession();
                throw;
            }

            EnsureConfiguration().AccessToken = CurrentTokens.AccessToken;
            PersistSession();
            OnUserDataChanged?.Invoke(CurrentUserData);
        }

        private Configuration EnsureConfiguration()
        {
            if (_configuration == null)
            {
                _configuration = ApiConfig.CreateConfiguration(CurrentTokens?.AccessToken, _basePath);
            }
            else if (CurrentTokens != null)
            {
                _configuration.AccessToken = CurrentTokens.AccessToken;
            }

            if (!string.IsNullOrWhiteSpace(_basePath))
            {
                _configuration.BasePath = _basePath;
            }

            return _configuration;
        }

        private void PersistSession()
        {
            if (CurrentUserData == null || CurrentTokens == null)
            {
                return;
            }

            EncryptedPrefs.SetString(StorageKeys.UserData, CurrentUserData.ToJson());
            EncryptedPrefs.SetString(StorageKeys.AuthTokens, CurrentTokens.ToJson());
            EncryptedPrefs.Save();
        }

        private Guid GetValidatedRefreshGuid()
        {
            if (!Guid.TryParse(CurrentTokens.RefreshToken, out var refreshGuid))
            {
                throw new InvalidRefreshTokenException(InvalidRefreshTokenMessage);
            }

            return refreshGuid;
        }

        private sealed class InvalidRefreshTokenException : InvalidOperationException
        {
            public InvalidRefreshTokenException(string message) : base(message)
            {
            }
        }
    }
}
