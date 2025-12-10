using UnityEngine;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{
    [SerializeField] private Slider HPSlider;

    public void UpdateHPSlider(float value)
    {
        HPSlider.value = value;
    }
}