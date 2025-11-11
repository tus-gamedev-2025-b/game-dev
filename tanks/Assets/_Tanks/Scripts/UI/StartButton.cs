using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Start()
    {
        startButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        SceneManager.LoadScene(SceneNames.m_HomeScene);
    }
}
