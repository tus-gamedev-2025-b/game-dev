using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    ///     Manages all wormhole gates in the battlefield.
    ///     Connects gates at the four edges of the battlefield.
    /// </summary>
    public class WormholeManager : MonoBehaviour
    {
        [Header("Gate References")]
        [Tooltip("Gate at the top edge of the battlefield")]
        public WormholeGate m_TopGate;

        [Tooltip("Gate at the bottom edge of the battlefield")]
        public WormholeGate m_BottomGate;

        [Tooltip("Gate at the left edge of the battlefield")]
        public WormholeGate m_LeftGate;

        [Tooltip("Gate at the right edge of the battlefield")]
        public WormholeGate m_RightGate;

        private void Start()
        {
            ConnectGates();
        }

        /// <summary>
        ///     Connects the gates: Top↔Bottom, Left↔Right
        /// </summary>
        private void ConnectGates()
        {
            // Connect top and bottom gates
            if (m_TopGate != null && m_BottomGate != null)
            {
                m_TopGate.m_ConnectedGate = m_BottomGate;
                m_BottomGate.m_ConnectedGate = m_TopGate;
            }
            else
            {
                Debug.LogWarning("Cannot connect Top and Bottom gates - one or both are missing");
            }

            // Connect left and right gates
            if (m_LeftGate != null && m_RightGate != null)
            {
                m_LeftGate.m_ConnectedGate = m_RightGate;
                m_RightGate.m_ConnectedGate = m_LeftGate;
            }
            else
            {
                Debug.LogWarning("Cannot connect Left and Right gates - one or both are missing");
            }
        }
    }
}
