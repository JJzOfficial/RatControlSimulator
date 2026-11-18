using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoAnimal {
        bool AdditionalSpecies=>record.species!="M01"&&record.species!="M02"&&record.species!="C01";
        DemoCatalog.Species Definition=>world.game.config.content.species.First(s=>s.id==record.species);
        bool StealingAnimal=>Definition.family=="SneakThief"||record.species=="M14"&&world.game.save.job.mapIndex==2;
        float specialTime,specialCooldown,hiddenTime,lightExposure;
        Vector3 specialOrigin,specialDirection,baseBodyOffset;
        bool specialActive;
        float AdditionalRandom(float minimum,float maximum){uint value=record.randomState;if(value==0){value=2166136261;foreach(char c in record.id)value=(value^c)*16777619;}value^=value<<13;value^=value>>17;value^=value<<5;record.randomState=value==0?1:value;return Mathf.Lerp(minimum,maximum,(record.randomState&0xffffff)/16777216f);}
        public void RestoreAdditionalLure(){if(!AdditionalSpecies||!record.behaviorSaved)return;lure=string.IsNullOrEmpty(record.aiLureId)?null:world.devices.TryGetValue(record.aiLureId,out var device)?device:null;entering=lure&&record.aiEntering;}
        public void RestoreAdditionalBehavior(){if(!AdditionalSpecies||!record.behaviorSaved)return;alarm=record.aiAlarm;pause=record.aiPause;walkLeft=record.aiWalk;food=record.aiFood;specialTime=record.aiSpecial;specialCooldown=record.aiCooldown;lightExposure=record.aiLight;lureWait=record.aiWait;bite=record.aiBite;rest=record.aiRest;think=record.aiThink;specialActive=record.aiActive;specialOrigin=record.aiOrigin;specialDirection=record.aiDirection;if(record.aiHasGoal)Repath(record.aiGoal);if(specialActive&&Definition.family=="Burrow")foreach(var r in body.GetComponentsInChildren<Renderer>())r.enabled=false;}
        public void SaveAdditionalBehavior(){if(!AdditionalSpecies)return;record.behaviorSaved=true;record.aiAlarm=alarm;record.aiPause=pause;record.aiWalk=walkLeft;record.aiFood=food;record.aiSpecial=specialTime;record.aiCooldown=specialCooldown;record.aiLight=lightExposure;record.aiWait=lureWait;record.aiBite=bite;record.aiRest=rest;record.aiThink=think;record.aiLureId=lure?lure.record.id:null;record.aiEntering=entering;record.aiActive=specialActive;record.aiOrigin=specialOrigin;record.aiDirection=specialDirection;record.aiHasGoal=pathIndex<route.Count;record.aiGoal=goal;}
        public void PushFromSpear(){if(specialCooldown>0)return;specialActive=false;specialCooldown=3;alarm=3;Repath(world.FleeDestination(this,recent));}
        void BuildAdditionalAnimalModel(){
            var d=Definition;float length=d.lengthM;string family=d.family;
            if(family=="Snake"){
                for(int i=0;i<14;i++)world.Shape("Snake segment",PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*.6f)*length*.08f,.04f,-length*.5f+i*length/14),new Vector3(length*.1f,.07f,length*.12f),world.foliage,body);
                world.Shape("Triangular snake head",PrimitiveType.Sphere,new Vector3(0,.055f,length*.5f),new Vector3(length*.12f,.08f,length*.13f),world.fur,body);
                if(record.species=="S02")for(int i=0;i<4;i++)world.Shape("Rattle",PrimitiveType.Sphere,new Vector3(0,.07f,-length*.52f-i*.025f),Vector3.one*.055f,world.amber,body);
                return;
            }
            bool deer=record.species=="C07",runner=family=="OpenRunner"&&!deer,canid=family=="Canid",heavy=family=="Heavy",curl=family=="Curl",burrow=family=="Burrow";
            float h=length*(heavy?.55f:canid?.5f:runner?.48f:.35f),width=length*(heavy?.5f:curl?.44f:.3f);
            var coat=record.species=="M13"||record.species=="C06"?world.dark:record.species=="C11"?world.cream:record.species=="M14"||record.species=="C12"?world.amber:world.fur;
            world.Shape("Species body",PrimitiveType.Sphere,new Vector3(0,h*.65f,0),new Vector3(width,h*.85f,length*.65f),coat,body);
            world.Shape("Species head",PrimitiveType.Sphere,new Vector3(0,h*.82f,length*.34f),new Vector3(width*.65f,h*.65f,length*.27f),coat,body);
            world.Shape(heavy?"Boar snout":"Muzzle",PrimitiveType.Sphere,new Vector3(0,h*.67f,length*.49f),new Vector3(width*.45f,h*.3f,length*.2f),heavy?world.pink:coat,body);
            foreach(float side in new[]{-1f,1f}){
                world.Shape(runner?"Long ear":"Pointed ear",PrimitiveType.Capsule,new Vector3(side*width*.25f,h*1.2f,length*.34f),new Vector3(width*.2f,h*(runner?.65f:.22f),width*.17f),coat,body);
                world.Shape("Eye",PrimitiveType.Sphere,new Vector3(side*width*.32f,h*.91f,length*.42f),Vector3.one*length*.04f,world.dark,body);
                foreach(float z in new[]{-.2f,.23f})world.Shape(burrow?"Digging paw":"Leg",PrimitiveType.Capsule,new Vector3(side*width*.34f,h*.23f,z*length),new Vector3(width*.2f,h*.25f,width*.23f),coat,body);
                if(heavy)world.Shape("Tusk",PrimitiveType.Capsule,new Vector3(side*width*.24f,h*.6f,length*.48f),new Vector3(.04f,.12f,.04f),world.cream,body);
            }
            float tail=canid||record.species=="C04"?.55f:runner?.12f:.35f;
            world.Shape("Species tail",PrimitiveType.Capsule,new Vector3(0,h*.65f,-length*.48f),new Vector3(width*.4f,length*tail*.5f,width*.4f),coat,body).transform.localRotation=Quaternion.Euler(canid?65:35,0,0);
            if(record.species=="M11")for(int i=0;i<4;i++)world.Shape("Raccoon tail ring",PrimitiveType.Sphere,new Vector3(0,h*.65f-i*.02f,-length*(.43f+i*.04f)),new Vector3(width*.43f,.06f,.06f),world.dark,body);
            if(record.species=="M13")world.Box("Skunk white stripe",new Vector3(0,h*1.03f,-length*.08f),new Vector3(width*.22f,.025f,length*.62f),world.cream,body,false);
            if(record.species=="C02")for(int i=0;i<18;i++)world.Shape("Overlapping scale",PrimitiveType.Sphere,new Vector3(Mathf.Sin(i*2.4f)*width*.38f,h*.9f,-length*.25f+(i/6)*length*.2f),new Vector3(width*.24f,.045f,length*.15f),world.wood,body);
            if(deer){foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-.2f,.23f})world.Shape("Long deer leg",PrimitiveType.Capsule,new Vector3(side*width*.3f,-h*.1f,z*length),new Vector3(.07f,h*.75f,.07f),coat,body);world.Shape("Raised deer neck",PrimitiveType.Capsule,new Vector3(0,h*1.1f,length*.3f),new Vector3(width*.55f,h*.6f,width*.55f),coat,body);body.localPosition=Vector3.up*h*.25f;}
            if(record.species=="C03")for(int i=0;i<16;i++)world.Shape("Cat coat spot",PrimitiveType.Sphere,new Vector3((i%2==0?-1:1)*width*.48f,h*(.45f+.13f*(i%3)),-length*.23f+(i/4)*length*.13f),new Vector3(.018f,.04f,.05f),world.dark,body);
            if(record.species=="C06")foreach(float side in new[]{-1f,1f})world.Box("Badger facial stripe",new Vector3(side*width*.18f,h*.99f,length*.37f),new Vector3(width*.16f,.025f,length*.25f),world.cream,body,false);
            if(record.species=="C08")world.Box("Mouse dorsal black line",new Vector3(0,h*1.1f,-length*.06f),new Vector3(width*.16f,.015f,length*.55f),world.dark,body,false);
            if(record.species=="C10")world.Shape("Marten pale throat",PrimitiveType.Sphere,new Vector3(0,h*.64f,length*.37f),new Vector3(width*.46f,h*.4f,.03f),world.cream,body);
            if(record.species=="C12")foreach(float side in new[]{-1f,1f})world.Shape("Very large fennec ear",PrimitiveType.Capsule,new Vector3(side*width*.28f,h*1.38f,length*.34f),new Vector3(width*.37f,h*.8f,.06f),world.amber,body);
            if(record.species=="M04"||record.species=="M05"||record.species=="C05"){var oldTail=body.Find("Species tail");if(oldTail)oldTail.localScale*=.35f;}
            if(record.species=="M05"||record.species=="C05")foreach(float side in new[]{-1f,1f})world.Box("Broad digging forepaw",new Vector3(side*width*.55f,h*.15f,length*.26f),new Vector3(width*.5f,.03f,length*.22f),record.species=="C05"?world.pink:coat,body,false);
            if(record.species=="M03"){var oldTail=body.Find("Species tail");if(oldTail)oldTail.localScale=new Vector3(width*.12f,length*.45f,width*.12f);}
            if(record.species=="C04"||record.species=="C09")world.Shape("Raised bushy tail",PrimitiveType.Sphere,new Vector3(0,h*1.4f,-length*.35f),new Vector3(width*.7f,length*.7f,width*.65f),coat,body);
        }
        void UpdateAdditionalAnimal(float step=-1){
            var g=world.game;var j=g.save.job;var d=Definition;float dt=step>=0?step:Time.deltaTime;
            if(g.Phase!=ShiftPhase.Night&&!record.species.StartsWith("C"))return;
            specialCooldown=Mathf.Max(0,specialCooldown-dt);think-=dt;pause-=dt;rest-=dt;lureWait=Mathf.Max(0,lureWait-dt);
            float distance=Vector3.Distance(transform.position,world.controller.transform.position);
            var eye=transform.position+Vector3.up*.15f;var playerEye=world.controller.transform.position+Vector3.up*.6f;bool visible=world.ClearLine(eye,playerEye);
            bool lit=world.torch.enabled&&Vector3.Distance(world.cam.transform.position,eye)<world.torch.range&&Vector3.Angle(world.cam.transform.forward,eye-world.cam.transform.position)<world.torch.spotAngle*.5f&&world.ClearLine(world.cam.transform.position,eye);lightExposure=lit?lightExposure+dt:Mathf.Max(0,lightExposure-dt*2);
            bool threat=visible&&distance<d.visionM&&(distance<1.5f||Vector3.Angle(transform.forward,world.controller.transform.position-transform.position)<d.visionFovDeg*.5f)||distance<g.Noise*d.hearingM*(visible?1:.35f)||lightExposure>=2;
            if(AdditionalSpecial(threat,distance,dt)){record.position=transform.position;return;}
            if(pause>0&&!threat&&alarm<=0&&food<0){State=d.family=="OpenRunner"?"Head up":"Observe";record.position=transform.position;return;}
            float speed=d.walkSpeed;
            if(threat){
                if(alarm<=0)j.fleeEvents++;alarm=4;speed=d.fleeSpeed;
                if(think<=0){Repath(world.FleeDestination(this,recent));think=1;}
            }else alarm=Mathf.Max(0,alarm-dt);
            if(alarm>0)speed=d.fleeSpeed;
            if(record.sedatedUntil>j.nightSeconds)speed*=.25f;
            if(record.stolenSource>=0&&!record.stolenLost&&!record.stolenRecovered){if(think<=0){think=2;Repath(world.AdditionalGroundNode(new Vector3(world.PlayBounds.xMin+3,transform.position.y,transform.position.z)));}food=-1;lure=null;}
            if(think<=0){
                think=1;
                if(alarm<=0&&(record.stolenSource<0||record.stolenLost||record.stolenRecovered)){
                    var nextLure=world.devices.Values.Where(device=>lureWait<=0&&device.record.baited&&!device.record.triggered&&device.CanFit(record)&&g.LureAllowed(device)&&Vector3.Distance(device.transform.position,transform.position)<9*g.SkillValue("baitRadiusMultiplier",1)&&world.ClearLine(transform.position+Vector3.up*.15f,device.Mouth)).OrderBy(device=>Vector3.Distance(device.transform.position,transform.position)).FirstOrDefault();
                    if(nextLure!=lure){lure=nextLure;entering=false;route.Clear();pathIndex=0;}
                    if(lure&&lureWait<=0){if(!entering&&(route.Count==0||pathIndex>=route.Count&&Vector3.Distance(transform.position,lure.Mouth)>=.5f))Repath(lure.Mouth);food=-1;}
                    else if(pathIndex>=route.Count){food=record.species.StartsWith("C")||d.family=="Snake"||j.mapIndex==5?-1:patrol++%5;var destination=world.FeedingPoints[food<0?patrol++%5:food];Repath(destination);}
                }
            }
            State=alarm>0?"Flee":lure?"Approach":food>=0?"Forage":"Roam";
            AdditionalMotionCue(dt);
            var mouthDelta=lure?lure.Mouth-transform.position:Vector3.zero;mouthDelta.y=0;
            if(lure&&alarm<=0&&(entering||mouthDelta.magnitude<.75f)){
                if(!entering&&Vector3.Distance(transform.position,lure.Mouth)<.04f)entering=true;
                var enter=entering?lure.transform.TransformPoint(new Vector3(0,.09f,-.12f)):lure.Mouth;if(world.ClearLine(transform.position+Vector3.up*.15f,enter+Vector3.up*.15f))transform.position=Vector3.MoveTowards(transform.position,enter,speed*dt);
            }else if(pathIndex<route.Count){
                Vector3 next=route[pathIndex],dir=next-transform.position;dir.y=0;if(dir.sqrMagnitude>.001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),dt*9);
                if(world.ClearLine(transform.position+Vector3.up*.46f,next+Vector3.up*.46f))transform.position=Vector3.MoveTowards(transform.position,next,speed*dt);else{route.Clear();think=0;}
                if(Vector3.Distance(transform.position,next)<.07f)pathIndex++;
            }
            if(food>=0&&alarm<=0&&rest<=0&&Vector3.Distance(transform.position,world.FeedingPoints[food])<.8f){
                State=StealingAnimal?"Steal":"Gnaw";bite+=dt;
                if(bite>=(StealingAnimal?1.2f:g.config.furnitureDamage.biteSeconds)){bite=0;if(StealingAnimal&&record.stolenSource<0){record.stolenSource=food;record.stolenAt=j.nightSeconds;world.CreateStolenObject(record);g.Notify(g.L("动物叼走物品：10秒内控制并追回","Item stolen: control and recover within 10 seconds"));}g.DamageFurniture(food,record);food=-1;route.Clear();think=0;pause=3;rest=10;}
            }
            if(alarm<=0&&!lure&&food<0){walkLeft-=dt;if(walkLeft<=0){pause=AdditionalRandom(1,3);walkLeft=AdditionalRandom(2,5);}}
            if(pathIndex<route.Count&&!entering&&pause<=0&&alarm<=0){stuck=Vector3.Distance(lastPosition,transform.position)<.002f?stuck+dt:0;if(stuck>=2){Repath(goal);stuck=0;}}lastPosition=transform.position;
            if(record.species=="C07")baseBodyOffset=Vector3.up*Definition.lengthM*.35f*.25f;body.localPosition=baseBodyOffset+Vector3.up*(Mathf.Sin(Time.time*15)*.01f);record.position=transform.position;
            foreach(var device in world.devices.Values)device.TryEnter(this);
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void VerifyNewBehaviorStep(float dt){UpdateAdditionalAnimal(dt);}
        #endif
        bool AdditionalSpecial(bool threat,float distance,float dt){
            var g=world.game;string id=record.species,family=Definition.family;
            if(specialActive){
                specialTime-=dt;
                if(id=="M16"){
                    State=specialTime>2?"Charge warning":"Charge";
                    if(specialTime<=2){var next=transform.position+specialDirection*5*dt;if(world.ClearLine(transform.position+Vector3.up*.5f,next+Vector3.up*.5f)&&Vector3.Distance(specialOrigin,next)<6)transform.position=next;else specialTime=0;
                        if(distance<1.3f){g.HurtPlayer(25,true);specialTime=0;}}
                }else if(id=="M17"){
                    State=specialTime>6?"Growl warning":"Chase";
                    if(specialTime<=6&&Vector3.Distance(transform.position,specialOrigin)<18){var next=Vector3.MoveTowards(transform.position,world.controller.transform.position,4.5f*dt);if(world.ClearLine(transform.position+Vector3.up*.5f,next+Vector3.up*.5f))transform.position=next;if(distance<1.5f){g.HurtPlayer(20,false);specialTime=0;}}
                }else if(id=="S02"){
                    State=specialTime>.1f?"Rattle warning":"Strike";if(specialTime<=0&&distance<1.5f)g.HurtPlayer(10,false);
                }else if(id=="M13"){
                    State="Tail raised";body.localRotation=Quaternion.Euler(0,180,0);if(specialTime<=0&&distance<3&&Vector3.Angle(specialDirection,world.controller.transform.position-transform.position)<=35&&world.ClearLine(transform.position+Vector3.up*.2f,world.controller.transform.position+Vector3.up*.5f)){g.SkunkSpray();}
                }else{State=id=="M12"?"Feign":family=="Curl"?"Curl":"Burrow";if(id=="M12")body.localRotation=Quaternion.Euler(0,0,85);if(family=="Curl"){body.localScale=new Vector3(1,.8f,.6f);body.localRotation=Quaternion.Euler(30,0,0);}}
                if(specialTime<=0){if(family=="Burrow"){var exits=world.FeedingPoints.Select(p=>world.AdditionalGroundNode(p+Vector3.right)).Where(p=>Vector3.Distance(p,specialOrigin)>4&&world.Route(specialOrigin,p).Count>0).ToArray();if(exits.Length>0){transform.position=exits[Mathf.Abs(record.id.GetHashCode()%exits.Length)];record.position=transform.position;route.Clear();pathIndex=0;think=0;}}specialActive=false;specialCooldown=id=="S02"||id=="M16"?2:8;body.localScale=Vector3.one;body.localRotation=Quaternion.identity;foreach(var r in body.GetComponentsInChildren<Renderer>())r.enabled=true;}
                return true;
            }
            if(specialCooldown>0||!threat)return false;
            if(id=="M16"&&distance<7||id=="M17"&&distance<8||id=="S02"&&distance<1.5f||id=="M13"&&distance<3||id=="M12"&&distance<3||family=="Curl"&&distance<3||family=="Burrow"&&distance<3){
                specialActive=true;specialOrigin=transform.position;specialDirection=(world.controller.transform.position-transform.position).normalized;specialDirection.y=0;
                specialTime=id=="M16"?3:id=="M17"?6.8f:id=="S02"?.8f:id=="M13"?1:5;
                world.AnimalCue(id=="M16"?"Boar":id=="M17"?"Wolf":id=="S02"?"Rattle":"Rustle",transform.position,.7f);
                if(family=="Burrow")foreach(var r in body.GetComponentsInChildren<Renderer>())r.enabled=false;
                g.Notify(id=="M16"?g.L("野猪低头预备冲撞","Boar lowers its head"):id=="M17"?g.L("狼正在低吼","Wolf growls"):id=="S02"?g.L("响尾蛇正在警告","Rattlesnake warns"):id=="M13"?g.L("臭鼬抬起尾巴","Skunk raises its tail"):g.L("动物进入防御状态","Animal takes a defensive posture"));return true;
            }
            return false;
        }
    }
    public partial class DemoGame {
        float hurtCooldown,skunkEffect;
        public void HurtPlayer(float amount,bool boar){if(hurtCooldown>0||save.atDepot||Vector3.Distance(world.controller.transform.position,DemoWorld.VanPoint)<4)return;hurtCooldown=2;var j=save.job;j.health=Mathf.Max(0,j.health-amount);j.dangerHits++;if(boar)j.boarHits++;Notify(L("受到攻击 · 生命 ","Hit · health ")+j.health);if(j.health<=0)FinalizeJob(false,true);}
        public void SkunkSpray(){save.job.skunkSprays++;skunkEffect=3;Notify(L("受到臭鼬喷雾干扰","Skunk spray interference"));}
    }
}
