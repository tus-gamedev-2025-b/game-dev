#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;
using Tanks.Complete;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Demo_Game_Desertシーンの設定テスト
/// </summary>
public class DemoDesertSceneTests
{
    private const string SCENE_PATH = "Assets/_Tanks/Tutorial_Demo/Demo_Scenes/";

    private bool OpenGameScene()
    {
        // Demo_Game_Desertシーンのみを開く（Wormhole機能はこのシーンでのみ実装されている）
        var fullPath = SCENE_PATH + "Demo_Game_Desert.unity";
        if (File.Exists(fullPath))
        {
            EditorSceneManager.OpenScene(fullPath);
            return true;
        }

        return false;
    }

    [Test]
    public void HasWormholeManager()
    {
        if (!OpenGameScene())
        {
            Assert.Ignore("Demo_Game_Desertシーンが見つかりません");
            return;
        }

        // WormholeManagerの存在確認
        var manager = Object.FindObjectOfType<WormholeManager>();
        Assert.IsNotNull(manager,
            "Demo_Game_DesertシーンにWormholeManagerが配置されていません");
    }

    [Test]
    public void HasFourWormholeGates()
    {
        if (!OpenGameScene())
        {
            Assert.Ignore("Demo_Game_Desertシーンが見つかりません");
            return;
        }

        // すべてのWormholeGateを検索
        var gates = Object.FindObjectsOfType<WormholeGate>();
        Assert.AreEqual(4, gates.Length,
            $"Demo_Game_Desertシーンに4つのWormholeGateが必要ですが、{gates.Length}個見つかりました");
    }

    [Test]
    public void WormholeManagerHasGatesConfigured()
    {
        if (!OpenGameScene())
        {
            Assert.Ignore("Demo_Game_Desertシーンが見つかりません");
            return;
        }

        var manager = Object.FindObjectOfType<WormholeManager>();
        Assert.IsNotNull(manager);

        // 各方向のゲートが設定されているか確認
        Assert.IsNotNull(manager.m_TopGate,
            "WormholeManagerにTopGateが設定されていません");
        Assert.IsNotNull(manager.m_BottomGate,
            "WormholeManagerにBottomGateが設定されていません");
        Assert.IsNotNull(manager.m_LeftGate,
            "WormholeManagerにLeftGateが設定されていません");
        Assert.IsNotNull(manager.m_RightGate,
            "WormholeManagerにRightGateが設定されていません");
    }

    [Test]
    public void WormholeGatesAreConnected()
    {
        if (!OpenGameScene())
        {
            Assert.Ignore("Demo_Game_Desertシーンが見つかりません");
            return;
        }

        var manager = Object.FindObjectOfType<WormholeManager>();
        Assert.IsNotNull(manager);

        // 上下のゲートが相互接続されているか確認
        if (manager.m_TopGate != null && manager.m_BottomGate != null)
        {
            Assert.AreEqual(manager.m_BottomGate, manager.m_TopGate.m_ConnectedGate,
                "TopGateがBottomGateに接続されていません");
            Assert.AreEqual(manager.m_TopGate, manager.m_BottomGate.m_ConnectedGate,
                "BottomGateがTopGateに接続されていません");
        }

        // 左右のゲートが相互接続されているか確認
        if (manager.m_LeftGate != null && manager.m_RightGate != null)
        {
            Assert.AreEqual(manager.m_RightGate, manager.m_LeftGate.m_ConnectedGate,
                "LeftGateがRightGateに接続されていません");
            Assert.AreEqual(manager.m_LeftGate, manager.m_RightGate.m_ConnectedGate,
                "RightGateがLeftGateに接続されていません");
        }
    }
}
#endif
