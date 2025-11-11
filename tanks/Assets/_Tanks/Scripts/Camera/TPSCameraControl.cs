using UnityEngine;

namespace Tanks.Complete
{
    public class TPSCameraControl : MonoBehaviour
    {
        [Header("追従対象（砲塔 or 車体）")]
        public Transform target;

        [Header("オフセット設定")]
        [SerializeField] private Vector3 posOffset = new Vector3(0f, 5f, -8f);
        [SerializeField] private Vector3 rotOffset = Vector3.zero;

        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotateSpeed = 5f;

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    Transform turret = player.transform.Find("TankRenderers/TankTurret");
                    target = turret != null ? turret : player.transform;
                    Debug.Log($"TPSCameraControl: {target.name} を追従対象に設定しました。");
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 worldPos = target.TransformPoint(posOffset);
            transform.position = Vector3.Lerp(transform.position, worldPos, Time.deltaTime * followSpeed);

            Quaternion worldRot = target.rotation * Quaternion.Euler(rotOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, worldRot, Time.deltaTime * rotateSpeed);
        }
    }
}