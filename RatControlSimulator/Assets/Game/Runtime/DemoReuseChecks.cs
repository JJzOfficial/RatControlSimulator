#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;
namespace Riverbend {
    public partial class DemoGame {
        public void RunP0ReuseChecks(){StartVerification(P0ReuseChecks(),"accepted-v4");}
        IEnumerator P0ReuseChecks(){
            bool previous=frontend;frontend=false;SyncPause();
            yield return V4Checks();
            frontend=previous;SyncPause();
        }
    }
}
#endif
