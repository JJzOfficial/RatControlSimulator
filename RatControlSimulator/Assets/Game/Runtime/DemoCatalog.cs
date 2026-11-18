using System;
using System.Linq;
using UnityEngine;
namespace Riverbend {
    // Data appended to P0; the original player, save and game loop remain in use.
    [Serializable] public class DemoCatalog {
        public string configVersion;
        public Species[] species;
        public Item[] items;
        public Contract[] contracts;
        public RareEvent[] rareEvents;
        public Level[] levels;
        public Loan[] loans;
        public Skill[] skills;
        public Companion[] companions;
        public Service[] services;
        [Serializable] public class Service {public string id,zh,target,finalValue;public int unlockLevel,price;}
        [Serializable] public class Companion {public string id,zh,en;public int price,unlockLevel,maximumOwned;public float radiusM,chaseSeconds,restSeconds,barkRadiusM,barkWindupSeconds,barkCooldownSeconds;public string[] targetSpecies;}
        [Serializable] public class Skill {public string id,branch,zh,effectKey;public float[] rankFinalValues;public int[] rankPointCosts,levelGates;public int branchPointsRequired;}
        [Serializable] public class Loan {public string id;public string[] items,selectOne;public Supply[] supplies;}
        [Serializable] public class Supply {public string id;public int count;}
        [Serializable] public class Level {public int level,totalXP,skillPointsGranted,extraSlotsGranted;}
        [Serializable] public class Species {
            public string id,zh,en,family,size,navigation,investigation;
            public float lengthM,walkSpeed,fleeSpeed,visionM,visionFovDeg,hearingM,health;
            public int livePrice,corpsePrice,donationXP;
            public bool attackPlayer;
            public string[] captureMethods;
        }
        [Serializable] public class Item {public string id,zh,en,kind,platformId;public int price,unlockLevel,emptySlots,maxUsesPerPackage;}
        [Serializable] public class Budget {public string speciesId,role,phase;public int count;public float activationSeconds;}
        [Serializable] public class Star {public string id,kind,threshold,zh;}
        [Serializable] public class Contract {
            public string id,zh,en,mapId,type,unlockAfter,mainPolicy,abortPolicy;
            public string[] loans;
            public Budget[] targetBudget;
            public string[] guaranteedInsects,requiredWorks,forbiddenMainTools;
            public Star[] starPredicates;
            public float durationSeconds,protectionMinimum;
            public int rewardCash,rewardXP,firstStarCash,firstStarXP;
            public int RequiredCount=>targetBudget.Where(b=>b.role=="Required"||b.role=="Delivery").Sum(b=>b.count);
        }
        [Serializable] public class RareEvent {public string speciesId,hostContractId,mapId;public float probability;public float[] delaySecondsRange;public bool firstClueGuarantee;}
        public static DemoCatalog Load(){var asset=Resources.Load<TextAsset>("ContinuationCatalog");if(!asset)throw new InvalidOperationException("Missing continuation catalog");var c=JsonUtility.FromJson<DemoCatalog>(asset.text);if(c.contracts.Length!=18||c.species.Length!=45||c.items.Length!=30)throw new InvalidOperationException("Incomplete continuation catalog");return c;}
    }
}
