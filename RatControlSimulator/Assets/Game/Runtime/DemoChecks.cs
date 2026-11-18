#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Riverbend {
 public partial class DemoGame {
  public string CheckResult {get;private set;}="Not run";
  void Require(bool ok,string message){if(!ok)throw new Exception(message);}
  public void RunIntegrationChecks(){StartCoroutine(CheckFlow());}
  void CheckPosition(Vector3 p,Vector3 look){world.controller.enabled=false;world.controller.transform.position=p;world.controller.transform.rotation=Quaternion.LookRotation(look-p);world.controller.enabled=true;world.cam.transform.localRotation=Quaternion.identity;world.cam.transform.localPosition=Vector3.up*1.6f;pitch=0;vertical=0;Physics.SyncTransforms();}
  public void PreviewLocation(string location){if(save.job==null)StartContract();if(location=="truck")CheckPosition(DemoWorld.VanPoint+new Vector3(0,.85f,2.65f),DemoWorld.VanPoint+new Vector3(0,.85f,-1));else if(location=="upstairs")CheckPosition(new Vector3(-3,3.35f,3.5f),new Vector3(-6,3.35f,1));else if(location=="stairs")CheckPosition(new Vector3(0,.1f,.5f),new Vector3(0,.1f,8));else CheckPosition(new Vector3(-12,.1f,-7),new Vector3(0,.1f,4));}
  IEnumerator CheckFlow(){
   CheckResult="Running";var original=save;var lines=new List<string>();string dir=Application.isEditor?Path.GetFullPath("Library/RiverbendChecksV3"):Path.GetFullPath(Application.dataPath+"/../Verification");Directory.CreateDirectory(dir);DemoSave.testPath=Path.Combine(dir,"profile.json");
   save=new SaveRecord{cash=300};StartContract();yield return null;
   try {
    var j=save.job;Require(j.kit.Count==0&&j.looseItems.Count==10,"initial physical equipment");Require(j.looseItems.Count(x=>x.kit==4)==1&&j.looseItems.Count(x=>x.kit==5)==1,"one cage one trap");
    Require(j.workPositions.Count==5&&world.pests.Count(x=>x.record.source==0)>=2&&world.pests.Count(x=>x.record.source==0)<=4,"random sites / pest population");
    Require(world.Route(new Vector3(6,.15f,3),new Vector3(-3,3.36f,3.5f)).Count>0,"connected upstairs path");Require(!world.ClearLine(new Vector3(4,.4f,3),new Vector3(4,4,3)),"floor blocks sight");lines.Add("PASS initial physical stock, single devices, work anchors, pest density, floor occlusion, stairs route");
    foreach(int type in new[]{0,1,2,7}){var item=world.dynamicRoot.GetComponentsInChildren<DemoLooseItem>().First(x=>x.record.kit==type);TapAction(item.GetComponent<DemoTarget>());}
    Require(j.kit.Count==4,"four slot capacity");var broom=world.dynamicRoot.GetComponentsInChildren<DemoLooseItem>().First(x=>x.record.kit==6);TapAction(broom.GetComponent<DemoTarget>());Require(j.kit.Count==4,"cannot overfill");
    Select(j.kit.IndexOf(7));int supplySlot=selected;
    j.spray=0;j.spraySeconds=0;UseSupply();Require(j.spray==5&&!j.kit.Contains(7)&&j.kit.Contains(0),"RMB refill backpack sprayer consumes supply");
    j.kit.Add(8);j.quantities.Add(5);NormalizeSlots();Select(j.kit.Count-1);UseSupply();Require(j.quantities[selected]==5,"full repair tool consumes nothing");
    j.repairs=0;UseSupply();Require(j.repairs==5&&!j.kit.Contains(8),"RMB repair refill from zero");
    j.kit.Add(11);j.quantities.Add(5);NormalizeSlots();Select(j.kit.Count-1);j.cleaning=0;UseSupply();Require(j.cleaning==5&&!j.kit.Contains(11),"RMB cleaning refill from zero");
    j.kit.Add(7);j.quantities.Add(5);NormalizeSlots();Select(j.kit.Count-1);j.spray=5;j.spraySeconds=2;UseSupply();Require(j.spray==5&&j.spraySeconds==0&&j.quantities[selected]==4,"partial charge refill preserves full condition");
    RemoveSlot(selected);j.kit.Add(8);j.quantities.Add(5);NormalizeSlots();Select(j.kit.Count-1);Open("pause");j.repairs=0;UseSupply();Require(j.repairs==0&&j.quantities[selected]==5,"paused RMB no mutation");Close();UseSupply();Require(j.repairs==5,"resume permits refill");
    Select(j.kit.IndexOf(2));CheckPosition(new Vector3(10,.1f,18),new Vector3(10,.1f,20));DropCargo();j.kit.Add(8);j.quantities.Add(5);NormalizeSlots();Select(j.kit.Count-1);j.repairs=0;UseSupply();Require(j.repairs==0&&j.quantities[selected]==5,"missing tool consumes no refill");RemoveSlot(selected);
    var repairItem=world.dynamicRoot.GetComponentsInChildren<DemoLooseItem>().First(x=>x.record.kit==2&&j.looseItems.Contains(x.record));TapAction(repairItem.GetComponent<DemoTarget>());j.repairs=5;
    j.cleaning=0;Select(j.kit.IndexOf(1));HoldAction(world.workTargets[0]);Require(j.work[0]<3,"depleted cleaning blocked");j.cleaning=5;
    Require(ToolCondition(Kit.Broom)==1&&ToolCondition(Kit.Repair)==1&&ToolCondition(Kit.Spray)==1,"slot condition full");
    foreach(var bug in world.pests)bug.Dose(1);Require(!j.RequiredWorkDone,"kill not clean");Select(j.kit.IndexOf(1));HoldAction(world.workTargets[0]);HoldAction(world.workTargets[1]);Require(CanInvestigate(world.workTargets[0]),"retained first sample");InvestigateTarget(world.workTargets[0]);InvestigateTarget(world.workTargets[1]);
    j.repairs=5;Select(j.kit.IndexOf(2));for(int i=2;i<5;i++)HoldAction(world.workTargets[i]);Require(j.RequiredWorkDone&&j.repairs==1&&j.cleaning==3,"repair and cleaning consume durability once");
    HoldAction(world.workTargets[1]);Require(j.cleaning==3,"completed clean cannot consume twice");
    lines.Add("PASS RMB supplies: spray, repair, cleaning, partial refill, full/missing tool rejection, pause, condition display values, depletion and no double consumption");
    var positions=j.workPositions.ToArray();Persist();var loaded=DemoSave.Load(300);Require(loaded.job.workPositions.SequenceEqual(positions)&&loaded.job.cleaning==3&&loaded.job.repairs==1,"save layout and condition stable");lines.Add("PASS slots, physical refill, cleaning independent of kill, samples, repair charges, saved layout");
    Order("bait",20);Open("van");AdvanceDelivery(20);Require(j.orderRemaining==10,"paused delivery");Close();AdvanceDelivery(9);Require(j.deliveredItem==null,"not early");AdvanceDelivery(1);Require(j.deliveredItem=="bait","10 second delivery");
    while(j.kit.Count<4){j.kit.Add(6);NormalizeSlots();}var crate=world.dynamicRoot.Find("Delivery crate").GetComponent<DemoTarget>();TapAction(crate);Require(j.deliveredItem=="bait","full crate retained");RemoveSlot(3);TapAction(crate);Require(j.kit.Contains(9)&&j.deliveredItem==null,"physical delivery collection");
    j.kit.Clear();j.quantities.Clear();j.cargoIds.Clear();j.baitedSlots.Clear();selected=-1;BeginNight();BeginNight();Require(j.animals.Count(a=>a.required)==5,"five targets idempotent");Require(!j.rareRolled&&!j.animals.Any(a=>a.species=="C01"),"no immediate rare");
    var cage=new DeviceRecord{id="test-cage",cage=true,position=new Vector3(10,.02f,15)};j.devices.Add(cage);var d=world.CreateDevice(cage);j.kit.Add(9);j.quantities.Add(2);Select(0);target=d.GetComponent<DemoTarget>();CheckPosition(new Vector3(10,.1f,16.5f),d.transform.position);UseSupply();Require(cage.baited&&j.quantities[0]==1,"manual bait consumes once");UseSupply();Require(j.quantities[0]==1,"no duplicate bait consumption");
    var rat=world.animals.Values.First(a=>a.record.required);rat.transform.position=d.transform.TransformPoint(new Vector3(0,.09f,-.1f));d.TryEnter(rat);Require(!rat.record.contained,"outside/inside teleport cannot capture");
    foreach(float z in new[]{.6f,.4f,.3f,.15f}){rat.transform.position=d.transform.TransformPoint(new Vector3(0,.09f,z));d.TryEnter(rat);}Require(rat.record.contained,"true mouth entry");HoldAction(d.GetComponent<DemoTarget>());Require(!rat.record.contained&&j.Handled==0,"release revokes count");
    foreach(var a in world.animals.Values.Where(a=>a.record.required))a.Kill();Require(j.Handled==5&&j.CanDepart,"all five required");
    var corpses=world.animals.Values.Where(a=>a.record.required).Take(2).ToArray();j.kit.Clear();j.quantities.Clear();j.cargoIds.Clear();j.baitedSlots.Clear();selected=-1;foreach(var a in corpses)TapAction(a.GetComponent<DemoTarget>());Require(j.kit.Count==2&&j.kit.All(k=>k==10)&&j.cargoIds.Distinct().Count()==2,"multiple independent corpse slots");Select(0);Require(world.handRoot.Find("Held mouse remains")&&!world.handRoot.Find("Held cage base"),"corpse shown without cage");
    CheckPosition(new Vector3(10,.1f,18),new Vector3(10,.1f,20));DropCargo();Require(j.kit.Count==1&&j.cargoIds.Count==1,"corpse drop preserves other slot");
    j.nightSeconds=0;j.rareAt=180;j.rareWillSpawn=true;Open("journal");AdvanceRare(400);Require(j.nightSeconds==0,"paused rare timer");Close();AdvanceRare(179);Require(!j.rareRolled,"rare delay");AdvanceRare(1);Require(j.rareRolled,"one rare decision");int count=j.animals.Count;AdvanceRare(1000);Require(j.animals.Count==count,"rare once");
    lines.Add("PASS delivery timing/capacity, five targets, manual bait, mouth crossing, release count, corpse identity, rare timer/once");
    CheckPosition(new Vector3(0,.1f,2.7f),new Vector3(0,.1f,9));
   }catch(Exception e){CheckResult="Failed: "+e;}
   if(CheckResult=="Running"){float t=0;while(t<6){world.controller.Move((Vector3.forward*1.35f+Vector3.down*2)*Time.deltaTime);t+=Time.deltaTime;yield return null;}try{Require(world.controller.transform.position.y>3.1f,"controller upstairs "+world.controller.transform.position);lines.Add("PASS actual player controller stair ascent");CheckResult="Passed";}catch(Exception e){CheckResult="Failed: "+e;}}
   lines.Add(CheckResult);File.WriteAllLines(Path.Combine(dir,"results.txt"),lines);DemoSave.testPath=null;save=original;
   if(save.job!=null){Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;world.RestoreJob();selected=-1;panel="";world.Equip(null);SyncPause();}else{ShowCompany();SyncPause();}Debug.Log("[Riverbend V3 checks] "+CheckResult);
  }
  #if DEVELOPMENT_BUILD && !UNITY_EDITOR
  IEnumerator VerifyStandalone(){for(int i=0;i<40;i++)yield return null;RunAllContinuationChecks();float elapsed=0;while(verificationRunning&&elapsed<300){yield return new WaitForSecondsRealtime(.5f);elapsed+=.5f;}Application.Quit(!verificationRunning&&CheckResult=="Passed"?0:2);}
  #endif
 }
}
#endif

