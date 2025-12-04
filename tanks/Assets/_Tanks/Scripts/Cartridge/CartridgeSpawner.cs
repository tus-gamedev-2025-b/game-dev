using System.Collections;
using Tanks.Complete;
using UnityEngine;
using Random = UnityEngine.Random;

public class CartridgeSpawner : MonoBehaviour
{
    [Header("Cartridge Data")]
    [Tooltip("砲弾カートリッジのデータ")]
    [SerializeField] private CartridgeData m_ShellCartridgeData;

    [Tooltip("地雷カートリッジのデータ")]
    [SerializeField] private CartridgeData m_MineCartridgeData;

    [Header("Spawn Settings")]
    [Tooltip("Area in which to spawn cartridges (x,z)")]
    public Vector2 m_SpawnArea = new Vector2(70f, 70f);
    [Tooltip("The height at which to spawn the cartridges")]
    public float m_SpawnHeight = 5f;

    public GameManager m_GameManager;
    private Transform m_CartridgeGroup;
    private Coroutine m_MineSpawnRoutine;

    private Coroutine m_ShellSpawnRoutine;

    private void Start()
    {
        m_GameManager.OnGameLoopStateChanged += HandleGameLoopStateChanged;

        var groupObject = new GameObject("Cartridge Group");
        groupObject.transform.SetParent(transform, false);
        m_CartridgeGroup = groupObject.transform;
    }

    private void OnDestroy()
    {
        m_GameManager.OnGameLoopStateChanged -= HandleGameLoopStateChanged;
    }

    /// <summary>
    ///     指定されたカートリッジデータを使用してカートリッジを生成
    /// </summary>
    /// <param name="cartridgeData">生成するカートリッジのデータ</param>
    private void SpawnCartridge(CartridgeData cartridgeData)
    {
        if (cartridgeData == null || cartridgeData.cartridgePrefab == null)
            return;

        var position = new Vector3(
            Random.Range(-m_SpawnArea.x / 2, m_SpawnArea.x / 2),
            m_SpawnHeight,
            Random.Range(-m_SpawnArea.y / 2, m_SpawnArea.y / 2)
        );
        var rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        Instantiate(cartridgeData.cartridgePrefab, position, rotation, m_CartridgeGroup);
    }

    private void WipeAllCartridges()
    {
        if (!m_CartridgeGroup) return;
        for (var i = m_CartridgeGroup.childCount - 1; i >= 0; i--)
        {
            var child = m_CartridgeGroup.GetChild(i);
            if (child) Destroy(child.gameObject);
        }
    }

    /// <summary>
    ///     指定されたカートリッジデータを使用して定期的にカートリッジを生成するコルーチン
    /// </summary>
    /// <param name="cartridgeData">生成するカートリッジのデータ</param>
    private IEnumerator SpawnRoutine(CartridgeData cartridgeData)
    {
        if (cartridgeData == null)
            yield break;

        var wait = new WaitForSeconds(cartridgeData.spawnInterval);
        while (true)
        {
            SpawnCartridge(cartridgeData);
            yield return wait;
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private void HandleGameLoopStateChanged(GameManager.GameLoopState state)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (state)
        {
            case GameManager.GameLoopState.RoundPlaying:
                // 砲弾カートリッジの生成開始
                if (m_ShellCartridgeData != null && m_ShellCartridgeData.cartridgePrefab != null)
                {
                    m_ShellSpawnRoutine ??= StartCoroutine(SpawnRoutine(m_ShellCartridgeData));
                }

                // 地雷カートリッジの生成開始
                if (m_MineCartridgeData != null && m_MineCartridgeData.cartridgePrefab != null)
                {
                    m_MineSpawnRoutine ??= StartCoroutine(SpawnRoutine(m_MineCartridgeData));
                }
                break;

            case GameManager.GameLoopState.RoundEnding:
                // 砲弾カートリッジの生成停止
                if (m_ShellSpawnRoutine != null)
                {
                    StopCoroutine(m_ShellSpawnRoutine);
                    m_ShellSpawnRoutine = null;
                }

                // 地雷カートリッジの生成停止
                if (m_MineSpawnRoutine != null)
                {
                    StopCoroutine(m_MineSpawnRoutine);
                    m_MineSpawnRoutine = null;
                }

                WipeAllCartridges();
                break;
        }
    }
}
