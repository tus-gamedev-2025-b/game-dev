#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tanks.Complete;
using UnityEngine;
using UnityEngine.TestTools;

public class WormholeTests
{
    private readonly List<Object> m_ToCleanup = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        if (Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
        {
            Register(new GameObject("AudioListener")).AddComponent<AudioListener>();
        }
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in m_ToCleanup.Where(obj => obj != null))
        {
            Object.DestroyImmediate(obj);
        }

        m_ToCleanup.Clear();
    }

    [UnityTest]
    public IEnumerator WormholeManager_ConnectsOppositeEdges()
    {
        var manager = Register(new GameObject("WormholeManager")).AddComponent<WormholeManager>();

        var topGate = CreateGate("TopGate", Vector3.forward * 20f);
        var bottomGate = CreateGate("BottomGate", Vector3.back * 20f);
        var leftGate = CreateGate("LeftGate", Vector3.left * 20f);
        var rightGate = CreateGate("RightGate", Vector3.right * 20f);

        manager.m_TopGate = topGate;
        manager.m_BottomGate = bottomGate;
        manager.m_LeftGate = leftGate;
        manager.m_RightGate = rightGate;

        yield return null;

        Assert.AreSame(bottomGate, topGate.m_ConnectedGate,
            "Top gate should connect to the opposite (bottom) gate.");
        Assert.AreSame(topGate, bottomGate.m_ConnectedGate,
            "Bottom gate should connect back to the top gate.");
        Assert.AreSame(rightGate, leftGate.m_ConnectedGate,
            "Left gate should connect to the right gate.");
        Assert.AreSame(leftGate, rightGate.m_ConnectedGate,
            "Right gate should connect back to the left gate.");
    }

    [UnityTest]
    public IEnumerator TankPassingGate_TeleportsAndRestrictsActions()
    {
        var entranceGate = CreateGate("EntranceGate", Vector3.zero);
        var exitGate = CreateGate("ExitGate", new Vector3(0f, 0f, 40f));
        entranceGate.m_ConnectedGate = exitGate;
        exitGate.m_ConnectedGate = entranceGate;

        var tank = CreateTank(Vector3.zero);
        var tankRigidbody = tank.GetComponent<Rigidbody>();
        var wormholeState = tank.GetComponent<TankWormholeState>();

        wormholeState.m_EnterDuration = 0.05f;
        wormholeState.m_ExitDuration = 0.05f;
        wormholeState.m_BlinkFrequency = 15f;
        wormholeState.m_TeleportCooldown = 0.1f;

        tank.transform.position = Vector3.back * 5f;
        tankRigidbody.linearVelocity = Vector3.forward * 25f;

        var waitForTrigger = 1f;
        while (!wormholeState.IsTeleporting && waitForTrigger > 0f)
        {
            waitForTrigger -= Time.deltaTime;
            yield return null;
        }

        Assert.IsTrue(wormholeState.IsTeleporting, "Tank should start teleporting after touching the gate.");
        tankRigidbody.linearVelocity = Vector3.zero;

        var tankHealth = tank.GetComponent<TankHealthStub>();
        Assert.IsFalse(wormholeState.CanShoot(), "Teleporting tank must not be able to shoot.");
        Assert.IsFalse(wormholeState.CanPlaceMine(), "Teleporting tank must not be able to place mines.");
        Assert.IsTrue(tankHealth.IsInvincible, "Tank should gain invincibility while blinking.");

        var waitTime = wormholeState.m_EnterDuration + wormholeState.m_ExitDuration + 0.1f;
        yield return new WaitForSeconds(waitTime);

        Assert.IsFalse(wormholeState.IsTeleporting, "Teleportation should finish after the blink durations elapse.");
        Assert.IsTrue(wormholeState.CanShoot(), "Tank should regain the ability to shoot after teleporting.");
        Assert.IsTrue(wormholeState.CanPlaceMine(), "Tank should regain the ability to place mines after teleporting.");
        Assert.IsFalse(tankHealth.IsInvincible, "Tank should lose invincibility after teleportation.");
        Assert.That(Vector3.Distance(tank.transform.position, exitGate.GetExitPosition()), Is.LessThan(0.1f),
            "Tank should exit near the connected gate's exit position.");
    }

    [UnityTest]
    public IEnumerator ShellCannotPassThroughWormhole()
    {
        var gate = CreateGate("Gate", Vector3.zero);
        var exitGate = CreateGate("ExitGate", Vector3.forward * 30f);
        gate.m_ConnectedGate = exitGate;

        var shell = Register(new GameObject("Shell"));
        shell.AddComponent<SphereCollider>();
        var shellRigidbody = shell.AddComponent<Rigidbody>();
        shellRigidbody.useGravity = false;
        shellRigidbody.isKinematic = false;
        ConfigureShellExplosion(shell);

        shell.transform.position = Vector3.back * 5f;
        shellRigidbody.linearVelocity = Vector3.forward * 25f;

        var waitForDestroy = 1f;
        while (shell != null && waitForDestroy > 0f)
        {
            waitForDestroy -= Time.deltaTime;
            yield return null;
        }

        Assert.IsTrue(shell == null,
            "Shells should be destroyed on contact to prevent them from passing through wormholes.");
    }

    private WormholeGate CreateGate(string name, Vector3 position)
    {
        var gateObject = Register(new GameObject(name));
        gateObject.transform.position = position;

        var collider = gateObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(8f, 8f, 2f);

        return gateObject.AddComponent<WormholeGate>();
    }

    private GameObject CreateTank(Vector3 position)
    {
        var tank = Register(new GameObject("Tank"));
        tank.transform.position = position;

        var rigidbody = tank.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = false;

        tank.AddComponent<SphereCollider>();
        tank.AddComponent<SpriteRenderer>();
        tank.AddComponent<TankHealthStub>();
        tank.AddComponent<TankWormholeState>();

        return tank;
    }

    private void ConfigureShellExplosion(GameObject shell)
    {
        var effectsRoot = Register(new GameObject("ShellEffects"));
        var particles = effectsRoot.AddComponent<ParticleSystem>();
        var audio = effectsRoot.AddComponent<AudioSource>();

        var shellExplosion = shell.AddComponent<ShellExplosion>();
        shellExplosion.m_ExplosionParticles = particles;
        shellExplosion.m_ExplosionAudio = audio;
        shellExplosion.m_MaxLifeTime = 0.1f;
    }

    private T Register<T>(T obj) where T : Object
    {
        m_ToCleanup.Add(obj);
        return obj;
    }

    private class TankHealthStub : TankHealth
    {
        private readonly static FieldInfo s_InvincibilityField =
            typeof(TankHealth).GetField("m_IsInvincible", BindingFlags.NonPublic | BindingFlags.Instance);

        public bool IsInvincible =>
            s_InvincibilityField != null && (bool)s_InvincibilityField.GetValue(this);

        protected new void Awake()
        {
        }

        protected new void OnEnable()
        {
        }

        protected new void OnDestroy()
        {
        }
    }
}
#endif
