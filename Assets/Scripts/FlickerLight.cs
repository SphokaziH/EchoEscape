using UnityEngine;


/// Works for flickering fluorescent tubes AND pulsing red emergency lights.
public class FlickerLight : MonoBehaviour
{
    public enum Mode { Fluorescent, Emergency }

    [Header("Setup")]
    public Mode mode = Mode.Fluorescent;
    public Light targetLight;
    public Renderer emissiveRenderer;
    public AudioSource buzz;                 // optional looping hum

    [Header("Look")]
    public Color lightColor = new Color(0.85f, 0.95f, 1f);
    public float maxIntensity = 3f;          
    public float emissionBoost = 2f;

    [Header("Fluorescent flicker")]
    [Range(0f, 1f)] public float brokenChance = 0.02f;   
    public Vector2 stutterDuration = new Vector2(0.1f, 0.6f);

    [Header("Emergency pulse")]
    public float pulseSpeed = 3f;
    [Range(0f, 1f)] public float minPulse = 0.1f;

    [Header("Power")]
    public bool powered = true;              

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock block;
    float stutterTimer;
    float current;

    void Awake()
    {
        if (!targetLight) targetLight = GetComponentInChildren<Light>();
        block = new MaterialPropertyBlock();
        targetLight.color = lightColor;
    }

    void Update()
    {
        float level = 0f;

        if (powered)
        {
            if (mode == Mode.Fluorescent)
            {
                if (stutterTimer > 0f)
                {
                    stutterTimer -= Time.deltaTime;
                    // rapid random on/off while stuttering
                    level = Random.value > 0.5f ? Random.Range(0.4f, 1f) : Random.Range(0f, 0.15f);
                }
                else
                {
                    level = 1f;
                    if (Random.value < brokenChance)
                        stutterTimer = Random.Range(stutterDuration.x, stutterDuration.y);
                }
            }
            else // Emergency
            {
                float s = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                level = Mathf.Lerp(minPulse, 1f, s * s); // squared = sharper pulse
            }
        }

        // slight smoothing so it does not look digital
        current = Mathf.Lerp(current, level, 1f - Mathf.Exp(-40f * Time.deltaTime));

        targetLight.intensity = maxIntensity * current;

        if (emissiveRenderer)
        {
            emissiveRenderer.GetPropertyBlock(block);
            block.SetColor(EmissionColor, lightColor * (current * emissionBoost));
            emissiveRenderer.SetPropertyBlock(block);
        }

        if (buzz) buzz.volume = powered ? Mathf.Lerp(0.2f, 1f, current) : 0f;
    }

    // Call from your power system / generator logic
    public void SetPowered(bool value) => powered = value;
}
