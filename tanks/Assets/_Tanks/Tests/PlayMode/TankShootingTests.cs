using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Tanks.Complete;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
///     TankShootingクラスのPlayModeテスト
/// </summary>
[TestFixture]
public class TankShootingTests
{

    [SetUp]
    public void SetUp()
    {
        // テスト用のタンクオブジェクトを作成
        tankObject = new GameObject("TestTank");
        tankObject.AddComponent<Rigidbody>();

        // TankInputUser が必要なので追加（モック）
        var inputUser = tankObject.AddComponent<TankInputUser>();

        // TankShootingを追加
        tankShooting = tankObject.AddComponent<TankShooting>();

        // 必要なコンポーネントを設定
        var fireTransform = new GameObject("FireTransform").transform;
        fireTransform.SetParent(tankObject.transform);
        tankShooting.m_FireTransform = fireTransform;

        // Sliderを設定
        var sliderObject = new GameObject("AimSlider");
        var slider = sliderObject.AddComponent<Slider>();
        tankShooting.m_AimSlider = slider;

        // AudioSourceを設定
        var audioSource = tankObject.AddComponent<AudioSource>();
        tankShooting.m_ShootingAudio = audioSource;

        // WeaponStockDataを設定（リフレクションを使用）
        shellStockData = CreateWeaponStockData("Shell", 10, 50, 10);
        mineStockData = CreateWeaponStockData("Mine", 0, 3, 1);

        SetPrivateField(tankShooting, "m_ShellStockData", shellStockData);
        SetPrivateField(tankShooting, "m_MineStockData", mineStockData);
    }

    [TearDown]
    public void TearDown()
    {
        if (tankObject != null)
            Object.DestroyImmediate(tankObject);
    }

    private GameObject tankObject;
    private TankShooting tankShooting;
    private WeaponStockData shellStockData;
    private WeaponStockData mineStockData;

    private WeaponStockData CreateWeaponStockData(string name, int initial, int max, int replenish)
    {
        var stockData = new WeaponStockData();
        var type = typeof(WeaponStockData);

        type.GetField("m_WeaponName", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, name);
        type.GetField("m_InitialQuantity", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, initial);
        type.GetField("m_MaxCapacity", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, max);
        type.GetField("m_ReplenishQuantity", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, replenish);

        return stockData;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    [Test]
    public void CurrentShells_ReturnsCorrectValue()
    {
        // Arrange
        shellStockData.InitializeQuantity();

        // Assert
        Assert.AreEqual(10, tankShooting.CurrentShells);
    }

    [Test]
    public void MaxShells_ReturnsCorrectValue()
    {
        // Assert
        Assert.AreEqual(50, tankShooting.MaxShells);
    }

    [Test]
    public void CurrentMines_ReturnsCorrectValue()
    {
        // Arrange
        mineStockData.InitializeQuantity();

        // Assert
        Assert.AreEqual(0, tankShooting.CurrentMines);
    }

    [Test]
    public void MaxMines_ReturnsCorrectValue()
    {
        // Assert
        Assert.AreEqual(3, tankShooting.MaxMines);
    }

    [Test]
    public void AddShells_IncreasesShellCount()
    {
        // Arrange
        shellStockData.InitializeQuantity();
        var initialShells = tankShooting.CurrentShells;

        // Act
        tankShooting.AddShells();

        // Assert
        Assert.AreEqual(initialShells + 10, tankShooting.CurrentShells);
    }

    [Test]
    public void AddShells_DoesNotExceedMax()
    {
        // Arrange
        // Set initial to max
        var fullStockData = CreateWeaponStockData("Shell", 50, 50, 10);
        SetPrivateField(tankShooting, "m_ShellStockData", fullStockData);
        fullStockData.InitializeQuantity();

        // Act
        tankShooting.AddShells();

        // Assert
        Assert.AreEqual(50, tankShooting.CurrentShells);
    }

    [Test]
    public void AddMines_IncreasesMineCount()
    {
        // Arrange
        mineStockData.InitializeQuantity();

        // Act
        tankShooting.AddMines();

        // Assert
        Assert.AreEqual(1, tankShooting.CurrentMines);
    }

    [Test]
    public void AddMines_DoesNotExceedMax()
    {
        // Arrange
        var fullMineData = CreateWeaponStockData("Mine", 3, 3, 1);
        SetPrivateField(tankShooting, "m_MineStockData", fullMineData);
        fullMineData.InitializeQuantity();

        // Act
        tankShooting.AddMines();

        // Assert
        Assert.AreEqual(3, tankShooting.CurrentMines);
    }

    [UnityTest]
    public IEnumerator OnWeaponStockChanged_FiresWhenShellsAdded()
    {
        // Arrange
        shellStockData.InitializeQuantity();
        WeaponStockData receivedData = null;
        tankShooting.OnWeaponStockChanged += data => receivedData = data;

        yield return null; // Wait a frame for OnEnable

        // Act
        tankShooting.AddShells();

        // Assert
        Assert.IsNotNull(receivedData);
        Assert.AreEqual("Shell", receivedData.WeaponName);
    }

    [UnityTest]
    public IEnumerator OnWeaponStockChanged_FiresWhenMinesAdded()
    {
        // Arrange
        mineStockData.InitializeQuantity();
        WeaponStockData receivedData = null;
        tankShooting.OnWeaponStockChanged += data => receivedData = data;

        yield return null;

        // Act
        tankShooting.AddMines();

        // Assert
        Assert.IsNotNull(receivedData);
        Assert.AreEqual("Mine", receivedData.WeaponName);
    }

    [Test]
    public void GetMinePrefab_ReturnsNull_WhenNotSet()
    {
        // Assert
        Assert.IsNull(tankShooting.GetMinePrefab());
    }

    [Test]
    public void GetMinePrefab_ReturnsPrefab_WhenSet()
    {
        // Arrange
        var minePrefab = new GameObject("MinePrefab");
        SetPrivateField(tankShooting, "m_Mine", minePrefab);

        // Act
        var result = tankShooting.GetMinePrefab();

        // Assert
        Assert.AreEqual(minePrefab, result);

        // Cleanup
        Object.DestroyImmediate(minePrefab);
    }

    [Test]
    public void CurrentChargeRatio_IsZero_Initially()
    {
        // Assert
        Assert.AreEqual(0f, tankShooting.CurrentChargeRatio);
    }

    [Test]
    public void IsCharging_IsFalse_Initially()
    {
        // Assert
        Assert.IsFalse(tankShooting.IsCharging);
    }
}
