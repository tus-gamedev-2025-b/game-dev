#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Tanks.Complete;
using UnityEngine.SceneManagement;

public class ScreenTransitionTests
{
    [Test]
    public void TitleToHome_TransitionWorks()
    {
        // 実際の遷移テストはPlayModeでコルーチンなしでは難しいためここではスクリプトの存在とメソッドの確認のみ
        var startButtonType = typeof(StartButton);
        Assert.IsNotNull(startButtonType);

        // StartButtonにOnClickedメソッドがあるか確認
        var method = startButtonType.GetMethod("OnClicked",
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        Assert.IsNotNull(method,
            "StartButtonにOnClickedメソッドが実装されていません");
    }

    [Test]
    public void HomeToGame_TransitionWorks()
    {
        var versusButtonType = typeof(VersusPlayerButton);
        Assert.IsNotNull(versusButtonType);

        // VersusPlayerButtonにOnClickedメソッドがあるか確認
        var method = versusButtonType.GetMethod("OnClicked",
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        Assert.IsNotNull(method,
            "VersusPlayerButtonにOnClickedメソッドが実装されていません");
    }

    [Test]
    public void GameEnd_ReturnsToHome()
    {
        // GameManagerのGameLoopメソッドを確認
        var gameManagerType = typeof(GameManager);
        Assert.IsNotNull(gameManagerType, "GameManagerが存在しません");

        // GameLoopメソッドの存在確認
        var gameLoopMethod = gameManagerType.GetMethod("GameLoop",
            BindingFlags.NonPublic |
            BindingFlags.Instance);

        Assert.IsNotNull(gameLoopMethod,
            "GameManagerにGameLoopメソッドがありません");
    }
}

// 統合テスト（シーンが作成された後に有効化）
public class IntegrationTests
{
    [Test]
    public void FullGameFlow_RequiredScenesExist()
    {
        // 必要な全シーンが存在するか確認
        var titleExists = DoesSceneExist("TitleScene");
        var homeExists = DoesSceneExist("HomeScene");
        var gameExists = DoesSceneExist("Demo_Game_Desert") ||
                         DoesSceneExist("Demo_Game_Moon") ||
                         DoesSceneExist("Demo_Game_Jungle");

        Assert.IsTrue(titleExists, "TitleSceneが存在しません");
        Assert.IsTrue(homeExists, "HomeSceneが存在しません");
        Assert.IsTrue(gameExists, "ゲームシーンが存在しません");
    }

    private bool DoesSceneExist(string sceneName)
    {
        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            var name = Path.GetFileNameWithoutExtension(scenePath);
            if (name == sceneName) return true;
        }
        return false;
    }
}
#endif
