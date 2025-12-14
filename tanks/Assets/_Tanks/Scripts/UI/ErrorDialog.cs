using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks.Complete.UI
{
    /// <summary>
    ///     Lightweight error dialog. Shows a message and closes when the button is pressed.
    /// </summary>
    public class ErrorDialog : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button closeButton;

        private Action _onClose;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Show(string message, Action onClose = null)
        {
            _onClose = onClose;

            if (messageText != null)
            {
                messageText.text = message;
            }

            Toggle(true);
        }

        public void Close()
        {
            Toggle(false);
            _onClose?.Invoke();
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
