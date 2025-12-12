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

    [SerializeField] private Camera player1Camera;

    [SerializeField] private PlayerHP player1HP;
    [SerializeField] private PlayerHP player2HP;

    // HPのキャッシュ
    private float lastP1HP = 1f;
    private float lastP2HP = 1f;

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
        if (player1HP == null) Debug.LogWarning("[HUDManager] player1HP is null");
        if (player2HP == null) Debug.LogWarning("[HUDManager] player2HP is null");

        // 初期は非表示（仕様）
        if (m_StockP1 != null) m_StockP1.gameObject.SetActive(false);
        if (m_StockP2 != null) m_StockP2.gameObject.SetActive(false);
        if (player1HP != null) player1HP.gameObject.SetActive(false);
        if (player2HP != null) player2HP.gameObject.SetActive(false);

        // ゲーム状態イベントを購読
        m_GameManager.OnGameLoopStateChanged += HandleGameLoopStateChanged;

        // GameManager 由来の RoundWinner を常時購読（取りこぼし防止）
        m_GameManager.OnRoundWinnerChanged += HandleRoundWinnerFromGM;

        // プレイヤがすでに存在しているなら、イベント購読しておく
        if (m_GameManager.m_Players != null)
        {
            foreach (var tank in m_GameManager.m_Players)
            {
                tank.OnWeaponStockChanged += HandleWeaponStockChanged;
                tank.OnHealthChanged += HandleHealthChanged;
                tank.OnWinCountChanged += HandleWinCountChanged;
            }
        }
    }


    private void OnDestroy()
    {
        if (m_GameManager != null)
        {
            m_GameManager.OnGameLoopStateChanged -= HandleGameLoopStateChanged;
            m_GameManager.OnRoundWinnerChanged -= HandleRoundWinnerFromGM;
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
        bool playing = (state == GameManager.GameLoopState.RoundPlaying);

        if (playing)
        {
            // UI ON
            m_StockP1?.gameObject.SetActive(true);
            m_StockP2?.gameObject.SetActive(true);
            player1HP?.gameObject.SetActive(true);
            player2HP?.gameObject.SetActive(true);

            SubscribePlayers();

            // ストック初期表示（存在すれば）
            if (m_GameManager.m_Players != null)
            {
                foreach (var tank in m_GameManager.m_Players)
                {
                    if (tank.ControlIndex == 1)
                    {
                        m_StockP1?.InitPlayerStock(tank.MaxShellStock, tank.ShellStock);
                        m_StockP1?.UpdateMineStock(tank.MineStock);
                    }
                    else if (tank.ControlIndex == 2)
                    {
                        m_StockP2?.InitPlayerStock(tank.MaxShellStock, tank.ShellStock);
                        m_StockP2?.UpdateMineStock(tank.MineStock);
                    }
                }
            }

            // Minimap Camera
            var p1 = m_GameManager.m_Players?.FirstOrDefault(t => t.ControlIndex == 1);
            player1Camera = p1?.m_Instance?.GetComponentInChildren<Camera>(true);
            if (player1Camera != null) player1Camera.enabled = true;
        }
        else
        {
            if (player1Camera != null) player1Camera.enabled = false;

            // UI OFF
            m_StockP1?.gameObject.SetActive(false);
            m_StockP2?.gameObject.SetActive(false);
            player1HP?.gameObject.SetActive(false);
            player2HP?.gameObject.SetActive(false);

            UnsubscribePlayers();
        }
    }

    /// <summary>
    ///     WeaponStockDataを使用した武器所持数変化のハンドラ
    /// </summary>
    private void HandleWeaponStockChanged(int controlIndex, WeaponStockData stockData)
    {
        if (controlIndex == 1) m_StockP1?.UpdatePlayerStock(stockData);
        else if (controlIndex == 2) m_StockP2?.UpdatePlayerStock(stockData);
    }

    private void HandleHealthChanged(int playerNumber, float value)
    {
        if (playerNumber == 1) lastP1HP = value;
        else if (playerNumber == 2) lastP2HP = value;

        if (playerNumber == 1 && player1HP != null && player1HP.gameObject.activeInHierarchy)
            player1HP.UpdateHPSlider(value);
        else if (playerNumber == 2 && player2HP != null && player2HP.gameObject.activeInHierarchy)
            player2HP.UpdateHPSlider(value);
    }

    // ラウンド勝利数 UI 更新
    private void HandleWinCountChanged(int playerNumber, int wins)
    {
        if (playerNumber == 1) lastP1Wins = wins;
        else if (playerNumber == 2) lastP2Wins = wins;

        if (playerNumber == 1 && m_StockP1 != null && m_StockP1.gameObject.activeInHierarchy)
            m_StockP1.UpdateWinCount(wins);
        else if (playerNumber == 2 && m_StockP2 != null && m_StockP2.gameObject.activeInHierarchy)
            m_StockP2.UpdateWinCount(wins);
    }

    // GameManager 由来の勝者通知にも対応（常時購読）
    private void HandleRoundWinnerFromGM(int controlIndex, int totalWins)
    {
        HandleWinCountChanged(controlIndex, totalWins);
    }

    private void SubscribePlayers()
    {
        if (m_GameManager?.m_Players == null) return;

        foreach (var tm in m_GameManager.m_Players)
        {
            // 購読
            tm.OnWeaponStockChanged += HandleWeaponStockChanged;
            tm.OnHealthChanged      += HandleHealthChanged;
            tm.OnWinCountChanged    += HandleWinCountChanged;

            // 購読直後に「現在値」を読み取って UIへ即反映
            var health = tm.m_Instance ? tm.m_Instance.GetComponent<TankHealth>() : null;
            var normalized = health ? health.GetNormalizedHealth() : 1f;
            if (tm.m_PlayerNumber == 1)
            {
                player1HP?.UpdateHPSlider(normalized);
                m_StockP1?.UpdateWinCount(lastP1Wins);
            }
            else if (tm.m_PlayerNumber == 2)
            {
                player2HP?.UpdateHPSlider(normalized);
                m_StockP2?.UpdateWinCount(lastP2Wins);
            }
        }
    }

    private void UnsubscribePlayers()
    {
        if (m_GameManager?.m_Players == null) return;

        foreach (var tm in m_GameManager.m_Players)
        {
            tm.OnWeaponStockChanged -= HandleWeaponStockChanged;
            tm.OnHealthChanged      -= HandleHealthChanged;
            tm.OnWinCountChanged    -= HandleWinCountChanged;
        }
    }
}
