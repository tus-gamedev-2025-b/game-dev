using UnityEngine;
using UnityEngine.UI;

namespace Tanks.UI
{
    public class PlayerWinCount : MonoBehaviour
    {
        [SerializeField] private Image[] WinImages;

        // 勝利数（0〜5）に応じて Win1〜Win5 の Image を点灯/消灯する
        public void UpdateWinCount(int winCount)
        {
            // 安全対策：範囲外の値はクランプ
            winCount = Mathf.Clamp(winCount, 0, WinImages.Length);

            for (int i = 0; i < WinImages.Length; i++)
            {
                // 勝利数以下の index のみ点灯
                WinImages[i].enabled = (i < winCount);
            }
        }
    }
}