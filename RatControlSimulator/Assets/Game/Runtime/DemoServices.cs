using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        int servicePage,opticsMode;bool opticsLit;
        string HeldItemId=>selected>=0&&selected<save.job.itemIds.Count?save.job.itemIds[selected]:null;
        bool HasService(string id)=>save.upgrades.Contains(id)||save.installations.Any(i=>i.serviceId==id&&i.itemId==HeldItemId);
        float ServiceValue(string target,float baseline){foreach(var s in config.content.services.Where(s=>s.target==target&&HasService(s.id)))if(float.TryParse(s.finalValue,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float value))baseline=target.EndsWith("Multiplier")?Mathf.Min(baseline,value):Mathf.Max(baseline,value);return baseline;}
        string ServiceTool(string id){var s=config.content.services.First(s=>s.id==id);if(id.StartsWith("V."))return null;var k=CatalogKit(s.target.Split('.')[0]);if(CurrentKit==k)return HeldItemId;var item=save.job.looseItems.Concat(save.depotItems).FirstOrDefault(i=>i.kit==(int)k);if(item!=null)return item.id;int slot=save.job.kit.IndexOf((int)k);return slot<0?null:save.job.itemIds[slot];}
        string ServicePrerequisite(string id){switch(id){case "V.Ordinary8":return "V.Ordinary6";case "V.Ordinary12":return "V.Ordinary8";case "V.Large3":return "V.Large2";case "U.E15.Thermal":return "U.E15.NV";case "U.E16.Thermal":return "U.E16.NV";default:return null;}}
        bool InstalledFor(string service,string item)=>item==null?save.upgrades.Contains(service):save.installations.Any(i=>i.itemId==item&&i.serviceId==service);
        bool BuyService(string id){var s=config.content.services.First(s=>s.id==id);string item=ServiceTool(id),prerequisite=ServicePrerequisite(id);if(!save.atDepot||!id.StartsWith("V.")&&item==null||InstalledFor(id,item)||PlayerLevel<s.unlockLevel||save.cash<s.price||prerequisite!=null&&!InstalledFor(prerequisite,item)){Notify(L("需要对应实物、前级升级、等级和资金","Requires the equipment instance, previous upgrade, level and cash"));return false;}
            SaveToolState();var candidate=JsonUtility.FromJson<SaveRecord>(JsonUtility.ToJson(save));candidate.cash-=s.price;if(item==null)candidate.upgrades.Add(id);else candidate.installations.Add(new InstalledService{itemId=item,serviceId=id});try{DemoSave.Write(candidate);}catch(System.Exception e){Notify(L("升级保存失败，未扣款：","Upgrade save failed; no charge: ")+e.Message);return false;}save=candidate;return true;}
        void ServicesUI(){
            Label(335,185,880,45,L("车辆与器械升级 · 永久服务","Vehicle / equipment upgrades · permanent services"),heading);
            for(int row=0;row<6;row++){int index=servicePage*6+row;if(index>=config.content.services.Length)break;var s=config.content.services[index];bool installed=InstalledFor(s.id,ServiceTool(s.id));float y=250+row*68;Label(335,y,580,55,s.zh+" · Lv"+s.unlockLevel+" · ¥"+s.price,small);if(Button(950,y,260,45,installed?L("已安装","Installed"):L("购买安装","Purchase / install"),!installed&&save.cash>=s.price&&PlayerLevel>=s.unlockLevel)){BuyService(s.id);}}
            if(Button(335,733,200,35,"←",servicePage>0))servicePage--;if(Button(550,733,200,35,"→",servicePage<1))servicePage++;if(Button(800,733,410,35,L("返回整备","Back to setup")))panel="growth";
        }
        void UpdateOptics(bool toggle){
            bool usingScope=CurrentKit==Kit.Binoculars&&aiming||droneActive;string prefix=droneActive?"U.E16.":"U.E15.";
            if(toggle&&usingScope){int next=(opticsMode+1)%3;for(int i=0;i<3;i++){if(next==0||HasService(prefix+(next==1?"NV":"Thermal")))break;next=(next+1)%3;}opticsMode=next;}
            bool lit=usingScope&&opticsMode>0&&HasService(prefix+(opticsMode==1?"NV":"Thermal"));if(lit!=opticsLit){opticsLit=lit;world.SetOpticalExposure(lit);}
        }
        void OpticsOverlay(){
            if(!opticsLit)return;Label(600,110,650,35,(opticsMode==1?L("夜视","NIGHT VISION"):L("热像","THERMAL"))+" · V "+L("切换","cycle"),heading);
            if(opticsMode==1){RectFill(new Rect(0,0,1600,900),new Color(.1f,.4f,.15f,.08f));return;}
            foreach(var a in world.animals.Values){if(a.record.terminal||a.record.loaded)continue;var position=a.transform.position+Vector3.up*.3f;var p=world.cam.WorldToViewportPoint(position);if(p.z<=0||p.x<0||p.x>1||p.y<0||p.y>1||!world.ClearLine(world.cam.transform.position,position))continue;float x=p.x*1600,y=(1-p.y)*900;RectFill(new Rect(x-8,y-12,16,24),a.record.dead?new Color(.4f,.4f,.3f,.5f):new Color(1,.45f,.1f,.7f));}
        }
    }
    public partial class DemoWorld {
        public void SetOpticalExposure(bool active){var volume=GetComponentsInChildren<UnityEngine.Rendering.Volume>().FirstOrDefault(v=>v.isGlobal&&v.profile&&v.profile.components.Any(c=>c is UnityEngine.Rendering.HighDefinition.Exposure));if(volume&&volume.profile.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(out var exposure))exposure.fixedExposure.Override(active?4.5f:game.save.job.stage==2&&!game.save.atDepot?7.2f:10.7f);}
    }
}
