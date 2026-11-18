#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RunMonitoringChecks(){StartVerification(MonitoringChecks(),"monitoring");}
        IEnumerator MonitoringChecks(){
            var original=save;bool originalFrontend=frontend,originalFrozen=reviewFrozen;int originalSelected=selected;string originalPath=DemoSave.testPath;string dir=Path.GetFullPath("Library/P0MonitoringChecks");Directory.CreateDirectory(dir);var log=new List<string>();
            DemoSave.testPath=Path.Combine(dir,"isolated-profile.json");frontend=false;reviewFrozen=false;save=new SaveRecord{starterGranted=true,atDepot=false,job=new JobRecord{runId="monitor-check",contractIndex=16,mapIndex=5,stage=2,player=new Vector3(-70,.1f,105)}};selected=-1;Phase=ShiftPhase.Night;panel="";world.SetDepot(false);world.RestoreJob();SyncPause();yield return null;
            var cameraRecord=new DeviceRecord{id="monitor-camera",itemId="monitor-camera-item",kit=(int)Kit.Camera,position=new Vector3(0,.02f,-6)};save.job.devices.Add(cameraRecord);var camera=world.CreateDevice(cameraRecord);var animal=BehaviorAnimal("M15",new Vector3(0,.08f,0));animal.enabled=false;
            AddBehaviorItem(Kit.Drone);AddBehaviorItem(Kit.Drone);save.job.conditions[0]=50;save.job.conditions[1]=80;Select(0);StartDrone();Vector3 player=world.controller.transform.position;float time=save.job.nightSeconds;
            for(float elapsed=0;elapsed<.8f;elapsed+=Time.unscaledDeltaTime)yield return null;
            bool failed=false;
            try{heardCue=null;heardUntil=0;HearAnimalCue("Wolf",world.cam.transform.position+Vector3.right*2,.024f);Require(heardCue==null,"inaudible source cannot reveal a direction");HearAnimalCue("Wolf",world.cam.transform.position+Vector3.right*2,.2f);Require(heardCue=="Wolf"&&heardUntil>Time.time,"audible source creates a brief local direction hint");log.Add("PASS sound direction hint rejects inaudible sources and accepts audible local cues");Require(droneActive&&save.job.nightSeconds>time+.3f,"drone operation advances the actual night clock");Require(Vector3.Distance(player,world.controller.transform.position)<.01f,"drone leaves the physical player at the launch point");Require(save.job.conditions[0]<50&&save.job.conditions[1]==80,"drone battery belongs only to the selected original item");Require(cameraRecord.observed.Contains(animal.record.id)&&save.job.cameraTargets.Contains(animal.record.id),"monitoring camera records an actual visible target ID");log.Add("PASS real frames advance task clock and selected drone battery while player stays at launch; camera records visible original target");}catch(Exception e){failed=true;Debug.LogException(e);log.Add(e.ToString());}
            Open("journal");float pausedClock=save.job.nightSeconds,pausedEnergy=cameraRecord.energy,pausedBattery=save.droneBattery;
            for(float elapsed=0;elapsed<.3f;elapsed+=Time.unscaledDeltaTime)yield return null;
            try{Require(save.job.nightSeconds==pausedClock&&cameraRecord.energy==pausedEnergy&&save.droneBattery==pausedBattery,"world pause freezes task clock and both camera/drone batteries");Close();StopDrone();Persist();Reload();Require(!droneActive&&save.job.conditions[0]<50&&save.job.conditions[1]==80,"save/load preserves per-instance battery and returns camera to physical player");log.Add("PASS pause freezes monitoring resources; save/load conserves separate drone batteries and restores player camera");}catch(Exception e){failed=true;Debug.LogException(e);log.Add(e.ToString());}
            CheckResult=failed?"Failed":"Passed";log.Add(CheckResult);File.WriteAllLines(Path.Combine(dir,"results.txt"),log);StopDrone();DemoSave.testPath=originalPath;save=original;frontend=originalFrontend;reviewFrozen=originalFrozen;selected=originalSelected;ResetGestures();if(save.atDepot)ShowCompany();else{world.SetDepot(false);world.RestoreJob();Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;SyncPause();}
        }
    }
}
#endif
