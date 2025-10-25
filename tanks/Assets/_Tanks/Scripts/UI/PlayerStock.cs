using UnityEngine;

public class PlayerStock : MonoBehaviour
{
    [SerializeField] private GameObject m_ShellImagePrefab;
    private bool initialized;

    private Transform m_ShellImagesContainer;

    public void Start()
    {
        m_ShellImagesContainer = transform.GetChild(0);
        initialized = false;
    }

    public void InitPlayerStock(int maxStock, int stock)
    {
        // If already initialized, just update the stock
        if (initialized)
        {
            UpdatePlayerStock(stock);
            return;
        }

        // Create shell images based on maxStock
        for (var i = 0; i < maxStock; i++)
        {
            var shellImage = Instantiate(m_ShellImagePrefab, m_ShellImagesContainer);
            shellImage.SetActive(i < stock);
        }

        initialized = true;
    }

    public void UpdatePlayerStock(int stock)
    {
        for (var i = 0; i < m_ShellImagesContainer.childCount; i++)
        {
            var shellImage = m_ShellImagesContainer.GetChild(i).gameObject;
            shellImage.SetActive(i < stock);
        }
    }
}
