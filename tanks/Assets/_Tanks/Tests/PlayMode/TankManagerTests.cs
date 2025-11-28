using System.Reflection;
using NUnit.Framework;
using Tanks.Complete;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     TankManagerクラスのPlayModeテスト
///     GameManagerを使用しないように修正（Menusプレハブのエラーを回避）
/// </summary>
[TestFixture]
public class TankManagerTests
{

    [SetUp]
    public void SetUp()
    {
        // GameManagerは作成しない（Menusプレハブが必要なエラーを回避）

        // スポーンポイントを作成
        spawnPoint = new GameObject("SpawnPoint");

        // タンクインスタンスを作成
        tankInstance = CreateMockTank();

        // TankManagerを作成
        tankManager = new TankManager();

        // リフレクションでフィールドを設定
        SetField("m_SpawnPoint", spawnPoint.transform);
        SetField("m_Instance", tankInstance);
        SetField("m_PlayerNumber", 1);
        SetField("m_PlayerColor", Color.blue);
    }

    [TearDown]
    public void TearDown()
    {
        if (tankInstance != null)
            Object.DestroyImmediate(tankInstance);
        if (spawnPoint != null)
            Object.DestroyImmediate(spawnPoint);
    }

    private TankManager tankManager;
    private GameObject tankInstance;
    private GameObject spawnPoint;

    private GameObject CreateMockTank()
    {
        var tank = new GameObject("Tank");
        tank.AddComponent<Rigidbody>();

        // TankMovementを追加
        var movement = tank.AddComponent<TankMovement>();

        // TankShootingを追加
        var shooting = tank.AddComponent<TankShooting>();

        // TankInputUserを追加
        tank.AddComponent<TankInputUser>();

        // Canvasを追加
        var canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(tank.transform);
        canvasObj.AddComponent<Canvas>();

        // MeshRendererを追加
        var meshObj = new GameObject("Mesh");
        meshObj.transform.SetParent(tank.transform);
        meshObj.AddComponent<MeshRenderer>();

        // Sliderを追加（TankShooting用）
        var sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(tank.transform);
        shooting.m_AimSlider = sliderObj.AddComponent<Slider>();

        // FireTransformを追加
        var fireTransform = new GameObject("FireTransform");
        fireTransform.transform.SetParent(tank.transform);
        shooting.m_FireTransform = fireTransform.transform;

        // AudioSourceを追加
        shooting.m_ShootingAudio = tank.AddComponent<AudioSource>();

        return tank;
    }

    private void SetField(string fieldName, object value)
    {
        var field = typeof(TankManager).GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(tankManager, value);
    }

    private object GetField(string fieldName)
    {
        var field = typeof(TankManager).GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(tankManager);
    }

    [Test]
    public void ControlIndex_DefaultValue_IsOne()
    {
        // Assert
        Assert.AreEqual(1, tankManager.ControlIndex);
    }

    [Test]
    public void ControlIndex_CanBeSet()
    {
        // Act
        tankManager.ControlIndex = 2;

        // Assert
        Assert.AreEqual(2, tankManager.ControlIndex);
    }

    [Test]
    public void DisableControl_DisablesMovement()
    {
        // Arrange
        var movement = tankInstance.GetComponent<TankMovement>();
        var shooting = tankInstance.GetComponent<TankShooting>();
        var canvas = tankInstance.GetComponentInChildren<Canvas>();
        movement.enabled = true;
        SetField("m_Movement", movement);
        SetField("m_Shooting", shooting);
        SetField("m_CanvasGameObject", canvas.gameObject);

        // Act
        tankManager.DisableControl();

        // Assert
        Assert.IsFalse(movement.enabled);
    }

    [Test]
    public void DisableControl_DisablesShooting()
    {
        // Arrange
        var movement = tankInstance.GetComponent<TankMovement>();
        var shooting = tankInstance.GetComponent<TankShooting>();
        var canvas = tankInstance.GetComponentInChildren<Canvas>();
        shooting.enabled = true;
        SetField("m_Movement", movement);
        SetField("m_Shooting", shooting);
        SetField("m_CanvasGameObject", canvas.gameObject);

        // Act
        tankManager.DisableControl();

        // Assert
        Assert.IsFalse(shooting.enabled);
    }

    [Test]
    public void DisableControl_HidesCanvas()
    {
        // Arrange
        var movement = tankInstance.GetComponent<TankMovement>();
        var shooting = tankInstance.GetComponent<TankShooting>();
        var canvas = tankInstance.GetComponentInChildren<Canvas>();
        canvas.gameObject.SetActive(true);

        SetField("m_Movement", movement);
        SetField("m_Shooting", shooting);
        SetField("m_CanvasGameObject", canvas.gameObject);

        // Act
        tankManager.DisableControl();

        // Assert
        Assert.IsFalse(canvas.gameObject.activeSelf);
    }

