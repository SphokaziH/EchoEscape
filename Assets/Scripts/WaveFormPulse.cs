using UnityEngine;
using UnityEngine.UI;

public class WaveformPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float pulseSpeed = 1.2f;
    public float minAlpha = 0.55f;
    public float maxAlpha = 1f;

    private Image waveformImage;

    void Start()
    {
        waveformImage = GetComponent<Image>();
    }

    void Update()
    {
        if (waveformImage == null)
            return;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);

        Color colour = waveformImage.color;
        colour.a = alpha;
        waveformImage.color = colour;
    }
}