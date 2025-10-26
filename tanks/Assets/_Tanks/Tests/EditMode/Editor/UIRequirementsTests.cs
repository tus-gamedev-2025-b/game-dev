using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIRequirementsTests
{
    private const string SCENE_PATH = "Assets/_Tanks/Tutorial_Demo/Demo_Scenes/";

    [Test]
    public void TitleScene_HasTitleLogo()
    {
        // タイトル画面を開く
        EditorSceneManager.OpenScene(SCENE_PATH + "TitleScene.unity");

        // タイトルロゴの存在確認
        var titleLogo = GameObject.Find("TitleLogo");
        Assert.IsNotNull(titleLogo, "タイトルロゴが配置されていません");

        // テキストコンポーネントの確認
        var textComponent = titleLogo.GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(textComponent, "TitleLogoにTextMeshProコンポーネントがありません");

        // ロゴテキストが空でないことを確認
        Assert.IsFalse(string.IsNullOrEmpty(textComponent.text),
            "タイトルロゴのテキストが設定されていません");
    }

    [Test]
    public void TitleScene_HasTapToStartButton()
    {
        EditorSceneManager.OpenScene(SCENE_PATH + "TitleScene.unity");

        // ボタンの存在確認
        var startButton = GameObject.Find("StartButton");
        Assert.IsNotNull(startButton,
            "「Tap to Start」ボタンが配置されていません");

        // Buttonコンポーネントの確認
        var buttonComponent = startButton.GetComponent<Button>();
        Assert.IsNotNull(buttonComponent, "StartButtonにButtonコンポーネントがありません");

        // ボタンテキストの確認
        var textChild = startButton.GetComponentInChildren<TextMeshProUGUI>();
        Assert.IsNotNull(textChild, "ボタンにテキストがありません");
        Assert.AreEqual("Tap to Start", textChild.text,
            "ボタンテキストが「Tap to Start」ではありません");

        // StartButtonスクリプトがアタッチされているか
        var startButtonScript = startButton.GetComponent<StartButton>();
        Assert.IsNotNull(startButtonScript,
            "StartButtonスクリプトがアタッチされていません");
    }

    [Test]
    public void TitleScene_ButtonHasTransitionEvent()
    {
        EditorSceneManager.OpenScene(SCENE_PATH + "TitleScene.unity");

        var startButton = GameObject.Find("StartButton");
        Assert.IsNotNull(startButton);

        var buttonComponent = startButton.GetComponent<Button>();

        // OnClickイベントが設定されているか確認
        Assert.IsTrue(buttonComponent.onClick.GetPersistentEventCount() > 0 ||
                      startButton.GetComponent<StartButton>() != null,
            "ボタンに画面遷移イベントが設定されていません");
    }

    [Test]
    public void HomeScene_HasVersusPlayerButton()
    {
        EditorSceneManager.OpenScene(SCENE_PATH + "HomeScene.unity");

        // ボタンの存在確認
        var versusButton = GameObject.Find("VersusPlayerButton");
        Assert.IsNotNull(versusButton,
            "「Versus Player」ボタンが配置されていません");

        // Buttonコンポーネントの確認
        var buttonComponent = versusButton.GetComponent<Button>();
        Assert.IsNotNull(buttonComponent,
            "VersusPlayerButtonにButtonコンポーネントがありません");

        // ボタンテキストの確認
        var textChild = versusButton.GetComponentInChildren<TextMeshProUGUI>();
        Assert.IsNotNull(textChild, "ボタンにテキストがありません");
        Assert.AreEqual("Versus Player", textChild.text,
            "ボタンテキストが「Versus Player」ではありません");
    }

    [Test]
    public void HomeScene_ButtonHasGameTransition()
    {
        EditorSceneManager.OpenScene(SCENE_PATH + "HomeScene.unity");

        var versusButton = GameObject.Find("VersusPlayerButton");
        Assert.IsNotNull(versusButton);

        // VersusPlayerButtonスクリプトがアタッチされているか
        var versusButtonScript = versusButton.GetComponent<VersusPlayerButton>();
        Assert.IsNotNull(versusButtonScript,
            "VersusPlayerButtonスクリプトがアタッチされていません");
    }

    [Test]
    public void BuildSettings_HasCorrectSceneOrder()
    {
        var scenes = EditorBuildSettings.scenes;

        Assert.IsTrue(scenes.Length >= 3,
            "Build Settingsに必要な3つのシーンが登録されていません");

        // シーン順序の確認
        var firstScene = System.IO.Path.GetFileNameWithoutExtension(scenes[0].path);
        var secondScene = System.IO.Path.GetFileNameWithoutExtension(scenes[1].path);

        Assert.AreEqual("TitleScene", firstScene,
            "最初のシーンがTitleSceneではありません");
        Assert.AreEqual("HomeScene", secondScene,
            "2番目のシーンがHomeSceneではありません");
    }
}
