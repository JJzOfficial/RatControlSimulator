using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        bool SpecializedSource(int index)=>save.job.contractIndex==3&&index<3||save.job.contractIndex==12&&index<2||save.job.contractIndex==2&&index==0||save.job.contractIndex==4&&index==0;
        Kit SourceTool(int index)=>save.job.contractIndex==12||save.job.contractIndex==2||index==1?Kit.Repair:Kit.Broom;
        bool SourceInspected(int index)=>save.job.sourceInspected.Contains(index);
        string SourceDescription(int index,out float duration,out string reject){duration=0;reject="";if(save.job.work[index]>=3)return WorkName(index)+L(" · 已处理"," · treated");
            if(!SourceInspected(index))return WorkName(index)+L(" · 长按Q确认受害范围，再处理源头"," · hold Q to inspect the affected source first");
            if(save.job.contractIndex==4&&save.job.work[2]<1){reject=L("先修复漏水点，再清理潮湿纸料","Repair the leak before treating damp paper");return WorkName(index)+" · "+reject;}
            duration=3*SkillValue("repairCleanMultiplier",1);var tool=SourceTool(index);if(CurrentKit!=tool)reject=L("需要：","Requires: ")+KitName(tool);else if((tool==Kit.Repair?save.job.repairs:save.job.cleaning)<10)reject=L("工具余量不足，需要补充","Refill the depleted tool");if(reject!="")duration=0;
            return WorkName(index)+L(" · 长按E隔离 / 修整受害材料（保留调查样本）"," · hold E to isolate / trim affected material (retain a sample)");
        }
        void CompleteSpecializedSource(int index){var j=save.job;var tool=SourceTool(index);if(j.work[index]>=3||!SourceInspected(index)||CurrentKit!=tool||(tool==Kit.Repair?j.repairs:j.cleaning)<10)return;
            if(j.contractIndex==4&&j.work[2]<1)return;
            if(tool==Kit.Repair)j.repairs-=10;else j.cleaning-=10;foreach(var pest in j.pests.Where(p=>p.source==index&&(j.contractIndex!=12||p.species=="I12"))){pest.dead=true;pest.cleaned=true;}j.work[index]=3;world.workTargets[index].Refresh();SaveToolState();Persist();Notify(L("受害源已隔离处理；样本可长按Q调查","Affected source treated; hold Q to investigate the retained sample"));
        }
    }
}
