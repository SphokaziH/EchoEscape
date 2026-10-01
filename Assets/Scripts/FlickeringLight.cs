using UnityEngine;

/// Horror-style light behaviour:
/// - Idle: subtle continuous flickering
/// - Dying: stronger flickering with brief blackouts
/// - Alert: red pulsing light
/// - Off: completely disabled
///
/// Designed for Unity 6 / URP point lights.

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    public enum FlickerMode
    {
        Idle,
        Dying,
        Alert,
        Off
    }

    [Header("General")]
    public FlickerMode mode = FlickerMode.Idle;

    [Tooltip("The Light component this script controls.")]
    public Light targetLight;

    [Tooltip("Base brightness used by Dying and Alert modes.")]
    public float baseIntensity = 600f;

    [Header("Idle Flicker")]
    [Tooltip("Minimum brightness during normal flickering.")]
    public float idleMinIntensity = 400f;

    [Tooltip("Maximum brightness during normal flickering.")]
    public float idleMaxIntensity = 1000f;

    [Tooltip("How quickly the light flickers.")]
    public float idleNoiseSpeed = 1.5f;

    [Header("Dying Flicker")]
    [Range(0f, 1f)]
    [Tooltip("Chance of the light suddenly turning off each frame.")]
    public float dyingFlickerChance = 0.05f;

    [Tooltip("Minimum duration of a blackout.")]
    public float dyingOffMin = 0.05f;

    [Tooltip("Maximum duration of a blackout.")]
    public float dyingOffMax = 0.25f;

    [Header("Alert State")]
    [Tooltip("Color used when the light enters Alert mode.")]
    public Color alertColor = new Color(1f, 0.05f, 0.02f);

    [Tooltip("Speed of the red alert pulse.")]
    public float alertPulseSpeed = 4f;

    [Header("Light Settings")]
    [Tooltip("Automatically set the light's range when the game starts.")]
    public bool overrideRange = true;

    [Tooltip("Recommended indoor range for this light.")]
    public float lightRange = 12f;

    [Header("Optional Audio")]
    public AudioSource audioSource;
    public AudioClip idleHum;
    public AudioClip alertAlarm;

    private Color baseColor;
    private float noiseOffset;

    private bool isOff;
    private float offTimer;

    private void Awake()
    {
        // Automatically find the Light component.
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        // Safety check.
        if (targetLight == null)
        {
            Debug.LogError(
                "FlickeringLight requires a Light component.",
                this
            );

            enabled = false;
            return;
        }

        // Remember the original light color.
        baseColor = targetLight.color;

        // Give each light a different flicker pattern.
        noiseOffset = Random.Range(0f, 100f);

        // Set a sensible indoor range.
        if (overrideRange)
        {
            targetLight.range = lightRange;
        }

        // Make sure the light starts in the correct state.
        targetLight.enabled = mode != FlickerMode.Off;
    }

    private void Start()
    {
        // Start the idle hum if one has been assigned.
        if (mode != FlickerMode.Off)
        {
            PlayLoop(idleHum);
        }
    }

    private void Update()
    {
        if (targetLight == null)
        {
            return;
        }

        switch (mode)
        {
            case FlickerMode.Idle:
                HandleIdleFlicker();
                break;

            case FlickerMode.Dying:
                HandleDyingFlicker();
                break;

            case FlickerMode.Alert:
                HandleAlert();
                break;

            case FlickerMode.Off:
                HandleOff();
                break;
        }
    }

    // ============================================================
    // IDLE MODE
    // ============================================================

    private void HandleIdleFlicker()
    {
        targetLight.enabled = true;

        // Perlin noise creates a smooth, natural-looking flicker
        // instead of random harsh brightness changes.
        float noise = Mathf.PerlinNoise(
            Time.time * idleNoiseSpeed,
            noiseOffset
        );

        targetLight.intensity = Mathf.Lerp(
            idleMinIntensity,
            idleMaxIntensity,
            noise
        );

        // Restore the normal light color.
        targetLight.color = baseColor;
    }

    // ============================================================
    // DYING MODE
    // ============================================================

    private void HandleDyingFlicker()
    {
        // If the light is currently in a blackout.
        if (isOff)
        {
            offTimer -= Time.deltaTime;

            if (offTimer <= 0f)
            {
                isOff = false;
                targetLight.enabled = true;
            }

            return;
        }

        targetLight.enabled = true;

        // Faster and more aggressive flickering.
        float noise = Mathf.PerlinNoise(
            Time.time * idleNoiseSpeed * 2f,
            noiseOffset
        );

        targetLight.intensity = Mathf.Lerp(
            idleMinIntensity * 0.1f,
            baseIntensity,
            noise
        );

        targetLight.color = baseColor;

        // Random chance of a blackout.
        if (Random.value < dyingFlickerChance * Time.deltaTime * 60f)
        {
            isOff = true;

            offTimer = Random.Range(
                dyingOffMin,
                dyingOffMax
            );

            targetLight.enabled = false;
        }
    }

    // ============================================================
    // ALERT MODE
    // ============================================================

    private void HandleAlert()
    {
        targetLight.enabled = true;

        // Smooth pulse between 0 and 1.
        float pulse =
            (Mathf.Sin(Time.time * alertPulseSpeed) * 0.5f)
            + 0.5f;

        targetLight.color = alertColor;

        targetLight.intensity = Mathf.Lerp(
            baseIntensity * 0.5f,
            baseIntensity * 1.5f,
            pulse
        );
    }

    // ============================================================
    // OFF MODE
    // ============================================================

    private void HandleOff()
    {
        targetLight.enabled = false;
    }

    // ============================================================
    // AUDIO
    // ============================================================

    private void PlayLoop(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    // ============================================================
    // CHANGE MODE
    // ============================================================

    /// <summary>
    /// Changes the current light behaviour.
    /// Can be called by NPC AI, triggers, scripts, etc.
    /// </summary>
    public void SetMode(FlickerMode newMode)
    {
        mode = newMode;

        // Reset blackout state.
        isOff = false;
        offTimer = 0f;

        // Enable/disable the light immediately.
        targetLight.enabled = newMode != FlickerMode.Off;

        // Handle audio.
        if (newMode == FlickerMode.Alert)
        {
            if (audioSource != null && alertAlarm != null)
            {
                audioSource.Stop();

                audioSource.clip = alertAlarm;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else if (newMode != FlickerMode.Off)
        {
            PlayLoop(idleHum);
        }
        else
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }

    // ============================================================
    // PUBLIC HELPER METHODS
    // ============================================================

    /// <summary>
    /// Turns the light on normally.
    /// </summary>
    public void TurnOn()
    {
        SetMode(FlickerMode.Idle);
    }

    /// <summary>
    /// Turns the light completely off.
    /// </summary>
    public void TurnOff()
    {
        SetMode(FlickerMode.Off);
    }

    /// <summary>
    /// Starts the dying/stuttering light effect.
    /// </summary>
    public void StartDying()
    {
        SetMode(FlickerMode.Dying);
    }

    /// <summary>
    /// Starts the red alert effect.
    /// </summary>
    public void StartAlert()
    {
        SetMode(FlickerMode.Alert);
    }
}