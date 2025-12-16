using System;
using System.Threading;
using System.Threading.Tasks;
using Tanks.ApiClient.Client;
using Tanks.Complete.Persistence.Models;
using Tanks.Complete.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tanks.Complete
{
    /// <summary>
    ///     Handles title screen login flow.
    /// </summary>
    public class TitleSceneUI : MonoBehaviour
    {
        private const string DefaultDisplayName = "NoName";
        [Header("UI References")]
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI userIdText;
        [SerializeField] private LoadingOverlay loadingOverlay;
        [SerializeField] private ErrorDialog errorDialog;

        [Header("Config")]
        [SerializeField] private bool autoStartOnAwake;

        private CancellationTokenSource _cts;

        private void Awake()
        {
            startButton?.onClick.AddListener(OnStartButtonPressed);
            statusText?.SetText("Tap to start");
            userIdText?.SetText("User ID: -");

            EnsureUserDataManagerExists();

            if (autoStartOnAwake)
            {
                OnStartButtonPressed();
            }

            UpdateUserIdLabel(UserDataManager.Instance?.CurrentUserData);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async void OnStartButtonPressed()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try
            {
                await RunLoginFlowAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                HandleError($"Sign-in failed: {ex.Message}");
            }
        }

        private async Task RunLoginFlowAsync(CancellationToken token)
        {
            var manager = UserDataManager.Instance;
            if (manager == null)
            {
                Debug.LogError("UserDataManager is missing in the scene.");
                return;
            }

            try
            {
                SetInteractable(false);
                loadingOverlay?.Show("Connecting to server...");
                statusText?.SetText("Starting authentication...");

                if (manager.HasSession)
                {
                    statusText?.SetText("Signing in with saved session...");
                    await manager.LoginWithStoredRefreshTokenAsync(token);
                }
                else
                {
                    var displayName = DefaultDisplayName;
                    statusText?.SetText("Creating a new user...");
                    await manager.CreateAndLoginUserAsync(displayName, token);
                }

                UpdateUserIdLabel(manager.CurrentUserData);
                statusText?.SetText("Signed in");

                await manager.EnsureAccessTokenAsync(token);
                SceneManager.LoadScene(SceneNames.m_HomeScene);
            }
            catch (OperationCanceledException)
            {
                statusText?.SetText("Canceled");
            }
            catch (ApiException apiEx)
            {
                var details = $"API error ({apiEx.ErrorCode}): {apiEx.Message}";
                var errorContent = apiEx.ErrorContent?.ToString();
                if (!string.IsNullOrEmpty(errorContent))
                {
                    details += $" | Content: {errorContent}";
                }

                HandleError(details);
            }
            catch (Exception ex)
            {
                HandleError($"Sign-in failed: {ex.Message}");
            }
            finally
            {
                loadingOverlay?.Hide();
                SetInteractable(true);
            }
        }

        private void HandleError(string message)
        {
            Debug.LogWarning(message);
            errorDialog?.Show(message, () => statusText?.SetText("Tap to retry"));
        }

        private void SetInteractable(bool enabled)
        {
            if (startButton != null)
            {
                startButton.interactable = enabled;
            }
        }

        private static void EnsureUserDataManagerExists()
        {
            if (UserDataManager.Instance != null)
            {
                return;
            }

            var existing = FindFirstObjectByType<UserDataManager>();
            if (existing != null)
            {
                return;
            }

            var go = new GameObject(nameof(UserDataManager));
            go.AddComponent<UserDataManager>();
        }

        private void UpdateUserIdLabel(UserData user)
        {
            if (userIdText == null)
            {
                return;
            }

            userIdText.text = user != null && user.IsValid
                ? $"User ID: {user.UserId}"
                : "User ID: -";
        }
    }
}
