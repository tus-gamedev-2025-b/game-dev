using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
///     TextMeshProのDynamicなフォントアセットの差分が毎実行時に出るのをなくす。
///     cf. https://www.stmn.tech/entry/2024/02/14/025205
/// </summary>
[InitializeOnLoad]
public static class DynamicFontCleaner
{
    static DynamicFontCleaner()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode) return;
        var tmpFontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var tmpFontAsset in tmpFontAssets)
        {
            if (tmpFontAsset == null || tmpFontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                continue;
            tmpFontAsset.ClearFontAssetData();
            Debug.Log("DynamicFontCleaner: ClearFontAssetData " + tmpFontAsset.name);
        }
    }
}
