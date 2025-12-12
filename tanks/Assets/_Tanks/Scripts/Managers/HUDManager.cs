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

    [SerializeField] private PlayerHP player1HP;
    [SerializeField] private PlayerHP player2HP;

    public void Start()
    {
        m_StockP1.gameObject.SetActive(false);
        m_StockP2.gameObject.SetActive(false);

        // ゲーム状態イベントを購読
        m_GameManager.OnGameLoopStateChanged += HandleGameLoopStateChanged;
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
        // UI を有効にする
        m_StockP1.gameObject.SetActive(true);
        m_StockP2.gameObject.SetActive(true);

        // Round 開始後にプレイヤー実体が存在
        foreach (var tank in m_GameManager.m_Players)
        {
            // ここで初めてイベントを購読できる
            tank.OnWeaponStockChanged += HandleWeaponStockChanged;
            tank.OnHealthChanged += HandleHealthChanged;

            // これで PlayerStock 内で砲弾アイコンを生成
            if (tank.ControlIndex == 1)
            {
                m_StockP1.InitPlayerStock(tank.MaxShellStock, tank.ShellStock);
                m_StockP1.UpdateMineStock(tank.MineStock);
            }
            else if (tank.ControlIndex == 2)
            {
                m_StockP2.InitPlayerStock(tank.MaxShellStock, tank.ShellStock);
                m_StockP2.UpdateMineStock(tank.MineStock);
            }
        }

        // プレイヤー1の実体を探す/minimap カメラ ON
        var p1 = m_GameManager.m_Players.FirstOrDefault(t => t.ControlIndex == 1);
        if (p1 != null)
        {
            // インスタンスから MinimapCamera を取得
            player1Camera = p1.m_Instance.GetComponentInChildren<Camera>(true);
            if (player1Camera != null)
                player1Camera.enabled = true;
        }
    }
    else
    {
        // minimap カメラ OFF
        if (player1Camera != null)
            player1Camera.enabled = false;

        // UI OFF
        m_StockP1.gameObject.SetActive(false);
        m_StockP2.gameObject.SetActive(false);

        // イベント解除
        foreach (var tank in m_GameManager.m_Players)
        {
            tank.OnWeaponStockChanged -= HandleWeaponStockChanged;
            tank.OnHealthChanged -= HandleHealthChanged;
        }
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

    private void HandleHealthChanged(int controlIndex, float value)
    {
        switch (controlIndex)
        {
            case 1:
                player1HP.UpdateHPSlider(value);
                break;
            case 2:
                player2HP.UpdateHPSlider(value);
                break;
        }
    }
}
