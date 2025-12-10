using Tanks.Complete;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStock : MonoBehaviour
{
    [Header("Shell Stock")]
    [SerializeField] private GameObject m_ShellImagePrefab;

    [Header("Mine Stock")]
    [Tooltip("地雷アイコンのImage配列（Mine1, Mine2, Mine3）")]
    [SerializeField] private Image[] mineImages;

    [Header("HP")]
    [SerializeField] private Slider HPSlider;

    private bool initialized;
    private Transform m_ShellImagesContainer;

    private void Awake()
    {
        // Awake()で早めに地雷アイコンを非表示にする
        HideMineIcons();
    }

    public void Start()
    {
        m_ShellImagesContainer = transform.GetChild(0);
        initialized = false;

        // 念のためStart()でも非表示にする
        HideMineIcons();
    }

    /// <summary>
    ///     地雷アイコンを全て非表示にする
    /// </summary>
    private void HideMineIcons()
    {
        if (mineImages != null)
        {
            foreach (var mineImage in mineImages)
            {
                if (mineImage != null)
                    mineImage.gameObject.SetActive(false);
            }
        }
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

    /// <summary>
    ///     砲弾の所持数を更新（従来のメソッド）
    /// </summary>
    /// <param name="stock">現在の砲弾数</param>
    public void UpdatePlayerStock(int stock)
    {
        if (m_ShellImagesContainer == null)
            return;

        for (var i = 0; i < m_ShellImagesContainer.childCount; i++)
        {
            var shellImage = m_ShellImagesContainer.GetChild(i).gameObject;
            shellImage.SetActive(i < stock);
        }
    }

    /// <summary>
    ///     WeaponStockDataを使用して武器の所持数を更新
    /// </summary>
    /// <param name="stockData">武器の所持数データ</param>
    public void UpdatePlayerStock(WeaponStockData stockData)
    {
        if (stockData == null)
            return;

        // 武器名で判定して適切なUIを更新
        if (stockData.WeaponName == "Shell" || stockData.WeaponName == "砲弾")
        {
            UpdateShellStock(stockData.CurrentQuantity);
        }
        else if (stockData.WeaponName == "Mine" || stockData.WeaponName == "地雷")
        {
            UpdateMineStock(stockData.CurrentQuantity);
        }
    }

    /// <summary>
    ///     砲弾の所持数UIを更新
    /// </summary>
    /// <param name="stock">現在の砲弾数</param>
    private void UpdateShellStock(int stock)
    {
        if (m_ShellImagesContainer == null)
            return;

        for (var i = 0; i < m_ShellImagesContainer.childCount; i++)
        {
            var shellImage = m_ShellImagesContainer.GetChild(i).gameObject;
            shellImage.SetActive(i < stock);
        }
    }

    /// <summary>
    ///     地雷の所持数UIを更新
    /// </summary>
    /// <param name="stock">現在の地雷数</param>
    public void UpdateMineStock(int stock)
    {
        if (mineImages == null)
            return;

        for (var i = 0; i < mineImages.Length; i++)
        {
            if (mineImages[i] != null)
            {
                mineImages[i].gameObject.SetActive(i < stock);
            }
        }
    }

    public void UpdateHP(float normalizedValue)
    {
        HPSlider.value = normalizedValue;
    }
}
