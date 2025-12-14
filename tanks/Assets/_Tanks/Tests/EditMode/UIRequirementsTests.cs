using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
    public void TitleScene_StartButtonIsPresentAndWired()
    {
        EditorSceneManager.OpenScene(SCENE_PATH + "TitleScene.unity");

        // TitleSceneUIが存在することを確認
        var titleSceneUI = Object.FindObjectOfType<Tanks.Complete.TitleSceneUI>();
        Assert.IsNotNull(titleSceneUI, "TitleSceneUIがシーンに存在しません");

        // StartButtonの存在とButtonコンポーネント確認
        var startButtonGO = GameObject.Find("StartButton");
        Assert.IsNotNull(startButtonGO, "StartButtonオブジェクトがありません");

        var buttonComponent = startButtonGO.GetComponent<Button>();
        Assert.IsNotNull(buttonComponent, "StartButtonにButtonコンポーネントがありません");

        // TitleSceneUIのstartButton参照が設定されているか確認
        var field = typeof(Tanks.Complete.TitleSceneUI)
            .GetField("startButton", BindingFlags.Instance | BindingFlags.NonPublic);
        var wiredButton = field?.GetValue(titleSceneUI) as Button;
        Assert.IsNotNull(wiredButton, "TitleSceneUIのstartButton参照が設定されていません");
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
        var firstScene = Path.GetFileNameWithoutExtension(scenes[0].path);
        var secondScene = Path.GetFileNameWithoutExtension(scenes[1].path);

        Assert.AreEqual("TitleScene", firstScene,
            "最初のシーンがTitleSceneではありません");
        Assert.AreEqual("HomeScene", secondScene,
            "2番目のシーンがHomeSceneではありません");
    }
}
