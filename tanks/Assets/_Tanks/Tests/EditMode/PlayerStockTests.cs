using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerStockTests
{
    private GameObject m_ContainerObject;
    private PlayerStock m_PlayerStock;
    private GameObject m_ShellImagePrefab;

    [SetUp]
    public void SetUp()
    {
        // Create a container GameObject with PlayerStock component
        m_ContainerObject = new GameObject("PlayerStockContainer");
        m_PlayerStock = m_ContainerObject.AddComponent<PlayerStock>();

        // Create a child container for shell images
        var shellImagesContainer = new GameObject("ShellImagesContainer");
        shellImagesContainer.transform.SetParent(m_ContainerObject.transform);

        // Create a simple shell image prefab
        m_ShellImagePrefab = new GameObject("ShellImage");

        // Use reflection to set the private field m_ShellImagePrefab
        var field = typeof(PlayerStock).GetField("m_ShellImagePrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(m_PlayerStock, m_ShellImagePrefab);

        // Call Start to initialize
        m_PlayerStock.Start();
    }

    [TearDown]
    public void TearDown()
    {
        if (m_ContainerObject != null)
        {
            Object.DestroyImmediate(m_ContainerObject);
        }
        if (m_ShellImagePrefab != null)
        {
            Object.DestroyImmediate(m_ShellImagePrefab);
        }
    }

    [Test]
    public void InitPlayerStock_CreatesCorrectNumberOfShellImages()
    {
        // Arrange
        const int maxStock = 5;
        const int currentStock = 3;

        // Act
        m_PlayerStock.InitPlayerStock(maxStock, currentStock);

        // Assert
        var shellImagesContainer = m_ContainerObject.transform.GetChild(0);
        Assert.AreEqual(maxStock, shellImagesContainer.childCount,
            "Should create shell images equal to maxStock");
    }

    [Test]
    public void InitPlayerStock_ActivatesCorrectNumberOfShells()
    {
        // Arrange
        const int maxStock = 5;
        const int currentStock = 3;

        // Act
        m_PlayerStock.InitPlayerStock(maxStock, currentStock);

        // Assert
        var shellImagesContainer = m_ContainerObject.transform.GetChild(0);
        var activeCount = 0;
        for (var i = 0; i < shellImagesContainer.childCount; i++)
        {
            if (shellImagesContainer.GetChild(i).gameObject.activeSelf)
            {
                activeCount++;
            }
        }
        Assert.AreEqual(currentStock, activeCount,
            "Should activate shell images equal to currentStock");
    }

    [Test]
    public void UpdatePlayerStock_UpdatesActiveShellCount()
    {
        // Arrange
        const int maxStock = 5;
        const int initialStock = 5;
        const int updatedStock = 2;
        m_PlayerStock.InitPlayerStock(maxStock, initialStock);

        // Act
        m_PlayerStock.UpdatePlayerStock(updatedStock);

        // Assert
        var shellImagesContainer = m_ContainerObject.transform.GetChild(0);
        var activeCount = 0;
        for (var i = 0; i < shellImagesContainer.childCount; i++)
        {
            if (shellImagesContainer.GetChild(i).gameObject.activeSelf)
            {
                activeCount++;
            }
        }
        Assert.AreEqual(updatedStock, activeCount,
            "Should update active shell count to updatedStock");
    }

    [Test]
    public void UpdatePlayerStock_ActivatesCorrectShells()
    {
        // Arrange
        const int maxStock = 5;
        const int initialStock = 0;
        const int updatedStock = 3;
        m_PlayerStock.InitPlayerStock(maxStock, initialStock);

        // Act
        m_PlayerStock.UpdatePlayerStock(updatedStock);

        // Assert
        var shellImagesContainer = m_ContainerObject.transform.GetChild(0);
        for (var i = 0; i < shellImagesContainer.childCount; i++)
        {
            var shouldBeActive = i < updatedStock;
            Assert.AreEqual(shouldBeActive, shellImagesContainer.GetChild(i).gameObject.activeSelf,
                $"Shell image {i} active state should be {shouldBeActive}");
        }
    }

    [Test]
    public void InitPlayerStock_CalledTwice_UpdatesInsteadOfRecreating()
    {
        // Arrange
        const int maxStock = 5;
        const int initialStock = 3;
        const int updatedStock = 2;

        // Act
        m_PlayerStock.InitPlayerStock(maxStock, initialStock);
        var shellImagesContainer = m_ContainerObject.transform.GetChild(0);
        var initialChildCount = shellImagesContainer.childCount;

        m_PlayerStock.InitPlayerStock(maxStock, updatedStock);

        // Assert
        Assert.AreEqual(initialChildCount, shellImagesContainer.childCount,
            "Should not create new shell images when already initialized");

        var activeCount = 0;
        for (var i = 0; i < shellImagesContainer.childCount; i++)
        {
            if (shellImagesContainer.GetChild(i).gameObject.activeSelf)
            {
                activeCount++;
            }
        }
        Assert.AreEqual(updatedStock, activeCount,
            "Should update stock count instead of reinitializing");
    }
}
