using System.Linq;
using UnityEngine;
namespace Riverbend {
    [System.Serializable] public class LivestockRecord {public string id;public float health=100,disturbance;public Vector3 position;}
    public class DemoLivestock:MonoBehaviour {
        public DemoWorld world;public string id;public int map;public bool pig;public Vector3 home;string run;LivestockRecord record;float tick,cueTime;
        void Bind(){var j=world.game.save.job;if(j==null||j.mapIndex!=map)return;if(run==j.runId&&record!=null)return;run=j.runId;record=j.livestock.FirstOrDefault(a=>a.id==id);if(record==null){record=new LivestockRecord{id=id,position=home};j.livestock.Add(record);}transform.position=record.position;}
        public void Hurt(float amount){Bind();if(record==null||record.health<=0)return;record.health=Mathf.Max(0,record.health-amount);world.game.Notify(world.game.L("误伤家畜 · 影响健康挑战","Livestock injured · health challenge affected"));}
        void Update(){if(!world||world.game.save.atDepot||world.game.Paused)return;Bind();if(record==null)return;if(record.health<=0){transform.rotation=Quaternion.Euler(0,0,80);return;}
            float dt=Time.deltaTime;tick+=dt;cueTime-=dt;bool dirty=world.game.save.job.pests.Any(p=>!p.dead&&Vector3.Distance(p.position,home)<12);var intruder=pig?null:world.animals.Values.FirstOrDefault(a=>!a.record.dead&&!a.record.contained&&!a.record.species.StartsWith("C")&&Vector3.Distance(a.transform.position,transform.position)<3);if(dirty||intruder){record.disturbance+=dt;transform.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*5)*14,Mathf.Sin(Time.time*12)*2);if(cueTime<=0){world.AnimalCue(pig?"Pig":"Chicken",transform.position,.4f);cueTime=pig?4:3;}if(dirty&&record.disturbance>=15){record.disturbance=0;record.health=Mathf.Max(0,record.health-1);}}else transform.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*.35f+home.x)*20,0);
            if(!pig){Vector3 goal=home+new Vector3(Mathf.Sin(tick*.3f+home.x),0,Mathf.Cos(tick*.2f+home.z))*.4f;if(intruder){var away=transform.position-intruder.transform.position;away.y=0;goal=home+Vector3.ClampMagnitude(transform.position+away.normalized-home,1.8f);}if(world.ClearLine(transform.position+Vector3.up*.2f,goal+Vector3.up*.2f))transform.position=Vector3.MoveTowards(transform.position,goal,dt*(intruder?2:.6f));}
            record.position=transform.position;
        }
    }
    public class DemoNest:MonoBehaviour {
        public DemoWorld world;public int index;
        void Update(){if(world.game.save.atDepot||world.game.save.job.mapIndex!=2)return;int remaining=Mathf.Clamp(world.game.save.job.durability[index]/10,0,10),egg=0;foreach(Transform child in transform)if(child.name=="Egg")child.gameObject.SetActive(egg++<remaining);}
    }
}
