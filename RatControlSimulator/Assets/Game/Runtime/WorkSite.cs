using UnityEngine;

// Day-phase objective site (insect source W01/W02, leak W03, or seal point F01/F02).
// Player holds E to complete. For the P0 test slice, no consumable cost is enforced.
public class WorkSite : MonoBehaviour
{
    public string siteId = "";
    public float duration = 2f;
    public bool IsCompleted { get; private set; }
    public float Progress { get; private set; }

    public void Interact(float dt)
    {
        if (IsCompleted) return;
        Progress += dt / duration;
        if (Progress >= 1f)
        {
            Progress = 1f;
            IsCompleted = true;
            if (GameFlow.Instance) GameFlow.Instance.NotifyWorkDone(this);
        }
    }
}
