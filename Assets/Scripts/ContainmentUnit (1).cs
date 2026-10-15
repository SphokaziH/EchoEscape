using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering;


[ExecuteAlways]
public class ContainmentUnit : MonoBehaviour
{
    public enum DoorState { Open, Closing, Closed, Opening }

    [Header("Size (metres)")]
    public float radius = 1.2f;
    public float glassHeight = 2.4f;
    [Tooltip("Plinth height. Keep it tiny (0.03 - 0.05) so tall Echo can walk straight in. Below 0.02 the ramps are skipped.")]
    public float baseHeight = 0.04f;

    [Header("Doors")]
    [Tooltip("Direction of the first doorway, degrees around Y")]
    public float doorAngle = 0f;
    [Tooltip("Width of each doorway in degrees")]
    public float doorArc = 70f;
    [Tooltip("A second doorway on the opposite side so the player can run THROUGH the cage with Echo behind them")]
    public bool twoDoorways = true;
    public bool startOpen = true;
    [Tooltip("Seconds the doors take to slam shut")]
    public float closeTime = 0.35f;
    [Tooltip("Seconds the doors take to open (only used by OpenDoors())")]
    public float openTime = 3f;
    public AudioClip closeClip;
    public AudioClip openClip;

    [Header("Trap")]
    [Tooltip("Drag Echo here, or give Echo the tag below")]
    public Transform echo;
    public string echoTag = "Echo";
    [Tooltip("Optional. Drag the player here, or tag it 'Player'")]
    public Transform player;
    public string playerTag = "Player";
    [Tooltip("Echo must get this close to the centre (fraction of the radius) before the doors close")]
    [Range(0.2f, 0.9f)] public float trapRadiusFactor = 0.65f;
    [Tooltip("Never close while the player is inside, so they can't be locked in")]
    public bool waitUntilPlayerOutside = true;
    [Tooltip("How far outside the cage wall the player's centre must be")]
    public float playerClearDistance = 0.45f;
    [Tooltip("Doors only work after FacilityPower.Restore()")]
    public bool requirePower = false;
    [Tooltip("Stop Echo's NavMeshAgent and disable his scripts when trapped")]
    public bool freezeEcho = true;
    public Color trappedColor = new Color(1f, 0.1f, 0.06f);
    public UnityEvent onEchoTrapped;

   
    public static event System.Action OnEchoTrapped;
    public static bool EchoTrapped { get; private set; }

    [Header("Look")]
    public int seed = 7;
    public Color glowColor = new Color(0.35f, 1f, 0.55f);
    public bool flicker = true;
    public bool ceilingStack = true;
    public bool glassCollider = true;

    const string RootName = "_ContainmentGenerated";
    Transform root;
    Shader lit;
    readonly List<Object> owned = new List<Object>();
    readonly List<Transform> negDoors = new List<Transform>();
    readonly List<Transform> posDoors = new List<Transform>();
    Material statusMat;
    Light glowLight;
    AudioSource audioSrc;

    DoorState state;
    float closeProgress, openProgress, nextFind;
    bool closingForEcho;

    void OnEnable() { Build(); }
    void OnDisable() { Clear(); }

