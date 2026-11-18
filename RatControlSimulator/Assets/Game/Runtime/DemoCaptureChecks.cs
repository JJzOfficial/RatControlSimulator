#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RunCaptureChecks(){StartVerification(CaptureChecks(),"captures");}
        IEnumerator CaptureChecks(){
            var original=save;bool oldFrontend=frontend;int oldSelected=selected;string oldPath=DemoSave.testPath;var log=new List<string>();string dir=Path.GetFullPath("Library/P0CaptureChecks");Directory.CreateDirectory(dir);DemoSave.testPath=Path.Combine(dir,"isolated-profile.json");frontend=false;save=new SaveRecord{starterGranted=true,atDepot=false,job=new JobRecord{runId="capture-check",contractIndex=16,mapIndex=5,stage=2,player=new Vector3(-70,.1f,105),yaw=0}};selected=-1;Phase=ShiftPhase.Night;panel="";world.SetDepot(false);world.RestoreJob();reviewFrozen=true;Noise=landingNoise=weaponNoise=0;SyncPause();yield return null;
            bool failed=false;
            foreach(var definition in config.content.species.Where(s=>!s.id.StartsWith("I")&&!new[]{"M01","M02","C01"}.Contains(s.id))){
                foreach(var animal in world.animals.Values.ToArray())Destroy(animal.gameObject);foreach(var device in world.devices.Values.ToArray())Destroy(device.gameObject);world.animals.Clear();world.devices.Clear();save.job.animals.Clear();save.job.devices.Clear();yield return null;
                try{var j=save.job;var record=new AnimalRecord{id="capture-"+definition.id,species=definition.id,position=new Vector3(-4,.08f,0)};var cage=new DeviceRecord{id="capture-cage",itemId="capture-cage-item",cage=true,baited=true,kit=(int)(definition.size=="Large"?Kit.LargeCage:definition.size=="Medium"?Kit.MediumCage:Kit.Cage),position=new Vector3(0,.02f,0)};save.snakeLiners.Add(cage.itemId);j.animals.Add(record);j.devices.Add(cage);var actor=world.CreateAnimal(record);var device=world.CreateDevice(cage);Physics.SyncTransforms();
                    for(int step=0;step<1800&&!record.contained;step++){j.nightSeconds+=.1f;actor.VerifyNewBehaviorStep(.1f);Physics.SyncTransforms();}
                    Require(record.contained&&record.deviceId==cage.id&&device.record.triggered,"real bait approach and cage entry "+definition.id+" at "+actor.transform.position+" state "+actor.State);log.Add("PASS "+definition.id+" moves along route / bait approach / physical entry / original animal and cage IDs retained");
                }catch(Exception e){failed=true;log.Add("FAIL "+definition.id+" "+e.Message);Debug.LogException(e);}
            }
            CheckResult=failed?"Failed":"Passed";log.Add(CheckResult);File.WriteAllLines(Path.Combine(dir,"results.txt"),log);reviewFrozen=false;DemoSave.testPath=oldPath;save=original;frontend=oldFrontend;selected=oldSelected;ResetGestures();if(save.atDepot)ShowCompany();else{world.SetDepot(false);world.RestoreJob();Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;SyncPause();}
        }
    }
}
#endif
