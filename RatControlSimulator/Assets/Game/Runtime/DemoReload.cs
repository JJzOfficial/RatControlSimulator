using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Riverbend {
    public partial class DemoGame {
        string reloadItem,lastSpentUnit;float reloadElapsed,reloadDuration,bowCharge,weaponNoise;
        Kit AmmoFor(Kit k)=>k==Kit.Blowpipe?Kit.Darts:k==Kit.Bow?Kit.Arrows:k==Kit.Airgun?Kit.Pellets:Kit.Cartridges;
        int MagazineCapacity(Kit k)=>k==Kit.Blowpipe?3:k==Kit.Airgun?5:1;
        MagazineRecord Magazine(string id){if(save.magazines==null)save.magazines=new System.Collections.Generic.List<MagazineRecord>();var m=save.magazines.FirstOrDefault(m=>m.itemId==id);if(m==null){m=new MagazineRecord{itemId=id};save.magazines.Add(m);}return m;}
        void BeginReload(){
            if(!IsRanged(CurrentKit)||SelectedCargo||reloadItem!=null)return;var k=CurrentKit.Value;var j=save.job;var m=Magazine(j.itemIds[selected]);
            if(m.rounds>=MagazineCapacity(k))return;if(!j.kit.Contains((int)AmmoFor(k))){Notify(L("背包中没有匹配弹药","No matching ammunition in backpack"));return;}
            reloadItem=m.itemId;reloadElapsed=0;reloadDuration=(k==Kit.Blowpipe?1.8f:k==Kit.Bow?1.2f:k==Kit.Airgun?2:2.5f)*SkillValue("reloadMultiplier",1);ResetGestures();
        }
        bool AdvanceReload(float dt){
            if(reloadItem==null)return false;var j=save.job;
            if(selected<0||selected>=j.itemIds.Count||j.itemIds[selected]!=reloadItem||!IsRanged(CurrentKit)){reloadItem=null;reloadElapsed=0;return false;}
            reloadElapsed+=dt;if(reloadElapsed<reloadDuration)return true;
            var k=CurrentKit.Value;var m=Magazine(reloadItem);int slot=j.kit.IndexOf((int)AmmoFor(k));
            if(slot>=0){int amount=Mathf.Min(MagazineCapacity(k)-m.rounds,j.quantities[slot]);if(m.unitIds==null)m.unitIds=new System.Collections.Generic.List<string>();for(int i=0;i<amount;i++)m.unitIds.Add(j.quantities[slot]==1&&j.itemIds[slot].Contains("/unit/")?j.itemIds[slot]:j.itemIds[slot]+"/unit/"+(j.quantities[slot]-1-i));m.rounds+=amount;j.quantities[slot]-=amount;if(j.quantities[slot]<=0){string id=reloadItem;RemoveSlot(slot);Select(j.itemIds.IndexOf(id));}}
            reloadItem=null;reloadElapsed=0;Persist();Sound("ui");return true;
        }
        bool RangedActionInput(Keyboard kb,Mouse mouse){
            if(kb.rKey.wasPressedThisFrame)BeginReload();if(AdvanceReload(Time.deltaTime))return true;
            if(CurrentKit!=Kit.Bow){bowCharge=0;return false;}
            if(mouse.leftButton.isPressed){bowCharge=Mathf.Min(.8f,bowCharge+Time.deltaTime);return true;}
            if(mouse.leftButton.wasReleasedThisFrame){if(bowCharge>=.8f&&attackCooldown<=0)UseAdditionalTool();else if(bowCharge>0)Notify(L("拉弓未满，未消耗箭矢","Draw incomplete; arrow retained"));bowCharge=0;return true;}return true;
        }
        bool SpendRound(Kit k){var m=Magazine(save.job.itemIds[selected]);if(m.rounds<=0){Notify(L("空膛，按R装填","Empty; press R to reload"));return false;}lastSpentUnit=m.unitIds!=null&&m.unitIds.Count>0?m.unitIds[0]:m.itemId+"/legacy-unit/"+Guid.NewGuid().ToString("N");if(m.unitIds!=null&&m.unitIds.Count>0)m.unitIds.RemoveAt(0);m.rounds--;return true;}
    }
}
