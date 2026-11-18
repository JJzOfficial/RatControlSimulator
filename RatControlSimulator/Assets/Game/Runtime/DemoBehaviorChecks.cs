#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RunBehaviorChecks(){StartVerification(BehaviorChecks(),"behavior");}
        IEnumerator BehaviorChecks(){
            var original=save;string oldPath=DemoSave.testPath;bool oldFrontend=frontend;int oldSelected=selected;var log=new List<string>();string dir=Path.GetFullPath("Library/P0BehaviorChecks");Directory.CreateDirectory(dir);DemoSave.testPath=Path.Combine(dir,"isolated-profile.json");frontend=false;save=new SaveRecord{starterGranted=true,atDepot=false,job=new JobRecord{runId="behavior",mapIndex=5,contractIndex=16,stage=2,player=new Vector3(0,.1f,0)}};selected=-1;Phase=ShiftPhase.Night;panel="";world.SetDepot(false);world.RestoreJob();SyncPause();yield return null;
            try{
                var j=save.job;var burrow=BehaviorAnimal("M05",new Vector3(0,.08f,1.5f));var start=burrow.transform.position;burrow.VerifyNewBehaviorStep(.1f);Require(burrow.State!="Flee"&&!burrow.record.contained,"burrow defense does not count as capture");burrow.VerifyNewBehaviorStep(5.1f);Require(Vector3.Distance(start,burrow.transform.position)>4&&burrow.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"burrow re-emerges at a different route exit");
                var opossum=BehaviorAnimal("M12",new Vector3(0,.08f,1.5f));opossum.VerifyNewBehaviorStep(.1f);opossum.VerifyNewBehaviorStep(1);Require(opossum.State=="Feign"&&!opossum.record.dead&&!opossum.record.contained,"feigned death is neither corpse nor stable control");
                var snake=BehaviorAnimal("S02",new Vector3(0,.08f,1));hurtCooldown=0;j.health=100;snake.VerifyNewBehaviorStep(.1f);Require(j.health==100,"snake warns before striking");snake.VerifyNewBehaviorStep(.81f);Require(j.health==90&&j.dangerHits==1,"snake short strike damages once");snake.VerifyNewBehaviorStep(.1f);Require(j.health==90,"snake cooldown prevents immediate repeated strike");
                var boar=BehaviorAnimal("M16",new Vector3(0,.08f,6));hurtCooldown=0;j.health=100;boar.VerifyNewBehaviorStep(.1f);boar.VerifyNewBehaviorStep(.5f);Require(j.health==100&&Vector3.Distance(boar.transform.position,new Vector3(0,.08f,6))<.1f,"boar warning gives reaction time");boar.VerifyNewBehaviorStep(.6f);boar.VerifyNewBehaviorStep(.5f);boar.VerifyNewBehaviorStep(.1f);Require(j.health==75&&j.boarHits==1,"boar charges and hits after warning");
                log.Add("PASS burrow entry/exit / feigned death / snake warning-strike-cooldown / boar warning-charge-hit");
                foreach(var a in world.animals.Values.ToArray())Destroy(a.gameObject);world.animals.Clear();j.animals.Clear();j.health=100;hurtCooldown=0;
                AddBehaviorItem(Kit.ControlPole);AddBehaviorItem(Kit.Cage);NormalizeSlots();Select(0);var corn=BehaviorAnimal("S01",new Vector3(0,.08f,1));target=corn.GetComponent<DemoTarget>();
                TransferWithTool(corn);Require(!corn.record.contained,"snake transfer requires sealed liner");save.snakeLiners.Add(j.itemIds[1]);GestureInput(true,true,false,false,false,false,.35f);GestureInput(false,true,false,false,false,false,1.99f);Require(!corn.record.contained,"pole cannot transfer before two seconds");GestureInput(false,true,false,false,false,false,.02f);Require(corn.record.contained&&j.cargoIds[1]==corn.record.id,"pole transfers into original sealed cage");ResetGestures();
                var small=new DeviceRecord{id="fit-small",itemId="fit-small-item",cage=true,kit=(int)Kit.Cage,position=new Vector3(5,0,0)};var smallActor=world.CreateDevice(small);Require(!smallActor.CanFit(new AnimalRecord{species="M10"}),"small cage rejects medium animal");var trap=world.CreateDevice(new DeviceRecord{id="fit-trap",kit=(int)Kit.SnapTrap,position=new Vector3(6,0,0)});Require(!trap.CanFit(new AnimalRecord{species="M05"})&&trap.CanFit(new AnimalRecord{species="M04"}),"trap species matrix");
                var corpse=new AnimalRecord{id="bag-corpse",species="M14",dead=true};Require(!BagCorpse(corpse),"medium corpse requires bag");AddBehaviorItem(Kit.BodyBag);int bags=j.kit.Count(k=>k==(int)Kit.BodyBag);Require(BagCorpse(corpse)&&corpse.bagged&&j.kit.Count(k=>k==(int)Kit.BodyBag)==bags-1,"bag consumed once");Require(BagCorpse(corpse)&&j.kit.Count(k=>k==(int)Kit.BodyBag)==bags-1,"bagged corpse cannot double charge");
                log.Add("PASS snake liner / timed pole transfer / cage size / trap species / corpse bag conservation");
                j.durability[0]=80;var thiefRecord=new AnimalRecord{id="thief",species="M09",dead=true,stolenSource=0,stolenAt=j.nightSeconds};j.animals.Add(thiefRecord);world.CreateStolenObject(thiefRecord);var item=world.dynamicRoot.GetComponentsInChildren<DemoStolenObject>().First(i=>i.animal.id==thiefRecord.id);RecoverStolen(item);RecoverStolen(item);Require(j.recoveredItems==1&&j.durability[0]==90,"stolen property recovered once, asset restored once");
                var targetRecord=new AnimalRecord{id="partner-target",species="M02",required=true,partnerClaim="partner-cat",partnerHeld=true,position=new Vector3(1,0,0)};j.animals.Add(targetRecord);world.CreateAnimal(targetRecord);j.cargoIds[1]=null;j.carriedAnimal=null;j.carriedSlot=-1;Select(0);var partnerRoot=new GameObject("Cat transfer fixture");partnerRoot.transform.SetParent(world.dynamicRoot);var partner=partnerRoot.AddComponent<DemoPartner>();partner.world=world;partner.record=new PartnerRecord{id="partner-cat",type="H01",target=targetRecord.id};TransferPartner(partner);Require(targetRecord.contained&&j.cargoIds[1]==targetRecord.id&&targetRecord.partnerClaim==null,"cat control transfers into original empty cage");
                log.Add("PASS stolen item exact recovery / cat handover uses actual cage and animal IDs");CheckResult="Passed";
            }catch(Exception e){CheckResult="Failed: "+e;Debug.LogException(e);}
            log.Add(CheckResult);File.WriteAllLines(Path.Combine(dir,"results.txt"),log);foreach(Transform child in world.dynamicRoot)Destroy(child.gameObject);yield return null;
            DemoSave.testPath=oldPath;save=original;frontend=oldFrontend;selected=oldSelected;StopDrone();ResetGestures();if(save.atDepot)ShowCompany();else{world.SetDepot(false);world.RestoreJob();Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;SyncPause();}
        }
        DemoAnimal BehaviorAnimal(string species,Vector3 p){var a=new AnimalRecord{id="behavior-"+species,species=species,position=p};save.job.animals.Add(a);var actor=world.CreateAnimal(a);actor.transform.rotation=Quaternion.LookRotation(world.controller.transform.position-p);return actor;}
        void AddBehaviorItem(Kit k){var j=save.job;var item=NewItem(k);j.kit.Add(item.kit);j.itemIds.Add(item.id);j.conditions.Add(item.condition);j.quantities.Add(item.quantity);NormalizeSlots();}
    }
}
#endif
