using System.Collections;
using Tanks.Complete;
using UnityEngine;
using Random = UnityEngine.Random;

public class CartridgeSpawner : MonoBehaviour
{
    [Tooltip("Prefab for the shell cartridge to spawn")]
    public GameObject m_ShellCartridge;
    [Tooltip("Interval in seconds between spawns")]
    public float m_SpawnInterval = 10f;
    [Tooltip("Area in which to spawn cartridges (x,z)")]
    public Vector2 m_SpawnArea = new Vector2(70f, 70f);
    [Tooltip("The height at which to spawn the cartridges")]
    public float m_SpawnHeight = 5f;

    public GameManager m_GameManager;
    private Transform m_CartridgeGroup;

    private Coroutine m_SpawnRoutine;

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

    private void SpawnCartridge()
    {
        var position = new Vector3(
            Random.Range(-m_SpawnArea.x / 2, m_SpawnArea.x / 2),
            m_SpawnHeight,
            Random.Range(-m_SpawnArea.y / 2, m_SpawnArea.y / 2)
        );
        var rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        Instantiate(m_ShellCartridge, position, rotation, m_CartridgeGroup);
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

    private IEnumerator SpawnRoutine()
    {
        var wait = new WaitForSeconds(m_SpawnInterval);
        while (true)
        {
            SpawnCartridge();
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
                m_SpawnRoutine ??= StartCoroutine(SpawnRoutine());
                break;

            case GameManager.GameLoopState.RoundEnding:
                if (m_SpawnRoutine != null)
                {
                    StopCoroutine(m_SpawnRoutine);
                    m_SpawnRoutine = null;
                }
                WipeAllCartridges();
                break;
        }
    }
}
