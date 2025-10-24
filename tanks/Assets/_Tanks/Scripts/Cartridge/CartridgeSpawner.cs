using Tanks.Complete;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class CartridgeSpawner : MonoBehaviour
{
    [Tooltip("Prefab for the shell cartridge to spawn")]
    public GameObject shellCartridge;
    [Tooltip("Interval in seconds between spawns")]
    public float spawnInterval = 10f;
    [Tooltip("Area in which to spawn cartridges (x,z)")]
    public Vector2 spawnArea = new Vector2(70f, 70f);
    [Tooltip("The height at which to spawn the cartridges")]
    public float spawnHeight = 5f;

    public GameManager gameManager;

    private Coroutine spawnRoutine;
    private Transform cartridgeGroup;

    private void Start()
    {
        gameManager.OnGameLoopStateChanged += HandleGameLoopStateChanged;

        var groupObject = new GameObject("Cartridge Group");
        groupObject.transform.SetParent(transform, false);
        cartridgeGroup = groupObject.transform;
    }

    private void OnDestroy()
    {
        gameManager.OnGameLoopStateChanged -= HandleGameLoopStateChanged;
    }

    private void SpawnCartridge()
    {
        var position = new Vector3(
            Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
            spawnHeight,
            Random.Range(-spawnArea.y / 2, spawnArea.y / 2)
        );
        var rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        Instantiate(shellCartridge, position, rotation, cartridgeGroup);
    }

    private void WipeAllCartridges()
    {
        if (!cartridgeGroup) return;
        for (var i = cartridgeGroup.childCount - 1; i >= 0; i--)
        {
            var child = cartridgeGroup.GetChild(i);
            if (child) Destroy(child.gameObject);
        }
    }

    private System.Collections.IEnumerator SpawnRoutine()
    {
        var wait = new WaitForSeconds(spawnInterval);
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
                spawnRoutine ??= StartCoroutine(SpawnRoutine());
                break;

            case GameManager.GameLoopState.RoundEnding:
                if (spawnRoutine != null)
                {
                    StopCoroutine(spawnRoutine);
                    spawnRoutine = null;
                }
                WipeAllCartridges();
                break;
        }
    }
}
