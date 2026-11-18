using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Riverbend
{
    public partial class DemoWorld : MonoBehaviour
    {
        public DemoGame game;
        public Transform siteRoot, dynamicRoot;
        public Light sun;
        public Camera cam;
        public CharacterController controller;
        public Transform handRoot;
        public Light torch;
        public Material wood, plaster, metal, foliage, dirt, dark, pink, fur, cream, amber, teal;
        public readonly List<DemoTarget> workTargets=new List<DemoTarget>();
        public readonly Dictionary<string,DemoAnimal> animals=new Dictionary<string,DemoAnimal>();
        public readonly Dictionary<string,DemoDevice> devices=new Dictionary<string,DemoDevice>();
        public static readonly Vector3 VanPoint=new Vector3(-18,0,-3);
        public static readonly Vector3[] WorkPoints={new Vector3(6,.5f,2),new Vector3(11,.45f,13),new Vector3(6,.8f,8),new Vector3(8.13f,.28f,1),new Vector3(-8.13f,.28f,2)};
        public static readonly Vector3[] FoodPoints={new Vector3(3.5f,.15f,.65f),new Vector3(5,.15f,4),new Vector3(-5,.15f,.65f),new Vector3(-3.5f,3.36f,1.5f),new Vector3(5,3.36f,7.5f)};
        public GameObject Box(string name,Vector3 p,Vector3 s,Material m,Transform parent=null,bool collision=true)
        {
            return Shape(name,PrimitiveType.Cube,p,s,m,parent,collision);
        }
        public GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 s,Material m,Transform parent=null,bool collision=false)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name;
            go.transform.SetParent(parent?parent:siteRoot,false);go.transform.localPosition=p;go.transform.localScale=s;
            go.GetComponent<Renderer>().sharedMaterial=m;if(handRoot&&parent==handRoot)go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            if(!collision) { var col=go.GetComponent<Collider>(); col.enabled=false; Destroy(col); } else go.layer=9;
            return go;
        }
        Material Mat(string name,Color c,float smooth=0.1f,string tex=null)
        {
            var m=new Material(Shader.Find("HDRP/Lit"));m.name=name;m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",smooth);
            if(tex!=null) {m.SetTexture("_BaseColorMap",Resources.Load<Texture2D>(tex));m.SetTextureScale("_BaseColorMap",new Vector2(3,3));}
            return m;
        }
        public void Create(DemoGame owner)
        {
            game=owner;siteRoot=new GameObject("Elm Street · environment").transform;siteRoot.SetParent(transform);
            dynamicRoot=new GameObject("Authoritative job entities").transform;dynamicRoot.SetParent(transform);
            wood=Mat("Weathered oak",new Color(.62f,.47f,.32f),.15f,"wood");
            plaster=Mat("Warm lime plaster",new Color(.73f,.70f,.59f));metal=Mat("Galvanised steel",new Color(.37f,.43f,.44f),.35f,"iron");
            foliage=Mat("Leaves",new Color(.19f,.30f,.16f));dirt=Mat("Leaf ground",new Color(.52f,.55f,.42f),0,"ground");
            dark=Mat("Charcoal",new Color(.035f,.05f,.055f));pink=Mat("Ears and tail",new Color(.65f,.39f,.37f));
            fur=Mat("Mouse brown",new Color(.28f,.22f,.16f));cream=Mat("Canvas and porcelain",new Color(.78f,.78f,.67f));
            amber=Mat("Safety amber",new Color(.89f,.51f,.15f));teal=Mat("Riverbend teal",new Color(.08f,.32f,.32f));
            BuildFarm();BuildFurniture();Box("Utility water supply",new Vector3(6,.8f,7.8f),new Vector3(.08f,.08f,2.4f),metal,null,false);BuildWork();BuildTruck();BuildLighting();BuildPlayer();
            DisableHandShadows();BuildNavigation();
            ApplyNight(false);
        }
        void Wall(Vector3 p,Vector3 s) { Box("House wall",p,s,plaster);Box("Skirting",new Vector3(p.x,.12f,p.z),new Vector3(s.x,.12f,s.z+.035f),wood); }
        void Fence(Vector3 p,bool turn=false)
        {
            Box("Fence post",p+Vector3.up*.65f,new Vector3(.1f,1.3f,.1f),wood);
            for(int y=0;y<2;y++)Box("Fence rail",p+Vector3.up*(.45f+y*.5f),turn?new Vector3(2,.12f,.09f):new Vector3(.09f,.12f,1.2f),wood);
        }
        void Tree(Vector3 p,float h)
        {
            Shape("Tree trunk",PrimitiveType.Cylinder,p+Vector3.up*h*.45f,new Vector3(.35f,h*.45f,.35f),wood,null,true);
            Shape("Tree canopy",PrimitiveType.Sphere,p+Vector3.up*h,new Vector3(h*.7f,h*.8f,h*.7f),foliage);
            Shape("Tree canopy",PrimitiveType.Sphere,p+new Vector3(h*.2f,h*.85f,0),new Vector3(h*.65f,h*.7f,h*.65f),foliage);
        }
        void BuildFurniture()
        {
            var table=Box("A01 dining table",new Vector3(3.5f,.77f,1.5f),new Vector3(1.8f,.12f,1),wood);table.AddComponent<DemoTarget>().kind="furniture";table.GetComponent<DemoTarget>().index=0;
            foreach(float x in new[]{2.8f,4.2f})foreach(float z in new[]{1.15f,1.85f})Box("Table leg",new Vector3(x,.4f,z),new Vector3(.1f,.8f,.1f),wood);
            for(int i=0;i<3;i++) {var p=new Vector3(3.4f+i*.45f,.87f,1.5f);Shape("Ceramic bowl",PrimitiveType.Cylinder,p,new Vector3(.23f,.06f,.23f),cream);}
            Box("Kitchen counter",new Vector3(6.95f,.45f,1.8f),new Vector3(1.4f,.9f,3.7f),teal);
            Box("Countertop",new Vector3(6.95f,.94f,1.8f),new Vector3(1.6f,.09f,3.9f),cream);
            var cabinet=Box("A02 cupboard",new Vector3(5,.65f,4.8f),new Vector3(2,1.3f,.6f),wood);cabinet.AddComponent<DemoTarget>().kind="furniture";cabinet.GetComponent<DemoTarget>().index=1;
            for(int i=0;i<3;i++) Box("Cabinet door",new Vector3(4.35f+i*.65f,.65f,4.45f),new Vector3(.6f,1.1f,.05f),cream);
            var sofa=Box("A03 sofa",new Vector3(-5,.45f,1.5f),new Vector3(2.7f,.55f,1),teal);sofa.AddComponent<DemoTarget>().kind="furniture";sofa.GetComponent<DemoTarget>().index=2;
            Box("Sofa back",new Vector3(-5,.83f,1.94f),new Vector3(2.7f,.75f,.23f),teal);
            foreach(float x in new[]{-6.3f,-3.7f})Box("Sofa arm",new Vector3(x,.7f,1.5f),new Vector3(.25f,.6f,1),teal);
            for(int i=0;i<3;i++)Box("Sofa cushion",new Vector3(-5.85f+i*.85f,.78f,1.4f),new Vector3(.8f,.18f,.75f),cream);
            var shelf=Box("A04 bookcase",new Vector3(-5.5f,3.9f,4.6f),new Vector3(2,1.4f,.45f),wood);shelf.AddComponent<DemoTarget>().kind="furniture";shelf.GetComponent<DemoTarget>().index=3;
            for(int i=0;i<10;i++) Box("Books",new Vector3(-6.35f+i*.18f,4.25f,4.33f),new Vector3(.13f,.48f,.25f),i%2==0?teal:amber,null,false);
            var boxes=Box("A05 stored supplies",new Vector3(4.8f,3.65f,8.6f),new Vector3(1.8f,.9f,.8f),wood);boxes.AddComponent<DemoTarget>().kind="furniture";boxes.GetComponent<DemoTarget>().index=4;
            Box("Upper crate",new Vector3(4.6f,4.32f,8.6f),new Vector3(.8f,.44f,.7f),wood);
            for(int i=0;i<5;i++) {var p=FoodPoints[i];Shape("Food crumbs",PrimitiveType.Sphere,p,new Vector3(.2f,.07f,.18f),amber);}
            var rug=Box("Living room rug",new Vector3(-4.7f,.085f,1.4f),new Vector3(4.8f,.01f,3),cream,null,false);
        }
        void BuildWork()
        {
            for(int i=0;i<5;i++) {
                var root=new GameObject("WorkSite-"+i);root.transform.SetParent(siteRoot);root.transform.position=WorkPoints[i];
                var target=root.AddComponent<DemoTarget>();target.kind="work";target.index=i;workTargets.Add(target);
                var col=root.AddComponent<BoxCollider>();col.size=i<3?new Vector3(.85f,1,.8f):new Vector3(.45f,.55f,.55f);
                if(i==0) {
                    Box("Contaminated bin",Vector3.zero,new Vector3(.5f,.7f,.5f),dark,root.transform,false);

                } else if(i==1) {
                    Shape("Standing water bucket",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.75f,.42f,.75f),metal,root.transform);
                    Shape("Stagnant water",PrimitiveType.Cylinder,new Vector3(0,.43f,0),new Vector3(.68f,.015f,.68f),teal,root.transform);

                } else if(i==2) {
                    var pipe=Shape("Leaking pipe",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.15f,.6f,.15f),metal,root.transform);pipe.transform.localRotation=Quaternion.Euler(0,0,90);
                    Shape("Water puddle",PrimitiveType.Cylinder,new Vector3(0,-.71f,0),new Vector3(1.3f,.015f,.8f),teal,root.transform);
                } else {
                    Box("Rodent entrance",Vector3.zero,new Vector3(.13f,.38f,.48f),dark,root.transform,false);
                    for(int j=0;j<5;j++)Shape("Rodent footprints",PrimitiveType.Sphere,new Vector3((i==3?1:-1)*(.22f+j*.1f),-.19f,.1f*Mathf.Sin(j)),new Vector3(.04f,.014f,.045f),dark,root.transform);
                }
            }
        }
        void BuildVan()
        {
            var p=VanPoint;
            Box("Service van body",p+Vector3.up*1.05f,new Vector3(2.5f,1.7f,4.5f),cream);
            Box("Service van stripe",p+new Vector3(0,.8f,0),new Vector3(2.56f,.45f,4.55f),teal);
            Box("Van front glass",p+new Vector3(0,1.65f,-2.05f),new Vector3(2.15f,.65f,.13f),dark);
            foreach(float x in new[]{-1.25f,1.25f})foreach(float z in new[]{-1.45f,1.45f}) {
                var wheel=Shape("Van tyre",PrimitiveType.Cylinder,p+new Vector3(x,.42f,z),new Vector3(.65f,.16f,.65f),dark);wheel.transform.localRotation=Quaternion.Euler(0,0,90);
            }
            Box("Rear equipment shelf",p+new Vector3(0,.5f,2.35f),new Vector3(2,.12f,.7f),wood);
            for(int i=0;i<3;i++)Box("Van supply boxes",p+new Vector3(-.7f+i*.7f,.75f,2.25f),new Vector3(.55f,.45f,.4f),i==1?amber:teal);
        }
        void BuildLighting()
        {
            var light=new GameObject("Sun");light.transform.SetParent(transform);sun=light.AddComponent<Light>();sun.type=LightType.Directional;
            var hd=light.AddComponent<HDAdditionalLightData>();hd.SetIntensity(35000,LightUnit.Lux);light.transform.rotation=Quaternion.Euler(48,-35,0);
            var vg=new GameObject("Exposure and sky");vg.transform.SetParent(transform);var vol=vg.AddComponent<Volume>();vol.isGlobal=true;vol.priority=20;vol.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var exposure=vol.profile.Add<Exposure>();exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(9.4f);
            var visual=vol.profile.Add<VisualEnvironment>();visual.skyType.Override(1);visual.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            var sky=vol.profile.Add<HDRISky>();sky.hdriSky.Override(Resources.Load<Cubemap>("sky"));
            if(!sky.hdriSky.value) {visual.skyType.Override(3);var gradient=vol.profile.Add<GradientSky>();gradient.top.Override(new Color(.19f,.37f,.53f));gradient.middle.Override(new Color(.65f,.74f,.76f));gradient.bottom.Override(new Color(.42f,.45f,.34f));}
            var ao=vol.profile.Add<ScreenSpaceAmbientOcclusion>();ao.active=true;ao.intensity.Override(.5f);
            for(int i=0;i<8;i++) {
                Vector3 p=new Vector3(i%2==0?4:-4,2.75f+(i>=4?3.2f:0),(i%4)<2?1.5f:8);
                Shape("Ceiling lamps",PrimitiveType.Cylinder,p,new Vector3(.4f,.06f,.4f),cream);
                var go=new GameObject("Warm room light");go.transform.SetParent(siteRoot);go.transform.position=p-Vector3.up*.15f;
                var l=go.AddComponent<Light>();l.type=LightType.Point;l.range=7;l.color=new Color(1,.8f,.52f);l.shadows=LightShadows.Soft;
                go.AddComponent<HDAdditionalLightData>().SetIntensity(1100,LightUnit.Lumen);
            }
        }
        void BuildPlayer()
        {
            var player=new GameObject("DemoPlayer");player.tag="Player";player.transform.SetParent(transform);player.layer=8;
            controller=player.AddComponent<CharacterController>();controller.height=1.75f;controller.radius=.3f;controller.center=new Vector3(0,.875f,0);controller.stepOffset=.24f;
            var camera=new GameObject("DemoCamera");camera.transform.SetParent(player.transform,false);camera.transform.localPosition=Vector3.up*1.6f;
            cam=camera.AddComponent<Camera>();cam.tag="MainCamera";cam.nearClipPlane=.04f;cam.farClipPlane=160;cam.fieldOfView=game.config.player.baseFOV;
            camera.AddComponent<HDAdditionalCameraData>();camera.AddComponent<AudioListener>();
            handRoot=new GameObject("Hands and equipped tool").transform;handRoot.SetParent(camera.transform,false);
            var flashlight=new GameObject("Shoulder flashlight");flashlight.transform.SetParent(camera.transform,false);flashlight.transform.localPosition=Vector3.zero;
            torch=flashlight.AddComponent<Light>();torch.type=LightType.Spot;torch.range=22;torch.spotAngle=55;torch.innerSpotAngle=22;torch.color=new Color(1,.94f,.79f);torch.shadows=LightShadows.Soft;
            flashlight.AddComponent<HDAdditionalLightData>().SetIntensity(10000,LightUnit.Lumen);torch.enabled=false;
        }
        public void ApplyNight(bool night)
        {
            sun.color=night?new Color(.38f,.53f,.9f):new Color(1,.91f,.75f);
            sun.GetComponent<HDAdditionalLightData>().SetIntensity(night?550:35000,LightUnit.Lux);
            var vol=GetComponentInChildren<Volume>();var exposure=vol.profile.components.Find(x=>x is Exposure) as Exposure;
            exposure.fixedExposure.Override(night?7.2f:10.7f);
            var sky=vol.profile.components.Find(x=>x is GradientSky) as GradientSky;
            if(sky){sky.skyIntensityMode.Override(SkyIntensityMode.Multiplier);sky.multiplier.Override(night?60:2500);}
        }
        public void Equip(Kit? kit)
        {
            foreach(Transform child in handRoot)Destroy(child.gameObject);
            if(!kit.HasValue)return;
            handRoot.localRotation=Quaternion.identity;
            var sleeve=Shape("Work sleeve",PrimitiveType.Capsule,new Vector3(.29f,-.3f,.45f),new Vector3(.13f,.25f,.13f),teal,handRoot);sleeve.transform.localRotation=Quaternion.Euler(60,0,-15);
            Shape("Gloved hand",PrimitiveType.Sphere,new Vector3(.27f,-.21f,.65f),new Vector3(.13f,.1f,.15f),cream,handRoot);
            if(ShowAdditionalHeldCargo(kit.Value)){DisableHandShadows();return;}
            if((int)kit.Value>=12){BuildAdditionalEquipment(kit.Value,handRoot,true);DisableHandShadows();return;}
            if(kit==Kit.Spray) {
                Shape("Sprayer canister",PrimitiveType.Cylinder,new Vector3(.28f,-.16f,.74f),new Vector3(.15f,.16f,.15f),amber,handRoot);
                Box("Sprayer nozzle",new Vector3(.27f,.01f,.8f),new Vector3(.06f,.05f,.18f),metal,handRoot,false);
            } else if(kit==Kit.Repair) {Box("Tool handle",new Vector3(.27f,-.16f,.7f),new Vector3(.035f,.3f,.05f),wood,handRoot,false);Box("Hammer head",new Vector3(.27f,.025f,.7f),new Vector3(.19f,.07f,.08f),metal,handRoot,false);}
            else if(kit==Kit.Broom) {Box("Broom shaft",new Vector3(.27f,-.1f,.85f),new Vector3(.035f,.7f,.035f),wood,handRoot,false);Box("Broom brush",new Vector3(.27f,-.44f,.85f),new Vector3(.32f,.12f,.1f),amber,handRoot,false);}
            else if(kit==Kit.Baton) {Box("Pest control baton",new Vector3(.27f,.05f,.8f),new Vector3(.06f,.7f,.06f),wood,handRoot,false);}
            else if(kit==Kit.Flashlight) {var t=Shape("Flashlight body",PrimitiveType.Cylinder,new Vector3(.27f,-.12f,.72f),new Vector3(.075f,.12f,.075f),dark,handRoot);t.transform.localRotation=Quaternion.Euler(90,0,0);}
            else if(kit==Kit.Corpse) {Shape("Held mouse remains",PrimitiveType.Sphere,new Vector3(.28f,-.12f,.8f),new Vector3(.14f,.09f,.25f),fur,handRoot);Shape("Remains tail",PrimitiveType.Capsule,new Vector3(.28f,-.12f,.59f),new Vector3(.018f,.12f,.018f),pink,handRoot).transform.localRotation=Quaternion.Euler(90,0,0);}
            else if(kit==Kit.SprayRefill||kit==Kit.RepairPack||kit==Kit.Bait||kit==Kit.CleaningRefill){Box("Held supply",new Vector3(.28f,-.13f,.76f),new Vector3(.16f,.22f,.14f),kit==Kit.SprayRefill?amber:cream,handRoot,false);}
            else if(kit==Kit.Cage) {
                Box("Held cage base",new Vector3(.28f,-.27f,.8f),new Vector3(.35f,.025f,.4f),metal,handRoot,false);
                foreach(float x in new[]{.11f,.45f})for(int i=0;i<4;i++)Box("Held cage bar",new Vector3(x,-.15f,.63f+i*.11f),new Vector3(.012f,.24f,.012f),metal,handRoot,false);
                for(int i=0;i<4;i++)Box("Held closed door",new Vector3(.12f+i*.11f,-.15f,1),new Vector3(.012f,.24f,.012f),metal,handRoot,false);
                Box("Held cage roof",new Vector3(.28f,-.025f,.8f),new Vector3(.35f,.015f,.4f),metal,handRoot,false);
            } else { Box("Packed equipment",new Vector3(.28f,-.17f,.8f),new Vector3(.16f,.035f,.3f),metal,handRoot,false); }
            DisableHandShadows();
            if(kit==Kit.Cage&&game.save.job!=null&&game.save.job.carriedSlot>=0&&game.save.job.kit.Count>game.save.job.carriedSlot&&!string.IsNullOrEmpty(game.save.job.carriedAnimal)) {
                var a=game.save.job.animals.Find(x=>x.id==game.save.job.carriedAnimal)??game.save.cargo.Find(x=>x.id==game.save.job.carriedAnimal);
                if(a!=null)Shape(a.dead?"Bagged remains":"Visible captive",PrimitiveType.Sphere,new Vector3(.28f,-.12f,.8f),new Vector3(.14f,.09f,.21f),a.dead?dark:fur,handRoot);
            }
        }
        public void RestoreJob()
        {
            foreach(Transform child in dynamicRoot)Destroy(child.gameObject);animals.Clear();devices.Clear();
            var job=game.save.job;
            foreach(var a in job.animals) if(!a.terminal && !a.loaded && a.id!=job.carriedAnimal&&!job.cargoIds.Contains(a.id))CreateAnimal(a);
            foreach(var d in job.devices)CreateDevice(d);
            foreach(var animal in animals.Values)animal.RestoreAdditionalLure();
            RestoreWorkLayout();RestoreLooseItems();RestorePests();RestoreContractInteractions();RefreshTruckCargo();
            foreach(var w in workTargets)w.Refresh();
            ApplyNight(job.stage==2);controller.enabled=false;controller.transform.position=job.player;controller.transform.rotation=Quaternion.Euler(0,job.yaw,0);controller.enabled=true;RestorePartners();RestoreStolenObjects();
        }
        public DemoAnimal CreateAnimal(AnimalRecord a)
        {
            var root=new GameObject("Animal-"+a.id);root.transform.SetParent(dynamicRoot);root.transform.position=a.position;
            var animal=root.AddComponent<DemoAnimal>();animal.record=a;animal.world=this;animal.MakeModel();animal.RestoreAdditionalBehavior();animals[a.id]=animal;return animal;
        }
        public DemoDevice CreateDevice(DeviceRecord d)
        {
            var root=new GameObject("Device-"+d.id);root.transform.SetParent(dynamicRoot);root.transform.position=d.position;root.transform.rotation=Quaternion.Euler(0,d.yaw,0);
            var device=root.AddComponent<DemoDevice>();device.record=d;device.world=this;device.MakeModel();devices[d.id]=device;return device;
        }
    }
    public class DemoTarget : MonoBehaviour
    {
        public string kind;public int index;
        public void Refresh()
        {
            if(kind!="work")return;var game=DemoGame.main;
            if(game==null || game.save.job==null)return;
            var state=game.save.job.work[index];bool done=state>=(game.CleaningWork(index)?3:1);
            if(index>=3) {
                var seal=transform.Find("Sealed entrance");
                if(done&&!seal){var panel=game.world.Box("Sealed entrance",Vector3.zero,new Vector3(.24f,.42f,.75f),game.world.wood,transform);}
                if(!done&&seal){seal.gameObject.SetActive(false);Destroy(seal.gameObject);}
            }
            if(index==1){var water=transform.Find("Stagnant water");if(water)water.gameObject.SetActive(!done);}
            if(index==2){var puddle=transform.Find("Water puddle");if(puddle)puddle.gameObject.SetActive(!done);}
            foreach(var r in GetComponentsInChildren<Renderer>())r.material.SetColor("_BaseColor",done?new Color(.22f,.48f,.35f):r.sharedMaterial.GetColor("_BaseColor"));
        }
    }
}
