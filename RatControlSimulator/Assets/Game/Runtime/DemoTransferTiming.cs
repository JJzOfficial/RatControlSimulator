using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        bool TimedTransfer(DemoTarget t)=>t&&save.job.contractIndex>0&&(t.kind=="item"||t.kind=="cargo"&&SelectedCargo||t.kind=="animal"&&t.GetComponent<DemoAnimal>().record.dead);
        float TransferDuration(DemoTarget t)=>Mathf.Max(.25f,t.kind=="cargo"?1.2f*SkillValue("cageTransferMultiplier",1):.8f*SkillValue("pickupTransferBagMultiplier",1));
    }
}
