#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Riverbend {
    public partial class DemoGame {
        public void RunInputChecks(){StartVerification(InputChecks(),"input");}
        IEnumerator InputChecks(){
            var original=save;bool oldFrontend=frontend;int oldSelected=selected;string oldPath=DemoSave.testPath;string directory=Path.GetFullPath("Library/P0InputChecks");Directory.CreateDirectory(directory);var log=new List<string>();
            DemoSave.testPath=Path.Combine(directory,"isolated-profile.json");frontend=false;save=new SaveRecord{atDepot=false,starterGranted=true,job=new JobRecord{runId="input-check",mapIndex=5,contractIndex=15,stage=1,player=new Vector3(0,.1f,-6)}};Phase=ShiftPhase.Day;selected=-1;panel="";world.SetDepot(false);world.RestoreJob();SyncPause();
            var keyboard=InputSystem.AddDevice<Keyboard>("Riverbend fixture keyboard");var mouse=InputSystem.AddDevice<Mouse>("Riverbend fixture mouse");bool failed=false;CheckResult="Running";
            bool crouchInput=true;Action inject=()=>{InputState.Change(keyboard,crouchInput?new KeyboardState(Key.LeftCtrl,Key.W):new KeyboardState(Key.W));keyboard.MakeCurrent();mouse.MakeCurrent();};InputSystem.onAfterUpdate+=inject;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl,Key.W));InputSystem.Update();yield return null;
            int before=stepEvents;Vector3 start=world.controller.transform.position;float loudest=0;
            for(float elapsed=0;elapsed<1.2f;elapsed+=Time.unscaledDeltaTime){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl,Key.W));InputSystem.Update();keyboard.MakeCurrent();mouse.MakeCurrent();loudest=Mathf.Max(loudest,Noise);yield return null;}
            try{var delta=world.controller.transform.position-start;delta.y=0;Require(delta.magnitude>.5f,"queued crouch input really moves the controller");Require(loudest==0&&stepEvents==before,"crouch emits no movement stimulus and no step audio calls");log.Add("PASS queued Ctrl+W moves controller with zero movement noise and zero footstep playback calls");}catch(Exception e){failed=true;CheckResult="Failed: "+e;Debug.LogException(e);}
            crouchInput=false;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();before=stepEvents;start=world.controller.transform.position;loudest=0;
            for(float elapsed=0;elapsed<1.2f;elapsed+=Time.unscaledDeltaTime){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();keyboard.MakeCurrent();mouse.MakeCurrent();loudest=Mathf.Max(loudest,Noise);yield return null;}
            try{Require(Vector3.Distance(start,world.controller.transform.position)>.5f&&loudest>0&&stepEvents>before,"normal walking still moves and plays steps: start="+start+", end="+world.controller.transform.position+", noise="+loudest+", steps="+(stepEvents-before)+", paused="+Paused+", keyboard="+Keyboard.current.name+", W="+Keyboard.current.wKey.isPressed+", Ctrl="+Keyboard.current.leftCtrlKey.isPressed+", grounded="+world.controller.isGrounded+", delta="+Time.deltaTime+", phase="+Phase);log.Add("PASS queued W walking preserves movement stimulus and footstep playback");}catch(Exception e){failed=true;CheckResult="Failed: "+e;Debug.LogException(e);}
            InputSystem.onAfterUpdate-=inject;InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            if(!failed)CheckResult="Passed";log.Add(CheckResult);File.WriteAllLines(Path.Combine(directory,"results.txt"),log);DemoSave.testPath=oldPath;save=original;frontend=oldFrontend;selected=oldSelected;ResetGestures();if(save.atDepot)ShowCompany();else{world.SetDepot(false);world.RestoreJob();Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;SyncPause();}
        }
    }
}
#endif
