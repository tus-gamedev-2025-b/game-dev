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

    public void Start()
    {
        m_StockP1.gameObject.SetActive(false);
        m_StockP2.gameObject.SetActive(false);

        m_GameManager.OnGameLoopStateChanged += HandleGameLoopStateChanged;
        foreach (var tank in m_GameManager.m_SpawnPoints)
        {
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
        return m_GameManager.m_SpawnPoints.Any(tank => tank.ControlIndex == 1);
    }

    private bool DoesP2Exist()
    {
        return m_GameManager.m_SpawnPoints.Any(tank => tank.ControlIndex == 2);
    }

    private void HandleGameLoopStateChanged(GameManager.GameLoopState state)
    {
        if (state == GameManager.GameLoopState.RoundPlaying)
        {
            if (DoesP1Exist()) m_StockP1.gameObject.SetActive(true);
            if (DoesP2Exist()) m_StockP2.gameObject.SetActive(true);

            foreach (var tank in m_GameManager.m_SpawnPoints)
            {
                switch (tank.ControlIndex)
                {
                    case 1:
                        m_StockP1.InitPlayerStock(tank.MaxShellStock, tank.ShellStock);
                        break;
                    case 2:
                        m_StockP2.InitPlayerStock(tank.MaxShellStock, tank.ShellStock);
                        break;
                }
            }
        }
        else
        {
            m_StockP1.gameObject.SetActive(false);
            m_StockP2.gameObject.SetActive(false);
        }
    }

    private void HandleWeaponStockChanged(int controlIndex, int stock)
    {
        switch (controlIndex)
        {
            case 1:
                m_StockP1.UpdatePlayerStock(stock);
                break;
            case 2:
                m_StockP2.UpdatePlayerStock(stock);
                break;
        }
    }
}
