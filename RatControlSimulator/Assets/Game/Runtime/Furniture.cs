using UnityEngine;

public class Furniture : MonoBehaviour
{
    public string assetId = "";
    public int durability = 100;
    public const int MaxDurability = 100;

    public void ApplyBite(int amount)
    {
        durability = Mathf.Max(0, durability - amount);
        if (GameFlow.Instance) GameFlow.Instance.RefreshFurniture();
    }
}
