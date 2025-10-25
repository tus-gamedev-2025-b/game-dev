using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class TankShooting : MonoBehaviour
    {
        public Rigidbody m_Shell;           // Prefab of the shell.
        public Transform m_FireTransform;   // A child of the tank where the shells are spawned.
        public Slider m_AimSlider;          // A child of the tank that displays the current launch force.
        public AudioSource m_ShootingAudio; // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;    // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;        // Audio that plays when each shot is fired.
        [Tooltip("The speed in unit/second the shell have when fired at minimum charge")]
        public float m_MinLaunchForce = 5f; // The force given to the shell if the fire button is not held.
        [Tooltip("The speed in unit/second the shell have when fired at max charge")]
        public float m_MaxLaunchForce = 20f; // The force given to the shell if the fire button is held for the max charge time.
        [Tooltip("The maximum time spent charging. When charging reach that time, the shell is fired at MaxLaunchForce")]
        public float m_MaxChargeTime = 0.75f; // How long the shell can charge for before it is fired at max force.
        [Tooltip("The time that must pass before being able to shoot again after a shot")]
        public float m_ShotCooldown = 1.0f; // The time required between 2 shots
        [Header("Shell Properties")]
        [Tooltip("The amount of health removed to a tank if they are exactly on the landing spot of a shell")]
        public float m_MaxDamage = 100f; // The amount of damage done if the explosion is centred on a tank.
        [Tooltip("The force of the explosion at the shell position. Keep it 50 and below")]
        public float m_ExplosionForce = 50f; // The amount of force added to a tank at the centre of the explosion.
        [Tooltip(
            "The radius of the explosion in Unity unit. Force decrease with distance to the center, and an tank further than this from the shell explosion won't be impacted by the explosion")]
        public float m_ExplosionRadius = 5f; // The maximum distance away from the explosion tanks can be and are still affected.

        [Tooltip("The number of shells the tank starts with")]
        public int m_StartingShells = 10;
        [Tooltip("The maximum number of shells the tank can carry")]
        public int m_MaxShells = 50;
        [Tooltip("The number of shells added when a shell cartridge power-up is collected")]
        public int m_ShellsPerCartridge = 10;

        [HideInInspector]
        public TankInputUser m_InputUser;   // The Input User component for that tanks. Contains the Input Actions.
        private InputAction fireAction;     // The Input Action for shooting, retrieve from TankInputUser
        private float m_BaseMinLaunchForce; // The initial value of m_MinLaunchForce
        private float m_ChargeSpeed;        // How fast the launch force increases, based on the max charge time.
        private float m_CurrentLaunchForce; // The force that will be given to the shell when the fire button is released.

        // The current number of shells the tank has
        private int m_CurrentShells;

        private string m_FireButton;    // The input axis that is used for launching shells.
        private bool m_Fired;           // Whether or not the shell has been launched with this button press.
        private bool m_HasSpecialShell; // has the tank a shell that makes extra damage?
        private bool m_IsChargingForward;
        private float m_ShotCooldownTimer;      // The timer counting down before a shot is allowed again
        private float m_SpecialShellMultiplier; // The amount that the special shell will multiply the damage.
        public int CurrentShells
        {
            get => m_CurrentShells;
            private set
            {
                if (value < 0 || value > m_MaxShells || value == m_CurrentShells)
                    return;
                m_CurrentShells = value;
                OnShellStockChanges?.Invoke(m_CurrentShells);
            }
        }

        public float CurrentChargeRatio =>
            (m_CurrentLaunchForce - m_MinLaunchForce) / (m_MaxLaunchForce - m_MinLaunchForce); //The charging amount between 0-1

        public bool IsCharging { get; private set; }

        public bool m_IsComputerControlled { get; set; } = false;

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();
        }

        private void Start()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire";
            fireAction = m_InputUser.ActionAsset.FindAction(m_FireButton);

            fireAction.Enable();

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }


        private void Update()
        {
            // Computer and Human control Tank use 2 different update functions
            if (!m_IsComputerControlled)
            {
                HumanUpdate();
            }
            else
            {
                ComputerUpdate();
            }
        }

        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force, the UI and the power ups
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_BaseMinLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_BaseMinLaunchForce;
            m_HasSpecialShell = false;
            m_SpecialShellMultiplier = 1.0f;
            m_IsChargingForward = true;

            m_AimSlider.minValue = m_MinLaunchForce;
            m_AimSlider.maxValue = m_MaxLaunchForce;

            CurrentShells = m_StartingShells;
        }

        public void OnCollisionEnter(Collision collision)
        {
            // If we collide with a shell cartridge, consume it and add shells
            if (collision.gameObject.CompareTag("ShellCartridge"))
            {
                AddShells();
                Destroy(collision.gameObject);
            }
        }

        public event Action<int> OnShellStockChanges;

        /// <summary>
        ///     Used by AI to start charging
        /// </summary>
        public void StartCharging()
        {
            IsCharging = true;
            // ... reset the fired flag and reset the launch force.
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            // Change the clip to the charging clip and start it playing.
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }

        public void StopCharging()
        {
            if (IsCharging)
            {
                Fire();
                IsCharging = false;
            }
        }

        private void ComputerUpdate()
        {
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_BaseMinLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // ... use the max force and launch the shell.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (IsCharging && !m_Fired)
            {
                // Increment the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                // ... launch the shell.
                Fire();
                IsCharging = false;
            }
        }

        private void HumanUpdate()
        {
            // if there is a cooldown timer, decrement it
            if (m_ShotCooldownTimer > 0.0f)
            {
                m_ShotCooldownTimer -= Time.deltaTime;
            }

            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_BaseMinLaunchForce;

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MaxLaunchForce;
                m_IsChargingForward = false;
            }
            // Otherwise, if the min force has been exceeded and the shell hasn't yet been launched...
            else if (m_CurrentLaunchForce <= m_MinLaunchForce && !m_Fired)
            {
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_IsChargingForward = true;
            }

            // If the fire button has just started being pressed...
            if (m_ShotCooldownTimer <= 0 && fireAction.WasPressedThisFrame())
            {
                // ... reset the fired flag and reset the launch force.
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_IsChargingForward = true;

                // Change the clip to the charging clip and start it playing.
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (fireAction.IsPressed() && !m_Fired)
            {
                // Increment of decrement the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime * (m_IsChargingForward ? 1 : -1);

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                // ... launch the shell.
                Fire();
            }
        }


        private void Fire()
        {
            // Check we have shells to fire
            if (CurrentShells <= 0)
            {
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_ShotCooldownTimer = m_ShotCooldown;
                m_IsChargingForward = true;
                return;
            }

            // Set the fired flag so only Fire is only called once.
            m_Fired = true;

            // Create an instance of the shell and store a reference to it's rigidbody.
            var shellInstance =
                Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation);

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

            var explosionData = shellInstance.GetComponent<ShellExplosion>();
            explosionData.m_ExplosionForce = m_ExplosionForce;
            explosionData.m_ExplosionRadius = m_ExplosionRadius;
            explosionData.m_MaxDamage = m_MaxDamage;

            // Increase the damage if extra damage PowerUp is active
            if (m_HasSpecialShell)
            {
                explosionData.m_MaxDamage *= m_SpecialShellMultiplier;
                // Reset the default values after increasing the damage of the fired shell
                m_HasSpecialShell = false;
                m_SpecialShellMultiplier = 1f;

                var powerUpDetector = GetComponent<PowerUpDetector>();
                if (powerUpDetector != null)
                    powerUpDetector.m_HasActivePowerUp = false;

                var powerUpHUD = GetComponentInChildren<PowerUpHUD>();
                if (powerUpHUD != null)
                    powerUpHUD.DisableActiveHUD();
            }

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            // Reset the launch force.  This is a precaution in case of missing button events.
            m_CurrentLaunchForce = m_MinLaunchForce;

            m_ShotCooldownTimer = m_ShotCooldown;

            // Reset the charging direction for the next shot
            m_IsChargingForward = true;

            // Consume a shell
            CurrentShells = Mathf.Max(0, CurrentShells - 1);
        }


        public void EquipSpecialShell(float damageMultiplier)
        {
            m_HasSpecialShell = true;
            m_SpecialShellMultiplier = damageMultiplier;
        }

        /// <summary>
        ///     Return the estyimated position the projectile will have with the charging level (between 0 & 1)
        /// </summary>
        /// <param name="chargingLevel">The fire charging level between 0 - 1</param>
        /// <returns>The position at which the projectile will be (ignore obstacle)</returns>
        public Vector3 GetProjectilePosition(float chargingLevel)
        {
            var chargeLevel = Mathf.Lerp(m_MinLaunchForce, m_MaxLaunchForce, chargingLevel);
            var velocity = chargeLevel * m_FireTransform.forward;

            var a = 0.5f * Physics.gravity.y;
            var b = velocity.y;
            var c = m_FireTransform.position.y;

            var sqrtContent = b * b - 4 * a * c;
            //no solution
            if (sqrtContent <= 0)
            {
                return m_FireTransform.position;
            }

            var answer1 = (-b + Mathf.Sqrt(sqrtContent)) / (2 * a);
            var answer2 = (-b - Mathf.Sqrt(sqrtContent)) / (2 * a);

            var answer = answer1 > 0 ? answer1 : answer2;

            var position = m_FireTransform.position +
                           new Vector3(velocity.x, 0, velocity.z) *
                           answer;
            position.y = 0;

            return position;
        }

        public void AddShells()
        {
            CurrentShells = Mathf.Min(CurrentShells + m_ShellsPerCartridge, m_MaxShells);
        }
    }
}
