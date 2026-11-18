using System;
using System.Linq;
using UnityEngine;

namespace Riverbend
{
    public class DemoLooseItem : MonoBehaviour { public LooseItemRecord record; }
    public partial class DemoWorld
    {
        public readonly System.Collections.Generic.List<DemoPest> pests=new System.Collections.Generic.List<DemoPest>();
        public void RestoreLooseItems()
        {
            foreach(var item in game.save.job.looseItems)CreateLooseItem(item);
            if((game.save.job.crateItems.Count>0)||!string.IsNullOrEmpty(game.save.job.deliveredItem))ShowDelivery();
        }
        public void CreateLooseItem(LooseItemRecord item)
        {
            var root=Box("Dropped "+(Kit)item.kit,item.position+Vector3.up*.15f,new Vector3(.28f,.25f,.45f),item.kit==0?amber:metal,dynamicRoot,false);
            root.AddComponent<BoxCollider>();root.AddComponent<DemoTarget>().kind="item";root.AddComponent<DemoLooseItem>().record=item;
            if(item.kit>=12){root.GetComponent<Renderer>().enabled=false;var model=new GameObject("Equipment display").transform;model.SetParent(root.transform,false);model.localScale=new Vector3(1/.28f,1/.25f,1/.45f);BuildAdditionalEquipment((Kit)item.kit,model,false);}
        }
        public void ShowDelivery()
        {
            var old=dynamicRoot.Find("Delivery crate");if(old)Destroy(old.gameObject);
            var box=Box("Delivery crate",VanPoint+new Vector3(2.6f,.3f,3),new Vector3(.65f,.6f,.65f),amber,dynamicRoot);box.AddComponent<DemoTarget>().kind="delivery";
        }
        public void RestorePests()
        {
            pests.Clear();var j=game.save.job;
            if(j.pests.Count==0&&j.contractIndex>0)CreateContractPestRecords();
            if(j.pests.Count==0&&j.contractIndex==0)for(int source=0;source<2;source++)for(int i=0;i<(source==0?UnityEngine.Random.Range(2,5):5);i++) {
                var p=j.workPositions[source]+new Vector3(Mathf.Sin(i*2.4f)*.65f,0,Mathf.Cos(i*2.4f)*.65f);p.y=source==0?.1f:1.3f+i*.12f;
                j.pests.Add(new PestRecord{id=j.runId+"-bug-"+source+"-"+i,species=source==0?"I01":"I05",source=source,position=p,dead=j.work[source]>=3});
            }
            foreach(var p in j.pests) {
                if(p.cleaned&&(game.save.encyclopedia.Contains(p.species)||pests.Any(x=>x.record.source==p.source&&x.record.cleaned)))continue;
                var root=new GameObject(p.species+"-"+p.id);root.transform.SetParent(dynamicRoot);root.transform.position=p.position;var bug=root.AddComponent<DemoPest>();bug.world=this;bug.record=p;bug.Build();pests.Add(bug);
            }
        }
    }
    public partial class DemoPest : MonoBehaviour
    {
        public DemoWorld world;public PestRecord record;
        Transform left,right;float seed;Vector3 home;
        public void Build()
        {
            seed=Mathf.Abs(record.id.GetHashCode()%1000);home=record.position;bool mosquito=record.species=="I05";
            if(record.species!="I01"&&record.species!="I05"){BuildAdditionalPest();return;}
            world.Shape("Insect body",PrimitiveType.Sphere,Vector3.zero,mosquito?new Vector3(.035f,.035f,.13f):new Vector3(.12f,.05f,.2f),world.fur,transform);
            world.Shape("Insect head",PrimitiveType.Sphere,new Vector3(0,.01f,.1f),Vector3.one*(mosquito?.045f:.07f),world.dark,transform);
            for(int i=0;i<3;i++)foreach(float side in new[]{-1f,1f}) {
                var leg=world.Box("Leg",new Vector3(side*.07f,-.015f,-.07f+i*.06f),new Vector3(.13f,.009f,.009f),world.dark,transform,false);leg.transform.localRotation=Quaternion.Euler(0,side*(i-1)*30,0);
            }
            if(mosquito) {
                left=world.Shape("Left wing",PrimitiveType.Sphere,new Vector3(-.08f,.02f,0),new Vector3(.16f,.012f,.08f),world.cream,transform).transform;
                right=world.Shape("Right wing",PrimitiveType.Sphere,new Vector3(.08f,.02f,0),new Vector3(.16f,.012f,.08f),world.cream,transform).transform;
                world.Box("Proboscis",new Vector3(0,0,.16f),new Vector3(.008f,.008f,.09f),world.dark,transform,false);
            } else foreach(float x in new[]{-.04f,.04f})world.Box("Antenna",new Vector3(x,0,.19f),new Vector3(.008f,.008f,.13f),world.dark,transform,false);
            if(mosquito)foreach(Transform child in transform){child.localPosition*=.25f;child.localScale*=.25f;}
            var c=gameObject.AddComponent<SphereCollider>();c.radius=mosquito?.17f:.16f;c.isTrigger=true;gameObject.AddComponent<DemoTarget>().kind="pest";
        }
        void Update()
        {
            if(world.game.Paused||world.game.save.job==null)return;
            if(record.cleaned){if(world.game.save.encyclopedia.Contains(record.species))gameObject.SetActive(false);return;}
            if(record.dead) {
                if(Physics.Raycast(transform.position+Vector3.up*.02f,Vector3.down,out var hit,3,1<<9))transform.position=Vector3.MoveTowards(transform.position,hit.point+Vector3.up*.035f,2.4f*Time.deltaTime);
                transform.rotation=Quaternion.Euler(0,seed,100);record.position=transform.position;return;
            }
            if(record.species!="I01"&&record.species!="I05"){UpdateAdditionalPest();return;}
            float t=Time.time+seed;Vector3 p;if(record.source==0&&Mathf.Repeat(t,3+seed%3)<1+seed%2)return;
            if(record.source==0){p=new Vector3(home.x+Mathf.Sin(t*.8f)*.4f,.105f,home.z+Mathf.Cos(t*.53f)*.35f);}
            else {p=new Vector3(home.x+Mathf.Sin(t*1.5f)*.8f,1.3f+Mathf.Sin(t*2.2f)*.4f,home.z+Mathf.Cos(t*1.3f)*.8f);if(left){left.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*80)*45);right.localRotation=Quaternion.Euler(0,0,-Mathf.Sin(t*80)*45);}}
            if(world.ClearLine(transform.position+Vector3.up*.04f,p+Vector3.up*.04f)){Vector3 dir=p-transform.position;if(dir.sqrMagnitude>.002f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),Time.deltaTime*5);transform.position=Vector3.MoveTowards(transform.position,p,Time.deltaTime*(record.source==0?.32f:.9f));}
            record.position=transform.position;
        }
        public void Dose(float dt)
        {
            if(world.game.save.job.contractIndex>0&&(record.species=="I11"||record.species=="I12"))return;
            if(record.dead)return;record.exposure+=dt;
            if(record.exposure<.65f)return;record.dead=true;world.game.Notify(world.game.L("害虫已消杀 · 源头仍需处理","Pest killed · source still needs treatment"));
            int source=record.source;var j=world.game.save.job;if(j.pests.Where(p=>p.source==source).All(p=>p.dead)&&j.work[source]<3)j.work[source]=1;
        }
    }
    public partial class DemoGame
    {
        ParticleSystem mist;float mistBudget;
        void Spray()
        {
            var j=save.job;if(j.spray<=0){if(mist)mist.Stop();return;}
            float dt=Time.deltaTime;j.spraySeconds+=dt;
            while(j.spraySeconds>=4&&j.spray>0){j.spray--;j.spraySeconds-=4;}if(j.spray<=0){j.spraySeconds=0;if(mist)mist.Stop();return;}
            Vector3 origin=world.cam.transform.position,forward=world.cam.transform.forward;float range=3.4f;
            if(Physics.Raycast(origin,forward,out var hit,range,1<<9))range=hit.distance;
            foreach(var pest in world.pests)if(pest&&!pest.record.dead) {
                Vector3 delta=pest.transform.position-origin;
                if(delta.magnitude<=range+.12f&&Vector3.Angle(forward,delta)<20&&world.ClearLine(origin,pest.transform.position))pest.Dose(dt*ServiceValue("E01.effectiveSpraySeconds",1));
            }
            if(!mist) {
                var obj=new GameObject("Continuous insecticide mist");obj.transform.SetParent(world.cam.transform,false);obj.transform.localPosition=new Vector3(.2f,-.1f,.4f);mist=obj.AddComponent<ParticleSystem>();
                var main=mist.main;main.playOnAwake=false;main.startLifetime=.5f;main.startSpeed=6;main.startSize=new ParticleSystem.MinMaxCurve(.025f,.11f);main.startColor=new Color(.73f,.86f,.8f,.6f);main.maxParticles=120;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=mist.emission;emission.rateOverTime=0;
                var shape=mist.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=17;shape.radius=.015f;
                var renderer=mist.GetComponent<ParticleSystemRenderer>();var material=new Material(world.cream);material.SetColor("_BaseColor",new Color(.72f,.83f,.76f,.16f));UnityEngine.Rendering.HighDefinition.HDMaterial.SetSurfaceType(material,true);UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(material);renderer.sharedMaterial=material;
            }
            var settings=mist.main;settings.startLifetime=Mathf.Max(.015f,(range-.4f)/6);mist.Play();mistBudget+=dt*100;
            SaveToolState();int count=Mathf.FloorToInt(mistBudget);mistBudget-=count;if(count>0)mist.Emit(count);
        }
    }
}
