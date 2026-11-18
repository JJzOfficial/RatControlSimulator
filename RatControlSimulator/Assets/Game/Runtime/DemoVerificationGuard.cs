#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        bool verificationRunning;
        public void RunAllContinuationChecks(){StartVerification(AllContinuationChecks(),"all");}
        IEnumerator AllContinuationChecks(){var log=new System.Collections.Generic.List<string>();var routines=new[]{EquipmentChecks(),BehaviorChecks(),InputChecks(),ContinuationChecks(),ContentChecks(),CaptureChecks(),StateChecks(),MonitoringChecks(),P0ReuseChecks()};string[] names={"Equipment and source work","Behavior","Real input","Maps and gestures","Contract boundaries","Actual cage captures","Interrupted state persistence","Actual monitoring and drone frames","Unmodified accepted V4"};for(int i=0;i<routines.Length;i++){CheckResult="Running";yield return routines[i];log.Add(names[i]+": "+CheckResult);if(CheckResult!="Passed")break;yield return null;}File.WriteAllLines(Path.GetFullPath("Library/P0VerificationGuard/all-results.txt"),log);}
        void StartVerification(IEnumerator routine,string name){if(verificationRunning||reviewOriginal!=null){Debug.LogWarning("Finish the current verification or End Review before starting another check.");return;}StartCoroutine(IsolatedVerification(routine,name));}
        IEnumerator IsolatedVerification(IEnumerator routine,string name){
            verificationRunning=true;string originalPath=DemoSave.testPath,originalFallback=DemoSave.verificationFallback;var originalSave=save;bool originalFrontend=frontend;int originalSelected=selected;string directory=Path.GetFullPath("Library/P0VerificationGuard/"+name);Directory.CreateDirectory(directory);DemoSave.testPath=DemoSave.verificationFallback=Path.Combine(directory,"fallback-profile.json");
            var errors=new System.Collections.Generic.List<string>();Application.LogCallback capture=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);};Application.logMessageReceived+=capture;
            try{yield return routine;if(errors.Count>0){CheckResult="Failed: unexpected runtime errors ("+errors.Count+")";if(name=="all")File.AppendAllText(Path.GetFullPath("Library/P0VerificationGuard/all-results.txt"),"Runtime log guard: "+CheckResult+"\n");}File.WriteAllLines(Path.Combine(directory,"unexpected-errors.txt"),errors);}
            finally{Application.logMessageReceived-=capture;DemoSave.testPath=originalPath;DemoSave.verificationFallback=originalFallback;save=originalSave;frontend=originalFrontend;selected=originalSelected;verificationRunning=false;}
        }
    }
}
#endif
