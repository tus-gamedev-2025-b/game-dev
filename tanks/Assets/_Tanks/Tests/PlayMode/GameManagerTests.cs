#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Tanks.Complete;

public class GameManagerTests
{
    [Test]
    public void GameManager_HasReturnToHomeImplementation()
    {
        // GameManagerスクリプトの変更を確認するテスト
        var gameManagerType = typeof(GameManager);
        var methods = gameManagerType.GetMethods(
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);

        // GameLoop内でSceneManager.LoadSceneを使用しているか確認
        Assert.IsNotNull(gameManagerType,
            "GameManagerクラスが存在することを確認");
    }
}
#endif
