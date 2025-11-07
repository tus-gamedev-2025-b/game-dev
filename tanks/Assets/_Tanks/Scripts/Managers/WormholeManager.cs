using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    ///     Manages all wormhole gates in the battlefield.
    ///     Creates and connects gates at the four edges of the battlefield.
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

        [Header("Auto Setup")]
        [Tooltip("If true, automatically create gates at battlefield edges")]
        public bool m_AutoCreateGates;

        [Tooltip("The prefab to use for creating gates")]
        public GameObject m_GatePrefab;

        [Tooltip("The size of the battlefield (used for auto-placement)")]
        public Vector2 m_BattlefieldSize = new Vector2(100f, 100f);

        [Tooltip("Distance from edge to place gates")]
        public float m_EdgeOffset = 5f;

        private void Start()
        {
            if (m_AutoCreateGates && m_GatePrefab != null)
            {
                CreateGatesAutomatically();
            }

            ConnectGates();
        }

        private void OnDrawGizmos()
        {
            // Draw battlefield bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(m_BattlefieldSize.x, 0.1f, m_BattlefieldSize.y));
        }

        /// <summary>
        ///     Automatically creates gates at the four edges of the battlefield
        /// </summary>
        private void CreateGatesAutomatically()
        {
            var halfWidth = m_BattlefieldSize.x / 2f;
            var halfHeight = m_BattlefieldSize.y / 2f;

            // Create top gate
            if (m_TopGate == null)
            {
                var topGateObj = Instantiate(m_GatePrefab, new Vector3(0, 0, halfHeight - m_EdgeOffset), Quaternion.Euler(0, 180, 0));
                topGateObj.name = "WormholeGate_Top";
                topGateObj.transform.parent = transform;
                m_TopGate = topGateObj.GetComponent<WormholeGate>();
            }

            // Create bottom gate
            if (m_BottomGate == null)
            {
                var bottomGateObj = Instantiate(m_GatePrefab, new Vector3(0, 0, -halfHeight + m_EdgeOffset), Quaternion.identity);
                bottomGateObj.name = "WormholeGate_Bottom";
                bottomGateObj.transform.parent = transform;
                m_BottomGate = bottomGateObj.GetComponent<WormholeGate>();
            }

            // Create left gate
            if (m_LeftGate == null)
            {
                var leftGateObj = Instantiate(m_GatePrefab, new Vector3(-halfWidth + m_EdgeOffset, 0, 0), Quaternion.Euler(0, 90, 0));
                leftGateObj.name = "WormholeGate_Left";
                leftGateObj.transform.parent = transform;
                m_LeftGate = leftGateObj.GetComponent<WormholeGate>();
            }

            // Create right gate
            if (m_RightGate == null)
            {
                var rightGateObj = Instantiate(m_GatePrefab, new Vector3(halfWidth - m_EdgeOffset, 0, 0), Quaternion.Euler(0, -90, 0));
                rightGateObj.name = "WormholeGate_Right";
                rightGateObj.transform.parent = transform;
                m_RightGate = rightGateObj.GetComponent<WormholeGate>();
            }
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
                Debug.Log("Connected Top ↔ Bottom gates");
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
                Debug.Log("Connected Left ↔ Right gates");
            }
            else
            {
                Debug.LogWarning("Cannot connect Left and Right gates - one or both are missing");
            }
        }

        /// <summary>
        ///     Validates the gate connections
        /// </summary>
        public bool ValidateConnections()
        {
            var isValid = true;

            if (m_TopGate == null || m_BottomGate == null)
            {
                Debug.LogError("Top or Bottom gate is missing!");
                isValid = false;
            }

            if (m_LeftGate == null || m_RightGate == null)
            {
                Debug.LogError("Left or Right gate is missing!");
                isValid = false;
            }

            if (m_TopGate != null && m_TopGate.m_ConnectedGate != m_BottomGate)
            {
                Debug.LogError("Top gate is not connected to Bottom gate!");
                isValid = false;
            }

            if (m_LeftGate != null && m_LeftGate.m_ConnectedGate != m_RightGate)
            {
                Debug.LogError("Left gate is not connected to Right gate!");
                isValid = false;
            }

            return isValid;
        }
    }
}
