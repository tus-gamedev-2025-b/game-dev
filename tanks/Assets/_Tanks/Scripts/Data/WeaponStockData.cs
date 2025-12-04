using System;
using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    ///     武器の所持数を管理するデータクラス
    ///     砲弾や地雷など、複数の武器の管理を一元化
    /// </summary>
    [Serializable]
    public class WeaponStockData
    {
        [SerializeField]
        [Tooltip("武器の名前")]
        private string m_WeaponName;

        [SerializeField]
        [Tooltip("武器の所持数の初期値")]
        private int m_InitialQuantity = 10;

        [SerializeField]
        [Tooltip("所持できる武器の最大数")]
        private int m_MaxCapacity = 50;

        [SerializeField]
        [Tooltip("カートリッジ取得時に補充される数")]
        private int m_ReplenishQuantity = 10;

        /// <summary>
        ///     現在の武器所持数
        /// </summary>
        private int m_CurrentQuantity;

        /// <summary>
        ///     武器名を取得
        /// </summary>
        public string WeaponName => m_WeaponName;

        /// <summary>
        ///     現在の所持数を取得
        /// </summary>
        public int CurrentQuantity => m_CurrentQuantity;

        /// <summary>
        ///     最大所持数を取得
        /// </summary>
        public int MaxCapacity => m_MaxCapacity;

        /// <summary>
        ///     初期所持数を取得
        /// </summary>
        public int InitialQuantity => m_InitialQuantity;

        /// <summary>
        ///     補充数を取得
        /// </summary>
        public int ReplenishQuantity => m_ReplenishQuantity;

        /// <summary>
        ///     武器を使用できるかどうか
        /// </summary>
        public bool CanUse => m_CurrentQuantity > 0;

        /// <summary>
        ///     所持数を初期化する
        /// </summary>
        public void InitializeQuantity()
        {
            m_CurrentQuantity = m_InitialQuantity;
        }

        /// <summary>
        ///     武器を補充する（最大値を超えない）
        /// </summary>
        public void Replenish()
        {
            m_CurrentQuantity = Mathf.Min(m_CurrentQuantity + m_ReplenishQuantity, m_MaxCapacity);
        }

        /// <summary>
        ///     指定数だけ補充する（最大値を超えない）
        /// </summary>
        /// <param name="amount">補充する数</param>
        public void Replenish(int amount)
        {
            m_CurrentQuantity = Mathf.Min(m_CurrentQuantity + amount, m_MaxCapacity);
        }

        /// <summary>
        ///     武器を1つ使用する（0を下回らない）
        /// </summary>
        /// <returns>使用できた場合はtrue</returns>
        public bool Use()
        {
            if (m_CurrentQuantity <= 0)
                return false;

            m_CurrentQuantity = Mathf.Max(0, m_CurrentQuantity - 1);
            return true;
        }
    }
}
