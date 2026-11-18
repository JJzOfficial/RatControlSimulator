using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Riverbend {
    public partial class DemoGame {
        void NormalizeSlots(){var j=save.job;while(j.itemIds.Count<j.kit.Count)j.itemIds.Add(Guid.NewGuid().ToString("N"));while(j.conditions.Count<j.kit.Count)j.conditions.Add(j.kit[j.conditions.Count]==0?100:j.kit[j.conditions.Count]==1?j.cleaning:j.kit[j.conditions.Count]==2?j.repairs:100);while(j.quantities.Count<j.kit.Count)j.quantities.Add(1);while(j.cargoIds.Count<j.kit.Count)j.cargoIds.Add(null);while(j.baitedSlots.Count<j.kit.Count)j.baitedSlots.Add(false);}
        void SyncSelectedCargo(){NormalizeSlots();var j=save.job;j.carriedAnimal=selected>=0&&selected<j.cargoIds.Count?j.cargoIds[selected]:null;j.carriedSlot=string.IsNullOrEmpty(j.carriedAnimal)?-1:selected;}
        void InitializeJob(JobRecord j) {
            InitializeLayout(j);
            GrantStarter(j);
        }
        void InitializeLayout(JobRecord j){j.workPositions.Clear();j.layoutSeed=UnityEngine.Random.Range(1,int.MaxValue);j.rareAt=UnityEngine.Random.Range(180f,300f);j.rareWillSpawn=UnityEngine.Random.value<.45f;}
        void AdvanceRare(float seconds) {
            if(save.job.contractIndex>0){AdvanceAdditionalContract(seconds);return;}
            var j=save.job;if(Paused||Phase!=ShiftPhase.Night||j.rareSpawned)return;
            j.nightSeconds+=seconds;if(j.nightSeconds<j.rareAt)return;
            if(!j.rareRolled){j.rareRolled=true;Persist();}
            if(!j.rareWillSpawn)return;
            var points=new[]{new Vector3(23,.08f,20),new Vector3(-23,.08f,20),new Vector3(20,.08f,-15),new Vector3(-23,.08f,-15)};
            foreach(var p in points){var v=world.cam.WorldToViewportPoint(p+Vector3.up*.2f);bool seen=v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1&&world.ClearLine(world.cam.transform.position,p+Vector3.up*.2f);
                if(Vector3.Distance(world.controller.transform.position,p)<10||seen)continue;
                var a=new AnimalRecord{id=j.runId+"-C01",species="C01",runId=j.runId,position=p};j.animals.Add(a);world.CreateAnimal(a);j.rareSpawned=true;Persist();break;
            }
        }
        bool IsSupply(Kit? k)=>k==Kit.SprayRefill||k==Kit.RepairPack||k==Kit.CleaningRefill||k==Kit.Bait||k==Kit.PoisonBait;
        float ToolCondition(Kit k) {
            var j=save.job;
            if(k==Kit.Spray)return Mathf.Clamp01((j.spray-j.spraySeconds/4f)/5f);
            if(k==Kit.Repair)return Mathf.Clamp01(j.repairs/100f);
            if(k==Kit.Broom)return Mathf.Clamp01(j.cleaning/100f);
            return -1;
        }
        string SupplyHint()=>CurrentKit==Kit.Bait?L("手持诱饵 · 对准已布置的笼/夹，右键添加","Bait · aim at a placed cage/trap, RMB add"):L("手持补给 · 右键补充背包中的对应工具","Supply · RMB refill the matching backpack tool");
        void UseSupply() {
            if(Paused||!IsSupply(CurrentKit))return;
            NormalizeSlots();var j=save.job;var supply=CurrentKit.Value;
            if(supply==Kit.Bait||supply==Kit.PoisonBait){
                if(!target||target.kind!="device"||Vector3.Distance(world.cam.transform.position,target.transform.position)>2.8f||!world.ClearLine(world.cam.transform.position,target.transform.position+Vector3.up*.2f)){Notify(L("请靠近并对准已放置的笼或夹","Aim at a nearby placed cage or trap"));return;}
                var d=target.GetComponent<DemoDevice>().record;
                if(d.kit==(int)Kit.Camera||((d.kit==(int)Kit.BaitStation)!=(supply==Kit.PoisonBait))){Notify(L("封装药饵仅用于药饵站；普通诱饵用于笼和夹","Poison only fits enclosed stations; ordinary bait fits cages and traps"));return;}
                if(d.triggered||d.baited){Notify(L("装置已触发或已有诱饵，请先复位","Reset the triggered or already baited device first"));return;}
                RecordKitUse(supply);d.baited=true;world.Shape("Bait",PrimitiveType.Sphere,new Vector3(0,.07f,-.15f),Vector3.one*.06f,world.amber,target.transform);ConsumeSelected();Persist();Notify(L("诱饵已添加","Bait added"));return;
            }
            Kit tool=supply==Kit.SprayRefill?Kit.Spray:supply==Kit.RepairPack?Kit.Repair:Kit.Broom;
            if(!j.kit.Contains((int)tool)){Notify(L("请先把对应工具放入背包","Collect the matching tool into your backpack first"));return;}
            int matching=j.kit.IndexOf((int)tool);if(j.kit.Count(k=>k==(int)tool)>1){refillKit=tool;Open("refill");return;}LoadToolState(matching);
            RefillSlot(matching);
        }
        void RefillSlot(int matching){var j=save.job;var tool=(Kit)j.kit[matching];LoadToolState(matching);
            float capacity=100;
            float current=tool==Kit.Spray?(j.spray-j.spraySeconds/4f)*20:tool==Kit.Repair?j.repairs:j.cleaning;
            int amount=Mathf.Min(j.quantities[selected],Mathf.CeilToInt(capacity-current));
            if(amount<=0){Notify(L("工具耐久已满","Tool condition is already full"));return;}
            float restored=Mathf.Min(capacity,current+amount);
            if(tool==Kit.Spray){j.spray=Mathf.CeilToInt(restored/20);j.spraySeconds=(j.spray-restored/20)*4;}
            else if(tool==Kit.Repair)j.repairs=Mathf.RoundToInt(restored);else j.cleaning=Mathf.RoundToInt(restored);
            SaveToolStateFor(matching);ConsumeSelected(amount);Persist();Notify(L("补充完成","Refilled"));
        }
        void ConsumeSelected(int amount=1) {
            var j=save.job;while(j.quantities.Count<j.kit.Count)j.quantities.Add(1);
            j.quantities[selected]-=amount;if(j.quantities[selected]<=0)RemoveSlot(selected);
        }
        void CleanArea(Vector3 center){foreach(var p in world.pests)if(p&&p.record.dead&&!p.record.cleaned&&Vector3.Distance(center,p.transform.position)<=2&&world.ClearLine(center+Vector3.up*.15f,p.transform.position+Vector3.up*.15f))CleanPest(p);}
        void CleanPest(DemoPest p) {
            p.record.cleaned=true;
            if(save.encyclopedia.Contains(p.record.species))p.gameObject.SetActive(false);
            else { // Leave one labelled sample per source until first investigation.
                bool sample=world.pests.Any(x=>x&&x!=p&&x.record.source==p.record.source&&x.record.cleaned&&x.gameObject.activeSelf);
                if(sample)p.gameObject.SetActive(false);else p.gameObject.name="Retained investigation sample "+p.record.species;
            }
        }
        void DrawActionRing(float fraction) {
            for(int i=0;i<64;i++) {float a=i*Mathf.PI*2/64-Mathf.PI*.5f;RectFill(new Rect(800+Mathf.Cos(a)*25-2,450+Mathf.Sin(a)*25-2,4,4),i<64*fraction?green:muted);}
            if(CurrentKit.HasValue)Icon(CurrentKit.Value,791,441,18,ink);
        }
        string WorkStatus(int i) {
            var j=save.job;if(!WorkUsesCleaning(i))return j.work[i]>0?L("已维修","Repaired"):L("待维修","Repair needed");
            return (j.pests.Where(p=>p.source==i).All(p=>p.dead)?L("灭杀 ✓","Kill ✓"):L("待灭杀","Kill needed"))+" / "+(j.work[i]>=3?L("清洁 ✓","Clean ✓"):L("待清洁","Clean needed"));
        }
    }
    public partial class DemoWorld {
        public void DisableHandShadows(){if(handRoot)foreach(var r in handRoot.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;}
        public void RestoreWorkLayout() {
            if(RestoreAdditionalWorkLayout())return;
            var j=game.save.job;
            if(j.workPositions.Count!=5) {
                j.workPositions.Clear();var rng=new System.Random(j.layoutSeed==0?j.runId.GetHashCode():j.layoutSeed);
                Vector3[][] candidates={
                    new[]{new Vector3(5.85f,.5f,2),new Vector3(5.85f,.5f,.1f),new Vector3(5.85f,.5f,3.5f)},
                    new[]{new Vector3(13.8f,.45f,13),new Vector3(14,.45f,16),new Vector3(10,.45f,17)},
                    new[]{new Vector3(6,.8f,8),new Vector3(6,.8f,7),new Vector3(6,.8f,8.6f)},
                    new[]{new Vector3(8.13f,.28f,1),new Vector3(8.13f,.28f,3),new Vector3(8.13f,.28f,7)},
                    new[]{new Vector3(-8.13f,.28f,2),new Vector3(-8.13f,.28f,4),new Vector3(-8.13f,.28f,7)}};
                for(int i=0;i<5;i++){
                    var list=candidates[i].OrderBy(_=>rng.Next()).ToArray();bool found=false;
                    foreach(var p in list){var approach=p+new Vector3(i==3?1:i==4?-1:-.65f,0,0);approach.y=.08f;
                        if(Physics.CheckCapsule(approach+Vector3.up*.35f,approach+Vector3.up*1.5f,.3f,1<<9)||Route(j.player,approach).Count==0)continue;
                        if(j.workPositions.Any(x=>Vector3.Distance(x,p)<1))continue;j.workPositions.Add(p);found=true;break;
                    }
                    if(!found)throw new InvalidOperationException("No reachable work anchor for "+i);
                }
            }
            for(int i=0;i<5;i++)workTargets[i].transform.position=j.workPositions[i];
            for(int i=3;i<5;i++){
                float x=i==3?8:-8,z=j.workPositions[i].z;
                foreach(Transform child in siteRoot){if(Mathf.Abs(child.position.x-x)>.01f||Mathf.Abs(child.position.y-.2f)>.01f)continue;
                    if(child.name=="Low wall front"){child.position=new Vector3(x,.2f,(-2+z-.4f)*.5f);child.localScale=new Vector3(.22f,.4f,z+1.6f);}
                    if(child.name=="Low wall rear"){child.position=new Vector3(x,.2f,(z+.4f+10)*.5f);child.localScale=new Vector3(.22f,.4f,9.6f-z);}
                }
            }
            Physics.SyncTransforms();BuildNavigation();
        }
        public Vector3 FleeDestination(DemoAnimal animal,Queue<Vector3> recent) {
            Vector3 start=animal.transform.position,player=controller.transform.position;float best=-10000;Vector3 result=start;
            var options=new List<Vector3>();
            for(int i=0;i<10;i++){float a=i*Mathf.PI*.2f;options.Add(start+new Vector3(Mathf.Cos(a)*4,0,Mathf.Sin(a)*4));}
            options.AddRange(new[]{new Vector3(0,.08f,3),new Vector3(0,3.36f,9.5f),new Vector3(-5,.08f,3),new Vector3(5,3.36f,3),new Vector3(10,.08f,16),new Vector3(-10,.08f,12)});
            foreach(var p in options){var path=Route(start,p);if(path.Count<2)continue;var end=path[path.Count-1];float distance=Vector3.Distance(end,player);float score=distance-Vector3.Distance(start,end)*.2f;
                if(Vector3.Distance(end,start)<1)continue;
                bool crosses=path.Any(n=>Vector3.Distance(n,player)<1);if(crosses)score-=30;
                if(!ClearLine(end+Vector3.up*.15f,player+Vector3.up*.6f))score+=4;
                if(Vector3.Dot((end-player).normalized,controller.transform.forward)>.65f)score-=3;
                foreach(var r in recent)if(Vector3.Distance(end,r)<2)score-=5;
                int exits=0;foreach(var dir in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})if(ClearLine(end+Vector3.up*.3f,end+Vector3.up*.3f+dir))exits++;score+=exits*.7f;
                if(score>best){best=score;result=end;}
            }return result;
        }
    }
}
