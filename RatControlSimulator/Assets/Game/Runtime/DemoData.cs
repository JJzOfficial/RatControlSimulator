using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Riverbend
{
    public enum ShiftPhase { Company, Day, Night, Result }
    public enum Kit { Spray, Broom, Repair, Flashlight, SnapTrap, Cage, Baton, SprayRefill, RepairPack, Bait, Corpse, CleaningRefill,
        Swatter,MediumCage,LargeCage,ControlPole,Net,BaitStation,Blowpipe,Spear,Bow,Airgun,Binoculars,Drone,Camera,Rifle,Darts,Arrows,Pellets,Cartridges,PoisonBait,BodyBag }
    [Serializable] public class DemoConfig
    {
        public string configVersion;
        public PlayerSettings player;
        public SenseSettings sense;
        public VehicleSettings vehicle;
        public ProfileSettings profile;
        public ContractSettings[] contracts;
        public SpeciesSettings[] species;
        public DamageSettings furnitureDamage;
        [NonSerialized] public DemoCatalog content;
        [Serializable] public class PlayerSettings { public int baseSlots; public float walkSpeed, sprintSpeed, crouchSpeed, jumpHeight, gravity, baseFOV, stamina, sprintDrainPerSecond, staminaRecoveryPerSecond; }
        [Serializable] public class SenseSettings { public float radius, markSeconds, cooldownSeconds; }
        [Serializable] public class VehicleSettings { public int ordinaryLiveSlots, deploymentLimit; public float deliverySeconds; }
        [Serializable] public class ProfileSettings { public int startingCash, level2RequiredTotalXP; }
        [Serializable] public class ContractSettings { public int requiredCount, rewardCash, rewardXP, firstStarCash, firstStarXP; public float minimumFurnitureRemaining; }
        [Serializable] public class SpeciesSettings { public string id; public int livePrice, corpsePrice, donationXP; public float runSpeed; }
        [Serializable] public class DamageSettings { public int pointsPerCompletedBite; public float biteSeconds, minimumAttackInterval; }
        public SpeciesSettings Species(string id) { return Array.Find(species, s => s.id == id); }
        public static DemoConfig Load()
        {
            var text = Resources.Load<TextAsset>("P0_Config");
            if (!text) throw new InvalidDataException("Missing P0_Config resource");
            var c = JsonUtility.FromJson<DemoConfig>(text.text);
            if (c.contracts == null || c.contracts.Length != 1 || c.contracts[0].requiredCount != 5 || c.player.baseSlots != 4)
                throw new InvalidDataException("P0 catalog validation failed");
            c.content=DemoCatalog.Load();
            var all=c.species.ToList();foreach(var s in c.content.species)if(!all.Any(existing=>existing.id==s.id))all.Add(new SpeciesSettings{id=s.id,livePrice=s.livePrice,corpsePrice=s.corpsePrice,donationXP=s.donationXP,runSpeed=s.fleeSpeed});c.species=all.ToArray();
            return c;
        }
    }
    [Serializable] public class AnimalRecord
    {
        public string id, species, runId;
        public bool required, dead, contained, investigated, loaded, terminal,deliveryTarget;
        public string deviceId;
        public float health=100,sedatedUntil,poisonRemaining;
        public bool identified,bagged,drivenOff;
        public string partnerClaim;
        public bool partnerHeld;
        public uint randomState;
        public float drivenUntil;
        public int stolenSource=-1;
        public float stolenAt;
        public bool stolenRecovered,stolenLost;
        public string controlMethod;
        public int assetDamage;
        public bool behaviorSaved,aiActive,aiHasGoal,attackRegistered;
        public float aiAlarm,aiPause,aiWalk,aiSpecial,aiCooldown,aiLight,aiWait,aiBite,aiRest,aiThink;
        public bool aiEntering;
        public string aiLureId;
        public int aiFood=-1;
        public Vector3 aiOrigin,aiDirection,aiGoal;
        public float caution, boldness, hunger;
        public Vector3 position;
    }
    [Serializable] public class JobRecord
    {
        public int mapIndex,contractIndex;
        public float health=100,noiseSeconds,controlTime=-1;
        public int fleeEvents,handDamageEvents,directShots,skunkSprays,dangerHits,boarHits,traceCount,cameraPasses,recoveredItems,prepTraceCount;
        public List<string> cameraTargets=new List<string>();
        public bool identifiedBeforeAttack=true;
        public bool sourcesPrepared;
        public List<RarePlan> rarePlans=new List<RarePlan>();
        public List<int> traceMarks=new List<int>();
        public List<int> sourceInspected=new List<int>();
        public List<LivestockRecord> livestock=new List<LivestockRecord>();
        public List<int> usedKits=new List<int>();
        public List<string> borrowedItems=new List<string>();
        public List<string> investigatedSpecies=new List<string>();
        public string runId;
        public int stage;
        public bool prepStar;
        public int layoutVersion=3;
        public List<Vector3> workPositions=new List<Vector3>();
        public List<int> quantities=new List<int>();
        public List<string> itemIds=new List<string>();
        public List<float> conditions=new List<float>();
        public List<LooseItemRecord> deliveredItems=new List<LooseItemRecord>();
        public List<LooseItemRecord> crateItems=new List<LooseItemRecord>();
        public List<string> cargoIds=new List<string>();
        public List<bool> baitedSlots=new List<bool>();
        public float nightSeconds, rareAt;
        public bool rareRolled, rareWillSpawn, rareSpawned;
        public int layoutSeed;
        public List<LooseItemRecord> looseItems=new List<LooseItemRecord>();
        public List<PestRecord> pests=new List<PestRecord>();
        public List<int> storedKit=new List<int>();
        public string deliveredItem;
        public int carriedSlot=-1;
        public float spraySeconds;
        public int cleaning = 100;
        public int spray = 5, repairs = 100, bait = 0, bags = 0;
        public int[] work = new int[5];
        public int[] durability = {100,100,100,100,100};
        public List<AnimalRecord> animals = new List<AnimalRecord>();
        public List<DeviceRecord> devices = new List<DeviceRecord>();
        public List<int> kit = new List<int>();
        public Vector3 player = new Vector3(-7.5f,0.1f,-6);
        public float yaw=45, senseCooldown;
        public bool carryingCage, loan = true;
        public string carriedAnimal;
        public string orderItem;
        public float orderRemaining;
        public int TotalDurability { get { int n=0; foreach(var d in durability) n+=d; return n; } }
        public int Handled { get { int n=0; foreach(var a in animals) if(a.required && (a.drivenOff||!a.terminal && (a.dead || a.contained))) n++; return n; } }
        public bool RequiredWorkDone { get { return contractIndex==0?work[0]>=3 && work[1]>=3 && work[2]>=1:DemoGame.main&&DemoGame.main.RequiredContractWork(this); } }
        public bool CanDepart { get { return contractIndex==0?stage==2 && RequiredWorkDone && Handled==5 && TotalDurability>=300:DemoGame.main&&DemoGame.main.CanFinishContract(this); } }
    }
    [Serializable] public class DeviceRecord { public string id, animalId; public Vector3 position; public float yaw; public string itemId; public bool cage, triggered, baited;public int kit;public float energy=300;public List<string> observed=new List<string>(); }
    [Serializable] public class LooseItemRecord { public string id;public int kit;public int quantity=1;public float condition=100;public bool baited;public Vector3 position; }
    [Serializable] public class PestRecord { public string id,species;public int source;public Vector3 position;public bool dead,cleaned;public float exposure; }
    [Serializable] public class RarePlan {public string species;public float at;public bool willSpawn,spawned;public Vector3 position;}
    [Serializable] public class MagazineRecord {public string itemId;public int rounds;public List<string> unitIds=new List<string>();}
    [Serializable] public class InstalledService {public string itemId,serviceId;}
    [Serializable] public class PartnerRecord {public string id,type,target,command="Work";public bool active,lethal;public float rest,chase,windup;public Vector3 position,commandPoint;}
    [Serializable] public class SaveRecord
    {
        public int version=1, cash=300, xp, successes, runs, stars;
        public bool rareCaught, english;
        public int[] contractStars=new int[18];
        public List<string> completedContracts=new List<string>();
        public List<string> capturedRare=new List<string>();
        public float droneBattery=120;
        public int[] skillRanks=new int[26];
        public List<MagazineRecord> magazines=new List<MagazineRecord>();
        public List<PartnerRecord> partners=new List<PartnerRecord>();
        public List<string> snakeLiners=new List<string>();
        public List<string> upgrades=new List<string>();
        public List<InstalledService> installations=new List<InstalledService>();
        public int revision, selectedMap, selectedContract;
        public bool atDepot=true, starterGranted;
        public List<int> cart=new List<int>();
        public List<LooseItemRecord> depotItems=new List<LooseItemRecord>();
        public string orderDestination="Counter";
        public List<string> saleHolding=new List<string>(), donationHolding=new List<string>();
        public float sensitivity=0.085f, volume=0.6f;
        public int cagesOwned;
        public List<string> encyclopedia = new List<string>();
        public List<AnimalRecord> cargo = new List<AnimalRecord>();
        public JobRecord job;
    }
    public static class DemoSave
    {
        public static int selectedSlot;
        public static string loadProblem="",protectedSavePath;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static string testPath,verificationFallback;
        #endif
        public static string PathName {
            get {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if(!string.IsNullOrEmpty(testPath))return testPath;
                if(!string.IsNullOrEmpty(verificationFallback))return verificationFallback;
                #endif
                return Path.Combine(Application.persistentDataPath, "RiverbendDemo",selectedSlot==0?"profile.json":"profile-slot"+(selectedSlot+1)+".json");
            }
        }
        public static SaveRecord Load(int startingCash)
        {
            loadProblem="";protectedSavePath=null;
            foreach(var path in new[]{PathName,PathName+".bak"})
            {
                try { if(File.Exists(path)) { var s=JsonUtility.FromJson<SaveRecord>(File.ReadAllText(path));if(s!=null&&s.version>1){loadProblem="此存档来自更新版本，已拒绝载入及覆盖。 / This save requires a newer game version; loading and overwriting are blocked.";protectedSavePath=PathName;return new SaveRecord{cash=startingCash};} if(s!=null && s.version==1 && s.cash>=0 && s.cargo!=null && s.encyclopedia!=null) {
                    // Unity's serializer materializes a null inline class as an empty object.
                    // The immutable run ID, rather than object nullness, identifies an active job.
                    if(s.job!=null && string.IsNullOrEmpty(s.job.runId))s.job=null;
                    if(s.job!=null && (s.job.work==null || s.job.work.Length!=5 || s.job.durability==null || s.job.durability.Length!=5 || s.job.kit==null || s.job.kit.Count>60))continue;
                    if(s.job!=null) {
                        var j=s.job;
                        if(j.looseItems==null)j.looseItems=new List<LooseItemRecord>();
                        if(j.crateItems==null)j.crateItems=new List<LooseItemRecord>();
                        if(j.deliveredItems==null)j.deliveredItems=new List<LooseItemRecord>();
                        if(j.itemIds==null)j.itemIds=new List<string>();
                        if(j.conditions==null)j.conditions=new List<float>();
                        if(j.pests==null)j.pests=new List<PestRecord>();
                        if(j.storedKit==null)j.storedKit=new List<int>{4,4,5};
                        j.kit.RemoveAll(k=>k==(int)Kit.Flashlight);
                        if(j.cargoIds==null)j.cargoIds=new List<string>();if(j.baitedSlots==null)j.baitedSlots=new List<bool>();
                        while(j.cargoIds.Count<j.kit.Count)j.cargoIds.Add(null);while(j.baitedSlots.Count<j.kit.Count)j.baitedSlots.Add(false);
                        if(j.carriedSlot>=0&&j.carriedSlot<j.kit.Count)j.cargoIds[j.carriedSlot]=j.carriedAnimal;
                        if(j.quantities==null)j.quantities=new List<int>();
                        while(j.quantities.Count<j.kit.Count)j.quantities.Add(1);
                        if(j.workPositions==null)j.workPositions=new List<Vector3>();
                        if(j.sourceInspected==null)j.sourceInspected=new List<int>();
                        if(j.livestock==null)j.livestock=new List<LivestockRecord>();
                        if(j.cameraTargets==null)j.cameraTargets=new List<string>();
                        if(j.rarePlans==null)j.rarePlans=new List<RarePlan>();if(j.traceMarks==null)j.traceMarks=new List<int>();if(j.usedKits==null)j.usedKits=new List<int>();if(j.borrowedItems==null)j.borrowedItems=new List<string>();if(j.investigatedSpecies==null)j.investigatedSpecies=new List<string>();if(j.devices==null)j.devices=new List<DeviceRecord>();if(j.animals==null)j.animals=new List<AnimalRecord>();
                        if(j.layoutVersion<3){
                            j.layoutVersion=3;j.player=new Vector3(-14,.1f,-5);
                            if(!string.IsNullOrEmpty(j.carriedAnimal)){var a=j.animals.Find(x=>x.id==j.carriedAnimal);if(a!=null&&a.dead&&j.carriedSlot>=0)j.kit[j.carriedSlot]=(int)Kit.Corpse;}
                            foreach(int type in j.storedKit.Where(k=>k!=4&&k!=5&&k!=3))j.looseItems.Add(new LooseItemRecord{id=Guid.NewGuid().ToString("N"),kit=type,position=new Vector3(-18,1.1f,-4.3f)});
                            foreach(int type in new[]{4,5}) {
                                bool kept=false;
                                for(int k=0;k<j.devices.Count;k++)if(j.devices[k].cage==(type==5)){
                                    if(!kept){kept=true;continue;}
                                    var d=j.devices[k];var animal=j.animals.Find(a=>a.deviceId==d.id);
                                    if(animal!=null&&animal.contained){animal.contained=false;animal.deviceId=null;}
                                    j.devices.RemoveAt(k--);
                                }
                                for(int k=j.kit.Count-1;k>=0;k--)if(j.kit[k]==type){
                                    if(kept&&k!=j.carriedSlot){j.kit.RemoveAt(k);j.quantities.RemoveAt(k);if(j.carriedSlot>k)j.carriedSlot--;}
                                    else kept=true;
                                }
                                j.looseItems.RemoveAll(x=>x.kit==type);
                                if(!kept)j.looseItems.Add(new LooseItemRecord{id=Guid.NewGuid().ToString("N"),kit=type,position=new Vector3(-18+(type==4?.6f:-.6f),1.1f,-4.3f)});
                            }
                            j.cargoIds=new List<string>();j.baitedSlots=new List<bool>();
                            for(int k=0;k<j.kit.Count;k++){j.cargoIds.Add(k==j.carriedSlot?j.carriedAnimal:null);j.baitedSlots.Add(false);}
                            j.animals.RemoveAll(a=>a.species=="C01"&&!a.loaded&&!a.contained&&!a.dead);
                            if(j.stage==2)for(int k=j.animals.FindAll(a=>a.required).Count;k<5;k++)j.animals.Add(new AnimalRecord{id=j.runId+"-M01-"+k,species="M01",required=true,runId=j.runId,position=new Vector3(10,.08f,15+k*.5f)});
                            j.storedKit.Clear();j.layoutSeed=UnityEngine.Random.Range(1,int.MaxValue);
                            j.rareAt=UnityEngine.Random.Range(180f,300f);j.rareWillSpawn=UnityEngine.Random.value<.45f;
                        }
                    }
                    if(s.depotItems==null)s.depotItems=new List<LooseItemRecord>();
                    if(s.cart==null)s.cart=new List<int>();
                    if(s.saleHolding==null)s.saleHolding=new List<string>();if(s.donationHolding==null)s.donationHolding=new List<string>();
                    if(s.completedContracts==null)s.completedContracts=new List<string>();if(s.capturedRare==null)s.capturedRare=new List<string>();if(s.magazines==null)s.magazines=new List<MagazineRecord>();if(s.partners==null)s.partners=new List<PartnerRecord>();if(s.snakeLiners==null)s.snakeLiners=new List<string>();if(s.upgrades==null)s.upgrades=new List<string>();if(s.installations==null)s.installations=new List<InstalledService>();if(s.contractStars==null||s.contractStars.Length!=18){var old=s.contractStars;s.contractStars=new int[18];if(old!=null)Array.Copy(old,s.contractStars,Math.Min(old.Length,18));}
                    if(s.revision<4){
                        if(s.job!=null){var j=s.job;
                            if(j.itemIds.Count==0&&j.conditions.Count==0){
                                for(int i=0;i<j.kit.Count;i++){j.itemIds.Add(Guid.NewGuid().ToString("N"));j.conditions.Add(j.kit[i]==0?Mathf.Clamp((j.spray-j.spraySeconds/4f)*20,0,100):j.kit[i]==1?Mathf.Clamp(j.cleaning*20,0,100):j.kit[i]==2?Mathf.Clamp(j.repairs*20,0,100):100);}
                                j.cleaning=Mathf.Clamp(j.cleaning*20,0,100);j.repairs=Mathf.Clamp(j.repairs*20,0,100);
                                for(int i=0;i<j.kit.Count;i++)if(j.kit[i]==7||j.kit[i]==8||j.kit[i]==11)j.quantities[i]*=20;
                                foreach(var item in j.looseItems)if(item.kit==7||item.kit==8||item.kit==11)item.quantity*=20;
                            }
                        }
                        s.atDepot=s.job==null;s.starterGranted=s.job!=null||s.runs>0;s.revision=4;
                    }
                    if(path.EndsWith(".bak"))loadProblem="主存档无效，已载入备份。 / Main save invalid; backup loaded.";return s;
                } } }
                catch(Exception e) { Debug.LogWarning("Save recovery: "+e.Message); }
            }
            return new SaveRecord {cash=startingCash};
        }
        public static void Write(SaveRecord s)
        {
            if(protectedSavePath==PathName)throw new InvalidOperationException(loadProblem);
            Directory.CreateDirectory(Path.GetDirectoryName(PathName));
            File.WriteAllText(PathName+".tmp",JsonUtility.ToJson(s,true));
            if(File.Exists(PathName)) File.Replace(PathName+".tmp",PathName,PathName+".bak");
            else File.Move(PathName+".tmp",PathName);
        }
        public static int Price(DemoConfig c,AnimalRecord a) => a.dead?c.Species(a.species).corpsePrice:c.Species(a.species).livePrice;
        public static bool Trade(SaveRecord s,DemoConfig c,string id,bool donate)
        {
            var a=s.cargo.Find(x=>x.id==id && !x.terminal);
            if(a==null || (donate && a.dead)) return false;
            var copy=JsonUtility.FromJson<SaveRecord>(JsonUtility.ToJson(s));
            var target=copy.cargo.Find(x=>x.id==id); target.terminal=true;
            if(donate) copy.xp+=c.Species(a.species).donationXP; else copy.cash+=Price(c,a);
            copy.cargo.RemoveAll(x=>x.terminal);
            Write(copy);
            s.cash=copy.cash; s.xp=copy.xp; s.cargo=copy.cargo;
            return true;
        }
    }
}
