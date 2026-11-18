using System;
using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        int guidePage;
        void AdditionalContractPanel(){
            if(Button(335,733,260,35,L("查看监测相机","View monitoring feeds"))){panel="monitor";return;}
            if(Button(620,733,400,35,L("线索调查所需借物","Borrow clue investigation gear"))){BorrowOptionalClueKit();return;}
            if(Button(1050,733,175,35,L("图鉴","Field guide"))){panel="journal";tab=1;return;}
            var j=save.job;var c=Contract(j);Label(335,185,890,140,ContractDescription(c),small);
            Label(335,335,890,45,L("受控目标 ","Controlled targets ")+j.Handled+" / "+c.RequiredCount+"   "+L("财产 ","Property ")+j.TotalDurability/5+"%   "+Mathf.FloorToInt(j.nightSeconds)+"s",heading);
            for(int i=0;i<5;i++)Label(335,390+i*34,890,32,WorkName(i)+" · "+WorkStatus(i),small);
            bool canFinishWatch=c.type=="Protect"&&j.stage==2&&j.animals.Count(a=>a.required)==c.RequiredCount&&j.Handled==c.RequiredCount;
            if(canFinishWatch&&Button(335,574,890,42,L("完成剩余值守（目标预算已全部处理）","Complete remaining watch (all target budgets controlled)"))){j.nightSeconds=Mathf.Max(j.nightSeconds,c.durationSeconds);Persist();}
            if(Button(335,628,890,46,L("复核通过并回公司结算","Finalize success and return to depot"),j.CanDepart)){FinalizeJob(true);return;}
            if(Button(335,687,430,42,L("返回工具与补给","Tools / supplies"))){panel="van";tab=0;}
            if(Button(790,687,435,42,L("终止本次委托","Abort contract")))Open("abort");
        }
        DemoCatalog.Contract Contract(JobRecord j)=>config.content.contracts[Mathf.Clamp(j.contractIndex,0,17)];
        bool ContractAvailable(int index){NormalizeContractHistory();if(index<0||index>=18)return false;var c=config.content.contracts[index];return string.IsNullOrEmpty(c.unlockAfter)||save.completedContracts.Contains(c.unlockAfter);}
        string ContractDescription(DemoCatalog.Contract c){return c.id+" · "+(save.english?c.en:c.zh)+"  "+c.type+" · "+L("目标预算 ","Target budget ")+c.RequiredCount+(c.durationSeconds>0?" / "+c.durationSeconds+"s":"")+"\n"+L("必做：","Work: ")+(c.requiredWorks.Length==0?L("无","none"):string.Join(" / ",c.requiredWorks))+"\n"+L("挑战：","Stars: ")+string.Join(" / ",c.starPredicates.Select(s=>s.zh));}
        void FinalizeAdditionalContract(bool success,bool failed){
            SaveToolState();NormalizeContractHistory();var candidate=JsonUtility.FromJson<SaveRecord>(JsonUtility.ToJson(save));var j=candidate.job;var c=Contract(j);int cash=0,xp=0,newStars=0;
            if(success){int stars=EvaluateAdditionalStars(j),newBits=stars&~candidate.contractStars[j.contractIndex];for(int i=0;i<3;i++)if((newBits&(1<<i))!=0)newStars++;
                candidate.contractStars[j.contractIndex]|=stars;if(!candidate.completedContracts.Contains(c.id))candidate.completedContracts.Add(c.id);
                cash=c.rewardCash+newStars*c.firstStarCash;xp=c.rewardXP+newStars*c.firstStarXP;candidate.cash+=cash;candidate.xp+=xp;candidate.successes++;
            }
            bool retainCargo=!failed&&(success||c.abortPolicy=="KeepLoadedAnimalsWithoutContractReward");
            if(retainCargo)foreach(var a in j.animals.Where(a=>a.loaded&&!a.terminal&&(!a.deliveryTarget||!success)))candidate.cargo.Add(a);
            foreach(var a in j.animals.Where(a=>a.deliveryTarget&&success)){a.terminal=true;a.loaded=false;}
            CarryEquipmentHome(candidate,j,!retainCargo);candidate.job.looseItems.RemoveAll(item=>j.borrowedItems.Contains(item.id));candidate.magazines.RemoveAll(m=>j.borrowedItems.Contains(m.itemId));candidate.snakeLiners.RemoveAll(id=>j.borrowedItems.Contains(id));candidate.installations.RemoveAll(i=>j.borrowedItems.Contains(i.itemId));candidate.atDepot=true;candidate.revision=4;
            try{DemoSave.Write(candidate);}catch(Exception e){Notify(L("结算保存失败：","Settlement save failed: ")+e.Message);return;}
            save=candidate;result=(success?L("委托成功","CONTRACT COMPLETE"):L("委托未完成","CONTRACT FAILED"))+" · "+(save.english?c.en:c.zh)+"\n+ ¥"+cash+" / "+xp+" XP\n"+L("新星：","New stars: ")+newStars;
            ShowCompany();Phase=ShiftPhase.Result;SyncPause();Sound("ui");
        }
        int EvaluateAdditionalStars(JobRecord j){
            int bits=0;var stars=Contract(j).starPredicates;for(int i=0;i<stars.Length;i++)if(AdditionalStar(stars[i],j))bits|=1<<i;return bits;
        }
        bool AdditionalStar(DemoCatalog.Star star,JobRecord j){
            float threshold;float.TryParse(star.threshold,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out threshold);
            switch(star.kind){
                case "AssetRatio":return j.TotalDurability/500f>=threshold;
                case "NoTool":return !j.usedKits.Contains((int)(j.contractIndex==2?Kit.BaitStation:Kit.Rifle));
                case "NoConsumable":return !j.usedKits.Contains((int)Kit.PoisonBait);
                case "NoToolSet":return j.contractIndex==10?!j.usedKits.Any(k=>k==(int)Kit.Drone||k==(int)Kit.Rifle||k==(int)Kit.Airgun):!j.usedKits.Any(k=>k==(int)Kit.BaitStation||k==(int)Kit.PoisonBait||k==(int)Kit.Rifle);
                case "NoSpeciesDeath":return !j.animals.Any(a=>a.species=="C05"&&a.dead);
                case "LocateHighTarget":return j.animals.Any(a=>a.required&&a.species=="M03"&&a.identified);
                case "ContainSpecies":return j.animals.Any(a=>a.species=="M11"&&a.contained&&!a.dead);
                case "InvestigateSpeciesSet":return j.investigatedSpecies.Contains("I12")&&j.investigatedSpecies.Contains("I14");
                case "NamedTargetLossMax":return j.animals.Where(a=>a.deliveryTarget).Sum(a=>a.assetDamage)/500f<=threshold;
                case "OnlyControlMethods":return j.animals.Where(a=>a.required).All(a=>a.controlMethod=="Cage");
                case "NoHandDamage":return j.handDamageEvents==0;
                case "InvestigateOrdinaryCorpse":return j.animals.Any(a=>a.dead&&a.investigated&&!a.species.StartsWith("C"));
                case "FleeEventsMax":return j.fleeEvents<=threshold;
                case "RequiredControlDeadline":case "RequiredCaptureDeadline":return j.controlTime>=0&&j.controlTime<=threshold;
                case "NoDirectShot":return j.directShots==0;
                case "TrapControls":return j.animals.Count(a=>a.controlMethod=="Trap")>=threshold;
                case "AllRequiredLive":return j.animals.Count(a=>a.required&&a.contained&&!a.dead)>=threshold;
                case "NoNonTargetSnakeDeath":return !j.animals.Any(a=>a.species.StartsWith("S")&&a.dead&&!a.required);
                case "SkunkSprayEventsMax":return j.skunkSprays<=threshold;
                case "DangerHitsMax":return j.dangerHits<=threshold;
                case "BoarHitsMax":return j.boarHits<=threshold;
                case "ContainOrdinaryCount":return j.animals.Count(a=>a.contained&&!a.dead&&!a.species.StartsWith("C"))>=threshold;
                case "CameraPassEvents":return j.cameraPasses>=threshold;
                case "CameraRequiredPass":return j.animals.Count(a=>a.required&&j.cameraTargets.Contains(a.id))>=threshold;
                case "RecoverIntactStolenItem":return j.recoveredItems>=threshold;
                case "MainTracesBeforeNight":return j.prepTraceCount>=threshold;
                case "InvestigateMainHighTraces":return j.traceCount>=threshold;
                case "RepairMainNetBeforeNight":return j.prepStar;
                case "MainSourcesBeforeNight":return j.sourcesPrepared;
                case "RepairMainPipes":return j.work[2]>0&&j.work[3]>0;
                case "NoNonTargetDeath":return !j.animals.Any(a=>!a.required&&a.dead)&&j.livestock.All(a=>a.health>0);
                case "IdentifyBeforeFirstAttack":return j.identifiedBeforeAttack&&j.animals.Where(a=>a.required).All(a=>a.identified);
                case "InvestigateRequiredCorpses":return j.animals.Count(a=>a.required&&a.dead&&a.investigated)>=threshold;
                case "InvestigatedRequiredCorpseLoaded":return j.animals.Any(a=>a.required&&a.dead&&a.investigated&&a.loaded);
                case "RecoverMainDevices":return j.devices.All(d=>j.borrowedItems.Contains(d.itemId));
                case "RecoverDevicesAndCorpseLoaded":return j.devices.All(d=>j.borrowedItems.Contains(d.itemId))&&j.animals.Any(a=>a.required&&a.dead&&a.loaded);
                case "OrdinaryAliveLoaded":return j.animals.Any(a=>a.loaded&&!a.dead&&!a.species.StartsWith("C"));
                case "ThiefAliveLoaded":return j.animals.Any(a=>a.loaded&&!a.dead&&new[]{"M09","M10","M11","M12","M13","M14"}.Contains(a.species));
                case "DeliveryHealthMin":return j.animals.Where(a=>a.deliveryTarget).All(a=>a.health>=threshold*100);
                case "AnimalHealthRatio":return j.livestock.Count>0?j.livestock.Average(a=>a.health)/100>=threshold:j.TotalDurability/500f>=threshold;
                default:return false;
            }
        }
        bool WorkUsesCleaning(int i)=>i<2||i==2&&save.job.contractIndex==3;
        public bool CleaningWork(int i)=>WorkUsesCleaning(i);
        void RecordKitUse(Kit k){if(save.job.usedKits==null)save.job.usedKits=new System.Collections.Generic.List<int>();if(!save.job.usedKits.Contains((int)k))save.job.usedKits.Add((int)k);}
        void NormalizeContractHistory(){
            if(save.completedContracts==null)save.completedContracts=new System.Collections.Generic.List<string>();
            if(save.contractStars==null||save.contractStars.Length!=18){var old=save.contractStars;save.contractStars=new int[18];if(old!=null)Array.Copy(old,save.contractStars,Math.Min(18,old.Length));}
            save.contractStars[0]|=save.stars;if(save.successes>0&&!save.completedContracts.Contains("Q01"))save.completedContracts.Add("Q01");
            if(save.capturedRare==null)save.capturedRare=new System.Collections.Generic.List<string>();if(save.rareCaught&&!save.capturedRare.Contains("C01"))save.capturedRare.Add("C01");
        }
        public bool RequiredContractWork(JobRecord j){
            if(j.contractIndex==3)return j.work[0]>=3&&j.work[1]>=3&&j.work[2]>=3;
            if(j.contractIndex==9)return j.work[0]>=3&&j.work[1]>=3&&j.work[2]>=1;
            if(j.contractIndex==12)return j.work[0]>=3&&j.work[1]>=3;
            if(j.contractIndex==13)return j.traceCount>=1;
            return true;
        }
        public bool CanFinishContract(JobRecord j){
            if(j.stage!=2||j.health<=0||!RequiredContractWork(j))return false;
            var c=Contract(j);var required=j.animals.Where(a=>a.required).ToArray();
            if(c.type=="Protect")return j.nightSeconds>=c.durationSeconds&&j.TotalDurability>=300;
            if(c.type=="Timed"&&j.nightSeconds>c.durationSeconds)return false;
            if(required.Length!=c.RequiredCount)return false;
            if(c.type=="Hunt")return required.All(a=>a.dead);
            if(c.type=="Live"){
                int delivered=c.targetBudget.Where(b=>b.role=="Delivery").Sum(b=>b.count);
                return required.All(a=>!a.terminal&&(a.dead||a.contained))&&required.Count(a=>!a.dead&&a.contained&&a.loaded&&a.deliveryTarget)>=delivered;
            }
            return j.Handled==c.RequiredCount;
        }
        void BeginAdditionalNight(){
            var j=save.job;j.prepStar=j.work[3]>0&&j.work[4]>0;j.prepTraceCount=j.traceCount;j.sourcesPrepared=j.work[0]>=3&&j.work[1]>=3;j.stage=2;j.nightSeconds=0;
            Phase=ShiftPhase.Night;ActivateContractBudget();world.ApplyNight(true);Close();Persist();
            Notify(L("夜班开始 · ","Night shift · ")+(save.english?Contract(j).en:Contract(j).zh));
        }
        void ActivateContractBudget(){
            var j=save.job;var c=Contract(j);
            for(int b=0;b<c.targetBudget.Length;b++){
                var budget=c.targetBudget[b];if(budget.activationSeconds>j.nightSeconds)continue;
                for(int n=0;n<budget.count;n++){
                    string id=j.runId+"-budget-"+b+"-"+n;if(j.animals.Any(a=>a.id==id))continue;
                    var p=world.ContractSpawn(budget.speciesId,b*3+n);
                    var a=new AnimalRecord{id=id,species=budget.speciesId,runId=j.runId,required=budget.role=="Required"||budget.role=="Delivery",deliveryTarget=budget.role=="Delivery",position=p};
                    j.animals.Add(a);world.CreateAnimal(a);
                }
            }
        }
        void AdvanceAdditionalContract(float seconds){
            var j=save.job;if(Paused||Phase!=ShiftPhase.Night)return;
            if(j.animals.Any(a=>a.deliveryTarget&&a.dead)){Notify(L("指定活体交付目标死亡，委托失败","Designated live delivery died; contract failed"));FinalizeJob(false,true);return;}
            j.nightSeconds+=seconds;if(Noise>.55f)j.noiseSeconds+=seconds;ActivateContractBudget();AdvanceAdditionalRare();
            var c=Contract(j);if(j.controlTime<0&&j.animals.Count(a=>a.required)==c.RequiredCount&&j.Handled==c.RequiredCount)j.controlTime=j.nightSeconds;
            if(c.type=="Timed"&&j.nightSeconds>c.durationSeconds&&!CanFinishContract(j))FinalizeJob(false,true);
        }
    }
    public partial class DemoWorld {
        public Vector3 ContractSpawn(string species,int ordinal){
            if(ActiveMap==0){var points=new[]{new Vector3(6,.15f,3),new Vector3(-6,3.36f,3.5f),new Vector3(5,3.36f,7),new Vector3(10,.08f,15),new Vector3(-10,.08f,11)};return points[ordinal%points.Length];}
            if(species=="M03"&&ActiveMap==1)return new Vector3(ordinal%2==0?-5:5,3.58f,10+(ordinal%4)*5);
            if(species=="M03"&&ActiveMap==4)return new Vector3(ordinal%2==0?-16:-8,1.68f,11);
            var p=FeedingPoints[ordinal%5]+Vector3.left*2;
            if(Physics.Raycast(p+Vector3.up*2,Vector3.down,out var h,3,1<<9)&&h.normal.y>.95f)p.y=h.point.y+.08f;
            return p;
        }
    }
}
