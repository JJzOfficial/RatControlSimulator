#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RunEquipmentChecks(){StartVerification(EquipmentChecks(),"equipment");}
        IEnumerator EquipmentChecks(){
            var original=save;var oldPath=DemoSave.testPath;bool oldFrontend=frontend;int oldSelected=selected;
            var log=new List<string>();string dir=Path.GetFullPath("Library/P0EquipmentChecks");Directory.CreateDirectory(dir);DemoSave.testPath=Path.Combine(dir,"isolated-profile.json");frontend=false;
            save=new SaveRecord{xp=15000,cash=0,starterGranted=true};ShowCompany();yield return null;
            try{
                var j=save.job;var gun=NewItem(Kit.Airgun);var ammo=NewItem(Kit.Pellets);
                j.kit.Add(gun.kit);j.itemIds.Add(gun.id);j.conditions.Add(100);j.quantities.Add(1);
                j.kit.Add(ammo.kit);j.itemIds.Add(ammo.id);j.conditions.Add(100);j.quantities.Add(30);NormalizeSlots();Select(0);
                BeginReload();Require(reloadItem==gun.id,"reload binds original weapon instance");AdvanceReload(reloadDuration*.5f);Require(j.quantities[1]==30&&Magazine(gun.id).rounds==0,"reload cannot consume early");Select(-1);AdvanceReload(9);Require(j.quantities[1]==30&&Magazine(gun.id).rounds==0,"cancelled reload conserves ammo");
                Select(0);BeginReload();AdvanceReload(reloadDuration+.01f);Require(j.quantities[1]==25&&Magazine(gun.id).rounds==5,"reload transfers exact capacity");Require(SpendRound(Kit.Airgun)&&Magazine(gun.id).rounds==4,"single round conserved");Persist();var loaded=DemoSave.Load(0);Require(loaded.magazines.Single(m=>m.itemId==gun.id).rounds==4,"weapon rounds survive save");
                log.Add("PASS reload clock / cancellation / exact transfer / shot / per-instance persisted ammunition");
                j.kit.Clear();j.itemIds.Clear();j.conditions.Clear();j.quantities.Clear();j.cargoIds.Clear();j.baitedSlots.Clear();selected=-1;
                Require(!LearnSkill(1),"branch prerequisite enforced");Require(LearnSkill(0)&&LearnSkill(0)&&LearnSkill(1),"rank and branch progression");Require(SkillValue("breathSeconds",8)==12,"learned effect applied");int points=SkillPoints;Require(!LearnSkill(0)&&SkillPoints==points,"maximum rank does not charge");
                save.skillRanks=new int[26];for(int i=0;i<6;i++){j.kit.Add((int)Kit.Baton);j.itemIds.Add("slot-"+i);j.conditions.Add(100);j.quantities.Add(1);}NormalizeSlots();Require(BackpackUsed()==6&&BackpackCapacity==6&&!HasBackpackSpace(Kit.Baton),"level slots and capacity");Persist();Require(DemoSave.Load(0).job.kit.Count==6,"expanded bag loads correctly");
                log.Add("PASS skill gates / branch points / rank costs / applied effects / level slots / expanded bag save");
                save=new SaveRecord{cash=100000,xp=15000,starterGranted=true};selected=-1;ShowCompany();j=save.job;var scope=NewItem(Kit.Binoculars);j.kit.Add(scope.kit);j.itemIds.Add(scope.id);j.conditions.Add(100);j.quantities.Add(1);NormalizeSlots();Select(0);
                int balance=save.cash;Require(!BuyService("U.E15.Thermal")&&save.cash==balance,"thermal upgrade cannot skip night vision");Require(BuyService("U.E15.NV")&&BuyService("U.E15.Thermal"),"sequential optical services installed");balance=save.cash;Require(!BuyService("U.E15.NV")&&save.cash==balance,"same instance cannot pay twice");Require(HasService("U.E15.Thermal"),"selected original instance owns its upgrade");AddBehaviorItem(Kit.Binoculars);Select(1);Require(!HasService("U.E15.Thermal"),"second instance cannot inherit the first upgrade");
                Require(!BuyService("V.Ordinary12")&&BuyService("V.Ordinary6")&&BuyService("V.Ordinary8")&&BuyService("V.Ordinary12"),"vehicle services require each previous tier");Require(ServiceValue("ordinaryLiveSlots",4)==12,"vehicle capacity applies final tier");
                balance=save.cash;string goodPath=DemoSave.testPath;DemoSave.testPath=Path.Combine(dir,"blocked-save");Directory.CreateDirectory(DemoSave.testPath+".tmp");Require(!BuyService("V.Large2")&&save.cash==balance&&!save.upgrades.Contains("V.Large2"),"failed service save preserves cash and installation");Directory.Delete(DemoSave.testPath+".tmp");DemoSave.testPath=goodPath;
                log.Add("PASS service prerequisites / per-instance isolation / duplicate rejection / vehicle effects / save failure rollback");
                for(int q=1;q<18;q++){
                    save=new SaveRecord{cash=0,starterGranted=true,selectedMap=q/3,selectedContract=q%3,completedContracts=config.content.contracts.Take(q).Select(c=>c.id).ToList()};selected=-1;ShowCompany();StartContract();j=save.job;
                    Require(!save.atDepot&&j.contractIndex==q,"zero cash may accept "+q);int count=j.borrowedItems.Count;PrepareContractLoans(j);Require(j.borrowedItems.Count==count,"loan repeated preparation must not duplicate "+q);
                    if(q==2||q==3||q==4||q==12){for(int source=0;source<(q==3?3:q==12?2:1);source++){var kit=SourceTool(source);int slot=j.kit.IndexOf((int)kit);if(slot<0){var item=j.looseItems.First(i=>i.kit==(int)kit);j.looseItems.Remove(item);j.kit.Add(item.kit);j.itemIds.Add(item.id);j.conditions.Add(item.condition);j.quantities.Add(1);NormalizeSlots();slot=j.kit.Count-1;}Select(slot);target=world.workTargets[source];CompleteSpecializedSource(source);Require(j.work[source]<3,"source work requires investigation");GestureInput(false,false,false,true,true,false,.35f);GestureInput(false,false,false,false,true,false,2.01f);GestureInput(false,false,false,false,false,true,0);Require(SourceInspected(source),"source inspected using Q action");if(q==4){CompleteSpecializedSource(source);Require(j.work[source]<3,"silverfish paper cannot be treated before the leak");int repairSlot=j.kit.IndexOf((int)Kit.Repair);if(repairSlot<0){var repairItem=j.looseItems.First(i=>i.kit==(int)Kit.Repair);j.looseItems.Remove(repairItem);j.kit.Add(repairItem.kit);j.itemIds.Add(repairItem.id);j.conditions.Add(repairItem.condition);j.quantities.Add(1);NormalizeSlots();repairSlot=j.kit.Count-1;}Select(repairSlot);HoldAction(world.workTargets[2]);Require(j.work[2]==1,"borrowed repair kit repairs the prerequisite leak");Select(slot);target=world.workTargets[source];}GestureInput(true,true,false,false,false,false,.35f);GestureInput(false,true,false,false,false,false,3.01f);ResetGestures();Require(j.work[source]==3&&j.pests.Where(p=>p.source==source&&(q!=12||p.species=="I12")).All(p=>p.dead),"borrowed tools actually complete source work "+q+"/"+source);}}
                    if(q==12){Require(j.pests.Where(p=>p.species=="I12").Select(p=>p.source).Distinct().Count()==2,"both required trees host their own I12 source");Require(j.pests.Where(p=>p.species=="I14").All(p=>!p.dead&&!p.cleaned),"tree treatment preserves separate living fruit pests");foreach(var pest in world.pests.Where(p=>p.record.species=="I14"))pest.Dose(1);target=world.workTargets[1];InvestigateTarget(target);InvestigateTarget(target);Require(j.investigatedSpecies.Contains("I12")&&j.investigatedSpecies.Contains("I14"),"both insect investigations remain obtainable after tree treatment");}var borrowed=j.borrowedItems.ToArray();FinalizeJob(false);Require(save.atDepot&&!save.job.looseItems.Any(i=>borrowed.Contains(i.id)),"borrowed equipment returned "+q);
                }
                log.Add("PASS Q02-Q18 zero-money loan packages / duplicate rejection / return removal");CheckCargoDisplayAndTransfer(log);CheckResult="Passed";
            }catch(Exception e){CheckResult="Failed: "+e;Debug.LogException(e);}
            log.Add(CheckResult);File.WriteAllLines(Path.Combine(dir,"results.txt"),log);reloadItem=null;bowCharge=0;DemoSave.testPath=oldPath;save=original;frontend=oldFrontend;selected=oldSelected;StopDrone();ResetGestures();
            if(save.atDepot)ShowCompany();else{world.SetDepot(false);world.RestoreJob();Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;SyncPause();}
        }
    }
}
#endif
