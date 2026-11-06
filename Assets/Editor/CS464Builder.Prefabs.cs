using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Builds the materials and prefabs: Player, Coin, Guard, Spikes, Crusher, Sweeper, BouncePad,
// Goal, Checkpoint, PowerUps, KillZone, GameManager and the HUD. Run from the CS464 menu.
public static partial class CS464Builder
{
    const string PrefabDir = "Assets/Prefabs";
    const string SceneDir = "Assets/Scenes";
    const string MatDir = "Assets/Materials";

    // ------------------------------------------------------------------ small helpers
    static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
    }

    static Material M(string name, Color c, float emission = 0f)
    {
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (emission > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // Walls are slippery so the ball slides along them instead of sticking; floors keep their friction so it rolls.
    static PhysicsMaterial WallSlippery()
    {
        string path = MatDir + "/WallSlippery.asset";
        PhysicsMaterial pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial("WallSlippery");
            AssetDatabase.CreateAsset(pm, path);
        }
        pm.dynamicFriction = 0f;
        pm.staticFriction = 0f;
        pm.bounciness = 0f;
        pm.frictionCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(pm);
        return pm;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return g;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static T Trigger<T>(GameObject go) where T : Collider
    {
        T c = go.AddComponent<T>();
        c.isTrigger = true;
        return c;
    }

    // ------------------------------------------------------------------ the prefabs
    static void BuildPrefabs()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Materials");

        Material player = M("Player", new Color(0.10f, 0.75f, 0.70f));
        Material coin = M("Coin", new Color(1.00f, 0.82f, 0.10f), 0.5f);
        Material guard = M("Guard", new Color(1.00f, 0.45f, 0.10f));
        Material guardEye = M("GuardEye", new Color(1f, 1f, 1f));
        Material spikeBase = M("SpikeBase", new Color(0.35f, 0.08f, 0.08f));
        Material crusherBase = M("CrusherBase", new Color(0.22f, 0.18f, 0.18f));
        Material warning = M("Warning", new Color(1.00f, 0.15f, 0.10f), 0.8f);
        Material slab = M("CrusherSlab", new Color(0.75f, 0.10f, 0.10f));
        Material sweeper = M("Sweeper", new Color(0.95f, 0.35f, 0.10f));
        Material bounce = M("BouncePad", new Color(0.30f, 0.90f, 0.40f), 0.5f);
        Material goal = M("Goal", new Color(0.20f, 1.00f, 0.50f), 0.7f);
        Material pole = M("CheckpointPole", new Color(0.7f, 0.7f, 0.7f));
        Material flag = M("CheckpointFlag", new Color(1.0f, 0.85f, 0.2f), 0.3f);
        Material speedMat = M("PowerSpeed", new Color(0.20f, 0.90f, 1.00f), 0.8f);
        Material shieldMat = M("PowerShield", new Color(0.30f, 0.50f, 1.00f), 0.8f);

        // Player: tagged Player, a non-kinematic Rigidbody (what makes triggers fire), controller and Health.
        {
            GameObject go = Prim(PrimitiveType.Sphere, "Player", null, Vector3.zero, Vector3.one, player, true);
            go.tag = "Player";
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;   // a free-rolling ball: rotation is not frozen
            go.AddComponent<PlayerController>();
            go.AddComponent<Health>();
            SavePrefab(go, "Player");
        }

        // Coin: a trigger with Coin.cs.
        {
            GameObject go = new GameObject("Coin");
            SphereCollider sc = Trigger<SphereCollider>(go);
            sc.radius = 0.6f;
            go.AddComponent<Coin>();
            GameObject vis = Prim(PrimitiveType.Cylinder, "Visual", go.transform, Vector3.zero, new Vector3(0.7f, 0.06f, 0.7f), coin, false);
            vis.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);   // a disc standing on its edge, so the spin shows
            SavePrefab(go, "Coin");
        }

        // Guard: kinematic Rigidbody, a trigger capsule, Hazard.cs and EnemyGuard.cs.
        {
            GameObject go = new GameObject("Guard");
            CapsuleCollider cc = Trigger<CapsuleCollider>(go);
            cc.radius = 0.55f;
            cc.height = 2f;
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            Hazard h = go.AddComponent<Hazard>();
            h.damage = 1;
            h.knockback = 8f;
            go.AddComponent<EnemyGuard>();
            Prim(PrimitiveType.Capsule, "Body", go.transform, Vector3.zero, new Vector3(1.1f, 1f, 1.1f), guard, false);
            Prim(PrimitiveType.Cube, "Eyes", go.transform, new Vector3(0f, 0.45f, 0.5f), new Vector3(0.7f, 0.18f, 0.2f), guardEye, false);
            SavePrefab(go, "Guard");
        }

        // Spikes: a trigger box and Hazard.cs. The level builder resizes it and adds the teeth.
        {
            GameObject go = new GameObject("Spikes");
            BoxCollider bc = Trigger<BoxCollider>(go);
            bc.size = new Vector3(1f, 0.8f, 1f);
            bc.center = new Vector3(0f, 0.4f, 0f);
            Hazard h = go.AddComponent<Hazard>();
            h.damage = 1;
            Prim(PrimitiveType.Cube, "Base", go.transform, new Vector3(0f, 0.075f, 0f), new Vector3(1f, 0.15f, 1f), spikeBase, false);
            SavePrefab(go, "Spikes");
        }

        // Crusher: off for restTime, warning for warnTime (0.7 s), then a strike for 2 damage.
        {
            GameObject go = new GameObject("Crusher");
            BoxCollider bc = Trigger<BoxCollider>(go);
            bc.size = new Vector3(4f, 1.4f, 3f);
            bc.center = new Vector3(0f, 0.7f, 0f);
            Hazard h = go.AddComponent<Hazard>();
            h.damage = 2;
            h.knockback = 10f;
            h.restTime = 2.4f;
            h.warnTime = 0.7f;
            h.strikeTime = 0.4f;
            Prim(PrimitiveType.Cube, "Base", go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(4f, 0.12f, 3f), crusherBase, false);
            GameObject w = Prim(PrimitiveType.Cube, "Warning", go.transform, new Vector3(0f, 0.09f, 0f), new Vector3(3.8f, 0.14f, 2.8f), warning, false);
            GameObject s = Prim(PrimitiveType.Cube, "Slab", go.transform, new Vector3(0f, 0.7f, 0f), new Vector3(4f, 1.4f, 3f), slab, false);
            h.warning = w;
            h.slab = s;
            w.SetActive(false);
            s.SetActive(false);
            SavePrefab(go, "Crusher");
        }

        // Sweeper: a moving hazard on a kinematic Rigidbody.
        {
            GameObject go = new GameObject("Sweeper");
            BoxCollider bc = Trigger<BoxCollider>(go);
            bc.size = new Vector3(2.4f, 1.2f, 1f);
            bc.center = new Vector3(0f, 0.6f, 0f);
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            Hazard h = go.AddComponent<Hazard>();
            h.damage = 1;
            go.AddComponent<MovingHazard>();
            Prim(PrimitiveType.Cube, "Visual", go.transform, new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 1f), sweeper, false);
            SavePrefab(go, "Sweeper");
        }

        // Bounce pad (Part A's force: VelocityChange, once).
        {
            GameObject go = new GameObject("BouncePad");
            BoxCollider bc = Trigger<BoxCollider>(go);
            bc.size = new Vector3(3f, 0.6f, 3f);
            bc.center = new Vector3(0f, 0.3f, 0f);
            go.AddComponent<BouncePad>();
            Prim(PrimitiveType.Cylinder, "Pad", go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(2.8f, 0.08f, 2.8f), bounce, false);
            SavePrefab(go, "BouncePad");
        }

        // Goal
        {
            GameObject go = new GameObject("Goal");
            BoxCollider bc = Trigger<BoxCollider>(go);
            bc.size = new Vector3(3f, 3f, 3f);
            bc.center = new Vector3(0f, 1.5f, 0f);
            go.AddComponent<Goal>();
            Prim(PrimitiveType.Cylinder, "Pad", go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(3f, 0.05f, 3f), goal, false);
            Prim(PrimitiveType.Cube, "Beacon", go.transform, new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 3f, 0.6f), goal, false);
            SavePrefab(go, "Goal");
        }

        // Checkpoint
        {
            GameObject go = new GameObject("Checkpoint");
            BoxCollider bc = Trigger<BoxCollider>(go);
            bc.size = new Vector3(3f, 3f, 3f);
            bc.center = new Vector3(0f, 1.5f, 0f);
            Checkpoint cp = go.AddComponent<Checkpoint>();
            Prim(PrimitiveType.Cylinder, "Pole", go.transform, new Vector3(0f, 1f, 0f), new Vector3(0.12f, 1f, 0.12f), pole, false);
            GameObject f = Prim(PrimitiveType.Cube, "Flag", go.transform, new Vector3(0.45f, 1.7f, 0f), new Vector3(0.9f, 0.55f, 0.05f), flag, false);
            SerializedObject so = new SerializedObject(cp);
            so.FindProperty("flag").objectReferenceValue = f.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(go, "Checkpoint");
        }

        // Power-ups: a speed boost (timed, runs on the player) and a shield (a bool Health checks).
        {
            GameObject go = new GameObject("PowerUp_Speed");
            SphereCollider sc = Trigger<SphereCollider>(go);
            sc.radius = 0.8f;
            PowerUp pu = go.AddComponent<PowerUp>();
            pu.type = PowerUpType.Speed;
            pu.multiplier = 1.6f;
            pu.duration = 5f;
            Prim(PrimitiveType.Sphere, "Orb", go.transform, Vector3.zero, Vector3.one * 0.7f, speedMat, false);
            Prim(PrimitiveType.Cube, "Bolt", go.transform, Vector3.zero, new Vector3(0.15f, 1.1f, 0.15f), speedMat, false).transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            SavePrefab(go, "PowerUp_Speed");
        }
        {
            GameObject go = new GameObject("PowerUp_Shield");
            SphereCollider sc = Trigger<SphereCollider>(go);
            sc.radius = 0.8f;
            PowerUp pu = go.AddComponent<PowerUp>();
            pu.type = PowerUpType.Shield;
            Prim(PrimitiveType.Sphere, "Orb", go.transform, Vector3.zero, Vector3.one * 0.8f, shieldMat, false);
            SavePrefab(go, "PowerUp_Shield");
        }

        // KillZone: one wide trigger under the level.
        {
            GameObject go = new GameObject("KillZone");
            BoxCollider bc = Trigger<BoxCollider>(go);
            bc.size = Vector3.one;
            go.AddComponent<KillZone>();
            SavePrefab(go, "KillZone");
        }

        // GameManager: the prefab keeps only its numbers; the level builder fills in the scene references.
        {
            GameObject go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
            SavePrefab(go, "GameManager");
        }

        BuildHud();
    }

    // ------------------------------------------------------------------ the HUD
    static TextMeshProUGUI Txt(Transform parent, string name, string text, float size, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    static GameObject Btn(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, UnityAction onClick, out TextMeshProUGUI labelText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        labelText = Txt(go.transform, "Label", label, 34f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, onClick);
        return go;
    }

    static GameObject Panel(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    static void BuildHud()
    {
        GameObject root = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        UIActions actions = root.AddComponent<UIActions>();
        Transform t = root.transform;

        Color white = Color.white;
        Color soft = new Color(1f, 1f, 1f, 0.85f);
        Vector2 tl = new Vector2(0f, 1f);
        Vector2 tr = new Vector2(1f, 1f);
        Vector2 tc = new Vector2(0.5f, 1f);
        Vector2 bc = new Vector2(0.5f, 0f);

        Txt(t, "ScoreText", "Coins: 0", 40f, TextAlignmentOptions.TopLeft, tl, tl, tl, new Vector2(24f, -20f), new Vector2(700f, 56f), white);
        Txt(t, "ObjectiveText", "", 28f, TextAlignmentOptions.TopLeft, tl, tl, tl, new Vector2(24f, -76f), new Vector2(1100f, 44f), soft);
        Txt(t, "LivesText", "Lives: 3", 40f, TextAlignmentOptions.TopRight, tr, tr, tr, new Vector2(-24f, -20f), new Vector2(800f, 56f), white);
        Txt(t, "TimerText", "", 52f, TextAlignmentOptions.Top, tc, tc, tc, new Vector2(0f, -16f), new Vector2(500f, 70f), new Color(1f, 0.92f, 0.5f));
        Txt(t, "MessageText", "", 36f, TextAlignmentOptions.Bottom, bc, bc, bc, new Vector2(0f, 110f), new Vector2(1500f, 130f), white);

        // Win panel
        GameObject win = Panel(t, "WinPanel");
        Txt(win.transform, "Title", "LEVEL COMPLETE", 90f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1400f, 120f), new Color(0.4f, 1f, 0.6f));
        Txt(win.transform, "Detail", "", 44f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1000f, 220f), white);
        TextMeshProUGUI l1;
        Btn(win.transform, "RestartButton", "Restart (R)", new Vector2(-230f, -150f), new Vector2(400f, 100f), new Color(0.25f, 0.3f, 0.4f), actions.Restart, out l1);
        TextMeshProUGUI l2;
        Btn(win.transform, "NextButton", "Next Level (Enter)", new Vector2(230f, -150f), new Vector2(400f, 100f), new Color(0.15f, 0.6f, 0.4f), actions.NextLevel, out l2);
        win.SetActive(false);

        // Lose panel
        GameObject lose = Panel(t, "LosePanel");
        Txt(lose.transform, "Title", "GAME OVER", 100f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1400f, 130f), new Color(1f, 0.4f, 0.35f));
        Txt(lose.transform, "Detail", "", 44f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1000f, 200f), white);
        TextMeshProUGUI l3;
        Btn(lose.transform, "RestartButton", "Restart (R)", new Vector2(0f, -150f), new Vector2(400f, 100f), new Color(0.6f, 0.2f, 0.2f), actions.Restart, out l3);
        lose.SetActive(false);

        SavePrefab(root, "HUD");
    }
}
