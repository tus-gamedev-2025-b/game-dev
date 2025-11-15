using UnityEngine;

namespace Tanks.Complete
{
    public class TPSCameraControl : MonoBehaviour
    {
        [Header("追従対象（砲塔 or 車体）")]
        public Transform target;

        [Header("オフセット設定")]
        public Vector3 posOffset = new Vector3(0f, 5f, -8f);
        public Vector3 rotOffset = Vector3.zero;

        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotateSpeed = 5f;

        private void LateUpdate()
        {
            if (target == null) return;

            var forward = target.forward;

            // Medium Variant などで forward が逆なら反転
            if (Vector3.Dot(forward, target.parent.forward) < 0f)
            {
                forward = -forward;
            }

            var desiredPosition = target.position - forward * Mathf.Abs(posOffset.z) + Vector3.up * posOffset.y;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

            // 砲塔を見る方向
            var lookPos = target.position + Vector3.up * 0.5f; // 砲塔の中心を少し上に
            var targetRotation = Quaternion.LookRotation(lookPos - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }
    }
}
