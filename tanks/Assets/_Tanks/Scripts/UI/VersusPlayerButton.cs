using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VersusPlayerButton : MonoBehaviour
{
    [SerializeField] private Button versusPlayerButton;

    private void Start()
    {
        versusPlayerButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        SceneManager.LoadScene(SceneNames.m_Demo_Game_Desert);
    }
}
