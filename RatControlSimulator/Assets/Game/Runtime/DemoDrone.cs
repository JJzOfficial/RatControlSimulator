using UnityEngine;
using UnityEngine.InputSystem;
namespace Riverbend {
    public partial class DemoGame {
        bool droneActive;string droneItemId;Transform droneBody;Vector3 droneOrigin;float dronePitch,droneYaw;
        void StartDrone(){
            if(save.job.contractIndex==2){Notify(L("该静默委托禁止操作无人机","Drone operation is forbidden on this silent contract"));FinalizeJob(false,true);return;}
            droneItemId=HeldItemId;save.droneBattery=selected>=0?save.job.conditions[selected]*1.2f:save.droneBattery;if(save.droneBattery<=0){Notify(L("无人机电量耗尽，请回车旁按住R充电","Drone battery empty; hold R near the truck to recharge"));return;}
            droneActive=true;droneOrigin=world.controller.transform.position;droneBody=new GameObject("Piloted survey drone").transform;droneBody.SetParent(world.dynamicRoot);droneBody.position=world.cam.transform.position;
            world.CreateDroneModel(droneBody);droneYaw=world.controller.transform.eulerAngles.y;dronePitch=pitch;world.cam.transform.SetParent(droneBody,false);world.cam.transform.localPosition=new Vector3(0,-.06f,.25f);world.handRoot.gameObject.SetActive(false);
            Notify(L("无人机：WASD移动，空格/蹲下升降，左键识别，右键或Esc返回","Drone: WASD move, Space/Ctrl altitude, LMB identify, RMB/Esc return"));
        }
        void StopDrone(){
            if(!droneActive)return;StoreDroneBattery();droneActive=false;if(world&&world.cam&&world.controller){world.cam.transform.SetParent(world.controller.transform,false);world.cam.transform.localPosition=Vector3.up*1.6f;world.cam.transform.localRotation=Quaternion.Euler(pitch,0,0);world.handRoot.gameObject.SetActive(true);}if(droneBody)Destroy(droneBody.gameObject);UpdateOptics(false);
        }
        void StoreDroneBattery(){if(!droneActive||save.job==null)return;int slot=save.job.itemIds.IndexOf(droneItemId);if(slot>=0)save.job.conditions[slot]=Mathf.Clamp(save.droneBattery/1.2f,0,100);}
        void DroneInput(Keyboard kb,Mouse mouse){
            if(kb.f5Key.wasPressedThisFrame){StoreDroneBattery();Persist();Notify(L("已保存","Saved"));}
            if(kb.escapeKey.wasPressedThisFrame||mouse.rightButton.wasPressedThisFrame){StopDrone();return;}
            if(!Paused){AdvanceDelivery(Time.deltaTime);AdvanceRare(Time.deltaTime);if(!droneActive||save.atDepot)return;}
            if(Paused)return;UpdateOptics(kb.vKey.wasPressedThisFrame);float dt=Time.deltaTime;save.droneBattery=Mathf.Max(0,save.droneBattery-dt/SkillValue("deviceBatteryMultiplier",1));StoreDroneBattery();if(save.droneBattery<=0){StopDrone();Notify(L("无人机电量耗尽","Drone battery depleted"));return;}
            var look=mouse.delta.ReadValue()*sensitivity;droneYaw+=look.x;dronePitch=Mathf.Clamp(dronePitch-look.y,-80,80);droneBody.rotation=Quaternion.Euler(0,droneYaw,0);world.cam.transform.localRotation=Quaternion.Euler(dronePitch,0,0);
            var axis=new Vector3((kb.dKey.isPressed?1:0)-(kb.aKey.isPressed?1:0),(kb.spaceKey.isPressed?1:0)-(kb.leftCtrlKey.isPressed?1:0),(kb.wKey.isPressed?1:0)-(kb.sKey.isPressed?1:0));
            var delta=droneBody.TransformDirection(Vector3.ClampMagnitude(axis,1))*4*dt;
            if(delta.sqrMagnitude>.000001f&&!Physics.SphereCast(droneBody.position,.25f,delta.normalized,out var hit,delta.magnitude,1<<9))droneBody.position+=delta;
            var p=droneBody.position;var offset=p-droneOrigin;if(offset.magnitude>60)p=droneOrigin+offset.normalized*60;p.y=Mathf.Clamp(p.y,.8f,droneOrigin.y+16);var bounds=world.PlayBounds;p.x=Mathf.Clamp(p.x,bounds.xMin+.4f,bounds.xMax-.4f);p.z=Mathf.Clamp(p.z,bounds.yMin+.4f,bounds.yMax-.4f);droneBody.position=p;
            ObserveThroughOptics(mouse.leftButton.isPressed,80,dt);
        }
    }
    public partial class DemoWorld {public void CreateDroneModel(Transform root){BuildAdditionalEquipment(Kit.Drone,root,false);}}
}
