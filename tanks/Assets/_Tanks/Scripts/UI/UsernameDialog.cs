using System;
using Tanks.Complete.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete.UI
{
    /// <summary>
    ///     Dialog for updating the player's username. Actual validation and API calls are handled by the host.
    /// </summary>
    public class UsernameDialog : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TextMeshProUGUI warningText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        public event Action<string> OnSubmit;
        public event Action OnCancel;

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleSubmit);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancel);
            }
        }

        public void Open(string currentName)
        {
            Toggle(true);
            SetLoading(false);
            SetWarning(string.Empty);

            if (usernameInput == null)
            {
                return;
            }

            usernameInput.text = string.IsNullOrWhiteSpace(currentName) ? string.Empty : currentName;
            usernameInput.interactable = true;
            usernameInput.Select();
            usernameInput.ActivateInputField();
        }

        public void Close()
        {
            Toggle(false);
        }

        public void SetWarning(string message)
        {
            if (warningText == null)
            {
                return;
            }

            warningText.text = message ?? string.Empty;
            warningText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        public void SetLoading(bool isLoading)
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = !isLoading;
            }

            if (cancelButton != null)
            {
                cancelButton.interactable = !isLoading;
            }

            if (usernameInput != null)
            {
                usernameInput.interactable = !isLoading;
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = !isLoading;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void HandleSubmit()
        {
            var value = UsernameValidator.Normalize(usernameInput != null ? usernameInput.text : string.Empty);
            OnSubmit?.Invoke(value);
        }

        private void HandleCancel()
        {
            OnCancel?.Invoke();
            Close();
        }

        private void Toggle(bool visible)
        {
            gameObject.SetActive(visible);

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
}
