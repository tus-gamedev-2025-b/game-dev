using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    /// Controls the third-person shooter (TPS) camera for the tank game.
    /// Follows and rotates around a target (turret or hull), applying position and rotation offsets.
    /// Handles special cases for different tank variants (e.g., Medium Variant with reversed orientation).
    /// Smoothly interpolates camera position and rotation, prioritizing turret tracking when available.
    /// </summary>
    public class TPSCameraControl : MonoBehaviour
        [Header("追従対象（砲塔 or 車体）")]
        public Transform target;

        [Header("オフセット設定")]
        public Vector3 posOffset = new Vector3(2f, 4f, -6f); // TPS用：斜め後ろ

        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotateSpeed = 5f;
        private bool initialAdjusted = false;

        private void Start()
        {
            if (target != null && target.name.Contains("001")) // Medium Variant
            {
                // 初期向きを180度回転
                transform.Rotate(0f, 180f, 0f, Space.World);
                Debug.Log("Applied 180 rotation to Medium Variant in Start");
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // 砲塔を優先して追従
            var turret = FindTurretRecursive(target);

            // Medium Variant の場合はオフセット方向を反転
            var back = -target.forward;
            if (target.name.Contains("001")) // Medium Variant
            {
                back = target.forward; // 前方向を反転させて背後に回る
            }

            var right = target.right;
            var up = Vector3.up;

            var desiredPosition = target.position
                                  + right * posOffset.x
                                  + up * posOffset.y
                                  + back * Mathf.Abs(posOffset.z);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

            // LookAtは砲塔中心を狙う
            var lookAtPos = turret != null ? turret.position + Vector3.up * 0.5f : target.position + Vector3.up * 0.5f;

            var targetRotation = Quaternion.LookRotation(lookAtPos - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }

        private Transform FindTurretRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                var lowerName = child.name.ToLower();
                if (lowerName.Contains("turret") || lowerName.Contains("barrel"))
                    return child;

                var found = FindTurretRecursive(child);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
