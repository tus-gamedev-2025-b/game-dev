using Tanks.Complete.Persistence.Models;
using TMPro;
using UnityEngine;

namespace Tanks.Complete.UI
{
    /// <summary>
    ///     Displays the current user's ID and username and updates when the session changes.
    /// </summary>
    public class UserInfoPresenter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI userIdText;
        [SerializeField] private TextMeshProUGUI userNameText;
        [SerializeField] private string userIdFormat = "User ID: {0}";
        [SerializeField] private string userNameFormat = "User Name: {0}";
        [SerializeField] private string userIdPlaceholder = "-";
        [SerializeField] private string userNameFallback = "NoName";

        private bool _isSubscribed;
        private UserDataManager _manager;

        private void OnEnable()
        {
            Subscribe();
            Refresh(_manager?.CurrentUserData);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Refresh(UserData data)
        {
            var hasValid = data != null && data.IsValid;
            var userIdValue = hasValid ? data.UserId.ToString() : userIdPlaceholder;
            var userNameValue = hasValid && !string.IsNullOrWhiteSpace(data.UserName)
                ? data.UserName
                : userNameFallback;

            if (userIdText != null)
            {
                userIdText.SetText(string.Format(userIdFormat, userIdValue));
            }

            if (userNameText != null)
            {
                userNameText.SetText(string.Format(userNameFormat, userNameValue));
            }
        }

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            _manager = UserDataManager.Instance ?? FindFirstObjectByType<UserDataManager>();
            if (_manager == null)
            {
                return;
            }

            _manager.OnUserDataChanged += HandleUserDataChanged;
            _manager.OnSessionCleared += HandleSessionCleared;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _manager == null)
            {
                return;
            }

            _manager.OnUserDataChanged -= HandleUserDataChanged;
            _manager.OnSessionCleared -= HandleSessionCleared;
            _isSubscribed = false;
        }

        private void HandleUserDataChanged(UserData data)
        {
            Refresh(data);
        }

        private void HandleSessionCleared()
        {
            Refresh(null);
        }
    }
}
