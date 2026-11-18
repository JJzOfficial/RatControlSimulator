using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RecoverStolen(DemoStolenObject item){var a=item.animal;var j=save.job;if(a.stolenRecovered||a.stolenLost)return;if(!a.dead&&!a.contained&&!a.loaded){Notify(L("先控制叼物动物，再追回物品","Control the carrier before recovering its item"));return;}a.stolenRecovered=true;a.assetDamage=Mathf.Max(0,a.assetDamage-10);j.recoveredItems++;j.durability[a.stolenSource]=Mathf.Min(100,j.durability[a.stolenSource]+10);Destroy(item.gameObject);Persist();Notify(L("已追回完整物品","Recovered the intact stolen item"));}
    }
    public partial class DemoWorld {
        public void CreateStolenObject(AnimalRecord a){var root=new GameObject("Stolen property · recover within 10 seconds");root.transform.SetParent(dynamicRoot);var actor=root.AddComponent<DemoStolenObject>();actor.world=this;actor.animal=a;Shape("Stolen parcel",PrimitiveType.Sphere,Vector3.zero,new Vector3(.25f,.18f,.2f),cream,root.transform);var c=root.AddComponent<SphereCollider>();c.radius=.22f;c.isTrigger=true;root.AddComponent<DemoTarget>().kind="stolen";}
        void RestoreStolenObjects(){foreach(var a in game.save.job.animals)if(a.stolenSource>=0&&!a.stolenRecovered&&!a.stolenLost)CreateStolenObject(a);}
    }
    public class DemoStolenObject:MonoBehaviour {
        public DemoWorld world;public AnimalRecord animal;
        void Update(){if(world.game.Paused)return;var j=world.game.save.job;if(animal.stolenRecovered||animal.stolenLost){Destroy(gameObject);return;}if(j.nightSeconds-animal.stolenAt>10){animal.stolenLost=true;world.game.Notify(world.game.L("追回窗口结束，物品已损坏","Recovery window ended; the stolen item was damaged"));Destroy(gameObject);return;}
            transform.position=animal.position+Vector3.up*.25f;if(world.animals.TryGetValue(animal.id,out var actor))transform.position=actor.transform.position+actor.transform.forward*.35f+Vector3.up*.25f;
        }
    }
}
