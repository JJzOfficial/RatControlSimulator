using System;
using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        void PrepareAdditionalRare(){
            var j=save.job;if(j.contractIndex==0)return;
            if(j.rarePlans==null)j.rarePlans=new System.Collections.Generic.List<RarePlan>();if(j.rarePlans.Count>0)return;
            var rng=new System.Random(j.layoutSeed);
            foreach(var e in config.content.rareEvents.Where(e=>e.hostContractId==Contract(j).id)){
                float delay=e.delaySecondsRange[0]+(e.delaySecondsRange[1]-e.delaySecondsRange[0])*(float)rng.NextDouble();
                j.rarePlans.Add(new RarePlan{species=e.speciesId,at=delay,willSpawn=e.firstClueGuarantee&&!save.capturedRare.Contains(e.speciesId)||rng.NextDouble()<e.probability,position=world.RareSpawnPoint(rng.Next(5),e.speciesId)});
            }
        }
        void AdvanceAdditionalRare(){
            var j=save.job;PrepareAdditionalRare();
            foreach(var plan in j.rarePlans){
                if(plan.spawned)continue;
                if(j.animals.Any(a=>a.species==plan.species)){plan.spawned=true;continue;}
                if(!plan.willSpawn||j.nightSeconds<plan.at)continue;
                var v=world.cam.WorldToViewportPoint(plan.position);if(Vector3.Distance(world.controller.transform.position,plan.position)<8||v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1&&world.ClearLine(world.cam.transform.position,plan.position))continue;
                var a=new AnimalRecord{id=j.runId+"-rare-"+plan.species,runId=j.runId,species=plan.species,position=plan.position};j.animals.Add(a);world.CreateAnimal(a);plan.spawned=true;Notify(L("发现了新的动物活动痕迹","New animal activity detected"));Persist();
            }
        }
    }
    public partial class DemoWorld {
        public Vector3 RareSpawnPoint(int index,string species=null){if(ActiveMap==0)return new[]{new Vector3(23,.08f,20),new Vector3(-23,.08f,20),new Vector3(20,.08f,-15),new Vector3(-23,.08f,-15),new Vector3(20,.08f,22)}[index];if(ActiveMap==4&&(species=="C04"||species=="C09"))return new Vector3(index%2==0?-16:-8,1.68f,11);return ClosestNode(FeedingPoints[index]+new Vector3(-4,0,3));}
    }
}
