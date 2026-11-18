#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        void CheckCargoDisplayAndTransfer(List<string> log){
            save=new SaveRecord{cash=100000,xp=15000,starterGranted=true};selected=-1;ShowCompany();var j=save.job;
            AddBehaviorItem(Kit.Cage);for(int i=0;i<5;i++)AddBehaviorItem(Kit.Baton);
            var a=new AnimalRecord{id="depot-fit",species="M03",contained=true,loaded=true};save.cargo.Add(a);Require(BackpackUsed()==6,"cargo test has a full bag");PickDepotAnimal(a.id);Require(j.cargoIds.Contains(a.id),"an unstacked empty cage can become occupied without an extra slot");
            j.cargoIds[0]=null;j.kit.Clear();j.itemIds.Clear();j.conditions.Clear();j.quantities.Clear();j.cargoIds.Clear();j.baitedSlots.Clear();selected=-1;
            save.skillRanks[System.Array.FindIndex(config.content.skills,s=>s.effectKey=="smallStackLimit")]=1;
            AddBehaviorItem(Kit.Cage);AddBehaviorItem(Kit.Cage);for(int i=0;i<5;i++)AddBehaviorItem(Kit.Baton);Require(BackpackUsed()==6,"stacked empty cage test has a full bag");PickDepotAnimal(a.id);Require(!j.cargoIds.Contains(a.id),"occupied cage cannot silently overflow a full bag");
            j.kit.Clear();j.itemIds.Clear();j.conditions.Clear();j.quantities.Clear();j.cargoIds.Clear();j.baitedSlots.Clear();selected=-1;save.skillRanks=new int[26];AddBehaviorItem(Kit.MediumCage);a.species="S01";PickDepotAnimal(a.id);Require(!j.cargoIds.Contains(a.id),"depot snake transfer needs this cage's liner");save.snakeLiners.Add(j.itemIds[0]);PickDepotAnimal(a.id);Require(j.cargoIds[0]==a.id,"sealed matching cage accepts snake");j.cargoIds[0]=null;
            save.cargo.Clear();save.cargo.Add(new AnimalRecord{id="depot-boar",species="M16",loaded=true,dead=true,bagged=true});save.cargo.Add(new AnimalRecord{id="depot-fox",species="M14",loaded=true,contained=true});world.RefreshDepotCargo();
            var boar=world.cargoRoot.Find("Depot animal depot-boar");var fox=world.cargoRoot.Find("Depot animal depot-fox");Require(boar&&boar.Find("Loaded sealed remains bag")&&boar.GetComponent<BoxCollider>().size.z>1.5f,"large remains retain a full sized sealed bag");Require(fox&&fox.Find("Body pose/Pointed ear")&&fox.GetComponent<DepotAnimal>().animalId=="depot-fox","company cargo uses the real fox model and stable pickup ID");
            Require(Mathf.Abs(boar.position.x-fox.position.x)>2,"large transport bay is separate from the original small cage");log.Add("PASS depot occupied-cage capacity / snake liner identity / full sized large cargo / species silhouette and pickup identity");
        }
    }
}
#endif
