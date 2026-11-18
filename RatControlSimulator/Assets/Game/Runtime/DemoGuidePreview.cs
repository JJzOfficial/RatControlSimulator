using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        int guideSelected;DemoGuidePreview guidePreview;
        void SpeciesPreview(float x,float y,float width){var d=config.content.species[Mathf.Clamp(guideSelected,0,44)];bool known=save.encyclopedia.Contains(d.id);
            if(!guidePreview){var root=new GameObject("Field guide model viewer");root.transform.SetParent(transform);guidePreview=root.AddComponent<DemoGuidePreview>();guidePreview.game=this;}
            guidePreview.Show(d.id,known);if(guidePreview.texture)GUI.DrawTexture(new Rect(x,y,width,245),guidePreview.texture,ScaleMode.ScaleToFit);
            Label(x,y+255,width,35,d.id+" · "+SpeciesName(d.id),heading);
            var hosts=config.content.contracts.Where(c=>c.guaranteedInsects.Contains(d.id)||c.targetBudget.Any(b=>b.speciesId==d.id)).Select(c=>c.id).Concat(config.content.rareEvents.Where(r=>r.speciesId==d.id).Select(r=>r.hostContractId)).Distinct();
            Label(x,y+298,width,125,L("活动线索委托：","Host contracts: ")+string.Join(" / ",hosts)+"\n"+(known?L("适配：","Control: ")+string.Join(" / ",d.captureMethods??new string[0])+"\n"+L("活体 / 遗体 / 捐赠：","Live / remains / donation: ")+d.livePrice+" / "+d.corpsePrice+" / "+d.donationXP+" XP":L("未知轮廓；完成要求的遗体或活体调查后解锁。","Unknown silhouette. Investigate the required remains or contained live animal.")),small);
        }
    }
    public class DemoGuidePreview:MonoBehaviour {
        public DemoGame game;public RenderTexture texture;Camera view;Transform model;string species;bool unlocked;
        public void Show(string id,bool known){if(species==id&&unlocked==known&&model)return;species=id;unlocked=known;if(model)Destroy(model.gameObject);if(!view){texture=new RenderTexture(512,384,24);texture.Create();var cameraRoot=new GameObject("Guide camera");cameraRoot.transform.SetParent(transform);view=cameraRoot.AddComponent<Camera>();view.CopyFrom(game.world.cam);view.targetTexture=texture;view.cullingMask=1<<11;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.04f,.08f,.09f);view.fieldOfView=35;view.aspect=4f/3;game.world.cam.cullingMask&=~(1<<11);}
            model=new GameObject("Guide "+id).transform;model.SetParent(transform);model.position=new Vector3(350,350,350);if(id.StartsWith("I")){var actor=model.gameObject.AddComponent<DemoPest>();actor.world=game.world;actor.record=new PestRecord{id="guide-"+id,species=id,position=model.position};actor.Build();actor.enabled=false;}else{var actor=model.gameObject.AddComponent<DemoAnimal>();actor.world=game.world;actor.record=new AnimalRecord{id="guide-"+id,species=id,position=model.position};actor.MakeModel();actor.enabled=false;}
            foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=11;foreach(var c in model.GetComponentsInChildren<Collider>())c.enabled=false;var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=new Bounds(model.position,Vector3.zero);foreach(var r in renderers){bounds.Encapsulate(r.bounds);if(!known)r.sharedMaterial=game.world.dark;}float distance=Mathf.Max(.35f,bounds.size.magnitude*1.8f);view.transform.position=bounds.center+new Vector3(1,.6f,1.3f).normalized*distance;view.transform.LookAt(bounds.center);view.nearClipPlane=.01f;view.farClipPlane=20;
        }
        void LateUpdate(){if(view)view.enabled=game&&game.GuideVisible;if(model&&view&&view.enabled)model.Rotate(0,Time.unscaledDeltaTime*12,0);}
        void OnDestroy(){if(texture){texture.Release();Destroy(texture);}}
    }
    public partial class DemoGame {public bool GuideVisible=>!frontend&&panel=="journal"&&tab==1;}
}
