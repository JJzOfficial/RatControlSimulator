using System;
using System.IO;
using UnityEngine;

namespace Riverbend {
    // Frontend only: all play still runs through the accepted DemoGame lifecycle.
    public partial class DemoGame {
        bool frontend=true,frontendSettings,confirmNew,enteredPlay;
        [Serializable] class FrontendSettings {public bool english;public float sensitivity,volume;}
        void SaveFrontendSettings(){
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(!string.IsNullOrEmpty(DemoSave.testPath)||!string.IsNullOrEmpty(DemoSave.verificationFallback))return;
            #endif
            try{string path=Path.Combine(Application.persistentDataPath,"RiverbendDemo","settings.json");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(new FrontendSettings{english=save.english,sensitivity=sensitivity,volume=save.volume},true));}catch(Exception e){Notify(e.Message);}}
        void LoadFrontendSettings(){try{string path=Path.Combine(Application.persistentDataPath,"RiverbendDemo","settings.json");if(File.Exists(path)){var settings=JsonUtility.FromJson<FrontendSettings>(File.ReadAllText(path));save.english=settings.english;save.volume=Mathf.Clamp01(settings.volume);sensitivity=Mathf.Clamp(settings.sensitivity,.025f,.2f);}}catch(Exception e){Notify(e.Message);}}

        void FrontendUI(){
            RectFill(new Rect(0,0,1600,900),new Color(.018f,.045f,.048f));
            Label(500,150,650,70,L("捕鼠模拟器","Riverbend Rat Control"),title);
            if(!frontendSettings&&!confirmNew)for(int slot=0;slot<3;slot++){int index=slot;if(Button(500+slot*205,225,190,42,(DemoSave.selectedSlot==slot?"● ":"○ ")+L("存档 ","Slot ")+(slot+1)))DemoSave.selectedSlot=index;}
            if(frontendSettings){SettingsUI(500,300,600);if(Button(500,600,600,50,L("返回","Back")))frontendSettings=false;return;}
            if(confirmNew){Label(500,260,600,90,L("开始新游戏？当前存档会先保留备份。","Start a new game? The current save will be backed up."));if(Button(500,390,285,55,L("确认","Confirm")))BeginFreshProfile();if(Button(815,390,285,55,L("取消","Cancel")))confirmNew=false;return;}
            if(Button(500,285,600,58,L("开始游戏","Start game")))confirmNew=true;
            if(Button(500,365,600,58,L("载入存档","Load game"),File.Exists(DemoSave.PathName)||File.Exists(DemoSave.PathName+".bak"))){Reload();if(DemoSave.protectedSavePath==DemoSave.PathName){frontend=true;Notify(DemoSave.loadProblem);SyncPause();}else{frontend=false;enteredPlay=true;SyncPause();}}
            if(Button(500,445,600,58,L("设置","Settings")))frontendSettings=true;
            if(Button(500,525,600,58,L("退出游戏","Quit"))){
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
                #else
                Application.Quit();
                #endif
            }
            if(toastTime>0)Label(500,675,650,100,toast,small);
        }
        void BeginFreshProfile(){
            try{if(File.Exists(DemoSave.PathName))File.Copy(DemoSave.PathName,DemoSave.PathName+".before-new-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));}
            catch(Exception e){Notify(L("备份失败，未覆盖存档：","Backup failed; save unchanged: ")+e.Message);return;}
            DemoSave.protectedSavePath=null;DemoSave.loadProblem="";bool english=save.english;float volume=save.volume;save=new SaveRecord{cash=config.profile.startingCash,english=english,volume=volume,sensitivity=sensitivity};frontend=false;confirmNew=false;enteredPlay=true;selected=-1;ShowCompany();Persist();
        }
    }
}
