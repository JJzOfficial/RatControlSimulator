using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoWorld {
        void CreateContractPestRecords(){
            var j=game.save.job;var c=game.config.content.contracts[j.contractIndex];
            for(int species=0;species<c.guaranteedInsects.Length;species++){
                string id=c.guaranteedInsects[species];int source=j.contractIndex==3?species%3:species%2;var definition=game.config.content.species.First(s=>s.id==id);
                for(int n=0;n<5;n++){
                    var p=j.workPositions[source]+new Vector3((n%3-1)*.3f,0,(n/3)*.3f);
                    if(definition.family=="InsectFly"||definition.family=="InsectNest")p.y+=.8f;
                    else if(Physics.Raycast(p+Vector3.up,Vector3.down,out var hit,3,1<<9))p.y=hit.point.y+.1f;
                    j.pests.Add(new PestRecord{id=j.runId+"-"+id+"-"+n,species=id,source=source,position=p});
                    if(j.contractIndex==12&&id=="I12")j.pests.Add(new PestRecord{id=j.runId+"-"+id+"-tree2-"+n,species=id,source=1,position=j.workPositions[1]+new Vector3((n%3-1)*.12f,.2f,n/3*.12f)});
                }
            }
        }
    }
    public partial class DemoPest {
        float nestWarning,nestRest;
        DemoCatalog.Species PestDefinition=>world.game.config.content.species.First(s=>s.id==record.species);
        void BuildAdditionalPest(){
            var d=PestDefinition;float size=record.species=="I02"?.7f:record.species=="I06"?.4f:1;
            var coat=record.species=="I08"?world.cream:record.species=="I10"?world.amber:world.fur;
            world.Shape("Insect abdomen",PrimitiveType.Sphere,Vector3.zero,new Vector3(.1f,.05f,.16f)*size,coat,transform);
            world.Shape("Insect thorax",PrimitiveType.Sphere,new Vector3(0,0,.08f)*size,new Vector3(.07f,.06f,.07f)*size,coat,transform);
            world.Shape("Insect head",PrimitiveType.Sphere,new Vector3(0,.01f,.13f)*size,Vector3.one*.06f*size,world.dark,transform);
            foreach(float side in new[]{-1f,1f}){
                for(int i=0;i<3;i++)world.Box("Jointed leg",new Vector3(side*.065f,-.015f,-.04f+i*.06f)*size,new Vector3(.1f,.008f,.008f)*size,world.dark,transform,false);
                var antenna=world.Box("Antenna",new Vector3(side*.03f,.02f,.2f)*size,new Vector3(.008f,.008f,record.species=="I12"?.25f:.1f)*size,world.dark,transform,false);antenna.transform.localRotation=Quaternion.Euler(0,side*20,0);
            }
            if(d.family=="InsectFly"||d.family=="InsectNest"||record.species=="I07"){
                left=world.Shape("Left wing",PrimitiveType.Sphere,new Vector3(-.07f,.025f,0),new Vector3(.14f,.008f,.1f),world.cream,transform).transform;
                right=world.Shape("Right wing",PrimitiveType.Sphere,new Vector3(.07f,.025f,0),new Vector3(.14f,.008f,.1f),world.cream,transform).transform;
            }
            if(record.species=="I02")foreach(float x in new[]{-.025f,.025f})world.Box("Two dorsal stripes",new Vector3(x,.03f,.055f),new Vector3(.01f,.01f,.08f),world.dark,transform,false);
            if(record.species=="I04"||record.species=="I11")world.Box("Forward mouthpart",new Vector3(0,0,.2f),new Vector3(.014f,.014f,.12f),world.dark,transform,false);
            if(record.species=="I09")for(int i=-1;i<=1;i++)world.Box("Three tail filaments",new Vector3(i*.035f,0,-.16f),new Vector3(.006f,.006f,.16f),world.cream,transform,false);
            if(record.species=="I13")foreach(float side in new[]{-1f,1f})world.Box("Jumping hind leg",new Vector3(side*.08f,.055f,-.08f),new Vector3(.025f,.14f,.025f),world.foliage,transform,false);
            if(record.species=="I10")for(int i=0;i<3;i++)world.Box("Wasp warning stripe",new Vector3(0,.03f,-.06f+i*.04f),new Vector3(.1f,.01f,.014f),world.dark,transform,false);
            if(record.species=="I14")world.Box("Shield back",new Vector3(0,.025f,-.02f),new Vector3(.13f,.018f,.13f),world.wood,transform,false).transform.localRotation=Quaternion.Euler(0,45,0);
            var col=gameObject.AddComponent<SphereCollider>();col.radius=.16f;col.isTrigger=true;gameObject.AddComponent<DemoTarget>().kind="pest";
            if(record.species=="I10"&&!record.id.StartsWith("guide-")&&!world.dynamicRoot.Find("Wasp nest "+record.source)){var nest=new GameObject("Wasp nest "+record.source).transform;nest.SetParent(world.dynamicRoot);nest.position=home+Vector3.up*.3f;world.Shape("Layered nest",PrimitiveType.Sphere,Vector3.zero,new Vector3(.4f,.5f,.4f),world.wood,nest);world.Shape("Nest opening",PrimitiveType.Sphere,new Vector3(0,-.2f,.16f),new Vector3(.12f,.12f,.03f),world.dark,nest);}
        }
        void UpdateAdditionalPest(){
            var d=PestDefinition;float t=Time.time+seed;Vector3 desired=home;
            if(record.species=="I10"){nestRest=Mathf.Max(0,nestRest-Time.deltaTime);float distance=Vector3.Distance(world.controller.transform.position,home);if(distance<3&&world.ClearLine(home,world.controller.transform.position+Vector3.up)){if(nestWarning<=0)world.game.Notify(world.game.L("蜂巢警告：退到4米外可避免防御","Nest warning: retreat beyond 4m to avoid defense"));nestWarning+=Time.deltaTime;if(nestWarning>=.6f&&nestRest<=0){world.game.HurtPlayer(3,false);nestRest=2;}}else if(distance>4)nestWarning=0;}
            if(d.family=="InsectFly"||d.family=="InsectNest"){
                desired+=new Vector3(Mathf.Sin(t*1.3f)*.7f,Mathf.Sin(t*2)*.3f,Mathf.Cos(t*1.6f)*.7f);
                if(left){left.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*80)*45);right.localRotation=Quaternion.Euler(0,0,-Mathf.Sin(t*80)*45);}
            }else if(d.family=="CropInsect"){
                if(record.species=="I13")desired+=new Vector3(Mathf.Sin(t)*.5f,Mathf.Max(0,Mathf.Sin(t*3))*.25f,Mathf.Cos(t)*.5f);
                else desired+=new Vector3(Mathf.Sin(t*.4f)*.1f,0,Mathf.Cos(t*.4f)*.1f);
            }else desired+=record.species=="I06"?new Vector3(Mathf.Sin(t*.4f)*.8f,0,0):new Vector3(Mathf.Sin(t*.6f)*.3f,0,Mathf.Cos(t*.5f)*.25f);
            if(world.ClearLine(transform.position+Vector3.up*.04f,desired+Vector3.up*.04f))transform.position=Vector3.MoveTowards(transform.position,desired,Mathf.Max(.1f,d.walkSpeed)*Time.deltaTime);
            record.position=transform.position;
        }
    }
}
