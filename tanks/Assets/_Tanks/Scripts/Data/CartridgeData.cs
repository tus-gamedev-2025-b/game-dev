using System;
using UnityEngine;

namespace Tanks.Complete
{
    /// <summary>
    ///     カートリッジのデータを管理するクラス
    ///     Inspectorでの表示を可能にするためSerializableを付与
    /// </summary>
    [Serializable]
    public class CartridgeData
    {
        [Tooltip("カートリッジのプレハブ")]
        public GameObject cartridgePrefab;

        [Tooltip("生成間隔（秒）")]
        public float spawnInterval = 10f;
    }
}
