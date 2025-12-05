using System.Linq;
using Tanks.Complete;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Tooltip("Reference to the P1 Stock")]
    [SerializeField] public PlayerStock m_StockP1;

    [Tooltip("Reference to the P2 Stock")]
    [SerializeField] public PlayerStock m_StockP2;

    [Tooltip("Reference to the GameManager")]
    [SerializeField] public GameManager m_GameManager;

    [SerializeField]
    private Camera player1Camera;

    public void Start()
    {
        // プレイヤー1の戦車プレハブから MinimapCamera を取得
        var cam = m_GameManager.m_Tank1Prefab.GetComponentInChildren<Camera>(true);
        player1Camera = cam;

        m_StockP1.gameObject.SetActive(false);
        m_StockP2.gameObject.SetActive(false);

        // TankPrefab を配列にまとめる（存在しない場合は null のまま）
        var tankPrefabs = new[]
        {
            m_GameManager.m_Tank1Prefab,
            m_GameManager.m_Tank2Prefab,
            m_GameManager.m_Tank3Prefab,
            m_GameManager.m_Tank4Prefab
        };

        m_GameManager.OnGameLoopStateChanged += HandleGameLoopStateChanged;
        foreach (var tank in m_GameManager.m_SpawnPoints)
        {
            // 新しいWeaponStockData対応のイベントを購読
            tank.OnWeaponStockChanged += HandleWeaponStockChanged;
        }
    }

    public void OnDestroy()
    {
        m_GameManager.OnGameLoopStateChanged -= HandleGameLoopStateChanged;
        foreach (var tank in m_GameManager.m_SpawnPoints)
        {
            tank.OnWeaponStockChanged -= HandleWeaponStockChanged;
        }
    }

    private bool DoesP1Exist()
    {
        return m_GameManager.m_Players?.Any(tank => tank.ControlIndex == 1) ?? false;
    }

    private bool DoesP2Exist()
    {
        return m_GameManager.m_Players?.Any(tank => tank.ControlIndex == 2) ?? false;
    }

    private void HandleGameLoopStateChanged(GameManager.GameLoopState state)
    {
        if (state == GameManager.GameLoopState.RoundPlaying)
        {
            // プレイヤー1の実体を探す
            var p1 = m_GameManager.m_Players.FirstOrDefault(t => t.ControlIndex == 1);
            if (p1 != null)
            {
                // インスタンスから MinimapCamera を取得
                player1Camera = p1.m_Instance.transform.GetComponentInChildren<Camera>(true);
                if (player1Camera != null)
                {
                    player1Camera.enabled = true;
                }
            }
        }
        else
        {
            if (player1Camera != null)
                player1Camera.enabled = false;
        }
    }

    /// <summary>
    ///     WeaponStockDataを使用した武器所持数変化のハンドラ
    /// </summary>
    /// <param name="controlIndex">プレイヤーのコントロールインデックス</param>
    /// <param name="stockData">武器の所持数データ</param>
    private void HandleWeaponStockChanged(int controlIndex, WeaponStockData stockData)
    {
        switch (controlIndex)
        {
            case 1:
                m_StockP1.UpdatePlayerStock(stockData);
                break;
            case 2:
                m_StockP2.UpdatePlayerStock(stockData);
                break;
        }
    }
}
