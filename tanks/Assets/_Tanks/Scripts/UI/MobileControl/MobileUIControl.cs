using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;

namespace Tanks.Complete
{
    // Handle on screen UI Control used for mobile platform. By default, will auto-disable itself when not on mobile
    // Default execution is low to ensure it disable itself before any other script so other scripts can use its enabled
    // status as a test to know if on a mobile platform or not.
    [DefaultExecutionOrder(-90)]
    public class MobileUIControl : MonoBehaviour
    {

        [Tooltip("If true (the default) the GameObject on which this is will get disabled when not on a mobile platform")]
        public bool AutoDisableOnNonMobilePlatform = true;

        private OnScreenControl m_Control;
        public static MobileUIControl Instance { get; private set; }

        public InputDevice Device => m_Control.control.device;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Awake()
        {
            Instance = this;

            m_Control = GetComponentInChildren<OnScreenControl>();
            if (AutoDisableOnNonMobilePlatform && !Application.isMobilePlatform)
            {
                gameObject.SetActive(false);
            }
        }

        public void Show()
        {
            // On non mobile platform with the auto disable, we cannot show it
            if (AutoDisableOnNonMobilePlatform && !Application.isMobilePlatform)
            {
                return;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (AutoDisableOnNonMobilePlatform && !Application.isMobilePlatform)
            {
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
