using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Riverbend {
    public partial class DemoGame {
        static readonly string[] additionalItemIds={"E02","E06.Medium","E06.Large","E07","E08","E09","E10","E11","E12","E13","E15","E16","E17","E18","P03","P04","P05","P06","P08","P09"};
        public static bool IsCage(Kit? kit)=>kit==Kit.Cage||kit==Kit.MediumCage||kit==Kit.LargeCage;
        public static Kit DeviceKit(DeviceRecord d)=>d.kit>=12?(Kit)d.kit:d.cage?Kit.Cage:Kit.SnapTrap;
        bool IsRanged(Kit? kit)=>kit==Kit.Blowpipe||kit==Kit.Bow||kit==Kit.Airgun||kit==Kit.Rifle;
        bool IsDeployable(Kit? kit)=>IsCage(kit)||kit==Kit.SnapTrap||kit==Kit.BaitStation||kit==Kit.Camera;
        bool aiming,breathHeld;float breath=8,breathDelay,breathLock;
        DemoCatalog.Item AdditionalItem(Kit k)=>config.content.items.FirstOrDefault(i=>i.id==additionalItemIds[(int)k-12]);
        void InitializeEquipmentCatalog(){shopPrices=new[]{25,30,20,20,200,200,200,120,750,60}.Concat(Enumerable.Range(12,20).Select(i=>AdditionalItem((Kit)i).price)).ToArray();}
        int PlayerLevel=>config.content.levels.Where(l=>l.totalXP<=save.xp).Max(l=>l.level);
        void ExtendedEquipmentInput(Keyboard kb,Mouse mouse){
            float dt=Time.deltaTime;
            if((CurrentKit==Kit.Swatter||CurrentKit==Kit.Camera||CurrentKit==Kit.Drone)&&kb.rKey.isPressed&&Vector3.Distance(world.controller.transform.position,DemoWorld.VanPoint)<3.2f&&!SelectedCargo){if(save.job.conditions[selected]<100){save.job.conditions[selected]=Mathf.MoveTowards(save.job.conditions[selected],100,dt*50);if(save.job.conditions[selected]>=100)Persist();}return;}
            aiming=(IsRanged(CurrentKit)||CurrentKit==Kit.Binoculars)&&mouse.rightButton.isPressed&&!SelectedCargo;
            breathLock=Mathf.Max(0,breathLock-dt);breathHeld=aiming&&!IsMoving&&kb.leftShiftKey.isPressed&&breath>0&&breathLock<=0;
            if(breathHeld){breath=Mathf.Max(0,breath-dt);breathDelay=1.5f;if(breath<=0)breathLock=1;}
            else{breathDelay=Mathf.Max(0,breathDelay-dt);if(breathDelay<=0)breath=Mathf.Min(SkillValue("breathSeconds",8),breath+dt*2);}
            UpdateOptics(kb.vKey.wasPressedThisFrame);
            ObserveThroughOptics(CurrentKit==Kit.Binoculars&&aiming&&mouse.leftButton.isPressed,100,dt);
            world.cam.fieldOfView=Mathf.Lerp(world.cam.fieldOfView,aiming?(CurrentKit==Kit.Binoculars?config.player.baseFOV/2:CurrentKit==Kit.Rifle?config.player.baseFOV/ServiceValue("E18.opticalZoom",1.7f):38):config.player.baseFOV,dt*12/SkillValue("aimTransitionMultiplier",1));
            if(aiming&&!breathHeld&&!kb.leftCtrlKey.isPressed)world.cam.transform.localRotation*=Quaternion.Euler(Mathf.Sin(Time.time*1.7f)*.13f*SkillValue("standingSwayMultiplier",1)*ServiceValue("E13.swayMultiplier",1),Mathf.Sin(Time.time*1.3f)*.15f*SkillValue("standingSwayMultiplier",1)*ServiceValue("E13.swayMultiplier",1),0);
            if(LargeCargo)return;bool handled=RangedActionInput(kb,mouse);
            if(!handled&&CurrentKit.HasValue&&(int)CurrentKit.Value>=12&&mouse.leftButton.wasPressedThisFrame&&attackCooldown<=0&&!IsDeployable(CurrentKit))UseAdditionalTool();
        }
        void UseAdditionalTool(){
            var k=CurrentKit.Value;var j=save.job;if(j.usedKits==null)j.usedKits=new System.Collections.Generic.List<int>();if(!j.usedKits.Contains((int)k))j.usedKits.Add((int)k);
            attackCooldown=.7f;world.handRoot.localRotation=Quaternion.Euler(-18,0,0);
            if(k==Kit.Drone){StartDrone();return;}
            if(k==Kit.Swatter){if(j.conditions[selected]<10){Notify(L("电拍电量耗尽，回车旁按住R充电","Swatter empty; hold R near the truck to recharge"));return;}j.conditions[selected]-=10;foreach(var p in world.pests)if(new[]{"I03","I04","I05","I10"}.Contains(p.record.species)&&!p.record.dead&&Vector3.Distance(p.transform.position,world.cam.transform.position)<.8f&&Vector3.Angle(world.cam.transform.forward,p.transform.position-world.cam.transform.position)<35&&world.ClearLine(world.cam.transform.position,p.transform.position))p.Dose(1);return;}
            if(k==Kit.ControlPole||k==Kit.Net){Notify(L("对准处于可控窗口的目标，长按E转入匹配空笼","Aim at a controllable target and hold E to transfer into an empty matching cage"));return;}
            if(k==Kit.Binoculars){Notify(L("右键举镜并按住左键持续观察识别","Hold RMB to aim and LMB to identify by continuous observation"));return;}
            if(IsRanged(k)){
                Kit ammo=AmmoFor(k);if(!SpendRound(k))return;
                if(k==Kit.Rifle&&j.contractIndex==2){Notify(L("委托失败：本次禁止使用步枪","Contract failed: rifle use is forbidden"));FinalizeJob(false,true);return;}
                j.directShots++;j.handDamageEvents++;attackCooldown=(k==Kit.Rifle?1:k==Kit.Blowpipe?.8f:k==Kit.Airgun?.6f:1.2f)*SkillValue("shotRecoveryMultiplier",1);
                if(k==Kit.Bow||k==Kit.Blowpipe){var projectile=new GameObject("Recoverable "+ammo);projectile.transform.SetParent(world.dynamicRoot);var p=projectile.AddComponent<DemoProjectile>();p.game=this;p.kit=k;p.ammo=ammo;p.unitId=lastSpentUnit;p.velocity=world.cam.transform.forward*(k==Kit.Bow?30:24);p.transform.position=world.cam.transform.position+world.cam.transform.forward*.3f;p.Build();}
                else if(Physics.Raycast(world.cam.transform.position,world.cam.transform.forward,out var hit,k==Kit.Rifle?100:35,~(1<<8),QueryTriggerInteraction.Collide)){var a=hit.collider.GetComponentInParent<DemoAnimal>();if(a){if(k==Kit.Rifle||a.record.species=="S02"||a.record.species.StartsWith("M")&&int.Parse(a.record.species.Substring(1))<=8)DamageAdditionalAnimal(a,100);else Notify(L("气枪不适用于该目标","Airgun is not suitable for this target"));}else{var livestock=hit.collider.GetComponentInParent<DemoLivestock>();if(livestock)livestock.Hurt(100);}}
                weaponNoise=k==Kit.Rifle?3.75f:k==Kit.Airgun?.7f:.25f;Noise=weaponNoise;Sound("hit");Persist();return;
            }
            if(k==Kit.Spear){j.handDamageEvents++;if(Mouse.current.rightButton.isPressed){var projectile=new GameObject("Thrown original spear");projectile.transform.SetParent(world.dynamicRoot);var p=projectile.AddComponent<DemoThrownSpear>();p.game=this;p.item=new LooseItemRecord{id=j.itemIds[selected],kit=(int)Kit.Spear,quantity=1,condition=j.conditions[selected]};p.velocity=world.cam.transform.forward*18;p.transform.position=world.cam.transform.position+world.cam.transform.forward*.4f;p.Build();j.looseItems.Add(p.item);RemoveSlot(selected);Persist();return;}foreach(var h in Physics.SphereCastAll(world.cam.transform.position,.12f,world.cam.transform.forward,1.8f,~(1<<8),QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(h.collider.gameObject.layer==9)break;var a=h.collider.GetComponentInParent<DemoAnimal>();if(a){SpearHit(a);break;}}}
        }
        public void SpearHit(DemoAnimal animal){if(config.content.species.First(s=>s.id==animal.record.species).size=="Large"){animal.PushFromSpear();Notify(L("大型目标被推拒，未造成致命控制","Large target repelled; not controlled"));}else DamageAdditionalAnimal(animal,100);}
        public void DamageAdditionalAnimal(DemoAnimal animal,float damage){if(animal.record.dead||animal.record.contained)return;if(!animal.record.attackRegistered){animal.record.attackRegistered=true;if(animal.record.required&&!animal.record.identified)save.job.identifiedBeforeAttack=false;}animal.record.health-=damage;if(animal.record.health<=0)animal.Kill();}
    }
    public class DemoThrownSpear:MonoBehaviour {
        public DemoGame game;public LooseItemRecord item;public Vector3 velocity;float life=8;
        public void Build(){game.world.Box("Flying spear shaft",Vector3.zero,new Vector3(.03f,.03f,1.3f),game.world.wood,transform,false);transform.rotation=Quaternion.LookRotation(velocity);}
        void Update(){if(game.Paused)return;float dt=Time.deltaTime;life-=dt;velocity+=Physics.gravity*dt;
            if(Physics.Raycast(transform.position,velocity.normalized,out var hit,velocity.magnitude*dt,~(1<<8),QueryTriggerInteraction.Collide)){var animal=hit.collider.GetComponentInParent<DemoAnimal>();if(animal)game.SpearHit(animal);else{var livestock=hit.collider.GetComponentInParent<DemoLivestock>();if(livestock)livestock.Hurt(100);}Land(hit.point);return;}
            transform.position+=velocity*dt;item.position=transform.position;transform.rotation=Quaternion.LookRotation(velocity);if(life<=0)Land(transform.position);
        }
        void Land(Vector3 p){item.position=p+Vector3.up*.05f;if(!game.save.job.looseItems.Any(i=>i.id==item.id))game.save.job.looseItems.Add(item);game.world.CreateLooseItem(item);game.Persist();Destroy(gameObject);}
    }
    public class DemoProjectile:MonoBehaviour {
        public DemoGame game;public Kit kit,ammo;public string unitId;public Vector3 velocity;float lifetime=10;
        public void Build(){game.world.Box("Projectile shaft",Vector3.zero,new Vector3(.012f,.012f,.45f),game.world.wood,transform,false);transform.rotation=Quaternion.LookRotation(velocity);}
        void Update(){if(game.Paused)return;float dt=Time.deltaTime;lifetime-=dt;velocity+=Physics.gravity*(kit==Kit.Bow?1:.3f)*dt;
            if(Physics.Raycast(transform.position,velocity.normalized,out var h,velocity.magnitude*dt,~(1<<8),QueryTriggerInteraction.Collide)){
                var livestock=h.collider.GetComponentInParent<DemoLivestock>();if(livestock&&kit==Kit.Bow)livestock.Hurt(100);var a=h.collider.GetComponentInParent<DemoAnimal>();if(a){var definition=game.config.content.species.First(s=>s.id==a.record.species);if(kit==Kit.Blowpipe&&!a.record.dead){if(new[]{"M01","M02","M03","M04","M05","C01","C08","C09"}.Contains(a.record.species)){a.record.sedatedUntil=game.save.job.nightSeconds+5*game.SkillValue("shortControlWindowMultiplier",1);game.Notify(game.L("动物暂时减速，尚未受控","Animal slowed temporarily; not yet controlled"));}}else if(definition.size!="Large")game.DamageAdditionalAnimal(a,100);else game.Notify(game.L("弓不适用于大型目标","Bow is not suitable for large targets"));}
                if(kit==Kit.Bow&&UnityEngine.Random.value<=game.SkillValue("arrowRecoveryProbability",.5f)){var item=new LooseItemRecord{id=unitId,kit=(int)ammo,position=h.point+Vector3.up*.04f};if(!game.save.job.looseItems.Any(i=>i.id==item.id)){game.save.job.looseItems.Add(item);game.world.CreateLooseItem(item);}}Destroy(gameObject);return;
            }
            transform.position+=velocity*dt;transform.rotation=Quaternion.LookRotation(velocity);if(lifetime<=0)Destroy(gameObject);
        }
    }
}
