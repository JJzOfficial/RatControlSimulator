using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Riverbend;

public static class DemoBuild
{
    const string ScenePath="Assets/Game/Scenes/RiverbendDemo.unity";
    [MenuItem("Tools/Riverbend/Create Playable Demo Scene")]
    public static void CreateScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first");
        Directory.CreateDirectory("Assets/Game/Resources");
        // Explicit shader asset reference prevents runtime procedural materials being stripped in players.
        if(!File.Exists("Assets/Game/Resources/DemoShader.mat")) {
            var m=new Material(Shader.Find("HDRP/Lit"));AssetDatabase.CreateAsset(m,"Assets/Game/Resources/DemoShader.mat");
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("RiverbendDemo").AddComponent<DemoGame>();
        EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        PlayerSettings.productName="Riverbend Rat Control Demo";PlayerSettings.companyName="Riverbend";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.runInBackground=true;PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();Debug.Log("[Riverbend] Demo scene saved; build settings configured.");
    }
    [MenuItem("Tools/Riverbend/Build Windows Playable Demo")]
    public static void BuildWindows()
    { BuildAt(false); }
    [MenuItem("Tools/Riverbend/Build Release Demo")]
    public static void BuildRelease()
    { BuildAt(true); }
    static void BuildAt(bool release)
    {
        {
            string root=Path.GetFullPath(release?"../Builds/RiverbendDemoV4/Windows":"../Builds/RiverbendDemoV4/Development");Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root,"build-status.txt"),"building");
            try {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{ScenePath},locationPathName=Path.Combine(root,"RiverbendDemo.exe"),target=BuildTarget.StandaloneWindows64,options=release?BuildOptions.None:BuildOptions.Development});
                string text=report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nSize: "+report.summary.totalSize+"\nUnity: "+Application.unityVersion;
                File.WriteAllText(Path.Combine(root,"build-status.txt"),text);
                Debug.Log("[Riverbend] "+text);
            }catch(Exception e){File.WriteAllText(Path.Combine(root,"build-status.txt"),"Failed\n"+e);Debug.LogException(e);}
        }
    }
}
