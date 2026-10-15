using UnityEngine;


public class CeilingLightPulse : MonoBehaviour
{
    public Light lamp;            
    public Renderer fixture;
    public bool isRed = true;
    public Color color = Color.red;
    public float maxIntensity = 8f;
    public float baseRange = 10f;
    public float emissionBoost = 4f;
    public float pulseSpeed = 2.5f;
    public float minPulse = 0.05f;
    public float phase;

    [Header("Power")]
    public float dimLevel = 0.12f;
    public bool redBecomeWhite = true;
    public Color restoredColor = Color.white;
    public float restoredIntensity = 6f;
    public float restoredRange = 12f;
    public float restoredBoost = 4f;
    public float restoreDelay;
    public float flickerTime = 0.8f;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock block;
    float nextGlitch;
    float glitchEnd;

    void OnEnable()
    {
        block = new MaterialPropertyBlock();
        nextGlitch = Time.time + Random.Range(0f, 15f);
        glitchEnd = 0f;
    }

    void Update()
    {
        Color c = color;
        float inten = maxIntensity;
        float boost = emissionBoost;
        float rng = baseRange;
        float k;

        float since = -1f;
        bool useRestored = false;
        if (FacilityPower.IsRestored)
        {
            since = Time.time - FacilityPower.RestoredAt - restoreDelay;
            useRestored = since >= 0f && (!isRed || redBecomeWhite);
        }

        if (useRestored)
        {
            c = restoredColor;
            inten = restoredIntensity;
            boost = restoredBoost;
            rng = restoredRange;
            k = since < flickerTime ? (Random.value > 0.5f ? 1f : 0.05f) : 1f;
        }
        else if (isRed)
        {
            float s = Mathf.Sin(Time.time * pulseSpeed + phase) * 0.5f + 0.5f;
            k = Mathf.Lerp(minPulse, 1f, s * s);
        }
        else
        {
            k = dimLevel;
            if (Time.time >= nextGlitch)
            {
                glitchEnd = Time.time + Random.Range(0.1f, 0.5f);
                nextGlitch = glitchEnd + Random.Range(3f, 15f);
            }
            if (Time.time < glitchEnd)
                k = Random.value > 0.5f ? 0.02f : dimLevel;
        }

        if (lamp != null)
        {
            lamp.color = c;
            lamp.range = rng;
            lamp.intensity = inten * k;
        }

        if (fixture != null)
        {
            Color glow = c * boost * k;
            block.SetColor(BaseColorId, glow);
            block.SetColor(EmissionId, glow);
            fixture.SetPropertyBlock(block);
        }
    }
}
