using System.Collections.Generic;
using UnityEngine;

public class PlayerStock : MonoBehaviour
{
    private readonly List<GameObject> shellImages = new List<GameObject>();

    void Start()
    {
        // TODO: Since the maximum shell capacity may different by tank model,
        //       we need to dynamically create and manage shell images.
        var stockGroup = transform.GetChild(0);
        for (var i = 0; i < stockGroup.childCount; i++)
        {
            shellImages.Add(stockGroup.GetChild(i).gameObject);
        }
    }

    public void UpdatePlayerStock(int stock)
    {
        if (stock < 0 || stock >= shellImages.Count)
            return;

        for (var i = 0; i < shellImages.Count; i++)
        {
            shellImages[i].SetActive(i < stock);
        }
    }
}
