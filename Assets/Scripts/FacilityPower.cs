using UnityEngine;

// Global power state. Call FacilityPower.Restore() from your generator puzzle.
public class FacilityPower : MonoBehaviour
{
    public bool startPowered = false;
    public float autoRestoreAfterSeconds = 0f;   // testing only: 0 = never

    public static bool IsRestored { get; private set; }
    public static float RestoredAt { get; private set; }

    float startTime;

    void Awake()
    {
        IsRestored = startPowered;
        RestoredAt = startPowered ? -999f : 0f;
        startTime = Time.time;
    }

    void Update()
    {
        if (!IsRestored && autoRestoreAfterSeconds > 0f && Time.time - startTime >= autoRestoreAfterSeconds)
            Restore();
    }

    public static void Restore()
    {
        if (IsRestored) return;
        IsRestored = true;
        RestoredAt = Time.time;
    }

    public static void Cut() { IsRestored = false; }

    [ContextMenu("Restore Power (Play mode)")]
    void RestoreFromMenu() { Restore(); }
}
