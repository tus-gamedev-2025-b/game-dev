using System.Reflection;
using NUnit.Framework;
using Tanks.Complete;

/// <summary>
///     WeaponStockDataクラスのEditModeテスト
/// </summary>
[TestFixture]
public class WeaponStockDataTests
{
    private WeaponStockData CreateTestStockData(int initial = 10, int max = 50, int replenish = 10)
    {
        // SerializeFieldはリフレクションで設定
        var stockData = new WeaponStockData();
        var type = typeof(WeaponStockData);

        type.GetField("m_WeaponName", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, "TestWeapon");
        type.GetField("m_InitialQuantity", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, initial);
        type.GetField("m_MaxCapacity", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, max);
        type.GetField("m_ReplenishQuantity", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(stockData, replenish);

        return stockData;
    }

    [Test]
    public void InitializeQuantity_SetsCurrentQuantityToInitialValue()
    {
        // Arrange
        var stockData = CreateTestStockData(5);

        // Act
        stockData.InitializeQuantity();

        // Assert
        Assert.AreEqual(5, stockData.CurrentQuantity);
    }

    [Test]
    public void InitializeQuantity_WithZeroInitial_SetsToZero()
    {
        // Arrange
        var stockData = CreateTestStockData(0);

        // Act
        stockData.InitializeQuantity();

        // Assert
        Assert.AreEqual(0, stockData.CurrentQuantity);
    }

    [Test]
    public void InitializeQuantity_CalledMultipleTimes_ResetsToInitial()
    {
        // Arrange
        var stockData = CreateTestStockData(10);
        stockData.InitializeQuantity();
        stockData.Use();
        stockData.Use();

        // Act
        stockData.InitializeQuantity();

        // Assert
        Assert.AreEqual(10, stockData.CurrentQuantity);
    }

    [Test]
    public void Use_DecrementsCurrentQuantity()
    {
        // Arrange
        var stockData = CreateTestStockData(10);
        stockData.InitializeQuantity();

        // Act
        var result = stockData.Use();

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(9, stockData.CurrentQuantity);
    }

    [Test]
    public void Use_WhenQuantityIsZero_ReturnsFalse()
    {
        // Arrange
        var stockData = CreateTestStockData(0);
        stockData.InitializeQuantity();

        // Act
        var result = stockData.Use();

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(0, stockData.CurrentQuantity);
    }

    [Test]
    public void Use_WhenQuantityIsOne_DecrementsToZero()
    {
        // Arrange
        var stockData = CreateTestStockData(1);
        stockData.InitializeQuantity();

        // Act
        var result = stockData.Use();

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0, stockData.CurrentQuantity);
    }

    [Test]
    public void Use_MultipleTimes_DecrementsCorrectly()
    {
        // Arrange
        var stockData = CreateTestStockData(5);
        stockData.InitializeQuantity();

        // Act
        stockData.Use();
        stockData.Use();
        stockData.Use();

        // Assert
        Assert.AreEqual(2, stockData.CurrentQuantity);
    }

    [Test]
    public void Use_NeverGoesNegative()
    {
        // Arrange
        var stockData = CreateTestStockData(2);
        stockData.InitializeQuantity();

        // Act
        stockData.Use();
        stockData.Use();
        stockData.Use(); // This should fail

        // Assert
        Assert.AreEqual(0, stockData.CurrentQuantity);
    }

    [Test]
    public void Replenish_IncreasesCurrentQuantity()
    {
        // Arrange
        var stockData = CreateTestStockData(5, 50, 10);
        stockData.InitializeQuantity();

        // Act
        stockData.Replenish();

        // Assert
        Assert.AreEqual(15, stockData.CurrentQuantity);
    }

    [Test]
    public void Replenish_DoesNotExceedMaxCapacity()
    {
        // Arrange
        var stockData = CreateTestStockData(45, 50, 10);
        stockData.InitializeQuantity();

        // Act
        stockData.Replenish();

        // Assert
        Assert.AreEqual(50, stockData.CurrentQuantity);
    }

