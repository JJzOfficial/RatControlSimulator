using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Builds the L01 greybox scene (house, furniture, work sites, mice, player, lighting).
// Runs automatically once (guarded by the scene file existing) and is re-runnable from the menu.
public static class SceneBuilder
{
    const string ScenePath = "Assets/Game/Scenes/L01.unity";

    [InitializeOnLoadMethod]
    static void AutoBuildOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScenePath)) return;
            Build();
        };
    }

    [MenuItem("Tools/RatControl/Build L01 Scene")]
    public static void BuildFromMenu() { Build(); }

    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory("Assets/Game/Scenes");

        // ---- materials ----
        Material floorMat = Mat(new Color(0.36f, 0.38f, 0.30f));
        Material wallMat  = Mat(new Color(0.66f, 0.64f, 0.60f));
        Material furnMat  = Mat(new Color(0.52f, 0.38f, 0.24f));
        Material mouseMat = Mat(new Color(0.45f, 0.34f, 0.24f));
        Material workMat  = Mat(new Color(0.85f, 0.55f, 0.15f));

        // ---- ground ----
        var ground = Box("Ground", new Vector3(0, -0.05f, 1), new Vector3(44, 0.1f, 40));
        ground.GetComponent<Renderer>().sharedMaterial = floorMat;
        ground.isStatic = true;

        // ---- house: walls (thickness 0.2, height 2.7) ----
        float H = 2.7f, T = 0.2f;
        // south wall with south door at X=0 (door width 1.0)
        Wall(wallMat, "Wall-South-L", new Vector3(-4.25f, H/2, -2), new Vector3(7.5f, H, T));
        Wall(wallMat, "Wall-South-R", new Vector3(4.25f, H/2, -2), new Vector3(7.5f, H, T));
        // north wall with back door at X=0
        Wall(wallMat, "Wall-North-L", new Vector3(-4.25f, H/2, 10), new Vector3(7.5f, H, T));
        Wall(wallMat, "Wall-North-R", new Vector3(4.25f, H/2, 10), new Vector3(7.5f, H, T));
        // east + west walls
        Wall(wallMat, "Wall-East", new Vector3(8, H/2, 4), new Vector3(T, H, 12));
        Wall(wallMat, "Wall-West", new Vector3(-8, H/2, 4), new Vector3(T, H, 12));

        // interior: hallway/kitchen boundary at X=1 (openings to kitchen Z~2 and north room Z~7)
        Wall(wallMat, "Wall-H-K1", new Vector3(1, H/2, 0.6f), new Vector3(T, H, 2.8f));
        Wall(wallMat, "Wall-H-K2", new Vector3(1, H/2, 4.2f), new Vector3(T, H, 1.6f));
        Wall(wallMat, "Wall-H-K3", new Vector3(1, H/2, 8.2f), new Vector3(T, H, 3.6f));
        // interior: hallway/storage boundary at X=-1
        Wall(wallMat, "Wall-H-S1", new Vector3(-1, H/2, 0.6f), new Vector3(T, H, 2.8f));
        Wall(wallMat, "Wall-H-S2", new Vector3(-1, H/2, 4.2f), new Vector3(T, H, 1.6f));
        // interior: kitchen/north-room boundary at Z=5
        Wall(wallMat, "Wall-KN", new Vector3(4.5f, H/2, 5), new Vector3(7f, H, T));
        // interior: storage/north-west boundary at Z=5
        Wall(wallMat, "Wall-SN", new Vector3(-4.5f, H/2, 5), new Vector3(7f, H, T));

        // ---- furniture (A01-A05) ----
        Furn("A01", new Vector3(3.5f, 0.4f, 1.5f), new Vector3(0.9f, 0.8f, 0.9f), furnMat);
        Furn("A02", new Vector3(4.5f, 0.5f, 4.2f), new Vector3(0.8f, 1.0f, 0.8f), furnMat);
        Furn("A03", new Vector3(-5f, 0.4f, 1.5f), new Vector3(0.9f, 0.8f, 0.9f), furnMat);
        Furn("A04", new Vector3(-6f, 0.5f, 4.2f), new Vector3(0.8f, 1.0f, 0.8f), furnMat);
        Furn("A05", new Vector3(5f, 0.5f, 8f), new Vector3(0.9f, 1.0f, 0.9f), furnMat);

        // ---- work sites ----
        Work("L01.W01", new Vector3(6, 0.4f, 2), workMat);
        Work("L01.W02", new Vector3(5, 0.4f, 14), workMat);
        Work("L01.W03", new Vector3(6, 0.4f, 8), workMat);
        Work("L01.F01", new Vector3(8.4f, 0.4f, 1), workMat);
        Work("L01.F02", new Vector3(-8.4f, 0.4f, 2), workMat);

        // ---- vehicle ----
        var van = Box("VAN", new Vector3(-12, 0.9f, -8), new Vector3(2.4f, 1.8f, 5f));
        van.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.75f, 0.75f, 0.78f));

        // ---- mice at spawn anchors (exterior) ----
        Mouse("Mouse-R01", new Vector3(10, 0.1f, 0), mouseMat,
            new Vector3[] { new Vector3(10,0.1f,0), new Vector3(11,0.1f,3), new Vector3(8.5f,0.1f,1), new Vector3(9,0.1f,-2) });
        Mouse("Mouse-R02", new Vector3(-10, 0.1f, 3), mouseMat,
            new Vector3[] { new Vector3(-10,0.1f,3), new Vector3(-11,0.1f,0), new Vector3(-8.5f,0.1f,2), new Vector3(-9,0.1f,-1) });
        Mouse("Mouse-R03", new Vector3(7, 0.1f, 14), mouseMat,
            new Vector3[] { new Vector3(7,0.1f,14), new Vector3(8,0.1f,16), new Vector3(5,0.1f,15), new Vector3(6,0.1f,12) });

        // ---- cage prefab (runtime template) ----
        var cage = Box("Cage", new Vector3(0, 0.15f, 0), new Vector3(0.4f, 0.3f, 0.5f));
        cage.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.3f, 0.6f, 0.35f));
        var cageCol = cage.GetComponent<BoxCollider>();
        cageCol.isTrigger = true;
        cageCol.size = new Vector3(2.0f, 1.4f, 2.4f);
        cage.AddComponent<CageTrap>();
        cage.SetActive(false);

        // ---- player ----
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 0.05f, -3.5f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.75f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.875f, 0);
        var camGO = new GameObject("Camera");
        camGO.transform.SetParent(player.transform, false);
        camGO.transform.localPosition = new Vector3(0, 1.52f, 0);
        camGO.tag = "MainCamera";
        camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        var pc = player.AddComponent<PlayerController>();
        pc.cagePrefab = cage;

        // ---- lighting ----
        var sunGO = new GameObject("Sun");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        sun.color = new Color(1f, 0.96f, 0.85f);
        sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

        // ---- game flow ----
        var gfGO = new GameObject("GameFlow");
        var gf = gfGO.AddComponent<GameFlow>();
        gf.sun = sun;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("[RatControl] L01 scene built and saved to " + ScenePath);
    }

    static Material Mat(Color c)
    {
        var sh = Shader.Find("HDRP/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", 0.4f);
        return m;
    }

    static GameObject Box(string name, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        return go;
    }

    static void Wall(Material m, string name, Vector3 pos, Vector3 scale)
    {
        var w = Box(name, pos, scale);
        w.GetComponent<Renderer>().sharedMaterial = m;
        w.isStatic = true;
    }

    static void Furn(string id, Vector3 pos, Vector3 scale, Material m)
    {
        var f = Box("Furniture-" + id, pos, scale);
        f.GetComponent<Renderer>().sharedMaterial = m;
        var comp = f.AddComponent<Furniture>();
        comp.assetId = id;
    }

    static void Work(string id, Vector3 pos, Material m)
    {
        var w = Box("Work-" + id, pos, new Vector3(0.5f, 0.8f, 0.5f));
        w.GetComponent<Renderer>().sharedMaterial = m;
        var comp = w.AddComponent<WorkSite>();
        comp.siteId = id;
    }

    static void Mouse(string name, Vector3 pos, Material m, Vector3[] wps)
    {
        var root = new GameObject(name);
        root.transform.position = pos;
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.14f, 0.1f, 0.18f);
        body.GetComponent<Renderer>().sharedMaterial = m;
        var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.name = "Tail";
        tail.transform.SetParent(root.transform, false);
        tail.transform.localPosition = new Vector3(0, 0.02f, -0.14f);
        tail.transform.localScale = new Vector3(0.02f, 0.02f, 0.14f);
        tail.GetComponent<Renderer>().sharedMaterial = m;

        var c = root.AddComponent<Creature>();
        c.waypoints = System.Array.ConvertAll(wps, p => Wp(p));
    }

    static Transform Wp(Vector3 p)
    {
        var go = new GameObject("Waypoint");
        go.transform.position = p;
        return go.transform;
    }
}
