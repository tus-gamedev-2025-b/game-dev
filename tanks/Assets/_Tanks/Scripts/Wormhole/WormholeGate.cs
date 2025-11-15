using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    ///     Represents a single wormhole gate that teleports tanks to a connected gate.
    ///     Gates should be placed at the edges of the battlefield.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WormholeGate : MonoBehaviour
    {
        [Header("Gate Configuration")]
        [Tooltip("The gate this gate is connected to")]
        public WormholeGate m_ConnectedGate;

        [Tooltip("The position offset from the gate where tanks will appear (in local space)")]
        public Vector3 m_ExitOffset = Vector3.forward * 5f;

        [Header("Visual Effects")]
        [Tooltip("Particle system to play when a tank enters")]
        public ParticleSystem m_EnterEffect;

        [Tooltip("Particle system to play when a tank exits")]
        public ParticleSystem m_ExitEffect;

        [Header("Audio")]
        [Tooltip("Sound effect when tank enters the gate")]
        public AudioClip m_EnterSound;

        [Tooltip("Sound effect when tank exits the gate")]
        public AudioClip m_ExitSound;

        private AudioSource m_AudioSource;

        private void Awake()
        {
            // Add AudioSource if not present
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void OnDrawGizmos()
        {
            // Draw connection line to connected gate
            if (m_ConnectedGate != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, m_ConnectedGate.transform.position);
            }

            // Draw exit position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GetExitPosition(), 0.5f);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the object entering is a shell (shells cannot pass through)
            // Check shells first to avoid processing them as tanks
            var shell = other.GetComponent<ShellExplosion>();
            if (shell != null)
            {
                HandleShellEntry(other.gameObject);
                return;
            }

            // Check if the object entering is a tank
            var tankState = other.GetComponent<TankWormholeState>();
            if (tankState != null)
            {
                HandleTankEntry(other.gameObject);
            }
        }

        /// <summary>
        ///     Handles when a tank enters this gate
        /// </summary>
        private void HandleTankEntry(GameObject tank)
        {
            // Get the wormhole state component (guaranteed to exist by OnTriggerEnter check)
            var wormholeState = tank.GetComponent<TankWormholeState>();

            // Don't teleport if already in teleportation
            if (wormholeState.IsTeleporting)
            {
                return;
            }

            // Check if connected gate exists
            if (m_ConnectedGate == null)
            {
                Debug.LogWarning("Gate has no connected gate!");
                return;
            }

            // Play enter effects
            PlayEnterEffects();

            // Start teleportation
            wormholeState.StartTeleportation(this, m_ConnectedGate);
        }

        /// <summary>
        ///     Handles when a shell enters this gate (shells are destroyed)
        /// </summary>
        private void HandleShellEntry(GameObject shell)
        {
            // Shells cannot pass through wormholes - destroy them without triggering explosion
            Destroy(shell);
        }

        /// <summary>
        ///     Called when a tank exits from this gate
        /// </summary>
        public void OnTankExit(GameObject tank)
        {
            PlayExitEffects();
        }

        /// <summary>
        ///     Gets the world position where tanks should exit
        /// </summary>
        public Vector3 GetExitPosition()
        {
            return transform.position + transform.TransformDirection(m_ExitOffset);
        }

        /// <summary>
        ///     Gets the rotation tanks should have when exiting (faces the exit offset direction)
        /// </summary>
        public Quaternion GetExitRotation()
        {
            return transform.rotation;
        }

        private void PlayEnterEffects()
        {
            // Play particle effect
            if (m_EnterEffect != null)
            {
                m_EnterEffect.Play();
            }

            // Play sound effect
            if (m_EnterSound != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(m_EnterSound);
            }
        }

        private void PlayExitEffects()
        {
            // Play particle effect
            if (m_ExitEffect != null)
            {
                m_ExitEffect.Play();
            }

            // Play sound effect
            if (m_ExitSound != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(m_ExitSound);
            }
        }
    }
}
