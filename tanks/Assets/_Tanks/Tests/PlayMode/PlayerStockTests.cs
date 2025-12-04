using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Tanks.Complete;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
///     PlayerStockクラスのPlayModeテスト
/// </summary>
[TestFixture]
public class PlayerStockTests
{

    [SetUp]
    public void SetUp()
    {
        // PlayerStockオブジェクトを作成
        stockObject = new GameObject("PlayerStock");
        playerStock = stockObject.AddComponent<PlayerStock>();

        // 砲弾イメージ用のコンテナを作成
        shellContainer = new GameObject("ShellContainer");
        shellContainer.transform.SetParent(stockObject.transform);

        // 地雷イメージを作成
        mineImages = new Image[3];
        for (var i = 0; i < 3; i++)
        {
            var mineObj = new GameObject($"Mine{i + 1}");
            mineObj.transform.SetParent(stockObject.transform);
            mineImages[i] = mineObj.AddComponent<Image>();
        }

        // リフレクションで地雷イメージを設定
        var field = typeof(PlayerStock).GetField("mineImages", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(playerStock, mineImages);
    }

    [TearDown]
    public void TearDown()
    {
        if (stockObject != null)
            Object.DestroyImmediate(stockObject);
    }

    private GameObject stockObject;
    private PlayerStock playerStock;
    private GameObject shellContainer;
    private Image[] mineImages;

    private void SetupShellImagePrefab()
    {
        var prefab = new GameObject("ShellImagePrefab");
        prefab.AddComponent<Image>();

        var field = typeof(PlayerStock).GetField("m_ShellImagePrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(playerStock, prefab);
    }

    private WeaponStockData CreateWeaponStockData(string name, int current)
    {
        var stockData = new WeaponStockData();
        var type = typeof(WeaponStockData);

        type.GetField("m_WeaponName", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, name);
        type.GetField("m_InitialQuantity", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, current);
        type.GetField("m_MaxCapacity", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, 10);

        stockData.InitializeQuantity();
        return stockData;
    }

    [UnityTest]
    public IEnumerator Start_HidesMineImages()
    {
        // Act - Start is called automatically
        yield return null;

        // Assert
        foreach (var mineImage in mineImages)
        {
            Assert.IsFalse(mineImage.gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator InitPlayerStock_CreatesShellImages()
    {
        // Arrange
        SetupShellImagePrefab();
        yield return null;

        // Act
        playerStock.InitPlayerStock(5, 3);

        // Assert
        Assert.AreEqual(5, shellContainer.transform.childCount);
    }

    [UnityTest]
    public IEnumerator InitPlayerStock_ActivatesCorrectNumberOfImages()
    {
        // Arrange
        SetupShellImagePrefab();
        yield return null;

        // Act
        playerStock.InitPlayerStock(5, 3);

        // Assert
        var activeCount = 0;
        for (var i = 0; i < shellContainer.transform.childCount; i++)
        {
            if (shellContainer.transform.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }
        Assert.AreEqual(3, activeCount);
    }

    [UnityTest]
    public IEnumerator InitPlayerStock_CalledTwice_UpdatesStock()
    {
        // Arrange
        SetupShellImagePrefab();
        yield return null;

        // Act
        playerStock.InitPlayerStock(5, 5);
        playerStock.InitPlayerStock(5, 2);

        // Assert
        var activeCount = 0;
        for (var i = 0; i < shellContainer.transform.childCount; i++)
        {
            if (shellContainer.transform.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }
        Assert.AreEqual(2, activeCount);
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_Int_UpdatesShellImages()
    {
        // Arrange
        SetupShellImagePrefab();
        yield return null;
        playerStock.InitPlayerStock(5, 5);

        // Act
        playerStock.UpdatePlayerStock(3);

        // Assert
        var activeCount = 0;
        for (var i = 0; i < shellContainer.transform.childCount; i++)
        {
            if (shellContainer.transform.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }
        Assert.AreEqual(3, activeCount);
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_Int_Zero_HidesAllImages()
    {
        // Arrange
        SetupShellImagePrefab();
        yield return null;
        playerStock.InitPlayerStock(5, 5);

        // Act
        playerStock.UpdatePlayerStock(0);

        // Assert
        var activeCount = 0;
        for (var i = 0; i < shellContainer.transform.childCount; i++)
        {
            if (shellContainer.transform.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }
        Assert.AreEqual(0, activeCount);
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_ShellData_UpdatesShellUI()
    {
        // Arrange
        SetupShellImagePrefab();
        yield return null;
        playerStock.InitPlayerStock(5, 5);
        var stockData = CreateWeaponStockData("Shell", 2);

        // Act
        playerStock.UpdatePlayerStock(stockData);

        // Assert
        var activeCount = 0;
        for (var i = 0; i < shellContainer.transform.childCount; i++)
        {
            if (shellContainer.transform.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }
        Assert.AreEqual(2, activeCount);
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_MineData_UpdatesMineUI()
    {
        // Arrange
        yield return null;
        var stockData = CreateWeaponStockData("Mine", 2);

        // Act
        playerStock.UpdatePlayerStock(stockData);

        // Assert
        Assert.IsTrue(mineImages[0].gameObject.activeSelf);
        Assert.IsTrue(mineImages[1].gameObject.activeSelf);
        Assert.IsFalse(mineImages[2].gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_MineData_Zero_HidesAllMines()
    {
        // Arrange
        yield return null;

        // First show all
        foreach (var img in mineImages)
            img.gameObject.SetActive(true);

        var stockData = CreateWeaponStockData("Mine", 0);

        // Act
        playerStock.UpdatePlayerStock(stockData);

        // Assert
        foreach (var mineImage in mineImages)
        {
            Assert.IsFalse(mineImage.gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_MineData_Max_ShowsAllMines()
    {
        // Arrange
        yield return null;
        var stockData = CreateWeaponStockData("Mine", 3);

        // Act
        playerStock.UpdatePlayerStock(stockData);

        // Assert
        foreach (var mineImage in mineImages)
        {
            Assert.IsTrue(mineImage.gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_NullData_DoesNotThrow()
    {
        // Arrange
        yield return null;

        // Act & Assert
        Assert.DoesNotThrow(() => playerStock.UpdatePlayerStock(null));
    }

    [UnityTest]
    public IEnumerator UpdateMineStock_Int_UpdatesMineImages()
    {
        // Arrange
        yield return null;

        // Act
        playerStock.UpdateMineStock(2);

        // Assert
        Assert.IsTrue(mineImages[0].gameObject.activeSelf);
        Assert.IsTrue(mineImages[1].gameObject.activeSelf);
        Assert.IsFalse(mineImages[2].gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator UpdateMineStock_Int_Zero_HidesAll()
    {
        // Arrange
        yield return null;
        foreach (var img in mineImages)
            img.gameObject.SetActive(true);

        // Act
        playerStock.UpdateMineStock(0);

        // Assert
        foreach (var mineImage in mineImages)
        {
            Assert.IsFalse(mineImage.gameObject.activeSelf);
        }
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_JapaneseShellName_Works()
    {
        // Arrange
        SetupShellImagePrefab();
        yield return null;
        playerStock.InitPlayerStock(5, 5);
        var stockData = CreateWeaponStockData("砲弾", 3);

        // Act
        playerStock.UpdatePlayerStock(stockData);

        // Assert
        var activeCount = 0;
        for (var i = 0; i < shellContainer.transform.childCount; i++)
        {
            if (shellContainer.transform.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }
        Assert.AreEqual(3, activeCount);
    }

    [UnityTest]
    public IEnumerator UpdatePlayerStock_JapaneseMineName_Works()
    {
        // Arrange
        yield return null;
        var stockData = CreateWeaponStockData("地雷", 2);

        // Act
        playerStock.UpdatePlayerStock(stockData);

        // Assert
        Assert.IsTrue(mineImages[0].gameObject.activeSelf);
        Assert.IsTrue(mineImages[1].gameObject.activeSelf);
        Assert.IsFalse(mineImages[2].gameObject.activeSelf);
    }
}
