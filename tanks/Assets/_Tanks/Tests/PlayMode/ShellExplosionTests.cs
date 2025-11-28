using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Tanks.Complete;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
///     ShellExplosionクラスのPlayModeテスト
/// </summary>
[TestFixture]
public class ShellExplosionTests
{

    [SetUp]
    public void SetUp()
    {
        // テスト用のシェルオブジェクトを作成
        shellObject = new GameObject("TestShell");

        // Colliderを追加（OnTriggerEnterに必要）
        var collider = shellObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;

        // ShellExplosionを追加
        shellExplosion = shellObject.AddComponent<ShellExplosion>();

        // パーティクルシステムを設定
        var particleObj = new GameObject("ExplosionParticles");
        particleObj.transform.SetParent(shellObject.transform);
        var particleSystem = particleObj.AddComponent<ParticleSystem>();
        shellExplosion.m_ExplosionParticles = particleSystem;

        // AudioSourceを設定
        var audioSource = shellObject.AddComponent<AudioSource>();
        shellExplosion.m_ExplosionAudio = audioSource;

        // デフォルト値を設定
        shellExplosion.m_MaxLifeTime = 2f;
        shellExplosion.m_MaxDamage = 100f;
        shellExplosion.m_ExplosionForce = 50f;
        shellExplosion.m_ExplosionRadius = 5f;
    }

    [TearDown]
    public void TearDown()
    {
        if (shellObject != null)
            Object.DestroyImmediate(shellObject);
    }

    private GameObject shellObject;
    private ShellExplosion shellExplosion;

    [UnityTest]
    public IEnumerator Start_WithoutMineTag_SchedulesDestroy()
    {
        // Arrange - 新しいオブジェクトを作成してm_MaxLifeTimeを先に設定
        var testShellObject = new GameObject("TestShell2");
        var collider = testShellObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;

        var testExplosion = testShellObject.AddComponent<ShellExplosion>();
        testExplosion.m_MaxLifeTime = 0.1f; // Start()が呼ばれる前に設定される

        // パーティクルシステムを設定
        var particleObj = new GameObject("ExplosionParticles2");
        particleObj.transform.SetParent(testShellObject.transform);
        var particleSystem = particleObj.AddComponent<ParticleSystem>();
        testExplosion.m_ExplosionParticles = particleSystem;

        // AudioSourceを設定
        var audioSource = testShellObject.AddComponent<AudioSource>();
        testExplosion.m_ExplosionAudio = audioSource;

        testShellObject.tag = "Untagged";

        // Act - Start()はAddComponentの後に呼ばれる
        yield return null; // 1フレーム待機
        yield return new WaitForSeconds(0.2f);

        // Assert
        Assert.IsTrue(testShellObject == null, "Shell should be destroyed after m_MaxLifeTime");
    }

    [UnityTest]
    public IEnumerator Start_WithMineTag_DoesNotAutoDestroy()
    {
        // Arrange
        // まずMineタグを作成（存在しない場合はUntaggedとして扱われる）
        try
        {
            shellObject.tag = "Mine";
        }
        catch
        {
            // タグが存在しない場合はスキップ
            Assert.Ignore("Mine tag does not exist in project");
            yield break;
        }

        shellExplosion.m_MaxLifeTime = 0.1f;

        // Act
        yield return new WaitForSeconds(0.2f);

        // Assert - Mine tagged objects should still exist
        Assert.IsNotNull(shellObject);
        Assert.IsTrue(shellObject.activeSelf);
    }

    [Test]
    public void CalculateDamage_AtExplosionCenter_ReturnsMaxDamage()
    {
        // Arrange
        shellExplosion.m_MaxDamage = 100f;
        shellExplosion.m_ExplosionRadius = 5f;

        // Act - Use reflection to call private method
        var method = typeof(ShellExplosion).GetMethod("CalculateDamage",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var damage = (float)method.Invoke(shellExplosion, new object[] { shellObject.transform.position });

        // Assert
        Assert.AreEqual(100f, damage);
    }

    [Test]
    public void CalculateDamage_AtEdgeOfRadius_ReturnsZero()
    {
        // Arrange
        shellExplosion.m_MaxDamage = 100f;
        shellExplosion.m_ExplosionRadius = 5f;
        var targetPosition = shellObject.transform.position + Vector3.right * 5f;

        // Act
        var method = typeof(ShellExplosion).GetMethod("CalculateDamage",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var damage = (float)method.Invoke(shellExplosion, new object[] { targetPosition });

        // Assert
        Assert.AreEqual(0f, damage);
    }

    [Test]
    public void CalculateDamage_AtHalfRadius_ReturnsHalfDamage()
    {
        // Arrange
        shellExplosion.m_MaxDamage = 100f;
        shellExplosion.m_ExplosionRadius = 10f;
        var targetPosition = shellObject.transform.position + Vector3.right * 5f;

        // Act
        var method = typeof(ShellExplosion).GetMethod("CalculateDamage",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var damage = (float)method.Invoke(shellExplosion, new object[] { targetPosition });

        // Assert
        Assert.AreEqual(50f, damage, 0.1f);
    }

    [Test]
    public void CalculateDamage_BeyondRadius_ReturnsZero()
    {
        // Arrange
        shellExplosion.m_MaxDamage = 100f;
        shellExplosion.m_ExplosionRadius = 5f;
        var targetPosition = shellObject.transform.position + Vector3.right * 10f;

        // Act
        var method = typeof(ShellExplosion).GetMethod("CalculateDamage",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var damage = (float)method.Invoke(shellExplosion, new object[] { targetPosition });

        // Assert
        Assert.AreEqual(0f, damage);
    }

    [Test]
    public void CalculateDamage_NeverReturnsNegative()
    {
        // Arrange
        shellExplosion.m_MaxDamage = 100f;
        shellExplosion.m_ExplosionRadius = 5f;
        var targetPosition = shellObject.transform.position + Vector3.right * 100f;

        // Act
        var method = typeof(ShellExplosion).GetMethod("CalculateDamage",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var damage = (float)method.Invoke(shellExplosion, new object[] { targetPosition });

        // Assert
        Assert.GreaterOrEqual(damage, 0f);
    }

    [Test]
    public void ExplosionRadius_CanBeSet()
    {
        // Act
        shellExplosion.m_ExplosionRadius = 10f;

        // Assert
        Assert.AreEqual(10f, shellExplosion.m_ExplosionRadius);
    }

    [Test]
    public void MaxDamage_CanBeSet()
    {
        // Act
        shellExplosion.m_MaxDamage = 200f;

        // Assert
        Assert.AreEqual(200f, shellExplosion.m_MaxDamage);
    }

    [Test]
    public void ExplosionForce_CanBeSet()
    {
        // Act
        shellExplosion.m_ExplosionForce = 100f;

        // Assert
        Assert.AreEqual(100f, shellExplosion.m_ExplosionForce);
    }

    [Test]
    public void MaxLifeTime_CanBeSet()
    {
        // Act
        shellExplosion.m_MaxLifeTime = 5f;

        // Assert
        Assert.AreEqual(5f, shellExplosion.m_MaxLifeTime);
    }
}
