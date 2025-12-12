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


    // 勝利数のキャッシュ
    private int lastP1Wins;
    private int lastP2Wins;

    private void Start()
    {
        // Nullガード
        if (m_GameManager == null)
        {
            Debug.LogError("[HUDManager] m_GameManager is null");
            return;
        }
        if (m_StockP1 == null) Debug.LogError("[HUDManager] m_StockP1 is null");
        if (m_StockP2 == null) Debug.LogError("[HUDManager] m_StockP2 is null");

        // 初期は非表示（仕様）
        if (m_StockP1 != null) m_StockP1.gameObject.SetActive(false);
        if (m_StockP2 != null) m_StockP2.gameObject.SetActive(false);

        // ゲーム状態イベントを購読
        m_GameManager.OnGameLoopStateChanged += HandleGameLoopStateChanged;

        // GameManager 由来の RoundWinner も購読して取りこぼし防止
        m_GameManager.OnRoundWinnerChanged += HandleRoundWinnerFromGM;
    }


    private void OnDestroy()
    {
        if (m_GameManager != null)
        {
            m_GameManager.OnGameLoopStateChanged -= HandleGameLoopStateChanged;
        }

        // 購読と解除の対象を m_Players で揃える
        if (m_GameManager?.m_Players != null)
        {
            foreach (var tank in m_GameManager.m_Players)
            {
                tank.OnWeaponStockChanged -= HandleWeaponStockChanged;
                tank.OnHealthChanged -= HandleHealthChanged;
                tank.OnWinCountChanged -= HandleWinCountChanged;
            }
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
                // イベント購読
                tank.OnWeaponStockChanged += HandleWeaponStockChanged;
                tank.OnHealthChanged += HandleHealthChanged;
                tank.OnWinCountChanged += HandleWinCountChanged;

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


            // 勝利数のキャッシュをUIに反映（取りこぼし対策）
            m_StockP1?.UpdateWinCount(lastP1Wins);
            m_StockP2?.UpdateWinCount(lastP2Wins);

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
                tank.OnWinCountChanged -= HandleWinCountChanged;
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

    // ラウンド勝利数 UI 更新
    private void HandleWinCountChanged(int controlIndex, int wins)
    {
        // キャッシュ更新（RoundEndでも値を保持）
        if (controlIndex == 1) lastP1Wins = wins;
        else if (controlIndex == 2) lastP2Wins = wins;

        // UIが有効なら直ちに反映
        if (m_StockP1 != null && m_StockP1.gameObject.activeInHierarchy && controlIndex == 1)
            m_StockP1.UpdateWinCount(wins);
        else if (m_StockP2 != null && m_StockP2.gameObject.activeInHierarchy && controlIndex == 2)
            m_StockP2.UpdateWinCount(wins);
    }

    // GameManager 由来の勝者通知にも対応
    private void HandleRoundWinnerFromGM(int controlIndex, int totalWins)
    {
        HandleWinCountChanged(controlIndex, totalWins);
    }
}
