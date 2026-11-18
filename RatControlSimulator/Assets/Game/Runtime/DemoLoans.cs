using System;
using System.Collections.Generic;
using System.Linq;
namespace Riverbend {
    public partial class DemoGame {
        Kit CatalogKit(string id){
            switch(id){case "E01":return Kit.Spray;case "E03":return Kit.Broom;case "E04":return Kit.Repair;case "E05":return Kit.SnapTrap;case "E06.Small":return Kit.Cage;case "E14":return Kit.Flashlight;case "P01":return Kit.SprayRefill;case "P02":return Kit.RepairPack;case "P07":return Kit.Bait;case "P10":return Kit.CleaningRefill;}
            int index=Array.IndexOf(additionalItemIds,id);if(index<0)throw new InvalidOperationException("Unknown loan item "+id);return (Kit)(index+12);
        }
        void PrepareContractLoans(JobRecord j){
            if(j.contractIndex==0){if(save.runs<=1)return;foreach(var k in new[]{Kit.Spray,Kit.Broom,Kit.Repair,Kit.SnapTrap,Kit.Cage})EnsureLoan(j,k,1);EnsureToolResource(j,Kit.Spray,100,Kit.SprayRefill);EnsureToolResource(j,Kit.Broom,20,Kit.CleaningRefill);EnsureToolResource(j,Kit.Repair,30,Kit.RepairPack);EnsureSupplyUnits(j,Kit.Bait,5);return;}
            var packages=Contract(j).loans;if(packages==null)return;
            foreach(var packageId in packages){
                var package=config.content.loans.First(p=>p.id==packageId);
                foreach(var id in package.items??Array.Empty<string>())if(!string.IsNullOrEmpty(id))EnsureLoan(j,CatalogKit(id),1);
                var choices=(package.selectOne??Array.Empty<string>()).Where(id=>!string.IsNullOrEmpty(id)).Select(CatalogKit).ToArray();
                if(choices.Length>0){var weapon=choices.FirstOrDefault(k=>OwnsForJob(j,k));if(!choices.Any(k=>OwnsForJob(j,k)))weapon=choices[0];EnsureLoan(j,weapon,1);EnsureLoan(j,weapon==Kit.Bow?Kit.Arrows:Kit.Cartridges,1);}
                foreach(var supply in package.supplies??Array.Empty<DemoCatalog.Supply>())EnsureLoan(j,CatalogKit(supply.id),supply.count);
            }
            if(j.contractIndex==3||j.contractIndex==9||j.contractIndex==12){EnsureLoan(j,Kit.Spray,1);EnsureLoan(j,Kit.Broom,1);EnsureLoan(j,Kit.Repair,1);}
            if(j.contractIndex==2)EnsureLoan(j,Kit.Repair,1);
            if(j.contractIndex==4){EnsureLoan(j,Kit.Repair,1);EnsureLoan(j,Kit.Broom,1);}
            if(OwnsForJob(j,Kit.Spray))EnsureToolResource(j,Kit.Spray,100,Kit.SprayRefill);
            if(OwnsForJob(j,Kit.Broom))EnsureToolResource(j,Kit.Broom,j.contractIndex==3?30:20,Kit.CleaningRefill);
            if(OwnsForJob(j,Kit.Repair))EnsureToolResource(j,Kit.Repair,30,Kit.RepairPack);
            if(OwnsForJob(j,Kit.SnapTrap)||OwnsForJob(j,Kit.Cage)||OwnsForJob(j,Kit.MediumCage))EnsureSupplyUnits(j,Kit.Bait,Math.Max(5,Contract(j).RequiredCount));
            foreach(var weapon in new[]{Kit.Bow,Kit.Rifle})if(OwnsForJob(j,weapon))EnsureSupplyUnits(j,AmmoFor(weapon),10);
        }
        int SupplyUnits(JobRecord j,Kit k){int amount=j.looseItems.Concat(j.crateItems).Concat(j.deliveredItems).Where(i=>i.kit==(int)k).Sum(i=>i.quantity);for(int i=0;i<j.kit.Count;i++)if(j.kit[i]==(int)k)amount+=j.quantities[i];return amount;}
        void EnsureSupplyUnits(JobRecord j,Kit k,int required){int missing=required-SupplyUnits(j,k);while(missing>0){var item=NewItem(k);item.quantity=Math.Min(item.quantity,missing);item.position=TruckItemPosition(j.looseItems.Count);j.borrowedItems.Add(item.id);j.looseItems.Add(item);missing-=item.quantity;}}
        void EnsureToolResource(JobRecord j,Kit tool,int needed,Kit supply){float current=j.looseItems.Concat(j.crateItems).Concat(j.deliveredItems).Where(i=>i.kit==(int)tool).Sum(i=>i.condition);for(int i=0;i<j.kit.Count;i++)if(j.kit[i]==(int)tool)current+=j.conditions[i];EnsureSupplyUnits(j,supply,Math.Max(0,needed-(int)current));}
        bool OwnsForJob(JobRecord j,Kit k)=>j.kit.Contains((int)k)||j.looseItems.Any(i=>i.kit==(int)k&&i.quantity>0)||j.crateItems.Any(i=>i.kit==(int)k&&i.quantity>0)||j.deliveredItems.Any(i=>i.kit==(int)k&&i.quantity>0);
        void EnsureLoan(JobRecord j,Kit kit,int count){
            int owned=j.kit.Count(k=>k==(int)kit)+j.looseItems.Count(i=>i.kit==(int)kit&&i.quantity>0)+j.crateItems.Count(i=>i.kit==(int)kit&&i.quantity>0)+j.deliveredItems.Count(i=>i.kit==(int)kit&&i.quantity>0);
            for(int i=owned;i<count;i++){var item=NewItem(kit);if(kit==Kit.Spray||kit==Kit.Broom||kit==Kit.Repair)item.condition=100;item.position=TruckItemPosition(j.looseItems.Count);j.borrowedItems.Add(item.id);j.looseItems.Add(item);}
        }
    }
}
