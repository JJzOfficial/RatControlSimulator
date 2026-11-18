using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoDevice {
        public RenderTexture feed;Camera monitorCamera;
        bool BuildAdditionalDevice(){
            if(record.kit==(int)Kit.Net){world.Shape("Occupied net bag",PrimitiveType.Sphere,new Vector3(0,.25f,0),new Vector3(.5f,.4f,.65f),world.cream,transform);world.Box("Net hoop",new Vector3(0,.42f,0),new Vector3(.55f,.03f,.65f),world.teal,transform,false);var netCollider=gameObject.AddComponent<BoxCollider>();netCollider.isTrigger=true;netCollider.center=Vector3.up*.25f;netCollider.size=new Vector3(.55f,.5f,.7f);gameObject.AddComponent<DemoTarget>().kind="device";return true;}
            if(record.kit!=(int)Kit.Camera&&record.kit!=(int)Kit.BaitStation)return false;
            if(record.kit==(int)Kit.Camera){
                world.Box("Camera support",new Vector3(0,.6f,0),new Vector3(.07f,1.2f,.07f),world.wood,transform,false);
                world.Box("Monitoring camera",new Vector3(0,1.2f,0),new Vector3(.25f,.28f,.15f),world.foliage,transform,false);
                world.Shape("Monitoring lens",PrimitiveType.Sphere,new Vector3(0,1.24f,.1f),new Vector3(.09f,.09f,.04f),world.dark,transform);
                feed=new RenderTexture(640,360,24);feed.Create();var cameraRoot=new GameObject("Actual monitoring view");cameraRoot.transform.SetParent(transform,false);cameraRoot.transform.localPosition=new Vector3(0,1.24f,.18f);monitorCamera=cameraRoot.AddComponent<Camera>();monitorCamera.CopyFrom(world.cam);monitorCamera.targetTexture=feed;monitorCamera.fieldOfView=70;monitorCamera.aspect=16f/9;
            }else{
                world.Box("Enclosed poison station",new Vector3(0,.08f,0),new Vector3(.55f,.16f,.38f),world.dark,transform,false);
                foreach(float x in new[]{-.28f,.28f})world.Box("Station opening",new Vector3(x,.07f,0),new Vector3(.01f,.08f,.12f),world.teal,transform,false);
                if(record.baited)world.Shape("Enclosed poison",PrimitiveType.Sphere,new Vector3(0,.1f,0),Vector3.one*.06f,world.amber,transform);
            }
            gameObject.AddComponent<DemoTarget>().kind="device";var col=gameObject.AddComponent<BoxCollider>();col.isTrigger=true;col.center=Vector3.up*(record.kit==(int)Kit.Camera?.6f:.1f);col.size=record.kit==(int)Kit.Camera?new Vector3(.3f,1.4f,.3f):new Vector3(.6f,.2f,.4f);return true;
        }
        public bool CanFit(AnimalRecord animal){
            var spec=world.game.config.content.species.First(s=>s.id==animal.species);var kit=DemoGame.DeviceKit(record);
            if(kit==Kit.Camera)return false;
            if(kit==Kit.BaitStation)return animal.species=="M01"||animal.species=="M02"||animal.species=="M03"||animal.species=="M04";
            if(!record.cage)return animal.species=="M01"||animal.species=="M02"||animal.species=="M03"||animal.species=="M04";
            if(spec.family=="Snake"&&!world.game.save.snakeLiners.Contains(record.itemId))return false;
            return kit==Kit.LargeCage||kit==Kit.MediumCage&&spec.size!="Large"||kit==Kit.Cage&&spec.size=="Small";
        }
        bool TryAdditionalEntry(DemoAnimal animal){
            if(record.kit==(int)Kit.Camera)return true;
            if(record.kit!=(int)Kit.BaitStation)return false;
            if(!record.baited||record.triggered||animal.record.dead||animal.record.contained||!CanFit(animal.record))return true;
            var local=transform.InverseTransformPoint(animal.transform.position);if(Mathf.Abs(local.x)<.3f&&Mathf.Abs(local.z)<.2f&&Mathf.Abs(local.y)<.25f){animal.record.poisonRemaining=12;record.baited=false;record.triggered=true;record.animalId=animal.record.id;world.game.Notify(world.game.L("药饵已取食，等待生效","Poison bait eaten; effect pending"));}return true;
        }
        void Update(){
            if(record.kit!=(int)Kit.Camera)return;
            if(monitorCamera)monitorCamera.enabled=record.energy>0&&world.game.MonitoringFeedRequested(record.id);
            if(world.game.Paused)return;
            record.energy=Mathf.Max(0,record.energy-Time.deltaTime/world.game.SkillValue("deviceBatteryMultiplier",1));if(record.energy<=0)return;
            if(record.observed==null)record.observed=new System.Collections.Generic.List<string>();
            foreach(var a in world.animals.Values){if(a.record.dead||a.record.loaded||record.observed.Contains(a.record.id))continue;var delta=a.transform.position-transform.position;
                if(delta.magnitude<=18&&Vector3.Angle(transform.forward,delta)<35&&world.ClearLine(transform.position+Vector3.up*1.2f,a.transform.position+Vector3.up*.2f)){
                    record.observed.Add(a.record.id);a.record.identified=true;world.game.save.job.cameraPasses++;if(!world.game.save.job.cameraTargets.Contains(a.record.id))world.game.save.job.cameraTargets.Add(a.record.id);world.game.Notify(world.game.L("监测相机记录到动物","Monitoring camera recorded an animal"));
                }
            }
        }
        void OnDestroy(){if(feed){feed.Release();Destroy(feed);}}
    }
}
