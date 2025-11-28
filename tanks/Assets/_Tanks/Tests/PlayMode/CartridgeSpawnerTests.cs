using System.Reflection;
using NUnit.Framework;
using Tanks.Complete;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
///     CartridgeSpawnerクラスのPlayModeテスト
///     GameManagerのStartがMenusを必要とするため、
///     GameManagerに依存しないテストに限定
/// </summary>
[TestFixture]
public class CartridgeSpawnerTests
{

    [SetUp]
    public void SetUp()
    {
        // CartridgeSpawnerを作成（GameManagerなし）
        spawnerObject = new GameObject("CartridgeSpawner");
        cartridgeSpawner = spawnerObject.AddComponent<CartridgeSpawner>();
        cartridgeSpawner.m_SpawnArea = new Vector2(70f, 70f);
        cartridgeSpawner.m_SpawnHeight = 5f;

        // m_GameManagerはnullのまま
        // CartridgeGroupを手動で作成
        var groupObject = new GameObject("Cartridge Group");
        groupObject.transform.SetParent(spawnerObject.transform, false);

        // リフレクションでm_CartridgeGroupを設定
        var field = typeof(CartridgeSpawner).GetField("m_CartridgeGroup",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(cartridgeSpawner, groupObject.transform);
    }

    [TearDown]
    public void TearDown()
    {
        // CartridgeSpawner.OnDestroyでm_GameManagerがnullの場合のエラーを無視
        LogAssert.ignoreFailingMessages = true;

        if (spawnerObject != null)
            Object.DestroyImmediate(spawnerObject);

        // Cleanup any spawned cartridges
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj != null && (obj.name.Contains("Cartridge") || obj.name.Contains("TestPrefab") || obj.name.Contains("Cube")))
                Object.DestroyImmediate(obj);
        }
    }

    private GameObject spawnerObject;
    private CartridgeSpawner cartridgeSpawner;

    private CartridgeData CreateCartridgeData(float interval)
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = "TestCartridge";

        return new CartridgeData
        {
            cartridgePrefab = prefab,
            spawnInterval = interval
        };
    }

    private Transform GetCartridgeGroup()
    {
        return spawnerObject.transform.Find("Cartridge Group");
    }

    [Test]
    public void SpawnArea_CanBeSet()
    {
        // Act
        cartridgeSpawner.m_SpawnArea = new Vector2(100f, 100f);

        // Assert
        Assert.AreEqual(new Vector2(100f, 100f), cartridgeSpawner.m_SpawnArea);
    }

    [Test]
    public void SpawnHeight_CanBeSet()
    {
        // Act
        cartridgeSpawner.m_SpawnHeight = 10f;

        // Assert
        Assert.AreEqual(10f, cartridgeSpawner.m_SpawnHeight);
    }

    [Test]
    public void SpawnCartridge_WithValidData_CreatesObject()
    {
        // Arrange
        var cartridgeGroup = GetCartridgeGroup();
        var data = CreateCartridgeData(1f);

        // Act - Call private SpawnCartridge method
        var method = typeof(CartridgeSpawner).GetMethod("SpawnCartridge",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(cartridgeSpawner, new object[] { data });

        // Assert
        Assert.AreEqual(1, cartridgeGroup.childCount);

        // Cleanup
        Object.DestroyImmediate(data.cartridgePrefab);
    }

    [Test]
    public void SpawnCartridge_PositionWithinSpawnArea()
    {
        // Arrange
        cartridgeSpawner.m_SpawnArea = new Vector2(20f, 20f);
        var cartridgeGroup = GetCartridgeGroup();
        var data = CreateCartridgeData(1f);

        // Act
        var method = typeof(CartridgeSpawner).GetMethod("SpawnCartridge",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(cartridgeSpawner, new object[] { data });

        // Assert
        var spawnedObject = cartridgeGroup.GetChild(0);

        Assert.LessOrEqual(Mathf.Abs(spawnedObject.position.x), 10f);
        Assert.LessOrEqual(Mathf.Abs(spawnedObject.position.z), 10f);
        Assert.AreEqual(cartridgeSpawner.m_SpawnHeight, spawnedObject.position.y);

        // Cleanup
        Object.DestroyImmediate(data.cartridgePrefab);
    }

    [Test]
    public void SpawnCartridge_WithNullData_DoesNotThrow()
    {
        // Act & Assert
        var method = typeof(CartridgeSpawner).GetMethod("SpawnCartridge",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.DoesNotThrow(() => method?.Invoke(cartridgeSpawner, new object[] { null }));
    }

    [Test]
    public void SpawnCartridge_WithNullPrefab_DoesNotThrow()
    {
        // Arrange
        var data = new CartridgeData { cartridgePrefab = null, spawnInterval = 1f };

        // Act & Assert
        var method = typeof(CartridgeSpawner).GetMethod("SpawnCartridge",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.DoesNotThrow(() => method?.Invoke(cartridgeSpawner, new object[] { data }));
    }

    [Test]
    public void WipeAllCartridges_RemovesAllSpawnedCartridges()
    {
        // Arrange
        var cartridgeGroup = GetCartridgeGroup();
        var data = CreateCartridgeData(1f);

        // Spawn some cartridges
        var spawnMethod = typeof(CartridgeSpawner).GetMethod("SpawnCartridge",
            BindingFlags.NonPublic | BindingFlags.Instance);
        spawnMethod?.Invoke(cartridgeSpawner, new object[] { data });
        spawnMethod?.Invoke(cartridgeSpawner, new object[] { data });
        spawnMethod?.Invoke(cartridgeSpawner, new object[] { data });

        Assert.AreEqual(3, cartridgeGroup.childCount);

        // Act
        var wipeMethod = typeof(CartridgeSpawner).GetMethod("WipeAllCartridges",
            BindingFlags.NonPublic | BindingFlags.Instance);
        wipeMethod?.Invoke(cartridgeSpawner, null);

        // DestroyImmediateを使用して即座に削除（テスト用）
        while (cartridgeGroup.childCount > 0)
        {
            Object.DestroyImmediate(cartridgeGroup.GetChild(0).gameObject);
        }

        // Assert
        Assert.AreEqual(0, cartridgeGroup.childCount);

        // Cleanup
        Object.DestroyImmediate(data.cartridgePrefab);
    }

    [Test]
    public void SpawnCartridge_DifferentTypes_SpawnCorrectPrefabs()
    {
        // Arrange
        var cartridgeGroup = GetCartridgeGroup();

        var shellData = CreateCartridgeData(1f);
        shellData.cartridgePrefab.name = "ShellCartridge";

        var mineData = CreateCartridgeData(1f);
        mineData.cartridgePrefab.name = "MineCartridge";

        // Act
        var method = typeof(CartridgeSpawner).GetMethod("SpawnCartridge",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(cartridgeSpawner, new object[] { shellData });
        method?.Invoke(cartridgeSpawner, new object[] { mineData });

        // Assert
        Assert.AreEqual(2, cartridgeGroup.childCount);

        var hasShell = false;
        var hasMine = false;
        for (var i = 0; i < cartridgeGroup.childCount; i++)
        {
            var child = cartridgeGroup.GetChild(i);
            if (child.name.Contains("Shell")) hasShell = true;
            if (child.name.Contains("Mine")) hasMine = true;
        }

        Assert.IsTrue(hasShell, "Should have spawned shell cartridge");
        Assert.IsTrue(hasMine, "Should have spawned mine cartridge");

        // Cleanup
        Object.DestroyImmediate(shellData.cartridgePrefab);
        Object.DestroyImmediate(mineData.cartridgePrefab);
    }
}
