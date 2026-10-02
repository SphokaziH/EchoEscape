using System.Collections.Generic;
using UnityEngine;

// Builds a ceiling slab, evenly spaced light fixtures, and dripping fire sprinklers.
// Right-click the component header > "Build Ceiling". Rebuilding clears the old one.
public class FacilityCeilingBuilder : MonoBehaviour
{
    [Header("Area (this object's position = center, at floor level)")]
    public Renderer floorRenderer;                 // optional: fit area to this floor's bounds
    public Vector2 areaSize = new Vector2(60f, 30f);
    public float ceilingHeight = 3.2f;

    [Header("Ceiling slab")]
    public bool buildCeilingSlab = true;
    public Material ceilingMaterial;

    [Header("Fixture layout")]
    public float spacingX = 6f;
    public float spacingZ = 6f;
    public int maxFixtures = 300;
    public Material fixtureMaterial;
    public Vector3 fixtureSize = new Vector3(0.15f, 0.02f, 1.2f);   // width, thickness, length
    public int seed = 7;

    [Header("Mix")]
    [Range(0f, 1f)] public float redFraction = 0.15f;
    public int whiteLightEvery = 4;
    public bool lightsCastShadows = false;

    [Header("Red alarm lights")]
    public Color redColor = new Color(1f, 0.1f, 0.06f);
    public float redIntensity = 8f;
    public float redRange = 10f;
    public float redPulseSpeed = 2.5f;
    public float redEmissionBoost = 4f;

    [Header("White lights (before power)")]
    public Color whiteColor = new Color(0.78f, 0.9f, 0.75f);
    public float whiteIntensity = 3f;
    public float whiteRange = 9f;
    public float whiteEmissionBoost = 2.5f;
    [Range(0f, 1f)] public float dimLevel = 0.12f;

    [Header("After power is restored")]
    public Color restoredColor = new Color(1f, 1f, 0.97f);
    public float restoredIntensity = 6f;
    public float restoredRange = 12f;
    public float restoredEmissionBoost = 4f;
    public bool redBecomeWhite = true;
    public float restoreSpread = 1.2f;             // lights switch on over this many seconds

    [Header("Ceiling pipes")]
    public bool buildPipes = true;
    public int pipeEveryNthLine = 2;               // 1 = a pipe between every fixture column
    public Vector2 pipeDiameterRange = new Vector2(0.12f, 0.25f);
    public Material pipeMaterial;

    [Header("Dripping sprinklers (max 16)")]
    public bool buildSprinklers = true;
    [Range(0, 16)] public int sprinklerCount = 10;

    const string RootName = "Generated Ceiling";

