using UnityEngine;
namespace Riverbend {
    public partial class DemoWorld {
        void BuildAdditionalEquipment(Kit kit,Transform parent,bool held){
            // Local authoring coordinates, shared by held and physical stock displays.
            var root=new GameObject(kit+" whitebox").transform;root.SetParent(parent,false);
            if(held)root.localPosition=new Vector3(.27f,-.16f,.75f);
            if(kit==Kit.Rifle||kit==Kit.Airgun){
                Box("Stock",new Vector3(0,-.04f,-.12f),new Vector3(.08f,.12f,.45f),wood,root,false);
                Box("Receiver",new Vector3(0,0,.08f),new Vector3(.09f,.1f,.3f),metal,root,false);
                Box("Barrel",new Vector3(0,.025f,.4f),new Vector3(.035f,.035f,kit==Kit.Rifle?.55f:.35f),metal,root,false);
                Box("Grip",new Vector3(0,-.13f,-.02f),new Vector3(.065f,.18f,.08f),wood,root,false);
                if(kit==Kit.Rifle)Shape("Scope",PrimitiveType.Cylinder,new Vector3(0,.11f,.08f),new Vector3(.05f,.16f,.05f),dark,root).transform.localRotation=Quaternion.Euler(90,0,0);
            }else if(kit==Kit.Bow){
                foreach(float side in new[]{-1f,1f}){var limb=Box("Bow limb",new Vector3(0,side*.28f,.08f),new Vector3(.035f,.52f,.035f),wood,root,false);limb.transform.localRotation=Quaternion.Euler(side*18,0,0);}
                Box("Bow string",new Vector3(0,0,-.02f),new Vector3(.006f,1,.006f),cream,root,false);
                Box("Nocked arrow",new Vector3(0,0,.16f),new Vector3(.012f,.012f,.55f),wood,root,false);
            }else if(kit==Kit.Blowpipe||kit==Kit.Spear||kit==Kit.ControlPole){
                float length=kit==Kit.Blowpipe?.8f:1.3f;Box("Shaft",new Vector3(0,0,length*.3f),new Vector3(.035f,.035f,length),kit==Kit.Spear?wood:metal,root,false);
                if(kit==Kit.Spear)Shape("Spear point",PrimitiveType.Capsule,new Vector3(0,0,length*.8f),new Vector3(.04f,.13f,.025f),metal,root).transform.localRotation=Quaternion.Euler(90,0,0);
                if(kit==Kit.ControlPole)for(int i=0;i<16;i++){float a=i*Mathf.PI/8;Box("Control loop",new Vector3(Mathf.Cos(a)*.1f,Mathf.Sin(a)*.1f,length*.8f),new Vector3(.04f,.012f,.012f),dark,root,false);}
            }else if(kit==Kit.Swatter||kit==Kit.Net){
                Box("Handle",new Vector3(0,-.15f,0),new Vector3(.025f,.45f,.025f),wood,root,false);
                float size=kit==Kit.Net?.42f:.22f;for(int i=0;i<6;i++){Box("Mesh horizontal",new Vector3(0,i*size/5,.02f),new Vector3(size,.009f,.009f),teal,root,false);Box("Mesh vertical",new Vector3(-size*.5f+i*size/5,size*.5f,.02f),new Vector3(.009f,size,.009f),teal,root,false);}
                if(kit==Kit.Net)Shape("Net bag",PrimitiveType.Sphere,new Vector3(0,size*.5f,.13f),new Vector3(size,.3f,.18f),cream,root);
            }else if(DemoGame.IsCage(kit)){
                Box("Folded cage base",Vector3.zero,new Vector3(.4f,.025f,.5f),metal,root,false);
                foreach(float side in new[]{-.2f,.2f})for(int i=0;i<6;i++)Box("Folded bars",new Vector3(side,.13f,-.25f+i*.1f),new Vector3(.012f,.26f,.012f),metal,root,false);
                Box("Cage lid",new Vector3(0,.27f,0),new Vector3(.4f,.02f,.5f),metal,root,false);
            }else if(kit==Kit.Binoculars){
                foreach(float x in new[]{-.07f,.07f}){Shape("Optical tube",PrimitiveType.Cylinder,new Vector3(x,0,0),new Vector3(.1f,.14f,.1f),dark,root).transform.localRotation=Quaternion.Euler(90,0,0);Shape("Lens",PrimitiveType.Sphere,new Vector3(x,0,.14f),new Vector3(.09f,.09f,.015f),teal,root);}
            }else if(kit==Kit.Drone){
                Box("Drone body",Vector3.zero,new Vector3(.18f,.06f,.18f),teal,root,false);
                foreach(float x in new[]{-.16f,.16f})foreach(float z in new[]{-.16f,.16f}){Box("Drone arm",new Vector3(x*.5f,0,z*.5f),new Vector3(.2f,.025f,.025f),metal,root,false);Shape("Propeller",PrimitiveType.Cylinder,new Vector3(x,.04f,z),new Vector3(.16f,.008f,.16f),dark,root);}
            }else if(kit==Kit.Camera){
                Box("Camera housing",Vector3.zero,new Vector3(.18f,.2f,.11f),foliage,root,false);Shape("Camera lens",PrimitiveType.Sphere,new Vector3(0,.03f,.065f),new Vector3(.065f,.065f,.02f),dark,root);
                Box("Camera mount",new Vector3(0,-.17f,0),new Vector3(.035f,.18f,.035f),wood,root,false);
            }else if(kit==Kit.BaitStation){Box("Enclosed station",Vector3.zero,new Vector3(.35f,.12f,.24f),dark,root,false);foreach(float x in new[]{-.18f,.18f})Box("Entry port",new Vector3(x,-.01f,0),new Vector3(.015f,.07f,.1f),teal,root,false);}
            else if(kit==Kit.Arrows){Box("Arrow quiver",Vector3.zero,new Vector3(.13f,.3f,.11f),wood,root,false);for(int i=0;i<4;i++){Box("Stored arrow",new Vector3(-.045f+i*.03f,.12f,0),new Vector3(.009f,.5f,.009f),wood,root,false);Box("Arrow fletching",new Vector3(-.045f+i*.03f,.34f,0),new Vector3(.03f,.06f,.012f),cream,root,false);}}
            else if(kit==Kit.Darts){Box("Dart cassette",Vector3.zero,new Vector3(.22f,.08f,.13f),teal,root,false);for(int i=0;i<5;i++)Box("Stored game dart",new Vector3(-.08f+i*.04f,.05f,0),new Vector3(.012f,.012f,.2f),wood,root,false);}
            else if(kit==Kit.BodyBag){Box("Folded body bag",Vector3.zero,new Vector3(.27f,.05f,.19f),dark,root,false);foreach(float x in new[]{-.07f,.07f})Box("Bag strap",new Vector3(x,.03f,0),new Vector3(.025f,.012f,.2f),cream,root,false);}
            else{Box("Labelled supply package",Vector3.zero,kit==Kit.Cartridges?new Vector3(.22f,.16f,.13f):new Vector3(.16f,.22f,.12f),kit==Kit.PoisonBait?amber:cream,root,false);Box("Package stripe",new Vector3(0,0,.07f),new Vector3(.13f,.05f,.01f),kit==Kit.Pellets?metal:kit==Kit.Cartridges?amber:teal,root,false);if(kit==Kit.Cartridges)for(int i=0;i<3;i++)Shape("Cartridge outline",PrimitiveType.Cylinder,new Vector3(-.06f+i*.06f,.11f,0),new Vector3(.035f,.07f,.035f),amber,root);if(kit==Kit.PoisonBait)Box("Locked supply latch",new Vector3(0,.12f,0),new Vector3(.07f,.04f,.14f),metal,root,false);}
        }
    }
}
