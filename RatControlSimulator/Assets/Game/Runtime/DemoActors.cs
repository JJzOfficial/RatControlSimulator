using System.Collections.Generic;
using UnityEngine;

namespace Riverbend
{
    public partial class DemoAnimal : MonoBehaviour
    {
        public AnimalRecord record;public DemoWorld world;
        public string State {get;private set;}="Observe";
        Transform body;List<Vector3> route=new List<Vector3>();int pathIndex,patrol;float think,pause,bite,rest,alarm;Vector3 goal;DemoDevice lure;int food=-1;bool entering;float walkLeft=3,hideLeft,lureWait,stuck;Vector3 lastPosition;readonly Queue<Vector3> recent=new Queue<Vector3>();
        public void MakeModel()
        {
            float scale=record.species=="M02"?1.45f:1;
            body=new GameObject("Body pose").transform;body.SetParent(transform,false);
            if(AdditionalSpecies){BuildAdditionalAnimalModel();scale=Mathf.Max(1,Definition.lengthM/.48f);}
            else {
            world.Shape("Fur body",PrimitiveType.Sphere,new Vector3(0,.075f,0),new Vector3(.18f,.14f,.28f)*scale,world.fur,body);
            world.Shape("Head",PrimitiveType.Sphere,new Vector3(0,.08f,.15f)*scale,new Vector3(.1f,.09f,.12f)*scale,world.fur,body);
            world.Shape("Nose",PrimitiveType.Sphere,new Vector3(0,.06f,.22f)*scale,Vector3.one*.026f*scale,world.pink,body);
            foreach(float x in new[]{-.05f,.05f}) {
                world.Shape("Ear",PrimitiveType.Sphere,new Vector3(x,.14f,.12f)*scale,new Vector3(.05f,.055f,.018f)*scale,world.pink,body);
                world.Shape("Eye",PrimitiveType.Sphere,new Vector3(x*.8f,.1f,.19f)*scale,Vector3.one*.015f*scale,world.dark,body);
                foreach(float z in new[]{-.08f,.07f})world.Shape("Paw",PrimitiveType.Sphere,new Vector3(x,.015f,z)*scale,new Vector3(.035f,.02f,.05f)*scale,world.pink,body);
            }
            if(record.species!="C01")for(int i=0;i<7;i++)world.Shape("Tail",PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*.7f)*.025f,.015f,-.15f-i*.033f)*scale,new Vector3(.022f,.02f,.05f)*scale,world.pink,body);
            else for(int i=0;i<48;i++){float a=i*2.399f;world.Shape("Quill",PrimitiveType.Capsule,new Vector3(Mathf.Cos(a)*.07f,.13f,Mathf.Sin(a)*.1f),new Vector3(.009f,.05f,.009f),world.cream,body);}
            }
            gameObject.AddComponent<DemoTarget>().kind="animal";
            var c=gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.1f,0);c.size=new Vector3(.25f,.23f,.48f)*scale;gameObject.layer=10;
            if(AdditionalSpecies){float length=Definition.lengthM,height=Definition.family=="Snake"?.12f:length*.6f;c.center=Vector3.up*height*.5f;c.size=new Vector3(Definition.family=="Snake"?.15f:length*.48f,height,length);}
            if(record.dead)body.localRotation=Quaternion.Euler(0,0,85);
            patrol=Mathf.Abs(record.id.GetHashCode()%5);if(AdditionalSpecies){if(!record.behaviorSaved){walkLeft=AdditionalRandom(2,5);pause=AdditionalRandom(1,3);if(record.boldness<=0)record.boldness=AdditionalRandom(.25f,1);if(record.hunger<=0)record.hunger=AdditionalRandom(.2f,.8f);lureWait=Mathf.Max(record.caution,AdditionalRandom(.5f,1.5f));}}else{walkLeft=Random.Range(2f,5f);pause=Random.Range(1f,3f);if(record.boldness<=0)record.boldness=Random.Range(.25f,1f);if(record.hunger<=0)record.hunger=Random.Range(.2f,.8f);lureWait=Mathf.Max(record.caution,Random.Range(.5f,1.5f));}lastPosition=transform.position;
        }
        public bool Threat()
        {
            var g=world.game;Vector3 p=world.controller.transform.position+Vector3.up*.6f;Vector3 eye=transform.position+Vector3.up*.12f;float d=Vector3.Distance(eye,p);
            if(!world.ClearLine(eye,p))return false;
            bool sight=d<6&&(d<1.5f||Vector3.Angle(transform.forward,p-eye)<70);
            bool sound=g.Noise>0.05f&&d<g.Noise*8;
            Vector3 camera=world.cam.transform.position;
            bool light=world.torch.enabled&&Vector3.Distance(camera,eye)<world.torch.range&&Vector3.Angle(world.cam.transform.forward,eye-camera)<world.torch.spotAngle*.5f&&world.ClearLine(camera,eye);
            return sight||sound||light;
        }
        void Repath(Vector3 destination)
        {
            goal=destination;route=world.Route(transform.position,destination,record.species=="C01",AdditionalSpecies?(Definition.size=="Large"?.6f:Definition.size=="Medium"?.3f:.12f):0);pathIndex=0;
            if(route.Count>0&&Vector3.Distance(transform.position,route[0])<.2f)pathIndex=1;
        }
        void Update()
        {
            var g=world.game;if(g.save.job==null||g.Paused||record.dead||record.contained||record.loaded||record.terminal)return;
            if(record.partnerHeld&&!string.IsNullOrEmpty(record.partnerClaim))return;
            if(record.drivenUntil>g.save.job.nightSeconds&&g.save.job.contractIndex>0&&g.config.content.contracts[g.save.job.contractIndex].type=="Protect"){var b=world.PlayBounds;var p=transform.position;if(p.x<b.xMin+3||p.x>b.xMax-3||p.z<b.yMin+3||p.z>b.yMax-3){record.drivenOff=true;record.terminal=true;world.animals.Remove(record.id);Destroy(gameObject);g.Notify(g.L("目标已越过驱离边界","Target crossed the drive-off boundary"));return;}}
            if(record.poisonRemaining>0){record.poisonRemaining=Mathf.Max(0,record.poisonRemaining-Time.deltaTime);if(record.poisonRemaining<=0){Kill();return;}}
            if(AdvanceDogDrive(Time.deltaTime))return;
            if(AdditionalSpecies){UpdateAdditionalAnimal();return;}
            if(g.Phase!=ShiftPhase.Night&&record.species!="C01")return;
            float dt=Time.deltaTime;think-=dt;rest-=dt;record.hunger=Mathf.Min(1,record.hunger+dt*.002f);
            bool threatened=record.species!="C01"&&Threat();
            if(threatened){if(g.save.job.contractIndex>0&&alarm<=0)g.save.job.fleeEvents++;hideLeft=Random.Range(3f,6f);lureWait=Random.Range(20f,65f);alarm=2.5f;record.caution=lureWait;bite=0;food=-1;lure=null;entering=false;pause=0;}
            else alarm=Mathf.Max(0,alarm-dt);
            float speed=record.species=="C01"?.55f:1.35f;
            if(alarm>0) {
                State="Flee";speed=record.species=="M02"?4.2f:3.8f;
                if(think<=0){think=.8f;var dest=world.FleeDestination(this,recent);Repath(dest);recent.Enqueue(dest);while(recent.Count>4)recent.Dequeue();}
            } else {
                if(hideLeft>0){hideLeft-=dt;State="Hide";record.position=transform.position;return;}
                walkLeft-=dt;
                if(pause<=0&&walkLeft<=0&&!entering){pause=Random.Range(1f,3f);walkLeft=Random.Range(2f,5f);}
                if(lure!=null&&!g.LureAllowed(lure)){lure=null;entering=false;route.Clear();pathIndex=0;lureWait=Random.Range(20f,60f);}
                lureWait=Mathf.Max(0,lureWait-dt);
                if(think<=0) {
                    think=1.5f;
                    if(lure==null||lure.record.triggered||!g.LureAllowed(lure)) {
                        lure=null;entering=false;float best=9f*g.SkillValue("baitRadiusMultiplier",1);
                        foreach(var d in world.devices.Values)if((g.save.job.contractIndex==0||d.CanFit(record))&&(record.species!="C01"||(Mathf.Abs(d.transform.position.x)>9||d.transform.position.z>11))&&d.record.baited&&!d.record.triggered&&g.LureAllowed(d)) {
                            float dist=Vector3.Distance(transform.position,d.transform.position);
                            if(lureWait<=0&&dist<best&&world.Route(transform.position,d.Mouth,record.species=="C01").Count>0&&world.ClearLine(transform.position+Vector3.up*.15f,d.transform.position+Vector3.up*.15f)){best=dist;lure=d;}
                        }
                        if(lure){food=-1;pause=Random.Range(2f,6f);Repath(lure.Mouth);}
                    }
                    if(!lure&&pathIndex>=route.Count&&pause<=0) {
                        food=-1;
                        if(rest<=0&&record.species!="C01") {
                            for(int i=0;i<5;i++)if(g.save.job.durability[(patrol+i)%5]>0){int f=(patrol+i)%5;var path=world.Route(transform.position,world.FeedingPoints[f]);if(path.Count>0){food=f;route=path;pathIndex=0;goal=world.FeedingPoints[f];patrol=f+1;break;}}
                        }
                        if(food<0){Vector3[] spots={new Vector3(-6,.15f,3),new Vector3(6,.15f,3),new Vector3(0,3.36f,9.5f),new Vector3(-3,3.36f,3.4f),new Vector3(4,3.36f,6.5f),new Vector3(10,.08f,13)};if(record.species=="C01"){Vector3[] outside={new Vector3(21,.08f,20),new Vector3(12,.08f,20),new Vector3(20,.08f,12),new Vector3(22,.08f,-10)};Repath(outside[patrol++%outside.Length]);}else Repath(spots[patrol++%spots.Length]);}
                    }
                }
                if(lure&&pause<=0&&pathIndex>=route.Count) {
                    if(!entering&&Vector3.Distance(transform.position,lure.Mouth)<.5f){
                        if(Random.value>record.boldness*record.hunger){lure=null;entering=false;pause=Random.Range(1f,3f);lureWait=Random.Range(20f,65f);record.caution=lureWait;think=0;}
                        else {entering=true;goal=lure.transform.TransformPoint(new Vector3(0,.09f,-.12f));}
                    }
                    if(lure&&entering&&(record.species!="C01"||Mathf.Abs(goal.x)>8.6f||goal.z>10.6f||goal.z< -2.6f)&&world.ClearLine(transform.position+Vector3.up*.15f,goal+Vector3.up*.15f))transform.position=Vector3.MoveTowards(transform.position,goal,speed*dt);
                }
                if(food>=0&&Vector3.Distance(transform.position,world.FeedingPoints[food])<.7f) {
                    State="Gnaw";bite+=dt;body.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*22)*9,0,0);
                    if(bite>=g.config.furnitureDamage.biteSeconds){int index=food;food=-1;bite=0;rest=g.config.furnitureDamage.minimumAttackInterval;route.Clear();pathIndex=0;g.DamageFurniture(index,record);return;}
                } else if(pause>0){State="Observe";pause-=dt;body.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*4)*25,0);}
                else State=lure?"Approach":"Roam";
            }
            if(record.sedatedUntil>g.save.job.nightSeconds)speed*=.25f;
            if(State!="Gnaw"&&pause<=0&&pathIndex<route.Count) {
                Vector3 n=route[pathIndex];Vector3 dir=n-transform.position;Vector3 flat=dir;flat.y=0;
                if(flat.sqrMagnitude>.001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(flat),dt*12);
                if(world.ClearLine(transform.position+Vector3.up*.46f,n+Vector3.up*.46f))transform.position=Vector3.MoveTowards(transform.position,n,speed*dt);
                else {route.Clear();think=0;}
                body.localPosition=Vector3.up*Mathf.Sin(Time.time*(alarm>0?35:20))*.014f;
                if(Vector3.Distance(transform.position,n)<.04f){pathIndex++;if(pathIndex>=route.Count&&!lure&&food<0)pause=Random.Range(1f,3f);}
            }
            if(pathIndex<route.Count&&pause<=0&&State!="Gnaw"){if(Vector3.Distance(lastPosition,transform.position)<.005f)stuck+=dt;else stuck=0;if(stuck>1.2f){route.Clear();pathIndex=0;think=0;stuck=0;}}
            record.caution=lureWait;lastPosition=transform.position;
            record.position=transform.position;
            foreach(var d in world.devices.Values)d.TryEnter(this);
        }
        public void Kill()
        {
            if(record.dead||record.contained||record.terminal)return;if(world.game.save.job.contractIndex>0&&!record.attackRegistered){record.attackRegistered=true;if(record.required&&!record.identified)world.game.save.job.identifiedBeforeAttack=false;}record.dead=true;body.localRotation=Quaternion.Euler(0,0,85);world.game.Sound("hit");world.game.Notify(world.game.L("留下了可拾取尸体","Remains can be collected"));
        }
        public void Contain(string id)
        {
            if(record.dead||record.contained||record.terminal)return;record.contained=true;record.deviceId=id;
            transform.position=world.devices[id].transform.position+Vector3.up*.08f;record.position=transform.position;
            if(record.species=="C01")world.game.save.rareCaught=true;world.game.Notify(world.game.L("已捕获 · E 拾取 / 长按 Q 调查","Caught · E collect / hold Q inspect"));
            if(record.species.StartsWith("C")&&!world.game.save.capturedRare.Contains(record.species))world.game.save.capturedRare.Add(record.species);
        }
    }
    public partial class DemoDevice : MonoBehaviour
    {
        public DeviceRecord record;public DemoWorld world;Transform door;
        readonly HashSet<string> entered=new HashSet<string>();readonly Dictionary<string,Vector3> lastEntry=new Dictionary<string,Vector3>();
        public Vector3 Mouth=>transform.TransformPoint(new Vector3(0,.09f,.55f));
        public void MakeModel()
        {
            if(BuildAdditionalDevice())return;
            if(record.kit==(int)Kit.MediumCage)transform.localScale=new Vector3(2.2f,2.2f,2.2f);
            if(record.kit==(int)Kit.LargeCage)transform.localScale=new Vector3(4.3f,4.3f,4.3f);
            world.Box("Trap base",new Vector3(0,.035f,0),record.cage?new Vector3(.43f,.035f,.7f):new Vector3(.18f,.025f,.3f),world.wood,transform,false);
            if(record.cage) {
                for(int i=0;i<6;i++)foreach(float x in new[]{-.22f,.22f})world.Box("Cage bars",new Vector3(x,.2f,-.3f+i*.12f),new Vector3(.015f,.36f,.015f),world.metal,transform,false);
                for(int i=0;i<4;i++){world.Box("Cage rear",new Vector3(0,.07f+i*.1f,-.35f),new Vector3(.45f,.015f,.015f),world.metal,transform,false);world.Box("Roof bar",new Vector3(-.2f+i*.135f,.39f,0),new Vector3(.015f,.015f,.7f),world.metal,transform,false);}
                door=new GameObject("Cage door").transform;door.SetParent(transform,false);door.localPosition=new Vector3(0,.39f,.35f);
                for(int i=0;i<4;i++)world.Box("Door bars",new Vector3(-.2f+i*.135f,-.17f,0),new Vector3(.015f,.35f,.015f),world.metal,door,false);
                door.localRotation=Quaternion.Euler(record.triggered?0:-100,0,0);
            } else world.Box("Snap arm",new Vector3(0,.07f,0),new Vector3(.16f,.02f,.2f),world.metal,transform,false).transform.localRotation=Quaternion.Euler(record.triggered?150:0,0,0);
            if(record.baited)world.Shape("Bait",PrimitiveType.Sphere,new Vector3(0,.07f,-.15f),Vector3.one*.06f,world.amber,transform);
            gameObject.AddComponent<DemoTarget>().kind="device";var c=gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.2f,0);c.size=new Vector3(.48f,.42f,.75f);c.isTrigger=true;
        }
        public void TryEnter(DemoAnimal a)
        {
            if(TryAdditionalEntry(a))return;
            if(record.triggered||a.record.dead||a.record.contained||a.record.loaded)return;
            var p=transform.InverseTransformPoint(a.transform.position);
            if(Mathf.Abs(p.x)>.25f||p.z>.75f||p.z<-.4f||Mathf.Abs(p.y)>.3f)entered.Remove(a.record.id);
            if(Mathf.Abs(p.y)>.25f||Mathf.Abs(p.x)>.19f)return;
            if(!record.cage){if(Mathf.Abs(p.z)<.13f)Trigger(a);return;}
            if(lastEntry.TryGetValue(a.record.id,out var previous)&&previous.z>.35f&&p.z<=.35f&&p.z>.16f&&Mathf.Abs(previous.x)<.19f&&Vector3.Distance(previous,p)<.3f)entered.Add(a.record.id);
            lastEntry[a.record.id]=p;
            if(entered.Contains(a.record.id)&&p.z<.16f&&p.z>-.28f)Trigger(a);
        }
        public void Trigger(DemoAnimal a)
        {
            if(record.triggered||a.record.dead||a.record.contained||a.record.terminal)return;
            if(world.game.save.job.contractIndex>0&&!CanFit(a.record))return;
            record.triggered=true;record.animalId=a.record.id;
            a.record.controlMethod=record.cage?"Cage":"Trap";
            if(record.cage){a.Contain(record.id);if(door)door.localRotation=Quaternion.identity;}else a.Kill();world.game.Sound("hit");
        }
    }
}
