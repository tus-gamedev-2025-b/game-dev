using System.Linq;
using Tanks.Complete;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Tooltip("Reference to the P1 Stock")]
    [SerializeField] public PlayerStock stockP1;

    [Tooltip("Reference to the P2 Stock")]
    [SerializeField] public PlayerStock stockP2;

    [Tooltip("Reference to the GameManager")]
    [SerializeField] public GameManager gameManager;

    public void Start()
    {
        stockP1.gameObject.SetActive(false);
        stockP2.gameObject.SetActive(false);

        gameManager.OnGameLoopStateChanged += HandleGameLoopStateChanged;
        foreach (var tank in gameManager.m_SpawnPoints)
        {
            tank.OnWeaponStockChanged += HandleWeaponStockChanged;
        }
    }

    public void OnDestroy()
    {
        gameManager.OnGameLoopStateChanged -= HandleGameLoopStateChanged;
        foreach (var tank in gameManager.m_SpawnPoints)
        {
            tank.OnWeaponStockChanged -= HandleWeaponStockChanged;
        }
    }

    private bool DoesP2Exist()
    {
        return gameManager.m_SpawnPoints.Any(tank => tank.ControlIndex == 2);
    }

    private void HandleGameLoopStateChanged(GameManager.GameLoopState state)
    {
        if (state == GameManager.GameLoopState.RoundPlaying)
        {
            stockP1.gameObject.SetActive(true);
            if (DoesP2Exist()) stockP2.gameObject.SetActive(true);

            foreach (var tank in gameManager.m_SpawnPoints)
            {
                HandleWeaponStockChanged(tank.ControlIndex, tank.ShellStock);
            }
        }
        else
        {
            stockP1.gameObject.SetActive(false);
            stockP2.gameObject.SetActive(false);
        }
    }

    private void HandleWeaponStockChanged(int controlIndex, int stock)
    {
        switch (controlIndex)
        {
            case 1:
                stockP1.UpdatePlayerStock(stock);
                break;
            case 2:
                stockP2.UpdatePlayerStock(stock);
                break;
        }
    }
}
