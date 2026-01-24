using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RefreshOnPlay : MonoBehaviour
{
    [InitializeOnEnterPlayMode]
    static void RunOnStart()
    {
        // ドメインリロード対応
        // https://docs.unity3d.com/2022.3/Documentation/Manual/DomainReloading.html
        EditorApplication.playModeStateChanged -= RefreshAssets;
    }

    [InitializeOnLoadMethod]
    static void Run()
    {
        EditorApplication.playModeStateChanged += RefreshAssets;
    }

    static void RefreshAssets(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorApplication.ExecuteMenuItem("Assets/Refresh");
        }
    }
}
