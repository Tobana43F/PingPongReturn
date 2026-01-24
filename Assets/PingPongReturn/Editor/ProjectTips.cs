using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using log4net.Config;

public class ProjectTips : EditorWindow
{
    [MenuItem("Project/Tips")]
    static void Init()
    {
        ProjectTips window = (ProjectTips)EditorWindow.GetWindow(typeof(ProjectTips));
        window.Show();
    }

    private void OnGUI()
    {
        string str = "Domain Reload を無効にしている。\n" +
            "開発速度向上のため。\n" +
            "参考：https://docs.unity3d.com/2022.3/Documentation/Manual/DomainReloading.html\n";
        GUILayout.Label(str);
    }
}
