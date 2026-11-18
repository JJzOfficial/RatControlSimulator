using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class P0ContinuationBuild {
    [MenuItem("Tools/Riverbend/P0 Continuation/Build Internal Verification Player")]
    public static void BuildVerification(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop play mode before building");
        string root=Path.GetFullPath("../Builds/InternalVerification-P0Continuation/Development");Directory.CreateDirectory(root);string status=Path.Combine(root,"build-status.txt");File.WriteAllText(status,"Building internal verification player; not a release");
        try{var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/RiverbendDemo.unity"},locationPathName=Path.Combine(root,"RiverbendVerification.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(report==null)throw new InvalidOperationException("BuildPipeline returned no report; inspect Editor.log");File.WriteAllText(status,report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nSize: "+report.summary.totalSize);if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Internal build failed; inspect the build report");}
        catch(Exception e){File.AppendAllText(status,"\n"+e);throw;}
    }
}
