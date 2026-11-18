using System.Linq;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        int feedIndex;string requestedFeed;
        public bool MonitoringFeedRequested(string id)=>panel=="monitor"&&requestedFeed==id;
        void MonitoringUI(){var cameras=world.devices.Values.Where(d=>d.record.kit==(int)Kit.Camera).ToArray();if(cameras.Length==0){Label(335,230,880,100,L("先在地图部署监测相机。","Deploy a monitoring camera first."),heading);return;}
            feedIndex=Mathf.Clamp(feedIndex,0,cameras.Length-1);var camera=cameras[feedIndex];requestedFeed=camera.record.id;Label(335,185,880,35,L("相机 ","Camera ")+(feedIndex+1)+" / "+cameras.Length+" · "+L("剩余电量 ","Energy ")+Mathf.CeilToInt(camera.record.energy)+"s · "+L("记录 ","Recorded ")+camera.record.observed.Count,heading);
            if(camera.feed&&camera.record.energy>0)GUI.DrawTexture(new Rect(350,235,860,460),camera.feed,ScaleMode.ScaleToFit);else Label(500,390,600,100,L("相机电量耗尽","Camera battery depleted"),heading);
            if(Button(335,730,220,38,"←"))feedIndex=(feedIndex+cameras.Length-1)%cameras.Length;if(Button(995,730,220,38,"→"))feedIndex=(feedIndex+1)%cameras.Length;
        }
    }
}
