using UnityEngine;

namespace Tanks.Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        public LayerMask m_TankMask;                // Used to filter what the explosion affects, this should be set to "Players".
        public ParticleSystem m_ExplosionParticles; // Reference to the particles that will play on explosion.
        public AudioSource m_ExplosionAudio;        // Reference to the audio that will play on explosion.
        public float m_MaxLifeTime = 2f;            // The time in seconds before the shell is removed.

        // All those are hidden in inspector as they will actually come from the TankShooting scripts
        [HideInInspector] public float m_MaxDamage = 100f;     // The amount of damage done if the explosion is centred on a tank.
        [HideInInspector] public float m_ExplosionForce = 50f; // The amount of force added to a tank at the centre of the explosion.
        [HideInInspector] public float m_ExplosionRadius = 5f; // The maximum distance away from the explosion tanks can be and are still affected.

        [Header("Mine Settings")]
        [Tooltip("地雷のアーム時間（設置後この時間が経過するまで爆発しない）")]
        public float m_ArmingTime = 1.5f;

        // 設置者を無視する時間
        private readonly float m_PlacerIgnoreTime = 1.0f;

        // 地雷を設置したタンク（一定時間無視する）
        private GameObject m_PlacerTank;

        // 地雷の生成時刻
        private float m_SpawnTime;

        private void Start()
        {
            m_SpawnTime = Time.time;

            // 地雷の場合
            if (gameObject.CompareTag("Mine"))
            {
                // 地雷は最大生存時間後に自動削除（踏まれなかった場合）
                Destroy(gameObject, m_MaxLifeTime);
            }
            else
            {
                // 通常の砲弾は m_MaxLifeTime 後に削除
                Destroy(gameObject, m_MaxLifeTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 地雷の場合の特別な処理
            if (gameObject.CompareTag("Mine"))
            {
                // アーム時間が経過していなければ爆発しない
                var elapsedTime = Time.time - m_SpawnTime;
                if (elapsedTime < m_ArmingTime)
                {
                    return;
                }

                // 設置者のタンクは一定時間無視
                if (m_PlacerTank != null && elapsedTime < m_PlacerIgnoreTime)
                {
                    // 設置者のタンクまたはその子オブジェクトか確認
                    var current = other.transform;
                    while (current != null)
                    {
                        if (current.gameObject == m_PlacerTank)
                        {
                            return; // 設置者なので無視
                        }
                        current = current.parent;
                    }
                }

                // Rigidbodyを持つオブジェクトのみ反応
                var rb = other.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    // 親にRigidbodyがあるか確認
                    rb = other.GetComponentInParent<Rigidbody>();
                    if (rb == null)
                    {
                        return; // Rigidbodyがなければ無視（地面など）
                    }
                }

                // TankHealthを持つオブジェクトのみ反応（戦車のみ）
                var tankHealth = rb.GetComponent<TankHealth>();
                if (tankHealth == null)
                {
                    return; // 戦車以外は無視
                }
            }

            // 爆発処理
            Explode();
        }

        /// <summary>
        ///     爆発処理を実行
        /// </summary>
        private void Explode()
        {
            // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
            var colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

            // Go through all the colliders...
            for (var i = 0; i < colliders.Length; i++)
            {
                // ... and find their rigidbody.
                var targetRigidbody = colliders[i].GetComponent<Rigidbody>();

                // If they don't have a rigidbody, go on to the next collider.
                if (!targetRigidbody)
                    continue;

                // Add an explosion force.
                var tankMovement = targetRigidbody.GetComponent<TankMovement>();
                if (tankMovement != null)
                {
                    tankMovement.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);
                }

                // Find the TankHealth script associated with the rigidbody.
                var targetHealth = targetRigidbody.GetComponent<TankHealth>();

                // If there is no TankHealth script attached to the gameobject, go on to the next collider.
                if (!targetHealth)
                    continue;

                // Calculate the amount of damage the target should take based on it's distance from the shell.
                var damage = CalculateDamage(targetRigidbody.position);

                // Deal this damage to the tank.
                targetHealth.TakeDamage(damage);
            }

            // Unparent the particles from the shell.
            if (m_ExplosionParticles != null)
            {
                m_ExplosionParticles.transform.parent = null;

                // Play the particle system.
                m_ExplosionParticles.Play();

                // Once the particles have finished, destroy the gameobject they are on.
                var mainModule = m_ExplosionParticles.main;
                Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
            }

            // Play the explosion sound effect.
            if (m_ExplosionAudio != null)
            {
                m_ExplosionAudio.Play();
            }

            // Destroy the shell/mine.
            Destroy(gameObject);
        }

        private float CalculateDamage(Vector3 targetPosition)
        {
            // Create a vector from the shell to the target.
            var explosionToTarget = targetPosition - transform.position;

            // Calculate the distance from the shell to the target.
            var explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
            var relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

            // Calculate damage as this proportion of the maximum possible damage.
            var damage = relativeDistance * m_MaxDamage;

            // Make sure that the minimum damage is always 0.
            damage = Mathf.Max(0f, damage);

            return damage;
        }

        /// <summary>
        ///     地雷を設置したタンクを設定（一定時間このタンクでは爆発しない）
        /// </summary>
        /// <param name="placer">設置したタンクのGameObject</param>
        public void SetPlacer(GameObject placer)
        {
            m_PlacerTank = placer;
        }
    }
}
