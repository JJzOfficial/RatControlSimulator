using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        bool CageFits(Kit kit,string species){var d=config.content.species.First(s=>s.id==species);return IsCage(kit)&&(d.size=="Small"||d.size=="Medium"&&kit!=Kit.Cage||d.size=="Large"&&kit==Kit.LargeCage);}
        bool ToolCanControl(DemoAnimal actor,Kit tool){var d=config.content.species.First(s=>s.id==actor.record.species);return tool==Kit.ControlPole?d.family=="Snake"||d.size=="Medium"&&actor.record.sedatedUntil>save.job.nightSeconds:tool==Kit.Net&&(d.size=="Small"&&(actor.record.sedatedUntil>save.job.nightSeconds||actor.NetSlowWindow)||actor.NetDefensiveWindow);}
        void TransferWithTool(DemoAnimal actor){
            var j=save.job;var a=actor.record;if(a.dead||a.contained||SelectedCargo||!CurrentKit.HasValue||!ToolCanControl(actor,CurrentKit.Value))return;var d=config.content.species.First(s=>s.id==a.species);
            int slot=Enumerable.Range(0,j.kit.Count).Where(i=>CageFits((Kit)j.kit[i],a.species)&&string.IsNullOrEmpty(j.cargoIds[i])&&(d.family!="Snake"||save.snakeLiners.Contains(j.itemIds[i]))).DefaultIfEmpty(-1).First();
            if(CurrentKit==Kit.Net)slot=selected;
            if(slot<0||!CanOccupySlot(slot)){Notify(L("需要匹配空容器及占用后的背包空间","Requires a matching empty container and space after occupation"));return;}RecordKitUse(CurrentKit.Value);a.controlMethod=CurrentKit==Kit.Net?"Net":"Pole";a.contained=true;a.deviceId=null;j.cargoIds[slot]=a.id;if(a.species.StartsWith("C")&&!save.capturedRare.Contains(a.species))save.capturedRare.Add(a.species);world.animals.Remove(a.id);Destroy(actor.gameObject);Select(slot);Persist();
        }
        bool RequiresLiveInspection(string species)=>config.content.species.First(s=>s.id==species).investigation=="LiveContained";
        bool InspectionDone(string species)=>save.job.contractIndex==0?save.encyclopedia.Contains(species):save.job.investigatedSpecies.Contains(species);
        bool LargeCargo=>save.job!=null&&save.job.cargoIds.Any(id=>!string.IsNullOrEmpty(id)&&config.content.species.First(s=>s.id==(save.job.animals.FirstOrDefault(a=>a.id==id)??save.cargo.First(a=>a.id==id)).species).size=="Large");
        bool BagCorpse(AnimalRecord animal){
            if(animal.bagged||config.content.species.First(s=>s.id==animal.species).size=="Small")return true;
            int bag=save.job.kit.IndexOf((int)Kit.BodyBag);if(bag<0){Notify(L("中大型遗体需要尸体袋","A body bag is required for medium / large remains"));return false;}
            save.job.quantities[bag]--;if(save.job.quantities[bag]<=0)RemoveSlot(bag);animal.bagged=true;RecordKitUse(Kit.BodyBag);return true;
        }
    }
    public partial class DemoAnimal {public bool NetDefensiveWindow=>(record.species=="C01"&&pause>0)||(record.species=="C02"&&specialActive);public bool NetSlowWindow=>pause>0&&alarm<=0;}
}
