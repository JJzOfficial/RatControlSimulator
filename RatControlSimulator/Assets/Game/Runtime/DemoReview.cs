#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.IO;
using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        SaveRecord reviewOriginal;string reviewPath;bool reviewFrontend;int reviewSelected;
        public void ReviewMap(int map,bool night=false){
            if(verificationRunning){Debug.LogWarning("Wait for verification to finish before reviewing a map.");return;}
            if(reviewOriginal==null){reviewOriginal=save;reviewPath=DemoSave.testPath;reviewFrontend=frontend;reviewSelected=selected;}
            reviewFrozen=false;DemoSave.testPath=Path.GetFullPath("Library/P0Review/isolated-profile.json");frontend=false;save=new SaveRecord{cash=100000,xp=15000,starterGranted=true,selectedMap=map,completedContracts=config.content.contracts.Select(c=>c.id).ToList()};selected=-1;ShowCompany();StartContract();
            if(night)BeginNight();reviewFrozen=true;panel="";world.controller.enabled=false;world.controller.transform.position=map==0?new Vector3(-13,9,-12):map==4?new Vector3(18,15,-13):new Vector3(-17,12,-13);world.controller.transform.rotation=Quaternion.Euler(0,map==4?-10:25,0);world.controller.enabled=true;world.cam.transform.localRotation=Quaternion.Euler(26,0,0);world.cam.transform.localPosition=Vector3.up*1.6f;SyncPause();
        }
        public void ReviewGrowth(){ReviewMap(0);reviewFrozen=false;ShowCompany();reviewFrozen=true;panel="growth";SyncPause();}
        public void ReviewCargo(){ReviewMap(5);var j=save.job;j.animals.Add(new AnimalRecord{id="review-boar",species="M16",loaded=true,dead=true,bagged=true});j.animals.Add(new AnimalRecord{id="review-fox",species="M14",loaded=true,contained=true});world.RefreshTruckCargo();CheckPosition(DemoWorld.VanPoint+new Vector3(-10,3.5f,7),DemoWorld.VanPoint+new Vector3(-3,1.3f,0));reviewFrozen=true;SyncPause();}
        public void ReviewFruitSource(){ReviewMap(4);var p=save.job.workPositions[1];CheckPosition(p+new Vector3(-1.8f,1,-1.8f),p+Vector3.up*.2f);reviewFrozen=true;SyncPause();}
        public void ReviewMonitoring(){ReviewMap(5,true);var j=save.job;var r=new DeviceRecord{id="review-camera",itemId="review-camera-item",kit=(int)Kit.Camera,position=new Vector3(-4,0,-5),yaw=20};j.devices.Add(r);world.CreateDevice(r);panel="monitor";SyncPause();}
        public void ReviewGuide(){ReviewMap(4);guideSelected=System.Array.FindIndex(config.content.species,s=>s.id=="C04");guidePage=guideSelected/9;save.encyclopedia.Add("C04");panel="journal";tab=1;SyncPause();}
        public void ReviewCommands(){ReviewMap(2,true);save.partners.Add(new PartnerRecord{id="review-cat",type="H01",active=true});save.partners.Add(new PartnerRecord{id="review-dog",type="H02",active=true});world.RestoreJob();panel="commands";SyncPause();}
        public void EndReview(){if(reviewOriginal==null)return;DemoSave.testPath=reviewPath;save=reviewOriginal;reviewOriginal=null;frontend=reviewFrontend;selected=reviewSelected;reviewFrozen=false;panel="";StopDrone();if(save.atDepot)ShowCompany();else{world.SetDepot(false);world.RestoreJob();Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;SyncPause();}}
    }
}
#endif
