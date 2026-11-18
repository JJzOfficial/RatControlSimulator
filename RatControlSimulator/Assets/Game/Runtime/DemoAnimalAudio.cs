using System.Collections.Generic;
using UnityEngine;
namespace Riverbend {
    public partial class DemoWorld {
        readonly Dictionary<string,AudioClip> animalCues=new Dictionary<string,AudioClip>();
        public void AnimalCue(string cue,Vector3 position,float volume=.35f){
            if(game.Paused)return;
            float range=cue=="Wolf"||cue=="Dog"?18:10;
            if(!ClearLine(position+Vector3.up*.15f,cam.transform.position))volume*=.35f;
            game.HearAnimalCue(cue,position,volume*game.save.volume*Mathf.Clamp01(1-Vector3.Distance(position,cam.transform.position)/range));
            if(!animalCues.TryGetValue(cue,out var clip)){
                const int rate=22050;float duration=cue=="Wolf"?.75f:cue=="Rattle"?.55f:cue=="Dog"?.3f:cue=="Boar"?.5f:.18f;var samples=new float[Mathf.CeilToInt(rate*duration)];uint noise=1234567;
                for(int i=0;i<samples.Length;i++){float t=i/(float)rate,u=t/duration,envelope=Mathf.Sin(Mathf.PI*u);noise^=noise<<13;noise^=noise>>17;noise^=noise<<5;float hiss=((noise&65535)/32767.5f-1),frequency=cue=="Wolf"?240+60*Mathf.Sin(u*Mathf.PI):cue=="Dog"?180+70*(1-u):cue=="Boar"?95:cue=="Cat"?680:cue=="Chicken"?950:cue=="Rattle"?2300:cue=="Gnaw"?1100:cue=="Pig"?120:cue=="Rustle"?400:1600;float tone=Mathf.Sin(t*frequency*Mathf.PI*2);samples[i]=envelope*(cue=="Rattle"?hiss*(.4f+.6f*Mathf.Abs(Mathf.Sin(t*45*Mathf.PI))):cue=="Rustle"?hiss*.45f:tone*.5f+hiss*.12f)*.3f;}
                clip=AudioClip.Create("Procedural whitebox "+cue,samples.Length,1,rate,false);clip.SetData(samples,0);animalCues.Add(cue,clip);
            }
            var root=new GameObject("Spatial cue "+cue);root.transform.SetParent(dynamicRoot);root.transform.position=position;var source=root.AddComponent<AudioSource>();source.clip=clip;source.volume=volume*game.save.volume;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=1;source.maxDistance=cue=="Wolf"||cue=="Dog"?18:10;source.Play();Object.Destroy(root,clip.length+.1f);
        }
        void OnDestroy(){foreach(var clip in animalCues.Values)if(clip)Destroy(clip);}
    }
    public partial class DemoAnimal {
        float animalCueTime;
        void AdditionalMotionCue(float dt){animalCueTime-=dt;if(animalCueTime>0)return;bool moving=State=="Roam"||State=="Flee"||State=="Approach";if(!moving&&State!="Gnaw"&&State!="Steal")return;animalCueTime=State=="Flee"?.45f:State=="Gnaw"?1:1.8f;world.AnimalCue(State=="Gnaw"||State=="Steal"?"Gnaw":Definition.family=="OpenRunner"||Definition.family=="Canid"||Definition.family=="SneakThief"?"Rustle":"Step",transform.position,.15f);}
    }
}
