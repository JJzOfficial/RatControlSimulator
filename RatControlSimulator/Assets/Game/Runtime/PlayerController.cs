using UnityEngine;
using UnityEngine.InputSystem;

// First-person controller for the P0 test slice.
// Movement: WASD, Shift sprint, Ctrl crouch, Space jump. Mouse look. Left-click shoot. Hold E interact. T place cage.
public class PlayerController : MonoBehaviour
{
    CharacterController cc;
    Camera cam;

    float yaw, pitch;
    Vector3 velocity;
    float stamina = 100f;
    bool crouching;

    public float mouseSensitivity = 2.0f;
    public float shootRange = 100f;
    public float shootCooldown = 0.4f;
    float shootTimer;

    public GameObject cagePrefab; // assigned by SceneBuilder

    WorkSite hoverSite;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (GameFlow.Instance != null && GameFlow.Instance.IsPaused) return;
        HandleLook();
        HandleMove();
        HandleShoot();
        HandleInteract();
        HandleCage();
    }

    void HandleLook()
    {
        var m = Mouse.current;
        if (m == null || Cursor.lockState != CursorLockMode.Locked) return;
        Vector2 d = m.delta.ReadValue() * mouseSensitivity * 0.1f;
        yaw += d.x;
        pitch -= d.y;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (cam) cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMove()
    {
        var kb = Keyboard.current;
        Vector2 move = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) move.y += 1;
            if (kb.sKey.isPressed) move.y -= 1;
            if (kb.dKey.isPressed) move.x += 1;
            if (kb.aKey.isPressed) move.x -= 1;
        }

        bool sprint = kb != null && kb.leftShiftKey.isPressed && move.y > 0 && stamina > 0f;
        crouching = kb != null && kb.leftCtrlKey.isPressed;

        float speed = crouching ? 1.5f : (sprint ? 5f : 3f);
        if (sprint) stamina = Mathf.Max(0f, stamina - 12.5f * Time.deltaTime);
        else stamina = Mathf.Min(100f, stamina + 20f * Time.deltaTime);

        Vector3 horiz = (transform.right * move.x + transform.forward * move.y) * speed;

        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        if (cc.isGrounded && kb != null && kb.spaceKey.wasPressedThisFrame)
            velocity.y = Mathf.Sqrt(0.35f * -2f * -20f);
        velocity.y += -20f * Time.deltaTime;

        float targetH = crouching ? 1.10f : 1.75f;
        cc.height = Mathf.Lerp(cc.height, targetH, Time.deltaTime * 12f);
        cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
        if (cam) cam.transform.localPosition = new Vector3(0f, crouching ? 0.92f : 1.52f, 0f);

        cc.Move((horiz + velocity) * Time.deltaTime);
    }

    Ray CenterRay()
    {
        if (cam) return cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        return new Ray(transform.position + Vector3.up * 1.5f, transform.forward);
    }

    void HandleShoot()
    {
        shootTimer -= Time.deltaTime;
        var m = Mouse.current;
        if (m == null || !m.leftButton.wasPressedThisFrame || shootTimer > 0f) return;
        if (GameFlow.Instance != null && GameFlow.Instance.CurrentPhase != GameFlow.Phase.Night) return;
        shootTimer = shootCooldown;
        Ray ray = CenterRay();
        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            var c = hit.collider.GetComponentInParent<Creature>();
            if (c != null) c.Die();
        }
    }

    void HandleInteract()
    {
        var kb = Keyboard.current;
        hoverSite = null;
        Ray ray = CenterRay();
        if (Physics.Raycast(ray, out RaycastHit hit, 2.5f))
            hoverSite = hit.collider.GetComponentInParent<WorkSite>();

        if (hoverSite != null && !hoverSite.IsCompleted && kb != null && kb.eKey.isPressed)
            hoverSite.Interact(Time.deltaTime);
    }

    void HandleCage()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.tKey.wasPressedThisFrame) return;
        if (cagePrefab == null) return;
        Vector3 pos = transform.position + transform.forward * 1.2f + Vector3.up * 0.1f;
        pos.y = 0.1f;
        var cage = Object.Instantiate(cagePrefab, pos, Quaternion.identity);
        cage.SetActive(true);
        cage.GetComponent<CageTrap>().Arm();
    }

    void OnGUI()
    {
        if (hoverSite != null && !hoverSite.IsCompleted)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 120f, Screen.height * 0.5f + 30f, 240f, 28f),
                hoverSite.siteId + "  " + Mathf.RoundToInt(hoverSite.Progress * 100f) + "% (hold E)");
        }
    }
}
