using System.Collections.Generic;
using UnityEngine;

public class PlayerStock : MonoBehaviour
{
    private readonly List<GameObject> m_ShellImages = new List<GameObject>();

    void Start()
    {
        // TODO: Since the maximum shell capacity may different by tank model,
        //       we need to dynamically create and manage shell images.
        var stockGroup = transform.GetChild(0);
        for (var i = 0; i < stockGroup.childCount; i++)
        {
            m_ShellImages.Add(stockGroup.GetChild(i).gameObject);
        }
    }

    public void UpdatePlayerStock(int stock)
    {
        if (stock < 0 || stock >= m_ShellImages.Count)
            return;

        for (var i = 0; i < m_ShellImages.Count; i++)
        {
            m_ShellImages[i].SetActive(i < stock);
        }
    }
}
