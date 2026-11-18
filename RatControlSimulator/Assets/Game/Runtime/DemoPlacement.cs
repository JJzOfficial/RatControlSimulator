using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        Vector3 DeviceSize(Kit? kit)=>kit==Kit.LargeCage?new Vector3(2.064f,1.806f,3.225f):kit==Kit.MediumCage?new Vector3(1.056f,.924f,1.65f):kit==Kit.Camera?new Vector3(.3f,1.4f,.3f):kit==Kit.BaitStation?new Vector3(.6f,.2f,.4f):kit==Kit.SnapTrap?new Vector3(.22f,.14f,.34f):new Vector3(.48f,.42f,.75f);
        void ValidateAdditionalPlacement(){
            if(save.job.contractIndex==0)return;var size=DeviceSize(CurrentKit);var rotation=Quaternion.Euler(0,deployYaw,0);
            if(Physics.CheckBox(place+Vector3.up*(size.y*.5f+.04f),size*.48f,rotation,1<<9))canPlace=false;
            float scale=CurrentKit==Kit.LargeCage?4.3f:CurrentKit==Kit.MediumCage?2.2f:1;
            var mouth=place+rotation*new Vector3(0,.09f*scale,.55f*scale);
            if(IsCage(CurrentKit)&&(Physics.CheckSphere(mouth+Vector3.up*.35f,.25f,1<<9)||world.Route(world.controller.transform.position,mouth).Count==0))canPlace=false;
            foreach(var device in world.devices.Values)if(Vector3.Distance(place,device.transform.position)<(size.x+DeviceSize(DeviceKit(device.record)).x)*.6f)canPlace=false;
        }
    }
}
