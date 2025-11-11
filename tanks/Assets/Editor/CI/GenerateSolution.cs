#if UNITY_EDITOR
using Unity.CodeEditor;
using UnityEditor;

public static class CI
{
    // GitHub Actions から呼び出すエントリ
    public static void GenerateSolution()
    {
        // .sln / .csproj を再生成
        CodeEditor.CurrentEditor.SyncAll();
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }
}
#endif