    Material MakeLit(Shader lit, Color c, float smooth)
    {
        Material m = new Material(lit);
        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    void StripCollider(GameObject g)
    {
        Collider col = g.GetComponent<Collider>();
        if (col == null) return;
        if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
    }

    GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        g.transform.localScale = scale;
        StripCollider(g);
        MeshRenderer r = g.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    [ContextMenu("Build Ceiling")]
    public void Build()
    {
        Clear();

        Vector3 center = transform.position;
        Vector2 size = areaSize;
        float floorY = transform.position.y;
        if (floorRenderer != null)
        {
            Bounds b = floorRenderer.bounds;
            center = b.center;
            size = new Vector2(b.size.x, b.size.z);
            floorY = b.max.y;
        }
        float ceilingY = floorY + ceilingHeight;

        int nx = Mathf.Max(1, Mathf.RoundToInt(size.x / Mathf.Max(0.5f, spacingX)));
        int nz = Mathf.Max(1, Mathf.RoundToInt(size.y / Mathf.Max(0.5f, spacingZ)));
        int cap = Mathf.Max(1, maxFixtures);
        while (nx * nz > cap)
        {
            if (nx >= nz) nx--; else nz--;
        }

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");

        // Auto-made materials are rebuilt every time so old settings never stick.
        if (fixtureMaterial != null && fixtureMaterial.name.StartsWith("CeilingFixture_"))
            fixtureMaterial = null;
        if (fixtureMaterial == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            fixtureMaterial = new Material(unlit != null ? unlit : lit);
            fixtureMaterial.name = "CeilingFixture_Unlit";
            fixtureMaterial.SetColor("_BaseColor", Color.white);
            fixtureMaterial.SetColor("_Color", Color.white);
        }
        if (ceilingMaterial != null && ceilingMaterial.name.StartsWith("CeilingSlab_"))
            ceilingMaterial = null;
        if (ceilingMaterial == null)
        {
            Shader wet = Shader.Find("Horror/WetCeilingURP");
            ceilingMaterial = new Material(wet != null ? wet : lit);
            ceilingMaterial.name = "CeilingSlab_Auto";
            ceilingMaterial.SetColor("_BaseColor", new Color(0.2f, 0.21f, 0.2f));
            ceilingMaterial.SetColor("_Color", new Color(0.2f, 0.21f, 0.2f));
        }

        if (pipeMaterial != null && pipeMaterial.name.StartsWith("CeilingPipe_"))
            pipeMaterial = null;
        if (pipeMaterial == null)
        {
            Shader wetShader = Shader.Find("Horror/WetCeilingURP");
            pipeMaterial = new Material(wetShader != null ? wetShader : lit);
            pipeMaterial.name = "CeilingPipe_Auto";
            pipeMaterial.SetColor("_BaseColor", new Color(0.42f, 0.43f, 0.41f));
            pipeMaterial.SetColor("_Color", new Color(0.42f, 0.43f, 0.41f));
            pipeMaterial.SetFloat("_StainScale", 2.5f);
            pipeMaterial.SetFloat("_StainAmount", 0.6f);
        }

        if (GetComponent<FacilityPower>() == null) gameObject.AddComponent<FacilityPower>();

        GameObject root = new GameObject(RootName);
        root.transform.SetParent(transform, false);

        if (buildCeilingSlab)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "CeilingSlab";
            slab.transform.SetParent(root.transform, true);
            slab.transform.position = new Vector3(center.x, ceilingY + 0.1f, center.z);
            slab.transform.localScale = new Vector3(size.x, 0.2f, size.y);
            slab.GetComponent<MeshRenderer>().sharedMaterial = ceilingMaterial;
        }

        System.Random rng = new System.Random(seed);
        List<Vector2> fixturePos = new List<Vector2>();
        int whiteCounter = 0;
        int count = 0, reds = 0, lights = 0;

        for (int ix = 0; ix < nx; ix++)
        {
            for (int iz = 0; iz < nz; iz++)
            {
                float x = center.x - size.x * 0.5f + (ix + 0.5f) * size.x / nx;
                float z = center.z - size.y * 0.5f + (iz + 0.5f) * size.y / nz;
                fixturePos.Add(new Vector2(x, z));

                bool red = rng.NextDouble() < redFraction;
                bool real = red;
                if (!red)
                {
                    real = (whiteCounter % Mathf.Max(1, whiteLightEvery)) == 0;
                    whiteCounter++;
                }

                GameObject fx = Part(PrimitiveType.Cube, red ? "Fixture_Red" : "Fixture_White", root.transform,
                    new Vector3(x, ceilingY - fixtureSize.y * 0.5f - 0.005f, z), fixtureSize, fixtureMaterial);

                Light lamp = null;
                if (real)
                {
                    GameObject lg = new GameObject(red ? "Lamp_Red" : "Lamp_White");
                    lg.transform.SetParent(root.transform, true);
                    lg.transform.position = fx.transform.position + Vector3.down * 0.3f;
                    lamp = lg.AddComponent<Light>();
                    lamp.type = LightType.Point;
                    lamp.color = red ? redColor : whiteColor;
                    lamp.range = red ? redRange : whiteRange;
                    lamp.intensity = red ? redIntensity : whiteIntensity;
                    lamp.shadows = lightsCastShadows ? LightShadows.Soft : LightShadows.None;
                    lights++;
                }

                CeilingLightPulse p = fx.AddComponent<CeilingLightPulse>();
                p.lamp = lamp;
                p.fixture = fx.GetComponent<MeshRenderer>();
                p.isRed = red;
                p.color = red ? redColor : whiteColor;
                p.maxIntensity = red ? redIntensity : whiteIntensity;
                p.baseRange = red ? redRange : whiteRange;
                p.emissionBoost = red ? redEmissionBoost : whiteEmissionBoost;
                p.pulseSpeed = redPulseSpeed * (0.85f + 0.3f * (float)rng.NextDouble());
                p.phase = (float)rng.NextDouble() * Mathf.PI * 2f;
                p.dimLevel = dimLevel;
                p.redBecomeWhite = redBecomeWhite;
                p.restoredColor = restoredColor;
                p.restoredIntensity = restoredIntensity;
                p.restoredRange = restoredRange;
                p.restoredBoost = restoredEmissionBoost;
                p.restoreDelay = (float)rng.NextDouble() * restoreSpread;

                count++;
                if (red) reds++;
            }
        }

        List<float> pipeLines = new List<float>();
        int pipes = 0;
        if (buildPipes && nx > 1)
        {
            for (int i = 1; i < nx; i += Mathf.Max(1, pipeEveryNthLine))
            {
                float lx = center.x - size.x * 0.5f + i * size.x / nx;
                pipeLines.Add(lx);
                BuildPipeRun(root.transform, lx, center.z, size.y, ceilingY, rng, pipeMaterial);
                pipes++;
            }
        }

        int drips = 0;
        if (buildSprinklers && sprinklerCount > 0)
            drips = BuildSprinklers(root.transform, center, size, floorY, ceilingY, fixturePos, rng, lit, pipeLines);

        Debug.Log("Ceiling built: " + count + " fixtures (" + reds + " red), " + lights + " real lights, " + drips + " sprinklers, " + pipes + " pipe runs.");
    }

