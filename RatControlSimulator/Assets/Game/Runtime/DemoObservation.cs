using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        string observationTarget;float observationElapsed;
        void ObserveThroughOptics(bool held,float range,float dt){
            if(!held||!Physics.Raycast(world.cam.transform.position,world.cam.transform.forward,out var hit,range,~(1<<8),QueryTriggerInteraction.Collide)){observationTarget=null;observationElapsed=0;return;}
            var animal=hit.collider.GetComponentInParent<DemoAnimal>();if(!animal){observationTarget=null;observationElapsed=0;return;}
            if(observationTarget!=animal.record.id){observationTarget=animal.record.id;observationElapsed=0;}
            if(animal.record.identified)return;observationElapsed+=dt;float duration=1.5f*SkillValue("identifyInvestigateMultiplier",1);prompt=L("持续观察识别 ","Observing ")+Mathf.FloorToInt(Mathf.Clamp01(observationElapsed/duration)*100)+"%";
            if(observationElapsed>=duration){animal.record.identified=true;Notify(L("已识别：","Identified: ")+SpeciesName(animal.record.species)+" · "+Mathf.RoundToInt(hit.distance)+"m");Persist();}
        }
    }
}
