using UnityEditor;
using UnityEngine;
using Riverbend;
public static class P0ContinuationChecks {
    [MenuItem("Tools/Riverbend/P0 Continuation/Review Cargo")] public static void RCargo(){Object.FindFirstObjectByType<DemoGame>().ReviewCargo();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review Fruit Source")] public static void RFruit(){Object.FindFirstObjectByType<DemoGame>().ReviewFruitSource();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify All Serially")] public static void VA(){Object.FindFirstObjectByType<DemoGame>().RunAllContinuationChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Captures")] public static void VC(){Object.FindFirstObjectByType<DemoGame>().RunCaptureChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Saved States")] public static void VS(){Object.FindFirstObjectByType<DemoGame>().RunStateChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Monitoring")] public static void VM(){Object.FindFirstObjectByType<DemoGame>().RunMonitoringChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review Monitoring")] public static void RM(){Object.FindFirstObjectByType<DemoGame>().ReviewMonitoring();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review Guide")] public static void RGuide(){Object.FindFirstObjectByType<DemoGame>().ReviewGuide();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review Commands")] public static void RCommands(){Object.FindFirstObjectByType<DemoGame>().ReviewCommands();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review Night L03")] public static void RN(){Object.FindFirstObjectByType<DemoGame>().ReviewMap(2,true);}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Input")] public static void VI(){Object.FindFirstObjectByType<DemoGame>().RunInputChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Behavior")] public static void VB(){Object.FindFirstObjectByType<DemoGame>().RunBehaviorChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review L01")] public static void R1(){Object.FindFirstObjectByType<DemoGame>().ReviewMap(0);}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review L02")] public static void R2(){Object.FindFirstObjectByType<DemoGame>().ReviewMap(1);}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review L03")] public static void R3(){Object.FindFirstObjectByType<DemoGame>().ReviewMap(2);}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review L04")] public static void R4(){Object.FindFirstObjectByType<DemoGame>().ReviewMap(3);}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review L05")] public static void R5(){Object.FindFirstObjectByType<DemoGame>().ReviewMap(4);}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review L06")] public static void R6(){Object.FindFirstObjectByType<DemoGame>().ReviewMap(5);}
    [MenuItem("Tools/Riverbend/P0 Continuation/Review Growth")] public static void RG(){Object.FindFirstObjectByType<DemoGame>().ReviewGrowth();}
    [MenuItem("Tools/Riverbend/P0 Continuation/End Review")] public static void RE(){Object.FindFirstObjectByType<DemoGame>().EndReview();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Equipment")]
    public static void VerifyEquipment(){if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first");Object.FindFirstObjectByType<DemoGame>().RunEquipmentChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Content")]
    public static void VerifyContent(){if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first");Object.FindFirstObjectByType<DemoGame>().RunContentChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Continuation")]
    public static void VerifyContinuation(){if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first");Object.FindFirstObjectByType<DemoGame>().RunContinuationChecks();}
    [MenuItem("Tools/Riverbend/P0 Continuation/Verify Accepted V4")]
    public static void Verify(){if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode first");Object.FindFirstObjectByType<DemoGame>().RunP0ReuseChecks();}
}
