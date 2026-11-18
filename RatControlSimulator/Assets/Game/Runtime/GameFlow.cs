using UnityEngine;
using UnityEngine.InputSystem;

// Day/night phase state machine, objective tracking and a simple OnGUI HUD.
public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    public enum Phase { Day, Night, Completed, Failed }
    public Phase CurrentPhase { get; private set; } = Phase.Day;

    public const int RequiredMice = 3;
    public int handledMice;

    public bool IsPaused { get; private set; }

    public Light sun;

    int furnitureMax;
    int furnitureCurrent;
    int workDone;
    int workTotal;
    string status = "";

    void Awake() { Instance = this; }

    void Start()
    {
        var furs = Object.FindObjectsByType<Furniture>(FindObjectsSortMode.None);
        furnitureMax = furs.Length * Furniture.MaxDurability;
        var ws = Object.FindObjectsByType<WorkSite>(FindObjectsSortMode.None);
        workTotal = ws.Length;
        RefreshFurniture();
        ApplyPhaseVisuals();
        status = "Day: hold E on work sites (" + workTotal + " total), then press N to begin night";
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) TogglePause();
        if (kb != null && kb.nKey.wasPressedThisFrame && CurrentPhase == Phase.Day) BeginNight();
    }

    public void BeginNight()
    {
        if (CurrentPhase != Phase.Day) return;
        CurrentPhase = Phase.Night;
        ApplyPhaseVisuals();
        foreach (var c in Object.FindObjectsByType<Creature>(FindObjectsSortMode.None)) c.OnNightBegin();
        status = "Night: catch " + RequiredMice + " mice (left-click to shoot, or T to drop a cage)";
    }

    public void NotifyWorkDone(WorkSite ws)
    {
        workDone++;
        status = "Day: work " + workDone + "/" + workTotal + " done. Press N to begin night.";
    }

    public void RefreshFurniture()
    {
        furnitureCurrent = 0;
        foreach (var f in Object.FindObjectsByType<Furniture>(FindObjectsSortMode.None)) furnitureCurrent += f.durability;
        if (CurrentPhase == Phase.Night && furnitureMax > 0 && (float)furnitureCurrent / furnitureMax < 0.60f)
        {
            CurrentPhase = Phase.Failed;
            status = "FAILED: furniture dropped below 60%";
        }
    }

    public void NotifyCreatureHandled(Creature c, bool live)
    {
        handledMice++;
        status = "Mice handled: " + handledMice + "/" + RequiredMice;
        if (handledMice >= RequiredMice && CurrentPhase == Phase.Night)
        {
            CurrentPhase = Phase.Completed;
            status = "SUCCESS! Contract complete: +150 cash, +120 XP";
        }
    }

    void TogglePause()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsPaused;
    }

    void ApplyPhaseVisuals()
    {
        if (sun == null) return;
        if (CurrentPhase == Phase.Night)
        {
            sun.color = new Color(0.35f, 0.45f, 0.85f);
            sun.intensity = 0.30f;
            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.13f);
        }
        else
        {
            sun.color = new Color(1f, 0.96f, 0.85f);
            sun.intensity = 1.15f;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.50f);
        }
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 520, 64), "");
        GUI.Label(new Rect(22, 20, 500, 22), "Phase: " + CurrentPhase);
        GUI.Label(new Rect(22, 44, 500, 22), status);
        GUI.Label(new Rect(Screen.width - 260, 10, 250, 40),
            "Mice: " + handledMice + "/" + RequiredMice + "   Furniture: " + furnitureCurrent + "/" + furnitureMax);
    }
}
