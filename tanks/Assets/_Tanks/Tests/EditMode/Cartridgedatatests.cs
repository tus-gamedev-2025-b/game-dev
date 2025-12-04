using System;
using NUnit.Framework;
using Tanks.Complete;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
///     CartridgeDataクラスのEditModeテスト
/// </summary>
[TestFixture]
public class CartridgeDataTests
{
    [Test]
    public void CartridgeData_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var cartridgeData = new CartridgeData();

        // Assert
        Assert.IsNull(cartridgeData.cartridgePrefab);
        Assert.AreEqual(10f, cartridgeData.spawnInterval);
    }

    [Test]
    public void CartridgeData_CanSetPrefab()
    {
        // Arrange
        var cartridgeData = new CartridgeData();
        var testPrefab = new GameObject("TestPrefab");

        // Act
        cartridgeData.cartridgePrefab = testPrefab;

        // Assert
        Assert.AreEqual(testPrefab, cartridgeData.cartridgePrefab);

        // Cleanup
        Object.DestroyImmediate(testPrefab);
    }

    [Test]
    public void CartridgeData_CanSetSpawnInterval()
    {
        // Arrange
        var cartridgeData = new CartridgeData();

        // Act
        cartridgeData.spawnInterval = 15f;

        // Assert
        Assert.AreEqual(15f, cartridgeData.spawnInterval);
    }

    [Test]
    public void CartridgeData_SpawnInterval_CanBeZero()
    {
        // Arrange
        var cartridgeData = new CartridgeData();

        // Act
        cartridgeData.spawnInterval = 0f;

        // Assert
        Assert.AreEqual(0f, cartridgeData.spawnInterval);
    }

    [Test]
    public void CartridgeData_SpawnInterval_CanBeNegative()
    {
        // Note: This tests that the data class allows negative values
        // Validation should be done elsewhere if needed

        // Arrange
        var cartridgeData = new CartridgeData();

        // Act
        cartridgeData.spawnInterval = -5f;

        // Assert
        Assert.AreEqual(-5f, cartridgeData.spawnInterval);
    }

    [Test]
    public void CartridgeData_IsSerializable()
    {
        // Arrange & Act
        var type = typeof(CartridgeData);
        var hasSerializableAttribute = type.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0;

        // Assert
        Assert.IsTrue(hasSerializableAttribute, "CartridgeData should have Serializable attribute");
    }

    [Test]
    public void CartridgeData_MultipleInstances_AreIndependent()
    {
        // Arrange
        var data1 = new CartridgeData { spawnInterval = 10f };
        var data2 = new CartridgeData { spawnInterval = 20f };

        // Assert
        Assert.AreNotEqual(data1.spawnInterval, data2.spawnInterval);
        Assert.AreEqual(10f, data1.spawnInterval);
        Assert.AreEqual(20f, data2.spawnInterval);
    }
}
