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

        [Header("Weapon Stock Data")]
        [Tooltip("砲弾の所持数管理データ")]
        [SerializeField] private WeaponStockData m_ShellStockData;

        [Header("Mine Settings")]
        [Tooltip("地雷の所持数管理データ")]
        [SerializeField] private WeaponStockData m_MineStockData;

        [Tooltip("地雷のプレハブ")]
        [SerializeField] private GameObject m_Mine;

        [HideInInspector]
        public TankInputUser m_InputUser;   // The Input User component for that tanks. Contains the Input Actions.
        private InputAction fireAction;     // The Input Action for shooting, retrieve from TankInputUser
        private float m_BaseMinLaunchForce; // The initial value of m_MinLaunchForce
        private float m_ChargeSpeed;        // How fast the launch force increases, based on the max charge time.
        private float m_CurrentLaunchForce; // The force that will be given to the shell when the fire button is released.

        private string m_FireButton;    // The input axis that is used for launching shells.
        private bool m_Fired;           // Whether or not the shell has been launched with this button press.
        private bool m_HasSpecialShell; // has the tank a shell that makes extra damage?
        private bool m_IsChargingForward;
        private string m_SetMineButton;         // The input axis that is used for setting mines.
        private float m_ShotCooldownTimer;      // The timer counting down before a shot is allowed again
        private float m_SpecialShellMultiplier; // The amount that the special shell will multiply the damage.

        // 武器の初期化済みフラグ（ラウンド開始時のみ初期化するため）
        private bool m_WeaponStockInitialized;

        // Wormhole state component for checking teleportation
        private TankWormholeState m_WormholeState;
        private InputAction setMineAction; // The Input Action for setting mines

        /// <summary>
        ///     砲弾の現在所持数
        /// </summary>
        public int CurrentShells => m_ShellStockData?.CurrentQuantity ?? 0;

        /// <summary>
        ///     砲弾の最大所持数
        /// </summary>
        public int MaxShells => m_ShellStockData?.MaxCapacity ?? 0;

        /// <summary>
        ///     地雷の現在所持数
        /// </summary>
        public int CurrentMines => m_MineStockData?.CurrentQuantity ?? 0;

        /// <summary>
        ///     地雷の最大所持数
        /// </summary>
        public int MaxMines => m_MineStockData?.MaxCapacity ?? 0;

        public float CurrentChargeRatio =>
            (m_CurrentLaunchForce - m_MinLaunchForce) / (m_MaxLaunchForce - m_MinLaunchForce); //The charging amount between 0-1

        public bool IsCharging { get; private set; }

        public bool m_IsComputerControlled { get; set; } = false;

        private void Awake()
        {
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();

            // Get wormhole state component (may be null if not using wormholes)
            m_WormholeState = GetComponent<TankWormholeState>();
        }

        private void Start()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire";
            m_SetMineButton = "SetMine";

            fireAction = m_InputUser.ActionAsset.FindAction(m_FireButton);
            setMineAction = m_InputUser.ActionAsset.FindAction(m_SetMineButton);

            fireAction?.Enable();
            setMineAction?.Enable();

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
            m_HasSpecialShell = false;
            m_SpecialShellMultiplier = 1.0f;
            m_IsChargingForward = true;

            // Sliderが設定されている場合のみUI更新
            if (m_AimSlider != null)
            {
                m_AimSlider.value = m_BaseMinLaunchForce;
                m_AimSlider.minValue = m_MinLaunchForce;
                m_AimSlider.maxValue = m_MaxLaunchForce;
            }

            // 武器の初期化は最初の1回のみ（ラウンド開始時）
            // Reset()が呼ばれた後のOnEnableでのみ初期化する
            // （TankManager.DisableControl/EnableControlでは初期化しない）
        }

        public void OnCollisionEnter(Collision collision)
        {
            // If we collide with a shell cartridge, consume it and add shells
            if (collision.gameObject.CompareTag("ShellCartridge"))
            {
                AddShells();
                Destroy(collision.gameObject);
            }
            // 地雷カートリッジに衝突した場合
            else if (collision.gameObject.CompareTag("MineCartridge"))
            {
                AddMines();
                Destroy(collision.gameObject);
            }
        }

        /// <summary>
        ///     武器の所持数が変化したときに発生するイベント
        ///     Parameters: (WeaponStockData stockData)
        /// </summary>
        public event Action<WeaponStockData> OnWeaponStockChanged;

        /// <summary>
        ///     地雷が設置されたときに発生するイベント
        /// </summary>
        public event Action OnMinePlaced;

        // 後方互換性のために残す（既存のHUDManager等への対応）
        public event Action<int> OnShellStockChanges;

        /// <summary>
        ///     ラウンド開始時に武器の所持数を初期化する
        ///     TankManager.Reset()から呼ばれることを想定
        /// </summary>
        public void InitializeWeaponStock()
        {
            m_ShellStockData?.InitializeQuantity();
            m_MineStockData?.InitializeQuantity();
            m_WeaponStockInitialized = true;

            // 初期化後に通知
            NotifyShellStockChange();
            NotifyMineStockChange();
        }

        /// <summary>
        ///     武器が初期化済みでなければ初期化する（後方互換性用）
        /// </summary>
        private void EnsureWeaponStockInitialized()
        {
            if (!m_WeaponStockInitialized)
            {
                InitializeWeaponStock();
            }
        }

        /// <summary>
        ///     Used by AI to start charging
        /// </summary>
        public void StartCharging()
        {
            // 武器が初期化されていなければ初期化
            EnsureWeaponStockInitialized();

            // Cannot charge during wormhole teleportation
            if (m_WormholeState != null && !m_WormholeState.CanShoot())
            {
                return;
            }

            IsCharging = true;
            // ... reset the fired flag and reset the launch force.
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            // Change the clip to the charging clip and start it playing.
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }

        /// <summary>
        ///     Used by AI to stop charging once reached the target
        /// </summary>
        public void StopCharging()
        {
            IsCharging = false;
            Fire();
        }

        private void ComputerUpdate()
        {
            // 武器が初期化されていなければ初期化
            EnsureWeaponStockInitialized();

            // the AI control code live in the TankAI script, so here we just make sure we track the launched force and
            // update the slider if the AI requested us to charge
            if (IsCharging)
            {
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime * (m_IsChargingForward ? 1 : -1);

                m_AimSlider.value = m_CurrentLaunchForce;

                if (m_CurrentLaunchForce > m_MaxLaunchForce)
                {
                    //m_IsChargingForward = false;
                    //StopCharging();
                }

                if (m_CurrentLaunchForce < m_MinLaunchForce)
                {
                    m_IsChargingForward = true;
                }
            }
        }

        private void HumanUpdate()
        {
            // 武器が初期化されていなければ初期化
            EnsureWeaponStockInitialized();

            if (m_ShotCooldownTimer > 0)
            {
                m_ShotCooldownTimer -= Time.deltaTime;
                return;
            }

            // If the max force has been exceeded and the shell hasn't yet been launched...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // Switch direction when reaching max force
                m_IsChargingForward = false;
            }

            // If we're below min launch force while charging backwards...
            if (m_CurrentLaunchForce <= m_MinLaunchForce && !m_IsChargingForward && !m_Fired)
            {
                m_IsChargingForward = true;
            }

            // If the fire button has just started being pressed...
            if (fireAction != null && fireAction.WasPressedThisFrame())
            {
                // Cannot start charging during wormhole teleportation
                if (m_WormholeState != null && !m_WormholeState.CanShoot())
                {
                    return;
                }

                // ... reset the fired flag and reset the launch force.
                m_Fired = false;
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_IsChargingForward = true;

                // Change the clip to the charging clip and start it playing.
                m_ShootingAudio.clip = m_ChargingClip;
                m_ShootingAudio.Play();
            }
            // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
            else if (fireAction != null && fireAction.IsPressed() && !m_Fired)
            {
                // Stop charging if teleporting
                if (m_WormholeState != null && !m_WormholeState.CanShoot())
                {
                    // Reset charge state
                    m_CurrentLaunchForce = m_MinLaunchForce;
                    m_ShootingAudio.Stop();
                    return;
                }

                // Increment or decrement the launch force and update the slider.
                m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime * (m_IsChargingForward ? 1 : -1);

                m_AimSlider.value = m_CurrentLaunchForce;
            }
            // Otherwise, if the fire button is released and the shell hasn't been launched yet...
            else if (fireAction != null && fireAction.WasReleasedThisFrame() && !m_Fired)
            {
                // ... launch the shell.
                Fire();
            }

            // 地雷設置の入力チェック
            if (setMineAction != null && setMineAction.WasPressedThisFrame())
            {
                PlaceMine();
            }
        }


        private void Fire()
        {
            // Check if tank is teleporting through wormhole (cannot shoot during teleportation)
            if (m_WormholeState != null && !m_WormholeState.CanShoot())
            {
                m_CurrentLaunchForce = m_MinLaunchForce;
                m_ShotCooldownTimer = m_ShotCooldown;
                m_IsChargingForward = true;
                return;
            }

            // Check we have shells to fire
            if (m_ShellStockData == null || !m_ShellStockData.CanUse)
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
            m_ShellStockData.Use();
            NotifyShellStockChange();
        }

        /// <summary>
        ///     地雷を設置する
        /// </summary>
        private void PlaceMine()
        {
            // 地雷を持っているかチェック
            if (m_MineStockData == null || !m_MineStockData.CanUse)
            {
                Debug.Log($"Cannot place mine: MineStockData is null={m_MineStockData == null}, CanUse={m_MineStockData?.CanUse}, CurrentMines={CurrentMines}");
                return;
            }

            // 地雷プレハブがあるかチェック
            if (m_Mine == null)
            {
                Debug.Log("Cannot place mine: Mine prefab is null");
                return;
            }

            // 地雷を消費
            var used = m_MineStockData.Use();
            Debug.Log($"Mine placed! Used={used}, Remaining mines: {CurrentMines}");

            NotifyMineStockChange();

            // 地雷設置イベントを発生（TankManagerで地雷のInstantiateと操作停止を行う）
            OnMinePlaced?.Invoke();
        }

        /// <summary>
        ///     地雷のプレハブを取得
        /// </summary>
        public GameObject GetMinePrefab()
        {
            return m_Mine;
        }

        /// <summary>
        ///     砲弾の所持数変化を通知
        /// </summary>
        private void NotifyShellStockChange()
        {
            OnWeaponStockChanged?.Invoke(m_ShellStockData);
            OnShellStockChanges?.Invoke(CurrentShells);
        }

        /// <summary>
        ///     地雷の所持数変化を通知
        /// </summary>
        private void NotifyMineStockChange()
        {
            OnWeaponStockChanged?.Invoke(m_MineStockData);
        }

        public void EquipSpecialShell(float damageMultiplier)
        {
            m_HasSpecialShell = true;
            m_SpecialShellMultiplier = damageMultiplier;
        }

        /// <summary>
        ///     Return the estimated position the projectile will have with the charging level (between 0 & 1)
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
            EnsureWeaponStockInitialized();
            m_ShellStockData?.Replenish();
            NotifyShellStockChange();
        }

        public void AddMines()
        {
            EnsureWeaponStockInitialized();
            m_MineStockData?.Replenish();
            NotifyMineStockChange();
        }

        /// <summary>
        ///     武器の初期化フラグをリセットする（ラウンドリセット時に呼ぶ）
        /// </summary>
        public void ResetWeaponStockInitialization()
        {
            m_WeaponStockInitialized = false;
        }
    }
}
