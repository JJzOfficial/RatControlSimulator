#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RunV4Checks(){StartCoroutine(V4Checks());}
        IEnumerator V4Checks(){
            CheckResult="Running";var original=save;var originalSelected=selected;var log=new List<string>();
            var path=Path.GetFullPath(Application.isEditor?"Library/RiverbendChecksV4":"VerificationV4");Directory.CreateDirectory(path);DemoSave.testPath=Path.Combine(path,"isolated-profile.json");
            save=new SaveRecord{cash=300,atDepot=true};selected=-1;ShowCompany();yield return null;
            try{
                EnsureDepot();var j=save.job;Require(Phase==ShiftPhase.Company&&world.depotRoot.gameObject.activeSelf&&!Paused,"walkable depot");
                Require(j.looseItems.Count==10,"starter count");var starterIds=j.looseItems.Select(x=>x.id).ToArray();EnsureDepot();Require(j.looseItems.Count==10,"starter once");
                log.Add("PASS walkable depot, physical starter stock, single gift");
                foreach(var k in new[]{Kit.Broom,Kit.Repair,Kit.CleaningRefill,Kit.RepairPack}){var item=world.dynamicRoot.GetComponentsInChildren<DemoLooseItem>().First(x=>x.record.kit==(int)k&&j.looseItems.Contains(x.record));TapAction(item.GetComponent<DemoTarget>());}
                Select(j.kit.IndexOf((int)Kit.CleaningRefill));UseSupply();Require(j.conditions[j.kit.IndexOf((int)Kit.Broom)]==100,"clean refill");
                Select(j.kit.IndexOf((int)Kit.RepairPack));UseSupply();Require(j.conditions[j.kit.IndexOf((int)Kit.Repair)]==100,"repair refill");
                Select(j.kit.IndexOf((int)Kit.Repair));j.repairs=70;SaveToolState();Select(j.kit.IndexOf((int)Kit.Broom));Require(j.conditions[j.kit.IndexOf((int)Kit.Repair)]==70&&j.cleaning==100,"independent tool states");
                AddCart(Kit.Broom,2);AddCart(Kit.Bait,3);Require(CartTotal()==460,"cart totals");int before=save.cash;Checkout();Require(save.cash==before&&CartTotal()==460,"insufficient cart not paid");
                save.cash=1000;Checkout();Require(save.cash==540&&CartTotal()==0&&j.looseItems.Count(x=>x.kit==(int)Kit.Broom)==2,"whole order delivered with multiple tools");Checkout();Require(save.cash==540,"no double checkout");
                var counterIds=j.looseItems.Where(x=>!IsTruckItem(x)).Select(x=>x.id).ToArray();Require(counterIds.Length==5,"counter goods remain physical");
                Require(save.selectedMap==0&&save.selectedContract==0,"default playable selection");
                Select(j.kit.IndexOf((int)Kit.Repair));Require(j.repairs==70,"load correct per-instance condition");
                var b2=NewItem(Kit.Broom);j.kit.Add(b2.kit);j.itemIds.Add(b2.id);j.conditions.Add(20);j.quantities.Add(1);NormalizeSlots();
                var pack=NewItem(Kit.CleaningRefill);j.kit.Add(pack.kit);j.itemIds.Add(pack.id);j.conditions.Add(-1);j.quantities.Add(100);NormalizeSlots();Select(3);UseSupply();Require(panel=="refill"&&Paused,"multi-tool selection opens");Close();RefillSlot(2);Require(j.conditions[0]==100&&j.conditions[2]==100&&j.quantities[3]==20,"partial fill targets chosen instance");UseSupply();Close();RefillSlot(2);Require(j.quantities[3]==20,"full condition does not consume");RemoveSlot(3);RemoveSlot(2);Select(j.kit.IndexOf((int)Kit.Repair));
                log.Add("PASS right click refills, multi-tool selection, partial/full rejection, per-instance condition, quantities, batch payment and insufficient funds");
                StartContract();j=save.job;Require(Phase==ShiftPhase.Day&&!save.atDepot&&j.looseItems.Count==6,"equipment transferred, no new gift");
                Require(save.depotItems.Count==5&&counterIds.All(id=>save.depotItems.Any(x=>x.id==id)),"unloaded counter stock stays at depot");
                Require(j.itemIds.Concat(j.looseItems.Select(x=>x.id)).Distinct().Count()==j.itemIds.Count+j.looseItems.Count,"item identities unique");
                Require(j.workPositions.Count==5&&world.Route(new Vector3(6,.1f,3),new Vector3(-3,3.36f,3.5f)).Count>0,"work reachable and upper floor connected");
                foreach(var p in world.pests)p.Dose(1);Select(j.kit.IndexOf((int)Kit.Broom));var source=world.workTargets[0];int cleanBefore=j.cleaning;HoldAction(source);Require(j.cleaning==cleanBefore-10,"one work clean costs ten");HoldAction(source);Require(j.cleaning==cleanBefore-10,"completed work not charged twice");
                var wallA=new PestRecord{id="wall-a",species="I01",source=0,dead=true,position=new Vector3(1.3f,.15f,0)};var wallB=new PestRecord{id="wall-b",species="I01",source=0,dead=true,position=new Vector3(1.8f,.15f,0)};var upper=new PestRecord{id="floor-b",species="I01",source=0,dead=true,position=new Vector3(1.3f,3.36f,0)};j.pests.AddRange(new[]{wallA,wallB,upper});world.RestorePests();CleanArea(wallA.position);Require(wallA.cleaned&&!wallB.cleaned&&!upper.cleaned,"wall and floor block area clean");
                Select(j.kit.IndexOf((int)Kit.Repair));int repairBefore=j.repairs;HoldAction(world.workTargets[2]);Require(j.repairs==repairBefore-10,"leak costs ten");
                var extra=new PestRecord{id="area-extra",species="I01",source=0,dead=true,position=j.workPositions[0]+Vector3.forward*.5f};var extra2=new PestRecord{id="area-extra2",species="I01",source=0,dead=true,position=j.workPositions[0]+Vector3.forward*.7f};j.pests.Add(extra);j.pests.Add(extra2);
                world.RestorePests();Select(j.kit.IndexOf((int)Kit.Broom));cleanBefore=j.cleaning;var bug=world.pests.First(x=>x.record.id==extra.id);HoldAction(bug.GetComponent<DemoTarget>());Require(extra.cleaned&&extra2.cleaned&&j.cleaning==cleanBefore-10,"area clean multiple corpses one cost");Require(CanInvestigate(world.workTargets[0]),"first sample retained");
                log.Add("PASS job equipment persistence, random anchors, upstairs route, 10 percent work cost and multi-remains cleaning");
                save.cart=Enumerable.Repeat(0,shopKits.Length).ToList();AddCart(Kit.Bait,2);AddCart(Kit.SprayRefill,1);int cash=save.cash;Checkout();Require(save.cash==cash-65&&j.orderRemaining==10,"field batch paid once");Open("van");AdvanceDelivery(30);Require(j.orderRemaining==10,"delivery pauses");Close();AdvanceDelivery(9);Require(j.deliveredItems.Count==3,"whole order pending");AdvanceDelivery(1);Require(j.deliveredItems.Count==0&&j.crateItems.Count==3&&j.orderRemaining==0,"whole order arrived");
                var crate=world.dynamicRoot.Find("Delivery crate").GetComponent<DemoTarget>();
                while(j.kit.Count<4){var item=NewItem(Kit.Baton);j.kit.Add(item.kit);j.itemIds.Add(item.id);j.conditions.Add(item.condition);j.quantities.Add(1);NormalizeSlots();}
                TakeDelivery(crate);Require(j.crateItems.Count==3,"full bag leaves crate stock");RemoveSlot(3);TakeDelivery(crate);Require(j.crateItems.Count==2&&j.kit.Count==4,"physical crate collection");
                var sprayStock=j.looseItems.First(x=>x.kit==(int)Kit.Spray);Require(sprayStock.condition==0,"unused starter state retained");
                log.Add("PASS 10 active-second order, menu pause, all line items delivered and full-bag crate retention");
                BeginNight();BeginNight();Require(j.animals.Count(a=>a.required)==5,"five targets one activation");
                var d1=new DeviceRecord{id="v4-cage",itemId="cage-original",cage=true,position=new Vector3(10,.02f,15)};j.devices.Add(d1);var device=world.CreateDevice(d1);
                var rat=world.animals.Values.First(a=>a.record.required);rat.transform.position=device.transform.TransformPoint(new Vector3(0,.09f,-.1f));device.TryEnter(rat);Require(!rat.record.contained,"no teleport capture");foreach(float z in new[]{.6f,.4f,.3f,.15f}){rat.transform.position=device.transform.TransformPoint(new Vector3(0,.09f,z));device.TryEnter(rat);}Require(rat.record.contained&&j.Handled==1,"physical entrance capture");HoldAction(device.GetComponent<DemoTarget>());Require(!rat.record.contained&&j.Handled==0,"release revokes control");
                foreach(var a in world.animals.Values.Where(a=>a.record.required))a.Kill();Require(j.Handled==5,"all five counted uniquely");
                j.rareAt=180;j.nightSeconds=0;j.rareWillSpawn=false;Open("pause");AdvanceRare(400);Require(j.nightSeconds==0,"rare pauses");Close();AdvanceRare(180);Require(j.rareRolled&&!j.rareSpawned,"rare one negative decision");var poses=j.workPositions.ToArray();Persist();var restored=DemoSave.Load(300);Require(restored.job.workPositions.SequenceEqual(poses)&&restored.job.rareRolled&&!restored.job.rareWillSpawn,"save restores layout and rare decision");
                log.Add("PASS five mouse activation, entrance capture, release recount and stored rare decision");
                var animal=new AnimalRecord{id="v4-trade",species="M01",contained=true,loaded=true,investigated=true};save.cargo.Add(animal);
                CheckPosition(new Vector3(0,.1f,2.6f),new Vector3(0,.1f,9));
                for(int climb=0;climb<300;climb++)world.controller.Move((Vector3.forward*1.35f+Vector3.down*2)*.02f);
                Require(world.controller.transform.position.y>3.1f,"actual controller ascends stairs");
                var keptIds=j.itemIds.Concat(j.looseItems.Select(x=>x.id)).ToArray();FinalizeJob(false);
                Require(Phase==ShiftPhase.Result&&Paused,"settlement remains paused until acknowledged");
                Require(counterIds.All(id=>save.job.looseItems.Any(x=>x.id==id)),"counter stock restored with original IDs");
                Require(save.atDepot&&save.job.looseItems.Any(x=>x.id==starterIds[0]),"return equipment original identity");
                Phase=ShiftPhase.Company;panel="";SyncPause();j=save.job;
                Require(!Paused&&Time.timeScale==1,"result acknowledged resumes depot");
                while(j.kit.Count>0)RemoveSlot(0);
                var cage=NewItem(Kit.Cage);j.kit.Add(cage.kit);j.quantities.Add(1);j.itemIds.Add(cage.id);j.conditions.Add(100);NormalizeSlots();PickDepotAnimal(animal.id);Require(j.cargoIds.Contains(animal.id),"physical live carrying");tradeDonation=true;StageCarriedAnimal();Require(save.donationHolding.Contains(animal.id)&&!j.cargoIds.Contains(animal.id),"stage not immediate payment");int xp=save.xp;cash=save.cash;CommitTrade(new[]{animal.id});Require(save.xp==xp+15&&save.cash==cash&&!save.cargo.Any(a=>a.id==animal.id),"donation only XP");CommitTrade(new[]{animal.id});Require(save.xp==xp+15,"no repeated donation");
                var a2=new AnimalRecord{id="trade-second",species="M02",contained=true,loaded=true,investigated=true};var a3=new AnimalRecord{id="trade-third",species="M01",contained=true,loaded=true,investigated=true};save.cargo.Add(a2);save.cargo.Add(a3);
                PickDepotAnimal(a2.id);PickDepotAnimal(a3.id);Require(j.cargoIds.Contains(a2.id)&&!j.cargoIds.Contains(a3.id),"occupied cage cannot take another animal");tradeDonation=false;StageCarriedAnimal();cash=save.cash;xp=save.xp;CommitTrade(new[]{a2.id});Require(save.cash==cash+80&&save.xp==xp&&!save.cargo.Any(a=>a.id==a2.id),"sale only cash");CommitTrade(new[]{a2.id});Require(save.cash==cash+80,"no repeat sale");
                var stock=save.job.looseItems.Count;FinalizeJob(false);Require(save.job.looseItems.Count==stock,"duplicate departure rejected at depot");
                log.Add("PASS return stock, cage transfer, XP-only donation and duplicate rejection");CheckResult="Passed";
            }catch(Exception e){CheckResult="Failed: "+e;Debug.LogException(e);}
            log.Add(CheckResult);File.WriteAllLines(Path.Combine(path,"results.txt"),log);
            DemoSave.testPath=null;save=original;selected=originalSelected;
            if(save.atDepot){ShowCompany();}else {Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;world.SetDepot(false);world.RestoreJob();panel="";SyncPause();}
            Debug.Log("[Riverbend V4 checks] "+CheckResult);
        }
    }
}
#endif
