using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Riverbend {
    public class DepotAnimal : MonoBehaviour { public string animalId; }
    public partial class DemoGame {
        Kit refillKit; bool destinationTruck, tradeDonation;
        readonly Kit[] shopKits=new[]{Kit.SprayRefill,Kit.RepairPack,Kit.CleaningRefill,Kit.Bait,Kit.Spray,Kit.Broom,Kit.Repair,Kit.SnapTrap,Kit.Cage,Kit.Baton}.Concat(Enumerable.Range(12,20).Select(i=>(Kit)i)).ToArray();
        int[] shopPrices;int shopPage;
        readonly string[] mapsZh={"榆树街住宅","风车旧粮仓","柳沟散养鸡场","铁棚养猪场","石坡果园","北岸开阔农田"};
        readonly string[] mapsEn={"Elm Street House","Old Windmill Granary","Willow Creek Chicken Farm","Iron Shed Pig Farm","Stone Slope Orchard","North Bank Open Farmland"};
        readonly string[] contractsZh={"回家前的清理","储物柜保卫","邻居已经睡了","开仓检查","梁上的脚步","取样活体","蛋窝的动静","沟渠来客","狐狸的撤离线","雨后的棚舍","饲料间翻找声","别惹那个尾巴","树干上的圆孔","谁在咬树根","收果前一夜","田野猎踪","麦地野猪","旷野最后一班"};
        readonly string[] contractsEn={"Before They Come Home","Cupboard Guard","The Neighbours Are Asleep","Opening Inspection","Footsteps in the Rafters","Live Samples","Sounds by the Nests","Ditch Visitor","The Fox Retreat","Sheds After Rain","Feed Store Rummaging","Leave That Tail Alone","Round Holes in the Trunk","Who Is Biting the Roots","Before Harvest Night","Field Tracking","Boars in the Wheat","Last Shift in the Open"};
        readonly string[] rewardRanges={"150 / 120 XP","140–200 / 120–160 XP","150–220 / 140–180 XP","180–260 / 160–200 XP","200–280 / 180–220 XP","220–320 / 200–240 XP","240–340 / 220–260 XP","260–360 / 240–280 XP","280–380 / 260–300 XP","300–420 / 280–320 XP","320–440 / 300–340 XP","340–460 / 320–360 XP","360–500 / 340–380 XP","400–540 / 360–400 XP","420–580 / 380–420 XP","450–620 / 420–460 XP","500–680 / 460–500 XP","550–750 / 520–600 XP"};
        int boardStep;
        void EnsureDepot() {
            if(save.depotItems==null)save.depotItems=new List<LooseItemRecord>();
            if(save.cart==null)save.cart=new List<int>();while(save.cart.Count<shopKits.Length)save.cart.Add(0);
            if(save.job==null)save.job=new JobRecord{runId="depot",player=new Vector3(0,.1f,-3),yaw=0};
            var j=save.job;
            if(j.itemIds==null)j.itemIds=new List<string>();if(j.conditions==null)j.conditions=new List<float>();if(j.deliveredItems==null)j.deliveredItems=new List<LooseItemRecord>();
            if(save.saleHolding==null)save.saleHolding=new List<string>();if(save.donationHolding==null)save.donationHolding=new List<string>();
            if(j.crateItems==null)j.crateItems=new List<LooseItemRecord>();
            NormalizeSlots();
            if(!save.starterGranted){if(j.looseItems.Count==0&&j.kit.Count==0)GrantStarter(j);save.starterGranted=true;}
            if(!string.IsNullOrEmpty(j.deliveredItem)){j.deliveredItems.Add(NewItem(j.deliveredItem=="repair"?Kit.RepairPack:j.deliveredItem=="clean"?Kit.CleaningRefill:j.deliveredItem=="bait"?Kit.Bait:Kit.SprayRefill));j.deliveredItem=null;}
        }
        int PackageAmount(Kit k)=>(int)k>=12?Mathf.Max(1,AdditionalItem(k).maxUsesPerPackage):k==Kit.RepairPack||k==Kit.CleaningRefill||k==Kit.SprayRefill?100:k==Kit.Bait?(int)SkillValue("usesPerBaitPackage",5):1;
        LooseItemRecord NewItem(Kit k)=>new LooseItemRecord{id=Guid.NewGuid().ToString("N"),kit=(int)k,quantity=PackageAmount(k),condition=IsSupply(k)?-1:k==Kit.Spray||k==Kit.Broom||k==Kit.Repair?0:100};
        void GrantStarter(JobRecord j){
            int n=0;foreach(var k in new[]{Kit.Spray,Kit.Broom,Kit.Repair,Kit.SnapTrap,Kit.Cage,Kit.Baton,Kit.SprayRefill,Kit.RepairPack,Kit.Bait,Kit.CleaningRefill}){
                var item=NewItem(k);item.position=TruckItemPosition(n++);j.looseItems.Add(item);
            }
        }
        Vector3 TruckItemPosition(int n)=>DemoWorld.VanPoint+(n%10<5?new Vector3(.85f,1.1f+(n/10)*.3f,1.85f+(n%5)*.27f):new Vector3(-1+(n%5)*.48f,1.1f+(n/10)*.3f,-1.3f));
        bool IsTruckItem(LooseItemRecord item){var p=item.position-DemoWorld.VanPoint;return Mathf.Abs(p.x)<1.8f&&p.z> -2&&p.z<3.5f;}
        JobRecord NewShift(JobRecord previous){
            var j=new JobRecord{runId="depot",player=new Vector3(-14,.1f,-5),yaw=45,loan=false};
            j.kit=new List<int>(previous.kit);j.quantities=new List<int>(previous.quantities);j.itemIds=new List<string>(previous.itemIds);j.conditions=new List<float>(previous.conditions);j.cargoIds=new List<string>(previous.cargoIds);j.baitedSlots=new List<bool>(previous.baitedSlots);
            save.depotItems=previous.looseItems.Where(x=>!IsTruckItem(x)).ToList();
            j.looseItems=previous.looseItems.Where(IsTruckItem).ToList();j.deliveredItems=previous.deliveredItems;j.crateItems=previous.crateItems;j.orderItem=previous.orderItem;j.orderRemaining=previous.orderRemaining;
            // Only physically loaded truck stock travels; counter stock stays at the depot.
            for(int n=0;n<j.looseItems.Count;n++)j.looseItems[n].position=TruckItemPosition(n);
            if(j.deliveredItems.Count>0)save.orderDestination="Field";
            return j;
        }
        void CarryEquipmentHome(SaveRecord candidate,JobRecord j,bool failed){
            var returned=new JobRecord{runId="depot",player=new Vector3(0,.1f,-3),yaw=0,loan=false};
            NormalizeSlots();var ids=new HashSet<string>();int n=0;
            for(int i=0;i<j.kit.Count;i++){
                if(i<j.cargoIds.Count&&!string.IsNullOrEmpty(j.cargoIds[i])){
                    var a=j.animals.Find(x=>x.id==j.cargoIds[i]);
                    if(!failed&&a!=null&&!a.terminal&&!a.loaded&&!candidate.cargo.Any(x=>x.id==a.id)){a.loaded=true;candidate.cargo.Add(a);}
                    if(!IsCage((Kit)j.kit[i])&&j.kit[i]!=(int)Kit.Net)continue;
                }
                string id=j.itemIds[i];if(!ids.Add(id))continue;
                returned.looseItems.Add(new LooseItemRecord{id=id,kit=j.kit[i],quantity=j.quantities[i],condition=j.conditions[i],baited=j.baitedSlots[i],position=TruckItemPosition(n++)});
            }
            foreach(var item in j.looseItems){if(!ids.Add(item.id))continue;item.position=TruckItemPosition(n++);returned.looseItems.Add(item);}
            foreach(var d in j.devices){string id=string.IsNullOrEmpty(d.itemId)?d.id:d.itemId;if(ids.Add(id))returned.looseItems.Add(new LooseItemRecord{id=id,kit=(int)DeviceKit(d),quantity=1,condition=d.kit==(int)Kit.Camera?d.energy/3:100,position=TruckItemPosition(n++)});}
            foreach(var item in j.crateItems){if(ids.Add(item.id)){item.position=TruckItemPosition(n++);returned.looseItems.Add(item);}}
            foreach(var item in candidate.depotItems)if(ids.Add(item.id))returned.looseItems.Add(item);candidate.depotItems.Clear();
            returned.deliveredItems=j.deliveredItems;returned.orderItem=j.orderItem;returned.orderRemaining=j.orderRemaining;
            candidate.job=returned;
        }
        public void SaveToolState(){if(save==null||save.job==null)return;NormalizeSlots();if(selected>=0&&selected<save.job.kit.Count)SaveToolStateFor(selected);}
        void SaveToolStateFor(int slot){var j=save.job;if(slot<0||slot>=j.kit.Count)return;var k=(Kit)j.kit[slot];if(k==Kit.Spray)j.conditions[slot]=Mathf.Clamp((j.spray-j.spraySeconds/4f)*20,0,100);else if(k==Kit.Repair)j.conditions[slot]=j.repairs;else if(k==Kit.Broom)j.conditions[slot]=j.cleaning;}
        void LoadToolState(int slot){var j=save.job;if(slot<0||slot>=j.kit.Count)return;NormalizeSlots();float v=j.conditions[slot];var k=(Kit)j.kit[slot];if(k==Kit.Spray){j.spray=Mathf.CeilToInt(v/20);j.spraySeconds=(j.spray-v/20)*4;}else if(k==Kit.Repair)j.repairs=Mathf.RoundToInt(v);else if(k==Kit.Broom)j.cleaning=Mathf.RoundToInt(v);}
        float SlotCondition(int slot){var j=save.job;NormalizeSlots();if(slot==selected)SaveToolStateFor(slot);return j.kit[slot]==0||j.kit[slot]==1||j.kit[slot]==2?j.conditions[slot]/100:-1;}
        void AddCart(Kit k,int change){EnsureCart();int i=Array.IndexOf(shopKits,k);if(i>=0)save.cart[i]=Mathf.Clamp(save.cart[i]+change,0,20);Persist();}
        void EnsureCart(){if(save.depotItems==null)save.depotItems=new List<LooseItemRecord>();
            if(save.cart==null)save.cart=new List<int>();while(save.cart.Count<shopKits.Length)save.cart.Add(0);}
        int CartTotal(){EnsureCart();return Enumerable.Range(0,shopKits.Length).Sum(i=>save.cart[i]*shopPrices[i]);}
        void Checkout(){
            var j=save.job;int total=CartTotal();SaveToolState();if(total<=0||save.cash<total||!string.IsNullOrEmpty(j.orderItem))return;
            var before=JsonUtility.ToJson(save);
            try{
                save.cash-=total;
                for(int i=0;i<shopKits.Length;i++)for(int n=0;n<save.cart[i];n++)j.deliveredItems.Add(NewItem(shopKits[i]));
                save.cart=Enumerable.Repeat(0,shopKits.Length).ToList();save.orderDestination=save.atDepot?(destinationTruck?"Truck":"Counter"):"Field";
                j.orderItem="batch";j.orderRemaining=save.atDepot?0:10;
                if(save.atDepot)DeliverCart();else DemoSave.Write(save);
                Notify(L("整单已确认，货物需实际领取","Order confirmed; collect the physical goods"));
            }catch(Exception e){save=JsonUtility.FromJson<SaveRecord>(before);Notify(L("付款失败：","Checkout failed: ")+e.Message);}
        }
        void DeliverCart(){
            var j=save.job;if(string.IsNullOrEmpty(j.orderItem))return;
            var items=j.deliveredItems.ToArray();int n=save.atDepot&&save.orderDestination=="Counter"?j.looseItems.Count(x=>!IsTruckItem(x)):j.looseItems.Count(IsTruckItem);
            foreach(var item in items){
                if(!save.atDepot){if(!j.crateItems.Any(x=>x.id==item.id))j.crateItems.Add(item);continue;}
                item.position=save.orderDestination=="Counter"?new Vector3(-1.6f+(n%5)*.65f,1.04f+(n/5)*.3f,13.8f):TruckItemPosition(n);
                if(!j.looseItems.Any(x=>x.id==item.id)){j.looseItems.Add(item);world.CreateLooseItem(item);}n++;
            }
            j.deliveredItems.Clear();j.orderItem=null;j.orderRemaining=0;if(!save.atDepot)world.ShowDelivery();DemoSave.Write(save);Notify(L("整单送达 · 到收货台/配送箱 E 拿取","Order delivered · E collect at counter or crate"));
        }
        void TakeDelivery(DemoTarget t){var j=save.job;if(j.crateItems.Count==0)return;var r=j.crateItems[0];if(!HasBackpackSpace((Kit)r.kit)){Notify(L("背包已满，货物保留","Backpack full; goods retained"));return;}j.crateItems.RemoveAt(0);j.kit.Add(r.kit);j.quantities.Add(r.quantity);j.itemIds.Add(r.id);j.conditions.Add(r.condition);NormalizeSlots();Select(j.kit.Count-1);if(j.crateItems.Count==0)Destroy(t.gameObject);Persist();}
        void ShopUI(float x,float y,float width){
            EnsureCart();Label(x,y,width,32,L("商品与数量 · 加入清单后统一确认","Choose products and quantities, then confirm the whole order"),small);
            for(int i=shopPage*8;i<Mathf.Min(shopKits.Length,shopPage*8+8);i++){
                float row=y+40+(i%8)*40;var k=shopKits[i];bool unlocked=(int)k<12||AdditionalItem(k).unlockLevel<=PlayerLevel;Label(x,row,width-240,38,KitName(k)+"  ¥"+shopPrices[i]+(PackageAmount(k)>1?"  ["+PackageAmount(k)+"]":"")+(unlocked?"":" [Lv."+AdditionalItem(k).unlockLevel+"]"),small);
                if(Button(x+width-230,row,45,34,"−"))AddCart(k,-1);Label(x+width-175,row+3,55,34,save.cart[i].ToString(),small);if(Button(x+width-110,row,45,34,"+",unlocked))AddCart(k,1);
            }
            if(Button(x,y+365,110,32,"←",shopPage>0))shopPage--;Label(x+130,y+369,150,30,(shopPage+1)+" / "+Mathf.CeilToInt(shopKits.Length/8f),small);if(Button(x+280,y+365,110,32,"→",(shopPage+1)*8<shopKits.Length))shopPage++;
            float bottom=y+405;
            if(save.atDepot){if(Button(x,bottom,width*.48f,40,(destinationTruck?"○ ":"● ")+L("立即送柜台","Now: counter")))destinationTruck=false;if(Button(x+width*.5f,bottom,width*.48f,40,(destinationTruck?"● ":"○ ")+L("立即送卡车","Now: truck")))destinationTruck=true;}
            else Label(x,bottom,width,36,L("现场整单10秒 · 菜单暂停，关闭菜单后计时","One delivery in 10 active seconds; close menus to start timer"),small);
            int total=CartTotal();if(Button(x,bottom+50,width,43,L("确认整单 ","Confirm order ")+"¥"+total+L("   余额 ","   Balance ")+save.cash,total>0&&save.cash>=total&&string.IsNullOrEmpty(save.job.orderItem)))Checkout();
        }
        void DepotUpdate(Keyboard kb,Mouse mouse){
            EnsureDepot();
            if(kb.escapeKey.wasPressedThisFrame){if(panel=="board"&&boardStep>0){boardStep=0;world.StartBoardView(0);}else if(panel!="")Close();else Open("pause");ResetGestures();return;}
            if(Paused)return;
            if(kb.f5Key.wasPressedThisFrame){Persist();Notify(L("已保存","Saved"));}if(kb.f9Key.wasPressedThisFrame){Reload();return;}
            if(kb.fKey.wasPressedThisFrame)world.torch.enabled=!world.torch.enabled;
            if(kb.kKey.wasPressedThisFrame){Open("growth");return;}if(kb.tabKey.wasPressedThisFrame||kb.iKey.wasPressedThisFrame){Open("bag");return;}
            Move(kb,mouse);AdvanceDelivery(Time.deltaTime);
            for(int i=0;i<9;i++)if(kb[(Key)((int)Key.Digit1+i)].wasPressedThisFrame)Select(i);
            if(kb.digit0Key.wasPressedThisFrame)Select(-1);
            if(mouse.scroll.ReadValue().y!=0){int n=save.job.kit.Count+1;Select(((selected+1+(mouse.scroll.ReadValue().y>0?-1:1)+n)%n)-1);}
            if(kb.gKey.wasPressedThisFrame){DropDepotItem();return;}
            FindTarget();
            if(mouse.rightButton.wasPressedThisFrame&&IsSupply(CurrentKit)){UseSupply();return;}
            if(kb.eKey.wasPressedThisFrame&&target){if(target.kind=="board"){boardStep=0;world.StartBoardView(boardStep);Open("board");}else if(target.kind=="shop")Open("shop");else if(target.kind=="sale"||target.kind=="donate"){tradeDonation=target.kind=="donate";StageCarriedAnimal();world.RefreshDepotCargo();Open("trade");}else if(target.kind=="depotAnimal")PickDepotAnimal(target.GetComponent<DepotAnimal>().animalId);else if(target.kind=="cargo"){if(SelectedCargo){save.job.cargoIds[selected]=null;if(CurrentKit==Kit.Corpse)RemoveSlot(selected);else {SyncSelectedCargo();world.Equip(CurrentKit);}world.RefreshDepotCargo();Persist();}else Open("depotcargo");}else TapAction(target);}
        }
        void DropDepotItem(){if(CurrentKit==Kit.Corpse||SelectedCargo){Notify(L("将动物放入车辆或交接笼，避免放生","Use a vehicle or transfer cage for this animal"));return;}DropCargo();}
        void PickDepotAnimal(string id){var j=save.job;var a=save.cargo.Find(v=>v.id==id);if(a==null||j.cargoIds.Contains(id)||(a.dead&&!HasBackpackSpace(Kit.Corpse)))return;
            int cage=-1;
            for(int i=0;i<j.kit.Count;i++)if(CageFits((Kit)j.kit[i],a.species)&&string.IsNullOrEmpty(j.cargoIds[i])&&CanOccupySlot(i)&&(config.content.species.First(s=>s.id==a.species).family!="Snake"||save.snakeLiners.Contains(j.itemIds[i]))){cage=i;break;}if(!a.dead&&cage<0){Notify(L("需要匹配空笼、占用后的背包空间；蛇还需封闭内胆","Requires a matching empty cage and occupied bag space; snakes need a sealed liner"));return;}
            int slot=a.dead?j.kit.Count:cage;if(a.dead){j.kit.Add((int)Kit.Corpse);j.quantities.Add(1);j.itemIds.Add(id+"-corpse");j.conditions.Add(-1);NormalizeSlots();}
            j.cargoIds[slot]=id;save.saleHolding.Remove(id);save.donationHolding.Remove(id);Select(slot);world.RefreshDepotCargo();Persist();
        }
        void StageCarriedAnimal(){var j=save.job;if(selected<0||selected>=j.cargoIds.Count)return;string id=j.cargoIds[selected];var a=save.cargo.Find(v=>v.id==id);if(a==null)return;if(tradeDonation&&a.dead){Notify(L("捐赠口只接收活体","Donation accepts living animals only"));return;}
            var list=tradeDonation?save.donationHolding:save.saleHolding;list.Add(id);j.cargoIds[selected]=null;if(a.dead)RemoveSlot(selected);else {SyncSelectedCargo();world.Equip(CurrentKit);}Persist();
        }
        void ConfirmTrade(){var list=(tradeDonation?save.donationHolding:save.saleHolding).ToArray();foreach(var id in list){var a=save.cargo.Find(v=>v.id==id);if(a==null)continue;if(!a.investigated&&!save.encyclopedia.Contains(a.species)){Notify(L("先点击调查，或明确选择继续交接","Inspect first, or explicitly continue transfer"));return;}}CommitTrade(list);}
        void CommitTrade(string[] ids){foreach(var id in ids)DemoSave.Trade(save,config,id,tradeDonation);save.saleHolding.RemoveAll(id=>!save.cargo.Any(a=>a.id==id));save.donationHolding.RemoveAll(id=>!save.cargo.Any(a=>a.id==id));world.RefreshDepotCargo();Persist();}
        void DepotHUD(){HUD();Label(30,150,600,80,L("公司基地 · 前方委托看板，后门通往三处服务区\nE 交互 · G 放下 · F 手电 · Esc 设置","DEPOT · Board ahead; vendors through the rear door\nE interact · G drop · F light · Esc settings"),small);Label(1260,120,310,40,"¥"+save.cash+"   "+save.xp+" XP",heading);}
        int depotCargoPage;
        void DepotPanel(){
            if(panel=="board"){BoardUI();return;}
            if(panel=="refill"){RefillUI();return;}
            if(panel!="shop"&&panel!="trade"&&panel!="depotcargo"){PanelUI();return;}
            RectFill(new Rect(285,80,1030,750),new Color(.025f,.065f,.06f,.98f));Label(325,100,800,42,panel=="shop"?L("器械采购柜台","EQUIPMENT COUNTER"):panel=="depotcargo"?L("车辆货物 · E拿取后搬往交接笼","TRUCK CARGO · Carry to the transfer cage"):tradeDonation?L("动物园捐赠 · 仅经验","ZOO DONATION · XP ONLY"):L("动物收购 · 出售得现金","ANIMAL BUYER · CASH"),heading);
            if(Button(1170,100,100,40,L("关闭","Close"))){Close();return;}
            if(panel=="shop"){ShopUI(325,175,920);return;}
            bool cargo=panel=="depotcargo";var ids=cargo?save.cargo.Where(a=>!save.saleHolding.Contains(a.id)&&!save.donationHolding.Contains(a.id)&&!save.job.cargoIds.Contains(a.id)).Select(a=>a.id).ToArray():(tradeDonation?save.donationHolding:save.saleHolding).ToArray();
            int pages=Mathf.Max(1,(ids.Length+5)/6);depotCargoPage=Mathf.Clamp(depotCargoPage,0,pages-1);
            for(int i=0;i<Mathf.Min(ids.Length-depotCargoPage*6,6);i++){var a=save.cargo.Find(v=>v.id==ids[depotCargoPage*6+i]);if(a==null)continue;float y=180+i*68;Label(325,y,390,60,SpeciesName(a.species)+(a.dead?L(" · 尸体"," · remains"):L(" · 活体"," · alive"))+"  "+(tradeDonation?config.Species(a.species).donationXP+" XP":"¥"+DemoSave.Price(config,a)),small);
                if(Button(740,y,140,42,L("拿取","Take"))){PickDepotAnimal(a.id);return;}
                if(!cargo&&Button(900,y,180,42,L("调查","Inspect"))){if(a.species.StartsWith("C")?a.dead:!a.dead){Notify(L("普通图鉴需尸体调查","Ordinary entries require remains"));}else {a.investigated=true;Unlock(a.species);Persist();}}
            }
            if(ids.Length==0)Label(325,220,800,100,L("笼内暂无待交动物。将手中动物带到此处按E放入。","No animals waiting here. Carry an animal here and press E."));
            if(pages>1){if(Button(325,590,170,36,L("上一页","Previous"),depotCargoPage>0))depotCargoPage--;Label(535,590,200,36,(depotCargoPage+1)+" / "+pages,small);if(Button(800,590,170,36,L("下一页","Next"),depotCargoPage<pages-1))depotCargoPage++;}
            if(!cargo){if(Button(325,640,920,48,tradeDonation?L("确认捐赠 · 只获得经验","Confirm donation · XP only"):L("确认出售 · 获得现金","Confirm sale · cash"),ids.Length>0))ConfirmTrade();if(Button(325,704,920,42,L("已知未调查，仍继续交接（不解锁图鉴）","Continue without inspection (entry remains locked)"),ids.Length>0))CommitTrade(ids);}
        }
        void RefillUI(){RectFill(new Rect(350,200,900,450),new Color(.03f,.08f,.07f,.98f));Label(385,220,800,50,L("选择要补充的工具","Choose the tool to refill"),heading);int row=0;for(int i=0;i<save.job.kit.Count;i++)if(save.job.kit[i]==(int)refillKit){int slot=i;if(Button(385,290+row++*65,800,52,KitName(refillKit)+"  #"+(i+1)+"  "+Mathf.RoundToInt(save.job.conditions[i])+"%")){Close();RefillSlot(slot);return;}}if(Button(385,565,800,45,L("取消","Cancel")))Close();}
        void BoardUI(){
            RectFill(new Rect(115,70,1370,750),new Color(.23f,.13f,.065f,.98f));RectFill(new Rect(140,95,1320,700),new Color(.055f,.105f,.085f,.98f));
            Label(180,120,590,50,L("地图","MAPS"),title);Label(830,120,580,50,L("委托","CONTRACTS"),title);
            for(int i=0;i<6;i++){int m=i;float x=180+(i%2)*285,y=200+(i/2)*123;string name=save.english?mapsEn[i]:mapsZh[i];bool available=ContractAvailable(i*3);if(Button(x,y,265,100,(save.selectedMap==i?"● ":"")+name+(available?"":L("\n完成前置委托解锁","\nComplete previous contracts")))){save.selectedMap=m;save.selectedContract=0;boardStep=1;world.StartBoardView(1);Persist();}}
            int map=Mathf.Clamp(save.selectedMap,0,5);for(int i=0;i<3;i++){int ci=map*3+i;var definition=config.content.contracts[ci];string name=save.english?definition.en:definition.zh;if(Button(830,200+i*123,565,100,(save.selectedContract==i?"● ":"")+definition.id+" · "+name+"\n¥"+definition.rewardCash+" / "+definition.rewardXP+" XP "+(ContractAvailable(ci)?"":L("[未解锁]","[locked]")))){save.selectedContract=i;Persist();}}
            var chosen=config.content.contracts[map*3+save.selectedContract];Label(180,590,1210,100,ContractDescription(chosen),small);
            if(Button(180,710,560,55,L("退出整理 · 保留选择","Leave to prepare · keep selection"))){Close();world.EndBoardView();}
            if(Button(800,710,595,55,L("确认出发 → ","Confirm departure → ")+(save.english?mapsEn[map]:mapsZh[map]),ContractAvailable(map*3+save.selectedContract))){world.EndBoardView();StartContract();}
        }
    }
    public partial class DemoWorld {
        public Transform farmRoot,depotRoot,fieldCargoRoot,depotCargoRoot;
        Quaternion boardLook;bool boardViewing;
        public void BuildDepot(){
            farmRoot=siteRoot;fieldCargoRoot=cargoRoot;depotRoot=new GameObject("Company depot").transform;depotRoot.SetParent(transform);siteRoot=depotRoot;
            Box("Depot ground",new Vector3(0,-.15f,2),new Vector3(70,.3f,65),dirt);
            Box("Company floor",new Vector3(0,.03f,0),new Vector3(12,.06f,10),wood);
            foreach(float side in new[]{-1f,1f}){Box("Office side wall",new Vector3(side*6,1.7f,0),new Vector3(.2f,3.4f,10),plaster);foreach(float z in new[]{-5f,5f})Box("Office wall by door",new Vector3(side*3.8f,1.7f,z),new Vector3(4.4f,3.4f,.2f),plaster);}
            Box("Office roof",new Vector3(0,3.6f,0),new Vector3(12.6f,.2f,10.6f),wood);
            var board=Box("Large contract blackboard",new Vector3(3.6f,1.9f,4.75f),new Vector3(4,.2f,.18f),wood);board.transform.localScale=new Vector3(4,2.5f,.18f);board.AddComponent<DemoTarget>().kind="board";
            Box("Chalkboard",new Vector3(3.6f,1.9f,4.63f),new Vector3(3.7f,2.2f,.05f),teal,null,false);
            foreach(float x in new[]{2.6f,4.5f})for(int r=0;r<3;r++){Box("Pinned contract card",new Vector3(x,2.6f-r*.65f,4.57f),new Vector3(1.45f,.5f,.02f),cream,null,false);Shape("Pin",PrimitiveType.Sphere,new Vector3(x,2.85f-r*.65f,4.53f),Vector3.one*.065f,amber);}
            Sign("委托看板  E",new Vector3(3.6f,3.18f,4.5f),.09f);
            Sign("地图",new Vector3(2.6f,2.92f,4.5f),.07f);Sign("委托",new Vector3(4.5f,2.92f,4.5f),.07f);
            Sign("榆树街住宅\n已开放",new Vector3(2.6f,2.6f,4.5f),.055f);
            Sign("其他地图\n未开放",new Vector3(2.6f,1.95f,4.5f),.055f);
            Sign("整备后确认出发",new Vector3(2.6f,1.3f,4.5f),.048f);
            Sign("回家前的清理\n150元 / 120XP",new Vector3(4.5f,2.6f,4.5f),.048f);
            Sign("储物柜保卫\n未开放",new Vector3(4.5f,1.95f,4.5f),.048f);
            Sign("邻居已经睡了\n未开放",new Vector3(4.5f,1.3f,4.5f),.048f);
            Box("Planning desk",new Vector3(-3.2f,.7f,2.6f),new Vector3(3,1.4f,1.2f),wood);
            Sign("后门 → 采购 / 捐赠 / 收购",new Vector3(0,2.8f,4.8f),.07f);
            for(int i=0;i<12;i++)Box("Rear footpath",new Vector3(0,.015f,5.5f+i*.65f),new Vector3(2.2f,.03f,.55f),cream,null,false);
            for(int i=0;i<9;i++)foreach(float side in new[]{-1f,1f})Box("Branch stepping stone",new Vector3(side*(i+1)*1.5f,.02f,11.5f+i*.5f),new Vector3(1.1f,.04f,.65f),cream,null,false);
            Vendor(0,false);Vendor(-14,false,true);Vendor(14,true,true);
            BuildTruck();depotCargoRoot=cargoRoot;siteRoot=farmRoot;cargoRoot=fieldCargoRoot;depotRoot.gameObject.SetActive(false);
        }
        void Sign(string value,Vector3 p,float scale){var g=new GameObject(value);g.transform.SetParent(siteRoot);g.transform.localPosition=p;g.transform.localRotation=Quaternion.identity;var t=g.AddComponent<TextMesh>();t.text=value;t.characterSize=scale*.2f;t.fontSize=40;t.anchor=TextAnchor.MiddleCenter;t.color=Color.white;}
        void Vendor(float x,bool shady,bool cage=false){
            Material m=shady?dark:wood;Box("Vendor roof",new Vector3(x,3.15f,17),new Vector3(9,.15f,6),m);
            foreach(float dx in new[]{-4.3f,4.3f})foreach(float z in new[]{14.5f,19.5f})Box("Shelter post",new Vector3(x+dx,1.55f,z),new Vector3(.2f,3.1f,.2f),wood);
            Box("Vendor back wall",new Vector3(x,1.5f,20),new Vector3(9,3,.18f),shady?dark:plaster);
            if(!cage){var counter=Box("Equipment counter",new Vector3(x,.55f,14.8f),new Vector3(6,1.1f,1),wood);counter.AddComponent<DemoTarget>().kind="shop";Box("Counter receiving platform",new Vector3(-.8f,.96f,13.8f),new Vector3(4,.12f,1),metal);Sign("器械采购  E",new Vector3(x,2.65f,14.6f),.1f);for(int r=0;r<3;r++)for(int i=0;i<7;i++)Box("Stock supplies",new Vector3(x-3+i,.5f+r*.7f,19.5f),new Vector3(.55f,.45f,.3f),i%2==0?teal:amber,null,false);}
            else {Box("Transfer cage platform",new Vector3(x,.08f,17),new Vector3(7,.16f,5),metal);
                for(int i=0;i<20;i++)foreach(float z in new[]{14.7f,19.4f})Box("Cage bars",new Vector3(x-3.3f+i*.35f,1.3f,z),new Vector3(.035f,2.4f,.035f),shady?metal:cream);
                foreach(float dx in new[]{-3.4f,3.4f})for(int i=0;i<14;i++)Box("Cage side bars",new Vector3(x+dx,1.3f,14.7f+i*.35f),new Vector3(.035f,2.4f,.035f),metal);
                Box("Cage roof",new Vector3(x,2.6f,17),new Vector3(7,.08f,5),metal);
                var terminal=Box("Transfer terminal",new Vector3(x+3.8f,.85f,13.8f),new Vector3(.8f,1.7f,.7f),shady?dark:teal);terminal.AddComponent<DemoTarget>().kind=shady?"sale":"donate";
                Sign(shady?"动物收购 · 出售 ¥":"动物园捐赠 · 经验 XP",new Vector3(x,2.95f,14.6f),.09f);
                for(int i=0;i<9;i++){if(shady)Box("Discarded crates",new Vector3(x-4.8f+i%3*.4f,.2f+i/3*.3f,16+i*.18f),Vector3.one*.5f,wood,null,false);else{Shape("Flowers",PrimitiveType.Sphere,new Vector3(x-4.4f,.35f,14+i*.6f),new Vector3(.45f,.25f,.45f),i%2==0?pink:cream);}}
            }
            var lamp=new GameObject("Vendor light");lamp.transform.SetParent(siteRoot);lamp.transform.position=new Vector3(x,2.8f,16);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=9;light.color=shady?new Color(.85f,.65f,.3f):new Color(1,.92f,.72f);lamp.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>().SetIntensity(shady?900:2500,UnityEngine.Rendering.LightUnit.Lumen);
        }
        public void SetDepot(bool value){if(!depotRoot)BuildDepot();depotRoot.gameObject.SetActive(value);if(value){foreach(var s in jobSites.Values)s.root.gameObject.SetActive(false);farmRoot.gameObject.SetActive(false);siteRoot=depotRoot;cargoRoot=depotCargoRoot;}else ActivateJobMap(game.save.job==null?0:game.save.job.mapIndex);}
        public void RestoreDepot(){
            foreach(Transform child in dynamicRoot){child.gameObject.SetActive(false);Destroy(child.gameObject);}animals.Clear();devices.Clear();pests.Clear();
            foreach(var item in game.save.job.looseItems)CreateLooseItem(item);RefreshDepotCargo();
            controller.enabled=false;controller.transform.position=game.save.job.player;controller.transform.rotation=Quaternion.Euler(0,game.save.job.yaw,0);controller.enabled=true;
            cam.transform.localPosition=Vector3.up*1.6f;cam.transform.localRotation=Quaternion.identity;ApplyNight(false);torch.enabled=false;
        }
        public void RefreshDepotCargo(){
            foreach(Transform child in cargoRoot){child.gameObject.SetActive(false);Destroy(child.gameObject);}int truck=0,sale=0,donate=0,large=0;float saleCursor=0,donateCursor=0;
            foreach(var a in game.save.cargo){if(a.terminal||game.save.job.cargoIds.Contains(a.id))continue;bool s=game.save.saleHolding.Contains(a.id),d=game.save.donationHolding.Contains(a.id);
                if(a.species!="M01"&&a.species!="M02"&&a.species!="C01"){
                    var definition=game.config.content.species.First(v=>v.id==a.species);Vector3 position;
                    if(s||d){float cursor=s?saleCursor:donateCursor;float width=Mathf.Max(.5f,definition.lengthM*.6f);position=new Vector3((s?9:-19)+cursor+width*.5f,.18f,16+(Mathf.Floor(cursor/6))*1.7f);if(s)saleCursor+=width+.25f;else donateCursor+=width+.25f;position.x=(s?9:-19)+Mathf.Repeat(cursor,6)+width*.5f;}
                    else position=AdditionalCargoPoint(a,ref truck,ref large);
                    var visible=BuildCargoAnimal(a,position,true);visible.name="Depot animal "+a.id;continue;
                }
                Vector3 p=s?new Vector3(12+(sale++%5),.18f,16):d?new Vector3(-16+(donate++%5),.18f,16):VanPoint+new Vector3(-1.1f,1.05f,-.7f+(truck++%5)*.6f);
                var root=new GameObject("Depot animal "+a.id);root.transform.SetParent(cargoRoot);root.transform.position=p;
                var body=Shape("Visible animal",PrimitiveType.Sphere,Vector3.up*.12f,new Vector3(.18f,.14f,.28f),fur,root.transform);Shape("Head",PrimitiveType.Sphere,new Vector3(0,.16f,.17f),Vector3.one*.11f,fur,root.transform);for(int i=0;i<5;i++)Shape("Tail",PrimitiveType.Sphere,new Vector3(0,.08f,-.16f-i*.04f),new Vector3(.02f,.02f,.06f),pink,root.transform);
                if(a.dead)body.transform.localRotation=Quaternion.Euler(0,0,85);var c=root.AddComponent<BoxCollider>();c.size=new Vector3(.35f,.4f,.6f);root.AddComponent<DemoTarget>().kind="depotAnimal";root.AddComponent<DepotAnimal>().animalId=a.id;
            }
        }
        int boardSection;
        public void StartBoardView(int step){boardViewing=true;boardSection=step;}
        public void EndBoardView(){boardViewing=false;cam.transform.localPosition=Vector3.up*1.6f;cam.transform.localRotation=Quaternion.identity;}
        void LateUpdate(){if(!boardViewing)return;Vector3 p=boardSection==1?new Vector3(4.35f,1.9f,2.7f):new Vector3(2.8f,1.9f,2.7f);cam.transform.position=Vector3.Lerp(cam.transform.position,p,Time.unscaledDeltaTime*5);cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,Quaternion.LookRotation(new Vector3(boardSection==1?4.35f:2.8f,1.9f,4.7f)-cam.transform.position),Time.unscaledDeltaTime*5);}
    }
}

