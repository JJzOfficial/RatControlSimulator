using UnityEngine;

// Small cage: once armed, the first creature entering its trigger volume is captured.
public class CageTrap : MonoBehaviour
{
    public bool Armed { get; private set; }
    public Creature Captured { get; private set; }

    public void Arm() { Armed = true; }

    void OnTriggerEnter(Collider other)
    {
        if (!Armed || Captured != null) return;
        var c = other.GetComponentInParent<Creature>();
        if (c != null)
        {
            Captured = c;
            c.Capture();
        }
    }
}
