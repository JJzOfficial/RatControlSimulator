using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Riverbend
{
    public partial class DemoGame : MonoBehaviour
    {
        public static DemoGame main;
        public DemoConfig config;
        public SaveRecord save;
        public DemoWorld world;
        public ShiftPhase Phase {get;private set;}
        bool reviewFrozen;
        public bool Paused => reviewFrozen || frontend || Phase==ShiftPhase.Result || panel!="";
        public bool IsMoving {get;private set;}
        public int Handled => save.job==null?0:save.job.Handled;
        public int CargoCount => save.cargo.Count;
        public int Cash => save.cash;
        public string PhaseName => Phase.ToString();
        public float sensitivity;
        string panel="",toast="",result="",prompt="";
        float toastTime,pitch,vertical,stamina=100,senseLeft,actionProgress,deployYaw,footTime;
        int selected,tab;
        DemoTarget target,working;
        bool preview,canPlace;
        Vector3 place;
        GameObject ghost;
        AudioSource audioSource;
        AudioClip stepSound,hitSound,uiSound;
        int stepEvents;
        Font font;
        GUIStyle text,small,title,heading,button;
        Texture2D white;
        bool stylesReady;
        readonly Color ink=new Color(.86f,.89f,.84f),muted=new Color(.53f,.62f,.61f),gold=new Color(.96f,.68f,.3f),green=new Color(.29f,.8f,.61f);
        public string L(string zh,string en)=>save!=null&&save.english?en:zh;
        Kit? CurrentKit => save.job!=null&&selected>=0&&selected<save.job.kit.Count?(Kit?)save.job.kit[selected]:null;
        void Awake()
        {
            main=this;Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            config=DemoConfig.Load();InitializeEquipmentCatalog();
            #if DEVELOPMENT_BUILD && !UNITY_EDITOR
            bool verify=Array.IndexOf(Environment.GetCommandLineArgs(),"-riverbend-verify")>=0;
            if(verify)DemoSave.testPath=System.IO.Path.GetFullPath(Application.dataPath+"/../Verification/launch-profile.json");
            #endif
            save=DemoSave.Load(config.profile.startingCash);sensitivity=save.sensitivity;LoadFrontendSettings();NormalizeContractHistory();
            world=gameObject.AddComponent<DemoWorld>();world.Create(this);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;
            stepSound=Resources.Load<AudioClip>("step");hitSound=Resources.Load<AudioClip>("hit");uiSound=Resources.Load<AudioClip>("ui");
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},20);
            if(save.job!=null&&!save.atDepot) {Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;world.SetDepot(false);world.RestoreJob();selected=save.job.carriedSlot>=0?save.job.carriedSlot:0;SyncSelectedCargo();world.Equip(CurrentKit);}
            else ShowCompany();
            SyncPause();
            #if DEVELOPMENT_BUILD && !UNITY_EDITOR
            if(verify)StartCoroutine(VerifyStandalone());
            #endif
        }
        public void Notify(string message){toast=message;toastTime=4;notices.Add(new Notice{message=message,left=4});if(notices.Count>3)notices.RemoveAt(0);}
        public void Sound(string kind){if(kind=="step")stepEvents++;var clip=kind=="step"?stepSound:kind=="hit"?hitSound:uiSound;if(clip)audioSource.PlayOneShot(clip,save.volume*(kind=="step"?.3f:.75f));}
        void SyncPause()
        {
            Time.timeScale=Paused?0:1;Cursor.lockState=Paused?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=Paused;
            ResetGestures();
            if(Paused) {preview=false;if(ghost)Destroy(ghost);}
        }
        void Open(string value){panel=value;tab=0;SyncPause();Sound("ui");}
        void Close(){if(panel=="board")world.EndBoardView();panel="";SyncPause();}
        void ShowCompany()
        {
            StopDrone();
            Phase=ShiftPhase.Company;panel="";save.atDepot=true;
            EnsureDepot();world.SetDepot(true);world.RestoreDepot();selected=-1;world.Equip(null);SyncPause();
        }
        public void StartContract()
        {
            if(!save.atDepot||!ContractAvailable(save.selectedMap*3+save.selectedContract))return;
            if(save.job!=null&&save.job.cargoIds.Any(id=>!string.IsNullOrEmpty(id))){Notify(L("出发前请将手中动物放回车辆或交接笼","Return carried animals to a vehicle or transfer cage before departing"));return;}
            world.EndBoardView();
            EnsureDepot();var previous=save.job;SaveToolState();
            var j=NewShift(previous);save.runs++;save.atDepot=false;save.job=j;
            j.mapIndex=save.selectedMap;j.contractIndex=save.selectedMap*3+save.selectedContract;
            j.runId=Guid.NewGuid().ToString("N");InitializeLayout(j);PrepareContractLoans(j);foreach(var p in save.partners){p.target=null;p.position=Vector3.zero;p.command="Work";p.rest=0;p.chase=0;p.windup=0;}
            Phase=ShiftPhase.Day;panel="";pitch=0;world.SetDepot(false);
            world.cam.transform.localRotation=Quaternion.identity;world.cam.transform.localPosition=Vector3.up*1.6f;
            world.RestoreJob();PrepareAdditionalRare();selected=-1;world.Equip(null);SyncPause();Persist();
            Notify(L("已到达 · Tab 查看任务和操作","Arrived · Tab for objectives and controls"));
        }

        public void BeginNight()
        {
            if(Phase!=ShiftPhase.Day || save.job.stage==2)return;
            if(save.job.contractIndex>0){BeginAdditionalNight();return;}
            var j=save.job;j.prepStar=j.work[3]>0&&j.work[4]>0;j.stage=2;
            Vector3[] spawn={new Vector3(6,.15f,3),new Vector3(-6,3.36f,3.5f),new Vector3(5,3.36f,7),new Vector3(10,.08f,15),new Vector3(-10,.08f,11)};
            for(int i=0;i<config.contracts[0].requiredCount;i++) {
                var a=new AnimalRecord{id=j.runId+"-M01-"+i,species="M01",runId=j.runId,required=true,position=spawn[i]};j.animals.Add(a);world.CreateAnimal(a);
            }
            if(save.runs>=2){var a=new AnimalRecord{id=j.runId+"-M02",species="M02",runId=j.runId,position=new Vector3(-9,.08f,13)};j.animals.Add(a);world.CreateAnimal(a);}
            Phase=ShiftPhase.Night;world.ApplyNight(true);Close();Persist();
            Notify(L("夜班开始：五只任务小家鼠。Q扫描；用诱饵与陷阱处理。","Night shift: five task house mice. Use Q to scan, and baited traps to control them."));
        }

        void Move(Keyboard kb,Mouse mouse)
        {
            var look=mouse.delta.ReadValue()*sensitivity;pitch=Mathf.Clamp(pitch-look.y,-80,80);
            world.controller.transform.Rotate(0,look.x,0);world.cam.transform.localRotation=Quaternion.Euler(pitch,0,0);
            Vector2 axis=new Vector2((kb.dKey.isPressed?1:0)-(kb.aKey.isPressed?1:0),(kb.wKey.isPressed?1:0)-(kb.sKey.isPressed?1:0));axis=Vector2.ClampMagnitude(axis,1);
            IsMoving=axis.sqrMagnitude>.01f;bool crouch=kb.leftCtrlKey.isPressed;
            bool run=kb.leftShiftKey.isPressed&&IsMoving&&!crouch&&stamina>1&&!aiming&&!LargeCargo;
            stamina=Mathf.Clamp(stamina+(run?-config.player.sprintDrainPerSecond:config.player.staminaRecoveryPerSecond*SkillValue("staminaRegenMultiplier",1))*Time.deltaTime,0,100*SkillValue("staminaMultiplier",1));
            float h=crouch?1.1f:1.75f;
            if(h<world.controller.height || !Physics.CheckSphere(world.controller.transform.position+Vector3.up*1.5f,.27f,1<<9)) {world.controller.height=h;world.controller.center=Vector3.up*h*.5f;}
            world.cam.transform.localPosition=Vector3.Lerp(world.cam.transform.localPosition,Vector3.up*(world.controller.height-.15f),Time.deltaTime*12);
            var move=world.controller.transform.TransformDirection(new Vector3(axis.x,0,axis.y))*(LargeCargo?SkillValue("largeCarrySpeed",1.5f):run?config.player.sprintSpeed:crouch?config.player.crouchSpeed:config.player.walkSpeed);
            bool groundedBefore=world.controller.isGrounded;float fallSpeed=vertical;
            if(world.controller.isGrounded) {vertical=-2;if(kb.spaceKey.wasPressedThisFrame)vertical=Mathf.Sqrt(config.player.jumpHeight*-2*config.player.gravity);}
            var beforeMove=world.controller.transform.position;vertical+=config.player.gravity*Time.deltaTime;move.y=vertical;world.controller.Move(move*Time.deltaTime);
            var actualMove=world.controller.transform.position-beforeMove;actualMove.y=0;IsMoving=actualMove.sqrMagnitude>.000001f;
            if(!groundedBefore&&world.controller.isGrounded&&fallSpeed< -3)landingNoise=1;
            landingNoise=Mathf.MoveTowards(landingNoise,0,Time.deltaTime*1.4f);
            weaponNoise=Mathf.MoveTowards(weaponNoise,0,Time.deltaTime*4);Noise=Mathf.Max(weaponNoise,Mathf.Max(landingNoise,IsMoving&&!crouch&&world.controller.isGrounded?(run?.8f:.28f):0));
            var p=world.controller.transform.position;var bounds=save.atDepot?new Rect(-25.8f,-17.8f,51.6f,43.6f):world.PlayBounds;p.x=Mathf.Clamp(p.x,bounds.xMin,bounds.xMax);p.z=Mathf.Clamp(p.z,bounds.yMin,bounds.yMax);
            if(p!=world.controller.transform.position){world.controller.enabled=false;world.controller.transform.position=p;world.controller.enabled=true;}
            if(IsMoving&&!crouch&&world.controller.isGrounded){footTime+=Time.deltaTime;if(footTime>(run?.32f:.5f)){Sound("step");footTime=0;}}else footTime=0;
            world.handRoot.localPosition=Vector3.up*(IsMoving?Mathf.Sin(Time.time*9)*.012f:0);
        }
        void Select(int index)
        {
            if(index< -1||index>=save.job.kit.Count)return;if(LargeCargo&&save.job.carriedSlot>=0&&index!=save.job.carriedSlot){Notify(L("先放下双手搬运的货物","Set down the large cargo first"));return;}SaveToolState();selected=index;LoadToolState(index);SyncSelectedCargo();preview=false;ResetGestures();world.Equip(CurrentKit);Sound("ui");
        }
        void FindTarget()
        {
            target=null;prompt="";
            var ray=new Ray(world.cam.transform.position,world.cam.transform.forward);
            var hits=Physics.SphereCastAll(ray,.045f,2.5f,~0,QueryTriggerInteraction.Collide);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits) {
                var t=hit.collider.GetComponentInParent<DemoTarget>();
                if(t){if(t.kind=="animal"){var animal=t.GetComponent<DemoAnimal>();if(animal&&animal.record.contained&&!string.IsNullOrEmpty(animal.record.deviceId)&&world.devices.TryGetValue(animal.record.deviceId,out var cage)){t=cage.GetComponent<DemoTarget>();}}target=t;break;}if(hit.collider.gameObject.layer==9)break;
            }
            if(target){string reject;float time;prompt=DescribeAction(target,out time,out reject);if(reject!="")prompt+="\n"+reject;}
        }



        void Unlock(string species){if(save.job!=null&&!save.job.investigatedSpecies.Contains(species))save.job.investigatedSpecies.Add(species);if(!save.encyclopedia.Contains(species)){save.encyclopedia.Add(species);Notify(L("图鉴新增：","Field guide unlocked: ")+SpeciesName(species));}}

        void UpdatePreview(Mouse mouse)
        {
            if(mouse.scroll.ReadValue().y!=0)deployYaw+=mouse.scroll.ReadValue().y>0?15:-15;
            canPlace=false;
            if(Physics.Raycast(world.cam.transform.position,world.cam.transform.forward,out var hit,3.5f,1<<9)) {
                place=hit.point;canPlace=hit.normal.y>.96f&&Vector3.Distance(place,world.controller.transform.position)>.6f;
                if(Physics.CheckBox(place+Vector3.up*.22f,new Vector3(.28f,.17f,.36f),Quaternion.Euler(0,deployYaw,0),1<<9))canPlace=false;
                foreach(var d in world.devices.Values)if(Vector3.Distance(place,d.transform.position)<.8f)canPlace=false;
            }
            ValidateAdditionalPlacement();
            if(!ghost){ghost=world.Box("Placement preview",place,new Vector3(.43f,.15f,.58f),world.teal,world.dynamicRoot,false);}
            if(save.job.contractIndex>0)ghost.transform.localScale=DeviceSize(CurrentKit);
            ghost.transform.position=place+Vector3.up*(save.job.contractIndex>0?DeviceSize(CurrentKit).y*.5f:.075f);ghost.transform.rotation=Quaternion.Euler(0,deployYaw,0);ghost.GetComponent<Renderer>().material.SetColor("_BaseColor",canPlace?green:new Color(.8f,.15f,.1f));
        }
        void Deploy()
        {
            var j=save.job;if(save.atDepot){Notify(L("请在委托地图布置装置","Deploy devices on a job map"));return;}if(!canPlace){Notify(L("位置无效：请选择平整、无障碍的地面","Invalid location. Choose flat, clear ground."));return;}
            
            if(!CurrentKit.HasValue||SelectedCargo||!IsDeployable(CurrentKit))return;
            bool cage=IsCage(CurrentKit);
            if(j.contractIndex==0?j.devices.Any(d=>d.cage==cage):j.devices.Count>=(int)SkillValue("deploymentTotal",8)){Notify(L("当前装置已达上限，请先收回","Device limit reached; collect an existing device"));return;}
            var d=new DeviceRecord{id=Guid.NewGuid().ToString("N"),itemId=j.itemIds[selected],position=place,yaw=deployYaw,cage=cage,kit=(int)CurrentKit.Value,baited=selected<j.baitedSlots.Count&&j.baitedSlots[selected]};
            RecordKitUse(CurrentKit.Value);if(CurrentKit==Kit.Camera)d.energy=j.conditions[selected]*3;j.devices.Add(d);world.CreateDevice(d);RemoveSlot(selected);preview=false;Sound("hit");Persist();
            Notify(L("装置已布置。蹲下退开，等待动物接近。","Device armed. Crouch and stand back while the animal approaches."));
        }


        public void DamageFurniture(int index,AnimalRecord source=null)
        {
            if(save.job.mapIndex==5)return;
            if(save.job.contractIndex>0&&Contract(save.job).type=="Protect"&&save.job.nightSeconds>=Contract(save.job).durationSeconds)return;
            var j=save.job;j.durability[index]=Mathf.Max(0,j.durability[index]-10);
            if(source!=null)source.assetDamage+=10;
            if(j.TotalDurability<300&&(j.contractIndex==0||Contract(j).type=="Protect"))FinalizeJob(false,true);
        }
        public void FinalizeJob(bool success,bool failed=false)
        {
            if(save.atDepot||save.job==null || (success&&!save.job.CanDepart))return;
            if(save.job.contractIndex>0){FinalizeAdditionalContract(success,failed);return;}
            SaveToolState();
            var candidate=JsonUtility.FromJson<SaveRecord>(JsonUtility.ToJson(save));var j=candidate.job;int newStars=0,reward=0,xp=0;
            if(success) {
                int stars=(j.prepStar?1:0)|(j.TotalDurability>=400?2:0)|(j.animals.Any(a=>a.loaded&&!a.dead&&a.species!="C01")?4:0);
                int newBits=stars&~candidate.stars;for(int i=0;i<3;i++)if((newBits&(1<<i))!=0)newStars++;
                candidate.stars|=stars;candidate.successes++;var c=config.contracts[0];reward=c.rewardCash+newStars*c.firstStarCash;xp=c.rewardXP+newStars*c.firstStarXP;candidate.cash+=reward;candidate.xp+=xp;
                if(!candidate.completedContracts.Contains("Q01"))candidate.completedContracts.Add("Q01");candidate.contractStars[0]=candidate.stars;
            }
            if(!failed)foreach(var a in j.animals.Where(a=>a.loaded&&!a.terminal))candidate.cargo.Add(a);

            CarryEquipmentHome(candidate,j,failed);
            candidate.job.looseItems.RemoveAll(item=>j.borrowedItems.Contains(item.id));candidate.magazines.RemoveAll(m=>j.borrowedItems.Contains(m.itemId));candidate.snakeLiners.RemoveAll(id=>j.borrowedItems.Contains(id));candidate.installations.RemoveAll(i=>j.borrowedItems.Contains(i.itemId));
            candidate.atDepot=true;candidate.revision=4;
            try{DemoSave.Write(candidate);}catch(Exception e){Notify(L("结算保存失败，请重试：","Settlement could not be saved: ")+e.Message);return;}
            save=candidate;result=success?L("委托成功","CONTRACT COMPLETE")+"\n+ ¥"+reward+"   + "+xp+" XP\n"+L("首次新星：","New stars: ")+newStars:failed?L("委托失败：家具低于60%。本次动物货物丢失。","Contract failed: property below 60%. This job's animal cargo was lost."):L("已主动终止。本次已装车货物保留，不发委托奖励。","Job aborted. Loaded cargo retained; no contract reward.");
            ShowCompany();Phase=ShiftPhase.Result;SyncPause();Sound("ui");
        }
        void DeliverOrder(){DeliverCart();}
        void Order(string item,int price){AddCart(item=="spray"?Kit.SprayRefill:item=="repair"?Kit.RepairPack:item=="clean"?Kit.CleaningRefill:Kit.Bait,1);}
        public void Persist()
        {
            if(frontend){SaveFrontendSettings();return;}
            if(world)foreach(var animal in world.animals.Values)if(animal)animal.SaveAdditionalBehavior();
            if(save.job!=null&&world&&world.controller){save.job.player=world.controller.transform.position;save.job.yaw=world.controller.transform.eulerAngles.y;}
            SaveToolState();save.revision=4;save.sensitivity=sensitivity;
            try{DemoSave.Write(save);}catch(Exception e){Notify(L("保存失败：","Save failed: ")+e.Message);Debug.LogError(e);}
        }
        void Reload()
        {
            StopDrone();
            save=DemoSave.Load(config.profile.startingCash);sensitivity=save.sensitivity;LoadFrontendSettings();NormalizeContractHistory();
            if(save.job!=null&&!save.atDepot){Phase=save.job.stage==2?ShiftPhase.Night:ShiftPhase.Day;world.SetDepot(false);world.RestoreJob();selected=0;world.cam.transform.localPosition=Vector3.up*1.6f;world.cam.transform.localRotation=Quaternion.identity;world.Equip(CurrentKit);panel="";SyncPause();}
            else {ShowCompany();SyncPause();}Notify(string.IsNullOrEmpty(DemoSave.loadProblem)?L("已载入最近存档。","Loaded the latest save."):DemoSave.loadProblem);
        }
        void OnApplicationQuit(){if(save!=null&&enteredPlay)Persist();Time.timeScale=1;}
        void OnDestroy(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        string KitName(Kit k){if((int)k>=12){var item=AdditionalItem(k);return save.english?item.en:item.zh;}string[] zh={"喷洒器","清洁工具","修缮工具","手电","捕鼠夹","小型捕捉笼","短棍","喷剂补充瓶","维修材料包","诱饵","老鼠尸体","清洁补充剂"};string[] en={"Sprayer","Cleaning kit","Repair kit","Flashlight","Snap trap","Small cage","Baton","Spray refill","Repair materials","Bait","Mouse remains","Cleaning refill"};return save.english?en[(int)k]:zh[(int)k];}
        string WorkName(int i){if(save.job!=null&&i==0&&save.job.contractIndex==2)return L("蚁路与源口封缝","Ant trail and entrance seal");if(save.job!=null&&i==0&&save.job.contractIndex==4)return L("潮湿纸料隔离","Damp paper isolation");if(save.job!=null&&save.job.mapIndex>0){string[][] names={new[]{"受害织物隔离清洁","受害木件处理","污染粮批隔离","入口封堵A","入口封堵B"},new[]{"垃圾与蝇源","积水与蜂巢","漏水维修","破网修复A","破网修复B"},new[]{"湿垫料与蝇源","排水与蝇源","漏水槽","围栏修复A","围栏修复B"},new[]{"受害树段A","受害树段B / 果面虫源","灌溉支管A","灌溉支管B","活动洞口"},new[]{"田边虫源","积水源","田埂修复","追踪点A","追踪点B"}};string[][] englishNames={new[]{"Infested cloth isolation","Damaged timber treatment","Contaminated grain quarantine","Entrance seal A","Entrance seal B"},new[]{"Waste and fly source","Standing water and nest","Leaking pipe","Net repair A","Net repair B"},new[]{"Wet bedding and flies","Drainage and flies","Leaking trough","Fence repair A","Fence repair B"},new[]{"Affected tree section A","Affected tree section B / fruit pests","Irrigation pipe A","Irrigation pipe B","Active burrow entrance"},new[]{"Field edge insect source","Standing water source","Field ridge repair","Tracking point A","Tracking point B"}};return "W"+(i+1)+" · "+(save.english?englishNames:names)[save.job.mapIndex-1][i];}string[] zh={"W01 厨房虫源","W02 后院积水","W03 杂物间漏水","F01 东墙鼠洞","F02 西墙鼠洞"};string[] en={"W01 Kitchen infestation","W02 Backyard water","W03 Utility leak","F01 East entrance","F02 West entrance"};return save.english?en[i]:zh[i];}
        string SpeciesName(string id){var s=config.content.species.FirstOrDefault(v=>v.id==id);return s==null?id:L(s.zh,s.en);}
        void InitStyles()
        {
            if(stylesReady)return;stylesReady=true;white=Texture2D.whiteTexture;
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=19,wordWrap=true};text.normal.textColor=ink;
            small=new GUIStyle(text){fontSize=15};small.normal.textColor=muted;
            title=new GUIStyle(text){fontSize=42,fontStyle=FontStyle.Bold};
            heading=new GUIStyle(text){fontSize=25,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=18,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(18,14,8,8)};
            button.normal.textColor=ink;button.hover.textColor=gold;
        }
        void RectFill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,white);GUI.color=old;}
        void Label(float x,float y,float w,float h,string s,GUIStyle style=null){GUI.Label(new Rect(x,y,w,h),s,style??text);}
        bool Button(float x,float y,float w,float h,string label,bool enabled=true){GUI.enabled=enabled;bool click=GUI.Button(new Rect(x,y,w,h),label,button);GUI.enabled=true;if(click)Sound("ui");return click;}
        void OnGUI()
        {
            if(save==null||world==null)return;InitStyles();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/900f,1));
            if(frontend){FrontendUI();return;}
            if(Phase==ShiftPhase.Result){CompanyUI();return;}
            if(Phase==ShiftPhase.Company){DepotHUD();if(panel!="")DepotPanel();return;}
            HUD();OpticsOverlay();if(panel!="")PanelUI();
        }

        void DrawMarkers()
        {
            foreach(var w in world.workTargets) {
                if(save.job.work[w.index]<(WorkUsesCleaning(w.index)?3:1))Marker(w.transform.position,WorkName(w.index),gold);
            }
            foreach(var a in world.animals.Values)if(!a.record.loaded&&!a.record.terminal)Marker(a.transform.position,SpeciesName(a.record.species)+(a.record.dead?L(" [尸体]"," [remains]"):a.record.contained?L(" [受控]"," [caged]"):""),a.record.species=="C01"?gold:green);
            foreach(var d in world.devices.Values)Marker(d.transform.position,d.record.cage?L("小笼","Cage"):L("捕鼠夹","Trap"),muted);
        }
        void Marker(Vector3 p,string name,Color c)
        {
            float distance=Vector3.Distance(world.controller.transform.position,p);if(distance>SenseRadius)return;
            Vector3 s=world.cam.WorldToViewportPoint(p+Vector3.up*.25f);if(s.z<0)return;
            float x=s.x*1600,y=(1-s.y)*900;RectFill(new Rect(x-3,y-3,6,6),c);Label(x-120,y-32,240,28,name+"  "+distance.ToString("0.0")+"m",small);
        }
        void CompanyUI()
        {
            RectFill(new Rect(0,0,680,900),new Color(.018f,.045f,.048f,.97f));RectFill(new Rect(680,0,5,900),gold);
            Label(48,46,580,35,"R I V E R B E N D   F I E L D   S E R V I C E S",small);
            Label(48,99,570,110,L("河湾场地管理\n捕鼠模拟器","RIVERBEND\nRat Control Simulator"),title);
            Label(48,224,580,45,L("公司整备  /  第 ","DEPOT  /  job ")+(save.runs+1),heading);
            Label(48,281,560,35,"¥ "+save.cash+"     "+save.xp+" XP     Lv."+PlayerLevel,heading);
            if(Phase==ShiftPhase.Result) {
                RectFill(new Rect(48,338,580,175),new Color(.06f,.18f,.16f));Label(68,355,540,145,result,heading);
                if(Button(48,537,580,58,L("返回公司整备","Return to depot"))){Phase=ShiftPhase.Company;tab=0;SyncPause();Persist();}
            } else if(tab==0) {
                Label(48,340,580,37,L("Q01 · 回家前的清理","Q01 · Before they come home"),heading);
                Label(48,389,565,113,L("榆树街住宅：处理厨房虫源、后院积水与漏水。夜间清除5只小家鼠，财产保留至少60%。\n白天不限时；可活捉，也可用捕鼠夹。","Elm Street home: treat two infestations and fix the leak. Control 5 house mice at night; retain at least 60% property.\nUnlimited daytime preparation; live capture accepted."));
                Label(48,518,560,85,L("三项挑战：入夜前封好两洞 / 财产≥80% / 普通活体装车\n报酬 ¥150 + 120XP；每颗首次星另加 ¥30 + 40XP","Challenges: both entrances sealed / property ≥80% / live pest loaded\nReward ¥150 + 120XP; first stars add ¥30 + 40XP"),small);
                if(Button(48,623,580,61,L("接单出勤 → 榆树街住宅","Accept job → Elm Street")))StartContract();
                if(Button(48,700,280,54,L("货物交易 / 捐赠","Cargo / donations")))tab=1;
                if(Button(345,700,283,54,L("图鉴 / 设置","Field guide / settings")))tab=2;
            } else if(tab==1) {
                CargoUI(48,350,580,true);
                if(Button(48,770,580,52,L("← 返回委托板","← Back to contracts")))tab=0;
            } else {
                GuideUI(48,350,580);SettingsUI(48,605,580);
                if(Button(48,800,580,52,L("← 返回委托板","← Back to contracts")))tab=0;
            }
            Label(738,726,800,64,L("准备决定夜班的难度。\n封堵能保护房子，但不会凭空消除动物。","YOUR PREPARATION SHAPES THE NIGHT.\nSealing protects the house; the animals remain outside."),heading);
            Label(738,827,800,38,L("白模开发版 · 十八份委托 · 无背景音乐","WHITEBOX DEVELOPMENT · eighteen contracts · no background music"),small);
            if(Button(48,845,270,34,save.english?"简体中文":"English")){save.english=!save.english;Persist();}
            if(Button(345,845,283,34,L("退出游戏","Quit")))Application.Quit();
            if(toastTime>0)Label(730,38,780,100,toast,heading);
        }
        void PanelUI()
        {
            if(panel=="refill"){RefillUI();return;}
            if(panel=="commands"){CompanionCommandsUI();return;}
            RectFill(new Rect(0,0,1600,900),new Color(0,0,0,.63f));RectFill(new Rect(300,95,1000,710),new Color(.024f,.065f,.069f,.99f));RectFill(new Rect(300,95,1000,4),gold);
            string name=panel=="growth"?L("成长与整备","GROWTH / SETUP"):panel=="services"?L("升级服务","UPGRADES"):panel=="partners"?L("作业伙伴","COMPANIONS"):panel=="containers"?L("笼具配置","CAGE SETUP"):panel=="monitor"?L("现场监测","MONITORING"):panel=="cargo"?L("装载笼","LOADING CAGE"):panel=="van"?L("工作卡车","WORK TRUCK"):panel=="night"?L("确认入夜","BEGIN NIGHT SHIFT"):panel=="journal"?L("作业手册","FIELD NOTES"):panel=="bag"?L("随身背包","BACKPACK"):L("暂停","PAUSED");
            Label(335,121,800,45,name,heading);
            if(Button(1152,118,110,42,L("关闭","Close"))){Close();return;}
            var j=save.job;
            if(panel=="services"){ServicesUI();return;}if(panel=="containers"){ContainerSetupUI();return;}if(panel=="monitor"){MonitoringUI();return;}if(panel=="growth"){GrowthUI();return;}if(panel=="partners"){PartnersUI();return;}
            if(j.contractIndex>0&&((panel=="van"&&tab==1)||panel=="journal"&&tab==0)){AdditionalContractPanel();return;}
            if(panel=="cargo") {
                CargoPanel();
            } else if(panel=="van") {
                if(Button(335,185,285,45,L("购买补给","Shop supplies")))tab=0;
                if(Button(635,185,285,45,L("货物与离场","Cargo / departure")))tab=1;
                if(tab==0) {ShopUI(335,240,880);if(Button(335,742,880,43,L("等待到夜晚","Wait until night"),Phase==ShiftPhase.Day))Open("night");}
                else {
                    Label(335,250,860,35,L("车辆活体：","Live cargo: ")+j.animals.Count(a=>a.loaded&&!a.dead)+"/4   "+L("尸体：","Remains: ")+j.animals.Count(a=>a.loaded&&a.dead),heading);
                    Label(335,302,860,50,L("请走到右侧装载笼按 E 装车或管理动物。","Use E at the loading cage to load or manage animals."));
                    Label(335,380,890,85,L("离场条件：必做作业3/3、任务鼠5/5、财产≥60%。\n动物装车或留在随身笼内可带回；释放现场目标会撤销计数。","Departure requires all work, 5/5 mice and property ≥60%.\nLoaded animals and portable cages return. Releasing a target cancels control."));
                    Label(335,472,880,55,L("尚在现场的动物/尸体：","Animals / remains left on site: ")+j.animals.Count(a=>!a.loaded&&!a.terminal)+L("；未调查：","; unexamined: ")+j.animals.Count(a=>!a.investigated),small);
                    if(Button(335,555,880,58,L("复核通过并回公司结算","Finalize success and return to depot"),j.CanDepart))FinalizeJob(true);
                    if(Button(335,635,880,53,L("主动终止（无报酬，保留已装车货物）","Abort (no reward; keep loaded cargo)")))Open("abort");
                }
            } else if(panel=="night") {
                Label(335,240,880,100,j.contractIndex==0?L("准备完成 ","Prepared ")+Enumerable.Range(0,3).Count(i=>j.work[i]>=(i<2?3:1))+"/3":L("本单必做作业：","Required work: ")+(j.RequiredWorkDone?L("已完成","complete"):L("尚未完成，入夜后仍可继续","unfinished; work remains available at night"))+"\n"+ContractDescription(Contract(j)),small);
                if(Button(335,400,880,60,L("等待到夜晚","Wait until night")))BeginNight();
            } else if(panel=="journal") {
                if(Button(335,185,280,43,L("合同与路线","Contract / routes")))tab=0;
                if(Button(630,185,280,43,L("图鉴","Field guide")))tab=1;
                if(tab==0) {
                    for(int i=0;i<5;i++)Label(335,250+i*52,485,48,(j.work[i]>=(i<2?3:1)?"✓ ":"○ ")+WorkName(i)+" · "+WorkStatus(i),text);
                    Label(335,532,450,110,L("准备星：","Preparation star: ")+(Phase==ShiftPhase.Day?L("入夜时判定","evaluated at night start"):j.prepStar?"✓":"○")+"\n"+L("鼠类受控：","Mice controlled: ")+j.Handled+"/5\n"+L("财产：","Property: ")+j.TotalDurability/5+"%",heading);
                    DrawMap(830,263);
                    Label(335,690,870,70,L("Q 扫描半径10m，标记持续4秒，冷却10秒。未知目标不会自动解锁图鉴。\n靠墙布笼后退开、移开视线；E 拾取 / 长按 E 开笼、作业；Q 扫描 / 长按 Q 调查。\nG 丢下；F 手电；0 空手；左键近战、工具；右键布笼。","Q scans 10m for 4 seconds with a 10-second cooldown; it does not unlock entries.\nPlace traps near walls, crouch and retreat. E collect / hold E open or work; Q scan / hold Q inspect.\nG drop; F light; 0 empty hands; LMB attack / tool; RMB place."),small);
                } else GuideUI(335,255,880);
            } else if(panel=="bag") {
                BackpackUI();
            } else if(panel=="abort") {
                Label(335,240,870,130,j.contractIndex==0?L("确定终止本次委托？\n不发合同奖励或新星。已装车动物保留；装备和未用补给跨委托保留。","Abort this job?\nNo contract reward or new stars. Loaded animals, equipment and unused supplies are retained."):L("确定终止本次委托？无委托报酬与新星；借物归还，自有装备保留。\n","Abort this job? No contract reward or stars. Loan items return; owned tools remain.\n")+(Contract(j).abortPolicy=="KeepLoadedAnimalsWithoutContractReward"?L("已装车动物保留。","Loaded animals are retained."):L("本单动物不带回。","This job's animals will not return with you.")),heading);
                if(Button(335,460,880,60,L("确认终止并回公司","Confirm abort and return")))FinalizeJob(false);
                if(Button(335,545,880,60,L("返回现场","Return to site")))Close();
            } else {
                if(Button(335,230,880,55,L("继续作业","Resume shift")))Close();
                if(Button(335,305,430,55,L("保存现场进度","Save job"))){Persist();Notify(L("已保存","Saved"));}
                if(Button(785,305,430,55,L("载入最近存档","Load latest save")))Reload();
                SettingsUI(335,405,880);
                if(save.atDepot){if(Button(335,590,880,45,L("成长与技能（K）","Growth / skills (K)")))Open("growth");if(Button(335,675,880,55,L("退出游戏","Quit game")))Application.Quit();}
                else if(Button(335,675,880,55,L("终止本次委托","Abort this contract")))Open("abort");
            }
        }

        void SupplyUI(float x,float y){ShopUI(x,y,850);}
        void CargoUI(float x,float y,float width,bool trade)
        {
            Label(x,y,width,35,L("每只货物只能出售或捐赠一次","Each animal can be sold or donated once"),small);
            if(save.cargo.Count==0)Label(x,y+55,width,100,L("暂无公司货物。把捕获物装车，正常离场后带回。","No cargo at the depot. Load animals into the van and return from a job."));
            for(int i=0;i<Mathf.Min(save.cargo.Count,4);i++) {
                var a=save.cargo[i];float row=y+47+i*73;
                Label(x,row,width*.5f,62,SpeciesName(a.species)+(a.dead?L(" / 尸体"," / remains"):L(" / 活体"," / alive"))+"\n"+a.id.Substring(a.id.Length-6),small);
                if(Button(x+width*.51f,row,width*.23f,51,"¥"+DemoSave.Price(config,a))) {try{DemoSave.Trade(save,config,a.id,false);}catch(Exception e){Notify(e.Message);}return;}
                if(Button(x+width*.77f,row,width*.23f,51,"+"+config.Species(a.species).donationXP+"XP",!a.dead)){try{DemoSave.Trade(save,config,a.id,true);}catch(Exception e){Notify(e.Message);}return;}
            }
            if(save.cargo.Count>4)Label(x,y+346,width,30,L("交易前四只后会显示其余货物。","Trade the first four to reveal the remaining cargo."),small);
            Label(x,y+371,width,49,L("初始礼包仅首次赠送；工具和耗材跨委托保留。","Starter stock is granted once; equipment persists between jobs."),small);
        }
        void GuideUI(float x,float y,float width)
        {
            var species=config.content.species;Label(x,y,width,38,L("图鉴 ","FIELD GUIDE ")+save.encyclopedia.Count+"/45",heading);
            for(int row=0;row<9;row++) {int i=guidePage*9+row;if(i>=species.Length)break;var s=species[i];bool known=save.encyclopedia.Contains(s.id);
                if(Button(x,y+48+row*38,width>700?470:width,35,(known?"✓  ":"○  ")+s.id+" · "+SpeciesName(s.id)+"   "+(known?L("已调查","investigated"):s.id.StartsWith("C")?L("活捕后长按Q调查","Hold Q after live capture"):L("调查遗体或源标本","Inspect remains or a source sample"))))guideSelected=i;
            }
            if(width>700)SpeciesPreview(x+500,y+38,width-500);
            if(Button(x,y+400,130,38,"←",guidePage>0))guidePage--;Label(x+150,y+405,100,30,(guidePage+1)+" / 5",small);if(Button(x+270,y+400,130,38,"→",guidePage<4))guidePage++;
        }
        void SettingsUI(float x,float y,float width)
        {
            Label(x,y,width,35,L("设置（即时保存）","Settings (saved immediately)"),heading);
            Label(x,y+47,200,28,L("鼠标灵敏度","Mouse sensitivity"),small);sensitivity=GUI.HorizontalSlider(new Rect(x+225,y+58,width-250,20),sensitivity,.025f,.2f);
            Label(x,y+91,200,28,L("音效音量","Effects volume"),small);save.volume=GUI.HorizontalSlider(new Rect(x+225,y+102,width-250,20),save.volume,0,1);
            if(Button(x,y+139,width,43,save.english?"切换简体中文":"Switch to English")){save.english=!save.english;Persist();}
            if(GUI.changed)Persist();
        }
        void DrawMap(float x,float y)
        {
            RectFill(new Rect(x,y,360,370),new Color(.07f,.14f,.14f));RectFill(new Rect(x+65,y+70,230,185),new Color(.21f,.28f,.25f));
            Label(x+80,y+90,100,30,L("客厅","Living"),small);Label(x+211,y+90,115,30,L("厨房 W01","Kitchen W01"),small);
            Label(x+172,y+220,150,35,L("杂物间 W03","Utility W03"),small);Label(x+85,y+20,235,35,L("后院 W02 / 稀有线索","Backyard W02 / rare clue"),small);
            Label(x+14,y+143,80,50,"F02\n○",small);Label(x+290,y+143,70,50,"F01\n○",small);
            Label(x+10,y+302,180,50,L("车辆 / 出发点","Van / arrival"),small);Label(x+132,y+282,140,30,L("正门 / 楼梯","Door / stairs"),small);
            Label(x+35,y+341,310,30,L("示意图 · 鼠在一楼、二楼与院落活动","Schematic · mice patrol walls / backyard"),small);
        }
    }
}


