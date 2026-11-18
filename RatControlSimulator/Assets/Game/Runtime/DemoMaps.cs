using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Riverbend {
    // Additional job sites use the accepted world's builders, truck and targets.
    public partial class DemoWorld {
        class JobSite {
            public Transform root,cargo;
            public List<DemoTarget> work=new List<DemoTarget>();
            public Vector3[] food;
        }
        readonly Dictionary<int,JobSite> jobSites=new Dictionary<int,JobSite>();
        public int ActiveMap {get;private set;}
        public Vector3 AdditionalGroundNode(Vector3 position)=>ClosestNode(position);
        public float NavigationStep=>ActiveMap<=1?.5f:1;
        public Vector3[] FeedingPoints=>ActiveMap==0?FoodPoints:jobSites[ActiveMap].food;
        public Rect PlayBounds=>ActiveMap==0?new Rect(-25.8f,-17.8f,51.6f,43.6f):ActiveMap==5?new Rect(-82,-30,164,156):new Rect(-43,-24,88,104);

        public void ActivateJobMap(int map) {
            map=Mathf.Clamp(map,0,5);
            if(!jobSites.ContainsKey(0))jobSites[0]=new JobSite{root=farmRoot,cargo=fieldCargoRoot,work=new List<DemoTarget>(workTargets),food=FoodPoints};
            foreach(var s in jobSites.Values)s.root.gameObject.SetActive(false);
            ActiveMap=map;
            if(!jobSites.ContainsKey(map)) {
                var site=new JobSite();site.root=new GameObject("L0"+(map+1)+" · job environment").transform;site.root.SetParent(transform);
                siteRoot=site.root;workTargets.Clear();BuildAdditionalSite(map);BuildTruck();site.cargo=cargoRoot;
                BuildWork();BuildAdditionalWorkVisuals();site.work=new List<DemoTarget>(workTargets);site.food=SiteFood(map);
                for(int i=0;i<5;i++){
                    var asset=Box("Property group "+i,site.food[i]+new Vector3(0,.35f,.8f),new Vector3(1.2f,.7f,.65f),map==4?foliage:wood);
                    var t=asset.AddComponent<DemoTarget>();t.kind="furniture";t.index=i;
                }
                jobSites.Add(map,site);
            }
            var chosen=jobSites[map];chosen.root.gameObject.SetActive(true);siteRoot=chosen.root;cargoRoot=chosen.cargo;
            workTargets.Clear();workTargets.AddRange(chosen.work);Physics.SyncTransforms();BuildNavigation();
        }
        Vector3[] SiteFood(int map) {
            if(map==1)return new[]{new Vector3(-10,.08f,12),new Vector3(10,.08f,23),new Vector3(-9,.08f,26),new Vector3(9,.08f,8),new Vector3(0,.08f,28)};
            if(map==2)return new[]{new Vector3(-26,.08f,9),new Vector3(-26,.08f,15),new Vector3(-26,.08f,23),new Vector3(11,.08f,28),new Vector3(11,.08f,35)};
            if(map==3)return new[]{new Vector3(23,.08f,8),new Vector3(23,.08f,17),new Vector3(23,.08f,26),new Vector3(23,.08f,35),new Vector3(23,.08f,43)};
            if(map==4)return new[]{new Vector3(-14,.08f,10),new Vector3(7,.08f,17),new Vector3(-14,1.88f,34),new Vector3(7,1.88f,41),new Vector3(-14,3.68f,60)};
            return new[]{new Vector3(-30,.08f,15),new Vector3(15,.08f,30),new Vector3(-15,.08f,52),new Vector3(30,.08f,70),new Vector3(-24,.08f,92)};
        }
        void SiteLabel(string id,Vector3 p) {
            var root=new GameObject(id);root.transform.SetParent(siteRoot);root.transform.position=p+Vector3.up*1.8f;
            var label=root.AddComponent<TextMesh>();label.text=id;label.characterSize=.13f;label.fontSize=42;label.anchor=TextAnchor.MiddleCenter;label.color=Color.white;
            Box(id+" post",p+Vector3.up*.8f,new Vector3(.08f,1.6f,.08f),wood);
        }
        void SiteGround(Rect bounds) {
            Box("Job floor",new Vector3(bounds.center.x,-.12f,bounds.center.y),new Vector3(bounds.width,.24f,bounds.height),dirt);
            foreach(float x in new[]{bounds.xMin-.3f,bounds.xMax+.3f})Box("Field boundary",new Vector3(x,1,bounds.center.y),new Vector3(.6f,2,bounds.height+1),foliage);
            foreach(float z in new[]{bounds.yMin-.3f,bounds.yMax+.3f})Box("Field boundary",new Vector3(bounds.center.x,1,z),new Vector3(bounds.width+1,2,.6f),foliage);
            SiteLabel("Parking / Load / Dispatch",VanPoint+new Vector3(4,0,2));
        }
        void OpenShed(string id,Vector3 center,float width,float length,float height,Material wall) {
            Box(id+" floor",center+new Vector3(0,-.03f,0),new Vector3(width,.06f,length),wood);
            foreach(float x in new[]{-width/2,width/2})Box(id+" side",center+new Vector3(x,height/2,0),new Vector3(.2f,height,length),wall);
            Box(id+" rear",center+new Vector3(0,height/2,length/2),new Vector3(width,height,.2f),wall);
            foreach(float x in new[]{-(width+4)/4,(width+4)/4})Box(id+" entrance",center+new Vector3(x,height/2,-length/2),new Vector3((width-4)/2,height,.2f),wall);
            Box(id+" lintel",center+new Vector3(0,height-.2f,-length/2),new Vector3(4,.4f,.2f),wall);
            Box(id+" roof",center+Vector3.up*(height+.1f),new Vector3(width+.5f,.18f,length+.5f),metal);
        }
        void BuildAdditionalSite(int map) {
            SiteGround(PlayBounds);
            if(map==1)BuildGranary();else if(map==2)BuildChickenFarm();else if(map==3)BuildPigFarm();else if(map==4)BuildOrchard();else BuildFarmland();
        }
        void BuildGranary() {
            OpenShed("Granary",new Vector3(0,0,17),36,34,5,wood);
            // Grain islands leave a central three metre carrying aisle.
            foreach(float x in new[]{-11f,11f})for(int z=7;z<=28;z+=7){
                Box("Grain isolation pallet",new Vector3(x,.12f,z),new Vector3(5,.24f,3),wood);
                for(int i=0;i<6;i++)Shape("Grain sack",PrimitiveType.Capsule,new Vector3(x-1.6f+(i%3)*1.6f,.65f+(i/3)*.65f,z),new Vector3(1.3f,.55f,.85f),cream);
            }
            foreach(float x in new[]{-5f,5f})Box("Rafter floor",new Vector3(x,3.4f,18),new Vector3(1.2f,.2f,25),wood);
            foreach(float z in new[]{5.5f,30.5f})Box("Rafter landing",new Vector3(0,3.4f,z),new Vector3(11,.2f,1.2f),wood);
            for(int k=0;k<18;k++)foreach(float x in new[]{-5f,5f})Box("Stair tread granary",new Vector3(x,(k+1)*.19f/2, -1+k*.35f),new Vector3(1.4f,(k+1)*.19f,.35f),wood);
            Box("Windmill tower",new Vector3(-25,7,31),new Vector3(3,14,3),wood);
            for(int i=0;i<4;i++){var blade=Box("Windmill sail",new Vector3(-25,11,29.3f),new Vector3(.5f,9,.1f),cream,null,false);blade.transform.rotation=Quaternion.Euler(0,0,i*45);}
            SiteLabel("L02 · Granary / high routes",new Vector3(0,0,-4));
        }
        void BuildChickenFarm() {
            // Three separate shelters and five nests, with a continuous walking route.
            foreach(var p in new[]{new Vector3(-26,0,12),new Vector3(-26,0,25),new Vector3(11,0,31)})OpenShed("Chicken house",p,9,9,2.6f,wood);
            int nestIndex=0;foreach(var p in SiteFood(2)){
                var nest=new GameObject("Egg nest "+nestIndex).transform;nest.SetParent(siteRoot);nest.position=p;var state=nest.gameObject.AddComponent<DemoNest>();state.world=this;state.index=nestIndex++;
                Box("Nest tray",new Vector3(0,.15f,.8f),new Vector3(1.2f,.25f,.8f),wood,nest);
                for(int i=0;i<10;i++)Shape("Egg",PrimitiveType.Sphere,new Vector3(-.4f+(i%5)*.2f,.34f,.6f+(i/5)*.35f),new Vector3(.12f,.17f,.12f),cream,nest);
            }
            for(int z=0;z<=42;z+=2)foreach(float x in new[]{-35f,31f})Fence(new Vector3(x,0,z));
            // Shallow ditch is traversed by two broad bridges.
            Box("Ditch water",new Vector3(36,-.03f,23),new Vector3(2,.05f,42),teal,null,false);
            foreach(float z in new[]{10f,35f})Box("Bridge floor",new Vector3(36,.1f,z),new Vector3(4,.2f,2.5f),wood);
            for(int i=0;i<12;i++)FarmLivestock("Chicken",new Vector3(-15+(i%4)*4,0,7+(i/4)*10),.3f,false);
            SiteLabel("L03 · Nests / ditch / recovery",new Vector3(0,0,-4));
        }
        void FarmLivestock(string name,Vector3 p,float scale,bool pig) {
            var root=new GameObject(name);root.transform.SetParent(siteRoot);root.transform.position=p;
            var livestock=root.AddComponent<DemoLivestock>();livestock.world=this;livestock.id=name+"-"+p.x+"-"+p.z;livestock.map=ActiveMap;livestock.pig=pig;livestock.home=p;root.layer=10;var interaction=root.AddComponent<BoxCollider>();interaction.isTrigger=true;interaction.center=Vector3.up*scale*.8f;interaction.size=new Vector3(scale*1.2f,scale*1.2f,scale*2);
            Shape(name+" body",PrimitiveType.Sphere,new Vector3(0,scale*.8f,0),new Vector3(scale*1.2f,scale,scale*2),pig?pink:cream,root.transform,pig);
            Shape(name+" head",PrimitiveType.Sphere,new Vector3(0,scale,scale*1.1f),Vector3.one*scale*.7f,pig?pink:amber,root.transform);
            foreach(float x in new[]{-.4f,.4f})foreach(float z in new[]{-.6f,.6f})Shape("Leg",PrimitiveType.Cylinder,new Vector3(x*scale,scale*.3f,z*scale),new Vector3(.1f,.3f,.1f)*scale,wood,root.transform);
        }
        void BuildPigFarm() {
            OpenShed("Iron pig shed",new Vector3(0,0,24),36,48,4,metal);
            foreach(float x in new[]{-10f,10f})for(int row=0;row<4;row++){
                float z=6+row*11;
                Box("Pen back rail",new Vector3(x,1,z+4.5f),new Vector3(12,.12f,.12f),metal);
                foreach(float side in new[]{-6f,6f})Box("Pen side rail",new Vector3(x+side,1,z),new Vector3(.12f,.12f,9),metal);
                // Gate is intentionally open, 2m beside central aisle.
                FarmLivestock("Domestic pig",new Vector3(x+2,0,z+1),.85f,true);
                Box("Feed trough",new Vector3(x,.35f,z-2),new Vector3(3,.6f,.8f),metal);
            }
            OpenShed("Feed store",new Vector3(27,0,23),14,17,3.4f,wood);
            foreach(float z in new[]{9f,19f,29f,39f})Shape("Feed silo",PrimitiveType.Cylinder,new Vector3(39,2,z),new Vector3(3,2,3),metal,null,true);
            Box("Machine enclosure",new Vector3(-26,1.1f,37),new Vector3(3,2.2f,5),teal);
            SiteLabel("L04 · Pens / feed store",new Vector3(0,0,-4));
        }
        void BuildOrchard() {
            // Continuous terraces and two 12 degree ramps; no teleport links.
            for(int tier=0;tier<3;tier++){
                float y=tier*1.8f,z=tier*24+12;
                if(tier>0)Box("Terrace floor",new Vector3(-6,y*.5f,z),new Vector3(62,y,23),dirt);
                for(int row=0;row<2;row++)for(int col=0;col<4;col++){
                    var p=new Vector3(-24+col*12,y,tier*24+6+row*10);Tree(p,3.5f);
                    for(int f=0;f<5;f++)Shape("Fruit",PrimitiveType.Sphere,p+new Vector3(Mathf.Cos(f*1.3f),2.7f,Mathf.Sin(f*1.3f)),Vector3.one*.22f,amber);
                }
                Box("Irrigation pipe",new Vector3(-6,y+.12f,z+10),new Vector3(54,.12f,.12f),metal,null,false);
            }
            foreach(int tier in new[]{1,2}){
                float y=(tier-.5f)*1.8f,z=tier*24;
                var ramp=Box("Orchard ramp floor",new Vector3(30,y-.1f,z),new Vector3(4,.2f,10),wood);ramp.transform.rotation=Quaternion.Euler(-Mathf.Atan2(1.8f,10)*Mathf.Rad2Deg,0,0);
                Box("Ramp approach floor",new Vector3(27,tier*1.8f-.1f,z+6),new Vector3(10,.2f,3),wood);
                Box("Lower ramp approach floor",new Vector3(27,(tier-1)*1.8f-.1f,z-6),new Vector3(10,.2f,3),wood);
            }
            OpenShed("Fruit packing shed",new Vector3(12,0,-7),12,8,3,wood);
            // Two sloped low branches connect the high-animal circuit to the ground.
            Box("Low branch floor",new Vector3(-12,1.5f,11),new Vector3(13,.2f,1.4f),wood);
            foreach(float x in new[]{-18f,-6f}){var branch=Box("Low branch ramp floor",new Vector3(x,.7f,7),new Vector3(1.4f,.2f,8.2f),wood);branch.transform.rotation=Quaternion.Euler(-Mathf.Atan2(1.6f,8)*Mathf.Rad2Deg,0,0);}
            for(int i=0;i<8;i++)Shape("Burrow mound",PrimitiveType.Sphere,new Vector3(-34,.2f,8+i*8),new Vector3(1.5f,.4f,1),dirt);
            SiteLabel("L05 · Three terraces / orchard",new Vector3(3,0,-16));
        }
        void BuildFarmland() {
            Box("Dirt track",new Vector3(-18,.015f,33),new Vector3(3,.03f,119),wood,null,false);
            foreach(var p in new[]{new Vector3(-25,0,20),new Vector3(31,0,43),new Vector3(-9,0,78)}){
                Shape("Retreat rock",PrimitiveType.Sphere,p+Vector3.up*.8f,new Vector3(3,1.6f,2),plaster,null,true);SiteLabel("Retreat",p+new Vector3(3,0,0));
            }
            foreach(var p in new[]{new Vector3(-55,0,48),new Vector3(53,0,70),new Vector3(-40,0,105),new Vector3(45,0,112)})Tree(p,5);
            // Low crop islands retain clear 20-80m observation windows.
            for(int x=-65;x<=65;x+=26)for(int z=14;z<115;z+=26){
                if(Mathf.Abs(x+18)<8)continue;
                Box("Low stubble plot",new Vector3(x,.12f,z),new Vector3(16,.24f,15),amber,null,false);
                for(int k=0;k<8;k++)Box("Crop row",new Vector3(x-7+k*2,.32f,z),new Vector3(.1f,.6f,14),foliage,null,false);
            }
            SiteLabel("L06 · Open hunting field",new Vector3(-8,0,-12));
        }
        bool RestoreAdditionalWorkLayout() {
            if(ActiveMap==0)return false;
            var j=game.save.job;
            if(j.workPositions.Count!=5){
                j.workPositions.Clear();var rng=new System.Random(j.layoutSeed);
                var centers=ActiveMap==1?new[]{new Vector3(-14,.5f,8),new Vector3(23,.5f,8),new Vector3(23,.8f,22),new Vector3(-23,.28f,38),new Vector3(20,.28f,38)}:
                    ActiveMap==2?new[]{new Vector3(-12,.5f,10),new Vector3(21,.5f,19),new Vector3(22,.8f,35),new Vector3(-34,.28f,5),new Vector3(29,.28f,41)}:
                    ActiveMap==3?new[]{new Vector3(-12,.5f,2),new Vector3(0,.5f,54),new Vector3(11,.8f,38),new Vector3(-23,.28f,10),new Vector3(21,.28f,47)}:
                    ActiveMap==4?new[]{new Vector3(-20,.5f,17),new Vector3(2,2.3f,38),new Vector3(11,.8f,23),new Vector3(-34,.28f,18),new Vector3(-34,2.08f,34)}:
                    new[]{new Vector3(-30,.5f,10),new Vector3(30,.5f,10),new Vector3(-35,.8f,56),new Vector3(-50,.28f,72),new Vector3(41,.28f,92)};
                foreach(var center in centers){
                    var candidates=Enumerable.Range(0,8).Select(k=>center+new Vector3((k%4)*.9f-1.35f,0,(k/4)*2-.5f)).OrderBy(_=>rng.Next());
                    bool found=false;var rejected=new List<string>();
                    foreach(var p in candidates){var approach=p+Vector3.left*.8f;
                        if(!Physics.Raycast(new Vector3(approach.x,p.y+1,approach.z),Vector3.down,out var ground,3,1<<9)||ground.normal.y<.95f){rejected.Add(p+": no ground");continue;}
                        approach.y=ground.point.y+.08f;
                        if(Physics.CheckCapsule(approach+Vector3.up*.35f,approach+Vector3.up*1.4f,.3f,1<<9)){rejected.Add(p+": blocked standing capsule");continue;}
                        if(Route(j.player,approach).Count==0){rejected.Add(p+": no route from "+j.player+" to "+ClosestNode(approach));continue;}
                        j.workPositions.Add(p);found=true;break;}
                    if(!found)throw new InvalidOperationException("No reachable L0"+(ActiveMap+1)+" work anchor "+j.workPositions.Count+" seed "+j.layoutSeed+"; "+string.Join("; ",rejected));
                }
            }
            for(int i=0;i<5;i++)workTargets[i].transform.position=j.workPositions[i];
            Physics.SyncTransforms();return true;
        }
    }
}
