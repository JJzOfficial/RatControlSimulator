using System.Linq;
using UnityEngine;

namespace Riverbend
{
    public partial class DemoGame
    {
        void Icon(Kit k,float x,float y,float size,Color color)
        {
            float u=size/32f;
            if(k==Kit.Cage){for(int i=0;i<4;i++)RectFill(new Rect(x+(4+i*7)*u,y+5*u,1.5f*u,22*u),color);RectFill(new Rect(x+4*u,y+5*u,23*u,2*u),color);RectFill(new Rect(x+4*u,y+25*u,23*u,2*u),color);}
            else if(k==Kit.SnapTrap){RectFill(new Rect(x+3*u,y+23*u,26*u,5*u),color);RectFill(new Rect(x+6*u,y+7*u,2*u,17*u),color);RectFill(new Rect(x+6*u,y+7*u,18*u,2*u),color);RectFill(new Rect(x+23*u,y+7*u,2*u,17*u),color);}
            else if(k==Kit.Spray||k==Kit.Flashlight){RectFill(new Rect(x+10*u,y+11*u,13*u,18*u),color);RectFill(new Rect(x+13*u,y+5*u,6*u,6*u),color);RectFill(new Rect(x+13*u,y+3*u,15*u,3*u),color);}
            else {RectFill(new Rect(x+15*u,y+5*u,3*u,24*u),color);RectFill(new Rect(x+5*u,y+(k==Kit.Broom?22:5)*u,24*u,7*u),color);}
        }
        void HUD()
        {
            var j=save.job;
            for(int i=0;i<notices.Count;i++) {
                float a=Mathf.Clamp01(notices[i].left);var c=GUI.color;GUI.color=new Color(1,1,1,a);
                RectFill(new Rect(25,28+i*49,500,42),new Color(.025f,.06f,.065f,.9f));RectFill(new Rect(25,28+i*49,3,42),gold);Label(40,34+i*49,470,34,notices[i].message,small);GUI.color=c;
            }
            RectFill(new Rect(1260,25,315,82),new Color(.025f,.06f,.065f,.86f));
            Label(1280,36,280,30,j.contractIndex==0?L("作业 ","Work ")+Enumerable.Range(0,3).Count(i=>j.work[i]>=(i<2?3:1))+"/3    "+L("鼠类 ","Mice ")+j.Handled+"/5":Contract(j).id+" · "+L("受控 ","Controlled ")+j.Handled+"/"+Contract(j).RequiredCount,small);
            Label(1280,70,280,30,L("总受损 ","Total damage ")+(100-j.TotalDurability/5)+"%",small);
            RectFill(new Rect(794,449,12,2),ink);RectFill(new Rect(799,444,2,12),ink);
            if(!Paused&&prompt!="") {
                RectFill(new Rect(530,506,540,78),new Color(.025f,.06f,.065f,.82f));Label(548,516,504,65,prompt,small);
                if(actionProgress>0)DrawActionRing(Mathf.Clamp01(actionProgress/Mathf.Max(.1f,heldDuration)));
            }
            int startSlot=selected<0?0:(selected/10)*10;int shown=Mathf.Max(4,Mathf.Min(10,j.kit.Count-startSlot));float bx=800-shown*36;
            RectFill(new Rect(bx-75,801,58,61),new Color(.025f,.06f,.065f,.9f));Label(bx-64,814,40,30,"0",small);if(selected<0)RectFill(new Rect(bx-75,801,58,3),gold);
            for(int row=0;row<shown;row++) {int i=startSlot+row;
                float x=bx+row*72;RectFill(new Rect(x,801,63,61),new Color(.025f,.06f,.065f,.9f));if(i==selected)RectFill(new Rect(x,801,63,3),gold);
                if(i<j.kit.Count){Icon((Kit)j.kit[i],x+18,813,29,Carrying&&j.carriedSlot==i?gold:ink);Label(x+5,807,20,22,(i+1).ToString(),small);float condition=SlotCondition(i);if(condition>=0){RectFill(new Rect(x+5,849,53,4),muted);RectFill(new Rect(x+5,849,53*condition,4),condition>.2f?green:gold);Label(x+25,831,38,20,Mathf.CeilToInt(condition*100)+"%",small);}else if(i<j.quantities.Count&&j.quantities[i]>1)Label(x+30,840,30,20,"×"+j.quantities[i],small);}
            }
            if(selected>=0&&selected<j.kit.Count)Label(650,866,420,30,SelectedCargo?(CurrentKit==Kit.Corpse?L("老鼠尸体","Mouse remains"):L("装有动物的笼子","Occupied cage")):KitName((Kit)j.kit[selected])+(selected<j.quantities.Count&&j.quantities[selected]>1?" ×"+j.quantities[selected]:""),small);
            // Speaker cone and four bars are driven only by footsteps, sprinting and landings.
            RectFill(new Rect(30,824,9,13),ink);for(int i=0;i<8;i++)RectFill(new Rect(39+i,820-i*.5f,1,21+i),ink);
            for(int i=0;i<4;i++)RectFill(new Rect(58+i*10,837-i*6,6,8+i*6),Noise>(i*.23f+.03f)?gold:new Color(.2f,.3f,.3f));
            if(j.contractIndex>0){Label(30,705,240,24,L("生命 ","Health ")+Mathf.CeilToInt(j.health),small);RectFill(new Rect(30,731,240*j.health/100,4),green);}
            Label(30,739,240,24,L("体力","Stamina"),small);RectFill(new Rect(30,764,240,4),muted);RectFill(new Rect(30,764,240*stamina/(100*SkillValue("staminaMultiplier",1)),4),green);
            Label(30,773,240,24,L("屏息","Breath"),small);RectFill(new Rect(30,798,240,4),muted);RectFill(new Rect(30,798,240*breath/SkillValue("breathSeconds",8),4),breathHeld?gold:green);
            if(CurrentKit.HasValue&&IsRanged(CurrentKit)){var m=Magazine(j.itemIds[selected]);Label(650,749,500,30,L("内装 ","Loaded ")+m.rounds+"/"+MagazineCapacity(CurrentKit.Value)+L(" · R装填"," · R reload"),small);if(reloadItem!=null){Label(650,720,400,25,L("正在装填","Reloading"),small);DrawActionRing(Mathf.Clamp01(reloadElapsed/reloadDuration));}else if(bowCharge>0)DrawActionRing(bowCharge/.8f);}
            DrawHeardCue();
            if(skunkEffect>0)RectFill(new Rect(0,0,1600,900),new Color(.35f,.42f,.13f,.12f));
            if(j.orderRemaining>0)Label(1295,859,280,28,L("配送 ","Delivery ")+Mathf.CeilToInt(j.orderRemaining)+"s",small);
            if(senseLeft>0)DrawMarkers();
        }
    }
}
