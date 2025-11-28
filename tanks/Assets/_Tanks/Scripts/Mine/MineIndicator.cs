using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    ///     地雷の位置にドクロマークを表示するコンポーネント
    ///     回転して目立たせる
    /// </summary>
    public class MineIndicator : MonoBehaviour
    {
        [Header("Indicator Settings")]
        [Tooltip("ドクロマークのスプライト")]
        [SerializeField] private Sprite m_SkullSprite;

        [Tooltip("スプライトの色")]
        [SerializeField] private Color m_SpriteColor = Color.white;

        [Tooltip("スプライトのサイズ")]
        [SerializeField] private float m_Size = 1.5f;

        [Tooltip("地雷からの高さオフセット")]
        [SerializeField] private float m_HeightOffset = 2.0f;

        [Tooltip("回転速度（度/秒）")]
        [SerializeField] private float m_RotationSpeed = 90f;
        private GameObject m_IndicatorObj;

        private SpriteRenderer m_SpriteRenderer;

        private void Start()
        {
            CreateIndicator();
        }

        private void Update()
        {
            if (m_IndicatorObj == null)
                return;

            // Y軸で回転
            m_IndicatorObj.transform.Rotate(0, m_RotationSpeed * Time.deltaTime, 0);
        }

        private void CreateIndicator()
        {
            // 子オブジェクトとしてスプライトを作成
            m_IndicatorObj = new GameObject("SkullIndicator");
            m_IndicatorObj.transform.SetParent(transform);
            m_IndicatorObj.transform.localPosition = new Vector3(0, m_HeightOffset, 0);
            m_IndicatorObj.transform.localScale = new Vector3(m_Size, m_Size, m_Size);

            // SpriteRenderer コンポーネントを追加
            m_SpriteRenderer = m_IndicatorObj.AddComponent<SpriteRenderer>();
            m_SpriteRenderer.sprite = m_SkullSprite;
            m_SpriteRenderer.color = m_SpriteColor;

            // ソート順を設定
            m_SpriteRenderer.sortingOrder = 100;
        }
    }
}