    static Vector3 Dir(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r));
    }

    static void Kill(Object o)
    {
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    T Own<T>(T o) where T : Object
    {
        o.hideFlags = HideFlags.DontSave;
        owned.Add(o);
        return o;
    }

    void Clear()
    {
        negDoors.Clear();
        posDoors.Clear();
        var old = transform.Find(RootName);
        if (old) Kill(old.gameObject);
        foreach (var o in owned) if (o) Kill(o);
        owned.Clear();
    }

    [ContextMenu("Rebuild")]
    public void Build()
    {
        Clear();
        lit = Shader.Find("Universal Render Pipeline/Lit");

        root = new GameObject(RootName).transform;
        root.SetParent(transform, false);

        float baseR = radius + 0.25f;
        float topY = baseHeight + glassHeight;
        float midY = baseHeight + glassHeight * 0.5f;
        float half = doorArc * 0.5f;

      
        Material metal = Mat(Color.white, 0.75f, 0.45f, Grime(new Color(0.17f, 0.18f, 0.18f)), 4f);
        Material redTrim = Mat(Color.white, 0.55f, 0.5f, Grime(new Color(0.42f, 0.07f, 0.05f)), 4f);
        Material dark = Mat(new Color(0.04f, 0.045f, 0.045f), 0.6f, 0.35f);
        Material rubber = Mat(new Color(0.02f, 0.02f, 0.02f), 0f, 0.25f);
        Material glass = GlassMat(0.12f);
        Material hazard = Mat(Color.white, 0.2f, 0.3f);
        hazard.mainTexture = Own(HazardTexture());
        hazard.mainTextureScale = new Vector2(1f, 6f);
        Color amber = new Color(1f, 0.55f, 0.05f);
        statusMat = Mat(Dim(amber, 0.1f), 0f, 0.5f, null, 1f, amber * 2.2f);

       
        Part("Base", Lathe(new[]
        {
            new Vector2(baseR, 0f),
            new Vector2(baseR, baseHeight * 0.55f),
            new Vector2(baseR - 0.03f, baseHeight * 0.8f),
            new Vector2(baseR - 0.05f, baseHeight),
            new Vector2(0f, baseHeight)
        }, 64), metal, true);

        Part("InnerFloor", Lathe(new[]
        {
            new Vector2(radius + 0.02f, baseHeight),
            new Vector2(radius + 0.02f, baseHeight + 0.005f),
            new Vector2(0f, baseHeight + 0.005f)
        }, 64), dark, false);

        Part("GasketBottom", Ring(baseHeight), rubber, false);
        Part("GasketTop", Ring(topY - 0.07f), rubber, false);

       
        float cr = radius + 0.22f;
        Part("Canopy", Lathe(new[]
        {
            new Vector2(0f, topY),
            new Vector2(cr, topY),
            new Vector2(cr, topY + 0.1f),
            new Vector2(cr - 0.06f, topY + 0.2f),
            new Vector2(cr - 0.06f, topY + 0.3f),
            new Vector2(radius * 0.55f, topY + 0.34f),
            new Vector2(radius * 0.55f, topY + 0.5f),
            new Vector2(0f, topY + 0.5f)
        }, 64), redTrim, true);

        if (ceilingStack)
        {
            float y0 = topY + 0.5f;
            Part("Stack", Lathe(new[]
            {
                new Vector2(0.2f, y0),
                new Vector2(0.2f, y0 + 0.06f),
                new Vector2(0.12f, y0 + 0.06f),
                new Vector2(0.12f, y0 + 0.9f),
                new Vector2(0.16f, y0 + 0.9f),
                new Vector2(0.16f, y0 + 0.96f),
                new Vector2(0f, y0 + 0.96f)
            }, 24), metal, false);
        }

        
        Mesh postMesh = Lathe(new[]
        {
            new Vector2(0.03f, 0f),
            new Vector2(0.03f, glassHeight),
            new Vector2(0f, glassHeight)
        }, 12);
        const int posts = 8;
        for (int i = 0; i < posts; i++)
        {
            float a = 360f / posts * i + 22.5f;
            if (InDoorZone(a, half + 6f)) continue;
            var p = Part("Rib", postMesh, metal, false);
            p.transform.localPosition = Dir(a) * (radius - 0.07f) + new Vector3(0, baseHeight, 0);
        }

     
        Mesh boltMesh = Lathe(new[]
        {
            new Vector2(0.025f, 0f),
            new Vector2(0.025f, 0.02f),
            new Vector2(0.015f, 0.028f),
            new Vector2(0f, 0.028f)
        }, 8);
        for (int i = 0; i < 24; i++)
        {
            float a = 360f / 24f * i;
            if (InDoorZone(a, half + 6f)) continue;
            var b = Part("Bolt", boltMesh, dark, false);
            b.transform.localPosition = Dir(a) * (radius + 0.17f) + new Vector3(0, baseHeight, 0);
        }

       
        var outer = Part("GlassOuter", GlassShell(radius, false), glass, glassCollider);
        outer.transform.localPosition = new Vector3(0, baseHeight, 0);
        outer.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var inner = Part("GlassInner", GlassShell(radius - 0.018f, true), glass, false);
        inner.transform.localPosition = new Vector3(0, baseHeight, 0);
        inner.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        BuildDoorway(doorAngle, baseR, metal, redTrim, hazard, statusMat);
        if (twoDoorways) BuildDoorway(doorAngle + 180f, baseR, metal, redTrim, hazard, statusMat);

        
        BuildConsole(baseR, doorAngle + 90f, metal, redTrim, dark, hazard);

       
        var lgo = new GameObject("InteriorGlow");
        lgo.transform.SetParent(root, false);
        lgo.transform.localPosition = new Vector3(0, midY, 0);
        glowLight = lgo.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.range = 4f;
        glowLight.intensity = 3f;
        glowLight.shadows = LightShadows.None;
        if (flicker)
        {
            var f = lgo.AddComponent<ContainmentFlicker>();
            f.lightSource = glowLight;
            f.baseIntensity = 3f;
            f.speed = 6f;
            f.dropoutChance = 0.01f;
        }

        audioSrc = root.gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 1f;
        audioSrc.maxDistance = 25f;

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.DontSave;

        //  initial door state 
        closeProgress = 0f;
        openProgress = 0f;
        closingForEcho = false;
        if (Application.isPlaying) EchoTrapped = false;
        state = startOpen ? DoorState.Open : DoorState.Closed;
        ApplyDoors(startOpen ? 1f : 0f);
        SetStatus(!startOpen);
    }

    bool InDoorZone(float deg, float halfWidth)
    {
        if (Mathf.Abs(Mathf.DeltaAngle(deg, doorAngle)) < halfWidth) return true;
        return twoDoorways && Mathf.Abs(Mathf.DeltaAngle(deg, doorAngle + 180f)) < halfWidth;
    }

    
    void BuildDoorway(float c, float baseR, Material metal, Material redTrim, Material hazard, Material status)
    {
        float half = doorArc * 0.5f;
        float topY = baseHeight + glassHeight;
        Vector3 dir = Dir(c);

        
        Part("FramePostA", ArcMesh(radius - 0.03f, radius + 0.045f, c - half - 2.5f, c - half, baseHeight, topY), hazard, true);
        Part("FramePostB", ArcMesh(radius - 0.03f, radius + 0.045f, c + half, c + half + 2.5f, baseHeight, topY), hazard, true);

        
        float chord = 2f * (radius + 0.1f) * Mathf.Sin(half * Mathf.Deg2Rad) * 0.95f;
        if (baseHeight >= 0.02f)
        {
            var ramp = Part("Ramp", Wedge(chord, Mathf.Max(0.3f, baseHeight * 8f), baseHeight), metal, true);
            ramp.transform.localPosition = dir * (baseR - 0.1f);
            ramp.transform.localRotation = Quaternion.LookRotation(dir);
        }

        //  (amber = open, red = sealed)
        var strip = Prim(root, PrimitiveType.Cube, "StatusStrip",
            dir * (radius + 0.23f) + new Vector3(0, topY + 0.05f, 0),
            new Vector3(chord * 0.6f, 0.05f, 0.03f), status, false);
        strip.transform.localRotation = Quaternion.LookRotation(dir);

        
        negDoors.Add(MakeDoor("DoorA", c - half - 1f, c + 0.5f, true, 0f, metal, redTrim, hazard));
        posDoors.Add(MakeDoor("DoorB", c - 0.5f, c + half + 1f, false, 0.012f, metal, redTrim, hazard));
    }

    Transform MakeDoor(string name, float a0, float a1, bool leadAtEnd, float rOff,
                       Material metal, Material redTrim, Material hazard)
    {
        var door = new GameObject(name);
        door.transform.SetParent(root, false);
        var rb = door.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        float rIn = radius + 0.05f + rOff;
        float rOut = radius + 0.11f + rOff;
        float y0 = baseHeight, y1 = baseHeight + glassHeight;

        Part("Panel", ArcMesh(rIn, rOut, a0, a1, y0, y1), metal, true, door.transform);

        float[] bands = { 0.2f, 0.5f, 0.8f };
        foreach (float f in bands)
        {
            float yc = y0 + glassHeight * f;
            Part("Band", ArcMesh(rIn + 0.02f, rOut + 0.012f, a0, a1, yc - 0.07f, yc + 0.07f), redTrim, false, door.transform);
        }

        float s0 = leadAtEnd ? a1 - 3f : a0;
        float s1 = leadAtEnd ? a1 : a0 + 3f;
        Part("HazardEdge", ArcMesh(rIn + 0.02f, rOut + 0.02f, s0, s1, y0, y1), hazard, false, door.transform);

        return door.transform;
    }

    /// <summary>pos: 1 = fully open, 0 = fully closed.</summary>
    void ApplyDoors(float pos)
    {
        float slide = doorArc * 0.5f + 3f;
        
        foreach (var d in negDoors) if (d) d.localRotation = Quaternion.Euler(0f, slide * pos, 0f);
        foreach (var d in posDoors) if (d) d.localRotation = Quaternion.Euler(0f, -slide * pos, 0f);
    }

    void SetStatus(bool sealedShut)
    {
        if (!statusMat) return;
        Color c = sealedShut ? trappedColor : new Color(1f, 0.55f, 0.05f);
        statusMat.SetColor("_BaseColor", Dim(c, 0.1f));
        statusMat.SetColor("_EmissionColor", c * 2.2f);
    }

    // door logic
    void Update()
    {
        if (!Application.isPlaying) return;

        switch (state)
        {
            case DoorState.Open:
                if (EchoInside()) BeginClose(true);
                break;

            case DoorState.Closing:
                {
                    closeProgress += Time.deltaTime / Mathf.Max(0.05f, closeTime);
                    float t = Mathf.Clamp01(closeProgress);
                    ApplyDoors(1f - t * t);   // slow start, fast finish: a slam
                    if (t >= 1f) FinishClose();
                    break;
                }

            case DoorState.Opening:
                {
                    openProgress += Time.deltaTime / Mathf.Max(0.1f, openTime);
                    float t = Mathf.Clamp01(openProgress);
                    ApplyDoors(Mathf.SmoothStep(0f, 1f, t));
                    if (t >= 1f)
                    {
                        state = DoorState.Open;
                        SetStatus(false);
                        if (glowLight) glowLight.color = glowColor;
                    }
                    break;
                }
        }
    }

    void BeginClose(bool forEcho)
    {
        if (state != DoorState.Open) return;
        state = DoorState.Closing;
        closeProgress = 0f;
        closingForEcho = forEcho;
        SetStatus(true);
        PlayClip(closeClip);
    }

    void FinishClose()
    {
        state = DoorState.Closed;
        ApplyDoors(0f);
        if (glowLight) glowLight.color = trappedColor;

        if (closingForEcho)
        {
            FreezeEcho();
            EchoTrapped = true;
            onEchoTrapped?.Invoke();
            OnEchoTrapped?.Invoke();
        }
    }

    /// Close the doors without needing Echo (testing).
    [ContextMenu("Test: Close Doors")]
    public void CloseNow()
    {
        if (Application.isPlaying) BeginClose(false);
        else { ApplyDoors(0f); SetStatus(true); }
    }

    /// Re-open the doors 
    [ContextMenu("Test: Open Doors")]
    public void OpenDoors()
    {
        if (Application.isPlaying)
        {
            if (state != DoorState.Closed) return;
            state = DoorState.Opening;
            openProgress = 0f;
            PlayClip(openClip);
        }
        else { ApplyDoors(1f); SetStatus(false); }
    }

    bool EchoInside()
    {
        ResolveTargets();
        if (echo == null) return false;
        if (requirePower && !FacilityPower.IsRestored) return false;

        Vector3 e = transform.InverseTransformPoint(echo.position);
        float flat = new Vector2(e.x, e.z).magnitude;
        if (flat > radius * trapRadiusFactor) return false;
        if (e.y < -0.5f || e.y > baseHeight + glassHeight) return false;

        if (waitUntilPlayerOutside && player != null)
        {
            Vector3 p = transform.InverseTransformPoint(player.position);
            float pf = new Vector2(p.x, p.z).magnitude;
            if (pf < radius + playerClearDistance && p.y < baseHeight + glassHeight) return false;
        }
        return true;
    }

    void ResolveTargets()
    {
        if (echo != null && player != null) return;
        if (Time.unscaledTime < nextFind) return;
        nextFind = Time.unscaledTime + 1f;

        if (echo == null)
        {
            echo = FindByTag(echoTag);
            if (echo == null)
            {
                var g = GameObject.Find("Echo");
                if (g) echo = g.transform;
            }
        }
        if (player == null)
        {
            player = FindByTag(playerTag);
            if (player == null)
            {
                var g = GameObject.Find("Player");
                if (g) player = g.transform;
            }
        }
    }

    static Transform FindByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;
        try
        {
            var g = GameObject.FindWithTag(tag);
            return g ? g.transform : null;
        }
        catch (UnityException) { return null; } 
    }

    void FreezeEcho()
    {
        if (!freezeEcho || echo == null) return;

        foreach (var agent in echo.GetComponentsInChildren<NavMeshAgent>())
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            agent.enabled = false;
        }
        foreach (var mb in echo.GetComponentsInChildren<MonoBehaviour>())
            mb.enabled = false;
        foreach (var rb in echo.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSrc && clip) audioSrc.PlayOneShot(clip);
    }

    void BuildConsole(float baseR, float angle, Material metal, Material redTrim, Material dark, Material hazard)
    {
        Vector3 dir = Dir(angle);
        var c = new GameObject("Console").transform;
        c.SetParent(root, false);
        c.localPosition = dir * (baseR + 0.2f);
        c.localRotation = Quaternion.LookRotation(dir); 

        const float ch = 1.75f;
        Prim(c, PrimitiveType.Cube, "Body", new Vector3(0, ch / 2f, 0), new Vector3(0.62f, ch, 0.46f), metal, true);
        Prim(c, PrimitiveType.Cube, "Cap", new Vector3(0, ch + 0.04f, 0), new Vector3(0.7f, 0.08f, 0.54f), redTrim, false);
        Prim(c, PrimitiveType.Cube, "HazardL", new Vector3(-0.29f, ch / 2f, 0.232f), new Vector3(0.06f, ch, 0.01f), hazard, false);
        Prim(c, PrimitiveType.Cube, "HazardR", new Vector3(0.29f, ch / 2f, 0.232f), new Vector3(0.06f, ch, 0.01f), hazard, false);

        for (int i = 0; i < 6; i++)
            Prim(c, PrimitiveType.Cube, "Vent", new Vector3(0, 0.25f + i * 0.06f, 0.232f), new Vector3(0.4f, 0.02f, 0.01f), dark, false);

        Screen(c, new Vector3(-0.14f, 1.5f, 0.2315f), new Color(0.1f, 0.9f, 1f), 6f, false);
        Screen(c, new Vector3(0.14f, 1.5f, 0.2315f), new Color(0.2f, 1f, 0.3f), 3f, false);
        Screen(c, new Vector3(-0.14f, 1.25f, 0.2315f), new Color(1f, 0.08f, 0.05f), 2.2f, true);   // alarm
        Screen(c, new Vector3(0.14f, 1.25f, 0.2315f), new Color(0.15f, 0.25f, 0.25f), 1f, false); // dead

        Color[] btn = { new Color(1f, 0.1f, 0.05f), new Color(1f, 0.6f, 0.05f), new Color(0.2f, 1f, 0.3f), new Color(0.2f, 1f, 0.3f) };
        for (int i = 0; i < 4; i++)
        {
            var m = Mat(Dim(btn[i], 0.15f), 0f, 0.5f, null, 1f, btn[i] * (i == 0 ? 2f : 0.6f));
            Prim(c, PrimitiveType.Cube, "Button", new Vector3(-0.15f + i * 0.1f, 0.95f, 0.235f), new Vector3(0.05f, 0.05f, 0.02f), m, false);
        }
    }

    void Screen(Transform parent, Vector3 lp, Color col, float speed, bool blink)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Kill(q.GetComponent<Collider>());
        q.name = "Screen";
        q.transform.SetParent(parent, false);
        q.transform.localPosition = lp;
        q.transform.localRotation = Quaternion.Euler(0, 180, 0); 
        q.transform.localScale = new Vector3(0.22f, 0.17f, 1f);
        var m = Mat(Dim(col, 0.1f), 0f, 0.8f, null, 1f, col * 1.5f);
        q.GetComponent<Renderer>().sharedMaterial = m;
        if (flicker)
        {
            var f = q.AddComponent<ContainmentFlicker>();
            f.emissiveMat = m;
            f.emissionColor = col * 1.5f;
            f.speed = speed;
            f.blink = blink;
            f.dropoutChance = blink ? 0f : 0.004f;
        }
    }

    static Color Dim(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

    //  glass
    Mesh GlassShell(float r, bool inwardFacing)
    {
        const int cols = 180, rows = 4;
        var verts = new Vector3[(cols + 1) * (rows + 1)];
        var norms = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];
        for (int j = 0; j <= rows; j++)
        {
            float y = glassHeight * j / rows;
            for (int i = 0; i <= cols; i++)
            {
                float a = 2f * Mathf.PI * i / cols;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                int k = j * (cols + 1) + i;
                verts[k] = new Vector3(r * c, y, r * s);
                norms[k] = new Vector3(c, 0, s) * (inwardFacing ? -1f : 1f);
                uvs[k] = new Vector2((float)i / cols, (float)j / rows);
            }
        }

        var tris = new List<int>();
        void TryTri(int i0, int i1, int i2)
        {
            Vector3 cen = (verts[i0] + verts[i1] + verts[i2]) / 3f;
            float deg = Mathf.Atan2(cen.z, cen.x) * Mathf.Rad2Deg;
            if (InDoorZone(deg, doorArc * 0.5f)) return; // doorway
            if (Vector3.Dot(Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]), norms[i0]) < 0f)
            { int t = i1; i1 = i2; i2 = t; }
            tris.Add(i0); tris.Add(i1); tris.Add(i2);
        }

        for (int j = 0; j < rows; j++)
            for (int i = 0; i < cols; i++)
            {
                int a = j * (cols + 1) + i, b = a + 1, c = a + cols + 1, d = c + 1;
                TryTri(a, c, d);
                TryTri(a, d, b);
            }

        var mesh = Own(new Mesh { name = "GlassShell" });
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // meshes
    Mesh Ring(float y)
    {
        return Lathe(new[]
        {
            new Vector2(radius + 0.035f, y),
            new Vector2(radius + 0.035f, y + 0.07f),
            new Vector2(radius - 0.035f, y + 0.07f),
            new Vector2(radius - 0.035f, y)
        }, 64);
    }

 
    Mesh ArcMesh(float rIn, float rOut, float a0, float a1, float y0, float y1)
    {
        int segs = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 3f));
        int K = segs + 1;

        var v = new List<Vector3>();
        var n = new List<Vector3>();
        var uv = new List<Vector2>();
        var tri = new List<int>();

        var bo = new Vector3[K]; var to = new Vector3[K];
        var bi = new Vector3[K]; var ti = new Vector3[K];
        var rad = new Vector3[K]; var radNeg = new Vector3[K];
        var uA = new float[K];

        for (int i = 0; i < K; i++)
        {
            float ang = Mathf.Lerp(a0, a1, i / (float)segs) * Mathf.Deg2Rad;
            Vector3 d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            bo[i] = d * rOut + Vector3.up * y0;
            to[i] = d * rOut + Vector3.up * y1;
            bi[i] = d * rIn + Vector3.up * y0;
            ti[i] = d * rIn + Vector3.up * y1;
            rad[i] = d;
            radNeg[i] = -d;
            uA[i] = ang * rOut * 0.5f;
        }

        Vector2[] Us(float y)
        {
            var r = new Vector2[K];
            for (int i = 0; i < K; i++) r[i] = new Vector2(uA[i], y * 0.5f);
            return r;
        }
        Vector3[] Fill(Vector3 val, int count)
        {
            var r = new Vector3[count];
            for (int i = 0; i < count; i++) r[i] = val;
            return r;
        }

        AddStrip(v, n, uv, tri, bo, to, rad, rad, Us(y0), Us(y1));                       // outer face
        AddStrip(v, n, uv, tri, bi, ti, radNeg, radNeg, Us(y0), Us(y1));                 // inner face
        AddStrip(v, n, uv, tri, ti, to, Fill(Vector3.up, K), Fill(Vector3.up, K), Us(y1), Us(y1));       // top
        AddStrip(v, n, uv, tri, bi, bo, Fill(Vector3.down, K), Fill(Vector3.down, K), Us(y0), Us(y0));   // bottom

        float r0 = a0 * Mathf.Deg2Rad, r1 = a1 * Mathf.Deg2Rad;
        Vector3 t0 = new Vector3(-Mathf.Sin(r0), 0f, Mathf.Cos(r0));
        Vector3 t1 = new Vector3(-Mathf.Sin(r1), 0f, Mathf.Cos(r1));
        Vector2[] capUv = { new Vector2(0, 0), new Vector2(1, 1) };
        AddStrip(v, n, uv, tri, new[] { bi[0], ti[0] }, new[] { bo[0], to[0] },
                 Fill(-t0, 2), Fill(-t0, 2), new[] { new Vector2(0, 0), new Vector2(0, 1) }, new[] { new Vector2(1, 0), new Vector2(1, 1) });
        AddStrip(v, n, uv, tri, new[] { bi[K - 1], ti[K - 1] }, new[] { bo[K - 1], to[K - 1] },
                 Fill(t1, 2), Fill(t1, 2), new[] { new Vector2(0, 0), new Vector2(0, 1) }, new[] { new Vector2(1, 0), new Vector2(1, 1) });

        var mesh = Own(new Mesh { name = "ArcBlock" });
        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddStrip(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri,
                         Vector3[] pa, Vector3[] pb, Vector3[] na, Vector3[] nb, Vector2[] ua, Vector2[] ub)
    {
        int start = v.Count;
        for (int i = 0; i < pa.Length; i++)
        {
            v.Add(pa[i]); n.Add(na[i]); uv.Add(ua[i]);
            v.Add(pb[i]); n.Add(nb[i]); uv.Add(ub[i]);
        }
        for (int i = 0; i < pa.Length - 1; i++)
        {
            int a0 = start + i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
            AddOriented(v, n, tri, a0, a1, b1);
            AddOriented(v, n, tri, a0, b1, b0);
        }
    }

    Mesh Wedge(float w, float len, float h)
    {
        var v = new List<Vector3>();
        var n = new List<Vector3>();
        var uv = new List<Vector2>();
        var tri = new List<int>();
        float hw = w * 0.5f;

        void Face(Vector3 nrm, params Vector3[] pts)
        {
            int s = v.Count;
            foreach (var p in pts) { v.Add(p); n.Add(nrm); uv.Add(new Vector2(p.x, p.z + p.y)); }
            for (int i = 1; i < pts.Length - 1; i++) AddOriented(v, n, tri, s, s + i, s + i + 1);
        }

        Vector3 tl = new Vector3(-hw, h, 0), tr = new Vector3(hw, h, 0);
        Vector3 ll = new Vector3(-hw, 0, len), lr = new Vector3(hw, 0, len);
        Vector3 bl = new Vector3(-hw, 0, 0), br = new Vector3(hw, 0, 0);

        Face(new Vector3(0, len, h).normalized, tl, tr, lr, ll); // slope
        Face(Vector3.back, bl, br, tr, tl);                      // back
        Face(Vector3.down, bl, ll, lr, br);                      // bottom
        Face(Vector3.left, bl, tl, ll);                          // left side
        Face(Vector3.right, br, lr, tr);                         // right side

        var mesh = Own(new Mesh { name = "Ramp" });
        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

   
    Mesh Lathe(Vector2[] prof, int segs)
    {
        var v = new List<Vector3>();
        var n = new List<Vector3>();
        var uv = new List<Vector2>();
        var tri = new List<int>();
        float run = 0f;

        for (int s = 0; s < prof.Length - 1; s++)
        {
            Vector2 p0 = prof[s], p1 = prof[s + 1];
            Vector2 d = p1 - p0;
            float len = d.magnitude;
            if (len < 1e-5f) continue;
            Vector2 n2 = new Vector2(d.y, -d.x) / len;
            int start = v.Count;

            for (int i = 0; i <= segs; i++)
            {
                float a = 2f * Mathf.PI * i / segs;
                float c = Mathf.Cos(a), sn = Mathf.Sin(a);
                v.Add(new Vector3(p0.x * c, p0.y, p0.x * sn));
                n.Add(new Vector3(n2.x * c, n2.y, n2.x * sn));
                uv.Add(new Vector2((float)i / segs, run));
                v.Add(new Vector3(p1.x * c, p1.y, p1.x * sn));
                n.Add(new Vector3(n2.x * c, n2.y, n2.x * sn));
                uv.Add(new Vector2((float)i / segs, run + len));
            }
            run += len;

            for (int i = 0; i < segs; i++)
            {
                int a0 = start + i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
                AddOriented(v, n, tri, a0, a1, b1);
                AddOriented(v, n, tri, a0, b1, b0);
            }
        }

        var mesh = Own(new Mesh { name = "Lathe" });
        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddOriented(List<Vector3> v, List<Vector3> n, List<int> tri, int a, int b, int c)
    {
        Vector3 cr = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (cr.sqrMagnitude < 1e-12f) return; // degenerate (profile touches the axis)
        if (Vector3.Dot(cr, n[a]) < 0f) { int t = b; b = c; c = t; }
        tri.Add(a); tri.Add(b); tri.Add(c);
    }

    GameObject Part(string name, Mesh mesh, Material mat, bool collider, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    GameObject Prim(Transform parent, PrimitiveType t, string n, Vector3 pos, Vector3 scale, Material m, bool collider)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = n;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Kill(go.GetComponent<Collider>());
        return go;
    }

    // materials / textures
    Material Mat(Color c, float metallic, float smooth, Texture tex = null, float tile = 1f, Color? emission = null)
    {
        var m = Own(new Material(lit));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smooth);
        if (tex != null)
        {
            m.mainTexture = tex;
            m.mainTextureScale = new Vector2(tile, 1f);
        }
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
        }
        return m;
    }

    Material GlassMat(float alpha)
    {
        var m = Own(new Material(lit));
        m.SetFloat("_Surface", 1);
        m.SetFloat("_Blend", 0);
        m.SetFloat("_ZWrite", 0);
        m.SetFloat("_Cull", 0); // double sided
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetColor("_BaseColor", new Color(0.6f, 0.82f, 0.75f, alpha));
        m.SetFloat("_Smoothness", 1f);
        m.SetFloat("_Metallic", 0f);
        return m;
    }

    Texture2D HazardTexture()
    {
        var tex = new Texture2D(8, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, (y / 16) % 2 == 0 ? new Color(0.9f, 0.55f, 0.1f) : new Color(0.04f, 0.04f, 0.04f));
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply();
        return tex;
    }

 
    Texture2D Grime(Color baseCol)
    {
        const int w = 128, h = 128;
        float o = seed * 3.1f;
        var cols = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float blotch = SeamlessNoise(x, y, w, 0.03f, 0.03f, o);
                float fine = SeamlessNoise(x, y, w, 0.25f, 0.25f, o + 50f);
                float streak = SeamlessNoise(x, y, w, 0.3f, 0.015f, o + 90f);
                float k = 0.55f + 0.3f * blotch + 0.15f * fine + 0.3f * streak;
                k = Mathf.Clamp(k, 0.35f, 1.15f);
                cols[y * w + x] = new Color(baseCol.r * k, baseCol.g * k, baseCol.b * k, 1f);
            }
        var tex = Own(new Texture2D(w, h, TextureFormat.RGBA32, true));
        tex.SetPixels(cols);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply(true);
        return tex;
    }

    static float SeamlessNoise(int x, int y, int w, float fx, float fy, float off)
    {
        float a = Mathf.PerlinNoise(x * fx + off, y * fy + off);
        float b = Mathf.PerlinNoise((x - w) * fx + off, y * fy + off);
        return Mathf.Lerp(a, b, x / (float)w);
    }
}


public class ContainmentFlicker : MonoBehaviour
{
    public Light lightSource;
    public Material emissiveMat;
    public Color emissionColor = Color.white;
    public float baseIntensity = 1f;
    public float speed = 6f;
    public float dropoutChance = 0.01f;
    public bool blink;

    float seed, dropTimer;

    void Start() { seed = Random.value * 100f; }

    void Update()
    {
        float k;
        if (blink)
        {
            k = Mathf.Repeat(Time.time * speed * 0.5f, 1f) < 0.5f ? 1f : 0.05f;
        }
        else
        {
            k = Mathf.Lerp(0.4f, 1f, Mathf.PerlinNoise(Time.time * speed, seed));
            if (dropTimer > 0f) { dropTimer -= Time.deltaTime; k = 0.03f; }
            else if (Random.value < dropoutChance) dropTimer = Random.Range(0.05f, 0.25f);
        }

        if (lightSource) lightSource.intensity = baseIntensity * k;
        if (emissiveMat) emissiveMat.SetColor("_EmissionColor", emissionColor * k);
    }
}