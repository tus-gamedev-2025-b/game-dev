using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Tanks.Complete
{
    [DefaultExecutionOrder(-10)]
    public class TankMovement : MonoBehaviour
    {
        public int m_PlayerNumber = 1;
        public float m_Speed = 12f;
        public float m_TurnSpeed = 180f;
        public bool m_IsDirectControl;
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;
        public bool m_IsComputerControlled = false;

        public Rigidbody Rigidbody => m_Rigidbody;
        public TankInputUser m_InputUser;

        public int ControlIndex { get; set; } = -1;

        private string m_MovementAxisName;
        private string m_TurnAxisName;
        private Rigidbody m_Rigidbody;
        private float m_MovementInputValue;
        private float m_TurnInputValue;
        private Vector3 m_ExplosionForceValue;
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;

        private InputAction m_MoveAction;
        private InputAction m_TurnAction;
        private Vector3 m_RequestedDirection;

        // ================================
        // ▼▼▼ 砲塔制御 追加部分 ▼▼▼
        // ================================
        private float m_TurretTurnInputValue;           // 入力量
        public float m_TurretTurnSpeedValue = 90f;      // 回転速度（調整可）
        public Transform m_TurretTransform;             // 砲塔Transform参照
        private string m_TurretTurnActionName;          // Action名
        private InputAction m_TurretTurnAction;         // InputAction参照

        // ▼ TurretHUD の Transform 参照
        public Transform m_TurretHUDTransform;
        // ================================

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
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
            m_Rigidbody.isKinematic = false;
            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
            m_ExplosionForceValue = Vector3.zero;
            m_TurretTurnInputValue = 0f;

            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
                m_particleSystems[i].Play();
        }

        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;
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
            TurretTurn();
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
            m_Rigidbody.linearVelocity = movement + m_ExplosionForceValue;
            m_ExplosionForceValue = Vector3.Lerp(m_ExplosionForceValue, Vector3.zero, Time.deltaTime * 3f);
        }

        private void Turn()
        {
            Quaternion turnRotation;
            if (m_InputUser.InputUser.controlScheme.Value.name == "Gamepad" || m_IsDirectControl)
            {
                float angleTowardTarget = Vector3.SignedAngle(m_RequestedDirection, transform.forward, transform.up);
                var rotatingAngle = Mathf.Sign(angleTowardTarget) * Mathf.Min(Mathf.Abs(angleTowardTarget), m_TurnSpeed * Time.deltaTime);
                turnRotation = Quaternion.AngleAxis(-rotatingAngle, Vector3.up);
            }
            else
            {
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
                turnRotation = Quaternion.Euler(0f, turn, 0f);
            }

            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }

        // ===================================
        // ▼▼▼ 砲塔回転メソッド ▼▼▼
        // ===================================
        private void TurretTurn()
        {
            if (m_TurretTransform == null)
                return;

            float turn = m_TurretTurnInputValue * m_TurretTurnSpeedValue * Time.deltaTime;

            // Quaternionを使ってY軸に回転
            Quaternion turretRotation = Quaternion.Euler(0f, turn, 0f);

            // 砲塔を回転
            m_TurretTransform.localRotation *= turretRotation;

            // TurretHUDも砲塔と同じ角度で回転
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
            Vector3 explosionDir = transform.position - explosionPosition;
            float explosionDistance = explosionDir.magnitude;

            if (upwardsModifier != 0)
            {
                explosionDir.y += upwardsModifier;
                explosionDir.Normalize();
            }
            else
            {
                explosionDir = explosionDir.normalized;
            }

            float attenuation = 1f - Mathf.Clamp01(explosionDistance / explosionRadius);
            Vector3 velocityChange = explosionDir * (explosionForce * attenuation);
            m_ExplosionForceValue = velocityChange;
        }
    }
}