using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        string heardCue;Vector3 heardPosition;float heardUntil,heardStrength;
        public void HearAnimalCue(string cue,Vector3 position,float strength){
            if(strength<.025f||Paused)return;
            if(Time.time<heardUntil&&strength<heardStrength*.7f)return;
            heardCue=cue;heardPosition=position;heardStrength=strength;heardUntil=Time.time+.8f;
        }
        void DrawHeardCue(){
            if(Time.time>=heardUntil||string.IsNullOrEmpty(heardCue))return;
            var delta=heardPosition-world.cam.transform.position;float side=Vector3.Dot(delta.normalized,world.cam.transform.right),forward=Vector3.Dot(delta.normalized,world.cam.transform.forward);
            string direction=Mathf.Abs(side)>.55f?side>0?L("右侧","Right"):L("左侧","Left"):forward>=0?L("前方","Ahead"):L("后方","Behind");
            string category=heardCue=="Wolf"?L("低吼","Growl"):heardCue=="Boar"?L("野猪预警","Boar warning"):heardCue=="Rattle"?L("响尾预警","Rattle warning"):heardCue=="Dog"?L("犬吠","Bark"):heardCue=="Chicken"||heardCue=="Pig"?L("家畜报警","Livestock alarm"):heardCue=="Gnaw"?L("啃咬","Gnawing"):L("窸窣声","Rustling");
            Label(30,858,300,28,direction+" · "+category,small);
        }
    }
}
