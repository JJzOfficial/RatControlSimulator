using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Riverbend
{
    public partial class DemoWorld
    {
        public Transform cargoRoot;
        readonly Dictionary<Vector2Int,List<Vector3>> navigation=new Dictionary<Vector2Int,List<Vector3>>();
        void BuildFarm()
        {
            Box("Farm soil",new Vector3(0,-.15f,3),new Vector3(100,.3f,100),dirt);
            Box("Front gravel path",new Vector3(0,.015f,-5.5f),new Vector3(3,.03f,7),metal);
            Box("Ground floor",new Vector3(0,.035f,4),new Vector3(16,.07f,12),wood);
            // Actual floor opening over the central flight. Side galleries connect both upper rooms.
            Box("Upper west floor",new Vector3(-4.65f,3.2f,4),new Vector3(6.7f,.16f,12),wood);
            Box("Upper east floor",new Vector3(4.65f,3.2f,4),new Vector3(6.7f,.16f,12),wood);
            Box("Upper front corridor",new Vector3(0,3.2f,.55f),new Vector3(2.6f,.16f,5.1f),wood);
            Box("Upper landing",new Vector3(0,3.2f,9.5f),new Vector3(2.6f,.16f,1),wood);
            for(int floor=0;floor<2;floor++) {
                float y=floor*3.2f;
                foreach(float z in new[]{-2f,10f}) {
                    foreach(float x in new[]{-7.75f,-4.425f,4.425f,7.75f})Box("Facade junction",new Vector3(x,y+1.5f,z),new Vector3(Mathf.Abs(x)>7?.5f:.3f,3,.22f),plaster);
                    foreach(float x in new[]{-6f,-2.85f,2.85f,6f}) {
                        // Split walls leave real recessed windows and a central doorway on each level.
                        Box("Window sill wall",new Vector3(x,y+.5f,z),new Vector3(2.85f,1,.22f),plaster);
                        Box("Window lintel",new Vector3(x,y+2.7f,z),new Vector3(2.85f,.6f,.22f),plaster);
                        foreach(float side in new[]{-1.25f,1.25f})Box("Window pier",new Vector3(x+side,y+1.7f,z),new Vector3(.35f,1.4f,.22f),plaster);
                        Box("Window glass",new Vector3(x,y+1.7f,z),new Vector3(2.1f,1.35f,.035f),teal);
                        Box("Window mullion",new Vector3(x,y+1.7f,z-.13f),new Vector3(.055f,1.4f,.05f),cream,null,false);
                    }
                    Box("Door lintel",new Vector3(0,y+2.75f,z),new Vector3(2.8f,.5f,.22f),wood);
                    if(floor==1)Box("Upper balcony safety",new Vector3(0,y+.65f,z),new Vector3(2.6f,1.3f,.16f),wood);
                }
                foreach(float x in new[]{-8f,8f}) {
                    Box("Exterior wall",new Vector3(x,y+1.7f,4),new Vector3(.22f,2.6f,12),plaster);
                    // Keep the low rodent openings at z=1 and z=2.
                    float opening=x>0?1:2;
                    if(floor==1)Box("Upper wall base",new Vector3(x,y+.2f,opening),new Vector3(.22f,.4f,.8f),plaster);
                    Box("Low wall front",new Vector3(x,y+.2f,(-2+opening-.4f)*.5f),new Vector3(.22f,.4f,opening+1.6f),plaster);
                    Box("Low wall rear",new Vector3(x,y+.2f,(opening+.4f+10)*.5f),new Vector3(.22f,.4f,9.6f-opening),plaster);
                }
                // Living/bedroom west, kitchen/storage east, broad central corridor.
                foreach(float x in new[]{-1.55f,1.55f}) {
                    Box("Room divider",new Vector3(x,y+1.5f,.1f),new Vector3(.16f,3,4.2f),plaster);
                    Box("Room divider rear",new Vector3(x,y+1.5f,6.35f),new Vector3(.16f,3,3.5f),plaster);
                    Box("Interior door header",new Vector3(x,y+2.7f,3.4f),new Vector3(.16f,.6f,2.4f),wood);
                }
                foreach(float x in new[]{3f,7.2f})Box("Utility / storage partition",new Vector3(x,y+1.5f,5.6f),new Vector3(x==3?2.8f:1.6f,3,.16f),plaster);
            }
            // 16 walkable risers; identical collision geometry is used by people and animals.
            for(int i=0;i<16;i++)Box("Stair tread "+i,new Vector3(0,(i+1)*.2f*.5f+.07f,3.5f+i*.34f),new Vector3(2.4f,(i+1)*.2f,.34f),wood);
            foreach(float x in new[]{-1.28f,1.28f}) {
                Box("Upper stair guard front",new Vector3(x,3.85f,5.5f),new Vector3(.1f,1.15f,4.9f),wood);
                for(int i=0;i<8;i++)Box("Stair baluster",new Vector3(x,.8f+i*.4f,3.6f+i*.68f),new Vector3(.055f,1.1f,.055f),wood);
            }
            Box("Bedroom bed",new Vector3(-5,3.65f,.4f),new Vector3(2,.65f,2.5f),wood);
            Box("Bedroom mattress",new Vector3(-5,4,.4f),new Vector3(1.9f,.25f,2.4f),cream);
            Box("Bedroom pillow",new Vector3(-5,4.2f,-.35f),new Vector3(1.1f,.2f,.55f),teal);
            Box("Bedroom wardrobe",new Vector3(-7,4.2f,7.5f),new Vector3(1.4f,1.8f,1.1f),wood);
            Box("Utility shelf",new Vector3(7,1.05f,9),new Vector3(1.4f,2,.55f),wood);
            for(int i=0;i<7;i++)Box("Storage boxes",new Vector3(3+i%3,3.6f+(i/3)*.6f,9),new Vector3(.7f,.55f,.7f),i%2==0?teal:wood);
            Box("Roof west",new Vector3(-4.3f,6.9f,4),new Vector3(9,.18f,13.2f),metal).transform.rotation=Quaternion.Euler(0,0,12);
            Box("Roof east",new Vector3(4.3f,6.9f,4),new Vector3(9,.18f,13.2f),metal).transform.rotation=Quaternion.Euler(0,0,-12);
            Box("Porch",new Vector3(0,.09f,-3),new Vector3(5,.18f,2),wood);
            for(int i=0;i<5;i++) {
                var p=new Vector3(11,.2f,3+i*2.3f);
                Box("Vegetable bed",p,new Vector3(3,.4f,1.5f),wood);
                for(int j=0;j<6;j++)Shape("Vegetables",PrimitiveType.Sphere,p+new Vector3(-1+j*.4f,.3f,0),new Vector3(.4f,.5f,.7f),foliage);
            }
            // Near rows allow entry; the outer densely packed crop has continuous physical boundaries.
            var combine=new List<CombineInstance>();var stalk=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=stalk.GetComponent<MeshFilter>().sharedMesh;
            for(float x=-29;x<=29;x+=.75f)for(float z=-24;z<=33;z+=.75f) {
                if(x>-24.5f&&x<24.5f&&z>-16.5f&&z<24.5f)continue;
                float h=1.5f+Mathf.Abs(Mathf.Sin(x*12+z*7))*.65f;
                combine.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3(x,h*.5f,z),Quaternion.Euler(0,x*31+z*17,7*Mathf.Sin(z)),new Vector3(.055f,h,.055f))});
                combine.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(new Vector3(x,h,z),Quaternion.Euler(0,x*31,12),new Vector3(.12f,.36f,.08f))});
            }
            Destroy(stalk);var wheat=new GameObject("Surrounding wheat field");wheat.transform.SetParent(siteRoot);var combined=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};combined.CombineMeshes(combine.ToArray());wheat.AddComponent<MeshFilter>().sharedMesh=combined;wheat.AddComponent<MeshRenderer>().sharedMaterial=amber;
            foreach(float x in new[]{-26.5f,26.5f}){var b=new GameObject("Dense wheat collision");b.transform.SetParent(siteRoot);b.transform.position=new Vector3(x,1,4);b.layer=9;b.AddComponent<BoxCollider>().size=new Vector3(1,2,46);}
            foreach(float z in new[]{-18.5f,26.5f}){var b=new GameObject("Dense wheat collision");b.transform.SetParent(siteRoot);b.transform.position=new Vector3(0,1,z);b.layer=9;b.AddComponent<BoxCollider>().size=new Vector3(54,2,1);}
        }
        void BuildTruck()
        {
            var p=VanPoint;
            Box("Truck chassis",p+new Vector3(0,.5f,0),new Vector3(3.5f,.3f,7),dark);
            Box("Truck cargo floor",p+new Vector3(0,.76f,.65f),new Vector3(3.5f,.16f,5.5f),metal);
            Box("Truck cab",p+new Vector3(0,1.5f,-3),new Vector3(3.45f,2,1.9f),cream);
            Box("Truck windscreen",p+new Vector3(0,1.95f,-3.97f),new Vector3(2.95f,.85f,.06f),teal);
            foreach(float x in new[]{-1.75f,1.75f}) {
                Box("Cargo side wall",p+new Vector3(x,2.1f,.65f),new Vector3(.12f,2.8f,5.5f),cream);
                Box("Truck livery",p+new Vector3(x*1.04f,1.5f,.65f),new Vector3(.035f,.55f,5.4f),teal,null,false);
                foreach(float z in new[]{-2.8f,2.1f})Shape("Truck tyre",PrimitiveType.Cylinder,p+new Vector3(x,.5f,z),new Vector3(1,.25f,1),dark).transform.rotation=Quaternion.Euler(0,0,90);
            }
            Box("Cargo front wall",p+new Vector3(0,2.1f,-2.05f),new Vector3(3.5f,2.8f,.12f),cream);
            Box("Cargo roof",p+new Vector3(0,3.5f,.65f),new Vector3(3.6f,.15f,5.6f),cream);
            foreach(float x in new[]{-2.45f,2.45f})Box("Open rear door",p+new Vector3(x,2.1f,3.6f),new Vector3(1.45f,2.8f,.12f),teal).transform.rotation=Quaternion.Euler(0,x>0?-25:25,0);
            for(int i=0;i<4;i++)Box("Boarding step",p+new Vector3(0,(4-i)*.2f*.5f,3.5f+i*.35f),new Vector3(2.2f,(4-i)*.2f,.35f),metal);
            Box("Equipment platform",p+new Vector3(1.05f,1.02f,2.35f),new Vector3(1.05f,.14f,1.35f),wood);
            Box("Supply platform",p+new Vector3(0,1.02f,-1.3f),new Vector3(2.8f,.14f,.9f),wood);
            // Looking in from the rear: left is +X, right is -X.
            var desk=Box("Truck workbench",p+new Vector3(1.15f,1.3f,.6f),new Vector3(.9f,.95f,2.7f),wood);
            desk.AddComponent<DemoTarget>().kind="van";
            var screen=Box("Dispatch screen",p+new Vector3(1.15f,2,-.8f),new Vector3(.85f,.6f,.12f),teal);screen.AddComponent<DemoTarget>().kind="van";
            var cage=Box("Truck loading cage floor",p+new Vector3(-1.1f,.9f,.5f),new Vector3(1.1f,.12f,3.3f),metal);cage.AddComponent<DemoTarget>().kind="cargo";
            for(int i=0;i<12;i++)foreach(float x in new[]{-1.65f,-.55f})Box("Loading cage bars",p+new Vector3(x,1.6f,-1+i*.28f),new Vector3(.035f,1.4f,.035f),metal);
            Box("Loading cage roof",p+new Vector3(-1.1f,2.3f,.5f),new Vector3(1.15f,.05f,3.3f),metal);
            var handle=Box("Load and release cage handle",p+new Vector3(-1.1f,1.6f,2.2f),new Vector3(.9f,1.3f,.08f),metal);handle.AddComponent<DemoTarget>().kind="cargo";
            cargoRoot=new GameObject("Visible truck animals").transform;cargoRoot.SetParent(siteRoot);
            var lamp=new GameObject("Truck work light");lamp.transform.SetParent(siteRoot);lamp.transform.position=p+new Vector3(0,3.2f,1);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=6;light.shadows=LightShadows.Soft;lamp.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();light.lightUnit=UnityEngine.Rendering.LightUnit.Lumen;light.intensity=1600;
        }
        public void RefreshTruckCargo()
        {
            foreach(Transform c in cargoRoot)Destroy(c.gameObject);
            if(game.save.job==null)return;int i=0,large=0;
            foreach(var a in game.save.job.animals.Where(a=>a.loaded&&!a.terminal)) {
                if(game.save.job.contractIndex>0){BuildCargoAnimal(a,AdditionalCargoPoint(a,ref i,ref large),false);continue;}
                var r=new GameObject("Loaded "+a.species);r.transform.SetParent(cargoRoot);r.transform.position=VanPoint+new Vector3(-1.1f,1.02f,-.7f+i++*.6f);
                var copy=JsonUtility.FromJson<AnimalRecord>(JsonUtility.ToJson(a));var model=r.AddComponent<DemoAnimal>();model.record=copy;model.world=this;model.MakeModel();
                r.GetComponent<DemoTarget>().kind="cargo";
                if(a.dead)r.transform.rotation=Quaternion.Euler(0,0,80);
            }
        }
        public bool ClearLine(Vector3 a,Vector3 b)=>!Physics.Linecast(a,b,1<<9,QueryTriggerInteraction.Ignore);
        public void BuildNavigation()
        {
            Physics.SyncTransforms();navigation.Clear();
            float step=NavigationStep;var bounds=PlayBounds;
            int minX=ActiveMap==0?-50:Mathf.CeilToInt(bounds.xMin/step),maxX=ActiveMap==0?50:Mathf.FloorToInt(bounds.xMax/step);
            int minZ=ActiveMap==0?-34:Mathf.CeilToInt(bounds.yMin/step),maxZ=ActiveMap==0?50:Mathf.FloorToInt(bounds.yMax/step);
            for(int x=minX;x<=maxX;x++)for(int z=minZ;z<=maxZ;z++) {
                var hits=Physics.RaycastAll(new Vector3(x*step,ActiveMap==0?3.6f:5,z*step),Vector3.down,ActiveMap==0?4:6,1<<9);
                foreach(var h in hits) {
                    string name=h.collider.name;
                    if(!(name.Contains("floor")||name.Contains("corridor")||name.Contains("landing")||name.Contains("Stair tread")||name=="Farm soil"||name=="Porch")||h.normal.y<.95f||h.point.y>(ActiveMap==0?3.35f:4.1f))continue;
                    var p=h.point+Vector3.up*.08f;
                    if(Physics.CheckSphere(p+Vector3.up*.46f,.09f,1<<9))continue;
                    if(ActiveMap>0&&Physics.CheckCapsule(p+Vector3.up*.35f,p+Vector3.up*1.45f,.3f,1<<9))continue;
                    var key=new Vector2Int(x,z);if(!navigation.ContainsKey(key))navigation[key]=new List<Vector3>();navigation[key].Add(p);
                }
            }
        }
        Vector3 ClosestNode(Vector3 p)
        {
            float best=float.MaxValue;Vector3 found=p;
            foreach(var list in navigation.Values)foreach(var n in list) {float d=(n-p).sqrMagnitude;if(d<best){best=d;found=n;}}
            return found;
        }
        public List<Vector3> Route(Vector3 start,Vector3 end,bool outdoor=false,float agentRadius=0)
        {
            start=ClosestNode(start);end=ClosestNode(end);
            var open=new List<Vector3>{start};var cost=new Dictionary<Vector3,float>{{start,0}};var prev=new Dictionary<Vector3,Vector3>();int budget=6000;
            while(open.Count>0&&budget-->0) {
                int index=0;float best=float.MaxValue;
                for(int i=0;i<open.Count;i++){float v=cost[open[i]]+Vector3.Distance(open[i],end);if(v<best){best=v;index=i;}}
                var p=open[index];open.RemoveAt(index);
                if(p==end){var result=new List<Vector3>{end};while(prev.ContainsKey(p)){p=prev[p];result.Add(p);}result.Reverse();return result;}
                var key=new Vector2Int(Mathf.RoundToInt(p.x/NavigationStep),Mathf.RoundToInt(p.z/NavigationStep));
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++) {
                    if(x==0&&z==0)continue;
                    if(!navigation.TryGetValue(key+new Vector2Int(x,z),out var list))continue;
                    foreach(var n in list) {
                        if(Mathf.Abs(n.y-p.y)>(ActiveMap<=1?.45f:.3f)||(outdoor&&ActiveMap==0&&Mathf.Abs(n.x)<8.6f&&n.z> -2.6f&&n.z<10.6f))continue;
                        if(!ClearLine(p+Vector3.up*.46f,n+Vector3.up*.46f))continue;
                        var segment=n-p;if(ActiveMap>0&&Physics.CapsuleCast(p+Vector3.up*.65f,p+Vector3.up*1.45f,.3f,segment.normalized,segment.magnitude,1<<9))continue;
                        if(agentRadius>0&&Physics.SphereCast(p+Vector3.up*(agentRadius+.08f),agentRadius,segment.normalized,out _,segment.magnitude,1<<9,QueryTriggerInteraction.Ignore))continue;
                        float c=cost[p]+Vector3.Distance(p,n);
                        if(cost.TryGetValue(n,out float old)&&old<=c)continue;
                        cost[n]=c;prev[n]=p;if(!open.Contains(n))open.Add(n);
                    }
                }
            }
            return new List<Vector3>();
        }
    }
}

