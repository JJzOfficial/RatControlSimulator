using System;
using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        int growthPage,bagPage;
        void BackpackUI(){
            var j=save.job;int pages=Mathf.Max(1,Mathf.CeilToInt(j.kit.Count/10f));bagPage=Mathf.Clamp(bagPage,0,pages-1);
            for(int row=0;row<10;row++){int i=bagPage*10+row;if(i>=j.kit.Count)break;float x=335+(row/5)*445,y=235+(row%5)*62;if(Button(x,y,430,52,(i+1)+" · "+KitName((Kit)j.kit[i])+(SlotCondition(i)>=0?" · "+Mathf.CeilToInt(SlotCondition(i)*100)+"%":"")+(j.quantities[i]>1?" ×"+j.quantities[i]:""))){Select(i);Close();}}
            Label(335,575,880,45,L("容量 ","Capacity ")+BackpackUsed()+"/"+BackpackCapacity+" · "+L("实物 ","Instances ")+j.kit.Count+" · "+(bagPage+1)+"/"+pages,heading);
            if(Button(335,640,200,40,"←",bagPage>0))bagPage--;if(Button(550,640,200,40,"→",bagPage<pages-1))bagPage++;
            Label(335,705,880,65,L("点击选择，滚轮轮换全部实物；G放下，右键使用补给。中笼2格；空小笼、夹和补给可按技能分类收纳。","Click to select; wheel cycles every instance. G drops, RMB uses supplies. Medium cages use two slots; stack skills group small empty cages, traps and supplies."),small);
        }
        public float SenseRadius=>SkillValue("qRadius",config.sense.radius);
        void NormalizeSkills(){if(save.skillRanks==null||save.skillRanks.Length!=26){var old=save.skillRanks;save.skillRanks=new int[26];if(old!=null)Array.Copy(old,save.skillRanks,Math.Min(old.Length,26));}}
        public float SkillValue(string effect,float baseline){NormalizeSkills();int i=Array.FindIndex(config.content.skills,s=>s.effectKey==effect);return i>=0&&save.skillRanks[i]>0?config.content.skills[i].rankFinalValues[Mathf.Clamp(save.skillRanks[i]-1,0,1)]:baseline;}
        int SkillPoints {get{NormalizeSkills();return config.content.levels.Where(l=>l.level<=PlayerLevel).Sum(l=>l.skillPointsGranted)-save.skillRanks.Sum();}}
        bool LearnSkill(int index){NormalizeSkills();var s=config.content.skills[index];int rank=save.skillRanks[index];if(!save.atDepot||rank>=2||SkillPoints<s.rankPointCosts[rank]||PlayerLevel<s.levelGates[rank])return false;
            int branch=config.content.skills.Select((entry,i)=>entry.branch==s.branch?save.skillRanks[i]:0).Sum();if(branch<s.branchPointsRequired)return false;save.skillRanks[index]++;Persist();return true;}
        int BackpackCapacity=>Mathf.Min(10,4+config.content.levels.Where(l=>l.level<=PlayerLevel).Sum(l=>l.extraSlotsGranted)+(int)SkillValue("extraSlots",0));
        bool Stackable(Kit k)=>IsSupply(k)||(int)k>=(int)Kit.Darts&&(int)k<=(int)Kit.Cartridges||k==Kit.BodyBag||k==Kit.SnapTrap||k==Kit.Cage;
        int BackpackUsed(Kit? extra=null){var j=save.job;int used=0,stack=(int)SkillValue("smallStackLimit",1);var grouped=new System.Collections.Generic.Dictionary<int,int>();
            for(int i=0;i<j.kit.Count;i++){var k=(Kit)j.kit[i];bool occupied=i<j.cargoIds.Count&&!string.IsNullOrEmpty(j.cargoIds[i]);if(Stackable(k)&&!occupied){if(!grouped.ContainsKey((int)k))grouped[(int)k]=0;grouped[(int)k]++;}else used+=k==Kit.MediumCage||k==Kit.LargeCage?2:1;}
            if(extra.HasValue){var k=extra.Value;if(Stackable(k)){if(!grouped.ContainsKey((int)k))grouped[(int)k]=0;grouped[(int)k]++;}else used+=k==Kit.MediumCage||k==Kit.LargeCage?2:1;}
            return used+grouped.Values.Sum(n=>Mathf.CeilToInt((float)n/stack));}
        bool HasBackpackSpace(Kit k)=>BackpackUsed(k)<=BackpackCapacity;
        bool CanOccupySlot(int slot){var j=save.job;var k=(Kit)j.kit[slot];if(!Stackable(k))return true;int stack=(int)SkillValue("smallStackLimit",1),empty=Enumerable.Range(0,j.kit.Count).Count(i=>j.kit[i]==(int)k&&string.IsNullOrEmpty(j.cargoIds[i]));int delta=1+Mathf.CeilToInt((float)(empty-1)/stack)-Mathf.CeilToInt((float)empty/stack);return BackpackUsed()+delta<=BackpackCapacity;}
        void GrowthUI(){
            if(Button(1040,173,170,35,L("升级服务","Upgrades")))panel="services";
            if(Button(335,733,425,35,L("伙伴购买与出勤整备","Companion purchase / assignment")))panel="partners";if(Button(785,733,425,35,L("笼具内胆整备","Cage liner configuration")))panel="containers";
            Label(335,180,880,35,L("等级 ","Level ")+PlayerLevel+" · "+L("可用技能点 ","Available skill points ")+SkillPoints+" · "+L("背包 ","Backpack ")+BackpackUsed()+"/"+BackpackCapacity,heading);
            for(int row=0;row<7;row++){int index=growthPage*7+row;if(index>=config.content.skills.Length)break;var s=config.content.skills[index];int rank=save.skillRanks[index];float y=235+row*61;
                Label(335,y,630,54,s.branch+" / "+s.zh+"  "+rank+"/2 · "+s.rankFinalValues[0]+" → "+s.rankFinalValues[1],small);
                if(Button(1015,y,195,44,L("学习","Learn"),rank<2&&SkillPoints>0))if(!LearnSkill(index))Notify(L("等级或本分支投入点数不足","Level or branch prerequisite not met"));}
            if(Button(335,684,120,38,"←",growthPage>0))growthPage--;if(Button(470,684,120,38,"→",growthPage<3))growthPage++;
            if(Button(650,684,260,38,L("免费重置","Reset skills"),save.atDepot)){var old=save.skillRanks;save.skillRanks=new int[26];if(BackpackUsed()>BackpackCapacity){save.skillRanks=old;Notify(L("先腾出背包空间再重置","Free backpack space before resetting"));}else{foreach(var partner in save.partners.Where(p=>p.active).Skip(1)){partner.active=false;partner.target=null;}Persist();}}
            if(Button(940,684,270,38,L("充满设备电量","Recharge devices"),save.atDepot)){save.droneBattery=120;foreach(var item in save.job.looseItems)if(item.kit==(int)Kit.Camera||item.kit==(int)Kit.Swatter||item.kit==(int)Kit.Drone)item.condition=100;for(int i=0;i<save.job.kit.Count;i++)if(save.job.kit[i]==(int)Kit.Camera||save.job.kit[i]==(int)Kit.Swatter||save.job.kit[i]==(int)Kit.Drone)save.job.conditions[i]=100;Persist();Notify(L("设备充电完成","Devices recharged"));}
        }
    }
}
