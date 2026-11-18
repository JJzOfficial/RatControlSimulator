using UnityEngine;

// Mouse (M01) and other small creatures for the P0 test slice.
// Dormant during day, activates at night, patrols waypoints, flees when it sees the player, then hides.
public class Creature : MonoBehaviour
{
    public enum State { Dormant, Patrol, Flee, Hide, Dead, Captured }

    public State CurrentState { get; private set; } = State.Dormant;
    public bool Handled { get; private set; }          // killed or captured -> counts toward the contract
    public bool LiveCaptured { get; private set; }

    public float runSpeed = 2f;
    public float feedSpeed = 0.7f;
    public float sightRange = 6f;
    public float viewAngle = 120f;
    public float fleeSeconds = 4f;
    public float hideMin = 3f;
    public float hideMax = 6f;
    public Transform[] waypoints;

    Transform player;
    int wpIndex;
    Vector3 fleeDir;
    float stateTimer;
    Collider col;
    Renderer[] renderers;
    Transform body;

    void Awake()
    {
        col = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        body = transform.Find("Body") != null ? transform.Find("Body") : transform;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player") != null ? GameObject.FindGameObjectWithTag("Player").transform : null;
        SetDormantVisuals();
    }

    public void OnNightBegin()
    {
        if (CurrentState != State.Dormant) return;
        CurrentState = State.Patrol;
        SetActiveVisuals();
        wpIndex = 0;
    }

    void Update()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        switch (CurrentState)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Flee: FleeUpdate(); break;
            case State.Hide: HideUpdate(); break;
        }
    }

    void PatrolUpdate()
    {
        if (CanSeePlayer()) { StartFlee(); return; }
        if (waypoints == null || waypoints.Length == 0) return;
        Vector3 target = waypoints[wpIndex].position;
        target.y = transform.position.y;
        if (Vector3.Distance(transform.position, target) < 0.35f)
            wpIndex = (wpIndex + 1) % waypoints.Length;
        MoveTowards(target, feedSpeed);
    }

    void FleeUpdate()
    {
        stateTimer -= Time.deltaTime;
        MoveTowards(transform.position + fleeDir, runSpeed);
        if (stateTimer <= 0f)
        {
            CurrentState = State.Hide;
            stateTimer = Random.Range(hideMin, hideMax);
        }
    }

    void HideUpdate()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) CurrentState = State.Patrol;
    }

    void StartFlee()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        fleeDir = -toPlayer.normalized;
        CurrentState = State.Flee;
        stateTimer = fleeSeconds;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.magnitude > sightRange) return false;
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > viewAngle * 0.5f) return false;

        Vector3 eye = transform.position + Vector3.up * 0.15f;
        Vector3 target = player.position + Vector3.up * 0.6f;
        Vector3 dir = target - eye;
        if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dir.magnitude))
        {
            if (!hit.collider.CompareTag("Player")) return false;
        }
        return true;
    }

    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.magnitude < 0.01f) return;
        transform.position += dir.normalized * speed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * 8f);
    }

    public void Die()
    {
        if (Handled || CurrentState == State.Dead || CurrentState == State.Captured) return;
        CurrentState = State.Dead;
        Handled = true;
        LiveCaptured = false;
        if (col) col.enabled = false;
        if (body) body.localRotation = Quaternion.Euler(90f, 0f, 0f);
        if (GameFlow.Instance) GameFlow.Instance.NotifyCreatureHandled(this, false);
    }

    public void Capture()
    {
        if (Handled || CurrentState == State.Dead || CurrentState == State.Captured) return;
        CurrentState = State.Captured;
        Handled = true;
        LiveCaptured = true;
        if (col) col.enabled = false;
        SetDormantVisuals();
        if (GameFlow.Instance) GameFlow.Instance.NotifyCreatureHandled(this, true);
    }

    void SetDormantVisuals()
    {
        foreach (var r in renderers) if (r) r.enabled = false;
        if (col) col.enabled = false;
    }

    void SetActiveVisuals()
    {
        foreach (var r in renderers) if (r) r.enabled = true;
        if (col) col.enabled = true;
    }
}
