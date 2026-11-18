using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Riverbend
{
    public partial class DemoGame
    {
        public float Noise {get;private set;}
        float landingNoise,eElapsed,qElapsed,attackCooldown,heldDuration;string gestureOwner="";
        bool eDone,qDone,sprayHeld;DemoTarget eTarget,qTarget;
        class Notice { public string message;public float left; }
        readonly List<Notice> notices=new List<Notice>();
        bool Carrying=>save.job!=null&&!string.IsNullOrEmpty(save.job.carriedAnimal);
        bool SelectedCargo=>Carrying&&selected==save.job.carriedSlot;
        void Update()
        {
            toastTime-=Time.unscaledDeltaTime;foreach(var n in notices)n.left-=Time.unscaledDeltaTime;notices.RemoveAll(n=>n.left<=0);
            var kb=Keyboard.current;var mouse=Mouse.current;if(kb==null||mouse==null)return;
            if(frontend)return;
            if(!Paused){hurtCooldown=Mathf.Max(0,hurtCooldown-Time.deltaTime);skunkEffect=Mathf.Max(0,skunkEffect-Time.deltaTime);}
            if(Phase==ShiftPhase.Result)return;
            if(droneActive){DroneInput(kb,mouse);return;}
            if(save.atDepot){DepotUpdate(kb,mouse);return;}
            if(kb.escapeKey.wasPressedThisFrame){if(panel!="")Close();else if(Phase==ShiftPhase.Day||Phase==ShiftPhase.Night)Open("pause");ResetGestures();return;}
            if(Phase==ShiftPhase.Company||Phase==ShiftPhase.Result)return;
            if(kb.tabKey.wasPressedThisFrame){if(panel=="journal")Close();else Open("journal");ResetGestures();return;}
            if(kb.tKey.wasPressedThisFrame){if(panel=="commands")Close();else Open("commands");return;}
            if(Paused){Noise=0;sprayHeld=false;return;}
            if(kb.f5Key.wasPressedThisFrame){Persist();Notify(L("已保存","Saved"));}
            if(kb.f9Key.wasPressedThisFrame){Reload();ResetGestures();return;}
            if(kb.iKey.wasPressedThisFrame){Open("bag");ResetGestures();return;}
            if(kb.fKey.wasPressedThisFrame){world.torch.enabled=!world.torch.enabled;Sound("ui");}
            senseLeft=Mathf.Max(0,senseLeft-Time.deltaTime);save.job.senseCooldown=Mathf.Max(0,save.job.senseCooldown-Time.deltaTime);
            AdvanceDelivery(Time.deltaTime);AdvanceRare(Time.deltaTime);
            Move(kb,mouse);attackCooldown-=Time.deltaTime;ExtendedEquipmentInput(kb,mouse);
            if(save.atDepot||Phase==ShiftPhase.Result)return;
            if(kb.digit0Key.wasPressedThisFrame)Select(-1);
            for(int i=0;i<9;i++)if(kb[(Key)((int)Key.Digit1+i)].wasPressedThisFrame)Select(i);
            if(!preview&&mouse.scroll.ReadValue().y!=0){int n=save.job.kit.Count+1;Select(((selected+1+(mouse.scroll.ReadValue().y>0?-1:1)+n)%n)-1);}
            if(kb.gKey.wasPressedThisFrame){SaveToolState();DropCargo();ResetGestures();return;}
            if(mouse.rightButton.wasPressedThisFrame&&IsDeployable(CurrentKit)&&!SelectedCargo){preview=!preview;ResetGestures();}
            if(preview){UpdatePreview(mouse);prompt=L("左键布置 · 右键取消 · 滚轮旋转","LMB place · RMB cancel · wheel rotate");if(mouse.leftButton.wasPressedThisFrame)Deploy();return;}
            if(ghost)Destroy(ghost);
            FindTarget();if(IsSupply(CurrentKit)){prompt=SupplyHint();if(mouse.rightButton.wasPressedThisFrame){ResetGestures();UseSupply();return;}}HandleGestures(kb);
            sprayHeld=CurrentKit==Kit.Spray&&!SelectedCargo&&mouse.leftButton.isPressed;
            if(sprayHeld)Spray();
            if(!LargeCargo&&mouse.leftButton.wasPressedThisFrame&&attackCooldown<=0&&(!CurrentKit.HasValue||CurrentKit==Kit.Baton))Melee();
            world.handRoot.localRotation=Quaternion.Slerp(world.handRoot.localRotation,Quaternion.identity,Time.deltaTime*9);
        }
        void ResetGestures(){eElapsed=qElapsed=actionProgress=0;eTarget=qTarget=working=null;eDone=qDone=false;heldDuration=0;gestureOwner="";}
        void AdvanceDelivery(float seconds)
        {
            if(Paused||save.job==null||string.IsNullOrEmpty(save.job.orderItem))return;
            save.job.orderRemaining-=seconds;if(save.job.orderRemaining<=0)DeliverOrder();
        }
        void HandleGestures(Keyboard kb)
        {
            GestureInput(kb.eKey.wasPressedThisFrame,kb.eKey.isPressed,kb.eKey.wasReleasedThisFrame,kb.qKey.wasPressedThisFrame,kb.qKey.isPressed,kb.qKey.wasReleasedThisFrame,Time.deltaTime);
        }
        void GestureInput(bool ePressed,bool eHeld,bool eReleased,bool qPressed,bool qHeld,bool qReleased,float dt)
        {
            // Freeze a single action duration; both the ring and commit read it.
            if(ePressed){ResetGestures();gestureOwner="E";eTarget=target;}
            else if(qPressed){ResetGestures();gestureOwner="Q";qTarget=target;}
            if(gestureOwner=="E"&&eHeld){
                eElapsed+=dt;
                if(eTarget!=target||!target){eDone=true;actionProgress=0;}
                if(!eDone&&eElapsed>=.35f){
                    if(heldDuration<=0){string reject;DescribeAction(target,out heldDuration,out reject);if(reject!=""||heldDuration<=0)eDone=true;}
                    if(!eDone){working=target;actionProgress=Mathf.Min(heldDuration,eElapsed-.35f);if(actionProgress>=heldDuration){HoldAction(target);eDone=true;actionProgress=0;}}
                }
            }
            if(gestureOwner=="E"&&eReleased){if(!eDone&&eElapsed<.35f&&target==eTarget&&target){if(TimedTransfer(target))Notify(L("长按E完成拾取或转移","Hold E to complete collection or transfer"));else TapAction(target);}ResetGestures();}
            if(gestureOwner=="Q"&&qHeld){qElapsed+=dt;if(target!=qTarget){qDone=true;actionProgress=0;}if(!qDone&&qElapsed>=.35f){if(!CanInvestigate(target)){qDone=true;actionProgress=0;}else{heldDuration=2*SkillValue("identifyInvestigateMultiplier",1);working=target;actionProgress=Mathf.Min(heldDuration,qElapsed-.35f);if(actionProgress>=heldDuration){InvestigateTarget(target);qDone=true;actionProgress=0;}}}}
            if(gestureOwner=="Q"&&qReleased){if(!qDone&&qElapsed<.35f&&save.job.senseCooldown<=0){senseLeft=SkillValue("qMarkSeconds",config.sense.markSeconds);save.job.senseCooldown=SkillValue("qCooldown",config.sense.cooldownSeconds);IdentifyScannedTargets();Sound("ui");}ResetGestures();}
        }

        bool CanInvestigate(DemoTarget t)
        {
            if(!t||save.job==null)return false;
            if(t.kind=="work"&&SpecializedSource(t.index)&&!SourceInspected(t.index))return true;
            if(t.kind=="trace")return !save.job.traceMarks.Contains(t.index);
            string species=null;
            if(t.kind=="animal"){var a=t.GetComponent<DemoAnimal>().record;if(RequiresLiveInspection(a.species)?a.contained&&!a.dead:a.dead)species=a.species;}
            if(t.kind=="device"){var d=t.GetComponent<DemoDevice>().record;var a=save.job.animals.Find(a=>a.id==d.animalId);if(a!=null&&(RequiresLiveInspection(a.species)?a.contained&&!a.dead:a.dead))species=a.species;}
            if(t.kind=="pest"){var p=t.GetComponent<DemoPest>();if(p.record.dead)species=p.record.species;}
            if(t.kind=="cargo")species=save.job.animals.FirstOrDefault(a=>a.loaded&&(RequiresLiveInspection(a.species)?!a.dead:a.dead)&&!InspectionDone(a.species))?.species;
            if(t.kind=="work")species=save.job.pests.FirstOrDefault(p=>p.source==t.index&&p.dead&&!InspectionDone(p.species))?.species;
            return species!=null&&!InspectionDone(species);
        }
        void InvestigateTarget(DemoTarget t)
        {
            if(!CanInvestigate(t))return;
            if(t.kind=="work"&&SpecializedSource(t.index)&&!SourceInspected(t.index)){save.job.sourceInspected.Add(t.index);Persist();Notify(L("已标记受害源范围，可处理材料","Affected source marked; material treatment is available"));return;}
            if(t.kind=="trace"){InspectTrace(t);return;}
            if(t.kind=="animal")Investigate(t.GetComponent<DemoAnimal>().record);
            else if(t.kind=="device")Investigate(save.job.animals.Find(a=>a.id==t.GetComponent<DemoDevice>().record.animalId));
            else if(t.kind=="cargo")Investigate(save.job.animals.First(a=>a.loaded&&(RequiresLiveInspection(a.species)?!a.dead:a.dead)&&!InspectionDone(a.species)));
            else Unlock(t.kind=="pest"?t.GetComponent<DemoPest>().record.species:save.job.pests.First(p=>p.source==t.index&&p.dead&&!InspectionDone(p.species)).species);
            Persist();
        }
        string DescribeAction(DemoTarget t,out float duration,out string reject)
        {
            duration=0;reject="";var j=save.job;string hint="";
            if(TimedTransfer(t)){duration=TransferDuration(t);return (t.kind=="item"?KitName((Kit)t.GetComponent<DemoLooseItem>().record.kit):t.kind=="animal"?SpeciesName(t.GetComponent<DemoAnimal>().record.species):L("装载笼","Loading cage"))+L(" · 长按E拾取 / 转移"," · hold E to collect / transfer")+(CanInvestigate(t)?L("\n长按Q调查","\nHold Q investigate"):"");}
            if(t.kind=="trace"){duration=2;return L("活动痕迹 · 长按E或Q确认路线","Activity trace · hold E or Q to inspect");}
            if(t.kind=="stolen")return L("被叼走的物品 · E 追回","Stolen item · E recover");
            if(t.kind=="partner")return L("作业伙伴 · E 接管已控制动物（需空笼）","Companion · E receive controlled animal (empty cage required)");
            if(t.kind=="board")return L("委托黑板 · E 选择地图与委托","Contract board · E choose map and job");
            if(t.kind=="shop")return L("器械柜台 · E 选购商品与数量","Equipment counter · E shop");
            if(t.kind=="sale")return L("收购大笼 · E 放入手中动物 / 出售","Buyer cage · E deposit animal / sell");
            if(t.kind=="donate")return L("捐赠大笼 · E 放入手中活体 / 捐赠经验","Donation cage · E deposit live animal / donate for XP");
            if(t.kind=="depotAnimal")return L("动物货物 · E 用笼携带活体 / 拿取尸体","Animal cargo · E carry in cage / collect remains");
            if(t.kind=="van")return L("操作台 · E 打开","Workbench · E open");
            if(t.kind=="cargo")return L("装载笼 · E 装车 / 查看","Loading cage · E load / view")+(CanInvestigate(t)?L("\n长按 Q 调查","\nHold Q investigate"):"");
            if(t.kind=="delivery")return L("配送箱 · E 取货","Delivery · E collect");
            if(t.kind=="item")return KitName((Kit)t.GetComponent<DemoLooseItem>().record.kit)+L(" · E 拾取"," · E collect");
            if(t.kind=="furniture")return L("财产 ","Property ")+j.durability[t.index]+"%";
            if(t.kind=="work") {
                if(SpecializedSource(t.index))return SourceDescription(t.index,out duration,out reject)+(CanInvestigate(t)?L("\n长按Q调查","\nHold Q inspect"):"");
                int i=t.index;if(WorkUsesCleaning(i)){bool killed=j.pests.Where(p=>p.source==i).All(p=>p.dead);
                    if(j.work[i]>=3)hint=WorkName(i)+L(" · 已处理"," · treated");
                    else if(!killed)hint=WorkName(i)+L(" · 左键喷洒害虫"," · spray pests with LMB");
                    else {duration=3;if(CurrentKit!=Kit.Broom)reject=L("需要清洁工具","Equip cleaning kit");else if(j.cleaning<10)reject=L("清理工具耐久耗尽，拾取清洁补充剂后右键补充","Cleaning tool depleted; collect a cleaning refill and RMB refill");hint=WorkName(i)+(i==0?L(" · 长按 E 清垃圾"," · hold E clean trash"):L(" · 长按 E 排水"," · hold E drain"));}
                }else if(j.work[i]>0)hint=WorkName(i)+L(" · 已修复"," · repaired");
                else {duration=3;if(CurrentKit!=Kit.Repair)reject=L("需要修缮工具","Equip repair kit");else if(j.repairs<10)reject=L("修缮材料不足","No repair material");hint=WorkName(i)+L(" · 长按 E 修复"," · hold E repair");}
            }else if(t.kind=="animal") {
                var a=t.GetComponent<DemoAnimal>().record;hint=SpeciesName(a.species)+(a.dead?L(" · E 拾取"," · E collect"):a.contained?L(" · E 拾取笼具"," · E collect cage"):L(" · 活动中"," · roaming"));
                if(!a.dead&&!a.contained&&(CurrentKit==Kit.ControlPole||CurrentKit==Kit.Net)){duration=2*SkillValue("netRodDurationMultiplier",1);hint+=L(" · 长按E约束并转移至空笼"," · hold E to restrain and transfer to an empty cage");if(!ToolCanControl(t.GetComponent<DemoAnimal>(),CurrentKit.Value))reject=L("目标不在此工具可控窗口","Target is outside this tool's control window");}
            }else if(t.kind=="device") {
                var d=t.GetComponent<DemoDevice>().record;
                if(CurrentKit==Kit.Bait&&!d.triggered&&!d.baited)return L("右键 添加诱饵 · E 拾取装置","RMB add bait · E collect device");
                duration=1.2f;hint=d.cage?L("捕捉笼 · E 拾取 / 长按 E 开笼","Cage · E collect / hold E open"):L("捕鼠夹 · E 拾取 / 长按 E 复位","Trap · E collect / hold E reset");
            }else if(t.kind=="pest"){var p=t.GetComponent<DemoPest>();hint=SpeciesName(p.record.species);if(p.record.dead&&!p.record.cleaned){duration=1.2f;if(CurrentKit!=Kit.Broom)reject=L("需要清洁工具","Equip cleaning kit");else if(j.cleaning<10)reject=L("清理工具耐久耗尽，拾取清洁补充剂后右键补充","Cleaning tool depleted; collect a cleaning refill and RMB refill");hint+=L(" · 长按 E 清理虫尸"," · hold E clean remains");}}
            if(CanInvestigate(t))hint+=L("\n长按 Q 调查","\nHold Q investigate");
            if(duration>0&&t.kind=="work")duration*=SkillValue("repairCleanMultiplier",1);if(duration>0&&t.kind=="device")duration*=SkillValue("deployResetMultiplier",1);
            if(reject!="")duration=0;
            return hint;
        }
        void TapAction(DemoTarget t)
        {
            var j=save.job;
            if(t.kind=="stolen"){RecoverStolen(t.GetComponent<DemoStolenObject>());return;}if(t.kind=="partner"){TransferPartner(t.GetComponent<DemoPartner>());return;}if(t.kind=="van"){Open("van");return;}
            if(t.kind=="cargo"){if(Carrying)LoadCargo();else Open("cargo");return;}
            if(t.kind=="delivery"){TakeDelivery(t);return;}
            if(t.kind=="item") {
                var r=t.GetComponent<DemoLooseItem>().record;if(!HasBackpackSpace((Kit)r.kit)){Notify(L("背包已满","Backpack full"));return;}if(!j.looseItems.Remove(r))return;j.kit.Add(r.kit);j.quantities.Add(r.quantity);j.itemIds.Add(r.id);j.conditions.Add(r.condition);j.cargoIds.Add(null);j.baitedSlots.Add(r.baited);Destroy(t.gameObject);Select(j.kit.Count-1);Persist();return;
            }

            if(t.kind=="device"){CollectDevice(t.GetComponent<DemoDevice>().record);return;}
            if(t.kind=="animal") {
                var a=t.GetComponent<DemoAnimal>().record;
                if(a.loaded||a.terminal||a.id==j.carriedAnimal)return;
                if(a.contained){CollectDevice(j.devices.Find(d=>d.id==a.deviceId));return;}
                if(!a.dead)return;
                if(save.job.contractIndex>0&&!BagCorpse(a))return;
                if(!HasBackpackSpace(Kit.Corpse)){Notify(L("背包空间不足","Not enough backpack space"));return;}
                j.carriedSlot=j.kit.Count;j.kit.Add((int)Kit.Corpse);j.quantities.Add(1);j.itemIds.Add(a.id+"-corpse");j.conditions.Add(-1);NormalizeSlots();j.cargoIds[j.carriedSlot]=a.id;j.carriedAnimal=a.id;world.animals.Remove(a.id);Destroy(t.gameObject);Select(j.carriedSlot);Persist();
            }
        }
        void HoldAction(DemoTarget t)
        {
            if(TimedTransfer(t)){TapAction(t);return;}
            if(t.kind=="work"&&SpecializedSource(t.index)){CompleteSpecializedSource(t.index);return;}
            var j=save.job;float seconds;string reject;DescribeAction(t,out seconds,out reject);if(reject!="")return;
            if(t.kind=="trace"){InspectTrace(t);return;}
            if(t.kind=="animal"&&(CurrentKit==Kit.ControlPole||CurrentKit==Kit.Net)){TransferWithTool(t.GetComponent<DemoAnimal>());return;}
            if(t.kind=="pest"){var p=t.GetComponent<DemoPest>();if(p.record.dead&&!p.record.cleaned&&CurrentKit==Kit.Broom&&j.cleaning>=10){j.cleaning-=10;CleanArea(p.transform.position);SaveToolState();Persist();}return;}
            if(t.kind=="work") {
                int i=t.index;if(WorkUsesCleaning(i)){if(j.work[i]>=3||j.cleaning<10||!j.pests.Where(p=>p.source==i).All(p=>p.dead)||CurrentKit!=Kit.Broom)return;j.cleaning-=10;j.work[i]=3;CleanArea(t.transform.position);SaveToolState();}
                else {if(j.work[i]>0||CurrentKit!=Kit.Repair||j.repairs<10)return;j.repairs-=10;j.work[i]=1;SaveToolState();}
                t.Refresh();world.BuildNavigation();Notify(L("处理完成","Source treated"));Sound("hit");Persist();return;
            }
            if(t.kind=="device") {
                var d=t.GetComponent<DemoDevice>().record;var a=j.animals.Find(x=>x.id==d.animalId);
                if(d.cage&&a!=null&&a.contained) {
                    Vector3 release=t.transform.TransformPoint(new Vector3(0,.09f,.8f));
                    if(Physics.CheckSphere(release+Vector3.up*.12f,.13f,1<<9)){Notify(L("笼口被挡住","Cage opening is blocked"));return;}
                    a.contained=false;a.deviceId=null;a.position=release;
                    if(world.animals.TryGetValue(a.id,out var actor)){actor.transform.position=release;}
                    d.animalId=null;d.triggered=false;d.baited=false;
                }else {d.animalId=null;d.triggered=false;d.baited=false;}
                Destroy(t.gameObject);world.devices.Remove(d.id);world.CreateDevice(d);Notify(L("装置已打开 / 复位","Device opened / reset"));Persist();
            }
        }
        // Kept as the common commit entry for scenario tests and work interactions.
        void CommitAction(DemoTarget t){if(t.kind=="work")HoldAction(t);else TapAction(t);}
        void Investigate(AnimalRecord a)
        {
            if(a==null||(RequiresLiveInspection(a.species)?!a.contained||a.dead:!a.dead)||InspectionDone(a.species))return;
            a.investigated=true;Unlock(a.species);foreach(var other in save.job.animals.Where(x=>x.species==a.species))other.investigated=true;Persist();
        }
        void CollectDevice(DeviceRecord d)
        {
            var j=save.job;if(d==null||!j.devices.Contains(d))return;
            var a=j.animals.Find(a=>a.id==d.animalId);bool occupied=d.cage&&a!=null&&a.contained;
            if(occupied&&DeviceKit(d)==Kit.Cage?BackpackUsed()+1>BackpackCapacity:!HasBackpackSpace(DeviceKit(d))){Notify(L("背包空间不足","Not enough backpack space"));return;}
            int slot=j.kit.Count;j.kit.Add((int)DeviceKit(d));j.quantities.Add(1);j.itemIds.Add(string.IsNullOrEmpty(d.itemId)?d.id:d.itemId);j.conditions.Add(d.kit==(int)Kit.Camera?d.energy/3:100);NormalizeSlots();j.baitedSlots[slot]=d.baited;
            if(occupied){j.carriedSlot=slot;j.carriedAnimal=a.id;j.cargoIds[slot]=a.id;a.deviceId=null;if(world.animals.TryGetValue(a.id,out var actor)){Destroy(actor.gameObject);world.animals.Remove(a.id);}}
            j.devices.Remove(d);if(world.devices.TryGetValue(d.id,out var device))Destroy(device.gameObject);world.devices.Remove(d.id);Select(slot);Persist();
        }
        void RemoveSlot(int index)
        {
            var j=save.job;if(index<0||index>=j.kit.Count)return;j.kit.RemoveAt(index);if(index<j.itemIds.Count)j.itemIds.RemoveAt(index);if(index<j.conditions.Count)j.conditions.RemoveAt(index);if(index<j.quantities.Count)j.quantities.RemoveAt(index);if(index<j.cargoIds.Count)j.cargoIds.RemoveAt(index);if(index<j.baitedSlots.Count)j.baitedSlots.RemoveAt(index);if(j.carriedSlot>index)j.carriedSlot--;else if(j.carriedSlot==index)j.carriedSlot=-1;selected=-1;SyncSelectedCargo();world.Equip(null);
        }
        bool DropPosition(out Vector3 p)
        {
            p=world.controller.transform.position+world.controller.transform.forward*.9f;
            if(!world.ClearLine(world.controller.transform.position+Vector3.up*.4f,p+Vector3.up*.4f))return false;
            if(!Physics.Raycast(p+Vector3.up*.7f,Vector3.down,out var hit,2,1<<9)||hit.normal.y<.96f)return false;
            p=hit.point+Vector3.up*.02f;
            if(save.job.contractIndex>0&&SelectedCargo&&IsCage(CurrentKit)){var size=DeviceSize(CurrentKit);if(Physics.CheckBox(p+Vector3.up*(size.y*.5f+.04f),size*.48f,world.controller.transform.rotation,1<<9))return false;}
            return !Physics.CheckBox(p+Vector3.up*.24f,new Vector3(.25f,.18f,.37f),world.controller.transform.rotation,1<<9);
        }
        void DropCargo()
        {
            SaveToolState();
            var j=save.job;if(!CurrentKit.HasValue)return;
            if(!DropPosition(out var p)){Notify(L("前方空间不足","Not enough space ahead"));return;}
            if(SelectedCargo) {
                var a=j.animals.Find(a=>a.id==j.carriedAnimal);a.position=p;
                if(a.contained&&!a.dead&&(j.contractIndex==0?j.devices.Any(d=>d.cage):j.devices.Count>=(int)SkillValue("deploymentTotal",8))){Notify(L("地图已有一个笼，请先收起再放下","Collect the existing cage before placing another"));return;}
                if(a.contained&&!a.dead){var d=new DeviceRecord{id=Guid.NewGuid().ToString("N"),itemId=j.itemIds[selected],kit=(int)CurrentKit.Value,cage=true,triggered=true,animalId=a.id,position=p,yaw=world.controller.transform.eulerAngles.y};a.deviceId=d.id;j.devices.Add(d);world.CreateDevice(d);}
                world.CreateAnimal(a);int slot=j.carriedSlot;j.carriedAnimal=null;RemoveSlot(slot);Notify(a.dead?L("尸体已放下","Remains dropped"):L("已放下 · 笼门保持关闭","Dropped · cage stays closed"));
            } else {
                var item=new LooseItemRecord{id=j.itemIds[selected],condition=j.conditions[selected],kit=(int)CurrentKit.Value,quantity=selected<j.quantities.Count?j.quantities[selected]:1,baited=selected<j.baitedSlots.Count&&j.baitedSlots[selected],position=p};j.looseItems.Add(item);world.CreateLooseItem(item);RemoveSlot(selected);Notify(L("物品已放下","Item dropped"));
            }
            Persist();
        }
        void LoadCargo()
        {
            var j=save.job;if(!Carrying||!SelectedCargo||Vector3.Distance(world.controller.transform.position,DemoWorld.VanPoint+new Vector3(-1.1f,1.2f,2.2f))>3)return;var a=j.animals.Find(a=>a.id==j.carriedAnimal);
            bool large=config.content.species.First(s=>s.id==a.species).size=="Large";if(a.dead&&large&&j.animals.Count(other=>other.loaded&&config.content.species.First(s=>s.id==other.species).size=="Large")>=(int)ServiceValue("largeLiveSlots",1)){Notify(L("大型货位已满，请先交接或卸下现有货物","Large cargo bay is full; transfer or unload existing cargo"));return;}
            if(!a.dead&&j.animals.Count(other=>other.loaded&&(large||!other.dead)&&(config.content.species.First(s=>s.id==other.species).size=="Large")==large)>=(config.content.species.First(s=>s.id==a.species).size=="Large"?(int)ServiceValue("largeLiveSlots",1):(int)ServiceValue("ordinaryLiveSlots",config.vehicle.ordinaryLiveSlots))){Notify(L("固定笼已满，可保留手持笼随车带回","Fixed cage full; this portable cage can return with you"));return;}
            a.loaded=true;int slot=j.carriedSlot;NormalizeSlots();j.cargoIds[slot]=null;j.carriedAnimal=null;j.carriedSlot=-1;
            if(a.dead)RemoveSlot(slot);else {a.contained=true;j.baitedSlots[slot]=false;}selected=-1;world.Equip(null);world.RefreshTruckCargo();Persist();Notify(L("已装车","Loaded"));
        }
        void Melee()
        {
            attackCooldown=.5f;world.handRoot.localRotation=Quaternion.Euler(-20,0,0);
            var hits=Physics.SphereCastAll(world.cam.transform.position,.17f,world.cam.transform.forward,CurrentKit==Kit.Baton?2.1f:1.65f,~(1<<8),QueryTriggerInteraction.Collide).OrderBy(h=>h.distance);
            foreach(var hit in hits){var livestock=hit.collider.GetComponentInParent<DemoLivestock>();if(livestock){livestock.Hurt(CurrentKit==Kit.Baton?35:15);return;}if(hit.collider.gameObject.layer==9)break;var a=hit.collider.GetComponentInParent<DemoAnimal>();if(a&&!a.record.contained&&!a.record.loaded){save.job.handDamageEvents++;if(CurrentKit.HasValue)RecordKitUse(CurrentKit.Value);a.record.controlMethod="Melee";if(save.job.contractIndex==0)a.Kill();else DamageAdditionalAnimal(a,CurrentKit==Kit.Baton?35:15);return;}}
        }
        public bool LureAllowed(DemoDevice d)
        {
            var p=world.controller.transform.position;var mouth=d.Mouth;
            if(Vector3.Distance(p,mouth)<5)return false;
            var view=world.cam.WorldToViewportPoint(mouth);
            bool seen=view.z>0&&view.x>=0&&view.x<=1&&view.y>=0&&view.y<=1&&world.ClearLine(world.cam.transform.position,mouth+Vector3.up*.1f);
            return !seen;
        }
        void LoadoutUI(float x,float y)
        {
            if(save.job.contractIndex>0){Label(x,y,460,190,L("E取物 · G放下 · 数字键/滚轮选择。R装填弹药；右键补给或预览部署。工具与货物共用容量；部署上限8件，可升级。K公司成长整备；V切换已升级镜具模式。","E collect · G drop · digits/wheel select. R reload; RMB supply / placement. Tools and cargo share capacity. Eight base deployments; skills expand it. K growth at depot; V cycles upgraded optics."));return;}
            Label(x,y,460,190,L("物品实际放在车厢平台上。\nE 拿取 · G 放下 · 1–4 选择。\n手持补给按右键，补充背包中的工具。\n手持诱饵对准已放置的笼或夹按右键。\n每局仅一个笼、一个夹。","Collect equipment from the truck platforms.\nE collect · G drop · 1–4 select.\nHold a refill and RMB to refill a backpack tool.\nHold bait, aim at a placed device and RMB.\nOne cage and one trap per job."));
        }

        void CargoPanel()
        {
            var j=save.job;Label(335,200,880,40,L("装载笼","LOADING CAGE"),heading);
            int row=0;
            foreach(var a in j.animals.Where(a=>a.loaded&&!a.terminal).ToArray()) {
                Label(335,265+row*70,480,50,SpeciesName(a.species)+(a.dead?L(" · 尸体"," · remains"):L(" · 活体"," · alive")));
                if(!a.dead&&Button(900,265+row*70,300,46,L("释放","Release"))) {
                    a.loaded=false;a.contained=false;a.deviceId=null;a.position=DemoWorld.VanPoint+new Vector3(2.8f,.1f,4.6f);world.CreateAnimal(a);world.RefreshTruckCargo();Persist();Notify(L("已释放","Released"));return;
                }row++;
            }
            if(row==0)Label(335,270,880,50,L("暂无动物","No animals loaded"));
        }
    }
}

