using System.Collections;
using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    ///     Manages the tank's state during wormhole teleportation.
    ///     Handles blinking effect, invincibility, and attack restrictions.
    /// </summary>
    [RequireComponent(typeof(TankHealth))]
    [RequireComponent(typeof(Rigidbody))]
    public class TankWormholeState : MonoBehaviour
    {
        [Header("Teleportation Settings")]
        [Tooltip("Duration of blinking at entrance gate before teleporting (in seconds)")]
        public float m_EnterDuration = 1.0f;

        [Tooltip("Duration of blinking at exit gate after teleporting (in seconds)")]
        public float m_ExitDuration = 1.0f;

        [Tooltip("How fast the tank blinks during teleportation (blinks per second)")]
        public float m_BlinkFrequency = 5f;

        [Tooltip("Cooldown time after teleportation to prevent immediate re-teleportation")]
        public float m_TeleportCooldown = 1.0f;
        private float m_CooldownTimer;

        // State tracking
        private Renderer[] m_Renderers;
        private Rigidbody m_Rigidbody;

        [Header("References")]
        private TankHealth m_TankHealth;
        private TankShooting m_TankShooting;
        private Coroutine m_TeleportCoroutine;

        public bool IsTeleporting { get; private set; }
        public bool IsInCooldown => m_CooldownTimer > 0f;

        private void Awake()
        {
            // Get required components
            m_TankHealth = GetComponent<TankHealth>();
            m_TankShooting = GetComponent<TankShooting>();
            m_Rigidbody = GetComponent<Rigidbody>();

            // Get all renderers for blinking effect
            m_Renderers = GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            // Update cooldown timer
            if (m_CooldownTimer > 0f)
            {
                m_CooldownTimer -= Time.deltaTime;
                if (m_CooldownTimer < 0f)
                {
                    m_CooldownTimer = 0f;
                }
            }
        }

        private void OnDisable()
        {
            // Stop teleportation if the tank is disabled
            if (m_TeleportCoroutine != null)
            {
                StopCoroutine(m_TeleportCoroutine);
                m_TeleportCoroutine = null;
            }

            // Ensure tank is visible
            SetRenderersEnabled(true);

            // Ensure invincibility is disabled
            if (IsTeleporting && m_TankHealth != null)
            {
                m_TankHealth.ToggleInvincibility();
            }

            IsTeleporting = false;
            m_CooldownTimer = 0f;
        }

        /// <summary>
        ///     Starts the teleportation process from entrance gate to exit gate
        /// </summary>
        public void StartTeleportation(WormholeGate entranceGate, WormholeGate exitGate)
        {
            // Don't teleport if already teleporting
            if (IsTeleporting)
            {
                return;
            }

            // Don't teleport if in cooldown (prevents infinite loop)
            if (IsInCooldown)
            {
                return;
            }

            if (exitGate == null)
            {
                Debug.LogError("Exit gate is null!");
                return;
            }

            // Stop any existing teleportation
            if (m_TeleportCoroutine != null)
            {
                StopCoroutine(m_TeleportCoroutine);
            }

            // Start new teleportation
            m_TeleportCoroutine = StartCoroutine(TeleportCoroutine(entranceGate, exitGate));
        }

        private IEnumerator TeleportCoroutine(WormholeGate entranceGate, WormholeGate exitGate)
        {
            IsTeleporting = true;

            // Enable invincibility
            if (m_TankHealth != null)
            {
                m_TankHealth.ToggleInvincibility();
            }

            // Phase 1: Blink at entrance gate (stay in place)
            var elapsed = 0f;
            var blinkTimer = 0f;
            var isVisible = true;

            while (elapsed < m_EnterDuration)
            {
                elapsed += Time.deltaTime;
                blinkTimer += Time.deltaTime;

                // Handle blinking effect
                var blinkInterval = 1f / m_BlinkFrequency / 2f; // Divide by 2 for on/off cycle
                if (blinkTimer >= blinkInterval)
                {
                    isVisible = !isVisible;
                    SetRenderersEnabled(isVisible);
                    blinkTimer = 0f;
                }

                yield return null;
            }

            // Phase 2: Instant teleportation
            var targetPosition = exitGate.GetExitPosition();
            var targetRotation = exitGate.GetExitRotation();

            if (m_Rigidbody != null)
            {
                m_Rigidbody.MovePosition(targetPosition);
            }
            else
            {
                transform.position = targetPosition;
            }

            // Rotate to face away from the gate (so pressing forward moves away from gate)
            transform.rotation = targetRotation;

            // Notify exit gate
            exitGate.OnTankExit(gameObject);

            // Phase 3: Blink at exit gate
            elapsed = 0f;
            blinkTimer = 0f;
            isVisible = true;

            while (elapsed < m_ExitDuration)
            {
                elapsed += Time.deltaTime;
                blinkTimer += Time.deltaTime;

                // Handle blinking effect
                var blinkInterval = 1f / m_BlinkFrequency / 2f;
                if (blinkTimer >= blinkInterval)
                {
                    isVisible = !isVisible;
                    SetRenderersEnabled(isVisible);
                    blinkTimer = 0f;
                }

                yield return null;
            }

            // Ensure tank is visible
            SetRenderersEnabled(true);

            // Disable invincibility
            if (m_TankHealth != null)
            {
                m_TankHealth.ToggleInvincibility();
            }

            IsTeleporting = false;

            // Set cooldown timer to prevent immediate re-teleportation
            m_CooldownTimer = m_TeleportCooldown;

            m_TeleportCoroutine = null;
        }

        /// <summary>
        ///     Enables or disables all renderers on the tank
        /// </summary>
        private void SetRenderersEnabled(bool enabled)
        {
            foreach (var renderer in m_Renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        /// <summary>
        ///     Checks if the tank can move (not during teleportation)
        /// </summary>
        public bool CanMove()
        {
            return !IsTeleporting;
        }

        /// <summary>
        ///     Checks if the tank can shoot (not during teleportation)
        /// </summary>
        public bool CanShoot()
        {
            return !IsTeleporting;
        }

        /// <summary>
        ///     Checks if the tank can place mines (not during teleportation)
        /// </summary>
        public bool CanPlaceMine()
        {
            return !IsTeleporting;
        }
    }
}