    [Test]
    public void Replenish_AtMaxCapacity_StaysAtMax()
    {
        // Arrange
        var stockData = CreateTestStockData(50, 50, 10);
        stockData.InitializeQuantity();

        // Act
        stockData.Replenish();

        // Assert
        Assert.AreEqual(50, stockData.CurrentQuantity);
    }

    [Test]
    public void Replenish_WithAmount_IncreasesCorrectly()
    {
        // Arrange
        var stockData = CreateTestStockData(5, 50);
        stockData.InitializeQuantity();

        // Act
        stockData.Replenish(3);

        // Assert
        Assert.AreEqual(8, stockData.CurrentQuantity);
    }

    [Test]
    public void Replenish_WithAmount_DoesNotExceedMax()
    {
        // Arrange
        var stockData = CreateTestStockData(48, 50);
        stockData.InitializeQuantity();

        // Act
        stockData.Replenish(5);

        // Assert
        Assert.AreEqual(50, stockData.CurrentQuantity);
    }

    [Test]
    public void CanUse_WhenQuantityPositive_ReturnsTrue()
    {
        // Arrange
        var stockData = CreateTestStockData(5);
        stockData.InitializeQuantity();

        // Assert
        Assert.IsTrue(stockData.CanUse);
    }

    [Test]
    public void CanUse_WhenQuantityZero_ReturnsFalse()
    {
        // Arrange
        var stockData = CreateTestStockData(0);
        stockData.InitializeQuantity();

        // Assert
        Assert.IsFalse(stockData.CanUse);
    }

    [Test]
    public void CanUse_AfterUsingAll_ReturnsFalse()
    {
        // Arrange
        var stockData = CreateTestStockData(1);
        stockData.InitializeQuantity();
        stockData.Use();

        // Assert
        Assert.IsFalse(stockData.CanUse);
    }

    [Test]
    public void MaxCapacity_ReturnsCorrectValue()
    {
        // Arrange
        var stockData = CreateTestStockData(max: 100);

        // Assert
        Assert.AreEqual(100, stockData.MaxCapacity);
    }

    [Test]
    public void InitialQuantity_ReturnsCorrectValue()
    {
        // Arrange
        var stockData = CreateTestStockData(25);

        // Assert
        Assert.AreEqual(25, stockData.InitialQuantity);
    }

    [Test]
    public void ReplenishQuantity_ReturnsCorrectValue()
    {
        // Arrange
        var stockData = CreateTestStockData(replenish: 15);

        // Assert
        Assert.AreEqual(15, stockData.ReplenishQuantity);
    }

    [Test]
    public void UseAndReplenish_WorksCorrectly()
    {
        // Arrange
        var stockData = CreateTestStockData(10, 20, 5);
        stockData.InitializeQuantity();

        // Act - Use some
        stockData.Use();
        stockData.Use();
        stockData.Use();
        Assert.AreEqual(7, stockData.CurrentQuantity);

        // Replenish
        stockData.Replenish();
        Assert.AreEqual(12, stockData.CurrentQuantity);

        // Use more
        stockData.Use();
        Assert.AreEqual(11, stockData.CurrentQuantity);
    }

    [Test]
    public void FullCycle_InitUseReplenishReset()
    {
        // Arrange
        var stockData = CreateTestStockData(5, 10, 3);

        // Act & Assert - Initialize
        stockData.InitializeQuantity();
        Assert.AreEqual(5, stockData.CurrentQuantity);

        // Use all
        for (var i = 0; i < 5; i++)
        {
            Assert.IsTrue(stockData.Use());
        }
        Assert.AreEqual(0, stockData.CurrentQuantity);
        Assert.IsFalse(stockData.CanUse);

        // Replenish
        stockData.Replenish();
        Assert.AreEqual(3, stockData.CurrentQuantity);
        Assert.IsTrue(stockData.CanUse);

        // Reset
        stockData.InitializeQuantity();
        Assert.AreEqual(5, stockData.CurrentQuantity);
    }
}
