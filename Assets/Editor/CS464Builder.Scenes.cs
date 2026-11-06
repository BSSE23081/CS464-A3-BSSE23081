using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Scene-building helpers. Every level is a short list of calls like Floor(...), Coin(...), Guard(...),
// so you can read a level as its layout, and change a number and rebuild.
public static partial class CS464Builder
{
    class Config
    {
        public string title = "Level";
        public string intro = "";
        public Objective objective = Objective.CollectCoins;
        public int coinsRequired = 8;
        public float surviveSeconds = 0f;
        public float timeLimit = 0f;
        public int lives = 3;
        public string next = "";
    }

    // The prefabs, loaded once per build.
    static GameObject pPlayer, pCoin, pGuard, pSpikes, pCrusher, pSweeper, pBounce, pGoal, pCheckpoint, pSpeed, pShield, pKill, pGM, pHud;

    // The current scene.
    static Transform tGeo, tCoins, tHaz, tGuards, tPick, tWaypoints, tMisc;
    static Material mFloor, mWall;
    static Transform startT;
    static GameObject playerGo;
    static float sceneCenterX, sceneCenterZ;

    static void LoadPrefabs()
    {
        pPlayer = Load("Player"); pCoin = Load("Coin"); pGuard = Load("Guard"); pSpikes = Load("Spikes");
        pCrusher = Load("Crusher"); pSweeper = Load("Sweeper"); pBounce = Load("BouncePad"); pGoal = Load("Goal");
        pCheckpoint = Load("Checkpoint"); pSpeed = Load("PowerUp_Speed"); pShield = Load("PowerUp_Shield");
        pKill = Load("KillZone"); pGM = Load("GameManager"); pHud = Load("HUD");
    }

