using System;
using System.Threading;
using System.Threading.Tasks;
using Tanks.ApiClient.Client;
using Tanks.Complete.Persistence.Models;
using Tanks.Complete.UI;
using Tanks.Complete.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete
{
    /// <summary>
    ///     Handles Home scene UI: displays user info and drives the username update dialog.
    /// </summary>
    public class HomeSceneUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button userRegistrationButton;
        [SerializeField] private UsernameDialog usernameDialog;
        [SerializeField] private LoadingOverlay loadingOverlay;
        [SerializeField] private ErrorDialog errorDialog;
        [SerializeField] private UserInfoPresenter userInfoPresenter;

        private CancellationTokenSource _cts;
        private UserDataManager _manager;

        private void Awake()
        {
            _manager = UserDataManager.Instance ?? FindFirstObjectByType<UserDataManager>();
            if (_manager == null)
            {
                Debug.LogError("UserDataManager is missing in the scene.");
                return;
            }

            _manager.OnUserDataChanged += HandleUserDataChanged;
            _manager.OnSessionCleared += HandleSessionCleared;

            if (userRegistrationButton != null)
            {
                userRegistrationButton.onClick.AddListener(OpenUsernameDialog);
            }

            if (usernameDialog != null)
            {
                usernameDialog.OnSubmit += HandleUsernameSubmit;
                usernameDialog.OnCancel += HandleDialogCancelled;
            }
        }

        private void Start()
        {
            UpdateUserLabels(_manager.CurrentUserData);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (_manager != null)
            {
                _manager.OnUserDataChanged -= HandleUserDataChanged;
                _manager.OnSessionCleared -= HandleSessionCleared;
            }

            if (usernameDialog != null)
            {
                usernameDialog.OnSubmit -= HandleUsernameSubmit;
                usernameDialog.OnCancel -= HandleDialogCancelled;
            }
        }

        private void OpenUsernameDialog()
        {
            var currentName = _manager?.CurrentUserData?.UserName;
            usernameDialog?.Open(string.IsNullOrWhiteSpace(currentName) ? "NoName" : currentName);
        }

        private void HandleUsernameSubmit(string proposedName)
        {
            if (usernameDialog == null || _manager == null)
            {
                return;
            }

            if (!UsernameValidator.TryValidate(proposedName, out var error))
            {
                usernameDialog.SetWarning(error);
                return;
            }

            usernameDialog.SetWarning(string.Empty);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ = UpdateUserNameAsync(proposedName, _cts.Token);
        }

        private void HandleDialogCancelled()
        {
            usernameDialog?.SetWarning(string.Empty);
        }

        private async Task UpdateUserNameAsync(string newName, CancellationToken token)
        {
            try
            {
                loadingOverlay?.Show("Updating username...");
                usernameDialog?.SetLoading(true);
                await _manager.UpdateUserNameAsync(newName, token);
                usernameDialog?.Close();
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (ApiException apiEx)
            {
                HandleError($"Username update failed ({apiEx.ErrorCode}): {apiEx.Message}");
                usernameDialog?.SetWarning("Failed to update username. Please try again later.");
            }
            catch (Exception ex)
            {
                HandleError($"Username update failed: {ex.Message}");
                usernameDialog?.SetWarning("Failed to update username. Please try again later.");
            }
            finally
            {
                usernameDialog?.SetLoading(false);
                loadingOverlay?.Hide();
            }
        }

        private void HandleUserDataChanged(UserData data)
        {
            UpdateUserLabels(data);
        }

        private void HandleSessionCleared()
        {
            UpdateUserLabels(null);
        }

        private void UpdateUserLabels(UserData data)
        {
            userInfoPresenter?.Refresh(data);
        }

        private void HandleError(string message)
        {
            Debug.LogWarning(message);
            errorDialog?.Show(message);
        }
    }
}
