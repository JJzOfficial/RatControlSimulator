using System.Linq;
using System.Collections.Generic;
namespace Riverbend {
    public partial class DemoGame {
        void ContainerSetupUI(){
            Label(335,185,880,65,L("小笼封闭蛇内胆 · 同件配置，不增加物品","Small cage sealed snake liner · configuration of the same item"),heading);
            var j=save.job;var ids=new List<string>();for(int i=0;i<j.kit.Count;i++)if(j.kit[i]==(int)Kit.Cage)ids.Add(j.itemIds[i]);ids.AddRange(j.looseItems.Where(i=>i.kit==(int)Kit.Cage).Select(i=>i.id));ids.AddRange(save.depotItems.Where(i=>i.kit==(int)Kit.Cage).Select(i=>i.id));
            for(int i=0;i<ids.Count&&i<7;i++){string id=ids[i];bool fitted=save.snakeLiners.Contains(id);if(Button(335,270+i*55,875,45,L("小笼 ","Small cage ")+(i+1)+" · "+(fitted?L("内胆已装：切换为普通","Liner fitted: switch to ordinary"):L("安装封闭蛇内胆","Fit sealed snake liner")))){if(fitted)save.snakeLiners.Remove(id);else save.snakeLiners.Add(id);Persist();}}
            if(ids.Count==0)Label(335,270,880,70,L("先把小笼带回公司或从柜台领取","Bring a small cage back or collect one from the counter"));
            if(Button(335,733,875,35,L("返回成长整备","Back to growth setup")))panel="growth";
        }
        void BorrowOptionalClueKit(){
            var j=save.job;if(j.contractIndex==0)return;
            if(j.animals.Any(a=>a.species=="S01")||Contract(j).targetBudget.Any(b=>b.speciesId=="S01")){EnsureLoan(j,Kit.ControlPole,1);EnsureLoan(j,Kit.Cage,1);string cageId=j.kit.Contains((int)Kit.Cage)?j.itemIds[j.kit.IndexOf((int)Kit.Cage)]:j.looseItems.First(i=>i.kit==(int)Kit.Cage).id;if(!save.snakeLiners.Contains(cageId))save.snakeLiners.Add(cageId);}
            if(j.rarePlans.Any(p=>p.species=="C07"&&p.willSpawn)){EnsureLoan(j,Kit.LargeCage,1);EnsureLoan(j,Kit.Bait,2);}
            Persist();world.RestoreJob();Notify(L("本局线索所需借物已放在车上；结算归还","Clue loan equipment placed on the truck; returned at settlement"));
        }
    }
}
