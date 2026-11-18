using UnityEditor;
using UnityEngine;
using UnitySkills;

// Starts the Unity Skills REST server (localhost:8090-8100) as soon as the editor is ready.
// This bypasses the package's opt-in auto-start prefs so automation can connect deterministically.
public static class SkillsServerBootstrap
{
    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                SkillsHttpServer.Start(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[RatControl] Failed to start Unity Skills server: " + ex);
            }
        };
    }
}
