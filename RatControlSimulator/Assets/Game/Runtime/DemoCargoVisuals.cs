using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoWorld {
        // Additional cargo uses its real silhouette. The accepted P0 truck mesh stays intact.
        Vector3 AdditionalCargoPoint(AnimalRecord animal,ref int ordinary,ref int large){
            var definition=game.config.content.species.First(s=>s.id==animal.species);
            if(definition.size=="Large"){
                int bay=large++;var position=VanPoint+new Vector3(-4.5f,1.02f,-.2f+bay*3.2f);var root=new GameObject("Large transport module "+bay).transform;root.SetParent(cargoRoot);root.position=position;
                Box("Large bay deck",new Vector3(0,-.12f,0),new Vector3(2.2f,.16f,3.05f),metal,root,false);
                foreach(float x in new[]{-1.08f,1.08f})for(int bar=0;bar<11;bar++)Box("Large bay rail",new Vector3(x,.8f,-1.4f+bar*.28f),new Vector3(.035f,1.8f,.035f),metal,root,false);
                Box("Large bay roof",new Vector3(0,1.75f,0),new Vector3(2.2f,.04f,3.05f),metal,root,false);
                foreach(float x in new[]{-.85f,.85f})foreach(float z in new[]{-1f,1f})Shape("Module transport wheel",PrimitiveType.Cylinder,new Vector3(x,-.55f,z),new Vector3(.65f,.13f,.65f),dark,root).transform.localRotation=Quaternion.Euler(0,0,90);
                return position;
            }
            int index=ordinary++,row=index%3,tier=index/3;float height=tier<2?tier*.55f:1.38f+(tier-2)*.55f;var p=VanPoint+new Vector3(-1.1f,1.02f+height,-.65f+row*1.05f);
            if(index==6){for(int bar=0;bar<12;bar++)foreach(float x in new[]{-1.65f,-.55f}){var rail=Box("Upper transport compartment rail",Vector3.zero,new Vector3(.025f,1.06f,.025f),metal,cargoRoot,false);rail.transform.position=VanPoint+new Vector3(x,2.86f,-1+bar*.28f);}}
            if(tier>0){var shelf=Box("Additional cargo shelf",p-Vector3.up*.05f,new Vector3(1.03f,.035f,.99f),metal,cargoRoot,false);shelf.transform.position=p-Vector3.up*.05f;}
            return p;
        }
        GameObject BuildCargoAnimal(AnimalRecord animal,Vector3 position,bool depot){
            var root=new GameObject("Loaded "+animal.species);root.transform.SetParent(cargoRoot);root.transform.position=position;
            var definition=game.config.content.species.First(s=>s.id==animal.species);
            if(animal.bagged){float width=definition.size=="Large"?.9f:.45f;var bag=Shape("Loaded sealed remains bag",PrimitiveType.Capsule,new Vector3(0,width*.5f,0),new Vector3(width,definition.lengthM*.55f,width),dark,root.transform);bag.transform.localRotation=Quaternion.Euler(90,0,0);var collider=root.AddComponent<BoxCollider>();collider.size=new Vector3(width,width,definition.lengthM*1.1f);collider.center=Vector3.up*width*.5f;collider.isTrigger=true;}
            else{var copy=JsonUtility.FromJson<AnimalRecord>(JsonUtility.ToJson(animal));copy.behaviorSaved=false;var model=root.AddComponent<DemoAnimal>();model.record=copy;model.world=this;model.MakeModel();model.enabled=false;if(animal.dead)root.transform.rotation=Quaternion.Euler(0,0,80);}
            var target=root.GetComponent<DemoTarget>()??root.AddComponent<DemoTarget>();target.kind=depot?"depotAnimal":"cargo";if(depot)root.AddComponent<DepotAnimal>().animalId=animal.id;return root;
        }
        bool ShowAdditionalHeldCargo(Kit kit){
            var j=game.save.job;if(j==null||j.contractIndex==0||string.IsNullOrEmpty(j.carriedAnimal))return false;
            var animal=j.animals.FirstOrDefault(a=>a.id==j.carriedAnimal)??game.save.cargo.FirstOrDefault(a=>a.id==j.carriedAnimal);if(animal==null)return false;
            if(kit!=Kit.Corpse&&!DemoGame.IsCage(kit)&&kit!=Kit.Net)return false;
            if(animal.bagged){float width=game.config.content.species.First(s=>s.id==animal.species).size=="Large"?.68f:.4f;Shape("Occupied remains bag",PrimitiveType.Capsule,new Vector3(.12f,-.23f,.85f),new Vector3(width,.24f,.32f),dark,handRoot).transform.localRotation=Quaternion.Euler(0,0,90);Box("Bag carrying straps",new Vector3(.12f,-.1f,.85f),new Vector3(width*.7f,.035f,.07f),cream,handRoot,false);return true;}
            if(kit!=Kit.Corpse)BuildAdditionalEquipment(kit,handRoot,true);
            var root=new GameObject("Held captive "+animal.species).transform;root.SetParent(handRoot,false);root.localPosition=new Vector3(.27f,-.16f,.75f);
            var model=root.gameObject.AddComponent<DemoAnimal>();model.world=this;model.record=JsonUtility.FromJson<AnimalRecord>(JsonUtility.ToJson(animal));model.MakeModel();model.enabled=false;
            float length=game.config.content.species.First(s=>s.id==animal.species).lengthM;root.localScale=Vector3.one*Mathf.Min(1,.32f/length);foreach(var c in root.GetComponentsInChildren<Collider>())c.enabled=false;foreach(var target in root.GetComponentsInChildren<DemoTarget>())Destroy(target);return true;
        }
    }
}
