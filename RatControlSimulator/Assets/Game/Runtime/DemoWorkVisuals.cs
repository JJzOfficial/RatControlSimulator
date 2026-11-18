using UnityEngine;
namespace Riverbend {
    public partial class DemoWorld {
        void BuildAdditionalWorkVisuals(){
            if(ActiveMap!=1&&ActiveMap!=4)return;
            for(int i=0;i<(ActiveMap==1?3:2);i++){var root=workTargets[i].transform;foreach(var r in root.GetComponentsInChildren<Renderer>())r.enabled=false;
                if(ActiveMap==4){Shape("Affected tree segment",PrimitiveType.Cylinder,new Vector3(0,.3f,0),new Vector3(.4f,.65f,.4f),wood,root);for(int n=0;n<5;n++)Shape("Round borer hole",PrimitiveType.Sphere,new Vector3(.2f,.1f+n*.12f,.04f),new Vector3(.03f,.03f,.03f),dark,root);Box("Treatment band",new Vector3(0,.35f,0),new Vector3(.45f,.08f,.45f),amber,root,false);}
                if(ActiveMap==4&&i==1){Box("Affected fruit tray",new Vector3(0,.08f,0),new Vector3(.8f,.15f,.6f),wood,root,false);for(int n=0;n<6;n++){var p=new Vector3(-.22f+n%3*.22f,.25f,n/3*.23f-.1f);Shape("Damaged fruit",PrimitiveType.Sphere,p,Vector3.one*.2f,amber,root);Shape("Fruit feeding scar",PrimitiveType.Sphere,p+new Vector3(0,.08f,-.04f),Vector3.one*.055f,dark,root);}Box("Quarantine tag",new Vector3(.43f,.25f,0),new Vector3(.04f,.28f,.22f),cream,root,false);}
                else if(ActiveMap==1&&i==0){Box("Quarantine cloth basket",Vector3.zero,new Vector3(.7f,.4f,.6f),wood,root,false);for(int n=0;n<4;n++)Box("Infested folded cloth",new Vector3(0,.25f+n*.06f,0),new Vector3(.6f,.05f,.45f),cream,root,false);}
                else if(ActiveMap==1&&i==1){for(int n=0;n<5;n++)Box("Damaged timber",new Vector3((n%2)*.18f-.1f,n*.06f,0),new Vector3(.13f,.08f,.85f),wood,root,false);for(int n=0;n<6;n++)Shape("Timber galleries",PrimitiveType.Sphere,new Vector3(.12f,.14f,-.3f+n*.12f),new Vector3(.035f,.02f,.025f),dark,root);}
                else if(ActiveMap==1){Box("Isolated grain pallet",Vector3.zero,new Vector3(.9f,.12f,.9f),wood,root,false);for(int n=0;n<3;n++)Shape("Affected grain sack",PrimitiveType.Capsule,new Vector3(-.25f+n*.25f,.25f,0),new Vector3(.23f,.3f,.55f),cream,root);Box("Quarantine marking",new Vector3(0,.5f,-.3f),new Vector3(.65f,.08f,.03f),amber,root,false);}
            }
        }
    }
}
