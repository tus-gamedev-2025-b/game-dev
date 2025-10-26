using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Tanks.Complete
{
    [DefaultExecutionOrder(-10)]
    public class TankMovement : MonoBehaviour
    {
        [Tooltip("The player number. Without a tank selection menu, Player 1 is left keyboard control, Player 2 is right keyboard")]
        public int m_PlayerNumber = 1; // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        [Tooltip("The speed in unity unit/second the tank move at")]
        public float m_Speed = 12f; // How fast the tank moves forward and back.
        [Tooltip("The speed in deg/s that tank will rotate at")]
        public float m_TurnSpeed = 180f; // How fast the tank turns in degrees per second.
        [Tooltip("If set to true, the tank auto orient and move toward the pressed direction instead of rotating on left/right and move forward on up")]
        public bool m_IsDirectControl;
        public AudioSource m_MovementAudio; // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        public AudioClip m_EngineIdling;    // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;   // Audio to play when the tank is moving.
        public float m_PitchRange = 0.2f;   // The amount by which the pitch of the engine noises can vary.
        [Tooltip("Is set to true this will be controlled by the computer and not a player")]
        public bool m_IsComputerControlled = false; // Is this tank player or computer controlled

        [HideInInspector]
        public TankInputUser m_InputUser; // The Input User component for that tank. Contains the Input Actions.

        public Rigidbody Rigidbody { get; private set; }
        public int ControlIndex { get; set; } = -1; // 1=left keyboard, 2=right keyboard, -1=none

        private string m_MovementAxisName;
        private string m_TurnAxisName;
        private float m_MovementInputValue;
        private float m_TurnInputValue;
        private Vector3 m_RequestedDirection;
        private float m_OriginalPitch;
        private Vector3 m_ExplosionForceValue;
        private ParticleSystem[] m_particleSystems;
        private InputAction m_MoveAction;
        private InputAction m_TurnAction;

        // ================================
        // ▼▼▼ 砲塔制御 追加部分 ▼▼▼
        // ================================
        private float m_TurretTurnInputValue;           // 入力量
        public float m_TurretTurnSpeedValue = 90f;      // 回転速度（調整可）
        public Transform m_TurretTransform;             // 砲塔Transform参照
        private string m_TurretTurnActionName;          // Action名
        private InputAction m_TurretTurnAction;         // InputAction参照
        public Transform m_TurretHUDTransform;          // TurretHUD の Transform 参照
        // ================================

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            m_InputUser = GetComponent<TankInputUser>();
            if (m_InputUser == null)
                m_InputUser = gameObject.AddComponent<TankInputUser>();

            // ▼ 砲塔TransformのNullチェック
            if (m_TurretTransform == null)
            {
                var turretObj = transform.Find("Turret");
                if (turretObj != null)
                    m_TurretTransform = turretObj;
                else
                    Debug.LogWarning($"[TankMovement] 砲塔オブジェクトが見つかりません: {name}");
            }
        }

        private void OnEnable()
        {
            Rigidbody.isKinematic = false;
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
            m_TurretTurnInputValue = 0f;
            m_ExplosionForceValue = Vector3.zero;

            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
                m_particleSystems[i].Play();
        }

        private void OnDisable()
        {
            Rigidbody.isKinematic = true;
            for (int i = 0; i < m_particleSystems.Length; ++i)
                m_particleSystems[i].Stop();
        }

        private void Start()
        {
            if (m_IsComputerControlled)
            {
                var ai = GetComponent<TankAI>();
                if (ai == null)
                    gameObject.AddComponent<TankAI>();
            }

            if (ControlIndex == -1 && !m_IsComputerControlled)
                ControlIndex = m_PlayerNumber;

            var mobileControl = FindAnyObjectByType<MobileUIControl>();

            if (mobileControl != null && ControlIndex == 1)
            {
                m_InputUser.SetNewInputUser(InputUser.PerformPairingWithDevice(mobileControl.Device));
                m_InputUser.ActivateScheme("Gamepad");
            }
            else
            {
                m_InputUser.ActivateScheme(ControlIndex == 1 ? "KeyboardLeft" : "KeyboardRight");
            }

            m_MovementAxisName = "Vertical";
            m_TurnAxisName = "Horizontal";

            m_MoveAction = m_InputUser.ActionAsset.FindAction(m_MovementAxisName);
            m_TurnAction = m_InputUser.ActionAsset.FindAction(m_TurnAxisName);

            m_MoveAction.Enable();
            m_TurnAction.Enable();

            // ▼ 砲塔回転用アクション設定
            m_TurretTurnActionName = "TurretTurn";
            m_TurretTurnAction = m_InputUser.ActionAsset.FindAction(m_TurretTurnActionName);
            if (m_TurretTurnAction != null)
                m_TurretTurnAction.Enable();
            else
                Debug.LogWarning($"[TankMovement] TurretTurn アクションが見つかりません（{name}）");

            if (m_MovementAudio)
                m_OriginalPitch = m_MovementAudio.pitch;
        }

        private void Update()
        {
            if (!m_IsComputerControlled)
            {
                m_MovementInputValue = m_MoveAction.ReadValue<float>();
                m_TurnInputValue = m_TurnAction.ReadValue<float>();

                if (m_TurretTurnAction != null)
                    m_TurretTurnInputValue = m_TurretTurnAction.ReadValue<float>();
            }

            if (m_MovementAudio)
                EngineAudio();
        }

        private void FixedUpdate()
        {
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                var camForward = Camera.main.transform.forward;
                camForward.y = 0;

                if (camForward.sqrMagnitude < 0.0001f)
                {
                    camForward = Camera.main.transform.up;
                    camForward.y = 0;
                }

                camForward.Normalize();
                var camRight = Vector3.Cross(Vector3.up, camForward);
                m_RequestedDirection = (camForward * m_MovementInputValue + camRight * m_TurnInputValue);
                m_RequestedDirection.Normalize();
            }

            Move();
            Turn();
            TurretTurn(); // 砲塔回転を追加
        }

        private void Move()
        {
            float speedInput = 0.0f;
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                speedInput = m_RequestedDirection.magnitude;
                speedInput *= 1.0f - Mathf.Clamp01((Vector3.Angle(m_RequestedDirection, transform.forward) - 90) / 90.0f);
            }
            else
            {
                speedInput = m_MovementInputValue;
            }

            Vector3 movement = transform.forward * speedInput * m_Speed;
            Rigidbody.linearVelocity = movement + m_ExplosionForceValue;
            m_ExplosionForceValue = Vector3.Lerp(m_ExplosionForceValue, Vector3.zero, Time.deltaTime * 3f);
        }

        private void Turn()
        {
            Quaternion turnRotation;
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                var angleTowardTarget = Vector3.SignedAngle(m_RequestedDirection, transform.forward, transform.up);
                var rotatingAngle = Mathf.Sign(angleTowardTarget) * Mathf.Min(Mathf.Abs(angleTowardTarget), m_TurnSpeed * Time.deltaTime);
                turnRotation = Quaternion.AngleAxis(-rotatingAngle, Vector3.up);
            }
            else
            {
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
                turnRotation = Quaternion.Euler(0f, turn, 0f);
            }

            Rigidbody.MoveRotation(Rigidbody.rotation * turnRotation);
        }

        // ===================================
        // ▼▼▼ 砲塔回転メソッド ▼▼▼
        // ===================================
        private void TurretTurn()
        {
            if (m_TurretTransform == null)
                return;

            float turn = m_TurretTurnInputValue * m_TurretTurnSpeedValue * Time.deltaTime;
            Quaternion turretRotation = Quaternion.Euler(0f, turn, 0f);

            m_TurretTransform.localRotation *= turretRotation;

            if (m_TurretHUDTransform != null)
                m_TurretHUDTransform.localRotation *= turretRotation;
        }
        // ===================================

        private void EngineAudio()
        {
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }

        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier = 0f)
        {
            var explosionDir = transform.position - explosionPosition;
            var explosionDistance = explosionDir.magnitude;

            if (upwardsModifier != 0)
            {
                explosionDir.y += upwardsModifier;
                explosionDir.Normalize();
            }
            else
            {
                explosionDir = explosionDir.normalized;
            }

            var attenuation = 1f - Mathf.Clamp01(explosionDistance / explosionRadius);
            var velocityChange = explosionDir * (explosionForce * attenuation);
            m_ExplosionForceValue = velocityChange;
        }
    }
}