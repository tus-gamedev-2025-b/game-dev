using TMPro;
using UnityEngine;

namespace Tanks.Complete.UI
{
    /// <summary>
    ///     Simple loading overlay that blocks input while an async operation runs.
    /// </summary>
    public class LoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI messageText;

        public void Show(string message = "Loading...")
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

            Toggle(true);
        }

        public void Hide()
        {
            Toggle(false);
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
