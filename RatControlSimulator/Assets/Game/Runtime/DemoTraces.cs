using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        void InspectTrace(DemoTarget trace){var j=save.job;if(j.traceMarks.Contains(trace.index))return;j.traceMarks.Add(trace.index);j.traceCount=j.traceMarks.Count;trace.gameObject.SetActive(false);Notify(L("已确认活动路线","Activity route confirmed"));Persist();}
        void IdentifyScannedTargets(){if(save.job.contractIndex==0)return;foreach(var a in world.animals.Values)if(Vector3.Distance(world.controller.transform.position,a.transform.position)<=SenseRadius)a.record.identified=true;}
    }
    public partial class DemoWorld {
        void RestoreContractInteractions(){
            var j=game.save.job;if(j.contractIndex==0)return;if(j.traceMarks==null)j.traceMarks=new System.Collections.Generic.List<int>();
            for(int i=0;i<2;i++){
                if(j.traceMarks.Contains(i))continue;
                Vector3 p=j.contractIndex==13?j.workPositions[4]+Vector3.left:new Vector3(FeedingPoints[i].x,FeedingPoints[i].y,FeedingPoints[i].z-2);
                if(ActiveMap==1)p=new Vector3(i==0?-5:5,3.58f,7);
                p=ClosestNode(p);var root=new GameObject("Activity trace "+i);root.transform.SetParent(dynamicRoot);root.transform.position=p;
                for(int n=0;n<5;n++)Shape("Track mark",PrimitiveType.Sphere,new Vector3(n%2==0?-.1f:.1f,.015f,n*.14f),new Vector3(.12f,.025f,.15f),dark,root.transform);
                var t=root.AddComponent<DemoTarget>();t.kind="trace";t.index=i;var col=root.AddComponent<SphereCollider>();col.radius=.45f;col.isTrigger=true;
            }
        }
    }
}
