using UnityEditor;
using UnityEngine;

public class ClearPlayerPrefs
{
    // 这会在你 Unity 顶部的菜单栏多出一个菜单项叫 "Tools"
    [MenuItem("Tools/Clear All Save Data (清理所有存档)")]
    public static void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("所有存档已成功清除！");
    }
}