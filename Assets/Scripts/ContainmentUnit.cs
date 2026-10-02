using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[ExecuteAlways]
public class ContainmentUnit : MonoBehaviour
{
    [Header("Size (metres)")]
    public float radius = 1f;
    public float glassHeight = 2.2f;
    public float baseHeight = 0.7f;

    [Header("Breakage")]
    [Tooltip("Direction the hole faces, degrees around Y (0 = +X, 90 = +Z)")]
    public float breakAngle = 0f;
    [Tooltip("Hole width as arc length in metres")]
    public float holeWidth = 1.3f;
    public float holeHeight = 1.7f;
    [Tooltip("Height of hole centre above the glass bottom. Lower it so the hole reaches the floor.")]
    public float holeCenterY = 0.85f;
    public int seed = 7;
    public int floorShards = 40;
    public bool glassCollider = true;

    [Header("Look")]
    public Color glowColor = new Color(0.35f, 1f, 0.55f);
    public bool flicker = true;
    public bool ceilingStack = true;

    const string RootName = "_ContainmentGenerated";
    Transform root;
    System.Random rng;
    Shader lit;
    readonly List<Object> owned = new List<Object>();

    void OnEnable() { Build(); }
    void OnDisable() { Clear(); }

    float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

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
        var old = transform.Find(RootName);
        if (old) Kill(old.gameObject);
        foreach (var o in owned) if (o) Kill(o);
        owned.Clear();
    }

    
    [ContextMenu("Rebuild")]
    public void Build()
    {
        Clear();
        rng = new System.Random(seed);
        lit = Shader.Find("Universal Render Pipeline/Lit");

        root = new GameObject(RootName).transform;
        root.SetParent(transform, false);

        float baseR = radius + 0.25f;
        float topY = baseHeight + glassHeight;
        float midY = baseHeight + glassHeight * 0.5f;

        // materials
        Material metal = Mat(Color.white, 0.75f, 0.45f, Grime(new Color(0.17f, 0.18f, 0.18f)), 4f);
        Material redTrim = Mat(Color.white, 0.55f, 0.5f, Grime(new Color(0.42f, 0.07f, 0.05f)), 4f);
        Material dark = Mat(new Color(0.04f, 0.045f, 0.045f), 0.6f, 0.35f);
        Material rubber = Mat(new Color(0.02f, 0.02f, 0.02f), 0f, 0.25f);
        Material glass = GlassMat(0.12f);
        Material shardM = GlassMat(0.3f);
        Material hazard = Mat(Color.white, 0.2f, 0.3f);
        hazard.mainTexture = Own(HazardTexture());
        hazard.mainTextureScale = new Vector2(1f, 6f);

        
        Part("Base", Lathe(new[]
        {
            new Vector2(baseR, 0f),
            new Vector2(baseR, 0.08f),
            new Vector2(baseR - 0.02f, 0.11f),
            new Vector2(baseR - 0.02f, baseHeight - 0.2f),
            new Vector2(baseR + 0.04f, baseHeight - 0.17f),
            new Vector2(baseR + 0.04f, baseHeight - 0.05f),
            new Vector2(baseR - 0.04f, baseHeight),
            new Vector2(0f, baseHeight)
        }, 64), metal, true);

    
        Part("InnerFloor", Lathe(new[]
        {
            new Vector2(radius + 0.02f, baseHeight),
            new Vector2(radius + 0.02f, baseHeight + 0.04f),
            new Vector2(0f, baseHeight + 0.04f)
        }, 64), dark, false);

        
        Part("GasketBottom", Ring(baseHeight), rubber, false);
        Part("GasketTop", Ring(topY - 0.07f), rubber, false);

        // ---------- canopy ----------
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

        // ---------- ribs (skipped where the glass is broken) ----------
        Mesh postMesh = Lathe(new[]
        {
            new Vector2(0.045f, 0f),
            new Vector2(0.045f, glassHeight),
            new Vector2(0f, glassHeight)
        }, 14);
        float holeHalfDeg = holeWidth * 0.5f / radius * Mathf.Rad2Deg;
        const int posts = 8;
        for (int i = 0; i < posts; i++)
        {
            float a = 360f / posts * i + 22.5f;
            if (Mathf.Abs(Mathf.DeltaAngle(a, breakAngle)) < holeHalfDeg + 6f) continue;
            var p = Part("Rib", postMesh, metal, false);
            p.transform.localPosition = Dir(a) * (radius + 0.05f) + new Vector3(0, baseHeight, 0);
        }

        // ---------- bolts around the base top ----------
        Mesh boltMesh = Lathe(new[]
        {
            new Vector2(0.025f, 0f),
            new Vector2(0.025f, 0.02f),
            new Vector2(0.015f, 0.028f),
            new Vector2(0f, 0.028f)
        }, 8);
        for (int i = 0; i < 24; i++)
        {
            var b = Part("Bolt", boltMesh, dark, false);
            b.transform.localPosition = Dir(360f / 24f * i) * (radius + 0.125f) + new Vector3(0, baseHeight, 0);
        }

        // ---------- glass: two thin shells, same hole ----------
        var outer = Part("GlassOuter", GlassShell(radius, false), glass, glassCollider);
        outer.transform.localPosition = new Vector3(0, baseHeight, 0);
        outer.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var inner = Part("GlassInner", GlassShell(radius - 0.018f, true), glass, false);
        inner.transform.localPosition = new Vector3(0, baseHeight, 0);
        inner.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // ---------- broken glass on the floor ----------
        float floorY = 0.01f;
        for (int i = 0; i < floorShards; i++)
        {
            float ang = breakAngle + R(-38f, 38f);
            float dist = baseR + R(0.05f, 1.6f);
            ScatterShard(shardM, Dir(ang) * dist + new Vector3(0, floorY, 0), i % 10 == 0);
        }
        // and some inside the tank, on the grate
        for (int i = 0; i < Mathf.Max(6, floorShards / 3); i++)
        {
            float ang = breakAngle + R(-45f, 45f);
            float dist = radius * R(0.1f, 0.9f);
            ScatterShard(shardM, Dir(ang) * dist + new Vector3(0, baseHeight + 0.045f, 0), false);
        }

        // ---------- console opposite the break ----------
        BuildConsole(baseR, metal, redTrim, dark, hazard);

        // ---------- interior light ----------
        var lgo = new GameObject("InteriorGlow");
        lgo.transform.SetParent(root, false);
        lgo.transform.localPosition = new Vector3(0, midY, 0);
        var l = lgo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = glowColor;
        l.range = 4f;
        l.intensity = 3f;
        l.shadows = LightShadows.None;
        if (flicker)
        {
            var f = lgo.AddComponent<ContainmentFlicker>();
            f.lightSource = l;
            f.baseIntensity = 3f;
            f.speed = 6f;
            f.dropoutChance = 0.01f;
        }

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.DontSave;
    }

    
    void BuildConsole(float baseR, Material metal, Material redTrim, Material dark, Material hazard)
    {
        float ang = breakAngle + 180f;
        Vector3 dir = Dir(ang);
        var c = new GameObject("Console").transform;
        c.SetParent(root, false);
        c.localPosition = dir * (baseR + 0.1f);
        c.localRotation = Quaternion.LookRotation(dir); // local +Z points away from the tank

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
        q.transform.localRotation = Quaternion.Euler(0, 180, 0); // a Unity quad is visible from -Z; flip it to face +Z
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

    // ------------------------------------------------------------------ glass
    bool InHole(float deg, float y, float sx)
    {
        float u = Mathf.DeltaAngle(deg, breakAngle) * Mathf.Deg2Rad * radius; // metres along the wall
        float v = y - holeCenterY;
        float noise = Mathf.PerlinNoise(u * 5f + sx, y * 5f + sx * 0.6f) - 0.5f;
        float hw = holeWidth * 0.5f, hh = holeHeight * 0.5f;
        float d = (u * u) / (hw * hw) + (v * v) / (hh * hh);
        return d < 1f + noise * 1.1f;
    }

    Mesh GlassShell(float r, bool inwardFacing)
    {
        const int cols = 96, rows = 40;
        float sx = seed * 1.73f;
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
            if (InHole(deg, cen.y, sx)) return; // this triangle was smashed out
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

    void ScatterShard(Material m, Vector3 pos, bool big)
    {
        float size = big ? R(0.12f, 0.25f) : R(0.03f, 0.12f);
        Vector2 a = Vector2.zero;
        Vector2 b = new Vector2(size, R(-0.02f, 0.02f));
        Vector2 c = new Vector2(R(0f, size), R(0.03f, size));
        Mesh mesh = Prism(a, b, c, 0.008f, out Vector3 cen);
        var go = new GameObject("Shard");
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, R(0, 360), 0) * Quaternion.Euler(90, 0, 0);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    /// <summary>Thin triangular prism centred on its centroid, flat-shaded.</summary>
    Mesh Prism(Vector2 a, Vector2 b, Vector2 c, float thick, out Vector3 centroid)
    {
        Vector2 g = (a + b + c) / 3f;
        centroid = new Vector3(g.x, g.y, 0);
        float h = thick * 0.5f;
        Vector2[] p = { a - g, b - g, c - g };
        var v = new List<Vector3>();
        var tri = new List<int>();

        void Tri(Vector3 x, Vector3 y, Vector3 z)
        {
            Vector3 outward = (x + y + z) / 3f;
            if (Vector3.Dot(Vector3.Cross(y - x, z - x), outward) < 0f) { var t = y; y = z; z = t; }
            int i = v.Count;
            v.Add(x); v.Add(y); v.Add(z);
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
        }

        Tri(new Vector3(p[0].x, p[0].y, -h), new Vector3(p[1].x, p[1].y, -h), new Vector3(p[2].x, p[2].y, -h));
        Tri(new Vector3(p[0].x, p[0].y, h), new Vector3(p[1].x, p[1].y, h), new Vector3(p[2].x, p[2].y, h));
        for (int i = 0; i < 3; i++)
        {
            Vector2 s0 = p[i], s1 = p[(i + 1) % 3];
            Vector3 q0 = new Vector3(s0.x, s0.y, -h), q1 = new Vector3(s1.x, s1.y, -h);
            Vector3 q2 = new Vector3(s1.x, s1.y, h), q3 = new Vector3(s0.x, s0.y, h);
            Tri(q0, q1, q2);
            Tri(q0, q2, q3);
        }

        var mesh = Own(new Mesh { name = "GlassShard" });
        mesh.SetVertices(v);
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    
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

    GameObject Part(string name, Mesh mesh, Material mat, bool collider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
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