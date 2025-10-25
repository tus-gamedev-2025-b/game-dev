using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VersusPlayerButton : MonoBehaviour
{
    [SerializeField] private Button versusPlayerButton;

    void Start()
    {
        versusPlayerButton.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        SceneManager.LoadScene(SceneNames.Demo_Game_Moon);
    }
}