    [Test]
    public void EnableControl_EnablesMovement()
    {
        // Arrange
        var movement = tankInstance.GetComponent<TankMovement>();
        var shooting = tankInstance.GetComponent<TankShooting>();
        var canvas = tankInstance.GetComponentInChildren<Canvas>();
        movement.enabled = false;
        SetField("m_Movement", movement);
        SetField("m_Shooting", shooting);
        SetField("m_CanvasGameObject", canvas.gameObject);

        // Act
        tankManager.EnableControl();

        // Assert
        Assert.IsTrue(movement.enabled);
    }

    [Test]
    public void EnableControl_EnablesShooting()
    {
        // Arrange
        var movement = tankInstance.GetComponent<TankMovement>();
        var shooting = tankInstance.GetComponent<TankShooting>();
        var canvas = tankInstance.GetComponentInChildren<Canvas>();
        shooting.enabled = false;
        SetField("m_Movement", movement);
        SetField("m_Shooting", shooting);
        SetField("m_CanvasGameObject", canvas.gameObject);

        // Act
        tankManager.EnableControl();

        // Assert
        Assert.IsTrue(shooting.enabled);
    }

    [Test]
    public void EnableControl_ShowsCanvas()
    {
        // Arrange
        var movement = tankInstance.GetComponent<TankMovement>();
        var shooting = tankInstance.GetComponent<TankShooting>();
        var canvas = tankInstance.GetComponentInChildren<Canvas>();
        canvas.gameObject.SetActive(false);

        SetField("m_Movement", movement);
        SetField("m_Shooting", shooting);
        SetField("m_CanvasGameObject", canvas.gameObject);

        // Act
        tankManager.EnableControl();

        // Assert
        Assert.IsTrue(canvas.gameObject.activeSelf);
    }

    [Test]
    public void Reset_MovesToSpawnPosition()
    {
        // Arrange
        spawnPoint.transform.position = new Vector3(10f, 0f, 10f);
        tankInstance.transform.position = Vector3.zero;

        // Act
        tankManager.Reset();

        // Assert
        Assert.AreEqual(spawnPoint.transform.position, tankInstance.transform.position);
    }

    [Test]
    public void Reset_SetsSpawnRotation()
    {
        // Arrange
        spawnPoint.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        tankInstance.transform.rotation = Quaternion.identity;

        // Act
        tankManager.Reset();

        // Assert
        Assert.AreEqual(spawnPoint.transform.rotation.eulerAngles,
            tankInstance.transform.rotation.eulerAngles);
    }

    [Test]
    public void Reset_ReactivatesTank()
    {
        // Arrange
        tankInstance.SetActive(false);

        // Act
        tankManager.Reset();

        // Assert
        Assert.IsTrue(tankInstance.activeSelf);
    }

    [Test]
    public void OnWeaponStockChanged_ReceivesEvent()
    {
        // Arrange
        var receivedControlIndex = -1;
        WeaponStockData receivedData = null;

        tankManager.OnWeaponStockChanged += (index, data) =>
        {
            receivedControlIndex = index;
            receivedData = data;
        };

        tankManager.ControlIndex = 1;

        // 内部のイベントハンドラを呼び出す
        var method = typeof(TankManager).GetMethod("HandleWeaponStockChanged",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var testData = new WeaponStockData();
        method?.Invoke(tankManager, new object[] { testData });

        // Assert
        Assert.AreEqual(1, receivedControlIndex);
        Assert.IsNotNull(receivedData);
    }

    [Test]
    public void ShellStock_ReturnsCorrectValue()
    {
        // Arrange
        var shooting = tankInstance.GetComponent<TankShooting>();
        var stockData = CreateWeaponStockData("Shell", 15, 50, 10);

        var field = typeof(TankShooting).GetField("m_ShellStockData",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(shooting, stockData);
        stockData.InitializeQuantity();

        SetField("m_Shooting", shooting);

        // Assert
        Assert.AreEqual(15, tankManager.ShellStock);
    }

    [Test]
    public void MaxShellStock_ReturnsCorrectValue()
    {
        // Arrange
        var shooting = tankInstance.GetComponent<TankShooting>();
        var stockData = CreateWeaponStockData("Shell", 10, 100, 10);

        var field = typeof(TankShooting).GetField("m_ShellStockData",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(shooting, stockData);

        SetField("m_Shooting", shooting);

        // Assert
        Assert.AreEqual(100, tankManager.MaxShellStock);
    }

    [Test]
    public void MineStock_ReturnsCorrectValue()
    {
        // Arrange
        var shooting = tankInstance.GetComponent<TankShooting>();
        var stockData = CreateWeaponStockData("Mine", 2, 3, 1);

        var field = typeof(TankShooting).GetField("m_MineStockData",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(shooting, stockData);
        stockData.InitializeQuantity();

        SetField("m_Shooting", shooting);

        // Assert
        Assert.AreEqual(2, tankManager.MineStock);
    }

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
}