    static GameObject Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + name + ".prefab");
    }

    static Transform Group(string name)
    {
        return new GameObject(name).transform;
    }

    static void Rec(Object o)
    {
        if (o != null) PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }

    static GameObject Inst(GameObject prefab, Transform parent, Vector3 pos)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        Rec(go.transform);
        return go;
    }

    // Plain scene object that was made from a prefab and then resized: unpack so the edits are ordinary scene data.
    static GameObject InstUnpacked(GameObject prefab, Transform parent, Vector3 pos)
    {
        GameObject go = Inst(prefab, parent, pos);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        return go;
    }

    // ------------------------------------------------------------------ one scene
    static void BeginScene(Color floor, Color wall)
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        tGeo = Group("Geometry");
        tCoins = Group("Coins");
        tHaz = Group("Hazards");
        tGuards = Group("Guards");
        tPick = Group("Pickups");
        tWaypoints = Group("Waypoints");
        tMisc = Group("Misc");
        mFloor = M("Floor_" + ColorKey(floor), floor);
        mWall = M("Wall_" + ColorKey(wall), wall);
        startT = null;
        playerGo = null;
        sceneCenterX = 0f;
        sceneCenterZ = 0f;
    }

    static string ColorKey(Color c)
    {
        return ((int)(c.r * 255f)).ToString("X2") + ((int)(c.g * 255f)).ToString("X2") + ((int)(c.b * 255f)).ToString("X2");
    }

    // ---- geometry
    static void Floor(float x0, float x1, float z0, float z1, float top = 0f, float thick = 2f)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "Floor";
        g.transform.SetParent(tGeo, false);
        g.transform.position = new Vector3((x0 + x1) * 0.5f, top - thick * 0.5f, (z0 + z1) * 0.5f);
        g.transform.localScale = new Vector3(x1 - x0, thick, z1 - z0);
        g.GetComponent<Renderer>().sharedMaterial = mFloor;
    }

    static void Wall(float x0, float x1, float z0, float z1, float height = 2.5f, float baseY = 0f)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "Wall";
        g.transform.SetParent(tGeo, false);
        g.transform.position = new Vector3((x0 + x1) * 0.5f, baseY + height * 0.5f, (z0 + z1) * 0.5f);
        g.transform.localScale = new Vector3(x1 - x0, height, z1 - z0);
        g.GetComponent<Renderer>().sharedMaterial = mWall;
        g.GetComponent<Collider>().sharedMaterial = WallSlippery();
    }

    // ---- things to collect and reach
    static void Coin(float x, float z, float top = 0f)
    {
        Inst(pCoin, tCoins, new Vector3(x, top + 0.8f, z));
    }

    static void Boost(float x, float z, float top = 0f) { Inst(pSpeed, tPick, new Vector3(x, top + 0.9f, z)); }
    static void Shield(float x, float z, float top = 0f) { Inst(pShield, tPick, new Vector3(x, top + 0.9f, z)); }
    static void Bounce(float x, float z, float top = 0f) { Inst(pBounce, tMisc, new Vector3(x, top, z)); }
    static void GoalAt(float x, float z, float top = 0f) { Inst(pGoal, tMisc, new Vector3(x, top, z)); }
    static void CheckpointAt(float x, float z, float top = 0f) { Inst(pCheckpoint, tMisc, new Vector3(x, top, z)); }

    // ---- hazards
    static void Spikes(float x0, float x1, float z0, float z1, float top = 0f)
    {
        float w = x1 - x0, d = z1 - z0;
        GameObject go = InstUnpacked(pSpikes, tHaz, new Vector3((x0 + x1) * 0.5f, top, (z0 + z1) * 0.5f));
        go.name = "Spikes";
        BoxCollider bc = go.GetComponent<BoxCollider>();
        bc.size = new Vector3(w, 0.8f, d);
        bc.center = new Vector3(0f, 0.4f, 0f);
        Transform b = go.transform.Find("Base");
        b.localScale = new Vector3(w, 0.15f, d);
        b.localPosition = new Vector3(0f, 0.075f, 0f);

        Material tooth = M("SpikeTooth", new Color(0.95f, 0.25f, 0.18f), 0.3f);
        for (float x = -w * 0.5f + 0.4f; x <= w * 0.5f - 0.2f; x += 0.8f)
        {
            for (float z = -d * 0.5f + 0.4f; z <= d * 0.5f - 0.2f; z += 0.8f)
            {
                GameObject t = Prim(PrimitiveType.Cube, "Tooth", go.transform, new Vector3(x, 0.3f, z), Vector3.one * 0.32f, tooth, false);
                t.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            }
        }
    }

    static void Crusher(float cx, float cz, float w, float d, float rest, float warn, float strike, float delay, float top = 0f)
    {
        GameObject go = InstUnpacked(pCrusher, tHaz, new Vector3(cx, top, cz));
        go.name = "Crusher";
        BoxCollider bc = go.GetComponent<BoxCollider>();
        bc.size = new Vector3(w, 1.4f, d);
        bc.center = new Vector3(0f, 0.7f, 0f);

        Transform b = go.transform.Find("Base");
        b.localScale = new Vector3(w, 0.12f, d);
        Transform wn = go.transform.Find("Warning");
        wn.localScale = new Vector3(w - 0.2f, 0.14f, d - 0.2f);
        Transform s = go.transform.Find("Slab");
        s.localScale = new Vector3(w, 1.4f, d);

        Hazard h = go.GetComponent<Hazard>();
        h.restTime = rest;
        h.warnTime = warn;
        h.strikeTime = strike;
        h.startDelay = delay;
    }

    static void Sweeper(float cx, float cz, float sizeX, float sizeZ, Vector3 axis, float distance, float speed, float top = 0f)
    {
        GameObject go = InstUnpacked(pSweeper, tHaz, new Vector3(cx, top, cz));
        go.name = "Sweeper";
        BoxCollider bc = go.GetComponent<BoxCollider>();
        bc.size = new Vector3(sizeX, 1.2f, sizeZ);
        bc.center = new Vector3(0f, 0.6f, 0f);
        go.transform.Find("Visual").localScale = new Vector3(sizeX, 1.2f, sizeZ);
        MovingHazard m = go.GetComponent<MovingHazard>();
        m.axis = axis;
        m.distance = distance;
        m.speed = speed;
    }

    // ---- guards: waypoints are (x, z) pairs
    static void Guard(string name, PatrolMode mode, float patrol, float chase, float range, float giveUp, bool canChase, float wait, float top, params Vector2[] pts)
    {
        GameObject g = Inst(pGuard, tGuards, new Vector3(pts[0].x, top + 1f, pts[0].y));
        g.name = name;

        Transform parent = new GameObject(name + "_Waypoints").transform;
        parent.SetParent(tWaypoints, false);
        Transform[] wp = new Transform[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            GameObject p = new GameObject(name + "_WP" + i);
            p.transform.SetParent(parent, false);
            p.transform.position = new Vector3(pts[i].x, top + 1f, pts[i].y);
            wp[i] = p.transform;
        }

        EnemyGuard eg = g.GetComponent<EnemyGuard>();
        eg.points = wp;
        eg.mode = mode;
        eg.patrolSpeed = patrol;
        eg.chaseSpeed = chase;
        eg.chaseRange = range;
        eg.giveUpRange = giveUp;
        eg.canChase = canChase;
        eg.waitTime = wait;
        Rec(eg);
    }

    // ---- the player and the end of the scene
    static void StartAt(float x, float z, float top = 0f)
    {
        GameObject s = new GameObject("PlayerStart");
        s.transform.SetParent(tMisc, false);
        s.transform.position = new Vector3(x, top + 0.6f, z);
        startT = s.transform;
        playerGo = Inst(pPlayer, null, s.transform.position);
        playerGo.name = "Player";
    }

    static void Finish(string sceneName, Config cfg, float centerX, float centerZ)
    {
        // Kill zone: one wide trigger under the level; a fall costs a life.
        GameObject kz = InstUnpacked(pKill, tMisc, new Vector3(centerX, -10f, centerZ));
        kz.name = "KillZone";
        kz.GetComponent<BoxCollider>().size = new Vector3(400f, 2f, 400f);

        // Camera.
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null && cam.GetComponent<CameraFollow>() == null) cam.gameObject.AddComponent<CameraFollow>();

        // GameManager.
        GameObject gmGo = Inst(pGM, null, Vector3.zero);
        GameManager gm = gmGo.GetComponent<GameManager>();
        gm.objective = cfg.objective;
        gm.coinsRequired = cfg.coinsRequired;
        gm.surviveSeconds = cfg.surviveSeconds;
        gm.timeLimit = cfg.timeLimit;
        gm.levelTitle = cfg.title;
        gm.intro = cfg.intro;
        gm.lives = cfg.lives;
        gm.nextSceneName = cfg.next;
        gm.player = playerGo.GetComponent<Health>();
        gm.respawnPoint = startT;

        // HUD and references.
        GameObject hud = Inst(pHud, null, Vector3.zero);
        gm.scoreText = hud.transform.Find("ScoreText").GetComponent<TMP_Text>();
        gm.objectiveText = hud.transform.Find("ObjectiveText").GetComponent<TMP_Text>();
        gm.livesText = hud.transform.Find("LivesText").GetComponent<TMP_Text>();
        gm.timerText = hud.transform.Find("TimerText").GetComponent<TMP_Text>();
        gm.messageText = hud.transform.Find("MessageText").GetComponent<TMP_Text>();
        gm.winPanel = hud.transform.Find("WinPanel").gameObject;
        gm.losePanel = hud.transform.Find("LosePanel").gameObject;
        gm.winDetail = hud.transform.Find("WinPanel/Detail").GetComponent<TMP_Text>();
        gm.loseDetail = hud.transform.Find("LosePanel/Detail").GetComponent<TMP_Text>();
        gm.nextButton = hud.transform.Find("WinPanel/NextButton").gameObject;
        gm.nextButtonLabel = hud.transform.Find("WinPanel/NextButton/Label").GetComponent<TMP_Text>();
        gm.nextButtonLabel.text = cfg.next == "Level01" ? "Play again (Enter)" : "Next Level (Enter)";
        Rec(gm);
        Rec(gm.nextButtonLabel);

        // EventSystem, so the Restart and Next buttons can be clicked.
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New Input System only: the UI module needs its default actions assigned, or clicks do nothing.
        Component module = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        System.Reflection.MethodInfo assign = module.GetType().GetMethod("AssignDefaultActions");
        if (assign != null) assign.Invoke(module, null);
#else
        es.AddComponent<StandaloneInputModule>();
#endif

        string path = SceneDir + "/" + sceneName + ".unity";
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
    }
}