    int BuildSprinklers(Transform root, Vector3 center, Vector2 size, float floorY, float ceilingY,
                        List<Vector2> fixturePos, System.Random rng, Shader lit, List<float> pipeLines)
    {
        FacilityDrips system = GetComponent<FacilityDrips>();
        if (system == null) system = gameObject.AddComponent<FacilityDrips>();
        system.drips.Clear();

        Material metal = MakeLit(lit, new Color(0.35f, 0.35f, 0.33f), 0.5f);
        Material glass = MakeLit(lit, new Color(0.85f, 0.05f, 0.04f), 0.9f);
        Material water = MakeLit(lit, new Color(0.65f, 0.78f, 0.85f), 0.98f);

        int n = Mathf.Min(16, sprinklerCount);
        int made = 0;
        for (int i = 0; i < n; i++)
        {
            Vector2 pos = Vector2.zero;
            if (pipeLines.Count > 0)
            {
                // Hang each sprinkler off a pipe run with a thin feeder line.
                float line = pipeLines[rng.Next(pipeLines.Count)];
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float px = line + side * (0.7f + (float)rng.NextDouble() * 0.7f);
                float pz = center.z + ((float)rng.NextDouble() - 0.5f) * (size.y - 2f);
                pos = new Vector2(px, pz);
                GameObject feeder = Part(PrimitiveType.Cylinder, "Feeder", root,
                    new Vector3((line + px) * 0.5f, ceilingY - 0.04f, pz),
                    new Vector3(0.04f, Mathf.Abs(px - line) * 0.5f, 0.04f), metal);
                feeder.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
            else
            {
                bool ok = false;
                for (int attempt = 0; attempt < 40 && !ok; attempt++)
                {
                    float x = center.x + ((float)rng.NextDouble() - 0.5f) * (size.x - 2f);
                    float z = center.z + ((float)rng.NextDouble() - 0.5f) * (size.y - 2f);
                    ok = true;
                    foreach (Vector2 f in fixturePos)
                    {
                        if (Mathf.Abs(f.x - x) < 0.8f && Mathf.Abs(f.y - z) < 1.2f) { ok = false; break; }
                    }
                    pos = new Vector2(x, z);
                }
            }

            GameObject head = new GameObject("Sprinkler");
            head.transform.SetParent(root, true);
            head.transform.position = new Vector3(pos.x, ceilingY, pos.y);

            Part(PrimitiveType.Cylinder, "Pipe", head.transform, new Vector3(pos.x, ceilingY - 0.04f, pos.y), new Vector3(0.05f, 0.04f, 0.05f), metal);
            Part(PrimitiveType.Sphere, "Bulb", head.transform, new Vector3(pos.x, ceilingY - 0.105f, pos.y), new Vector3(0.03f, 0.05f, 0.03f), glass);
            Part(PrimitiveType.Cylinder, "Deflector", head.transform, new Vector3(pos.x, ceilingY - 0.15f, pos.y), new Vector3(0.12f, 0.004f, 0.12f), metal);

            Vector3 spawn = new Vector3(pos.x, ceilingY - 0.17f, pos.y);
            GameObject drop = Part(PrimitiveType.Sphere, "Drop", head.transform, spawn, Vector3.zero, water);

            float fall = Mathf.Sqrt(2f * (spawn.y - floorY) / 9.81f);
            float period = fall + FacilityDrips.RippleLife + 1f + (float)rng.NextDouble() * 2.5f;

            FacilityDrips.Drip d = new FacilityDrips.Drip();
            d.drop = drop.transform;
            d.head = spawn;
            d.floorY = floorY;
            d.fall = fall;
            d.period = period;
            d.phase = (float)rng.NextDouble() * period;
            system.drips.Add(d);
            made++;
        }
        return made;
    }

    void BuildPipeRun(Transform root, float x, float centerZ, float lengthZ, float ceilingY, System.Random rng, Material mat)
    {
        float diam = Mathf.Lerp(pipeDiameterRange.x, pipeDiameterRange.y, (float)rng.NextDouble());
        float r = diam * 0.5f;
        float y = ceilingY - r - 0.06f;
        Quaternion alongZ = Quaternion.Euler(90f, 0f, 0f);

        GameObject pipe = Part(PrimitiveType.Cylinder, "Pipe_Run", root,
            new Vector3(x, y, centerZ), new Vector3(diam, lengthZ * 0.5f, diam), mat);
        pipe.transform.rotation = alongZ;

        float zStart = centerZ - lengthZ * 0.5f;

        // Flanges every 4 m
        for (float z = zStart + 2f; z < zStart + lengthZ; z += 4f)
        {
            GameObject ring = Part(PrimitiveType.Cylinder, "Joint", root,
                new Vector3(x, y, z), new Vector3(diam * 1.25f, 0.05f, diam * 1.25f), mat);
            ring.transform.rotation = alongZ;
        }

        // Hangers every 2.5 m
        float h = ceilingY - y;
        for (float z = zStart + 1f; z < zStart + lengthZ; z += 2.5f)
        {
            Part(PrimitiveType.Cube, "Hanger", root,
                new Vector3(x, ceilingY - h * 0.5f, z), new Vector3(diam * 0.6f, h, 0.06f), mat);
        }

        // Thin conduit alongside half of the runs
        if (rng.NextDouble() < 0.5)
        {
            float side = rng.NextDouble() < 0.5 ? -1f : 1f;
            GameObject conduit = Part(PrimitiveType.Cylinder, "Conduit", root,
                new Vector3(x + side * (r + 0.07f), ceilingY - 0.04f, centerZ),
                new Vector3(0.05f, lengthZ * 0.5f, 0.05f), mat);
            conduit.transform.rotation = alongZ;
        }
    }

    [ContextMenu("Clear Ceiling")]
    public void Clear()
    {
        FacilityDrips system = GetComponent<FacilityDrips>();
        if (system != null) system.drips.Clear();

        Transform old = transform.Find(RootName);
        if (old == null) return;
        if (Application.isPlaying) Destroy(old.gameObject);
        else DestroyImmediate(old.gameObject);
    }
}