#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RunContinuationChecks(){StartVerification(ContinuationChecks(),"continuation");}
        IEnumerator ContinuationChecks(){
            var original=save;bool oldFrontend=frontend;int oldSelected=selected;string oldPath=DemoSave.testPath;
            string directory=Path.GetFullPath("Library/P0ContinuationChecks");Directory.CreateDirectory(directory);
            var log=new List<string>();DemoSave.testPath=Path.Combine(directory,"isolated-profile.json");
            frontend=false;save=new SaveRecord();ShowCompany();yield return null;
            try {
                StartContract();var j=save.job;
                j.kit.Add((int)Kit.Repair);j.itemIds.Add("timing-repair");j.quantities.Add(1);j.conditions.Add(100);NormalizeSlots();Select(0);j.repairs=100;
                target=world.workTargets[2];GestureInput(true,true,false,false,false,false,.35f);GestureInput(false,true,false,false,false,false,1.5f);
                Require(j.work[2]==0&&Mathf.Abs(actionProgress/heldDuration-.5f)<.001f,"E ring and commit share halfway clock");
                GestureInput(false,true,false,false,false,false,1.49f);Require(j.work[2]==0&&actionProgress<heldDuration,"E cannot commit early");
                GestureInput(false,true,false,false,false,false,.02f);Require(j.work[2]==1&&j.repairs==90,"E completes once at duration");
                GestureInput(false,true,false,false,false,false,5);Require(j.repairs==90,"held E cannot repeat purchase");GestureInput(false,false,true,false,false,false,0);
                target=world.workTargets[3];GestureInput(true,true,false,false,false,false,1.35f);target=world.workTargets[4];GestureInput(false,true,false,false,false,false,5);Require(j.work[3]==0&&j.work[4]==0&&actionProgress==0,"changing aim cancels work");ResetGestures();
                log.Add("PASS E halfway / pre-completion / exact commit / held-repeat / aim cancellation");
                var positions=j.workPositions.ToArray();Persist();Reload();Require(save.job.workPositions.SequenceEqual(positions),"load preserves original anchors");
                var layouts=new HashSet<string>();for(int n=0;n<12;n++){InitializeLayout(save.job);world.RestoreWorkLayout();layouts.Add(string.Join(";",save.job.workPositions.Select(p=>p.ToString("F2"))));}
                Require(layouts.Count>1,"new job reselects original candidates");log.Add("PASS original candidate pool changes with new job seed; load retains exact positions");
                for(int map=1;map<6;map++){
                    save.job=new JobRecord{runId="map-test-"+map,mapIndex=map,layoutSeed=123+map,player=new Vector3(-14,.1f,-5)};
                    world.SetDepot(false);world.RestoreJob();world.VerifyAdditionalMap();
                    log.Add("PASS L0"+(map+1)+" geometry / all work approaches / truck to assets / no duplicate target list");
                }
                CheckResult="Passed";
            }catch(Exception e){CheckResult="Failed: "+e;Debug.LogException(e);}
            log.Add(CheckResult);File.WriteAllLines(Path.Combine(directory,"results.txt"),log);
            DemoSave.testPath=oldPath;save=original;selected=oldSelected;frontend=oldFrontend;ResetGestures();
            if(save.atDepot)ShowCompany();else {world.SetDepot(false);world.RestoreJob();Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;SyncPause();}
            Debug.Log("[P0 continuation] "+CheckResult);
        }
    }
    public partial class DemoWorld {
        public void VerifyAdditionalMap(){
            if(workTargets.Count!=5||game.save.job.workPositions.Count!=5)throw new Exception("Work target duplication");
            foreach(var p in FeedingPoints){var node=ClosestNode(p);if(Vector3.Distance(node,p)>1.5f||Route(new Vector3(-14,.1f,-5),node).Count==0)throw new Exception("Asset not reachable: "+ActiveMap+" "+p+" closest "+node+"; "+ExplainSiteRoute());}
            foreach(var p in game.save.job.workPositions){var node=ClosestNode(p+Vector3.left*.8f);if(Vector3.Distance(node,p)>2||Route(game.save.job.player,node).Count==0)throw new Exception("Work not reachable: "+p);}
            var destination=FeedingPoints[4];var path=Route(new Vector3(-14,.1f,-5),destination);
            controller.enabled=false;controller.transform.position=path[0]+Vector3.up*.04f;controller.enabled=true;
            for(int node=1;node<path.Count;node++){
                int frames=0;while(frames++<180){var delta=path[node]-controller.transform.position;delta.y=0;if(delta.magnitude<.09f)break;controller.Move(delta.normalized*.05f+Vector3.down*.08f);}
                var flat=path[node]-controller.transform.position;flat.y=0;if(flat.magnitude>.3f)throw new Exception("Controller route blocked at map "+ActiveMap+" "+path[node]+" from "+controller.transform.position);
            }
            if(Mathf.Abs(controller.transform.position.y-destination.y)>.35f)throw new Exception("Controller did not reach asset level "+ActiveMap+" "+controller.transform.position);
        }
        string ExplainSiteRoute(){
            var lines=new List<string>();for(int z=3;z<10;z++){var a=ClosestNode(new Vector3(-26,.08f,z));var b=ClosestNode(new Vector3(-26,.08f,z+1));var delta=b-a;
                bool hit=Physics.CapsuleCast(a+Vector3.up*.65f,a+Vector3.up*1.45f,.3f,delta.normalized,out var h,delta.magnitude,1<<9);
                lines.Add(a+"->"+b+" "+(hit?h.collider.name+"@"+h.distance:"clear")+" path="+Route(new Vector3(-14,.1f,-5),a).Count);}
            return string.Join("; ",lines);
        }
    }
}
#endif
