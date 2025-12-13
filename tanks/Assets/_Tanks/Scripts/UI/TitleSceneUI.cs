using System;
using System.Threading;
using System.Threading.Tasks;
using Tanks.ApiClient.Client;
using Tanks.ApiClient.Model;
using Tanks.Complete.Persistence.Models;
using Tanks.Complete.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Tanks.Complete.Persistence;
using Tanks.Complete.Utils;

namespace Tanks.Complete
{
    /// <summary>
    /// Handles title screen login flow.
    /// </summary>
    public class TitleSceneUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI userIdText;
        [SerializeField] private LoadingOverlay loadingOverlay;
        [SerializeField] private ErrorDialog errorDialog;

        [Header("Config")]
        [SerializeField] private string defaultDisplayNamePrefix = "Tanker";
        [SerializeField] private bool autoStartOnAwake = false;

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

        private void OnStartButtonPressed()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ = RunLoginFlowAsync(_cts.Token);
        }

        private async Task RunLoginFlowAsync(CancellationToken token)
        {
            var manager = UserDataManager.Instance;
            if (manager == null)
            {
                Debug.LogError("UserDataManager is missing in the scene.");
                return;
            }

            SetInteractable(false);
            loadingOverlay?.Show("Connecting to server...");
            statusText?.SetText("Starting authentication...");

            try
            {
                AuthResponse result;
                if (manager.HasSession)
                {
                    statusText?.SetText("Signing in with saved session...");
                    result = await manager.LoginWithStoredRefreshTokenAsync(token);
                }
                else
                {
                    var displayName = $"{defaultDisplayNamePrefix}{UnityEngine.Random.Range(1000, 9999)}";
                    statusText?.SetText("Creating a new user...");
                    result = await manager.CreateAndLoginUserAsync(displayName, token);
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
                HandleError($"API error: {apiEx.Message}");
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

            var existing = FindObjectOfType<UserDataManager>();
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

            if (user != null && user.IsValid)
            {
                userIdText.text = $"User ID: {user.UserId}";
            }
            else
            {
                userIdText.text = "User ID: -";
            }
        }
    }
}
