#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RunContentChecks(){StartVerification(ContentChecks(),"content");}
        IEnumerator ContentChecks(){
            var original=save;bool oldFrontend=frontend;int oldSelected=selected;string oldPath=DemoSave.testPath;
            string directory=Path.GetFullPath("Library/P0ContentChecks");Directory.CreateDirectory(directory);var log=new List<string>();
            DemoSave.testPath=Path.Combine(directory,"isolated-profile.json");frontend=false;CheckResult="Running";
            var steps=new List<Action>();
            for(int q=1;q<18;q++){int index=q;steps.Add(()=>CheckOneContract(index,log));}
            steps.Add(()=>{
                foreach(var definition in config.content.species){
                    if(definition.id.StartsWith("I")){var root=new GameObject("Model fixture "+definition.id);var pest=root.AddComponent<DemoPest>();pest.world=world;pest.record=new PestRecord{id="model-"+definition.id,species=definition.id};pest.Build();Require(root.GetComponentsInChildren<Renderer>().Length>=3,"insect anatomy "+definition.id);Destroy(root);}
                    else{var record=new AnimalRecord{id="model-"+definition.id,species=definition.id};var actor=world.CreateAnimal(record);Require(actor.GetComponentsInChildren<Renderer>().Length>=3,"animal anatomy "+definition.id);Require(actor.GetComponent<Collider>().bounds.size.magnitude>.05f,"animal collider "+definition.id);world.animals.Remove(record.id);Destroy(actor.gameObject);}
                }
                foreach(Kit kit in Enum.GetValues(typeof(Kit))){world.Equip(kit);Require(world.handRoot.childCount>0,"equipment model "+kit);}
                world.Equip(null);log.Add("PASS 45 species models/colliders and all original plus appended held equipment models instantiate");
            });
            foreach(var step in steps){bool failed=false;try{step();}catch(Exception e){CheckResult="Failed: "+e;Debug.LogException(e);failed=true;}if(failed)break;yield return null;}
            if(CheckResult=="Running")CheckResult="Passed";log.Add(CheckResult);File.WriteAllLines(Path.Combine(directory,"results.txt"),log);
            DemoSave.testPath=oldPath;save=original;selected=oldSelected;frontend=oldFrontend;StopDrone();ResetGestures();
            if(save.atDepot)ShowCompany();else{world.SetDepot(false);world.RestoreJob();Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;SyncPause();}
            Debug.Log("[P0 content checks] "+CheckResult);
        }
        void CheckOneContract(int index,List<string> log){
            var definition=config.content.contracts[index];save=new SaveRecord{cash=100000,xp=15000,selectedMap=index/3,selectedContract=index%3,completedContracts=config.content.contracts.Take(index).Select(c=>c.id).ToList()};selected=-1;ShowCompany();StartContract();var j=save.job;
            Require(!save.atDepot&&j.contractIndex==index&&j.mapIndex==index/3,"selected contract retains map identity "+definition.id);
            BeginNight();int firstCount=j.animals.Count;BeginNight();Require(j.animals.Count==firstCount,"night cannot duplicate budget "+definition.id);Require(!j.CanDepart,"cannot settle untreated contract "+definition.id);
            float before=j.nightSeconds;Open("pause");AdvanceAdditionalContract(1000);Require(j.nightSeconds==before,"pause freezes contract clock "+definition.id);Close();
            AdvanceAdditionalContract(Mathf.Max(0,definition.durationSeconds));Require(j.animals.Count(a=>a.required)==definition.RequiredCount,"published target budget "+definition.id);
            j.work=new[]{3,3,3,1,1};j.traceCount=2;j.traceMarks=new List<int>{0,1};
            foreach(var a in j.animals.Where(a=>a.required)){if(definition.type=="Live"){a.contained=true;a.loaded=true;a.controlMethod="Cage";}else a.dead=true;}
            AdvanceAdditionalContract(0);Require(j.CanDepart,"main objective accepts valid completion "+definition.id);
            if(definition.type=="Live"){var delivery=j.animals.First(a=>a.deliveryTarget);delivery.dead=true;Require(!j.CanDepart,"dead delivery rejected "+definition.id);delivery.dead=false;}
            if(definition.type=="Hunt"){var target=j.animals.First(a=>a.required);target.dead=false;target.contained=true;Require(!j.CanDepart,"live capture cannot replace hunt "+definition.id);target.dead=true;}
            Persist();var loaded=DemoSave.Load(300);Require(loaded.job.contractIndex==index&&loaded.job.mapIndex==index/3,"save retains contract identity "+definition.id);var replay=JsonUtility.ToJson(j);
            int money=save.cash;FinalizeJob(true);Require(save.atDepot&&save.cash>=money+definition.rewardCash&&save.completedContracts.Contains(definition.id),"settlement reward and unlock "+definition.id);
            int stars=save.contractStars[index];money=save.cash;save.job=JsonUtility.FromJson<JobRecord>(replay);save.atDepot=false;Phase=ShiftPhase.Night;FinalizeJob(true);
            Require(save.cash==money+definition.rewardCash&&save.contractStars[index]==stars,"repeat contract cannot repeat first-star reward "+definition.id);
            Phase=ShiftPhase.Company;panel="";SyncPause();log.Add("PASS "+definition.id+" selection / finite budget / pause / invalid and valid main conditions / save / reward / repeat-star rejection");
        }
    }
}
#endif
